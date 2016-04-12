using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class EditLayer : MonoBehaviour
{
	protected BlockGroup blockGroup = new BlockGroup();
	bool dirtyMesh = false;
	
	void Start() {
		this.gameObject.AddComponent<MeshFilter>();
		this.gameObject.AddComponent<MeshCollider>();
		var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.material = Resources.Load<Material>("Materials/StandardBlockMaterial");
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

	public Block AddBlock(Block block) {
		this.blockGroup.AddBlock(block);
		this.dirtyMesh = true;
		return block;
	}

	public Block RemoveBlock(Vector3 position) {
		var block = this.blockGroup.RemoveBlock(position);
		this.dirtyMesh = true;
		return block;
	}
	public Block GetBlock(Vector3 position) {
		return this.blockGroup.GetBlock(position);
	}
	public Block[] GetAllBlocks() {
		return this.blockGroup.GetAllBlocks();
	}

	protected void UpdateMesh() {
		this.blockGroup.UpdateMesh();

		this.GetComponent<MeshFilter>().sharedMesh = this.blockGroup.GetSurfaceMesh();
		this.GetComponent<MeshCollider>().sharedMesh = this.blockGroup.GetColliderMesh();

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
