using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public static class FileUtil
{
	public static void Write(BinaryWriter writer, Vector3 vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}
	public static void Write(BinaryWriter writer, Vector3i vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}
	public static void Write(BinaryWriter writer, Color color) {
		writer.Write(color.r);
		writer.Write(color.g);
		writer.Write(color.b);
		writer.Write(color.a);
	}
}

// オレオレフォーマット向けのエクスポータ
public static class E3DExporter
{
	struct StaticMeshVertex
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector2 texCoord;

		public static int GetSize() {
			return Marshal.SizeOf(typeof(StaticMeshVertex));
		}
		public static int GetAttribCount() {
			return 3;
		}
		public void Write(BinaryWriter writer) {
			writer.Write(this.position.x);
			writer.Write(this.position.y);
			writer.Write(this.position.z);
			writer.Write(this.normal.x);
			writer.Write(this.normal.y);
			writer.Write(this.normal.z);
			writer.Write(this.texCoord.x);
			writer.Write(this.texCoord.y);
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
		public Vector3i position;
		public Vector3[] vertices;
		public bool reversed;
		public List<FieldPanel> routePath;
	}

	class FieldPanelGroup {
		public List<FieldPanel> panels = new List<FieldPanel>();

		public FieldPanelGroup(Mesh mesh, Block[] blocks, Vector3 offset) {
			for (int i = 0; i < blocks.Length; i++) {
				FieldPanel fieldPanel = new FieldPanel();
				fieldPanel.position.x = Mathf.RoundToInt(blocks[i].position.x);
				fieldPanel.position.y = Mathf.RoundToInt(blocks[i].position.y * 2.0f) + 1;
				fieldPanel.position.z = Mathf.RoundToInt(blocks[i].position.z);
			
				fieldPanel.vertices = new Vector3[4];
				for (int j = 0; j < 4; j++) {
					fieldPanel.vertices[j] = mesh.vertices[i * 4 + j] + offset;
				}
				fieldPanel.reversed = (blocks[i].direction == BlockDirection.Xplus) || 
									  (blocks[i].direction == BlockDirection.Xminus);

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
			for (int i = 0; i < blocks.Length; i++) {
				this.panels[i].id = i;
			}
		}

		public void ApplyRightHanded() {
			foreach (var panel in this.panels) {
				panel.position.z = -panel.position.z;
				for (int j = 0; j < 4; j++) {
					panel.vertices[j].z = -panel.vertices[j].z;
				}
			}
		}

		public void ApplyRoutePath(BlockGroup blockGroup) {
			Vector3i[] offsets = {
				new Vector3i( 0, 0,  1),
				new Vector3i( 0, 0, -1),
				new Vector3i( 1, 0,  0),
				new Vector3i(-1, 0,  0),
			};

			foreach (var panel in this.panels) {
				panel.routePath = new List<FieldPanel>();

				for (int i = 0; i < 4; i++) {
					Vector3i key = panel.position + offsets[i];
					var neighbourPanels = this.panels.FindAll(delegate(FieldPanel a) {
						return a.position.x == key.x && a.position.z == key.z;
					});

					foreach (var target in neighbourPanels) {
						bool movable = true;
						if (target.position.y == panel.position.y) {
							// 同じ高さのパネル
						} else if (target.position.y > panel.position.y) {
							// 上の方のパネル
							float dy = (target.position.y - 1) * 0.5f;
							float sy = ( panel.position.y - 1) * 0.5f;
							for (float j = sy + 0.5f; j <= dy + 1.5f; j += 0.5f) {
								Block block = blockGroup.GetBlock(
									new Vector3(panel.position.x, j, panel.position.z));
								if (block != null) {
									movable = false;
									break;
								}
							}
						} else if (target.position.y < panel.position.y) {
							// 下の方のパネル
							float dy = (target.position.y - 1) * 0.5f;
							float sy = ( panel.position.y - 1) * 0.5f;
							for (float j = sy + 1.5f; j >= dy + 0.5f; j -= 0.5f) {
								Block block = blockGroup.GetBlock(
									new Vector3(target.position.x, j, target.position.z));
								if (block != null) {
									movable = false;
									break;
								}
							}
						}
						
						if (movable) {
							panel.routePath.Add(target);
						}
					}
				}
			}
		}

		public void Write(BinaryWriter writer) {
			
			writer.Write(this.panels.Count);
			
			foreach (var panel in this.panels) {
				FileUtil.Write(writer, panel.position);
				FileUtil.Write(writer, panel.vertices[0]);
				FileUtil.Write(writer, panel.vertices[1]);
				FileUtil.Write(writer, panel.vertices[2]);
				FileUtil.Write(writer, panel.vertices[3]);
				writer.Write(panel.reversed ? 1 : 0);
				
				writer.Write(panel.routePath.Count);
				foreach (var target in panel.routePath) {
					writer.Write(target.id);
				}
			}
		}
	}

	public static void Export(string path, BlockGroup blockGroup) {
		
		bool toRightHanded = EditManager.Instance.IsRightHanded();
		Vector3 offset = new Vector3(0.5f, 0.25f, -0.5f);
		
		WriteModelFile(path, blockGroup, offset, toRightHanded);
		
		string metaFilePath = Path.ChangeExtension(path, ".dat");
		WriteMetaFile(metaFilePath, blockGroup, offset, toRightHanded);
	}

	private static void WriteModelFile(string path, BlockGroup blockGroup, Vector3 offset, bool toRightHanded) {
		Mesh mesh = blockGroup.GetSurfaceMesh();
		int[] indices = mesh.GetIndices(0);
		if (indices.Length > ushort.MaxValue) {
			Debug.LogError("Indices count is over than 65535.");
			return;
		}

		var vertices = new StaticMeshVertex[mesh.vertexCount];
		{
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
					vertices[i].position.z = -vertices[i].position.z;
					vertices[i].normal.z = -vertices[i].normal.z;
				}
			}
		}

		var mem = new MemoryStream();
		var writer = new BinaryWriter(mem);
		
		int vertexCount = vertices.Length;
		int indexCount = indices.Length;
		int textureCount = 0;
		int materialCount = 1;
		int batchCount = 1;
		
		writer.Write(Encoding.UTF8.GetBytes("E3D3"));
		writer.Write(StaticMeshVertex.GetSize());			// VertexSize
		writer.Write(StaticMeshVertex.GetAttribCount());	// AttribCount

		writer.Write(textureCount);						// TextureCount
		writer.Write(materialCount);					// MaterialCount
		writer.Write(batchCount);						// BatchCount
		writer.Write(0);								// BoneCount
		writer.Write(0);								// AnimClipCount

		writer.Write(vertexCount * StaticMeshVertex.GetSize());		// VertexDataSize
		writer.Write(indexCount * 2);								// IndexDataSize

		FileUtil.Write(writer, mesh.bounds.center);
		FileUtil.Write(writer, mesh.bounds.size);
		
		// AttribInfo
		var attribs = new ShaderAttrib[]{
			new ShaderAttrib(10, 3,  0, 0),	// "a_Position",    
			new ShaderAttrib(10, 3, 12, 0),	// "a_Normal",      
			new ShaderAttrib(10, 2, 24, 0)	// "a_TexCoord",    
		};
		for (int i = 0; i < attribs.Length; i++) {
			attribs[i].Write(writer);
		}

		// MaterialInfo
		FileUtil.Write(writer, new Color(0.8f, 0.8f, 0.8f, 1.0f));	// diffuseColor
		FileUtil.Write(writer, new Color(0.4f, 0.4f, 0.4f, 1.0f));	// ambientColor
		FileUtil.Write(writer, new Color(0.0f, 0.0f, 0.0f, 1.0f));	// emissionColor
		FileUtil.Write(writer, new Color(0.0f, 0.0f, 0.0f, 1.0f));	// specularColor
		writer.Write(0.0f);									// shiniess
		writer.Write(-1);									// TextureId0
		writer.Write(-1);									// TextureId1
		writer.Write(-1);									// TextureId2
		writer.Write(-1);									// TextureId3

		// BatchInfo
		writer.Write(0);				// IndexOffset
		writer.Write(indices.Length);	// IndexCount
		writer.Write(0);				// MaterialId

		// Output Vertices
		for (int i = 0; i < vertices.Length; i++) {
			vertices[i].Write(writer);
		}
		// Output Indeces
		for (int i = 0; i < indices.Length; i++) {
			writer.Write((ushort)indices[i]);
		}

		writer.Flush();
		mem.Flush();
		
		File.WriteAllBytes(path, mem.ToArray());
	}
	
	private static void WriteMetaFile(string path, BlockGroup blockGroup, Vector3 offset, bool toRightHanded) {
		Mesh mesh = blockGroup.GetRouteMesh();
		Block[] blocks = blockGroup.GetMovableBlocks();
		var fieldPanels = new FieldPanelGroup(mesh, blocks, offset);
		fieldPanels.ApplyRoutePath(blockGroup);

		var mem = new MemoryStream();
		var writer = new BinaryWriter(mem);
		
		Vector3[] positions = mesh.vertices;
		Vector3 minpos = mesh.bounds.min + offset;
		Vector3 maxpos = mesh.bounds.max + offset;
		
		// 座標系反転処理
		if (toRightHanded) {
			fieldPanels.ApplyRightHanded();
			float z = -minpos.z;
			minpos.z = -maxpos.z;
			maxpos.z = z;
		}

		FileUtil.Write(writer, minpos);
		FileUtil.Write(writer, maxpos);
		fieldPanels.Write(writer);

		File.WriteAllBytes(path, mem.ToArray());
	}
}
