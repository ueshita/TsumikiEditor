using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.IO;

public enum BlockConnection {
	None				= 0,
	Square				= 1,
	Slope				= 3,
	SteepSlopeTop		= 4,
	SteepSlopeBtm		= 5,
	Stair				= 6,
	Fence				= 7,
	Arch				= 8,
	ArcSquare			= 9,
	
	HalfSquare1			= 10,
	HalfSquare2			= 11,

	Circle1				= 12,
	Circle2				= 13,
	
	Triangle1			= 14,
	Triangle2			= 15,

	Pipe2				= 17,
}
public class BlockShape
{
	public class Face
	{
		public readonly int type;
		public readonly int[] indices;
		public readonly Vector2[] uv;

		public Face(int type, int[] indices, Vector2[] uv) {
			this.indices = indices;
			this.uv = uv;
		}
	}

	public string name {get; private set;}
	public string displayName {get; private set;}
	public BlockConnection[] connection {get; private set;}
	public BlockDirection[] connectionDir {get; private set;}
	public Mesh[] meshes {get; private set;}
	public float[] panelVertices {get; private set;}

	public bool autoPlacement {get; private set;}
	public BlockShape[] subShapes {get; private set;}

	public BlockShape(string name, string displayName, 
		BlockConnection[] connection, BlockDirection[] connectionDir, 
		float[] panelVertices, bool autoPlacement
	) {
		this.name = name;
		this.displayName = displayName;
		this.connection = connection;
		this.connectionDir = connectionDir;
		this.panelVertices = panelVertices;
		this.autoPlacement = autoPlacement;
	}

	public bool Init() {
		var obj = Resources.Load<GameObject>("Blocks/" + name);
		if (obj == null) {
			return false;
		}
			
		var meshes = new List<Mesh>();

		// 固定メッシュを探す
		string[] objNames = new string[]{"Zplus", "Zminus", "Xminus", "Xplus", "Yplus", "Yminus"};
		for (int i = 0; i < objNames.Length; i++) {
			var child = obj.transform.Find(objNames[i]);
			if (child) {
				var meshFilter = child.GetComponent<MeshFilter>();
				if (meshFilter) {
					meshes.Add(meshFilter.sharedMesh);
				}
			}
		}

		// その他のメッシュを探す
		for (int i = 0; i < obj.transform.childCount; i++) {
			var child = obj.transform.GetChild(i);
			if (child) {
				var meshFilter = child.GetComponent<MeshFilter>();
				if (meshFilter && !meshes.Contains(meshFilter.sharedMesh)) {
					meshes.Add(meshFilter.sharedMesh);
				}
			}
		}

		this.meshes = meshes.ToArray();

		return true;
	}

	public static BlockShape Find(string name) {
		return table[name];
	}

	public static BlockShape[] palette;
	public static Dictionary<string, BlockShape> table;
	
	public static void LoadData() {
		var jsonAsset = Resources.Load<TextAsset>("BlockShapes");
		string jsonText = jsonAsset.text;

		var jsonDict = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
		var data = jsonDict["data"] as List<object>;

		palette = new BlockShape[data.Count];
		table = new Dictionary<string, BlockShape>();

		for (int i = 0; i < data.Count; i++) {
			var shape = data[i] as Dictionary<string, object>;
			//try {
				string name = (string)shape["name"];
				string displayName = (string)shape["displayName"];
				var connection = objectToBlockConnectionArray(shape["connection"]);
				var connectionDir = objectToBlockDirectionArray(shape["connectionDir"]);
				var panelVertices = objectToFloatArray(shape["panelVertices"]);
				bool autoPlacement = false;
				if (shape.ContainsKey("autoPlacement")) {
					autoPlacement = (bool)shape["autoPlacement"];
				}

				palette[i] = new BlockShape(name, displayName, connection, connectionDir, panelVertices, autoPlacement);
				if (palette[i].Init()) {
					table.Add(name, palette[i]);
				} else {
					Debug.LogError("Loading error: " + (string)shape["name"]);
				}
			//} finally {
			//	Debug.LogError("Format error: " + (string)shape["name"]);
			//}
		}
	}

	private static float[] objectToFloatArray(object listObj) {
		var list = listObj as List<object>;
		var result = new float[list.Count];
		for (int i = 0; i < result.Length; i++) {
			result[i] = (float)(double)list[i];
		}
		return result;
	}
	private static BlockConnection[] objectToBlockConnectionArray(object listObj) {
		var list = listObj as List<object>;
		var result = new BlockConnection[list.Count];
		for (int i = 0; i < result.Length; i++) {
			result[i] = (BlockConnection)(long)list[i];
		}
		return result;
	}
	private static BlockDirection[] objectToBlockDirectionArray(object listObj) {
		var list = listObj as List<object>;
		var result = new BlockDirection[list.Count];
		for (int i = 0; i < result.Length; i++) {
			result[i] = (BlockDirection)(long)list[i];
		}
		return result;
	}
}
