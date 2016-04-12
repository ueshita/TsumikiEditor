using UnityEngine;
using System.Collections;

public class EditGrid : MonoBehaviour
{
	void Start() {
		int sizeX = 99, sizeZ = 99;

		var meshFilter = this.gameObject.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = EditUtil.CreateGrid(new Vector3(0.0f, -0.25f, 0.0f), sizeX, sizeZ);
		
		var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");
		meshRenderer.material.color = new Color(0.5f, 0.5f, 0.5f);

		var collider = this.gameObject.AddComponent<BoxCollider>();
		collider.size = new Vector3(sizeX, 0.0f, sizeZ);
	}

	public void SetPosition(Vector3 position) {
		this.transform.position = position;
	}
}
