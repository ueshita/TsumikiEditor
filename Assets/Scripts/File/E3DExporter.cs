using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

public static class FileUtil
{
	public static void Write(this BinaryWriter writer, Vector2 vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
	}
	public static void Write(this BinaryWriter writer, Vector2i vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
	}
	public static void Write(this BinaryWriter writer, Vector2h vector) {
		writer.Write(vector.x.value);
		writer.Write(vector.y.value);
	}
	public static void Write(this BinaryWriter writer, Vector3 vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}
	public static void Write(this BinaryWriter writer, Vector3i vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}
	public static void Write(this BinaryWriter writer, Vector3h vector) {
		writer.Write(vector.x.value);
		writer.Write(vector.y.value);
		writer.Write(vector.z.value);
		writer.Write((ushort)0);	// padding
	}
	public static void Write(this BinaryWriter writer, Color color) {
		writer.Write(color.r);
		writer.Write(color.g);
		writer.Write(color.b);
		writer.Write(color.a);
	}
	public static Vector3i ApplyRightHanded(Vector3i vector) {
		vector.z = -vector.z;
		return vector;
	}
	public static Vector3h ApplyRightHanded(Vector3h vector) {
		vector.z = -vector.z;
		return vector;
	}
	public static Vector3 ApplyRightHanded(Vector3 vector) {
		vector.z = -vector.z;
		return vector;
	}
}

// オレオレフォーマット向けのエクスポータ
public static class E3DExporter
{
	struct StaticMeshVertex
	{
		public Vector3 position;
		//public Vector3 normal;
		//public Vector2 texCoord;
		public Vector3h normal;
		public Vector2h texCoord;

		public static int GetSize() {
			return Marshal.SizeOf(typeof(StaticMeshVertex));
		}
		public static int GetAttribCount() {
			return 3;
		}
		public void Write(BinaryWriter writer) {
			writer.Write(this.position);
			writer.Write(this.normal);
			writer.Write(this.texCoord);
		}
	}
	
	struct ShaderAttrib
	{
		public byte type;
		public byte count;
		public byte offset;
		public byte normalized;

		public ShaderAttrib(byte type, byte count, byte offset, byte normalized) {
			this.type = type;
			this.count = count;
			this.offset = offset;
			this.normalized = normalized;
		}
		
		public void Write(BinaryWriter writer) {
			writer.Write(this.type);
			writer.Write(this.count);
			writer.Write(this.offset);
			writer.Write(this.normalized);
		}
	}

	class FieldPanel {
		public int id;
		public Vector3 originalPosition;
		public Vector3i position;
		public Vector3[] vertices;
		public bool reversed;
		public List<FieldPanel> paths;

		public bool CanMoveTo(FieldPanel target, BlockGroup blockGroup) {
			bool movable = true;
			if (target.position.y == this.position.y) {
				// 同じ高さのパネル
			} else if (target.position.y > this.position.y) {
				// 上の方のパネル
				float dy = target.originalPosition.y;
				float sy = this.originalPosition.y;
				for (float j = sy + 0.5f; j <= dy + 1.5f; j += 0.5f) {
					Block block = blockGroup.GetBlock(
						new Vector3(this.originalPosition.x, j, this.originalPosition.z));
					if (block != null) {
						movable = false;
						break;
					}
				}
			} else if (target.position.y < this.position.y) {
				// 下の方のパネル
				float dy = target.originalPosition.y;
				float sy = this.originalPosition.y;
				for (float j = sy + 1.5f; j >= dy + 0.5f; j -= 0.5f) {
					Block block = blockGroup.GetBlock(
						new Vector3(target.originalPosition.x, j, target.originalPosition.z));
					if (block != null) {
						movable = false;
						break;
					}
				}
			}
			return movable;
		}
	}

	class FieldPanelGroup {
		public List<FieldPanel> panels = new List<FieldPanel>();

		public FieldPanelGroup(Mesh mesh, Block[] blocks, Vector3 offset) {
			for (int i = 0; i < blocks.Length; i++) {
				
				if (blocks[i].position.x < 0 || 
					blocks[i].position.y < 0 || 
					blocks[i].position.z > 0) {
					// マイナス位置にあるパネルは除外
					continue;
				}

				FieldPanel fieldPanel = new FieldPanel();
				fieldPanel.originalPosition = blocks[i].position;
				fieldPanel.position.x = Mathf.RoundToInt(blocks[i].position.x);
				fieldPanel.position.y = Mathf.RoundToInt(blocks[i].position.y * 2.0f) + 1;
				fieldPanel.position.z = Mathf.RoundToInt(blocks[i].position.z);
			
				fieldPanel.vertices = new Vector3[4];
				for (int j = 0; j < 4; j++) {
					fieldPanel.vertices[j] = mesh.vertices[i * 4 + j] + offset;
				}
				fieldPanel.reversed = (blocks[i].direction == BlockDirection.Zplus) || 
									  (blocks[i].direction == BlockDirection.Zminus);

				this.panels.Add(fieldPanel);
			}
			
			// 上から順にソート
			this.panels.Sort(delegate(FieldPanel a, FieldPanel b) {
				if (a.position.y * 10000 + a.position.z * 100 + a.position.x < 
					b.position.y * 10000 + b.position.z * 100 + b.position.x) {
					return 1;
				} else {
					return -1;
				};
			});
			
			// IDを割り当て
			for (int i = 0; i < this.panels.Count; i++) {
				this.panels[i].id = i;
			}
		}

		public void ApplyRightHanded() {
			foreach (var panel in this.panels) {
				panel.position = FileUtil.ApplyRightHanded(panel.position);
				for (int j = 0; j < 4; j++) {
					panel.vertices[j] = FileUtil.ApplyRightHanded(panel.vertices[j]);
				}
			}
		}

		public void ApplyRoutePath(BlockGroup blockGroup, RoutePath routePath) {
			Vector3i[] offsets = {
				new Vector3i( 0, 0,  1),
				new Vector3i( 0, 0, -1),
				new Vector3i( 1, 0,  0),
				new Vector3i(-1, 0,  0),
			};

			foreach (var panel in this.panels) {
				panel.paths = new List<FieldPanel>();

				for (int i = 0; i < 4; i++) {
					Vector3i key = panel.position + offsets[i];
					// 隣接のセルを探す
					var neighbourPanels = this.panels.FindAll(delegate(FieldPanel a) {
						return a.position.x == key.x && a.position.z == key.z;
					});
					foreach (var target in neighbourPanels) {
						if (panel.CanMoveTo(target, blockGroup)) {
							panel.paths.Add(target);
						}
					}
				}
				var paths = routePath.FindPaths(panel.originalPosition);
				foreach (var path in paths) {
					foreach (var target in this.panels) {
						if (target.originalPosition == path) {
							panel.paths.Add(target);
						}
					}
				}
			}
		}

		public void Write(BinaryWriter writer) {
			
			writer.Write(this.panels.Count);
			
			foreach (var panel in this.panels) {
				// パネルの位置
				writer.Write(panel.position);
				
				// パネルの頂点
				writer.Write(panel.vertices[0]);
				writer.Write(panel.vertices[1]);
				writer.Write(panel.vertices[2]);
				writer.Write(panel.vertices[3]);
				// パネルを三角形に分割した際の反転情報
				writer.Write(panel.reversed ? 1 : 0);
				
				// ルートパス
				writer.Write(panel.paths.Count);
				foreach (var target in panel.paths) {
					writer.Write(target.id);
				}
			}
		}
	}

	public static void Export(string path) {
		bool toRightHanded = EditManager.Instance.IsRightHanded();
		Vector3 offset = new Vector3(0.5f, 0.25f, -0.5f);
		
		WriteModelFile(path, offset, toRightHanded);
		
		string metaFilePath = Path.ChangeExtension(path, ".dat");
		WriteMetaFile(metaFilePath, offset, toRightHanded);
	}

	private static void WriteModelFile(string path, Vector3 offset, bool toRightHanded) {
		Vector3 center = Vector3.zero;
		Vector3 size = Vector3.zero;
		
		var vertexData = new List<StaticMeshVertex>();
		var indexData = new List<int>();

		int numLayers = EditManager.Instance.Layers.Count;
		int[] batchIndexOffset = new int[numLayers];
		int[] batchIndexCount = new int[numLayers];

		for (int layerId = 0; layerId < numLayers; layerId++) {
			var blockGroup = EditManager.Instance.Layers[layerId].GetBlockGroup();
			Mesh mesh = blockGroup.GetSurfaceMesh();
			
			if (layerId == 0) {
				center = mesh.bounds.center + offset;
				size = mesh.bounds.size;
			}
			
			int[] indices = mesh.GetIndices(0);
			for (int i = 0; i < indices.Length; i++) {
				indices[i] += vertexData.Count;
			}
			
			batchIndexOffset[layerId] = indexData.Count;
			batchIndexCount[layerId] = indices.Length;
			
			indexData.AddRange(indices);

			var vertices = new StaticMeshVertex[mesh.vertexCount];
			Vector3[] positions = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector2[] texCoords = mesh.uv;
			for (int i = 0; i < vertices.Length; i++) {
				vertices[i].position = positions[i] + offset;
				vertices[i].normal = normals[i];
				vertices[i].texCoord = texCoords[i];
			}
			if (toRightHanded) {
				for (int i = 0; i < vertices.Length; i++) {
					vertices[i].position = FileUtil.ApplyRightHanded(vertices[i].position);
					vertices[i].normal = FileUtil.ApplyRightHanded(vertices[i].normal);
				}
			}
			vertexData.AddRange(vertices);
		}

		if (vertexData.Count > ushort.MaxValue) {
			Debug.LogError("Vertex count is over than 65535.");
			return;
		}

		var mem = new MemoryStream();
		var writer = new BinaryWriter(mem);
		
		int textureCount = TexturePalette.Instance.GetNumTextures();
		int materialCount = numLayers;
		int batchCount = numLayers;
		
		writer.Write(Encoding.UTF8.GetBytes("E3D3"));
		writer.Write(StaticMeshVertex.GetSize());			// VertexSize
		writer.Write(StaticMeshVertex.GetAttribCount());	// AttribCount

		writer.Write(textureCount);						// TextureCount
		writer.Write(materialCount);					// MaterialCount
		writer.Write(batchCount);						// BatchCount
		writer.Write(0);								// BoneCount
		writer.Write(0);								// AnimClipCount

		writer.Write(vertexData.Count * StaticMeshVertex.GetSize());	// VertexDataSize
		writer.Write(indexData.Count * 2);								// IndexDataSize

		writer.Write(center);
		writer.Write(size);
		
		// AttribInfo
		var attribs = new ShaderAttrib[]{
			new ShaderAttrib(10, 3,  0, 0),		// "a_Position",
			//new ShaderAttrib(10, 3, 12, 0),	// "a_Normal",
			//new ShaderAttrib(10, 2, 24, 0)	// "a_TexCoord",
			new ShaderAttrib(9, 3, 12, 0),		// "a_Normal", (16bit)
			new ShaderAttrib(9, 2, 20, 0)		// "a_TexCoord",　(16bit)
		};
		for (int i = 0; i < attribs.Length; i++) {
			attribs[i].Write(writer);
		}

		// TextureInfo
		for (int i = 0; i < textureCount; i++) {
			Texture2D texture = TexturePalette.Instance.GetTexture(i);
			byte[] bytes = Encoding.UTF8.GetBytes(texture.name);
			writer.Write(bytes.Length);
			writer.Write(bytes);
		}

		// MaterialInfo
		for (int i = 0; i < numLayers; i++) {
			writer.Write(new Color(0.8f, 0.8f, 0.8f, 1.0f));	// diffuseColor
			writer.Write(new Color(0.4f, 0.4f, 0.4f, 1.0f));	// ambientColor
			writer.Write(new Color(0.0f, 0.0f, 0.0f, 1.0f));	// emissionColor
			writer.Write(new Color(0.0f, 0.0f, 0.0f, 1.0f));	// specularColor
			writer.Write(0.0f);									// shiniess

			if (i == 0) {
				// ブロックレイヤー
				for (int j = 0; j < 4; j++) {
					Texture2D texture = TexturePalette.Instance.GetTexture(j);
					writer.Write((texture != null) ? j : -1);   // TextureId
				}
			} else {
				// 水レイヤー
				for (int j = 0; j < 4; j++) {
					writer.Write(-1);                           // TextureId
				}
			}
		}

		// BatchInfo
		for (int i = 0; i < numLayers; i++) {
			writer.Write(batchIndexOffset[i]);	// IndexOffset
			writer.Write(batchIndexCount[i]);	// IndexCount
			writer.Write(i);					// MaterialId
		}

		// Output Vertices
		for (int i = 0; i < vertexData.Count; i++) {
			vertexData[i].Write(writer);
		}
		// Output Indeces
		for (int i = 0; i < indexData.Count; i++) {
			writer.Write((ushort)indexData[i]);
		}

		writer.Flush();
		mem.Flush();
		
		File.WriteAllBytes(path, mem.ToArray());
	}
	
	private static void WriteMetaFile(string path,
		Vector3 offset, bool toRightHanded)
	{
		BlockGroup blockGroup = EditManager.Instance.Layers[0].GetBlockGroup();
		ModelGroup modelGroup = EditManager.Instance.Layers[0].GetModelGroup();

		Mesh mesh = blockGroup.GetRouteMesh();
		Block[] blocks = blockGroup.GetEnterableBlocks();
		
		// モデルを見て移動可能かどうかをチェックする
		blocks = blocks.Where(block => {
			var model = modelGroup.GetModel(block.position);
			return model == null || model.shape.enterable;
		}).ToArray();

		var fieldPanels = new FieldPanelGroup(mesh, blocks, offset);
		fieldPanels.ApplyRoutePath(blockGroup, EditManager.Instance.RoutePath);
		
		// バウンディングボックス
		Vector3 minpos = mesh.bounds.min + offset;
		Vector3 maxpos = mesh.bounds.max + offset;
				
		// 座標系反転処理
		if (toRightHanded) {
			fieldPanels.ApplyRightHanded();
			minpos = FileUtil.ApplyRightHanded(minpos);
			maxpos = FileUtil.ApplyRightHanded(maxpos);
			float z = minpos.z;
			minpos.z = maxpos.z;
			maxpos.z = z;
		}

		var mem = new MemoryStream();
		var writer = new BinaryWriter(mem);

		writer.Write(Encoding.UTF8.GetBytes("E3MT").ToArray());	// Identifier
		writer.Write(3);				// Version

		// バウンディングボックス
		writer.Write(minpos);
		writer.Write(maxpos);
		
		// 移動パネル
		fieldPanels.Write(writer);

		// 当たり判定ブロック
		Block[] colliderBlocks = blockGroup.GetAllBlocks();
		writer.Write(colliderBlocks.Length);
		Vector3 colliderOffset = new Vector3(0.5f, 0.25f, 0.5f);
		foreach (var block in colliderBlocks) {
			Vector3 position = block.position;
			if (toRightHanded) {
				position = FileUtil.ApplyRightHanded(position);
			}
			position += colliderOffset;
			writer.Write(position);
		}
		
		// 3Dモデル配置
		Model[] models = modelGroup.GetAllModels();

		// 使われているモデルシェイプの名前を出力
		List<ModelShape> modelShapes = models.Select(model => model.shape).Distinct().ToList();
		writer.Write(modelShapes.Count);
		foreach (var shape in modelShapes) {
			writer.Write(Encoding.UTF8.GetBytes(shape.name));
			writer.Write((byte)0);
		}
		
		// 3Dモデル配置
		writer.Write(models.Length);
		foreach (var model in models) {
			Vector3 position = model.position;
			if (toRightHanded) {
				position = FileUtil.ApplyRightHanded(position);
			}
			writer.Write(modelShapes.IndexOf(model.shape));
			writer.Write(position);
			writer.Write(model.shape.offset + model.offset);
			writer.Write((float)model.rotation);
			writer.Write(model.shape.scale * model.scale);
		}

		File.WriteAllBytes(path, mem.ToArray());
	}
}
