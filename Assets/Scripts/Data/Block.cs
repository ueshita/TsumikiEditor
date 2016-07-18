using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

public enum BlockDirection {
	Zplus,
	Zminus,
	Xplus,
	Xminus,
	Yplus,
	Yminus,
};

// 基本ブロック
public class Block
{
	// 隣接ブロックへのオフセット
	public static readonly Vector3[] neighborOffsets = {
		new Vector3( 0.0f,  0.0f,  1.0f),
		new Vector3( 0.0f,  0.0f, -1.0f),
		new Vector3( 1.0f,  0.0f,  0.0f),
		new Vector3(-1.0f,  0.0f,  0.0f),
		new Vector3( 0.0f,  0.5f,  0.0f),
		new Vector3( 0.0f, -0.5f,  0.0f),
	};
	
	public static int CalcHashCode(Vector3 position) {
		int x = Mathf.RoundToInt(position.x) + 512;
		int y = Mathf.RoundToInt(position.y * 2) + 512;
		int z = Mathf.RoundToInt(position.z) + 512;
		return ((z & 0x3f) << 20) | ((y & 0x3f) << 10) | (x & 0x3f);
	}

	public Vector3 position {get; private set;}
	public BlockDirection direction {get; private set;}
	public BlockShape shape {get; private set;}
	private int[] textureChips = new int[6];
	private bool meshIsDirty = true;
	private int hashCode;

	public override int GetHashCode() {
		return this.hashCode;
	}
	
	public Block(Vector3 position, BlockDirection direction)
		: this(position, direction, "cube") {
	}

	public Block(Vector3 position, BlockDirection direction, string typeName) {
		this.shape = BlockShape.Find(typeName);
		this.direction = direction;
		this.SetPosition(position);
	}

	public Block(XmlElement node) {
		this.Deserialize(node);
	}

	public void SetPosition(Vector3 position) {
		this.position = position;
		this.hashCode = Block.CalcHashCode(position);
		this.meshIsDirty = true;
	}

	public void SetDirection(BlockDirection direction) {
		this.direction = direction;
		this.meshIsDirty = true;
	}

	public int GetTextureChip(BlockDirection direction) {
		return this.textureChips[(int)direction];
	}

	public void SetTextureChip(BlockDirection direction, int textureChip) {
		this.textureChips[this.ToLocalDirection((int)direction)] = textureChip;
		this.meshIsDirty = true;
	}
	
	private static readonly int[,] ToWorldDirectionTable = new int[4, 6] {
		{0, 1, 2, 3, 4, 5}, {1, 0, 3, 2, 4, 5}, 
		{2, 3, 1, 0, 4, 5}, {3, 2, 0, 1, 4, 5}};
	private int ToWorldDirection(int index) {
		return ToWorldDirectionTable[(int)this.direction, index];
	}

	private static readonly int[,] ToLocalDirectionTable = new int[4, 6] {
		{0, 1, 2, 3, 4, 5}, {1, 0, 3, 2, 4, 5}, 
		{3, 2, 0, 1, 4, 5}, {2, 3, 1, 0, 4, 5}};
	private int ToLocalDirection(int index) {
		return ToLocalDirectionTable[(int)this.direction, index];
	}

	public int GetConnection(BlockDirection direction) {
		return this.shape.connection[ToLocalDirection((int)direction)];
	}

	// 隣のブロックとの閉塞チェック
	private bool CheckOcculusion(BlockGroup group, BlockDirection direction) {
		int dirIndex = (int)direction;
		// 隣のブロックに完全に覆われていたら省略する（閉塞チェック）
		Block neighborBlock = group.GetBlock(this.position + neighborOffsets[dirIndex]);
		if (neighborBlock != null) {
			int con1 = this.GetConnection((BlockDirection)(dirIndex));
			int con2 = neighborBlock.GetConnection((BlockDirection)(dirIndex ^ 1));
			if (con1 >= 1 && con2 == 1) {
				return true;
			} else if (con1 >= 2 && con2 >= 2 &&
				(this.shape.connectionType == neighborBlock.shape.connectionType))
			{
				int idx1 = this.ToWorldDirection(con1 - 2);
				int idx2 = neighborBlock.ToWorldDirection(con2 - 2);
				if (idx1 == idx2) {
					return true;
				}
			}
		}
		return false;
	}

	// 表示用メッシュを出力
	public void WriteToMesh(BlockGroup group, BlockMeshMerger meshMerger) {
		BlockShape shape = this.shape;
		for (int i = 0; i < 6; i++) {
			int index = this.ToLocalDirection(i);
			int offset = meshMerger.vertexPos.Count;
			
			var mesh = shape.meshes[index];
			if (mesh == null) {
				continue;
			}

			// 隣のブロックに完全に覆われていたら省略する
			if (this.CheckOcculusion(group, (BlockDirection)i)) {
				continue;
			}

			var chip = TexturePalette.Instance.GetChip(this.textureChips[index]);
			
			Vector3[] vertexPos = mesh.vertices;
			Vector2[] vertexUv = mesh.uv;
			for (int j = 0; j < vertexPos.Length; j++) {
				Vector3 localPosition = vertexPos[j];
				meshMerger.vertexPos.Add(this.position + EditUtil.RotatePosition(localPosition, this.direction));
				meshMerger.vertexUv.Add(chip.ApplyUV(vertexUv[j], (BlockDirection)i, this.position.y));
			}
			int[] indices = mesh.GetIndices(0);
			for (int j = 0; j < indices.Length; j++) {
				meshMerger.triangles.Add(offset + indices[j]);
			}
		}
	}

	// ガイド用メッシュを出力
	public void WriteToGuideMesh(BlockGroup group, BlockMeshMerger mesh) {
		bool vertexHasWrote = false;

		for (int i = 0; i < 6; i++) {
			// 隣のブロックに完全に覆われていたら省略する
			if (this.CheckOcculusion(group, (BlockDirection)i)) {
				continue;
			}
			
			// 頂点が書き出されていなければここで書き出す
			if (!vertexHasWrote) {
				for (int j = 0; j < EditUtil.cubeVertices.Length; j++) {
					mesh.vertexPos.Add(this.position + EditUtil.cubeVertices[j]);
				}
				vertexHasWrote = true;
			}

			int offset = mesh.vertexPos.Count - EditUtil.cubeVertices.Length;
			mesh.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 0]);
			mesh.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 1]);
			mesh.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 2]);
			mesh.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 0]);
			mesh.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 2]);
			mesh.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 3]);
		}
	}

	// ルート用メッシュを出力
	public void WriteToRouteMesh(BlockGroup group, BlockMeshMerger mesh) {
		if (!this.IsMovable(group)) {
			return;
		}

		int offset = mesh.vertexPos.Count;
		for (int j = 0; j < 4; j++) {
			int index = EditUtil.ReversePanelVertexIndex(j, this.direction);
			Vector3 vertex = EditUtil.panelVertices[index];
			vertex = EditUtil.RotatePosition(vertex, this.direction);
			vertex.y = this.shape.routePanel[index] * 0.5f - 0.25f;
			mesh.vertexPos.Add(this.position + vertex);
		}

		if (this.direction == BlockDirection.Xplus || 
			this.direction == BlockDirection.Xminus
		){
			mesh.triangles.Add(offset + 0);
			mesh.triangles.Add(offset + 1);
			mesh.triangles.Add(offset + 3);
			mesh.triangles.Add(offset + 0);
			mesh.triangles.Add(offset + 3);
			mesh.triangles.Add(offset + 2);
		} else {
			mesh.triangles.Add(offset + 0);
			mesh.triangles.Add(offset + 1);
			mesh.triangles.Add(offset + 2);
			mesh.triangles.Add(offset + 1);
			mesh.triangles.Add(offset + 3);
			mesh.triangles.Add(offset + 2);
		}
	}
	
	// 上に4ブロック分のスペースがあるか
	public bool IsMovable(BlockGroup group) {
		for (int i = 1; i <= 4; i++) {
			if (group.GetBlock(this.position + new Vector3(0, 0.5f * i, 0)) != null) {
				return false;
			}
		}
		return true;
	}

	// 書き込み
	public void Serialize(XmlElement node) {
		node.SetAttribute("type", this.shape.name);
		node.SetAttribute("x", this.position.x.ToString());
		node.SetAttribute("y", this.position.y.ToString());
		node.SetAttribute("z", this.position.z.ToString());
		node.SetAttribute("dir", ((int)this.direction).ToString());
		string[] tileStrArray = Array.ConvertAll<int, string>(this.textureChips, (value) => {return value.ToString();});
		node.SetAttribute("tile", string.Join(",", tileStrArray));
	}

	// 読み込み
	public void Deserialize(XmlElement node) {
		if (node.HasAttribute("type")) {
			string typeName = node.GetAttribute("type");
			this.shape = BlockShape.Find(typeName);
		}
		if (this.shape == null) {
			this.shape = BlockShape.Find("cube");
		}

		Vector3 position = Vector3.zero;
		if (node.HasAttribute("x")) {
			float.TryParse(node.GetAttribute("x"), out position.x);
		}
		if (node.HasAttribute("y")) {
			float.TryParse(node.GetAttribute("y"), out position.y);
		}
		if (node.HasAttribute("z")) {
			float.TryParse(node.GetAttribute("z"), out position.z);
		}
		this.SetPosition(position);

		if (node.HasAttribute("dir")) {
			int dir;
			int.TryParse(node.GetAttribute("dir"), out dir);
			this.direction = (BlockDirection)dir;
		}
		
		if (node.HasAttribute("tile")) {
			string tileStr = node.GetAttribute("tile");
			string[] tileStrArray = tileStr.Split(',');
			this.textureChips = Array.ConvertAll<string, int>(tileStrArray, (value) => {return int.Parse(value);});
		}
	}
};
