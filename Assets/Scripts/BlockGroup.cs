using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

public class BlockGroup
{
	private Dictionary<int, Block> blocks = new Dictionary<int, Block>();
	private BlockMeshMerger meshMerger = new BlockMeshMerger();

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

	// ブロック数を取得
	public int GetNumBlocks() {
		return this.blocks.Count;
	}

	// メッシュの更新
	public void UpdateMesh() {
		this.meshMerger.Clear();
		foreach (var block in this.blocks) {
			block.Value.WriteToMesh(this, this.meshMerger);
			block.Value.WriteToGuideMesh(this, this.meshMerger);
		}
	}
	public Mesh GetSurfaceMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "SurfaceBlocks";
		mesh.vertices = this.meshMerger.vertexPos.ToArray();
		mesh.uv       = this.meshMerger.vertexUv.ToArray();
		mesh.SetIndices(this.meshMerger.triangles.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}
	public Mesh GetWireMesh() {
		int trianglesCount = this.meshMerger.triangles.Count / 3;
		var tris = this.meshMerger.triangles;
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
		mesh.vertices = this.meshMerger.vertexPos.ToArray();
		mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
		mesh.RecalculateBounds();
		return mesh;
	}
	public Mesh GetColliderMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "ColliderBlocks";
		mesh.vertices = this.meshMerger.guideVertexPos.ToArray();
		mesh.SetIndices(this.meshMerger.guideTriangles.ToArray(), MeshTopology.Triangles, 0);
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
	
	public List<Vector3> guideVertexPos = new List<Vector3>();
	public List<int> guideTriangles = new List<int>();

	public void Clear() {
		this.vertexPos.Clear();
		this.vertexUv.Clear();
		this.triangles.Clear();
		this.guideVertexPos.Clear();
		this.guideTriangles.Clear();
	}
};
