using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class OBJLoader
{
	static public Mesh[] LoadMesh(string path) {
		if (!File.Exists(path)) {
			return null;
		}

		string[] lines = File.ReadAllLines(path);

		List<Vector3> positionList = new List<Vector3>();
		List<Vector2> texcoordList = new List<Vector2>();
		List<Vector3> normalList = new List<Vector3>();

		MeshBuilder builder = new MeshBuilder();
		List<Mesh> meshes = new List<Mesh>();
		
		foreach (string line in lines) {
			if (line.Length == 0) {
				continue;
			}
			if (line[0] == '#') {
				continue;
			}

			string[] tokens = line.Split(' ');
			if (tokens[0] == "v") {
				positionList.Add(ParseVector3(tokens));
			} else if (tokens[0] == "vt") {
				texcoordList.Add(ParseVector2(tokens));
			} else if (tokens[0] == "vn") {
				normalList.Add(ParseVector3(tokens));
			} else if (tokens[0] == "g") {
				if (builder.vertices.Count > 0) {
					meshes.Add(builder.Build());
				}
				builder = new MeshBuilder();
				builder.meshName = tokens[1];
			} else if (tokens[0] == "f") {
				for (int i = 1; i < tokens.Length; i++) {
					string[] index = tokens[i].Split('/');
					
					if (index.Length >= 1) {
						builder.vertices.Add(positionList[int.Parse(index[0]) - 1] * 0.01f);
					}
					if (index.Length >= 2) {
						builder.uv.Add(texcoordList[int.Parse(index[1]) - 1]);
					}
					if (index.Length >= 3) {
						builder.normals.Add(normalList[int.Parse(index[2]) - 1]);
					}
				}

				int vertPerFace = tokens.Length - 1;
				int offset = builder.vertices.Count - vertPerFace;
				if (vertPerFace == 3) {
					builder.triangles.Add(offset + 0);
					builder.triangles.Add(offset + 1);
					builder.triangles.Add(offset + 2);
				} else if (vertPerFace == 4) {
					builder.triangles.Add(offset + 0);
					builder.triangles.Add(offset + 1);
					builder.triangles.Add(offset + 2);
					builder.triangles.Add(offset + 0);
					builder.triangles.Add(offset + 2);
					builder.triangles.Add(offset + 3);
				}
			}
		}

		if (builder.vertices.Count > 0) {
			meshes.Add(builder.Build());
		}
		return meshes.ToArray();
	}

	private class MeshBuilder
	{
		public string meshName = "";
		public List<Vector3> vertices = new List<Vector3>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Vector2> uv = new List<Vector2>();
		public List<int> triangles = new List<int>();

		public Mesh Build()
		{
			Mesh mesh = new Mesh();
			mesh.name = this.meshName;
			mesh.vertices = vertices.ToArray();
			mesh.uv = uv.ToArray();
			if (this.normals.Count > 0) {
				mesh.normals = normals.ToArray();
			}
			mesh.triangles = triangles.ToArray();
			if (this.normals.Count <= 0) {
				mesh.RecalculateNormals();
			}
			mesh.RecalculateBounds();
			return mesh;
		}
	};
	
	static private Vector2 ParseFace(string[] tokens) {
		Vector2 v;
		v.x = float.Parse(tokens[1]);
		v.y = float.Parse(tokens[2]);
		return v;
	}

	static private Vector2 ParseVector2(string[] tokens) {
		Vector2 v;
		v.x = float.Parse(tokens[1]);
		v.y = float.Parse(tokens[2]);
		return v;
	}

	static private Vector3 ParseVector3(string[] tokens) {
		Vector3 v;
		v.x = float.Parse(tokens[1]);
		v.y = float.Parse(tokens[2]);
		v.z = float.Parse(tokens[3]);
		return v;
	}
}
