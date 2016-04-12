using UnityEngine;
using System.Collections;

public class EditCursor : MonoBehaviour
{
	void Start() {
		BlockGroup layer = new BlockGroup();

		layer.AddBlock(new CubeBlock());
		layer.UpdateMesh();
		
		var meshFilter = this.gameObject.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = EditUtil.CreateWireBlock();
		
		var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");
		meshRenderer.material.color = new Color(1.0f, 0.5f, 0.5f);
	}

	public void SetPosition(Vector3 position) {
		this.transform.position = position;
	}

	public void SetBlock(Block block) {
		this.transform.position = block.position;
	}
}
