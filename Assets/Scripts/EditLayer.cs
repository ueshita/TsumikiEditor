using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

// レイヤー内のブロックを表示するクラス
public class EditLayer : MonoBehaviour
{
	protected BlockGroup blockGroup = new BlockGroup();
	bool dirtyMesh = false;
	
	void Start() {
		this.gameObject.AddComponent<MeshFilter>();
		this.gameObject.AddComponent<MeshCollider>();
		var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		var material = Resources.Load<Material>("Materials/BlockMaterial");
		meshRenderer.material = material;
		material.mainTexture = Resources.Load<Texture>("Textures/01");
	}

	void Update() {
		if (this.dirtyMesh) {
			this.UpdateMesh();
		}
	}

	public void Clear() {
		this.blockGroup.Clear();
		this.dirtyMesh = true;
	}

	public void SetDirty() {
		this.dirtyMesh = true;
	}

	// ブロックの追加
	public Block AddBlock(Block block) {
		this.blockGroup.AddBlock(block);
		this.dirtyMesh = true;
		return block;
	}
	
	// ブロックの追加(複数)
	public void AddBlocks(Block[] blocks) {
		foreach (var block in blocks) {
			this.blockGroup.AddBlock(block);
		}
		this.dirtyMesh = true;
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

	protected void UpdateMesh() {
		this.blockGroup.UpdateMesh();

		this.GetComponent<MeshFilter>().sharedMesh = this.blockGroup.GetSurfaceMesh();
		this.GetComponent<MeshCollider>().sharedMesh = this.blockGroup.GetGuideMesh();

		this.dirtyMesh = false;
	}

	public void Serialize(XmlElement node) {
		this.blockGroup.Serialize(node);
	}

	public void Deserialize(XmlElement node) {
		this.blockGroup.Deserialize(node);
		this.dirtyMesh = true;
	}
}
