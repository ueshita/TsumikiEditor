using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

public class BlockGroup
{
	private Dictionary<int, Block> blocks = new Dictionary<int, Block>();
	private BlockMeshMerger surfaceMeshMerger = new BlockMeshMerger();
	private BlockMeshMerger guideMeshMerger = new BlockMeshMerger();
	private BlockMeshMerger routeMeshMerger = new BlockMeshMerger();

	// ブロック追加
	public void AddBlock(Block block) {
		if (this.blocks.ContainsKey(block.GetHashCode())) {
			return;
		}
		this.blocks.Add(block.GetHashCode(), block);
	}
	// ブロック削除
	public void RemoveBlock(Block block) {
		this.blocks.Remove(block.GetHashCode());
	}
	// 全クリア
	public void Clear() {
		this.blocks.Clear();
	}
	
	// ブロック数の取得
	public int GetBlockCount() {
		return this.blocks.Count;
	}
	
	// 位置指定でブロックの取得
	public Block GetBlock(Vector3 position) {
		int key = Block.CalcHashCode(position);
		Block value;
		if (this.blocks.TryGetValue(key, out value)) {
			return value;
		}
		return null;
	}

	// 全ブロック取得
	public Block[] GetAllBlocks() {
		var values = this.blocks.Values;
		Block[] blocks = new Block[values.Count];
		values.CopyTo(blocks,0);
		return blocks;
	}

	// 移動可能な全ブロックを取得
	public Block[] GetMovableBlocks() {
		List<Block> resultBlocks = new List<Block>();
		foreach (var keyValue in this.blocks) {
			Block block = keyValue.Value;
			if (block.IsMovable(this)) {
				resultBlocks.Add(block);
			}
		}
		return resultBlocks.ToArray();
	}

	// ブロック数を取得
	public int GetNumBlocks() {
		return this.blocks.Count;
	}

	// メッシュの更新
	public void UpdateMesh() {
		this.surfaceMeshMerger.Clear();
		this.guideMeshMerger.Clear();
		this.routeMeshMerger.Clear();
		foreach (var keyValue in this.blocks) {
			var block = keyValue.Value;
			block.WriteToMesh(this, this.surfaceMeshMerger);
			block.WriteToGuideMesh(this, this.guideMeshMerger);
			block.WriteToRouteMesh(this, this.routeMeshMerger);
		}
	}
	public Mesh GetSurfaceMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "SurfaceBlocks";
		mesh.vertices = this.surfaceMeshMerger.vertexPos.ToArray();
		mesh.uv       = this.surfaceMeshMerger.vertexUv.ToArray();
		mesh.SetIndices(this.surfaceMeshMerger.triangles.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	public Mesh GetWireMesh() {
		int trianglesCount = this.surfaceMeshMerger.triangles.Count / 3;
		var tris = this.surfaceMeshMerger.triangles;
		List<int> lines = new List<int>(trianglesCount * 6);

		for (int i = 0; i < trianglesCount; i++) {
			int i0 = tris[i * 3 + 0];
			int i1 = tris[i * 3 + 1];
			int i2 = tris[i * 3 + 2];
			if (tris.Count >= i * 3 + 6 && 
				tris[i * 3 + 3] == i0 && tris[i * 3 + 4] == i2
			) {
				// 四角形を作る
				int i3 = tris[i * 3 + 5];
				lines.Add(i0); lines.Add(i1);
				lines.Add(i1); lines.Add(i2);
				lines.Add(i2); lines.Add(i3);
				lines.Add(i3); lines.Add(i0);
				i += 1;
			} else {
				// 三角形を作る
				lines.Add(i0); lines.Add(i1);
				lines.Add(i1); lines.Add(i2);
				lines.Add(i2); lines.Add(i0);
			}
		}

		Mesh mesh = new Mesh();
		mesh.name = "WireBlocks";
		mesh.vertices = this.surfaceMeshMerger.vertexPos.ToArray();
		mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
		mesh.RecalculateBounds();
		return mesh;
	}
	public Mesh GetGuideMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "ColliderBlocks";
		mesh.vertices = this.guideMeshMerger.vertexPos.ToArray();
		mesh.SetIndices(this.guideMeshMerger.triangles.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		return mesh;
	}
	public Mesh GetRouteMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "RoutePanels";
		mesh.vertices = this.routeMeshMerger.vertexPos.ToArray();
		mesh.SetIndices(this.routeMeshMerger.triangles.ToArray(), MeshTopology.Triangles, 0);
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
			Block block = new Block(blockNode);
			this.AddBlock(block);
		}
	}
}

// ブロックのメッシュをまとめるクラス
public class BlockMeshMerger
{
	public List<Vector3> vertexPos = new List<Vector3>();
	public List<Vector2> vertexUv = new List<Vector2>();
	public List<int> triangles = new List<int>();
	
	public void Clear() {
		this.vertexPos.Clear();
		this.vertexUv.Clear();
		this.triangles.Clear();
	}
};
