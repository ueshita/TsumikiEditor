using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

// 3Dモデル配置
public class Model
{
	public ModelShape shape {get; private set;}
	public Vector3 position {get; private set;}
	public Vector3 offset {get; private set;}
	public int rotation {get; private set;}
	public float scale {get; private set;}
	public GameObject gameObject {get; private set;}
	private int hashCode;

	public override int GetHashCode() {
		return this.hashCode;
	}

	public Model(ModelShape shape, Vector3 position, int rotation = 0, float scale = 1.0f) {
		this.shape = shape;
		this.SetPosition(position);
		this.SetRotation(rotation);
		this.SetScale(scale);
		this.Show();
	}

	public Model(XmlElement node) {
		this.Deserialize(node);
		this.Show();
	}
	
	public void Show() {
		this.gameObject = this.shape.Build(EditManager.Instance.ModelMaterial);
		this.gameObject.transform.parent = EditManager.Instance.CurrentLayer.transform;
		this.gameObject.transform.localScale = Vector3.one * this.shape.scale * this.scale;
		foreach (var meshFilter in this.gameObject.GetComponentsInChildren<MeshFilter>()) {
			meshFilter.gameObject.AddComponent<MeshCollider>();
		}
		this.SetPosition(this.position);
		this.SetRotation(this.rotation);
	}

	public void Hide() {
		if (this.gameObject != null) {
			GameObject.Destroy(this.gameObject);
			this.gameObject = null;
		}
	}

	public void SetPosition(Vector3 position) {
		this.position = position;
		this.hashCode = EditUtil.PositionToHashCode(position);
		if (this.gameObject != null) {
			this.gameObject.transform.position = this.position + this.shape.offset + this.offset;
		}
	}
	
	public void SetOffset(Vector3 offset) {
		offset.x = Mathf.Clamp(offset.x, -0.5f, 0.5f);
		offset.y = Mathf.Clamp(offset.y, -0.25f, 0.25f);
		offset.z = Mathf.Clamp(offset.z, -0.5f, 0.5f);
		this.offset = offset;
		if (this.gameObject != null) {
			this.gameObject.transform.position = this.position + this.shape.offset + this.offset;
		}
	}
	
	public void SetRotation(int rotation) {
		while (rotation < -180) rotation += 360;
		while (rotation >  180) rotation -= 360;
		this.rotation = rotation;
		if (this.gameObject != null) {
			this.gameObject.transform.rotation = Quaternion.AngleAxis(180.0f - this.rotation, Vector3.up);
		}
	}
	
	public void SetScale(float scale) {
		scale = Mathf.Clamp(scale, 0.1f, 100.0f);
		this.scale = scale;
		if (this.gameObject != null) {
			this.gameObject.transform.localScale = Vector3.one * (this.shape.scale * scale);
		}
	}
	
	// 書き込み
	public void Serialize(XmlElement node) {
		node.SetAttribute("type", this.shape.name);
		node.SetAttribute("x", this.position.x.ToString());
		node.SetAttribute("y", this.position.y.ToString());
		node.SetAttribute("z", this.position.z.ToString());
		node.SetAttribute("offsetX", this.offset.x.ToString());
		node.SetAttribute("offsetY", this.offset.y.ToString());
		node.SetAttribute("offsetZ", this.offset.z.ToString());
		node.SetAttribute("rotation", this.rotation.ToString());
		node.SetAttribute("scale", this.scale.ToString());
	}

	// 読み込み
	public void Deserialize(XmlElement node) {
		if (node.HasAttribute("type")) {
			string typeName = node.GetAttribute("type");
			this.shape = ModelShape.Find(typeName);
			if (this.shape == null) {
				Debug.LogError("Unknown Model's type: " + typeName);
				return;
			}
		} else {
			Debug.LogError("Model's type element is not found.");
			return;
		}

		{
			Vector3 position = Vector3.zero;
			if (node.HasAttribute("x")) {float.TryParse(node.GetAttribute("x"), out position.x);}
			if (node.HasAttribute("y")) {float.TryParse(node.GetAttribute("y"), out position.y);}
			if (node.HasAttribute("z")) {float.TryParse(node.GetAttribute("z"), out position.z);}
			this.position = position;
		}
		{
			Vector3 offset = Vector3.zero;
			if (node.HasAttribute("offsetX")) {float.TryParse(node.GetAttribute("offsetX"), out offset.x);}
			if (node.HasAttribute("offsetY")) {float.TryParse(node.GetAttribute("offsetY"), out offset.y);}
			if (node.HasAttribute("offsetZ")) {float.TryParse(node.GetAttribute("offsetZ"), out offset.z);}
			this.offset = offset;
		}

		{
			int rotation = 0;
			if (node.HasAttribute("rotation")) {
				int.TryParse(node.GetAttribute("rotation"), out rotation);
			}
			this.rotation = rotation;
		}
		{
			float scale = 0;
			if (node.HasAttribute("scale")) {
				float.TryParse(node.GetAttribute("scale"), out scale);
			}
			this.scale = scale;
		}
	}
	
	// ガイド用メッシュを出力
	public void WriteToGuideMesh(ModelMeshMerger meshMerger) {
		Bounds bounds = new Bounds();
		float totalScale = this.shape.scale * this.scale;

		foreach (var meshFilter in this.gameObject.GetComponentsInChildren<MeshFilter>()) {
			Mesh mesh = meshFilter.sharedMesh;
			bounds.SetMinMax(
				Vector3.Min(mesh.bounds.min, bounds.min),
				Vector3.Max(mesh.bounds.max, bounds.max));
		}

		Vector3 localScale = Vector3.Scale(bounds.size, new Vector3(1.0f, 2.0f, 1.0f)) * totalScale;
		Quaternion localRotation = Quaternion.AngleAxis(180.0f - this.rotation, Vector3.up);
		Vector3 localPosition = this.shape.offset + this.offset + 
			Matrix4x4.Rotate(localRotation).MultiplyVector(bounds.center * totalScale);
		
		Matrix4x4 matrix = Matrix4x4.TRS(this.position + localPosition, localRotation, localScale);

		// 頂点を書き出す
		for (int j = 0; j < EditUtil.cubeVertices.Length; j++) {
			meshMerger.vertexPos.Add(matrix.MultiplyPoint3x4(EditUtil.cubeVertices[j]));
			
		}
		// インデックスを書き出す
		for (int i = 0; i < 6; i++) {
			int offset = meshMerger.vertexPos.Count - EditUtil.cubeVertices.Length;
			meshMerger.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 0]);
			meshMerger.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 1]);
			meshMerger.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 2]);
			meshMerger.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 0]);
			meshMerger.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 2]);
			meshMerger.triangles.Add(offset + EditUtil.cubeQuadIndices[i * 4 + 3]);
		}
	}
};
