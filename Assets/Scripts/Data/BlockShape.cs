using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.IO;

public enum BlockConnection {
	None				= 0,
	Square				= 1,	// 四角面
	Half				= 2,	// 半四角面(横)
	HalfVertical		= 19,	// 半四角面(縦)
	Slope				= 3,	// 斜面
	SteepSlopeTop		= 4,	// 急斜面(上)
	SteepSlopeBtm		= 5,	// 急斜面(下)
	Stair				= 6,	// 階段
	Fence				= 7,
	Arch				= 8,
	ArcSquare			= 9,
	
	HalfSquare1			= 10,
	HalfSquare2			= 11,

	Circle1				= 12,
	Circle2				= 13,
	
	Triangle1			= 14,
	Triangle2			= 15,

	Pipe2				= 17,	// パイプ(2本)
	ReverseSlope		= 18,	// 天井斜面
	
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

	public bool autoPlacement {get; private set;}	// 自動配置
	public int wall {get; private set;}				// 壁タイプ
	
	public bool Init(string name, string displayName, 
		BlockConnection[] connection, BlockDirection[] connectionDir, 
		float[] panelVertices, bool autoPlacement, int wall
	) {
		this.name = name;
		this.displayName = displayName;
		this.connection = connection;
		this.connectionDir = connectionDir;
		this.panelVertices = panelVertices;
		this.autoPlacement = autoPlacement;
		this.wall = wall;
		return this.LoadMesh();
	}

	public bool InitWithDict(Dictionary<string, object> dict) {
		this.name = (string)dict["name"];
		this.displayName = (string)dict["displayName"];
		this.connection = objectToBlockConnectionArray(dict["connection"]);
		this.connectionDir = objectToBlockDirectionArray(dict["connectionDir"]);
		if (dict.ContainsKey("panelVertices")) {
			this.panelVertices = objectToFloatArray(dict["panelVertices"]);
		}

		if (dict.ContainsKey("autoPlacement")) {
			this.autoPlacement = (bool)dict["autoPlacement"];
		}
		if (dict.ContainsKey("wall")) {
			this.wall = (int)(long)dict["wall"];
		}
		
		return this.LoadMesh();
	}

	public bool LoadMesh() {
		var obj = Resources.Load<GameObject>("Blocks/" + name);
		if (obj == null) {
			return false;
		}
			
		var meshes = new List<Mesh>();

		// 固定メッシュを探す
		string[] objNames = new string[]{"Zplus", "Zminus", "Xplus", "Xminus", "Yplus", "Yminus"};
		for (int i = 0; i < objNames.Length; i++) {
			Mesh mesh = null;
			var child = obj.transform.Find(objNames[i]);
			if (child) {
				var meshFilter = child.GetComponent<MeshFilter>();
				if (meshFilter) {
					mesh = meshFilter.sharedMesh;
				}
			}
			meshes.Add(mesh);
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

	public static List<BlockShape> palette;
	public static Dictionary<string, BlockShape> table;
	
	public static void LoadData() {
		var jsonAsset = Resources.Load<TextAsset>("BlockShapes");
		string jsonText = jsonAsset.text;

		var jsonDict = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
		var data = jsonDict["data"] as List<object>;

		palette = new List<BlockShape>();
		table = new Dictionary<string, BlockShape>();

		for (int i = 0; i < data.Count; i++) {
			var dict = data[i] as Dictionary<string, object>;
			string name = (string)dict["name"];

			var shape = new BlockShape();
			if (shape.InitWithDict(dict)) {
				palette.Add(shape);
				table.Add(name, shape);
			}
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
