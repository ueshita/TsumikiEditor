using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class OBJLoader
{
	public class OBJMaterial {
		public string name;
		public Texture2D albedo;
		public Texture2D emission;
		public Texture2D normal;
		public Texture2D specular;
	}

	public class OBJGroup {
		public string name;
		public Mesh mesh;
		public OBJMaterial material;

		public OBJGroup(string name, Mesh mesh) {
			this.name = name;
			this.mesh = mesh;
		}
	}

	public class OBJModel {
		public string name;
		public OBJGroup[] groups;
		public OBJMaterial[] materials;

		public Mesh GetMesh(int index) {
			return this.groups[index].mesh;
		}

		public GameObject Build(Material baseMaterial) {
			GameObject root = new GameObject();
			root.name = this.name;
			foreach (OBJGroup group in this.groups) {
				GameObject go = new GameObject();
				go.transform.parent = root.transform;
				go.name = group.name;
				var meshFilter = go.AddComponent<MeshFilter>();
				var meshRenderer = go.AddComponent<MeshRenderer>();
				meshFilter.sharedMesh = group.mesh;
				var material = new Material(baseMaterial);
				material.mainTexture = group.material.albedo;
				if (group.material.emission) material.SetTexture("_EmissionMap", group.material.emission);
				if (group.material.normal) material.SetTexture("_BumpMap", group.material.normal);
				if (group.material.specular) material.SetTexture("_SpecGlossMap", group.material.specular);
				meshRenderer.sharedMaterial = material;
			}
			return root;
		}
	}

	static public OBJModel LoadModel(string path) {
		if (!File.Exists(path)) {
			return null;
		}

		string[] lines = File.ReadAllLines(path);

		List<Vector3> positionList = new List<Vector3>();
		List<Vector2> texcoordList = new List<Vector2>();
		List<Vector3> normalList = new List<Vector3>();

		MeshBuilder builder = new MeshBuilder();
		List<OBJGroup> groups = new List<OBJGroup>();

		var materials = LoadMTL(Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".mtl");
			
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
				texcoordList.Add(ParseVector2(tokens) * new Vector2(1.0f, -1.0f));
			} else if (tokens[0] == "vn") {
				normalList.Add(ParseVector3(tokens));
			} else if (tokens[0] == "g") {
				if (builder.vertices.Count > 0) {
					groups.Add(builder.Build(materials));
				}
				builder = new MeshBuilder();
				builder.meshName = tokens[1];
			} else if (tokens[0] == "usemtl") {
				builder.mtlName = tokens[1];
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
			groups.Add(builder.Build(materials));
		}

		var loadedObj = new OBJModel();
		loadedObj.name = Path.GetFileNameWithoutExtension(path);
		loadedObj.groups = groups.ToArray();
		loadedObj.materials = materials.ToArray();
		return loadedObj;
	}

	public static List<OBJMaterial> LoadMTL(string path) {
		if (!File.Exists(path)) {
			return null;
		}

		string[] lines = File.ReadAllLines(path);
		List<OBJMaterial> materials = new List<OBJMaterial>();

		OBJMaterial currentMaterial = null;
		foreach (string line in lines) {
			if (line.Length == 0) {
				continue;
			}
			if (line[0] == '#') {
				continue;
			}
			
			string[] tokens = line.Split(' ');
			if (tokens[0] == "newmtl") {
				currentMaterial = new OBJMaterial();
				materials.Add(currentMaterial);
				currentMaterial.name = tokens[1];
			} else if (tokens[0] == "map_Kd") {
				string dirName = Path.GetDirectoryName(path);
				string textureName = Path.GetFileNameWithoutExtension(tokens[1]);
				string textureExt = Path.GetExtension(tokens[1]);
				currentMaterial.albedo   = EditUtil.LoadTextureFromFile(dirName + "/" + tokens[1]);
				currentMaterial.emission = EditUtil.LoadTextureFromFile(dirName + "/" + textureName + "_e" + textureExt);
				currentMaterial.normal   = EditUtil.LoadTextureFromFile(dirName + "/" + textureName + "_n" + textureExt);
				currentMaterial.specular = EditUtil.LoadTextureFromFile(dirName + "/" + textureName + "_s" + textureExt);
			}
		}

		return materials;
	}

	private class MeshBuilder
	{
		public string meshName = null;
		public string mtlName = null;
		public List<Vector3> vertices = new List<Vector3>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Vector2> uv = new List<Vector2>();
		public List<int> triangles = new List<int>();

		public OBJGroup Build(List<OBJMaterial> materials)
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

			var group = new OBJGroup(this.meshName, mesh);
			if (materials != null) {
				group.material = materials.Find((mat) => mat.name == this.mtlName);
			}

			return group;
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
