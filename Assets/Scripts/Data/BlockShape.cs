using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.IO;

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

	public readonly string name;
	public readonly string displayName;
	public readonly int connectionType;
	
	/// 隣のブロックへの接続タイプ
	/// 0: 閉塞無し
	/// 1: 完全閉塞
	/// 2~: 不完全閉塞(~の向き)
	public readonly int[] connection;
	public readonly Mesh[] meshes;
	public readonly int[] panelVertices;

	public BlockShape(string name, string displayName, 
		int connectionType, int[] connection, int[] panelVertices
	) {
		this.name = name;
		this.displayName = displayName;
		this.connectionType = connectionType;
		this.connection = connection;
		this.panelVertices = panelVertices;

		this.meshes = new Mesh[6];

		var obj = Resources.Load<GameObject>("Blocks/" + name);
		if (obj == null) {
			Debug.LogError("Failed to load " + name);
			return;
		}
		string[] objNames = new string[]{"Zplus", "Zminus", "Xminus", "Xplus", "Yplus", "Yminus"};
		for (int i = 0; i < 6; i++) {
			var child = obj.transform.Find(objNames[i]);
			var meshFilter = child.GetComponent<MeshFilter>();
			if (meshFilter) {
				this.meshes[i] = meshFilter.sharedMesh;
			}
		}
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

			string name = (string)shape["name"];
			string displayName = (string)shape["displayName"];
			int connectionType = (int)(long)shape["connectionType"];
			int[] connection = objectToIntArray(shape["connection"]);
			int[] panelVertices = objectToIntArray(shape["panelVertices"]);
			
			palette[i] = new BlockShape(name, displayName, connectionType, connection, panelVertices);
			table.Add(name, palette[i]);
		}
	}

	private static int[] objectToIntArray(object listObj) {
		var list = listObj as List<object>;
		var result = new int[list.Count];
		for (int i = 0; i < result.Length; i++) {
			result[i] = (int)(long)list[i];
		}
		return result;
	}
}
