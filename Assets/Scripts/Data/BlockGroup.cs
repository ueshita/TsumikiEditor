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

	public void SetSurfaceSubMesh(Vector3i subMeshDivs) {
		this.surfaceMeshMerger.SetSubMesh(subMeshDivs);
	}
	
	// ブロック数の取得
	public int GetBlockCount() {
		return this.blocks.Count;
	}
	
	// 位置指定でブロックの取得
	public Block GetBlock(Vector3 position) {
		int key = EditUtil.PositionToHashCode(position);
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
		values.CopyTo(blocks, 0);
		return blocks;
	}

	// ユニットが移動可能な全ブロックを取得
	public Block[] GetEnterableBlocks() {
		List<Block> resultBlocks = new List<Block>();
		foreach (var keyValue in this.blocks) {
			Block block = keyValue.Value;
			if (block.IsEnterable(this)) {
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
		return this.surfaceMeshMerger.GetMesh();
	}
	
	public Mesh[] GetSurfaceSubMeshes() {
		return this.surfaceMeshMerger.GetSubMeshes();
	}

	public Mesh GetWireMesh() {
		int indexCount = this.surfaceMeshMerger.triangles.Count;
		var tris = this.surfaceMeshMerger.triangles;
		List<int> lines = new List<int>(indexCount);

		for (int i = 0; i < indexCount; i += 3) {
			int i0 = tris[i + 0];
			int i1 = tris[i + 1];
			int i2 = tris[i + 2];
			int i3 = -1;

			if (tris.Count >= i + 6) {
				if (tris[i + 3] == i0 && tris[i + 4] == i2) {
					i3 = tris[i + 5];
				} else if (tris[i + 3] == i2 && tris[i + 5] == i0 ) {
					i3 = tris[i + 4];
				} else if (tris[i + 4] == i0 && tris[i + 5] == i2 ) {
					i3 = tris[i + 3];
				}
			}

			if (i3 >= 0) {
				// 四角形を作る
				lines.Add(i0); lines.Add(i1);
				lines.Add(i1); lines.Add(i2);
				lines.Add(i2); lines.Add(i3);
				lines.Add(i3); lines.Add(i0);
				i += 3;
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
	public List<Vector3> vertexNormal = new List<Vector3>();
	public List<Vector2> vertexUv = new List<Vector2>();
	public List<Vector2> vertexMeta = new List<Vector2>();
	public List<int> triangles = new List<int>();
	
	public Dictionary<int, BlockMeshMerger> subMeshMerger = null;
	public Vector3i subMeshDivs;
	
	// サブメッシュを設定
	public void SetSubMesh(Vector3i subMeshDivs) {
		this.subMeshMerger = new Dictionary<int, BlockMeshMerger>();
		this.subMeshDivs = subMeshDivs;
	}

	public void Clear() {
		this.vertexPos.Clear();
		this.vertexNormal.Clear();
		this.vertexUv.Clear();
		this.vertexMeta.Clear();
		this.triangles.Clear();
		
		if (this.subMeshMerger != null) {
			this.subMeshMerger.Clear();
		}
	}

	// ブロックメッシュをマージ
	public void Merge(Mesh mesh, Vector3 position, BlockDirection direction, Vector3 scale,
		bool divideChipVert, int textureId, int meshId) {
		var chip = TexturePalette.Instance.GetChip(textureId);
		
		int vertexOffset = this.vertexPos.Count;
		Vector3[] vertexPos = mesh.vertices;
		Vector3[] vertexNormal = mesh.normals;
		Vector2[] vertexUv = mesh.uv;
		int[] indices = mesh.GetIndices(0);
		float[] heights = new float[vertexUv.Length];

		// Vが0.0～1.0の範囲外の場合は高さを調整する
		for (int j = 0; j < indices.Length; j += 3) {
			Vector2 uv0 = vertexUv[indices[j + 0]];
			Vector2 uv1 = vertexUv[indices[j + 1]];
			Vector2 uv2 = vertexUv[indices[j + 2]];
			
			float height = position.y;

			if (uv0.y < 0.0f || uv1.y < 0.0f || uv2.y < 0.0f) {
				height += 0.5f;
			} else if (uv0.y > 1.0f || uv1.y > 1.0f || uv2.y > 1.0f) {
				height -= 0.5f;
			}

			heights[indices[j + 0]] = height;
			heights[indices[j + 1]] = height;
			heights[indices[j + 2]] = height;
		}

		for (int j = 0; j < vertexPos.Length; j++) {
			Vector3 localPosition = Vector3.Scale(vertexPos[j], Vector3.Scale(new Vector3(-1, 1, -1), scale));
			Vector3 localNormal = Vector3.Scale(vertexNormal[j], new Vector3(-1, 1, -1));
			this.vertexPos.Add(position + EditUtil.RotatePosition(localPosition, direction));
			this.vertexNormal.Add(EditUtil.RotatePosition(localNormal, direction));
			this.vertexUv.Add(chip.ApplyUV(vertexUv[j], divideChipVert, heights[j]));
			this.vertexMeta.Add(new Vector2((float)meshId, 0.0f));
		}
		for (int j = 0; j < indices.Length; j++) {
			this.triangles.Add(vertexOffset + indices[j]);
		}

		if (this.subMeshMerger != null) {
			int key = EditUtil.PositionToHashCode(new Vector3(
				Mathf.Floor( position.x / this.subMeshDivs.x),
				Mathf.Floor( position.y / this.subMeshDivs.y),
				Mathf.Floor(-position.z / this.subMeshDivs.z)));

			if (!this.subMeshMerger.ContainsKey(key)) {
				this.subMeshMerger.Add(key, new BlockMeshMerger());
			}
			this.subMeshMerger[key].Merge(mesh, position, direction, scale, divideChipVert, textureId, meshId);
		}
	}
	
	// メインメッシュを取得
	public Mesh GetMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "SurfaceBlocks";
		mesh.vertices = this.vertexPos.ToArray();
		mesh.normals  = this.vertexNormal.ToArray();
		mesh.uv       = this.vertexUv.ToArray();
		mesh.uv2      = this.vertexMeta.ToArray();
		mesh.SetIndices(this.triangles.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		return mesh;
	}
	
	// サブメッシュを取得
	public Mesh[] GetSubMeshes() {
		if (this.subMeshMerger != null) {
			List<Mesh> meshes = new List<Mesh>();
			foreach (var keyValue in this.subMeshMerger) {
				meshes.Add(keyValue.Value.GetMesh());
			}
			return meshes.ToArray();
		}
		return null;
	}
};
