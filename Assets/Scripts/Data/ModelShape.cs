using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;
using System.IO;

public class ModelShape
{
	public readonly string name;
	public readonly string displayName;
	public readonly Vector3 offset;
	public readonly float scale;
	public readonly OBJLoader.OBJModel model;
	public readonly bool enterable;

	public ModelShape(string name, string displayName, OBJLoader.OBJModel model, 
		Vector3 offset, float scale, bool enterable
	) {
		this.name = name;
		this.displayName = displayName;
		this.model = model;
		this.offset = offset;
		this.scale = scale;
		this.enterable = enterable;
	}

	public GameObject Build(Material baseMaterial) {
		return this.model.Build(baseMaterial);
	}
	
	public static ModelShape Find(string name) {
		ModelShape shape;
		return table.TryGetValue(name, out shape) ? shape : null;
	}

	public static ModelShape[] palette;
	public static Dictionary<string, ModelShape> table;
	
	public static void LoadData() {
		string jsonText = File.ReadAllText(Application.streamingAssetsPath + "/ModelShapes.json");
		
		var jsonDict = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
		var data = jsonDict["data"] as List<object>;

		palette = new ModelShape[data.Count];
		table = new Dictionary<string, ModelShape>();

		for (int i = 0; i < data.Count; i++) {
			var shape = data[i] as Dictionary<string, object>;

			string name = (string)shape["name"];
			string path = (string)shape["path"];
			string displayName = shape.ContainsKey("displayName") ? (string)shape["displayName"] : name;
			float scale = shape.ContainsKey("scale") ? (float)(double)shape["scale"] : 1.0f;
			float offsetY = shape.ContainsKey("offsetY") ? (float)(double)shape["offsetY"] : 0.0f;
			bool enterable = shape.ContainsKey("enterable") ? (bool)shape["enterable"] : false;

			var model = OBJLoader.LoadModel(Application.streamingAssetsPath + "/ObjectModels/" + path);
			if (model != null) {
				palette[i] = new ModelShape(name, displayName, model, new Vector3(0.0f, offsetY, 0.0f), scale, enterable);
				table.Add(name, palette[i]);
				ModelPalette.Instance.AddModel(name, displayName);
			}
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
