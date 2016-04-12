using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class BlockMesh
{
	public List<Vector3> vertexPos = new List<Vector3>();
	public List<Vector2> vertexUv = new List<Vector2>();
	public List<int> trianglesIndices = new List<int>();
	public List<int> linesIndices = new List<int>();

	public void Clear() {
		this.vertexPos.Clear();
		this.vertexUv.Clear();
		this.trianglesIndices.Clear();
		this.linesIndices.Clear();
	}
};

public class BlockGroup
{
	private Dictionary<int, Block> blocks = new Dictionary<int, Block>();
	private BlockMesh blockMesh = new BlockMesh();

	public Block AddBlock(Block block) {
		if (this.blocks.ContainsKey(block.GetHashCode())) {
			return null;
		}
		this.blocks.Add(block.GetHashCode(), block);
		return block;
	}
	public Block RemoveBlock(Vector3 position) {
		Block block = null;
		int hash = Block.CalcHashCode(position);
		if (this.blocks.TryGetValue(hash, out block)) {
			this.blocks.Remove(hash);
			return block;
		}
		return null;
	}
	public void Clear() {
		this.blocks.Clear();
	}
	public int GetBlockCount() {
		return this.blocks.Count;
	}
	public Block GetBlock(Vector3 position) {
		int key = Block.CalcHashCode(position);
		Block value;
		if (this.blocks.TryGetValue(key, out value)) {
			return value;
		}
		return null;
	}
	public Block[] GetAllBlocks() {
		var values = this.blocks.Values;
		Block[] blocks = new Block[values.Count];
		values.CopyTo(blocks,0);
		return blocks;
	}

	public void UpdateMesh() {
		this.blockMesh.Clear();
		foreach (var block in this.blocks) {
			block.Value.WriteToMesh(this, this.blockMesh);
		}
	}
	public Mesh GetSurfaceMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "SurfaceBlocks";
		mesh.vertices = this.blockMesh.vertexPos.ToArray();
		mesh.uv       = this.blockMesh.vertexUv.ToArray();
		mesh.SetIndices(this.blockMesh.trianglesIndices.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}
	public Mesh GetWireMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "WireBlocks";
		mesh.vertices = this.blockMesh.vertexPos.ToArray();
		mesh.SetIndices(this.blockMesh.linesIndices.ToArray(), MeshTopology.Lines, 0);
		mesh.RecalculateBounds();
		return mesh;
	}
	public Mesh GetColliderMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "ColliderBlocks";
		mesh.vertices = this.blockMesh.vertexPos.ToArray();
		mesh.SetIndices(this.blockMesh.trianglesIndices.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		return mesh;
	}

	public void Serialize(XmlElement node) {
		foreach (var keyValue in this.blocks) {
			XmlElement blockNode = node.OwnerDocument.CreateElement("block");
			Block block = keyValue.Value;
			block.Serialize(blockNode);
			node.AppendChild(blockNode);
		}
	}
	public void Deserialize(XmlElement node) {
		XmlNodeList blockList = node.GetElementsByTagName("block");
		for (int i = 0; i < blockList.Count; i++) {
			XmlElement blockNode = blockList[i] as XmlElement;
			Block block = Block.Create(blockNode.GetAttribute("type"));
			block.Deserialize(blockNode);
			this.AddBlock(block);
		}
	}
}

public class Block
{
	public enum Direction {
		Zplus,
		Zminus,
		Xplus,
		Xminus,
		Yplus,
		Yminus,
	};
	public static readonly Vector3[] neighborOffsets = {
		new Vector3( 0,  0,  1),
		new Vector3( 0,  0, -1),
		new Vector3( 1,  0,  0),
		new Vector3(-1,  0,  0),
		new Vector3( 0,  0.5f,  0),
		new Vector3( 0, -0.5f,  0),
	};
	
	public static Block Create(string typeName) {
		switch (typeName) {
		case "cube":
			return new CubeBlock();
		default:
			return new CubeBlock();
		}
	}
	public static int CalcHashCode(Vector3 position) {
		int x = Mathf.RoundToInt(position.x) + 512;
		int y = Mathf.RoundToInt(position.y * 2) + 512;
		int z = Mathf.RoundToInt(position.z) + 512;
		return ((z & 0x3f) << 20) | ((y & 0x3f) << 10) | (x & 0x3f);
	}

	public virtual string typeName {
		get {return "";}
	}
	public Vector3 position {get; private set;}
	public Vector3 realPosition {get; private set;}
	
	private int hashCode;

	public Block() {
		this.SetPosition(Vector3.zero);
	}

	public void SetPosition(Vector3 position) {
		this.position = position;
		this.hashCode = Block.CalcHashCode(position);
	}

	public virtual void WriteToMesh(BlockGroup group, BlockMesh mesh) {
	}

	public virtual bool IsBlockage(Direction direction) {
		return false;
	}

	public override int GetHashCode() {
		return this.hashCode;
	}

	public virtual void Serialize(XmlElement node) {
		node.SetAttribute("type", this.typeName);
		node.SetAttribute("x", this.position.x.ToString());
		node.SetAttribute("y", this.position.y.ToString());
		node.SetAttribute("z", this.position.z.ToString());
	}
	public virtual void Deserialize(XmlElement node) {
		Vector3 position = Vector3.zero;
		float.TryParse(node.Attributes["x"].Value, out position.x);
		float.TryParse(node.Attributes["y"].Value, out position.y);
		float.TryParse(node.Attributes["z"].Value, out position.z);
		this.SetPosition(position);
	}
};

public class CubeBlock : Block
{
	public override string typeName {
		get {return "cube";}
	}

	public override void WriteToMesh(BlockGroup group, BlockMesh mesh) {
		for (int i = 0; i < 6; i++) {
			int offset = mesh.vertexPos.Count;
			
			Block neighbor = group.GetBlock(this.position + Block.neighborOffsets[i]);
			if (neighbor != null && neighbor.IsBlockage((Block.Direction)(i ^ 1))) {
				continue;
			}

			for (int j = 0; j < 4; j++) {
				mesh.vertexPos.Add(this.position + EditUtil.blockVertexPoints[EditUtil.blockFaceIndices[i][j]] * 0.5f);
				mesh.vertexUv.Add(EditUtil.blockVertexUvs[j]);
			}
			
			for (int j = 0; j < 6; j++) {
				mesh.trianglesIndices.Add(offset + EditUtil.trianglesIndices[j]);
			}
			for (int j = 0; j < 8; j++) {
				mesh.linesIndices.Add(offset + EditUtil.linesIndices[j]);
			}
		}
	}
	
	public override bool IsBlockage(Direction direction) {
		return true;
	}
	
	public override void Serialize(XmlElement node) {
		base.Serialize(node);
	}
	public override void Deserialize(XmlElement node) {
		base.Deserialize(node);
	}
};
