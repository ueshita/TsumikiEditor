using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

// レイヤー内のブロックを表示するクラス
public class EditLayer : MonoBehaviour
{
	private BlockGroup blockGroup = new BlockGroup();
	private ModelGroup modelGroup = new ModelGroup();
	bool dirtyMesh = false;

	private List<GameObject> subLayers = new List<GameObject>();
	private Material material;

	public int NumVertices {get; private set;}
	public int NumTriangles {get; private set;}
	public bool SubmeshEnabled {get; set;}
	
	void Awake() {
		this.gameObject.AddComponent<MeshFilter>();
		this.gameObject.AddComponent<MeshCollider>();
		this.gameObject.AddComponent<MeshRenderer>();

		this.blockGroup.SetSurfaceSubMesh(new Vector3i(3, 10, 3));
	}

	void LateUpdate() {
		if (this.dirtyMesh) {
			this.UpdateMesh();
			if (this.SubmeshEnabled) {
				this.UpdateSubMeshes();
			}
			this.dirtyMesh = false;
		}
	}
	
	public void SetMaterial(string materialName) {
		var material = Resources.Load<Material>("Materials/" + materialName);
		this.SetMaterial(material);
	}

	public void SetMaterial(Material material) {
		this.material = material;
		var meshRenderer = this.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = this.material;
		meshRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
	}

	public Material GetMaterial() {
		return material;
	}

	public void Clear() {
		this.blockGroup.Clear();
		this.dirtyMesh = true;
	}

	public void SetDirty() {
		this.dirtyMesh = true;
	}

	// ブロックの追加
	public void AddBlock(Block block) {
		this.blockGroup.AddBlock(block);
		this.dirtyMesh = true;
	}
	
	// ブロックの追加(複数)
	public Block[] AddBlocks(Block[] blocks) {
		var removedBlocks = new List<Block>();
		foreach (var block in blocks) {
			Block foundBlock = this.blockGroup.GetBlock(block.position);
			if (foundBlock != null) {
				this.blockGroup.RemoveBlock(foundBlock);
				removedBlocks.Add(foundBlock);
			}
			this.blockGroup.AddBlock(block);
		}
		this.dirtyMesh = true;
		return removedBlocks.ToArray();
	}
	
	// ブロックの削除
	public void RemoveBlock(Block block) {
		this.blockGroup.RemoveBlock(block);
		this.dirtyMesh = true;
	}

	// ブロックの削除(複数)
	public void RemoveBlocks(Block[] blocks) {
		foreach (var block in blocks) {
			this.blockGroup.RemoveBlock(block);
		}
		this.dirtyMesh = true;
	}

	// 特定の位置のブロックを取得
	public Block GetBlock(Vector3 position) {
		return this.blockGroup.GetBlock(position);
	}

	// 全ブロックの取得
	public Block[] GetAllBlocks() {
		return this.blockGroup.GetAllBlocks();
	}

	public BlockGroup GetBlockGroup() {
		return this.blockGroup;
	}
	
	public ModelGroup GetModelGroup() {
		return this.modelGroup;
	}
	
	// モデルの追加
	public void AddModel(Model model) {
		this.modelGroup.AddModel(model);
	}
	
	// モデルの複数(複数)
	public Model[] AddModels(Model[] models) {
		var removedModels = new List<Model>();
		foreach (var model in models) {
			Model foundModel = this.modelGroup.GetModel(model.position);
			if (foundModel != null) {
				this.modelGroup.RemoveModel(model, true);
				removedModels.Add(foundModel);
			}
			this.modelGroup.AddModel(model);
		}
		return removedModels.ToArray();
	}

	// モデルの削除
	public void RemoveModel(Model model, bool hiding) {
		this.modelGroup.RemoveModel(model, hiding);
	}

	// モデルの削除(複数)
	public void RemoveModels(Model[] models, bool hiding) {
		foreach (var model in models) {
			this.modelGroup.RemoveModel(model, hiding);
		}
	}

	public Model GetModel(GameObject gameObject) {
		return modelGroup.GetModel(gameObject);
	}

	public Model GetModel(Vector3 position) {
		return modelGroup.GetModel(position);
	}

	protected void UpdateMesh() {
		this.blockGroup.UpdateMesh();

		var oldMesh = this.GetComponent<MeshFilter>().sharedMesh;
		if (oldMesh) {
			DestroyImmediate(oldMesh, true);
		}

		var newMesh = this.blockGroup.GetSurfaceMesh();
		this.GetComponent<MeshFilter>().sharedMesh = newMesh;
		this.GetComponent<MeshCollider>().sharedMesh = newMesh;
		this.dirtyMesh = false;

		this.NumVertices = newMesh.vertices.Length;
		this.NumTriangles = newMesh.triangles.Length / 3;
	}
	
	// カリングを有効にするためサブメッシュに分割する
	protected void UpdateSubMeshes() {
		Mesh[] newMeshes = this.blockGroup.GetSurfaceSubMeshes();
		// サブメッシュ用オブジェクトを増やす
		for (int i = this.subLayers.Count; i < newMeshes.Length; i++) {
			GameObject layer = new GameObject();
			layer.name = "SubLayer-" + i;
			layer.transform.parent = this.transform;
			layer.transform.localPosition = Vector3.zero;
			layer.AddComponent<MeshFilter>();
			layer.AddComponent<MeshRenderer>();
			this.subLayers.Add(layer);
		}
		// サブメッシュ用オブジェクトを減らす
		for (int i = this.subLayers.Count; i > newMeshes.Length; i--) {
			GameObject layer = this.subLayers[i - 1];
			GameObject.Destroy(layer);
			this.subLayers.RemoveAt(i - 1);
		}
		for (int i = 0; i < this.subLayers.Count; i++) {
			GameObject layer = this.subLayers[i];
			var meshFilter = layer.GetComponent<MeshFilter>();
			meshFilter.sharedMesh = newMeshes[i];
			var renderer = layer.GetComponent<MeshRenderer>();
			renderer.enabled = true;
			renderer.sharedMaterial = this.material;
			renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
		}
		this.GetComponent<MeshRenderer>().enabled = false;
	}

	public void Serialize(XmlElement node) {
		this.blockGroup.Serialize(node);
		this.modelGroup.Serialize(node);
	}

	public void Deserialize(XmlElement node) {
		this.blockGroup.Deserialize(node);
		this.modelGroup.Deserialize(node);
		this.dirtyMesh = true;
	}
}
