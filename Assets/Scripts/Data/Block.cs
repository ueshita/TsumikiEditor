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
	
	public Vector3 position {get; private set;}
	public BlockDirection direction {get; private set;}
	public BlockShape shape {get; private set;}
	public string metaInfo {get; private set;}
	private int[] textureChips = null;
	private int hashCode;

	public override int GetHashCode() {
		return this.hashCode;
	}
	
	public Block(Vector3 position, BlockDirection direction, BlockShape shape) {
		this.shape = shape;
		this.textureChips = new int[shape.meshes.Length];
		this.direction = direction;
		this.SetPosition(position);
	}

	public Block(XmlElement node) {
		this.Deserialize(node);
	}

	public void SetPosition(Vector3 position) {
		this.position = position;
		this.hashCode = EditUtil.PositionToHashCode(position);
	}

	public void SetDirection(BlockDirection direction) {
		this.direction = direction;
	}

	public int GetTextureChip(BlockDirection direction, bool isObject = false) {
		return this.textureChips[(isObject) ? 6 : ToLocalDirection((int)direction)];
	}

	public void SetTextureChip(BlockDirection direction, bool isObject, int textureChip) {
		if (isObject) {
			for (int i = 6; i < this.textureChips.Length; i++) {
				this.textureChips[i] = textureChip;
			}
		} else {
			this.textureChips[this.ToLocalDirection((int)direction)] = textureChip;
		}
	}

	public void SetMetaInfo(string metaInfo) {
		this.metaInfo = metaInfo;
	}
	
	private static readonly int[,] ToWorldDirectionTable = new int[4, 4] {
		{0, 1, 2, 3}, {1, 0, 3, 2}, 
		{3, 2, 0, 1}, {2, 3, 1, 0}};
	private int ToWorldDirection(int index) {
		return (index < 4) ? ToWorldDirectionTable[(int)this.direction, index] : index;
	}

	private static readonly int[,] ToLocalDirectionTable = new int[4, 4] {
		{0, 1, 2, 3}, {1, 0, 3, 2}, 
		{2, 3, 1, 0}, {3, 2, 0, 1}};
	private int ToLocalDirection(int index) {
		return (index < 4) ? ToLocalDirectionTable[(int)this.direction, index] : index;
	}

	public BlockConnection GetConnection(BlockDirection direction) {
		return this.shape.connection[ToLocalDirection((int)direction)];
	}
	public BlockDirection GetConnectionDir(BlockDirection direction) {
		return (BlockDirection)ToWorldDirection((int)this.shape.connectionDir[ToLocalDirection((int)direction)]);
	}

	// 隣のブロックとの閉塞チェック
	private bool IsOcculuded(BlockGroup group, BlockDirection direction) {
		int dirIndex = (int)direction;
		// 隣のブロックに完全に覆われていたら省略する（閉塞チェック）
		Block neighborBlock = group.GetBlock(this.position + EditManager.Instance.ToWorldCoordinate(neighborOffsets[dirIndex]));
		if (neighborBlock != null) {
			var con1 = this.GetConnection((BlockDirection)(dirIndex));
			var con2 = neighborBlock.GetConnection((BlockDirection)(dirIndex ^ 1));
			if (con2 == BlockConnection.None) {
				return false;
			} else if (con2 == BlockConnection.Square) {
				return true;
			} else if (con1 == con2) {
				var dir1 = this.GetConnectionDir((BlockDirection)(dirIndex));
				var dir2 = neighborBlock.GetConnectionDir((BlockDirection)(dirIndex ^ 1));
				if (dir1 == dir2) {
					return true;
				}
			}
		}

		return false;
	}

	private bool IsCombinable(BlockDirection direction) {
		var con = this.GetConnection(direction);
		return (con == BlockConnection.Square || con == BlockConnection.Wall);
	}

	// 表示用メッシュを出力
	public void WriteToMesh(BlockGroup blockGroup, BlockMeshMerger meshMerger) {
		BlockShape shape = this.shape;
		if (shape.autoPlacement) {
			// 自動配置ブロックの処理
			WriteToMeshAutoPlacement(blockGroup, meshMerger);
		} else {
			// 6方向ブロックメッシュ
			WriteToMeshStandard(blockGroup, meshMerger);
		}
	}
	
	// 6方向ブロックメッシュ
	public void WriteToMeshStandard(BlockGroup blockGroup, BlockMeshMerger meshMerger) {
		BlockShape shape = this.shape;
		for (int i = 0; i < shape.meshes.Length; i++) {
			int index = this.ToLocalDirection(i);

			var mesh = shape.meshes[index];
			if (mesh == null) {
				continue;
			}

			// 隣のブロックに完全に覆われていたら省略する
			if (i < 6 && this.IsOcculuded(blockGroup, (BlockDirection)i)) {
				continue;
			}
			
			// 一番下の底面は省略する
			if ((BlockDirection)i == BlockDirection.Yminus && this.position.y == 0.0f) {
				continue;
			}

			Vector3 position = this.position;
			Vector3 scale = Vector3.one;
			// 側面パネルの場合は分割する
			bool divideChipVert = shape.divideChipVert && i < 6 && 
				(BlockDirection)i != BlockDirection.Yplus &&
				(BlockDirection)i != BlockDirection.Yminus;
			if (divideChipVert && this.IsCombinable((BlockDirection)i)) {
				BlockDirection direction = (BlockDirection)i;
				// 上下に2個続いているブロックをまとめる
				if (this.position.y - Math.Floor(this.position.y) >= 0.5f) {
					Block neighborBlock = blockGroup.GetBlock(this.position - new Vector3(0, 0.5f, 0));
					if (neighborBlock != null && neighborBlock.IsCombinable(direction) && 
						this.GetTextureChip(direction) == neighborBlock.GetTextureChip(direction)) {
						divideChipVert = false;
						position.y -= 0.25f;
						scale.y = 2.0f;
					}
				} else {
					Block neighborBlock = blockGroup.GetBlock(this.position + new Vector3(0, 0.5f, 0));
					if (neighborBlock != null && neighborBlock.IsCombinable(direction) &&
						this.GetTextureChip(direction) == neighborBlock.GetTextureChip(direction)) {
						if (!neighborBlock.IsOcculuded(blockGroup, direction)) {
							continue;
						}
					}
				}
			}

			meshMerger.Merge(mesh, position, this.direction, scale,
				divideChipVert, this.textureChips[index], i);
		}
	}
	
	// 自動配置ブロックの処理
	public void WriteToMeshAutoPlacement(BlockGroup blockGroup, BlockMeshMerger meshMerger) {
		BlockShape shape = this.shape;
		// 隣接ブロックを取得
		var list = new Vector3[] {
			new Vector3( 0, 0, 1), new Vector3( 1, 0, 0), new Vector3( 0, 0,-1), new Vector3(-1, 0, 0),
			new Vector3( 1, 0, 1), new Vector3( 1, 0,-1), new Vector3(-1, 0,-1), new Vector3(-1, 0, 1),
			new Vector3( 0, 0.5f, 0), 
			new Vector3( 0, 0.5f, 1), new Vector3( 1, 0.5f, 0), new Vector3( 0, 0.5f,-1), new Vector3(-1, 0.5f, 0),
		};

		int pattern = 0;
		for (int i = 0; i < list.Length; i++) {
			var block = blockGroup.GetBlock(position + EditManager.Instance.ToWorldCoordinate(list[i]));
			if (block != null) {pattern |= (1 << i);}
		}

		for (int i = 0; i < 4; i++) {
			bool s1 = (pattern & (1 << (i))) != 0;				// 隣接1
			bool s2 = (pattern & (1 << ((i + 1) % 4))) != 0;	// 隣接2
			bool s3 = (pattern & (1 << (i + 4))) != 0;			// 斜め隣接
			bool s4 = (pattern & (1 << 8)) != 0;				// 上隣接
			bool s5 = (pattern & (1 << (9 + i))) != 0;			// 上横隣接1
			bool s6 = (pattern & (1 << (9 + (i + 1) % 4))) != 0;// 上横隣接2

			int meshOffset;
			if (s3) {
				if (s1 && s2) meshOffset = (s4 && (s5 || s6)) ? -1 : 0;
				else if (s1)  meshOffset = (s4) ? (s5) ? 24 : 32 : 8;
				else if (s2)  meshOffset = (s4) ? (s6) ? 28 : 36 : 12;
				else          meshOffset = (s4) ? 20 : 4;
			} else {
				if (s1 && s2) meshOffset = 16;
				else if (s1)  meshOffset = (s4) ? (s5) ? 24 : 32 : 8;
				else if (s2)  meshOffset = (s4) ? (s6) ? 28 : 36 : 12;
				else          meshOffset = (s4) ? 20 : 4;
			}
			if (meshOffset < 0) {
				continue;
			}

			var mesh = shape.meshes[6 + meshOffset + i];
			if (mesh == null) {
				continue;
			}

			meshMerger.Merge(mesh, this.position, this.direction, Vector3.one,
				shape.divideChipVert, this.textureChips[6], 0);
		}
	}

	// ガイド用メッシュを出力
	public void WriteToGuideMesh(BlockGroup blockGroup, BlockMeshMerger mesh) {
		BlockShape shape = this.shape;

		bool vertexHasWrote = false;
		for (int i = 0; i < 6; i++) {
			// 隣のブロックに完全に覆われていたら省略する
			bool occuludes = false;
			if (this.IsOcculuded(blockGroup, (BlockDirection)i)) {
				continue;
			}

			// 壁タイプのガイドは限定的にする
			if (shape.wall > 0) {
				int index = this.ToLocalDirection(i);
				if (shape.wall >= 1 && (BlockDirection)index == BlockDirection.Zplus) {
				} else if (shape.wall >= 2 && (BlockDirection)index == BlockDirection.Xplus) {
				} else {
					continue;
				}
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
	public void WriteToRouteMesh(BlockGroup blockGroup, BlockMeshMerger mesh) {
		if (!this.IsEnterable(blockGroup)) {
			return;
		}

		int offset = mesh.vertexPos.Count;
		for (int j = 0; j < 4; j++) {
			int index = EditUtil.ReversePanelVertexIndex(j, this.direction);
			Vector3 vertex = EditUtil.panelVertices[j];
			vertex.y = this.shape.panelVertices[index] * 0.5f - 0.25f;
			vertex = EditManager.Instance.ToWorldCoordinate(vertex);
			mesh.vertexPos.Add(this.position + vertex);
		}
		
		if (this.direction == BlockDirection.Xplus || 
			this.direction == BlockDirection.Xminus
		) {
			mesh.triangles.Add(offset + 0);
			mesh.triangles.Add(offset + 2);
			mesh.triangles.Add(offset + 1);
			mesh.triangles.Add(offset + 1);
			mesh.triangles.Add(offset + 2);
			mesh.triangles.Add(offset + 3);
		} else {
			mesh.triangles.Add(offset + 0);
			mesh.triangles.Add(offset + 3);
			mesh.triangles.Add(offset + 1);
			mesh.triangles.Add(offset + 0);
			mesh.triangles.Add(offset + 2);
			mesh.triangles.Add(offset + 3);
		}
	}

	public Mesh GetMesh(BlockDirection direction) {
		int index = this.ToLocalDirection((int)direction);
		return this.shape.meshes[index];
	}
	
	// 上に4ブロック分のスペースがあるか
	public bool IsEnterable(BlockGroup blockGroup) {
		if (this.shape.panelVertices == null) {
			return false;
		}
		for (int i = 1; i <= 4; i++) {
			var block = blockGroup.GetBlock(this.position + new Vector3(0, 0.5f * i, 0));
			if (block != null) {
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
		
		if (!String.IsNullOrEmpty(this.metaInfo)) {
			node.SetAttribute("meta", this.metaInfo);
		}
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
		if (node.HasAttribute("x")) {float.TryParse(node.GetAttribute("x"), out position.x);}
		if (node.HasAttribute("y")) {float.TryParse(node.GetAttribute("y"), out position.y);}
		if (node.HasAttribute("z")) {float.TryParse(node.GetAttribute("z"), out position.z);}
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

		if (node.HasAttribute("meta")) {
			this.metaInfo = node.GetAttribute("meta");
		}
	}
};
