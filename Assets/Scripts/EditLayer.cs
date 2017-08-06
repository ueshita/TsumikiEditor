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
	
	void Awake() {
		this.gameObject.AddComponent<MeshFilter>();
		this.gameObject.AddComponent<MeshCollider>();
		this.gameObject.AddComponent<MeshRenderer>();
	}

	void Update() {
		if (this.dirtyMesh) {
			this.UpdateMesh();
		}
	}
	
	public void SetMaterial(string materialName, string textureName) {
		var material = Resources.Load<Material>("Materials/" + materialName);
		if (!String.IsNullOrEmpty(textureName)) {
			material.mainTexture = Resources.Load<Texture>("Textures/" + textureName);
		}
		var meshRenderer = this.GetComponent<MeshRenderer>();
		meshRenderer.material = material;
		meshRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
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

		this.GetComponent<MeshFilter>().sharedMesh = this.blockGroup.GetSurfaceMesh();
		this.GetComponent<MeshCollider>().sharedMesh = this.blockGroup.GetGuideMesh();

		this.dirtyMesh = false;
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
