using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

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
	}

	private static void Write(BinaryWriter writer, Vector3 vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}
	private static void Write(BinaryWriter writer, Vector3i vector) {
		writer.Write(vector.x);
		writer.Write(vector.y);
		writer.Write(vector.z);
	}
	private static void Write(BinaryWriter writer, Color color) {
		writer.Write(color.r);
		writer.Write(color.g);
		writer.Write(color.b);
		writer.Write(color.a);
	}
	private static void Write(BinaryWriter writer, StaticMeshVertex vertex) {
		writer.Write(vertex.position.x);
		writer.Write(vertex.position.y);
		writer.Write(vertex.position.z);
		writer.Write(vertex.normal.x);
		writer.Write(vertex.normal.y);
		writer.Write(vertex.normal.z);
		writer.Write(vertex.texCoord.x);
		writer.Write(vertex.texCoord.y);
	}
	private static void Write(BinaryWriter writer, ShaderAttrib attrib) {
		writer.Write(attrib.type);
		writer.Write(attrib.count);
		writer.Write(attrib.offset);
		writer.Write(attrib.normalized);
	}
	
	struct FieldPanel {
		public Vector3i position;
		public Vector3[] vertices;
		public bool reversed;
	}

	public static void Export(string path, BlockGroup blockGroup) {
		
		bool reversing = EditManager.Instance.IsRightHanded();
		Vector3 offset = new Vector3(0.5f, 0.25f, -0.5f);

		{
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
				if (reversing) {
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

			Write(writer, mesh.bounds.center);
			Write(writer, mesh.bounds.size);
		
			// AttribInfo
			var attribs = new ShaderAttrib[]{
				new ShaderAttrib(10, 3,  0, 0),	// "a_Position",    
				new ShaderAttrib(10, 3, 12, 0),	// "a_Normal",      
				new ShaderAttrib(10, 2, 24, 0)	// "a_TexCoord",    
			};
			for (int i = 0; i < attribs.Length; i++) {
				Write(writer, attribs[i]);
			}

			// MaterialInfo
			Write(writer, new Color(0.8f, 0.8f, 0.8f, 1.0f));	// diffuseColor
			Write(writer, new Color(0.4f, 0.4f, 0.4f, 1.0f));	// ambientColor
			Write(writer, new Color(0.0f, 0.0f, 0.0f, 1.0f));	// emissionColor
			Write(writer, new Color(0.0f, 0.0f, 0.0f, 1.0f));	// specularColor
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
				Write(writer, vertices[i]);
			}
			// Output Indeces
			for (int i = 0; i < indices.Length; i++) {
				writer.Write((ushort)indices[i]);
			}

			writer.Flush();
			mem.Flush();
		
			File.WriteAllBytes(path, mem.ToArray());
		}

		{
			Mesh mesh = blockGroup.GetRouteMesh();
			Block[] blocks = blockGroup.GetMovableBlocks();
			var fieldPanels = new FieldPanel[blocks.Length];
			
			for (int i = 0; i < blocks.Length; i++) {
				fieldPanels[i].position.x = Mathf.RoundToInt(blocks[i].position.x);
				fieldPanels[i].position.y = Mathf.RoundToInt(blocks[i].position.y * 2.0f) + 1;
				fieldPanels[i].position.z = Mathf.RoundToInt(blocks[i].position.z);
				
				fieldPanels[i].vertices = new Vector3[4];
				for (int j = 0; j < 4; j++) {
					fieldPanels[i].vertices[j] = mesh.vertices[i * 4 + j] + offset;
				}
				fieldPanels[i].reversed = (blocks[i].direction == BlockDirection.Xplus) || 
										  (blocks[i].direction == BlockDirection.Xminus);
			}

			Array.Sort(fieldPanels, delegate(FieldPanel a, FieldPanel b) {
				if (a.position.y * 10000 + a.position.z * 100 + a.position.x < 
					b.position.y * 10000 + b.position.z * 100 + b.position.x) {
					return 1;
				} else {
					return -1;
				};
			});

			var mem = new MemoryStream();
			var writer = new BinaryWriter(mem);
			
			Vector3[] positions = mesh.vertices;
			
			Vector3 minpos = mesh.bounds.min + offset;
			Vector3 maxpos = mesh.bounds.max + offset;
			
			if (reversing) {
				for (int i = 0; i < fieldPanels.Length; i++) {
					fieldPanels[i].position.z = -fieldPanels[i].position.z;
					for (int j = 0; j < 4; j++) {
						fieldPanels[i].vertices[j].z = -fieldPanels[i].vertices[j].z;
					}
				}
				float z = -minpos.z;
				minpos.z = -maxpos.z;
				maxpos.z = z;
			}

			Write(writer, minpos);
			Write(writer, maxpos);
			writer.Write(fieldPanels.Length);

			for (int i = 0; i < fieldPanels.Length; i++) {
				Write(writer, fieldPanels[i].position);
				Write(writer, fieldPanels[i].vertices[0]);
				Write(writer, fieldPanels[i].vertices[1]);
				Write(writer, fieldPanels[i].vertices[2]);
				Write(writer, fieldPanels[i].vertices[3]);
				writer.Write(fieldPanels[i].reversed ? 1 : 0);
			}

			string datPath = Path.ChangeExtension(path, ".dat");
			File.WriteAllBytes(datPath, mem.ToArray());
		}
	}
}
