using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.IO;

public class ModelShape
{
	public readonly string name;
	public readonly int id;
	public readonly string displayName;
	public readonly float scale;
	public readonly GameObject prefab;
	public readonly bool enterable;

	public ModelShape(string name, int id, string displayName, 
		GameObject prefab, float scale, bool enterable
	) {
		this.name = name;
		this.id = id;
		this.displayName = displayName;
		this.prefab = prefab;
		this.scale = scale;
		this.enterable = enterable;
	}
	
	public static ModelShape Find(string name) {
		return table[name];
	}

	public static ModelShape[] palette;
	public static Dictionary<string, ModelShape> table;
	
	public static void LoadData() {
		var jsonAsset = Resources.Load<TextAsset>("ModelShapes");
		string jsonText = jsonAsset.text;

		var jsonDict = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
		var data = jsonDict["data"] as List<object>;

		palette = new ModelShape[data.Count];
		table = new Dictionary<string, ModelShape>();

		for (int i = 0; i < data.Count; i++) {
			var shape = data[i] as Dictionary<string, object>;

			string name = (string)shape["name"];
			int id = (int)(long)shape["id"];
			string displayName = (string)shape["displayName"];
			float scale = (float)(double)shape["scale"];

			var prefab = Resources.Load<GameObject>("Models/" + name);
			if (prefab == null) {
				Debug.LogError("Models/" + name + " is not found.");
			}
			bool enterable = (bool)shape["enterable"];

			palette[i] = new ModelShape(name, id, displayName, prefab, scale, enterable);
			table.Add(name, palette[i]);
		}
	}

	private static Vector3 objectToVector(object listObj) {
		var list = listObj as List<object>;
		return new Vector3(
			(float)(double)list[0],
			(float)(double)list[1],
			(float)(double)list[2]);
	}
}
