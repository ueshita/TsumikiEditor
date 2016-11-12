using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

public class ModelGroup
{
	private List<Model> models = new List<Model>();
	private ModelMeshMerger guideMeshMerger = new ModelMeshMerger();

	// ブロック追加
	public void AddModel(Model model) {
		var foundModel = GetModel(model.position);
		if (foundModel != null) {
			this.RemoveModel(foundModel, true);
		}
		this.models.Add(model);

		if (model.gameObject == null) {
			model.Show();
		}
	}
	// ブロック削除
	public void RemoveModel(Model model, bool hiding) {
		this.models.Remove(model);

		if (hiding) {
			model.Hide();
		}
	}
	// 全クリア
	public void Clear() {
		this.models.Clear();
	}
	
	// ブロック数の取得
	public int GetModelCount() {
		return this.models.Count;
	}
	
	public Model GetModel(GameObject gameObject) {
		for (int i = 0; i < this.models.Count; i++) {
			if (gameObject == this.models[i].gameObject) {
				return this.models[i];
			}
		}
		return null;
	}

	public Model GetModel(Vector3 position) {
		int hash = EditUtil.PositionToHashCode(position);
		for (int i = 0; i < this.models.Count; i++) {
			if (hash == this.models[i].GetHashCode()) {
				return this.models[i];
			}
		}
		return null;
	}

	public bool Contains(Model model) {
		return this.models.Contains(model);
	}
	
	// 全ブロック取得
	public Model[] GetAllModels() {
		return this.models.ToArray();
	}

	// ブロック数を取得
	public int GetNumModels() {
		return this.models.Count;
	}

	// メッシュの更新
	public void UpdateMesh() {
		this.guideMeshMerger.Clear();
		foreach (var model in this.models) {
			model.WriteToGuideMesh(this.guideMeshMerger);
		}
	}

	public Mesh GetGuideMesh() {
		Mesh mesh = new Mesh();
		mesh.name = "ColliderModels";
		mesh.vertices = this.guideMeshMerger.vertexPos.ToArray();
		mesh.SetIndices(this.guideMeshMerger.triangles.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateBounds();
		return mesh;
	}

	public Mesh GetWireMesh() {
		int trianglesCount = this.guideMeshMerger.triangles.Count / 3;
		var tris = this.guideMeshMerger.triangles;
		List<int> lines = new List<int>(trianglesCount * 6);

		for (int i = 0; i < trianglesCount; i++) {
			int i0 = tris[i * 3 + 0];
			int i1 = tris[i * 3 + 1];
			int i2 = tris[i * 3 + 2];
			if (tris.Count >= i * 3 + 6 && 
				tris[i * 3 + 3] == i0 && tris[i * 3 + 4] == i2
			) {
				// 四角形を作る
				int i3 = tris[i * 3 + 5];
				lines.Add(i0); lines.Add(i1);
				lines.Add(i1); lines.Add(i2);
				lines.Add(i2); lines.Add(i3);
				lines.Add(i3); lines.Add(i0);
				i += 1;
			} else {
				// 三角形を作る
				lines.Add(i0); lines.Add(i1);
				lines.Add(i1); lines.Add(i2);
				lines.Add(i2); lines.Add(i0);
			}
		}

		Mesh mesh = new Mesh();
		mesh.name = "WireBlocks";
		mesh.vertices = this.guideMeshMerger.vertexPos.ToArray();
		mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
		mesh.RecalculateBounds();
		return mesh;
	}

	public void Serialize(XmlElement node) {
		foreach (var model in this.models) {
			XmlElement modelNode = node.OwnerDocument.CreateElement("model");
			model.Serialize(modelNode);
			node.AppendChild(modelNode);
		}
	}
	public void Deserialize(XmlElement node) {
		XmlNodeList modelList = node.GetElementsByTagName("model");
		for (int i = 0; i < modelList.Count; i++) {
			XmlElement modelNode = modelList[i] as XmlElement;
			Model model = new Model(modelNode);
			this.AddModel(model);
		}
	}
}

// ブロックのメッシュをまとめるクラス
public class ModelMeshMerger
{
	public List<Vector3> vertexPos = new List<Vector3>();
	public List<Vector2> vertexUv = new List<Vector2>();
	public List<int> triangles = new List<int>();
	
	public void Clear() {
		this.vertexPos.Clear();
		this.vertexUv.Clear();
		this.triangles.Clear();
	}
};
