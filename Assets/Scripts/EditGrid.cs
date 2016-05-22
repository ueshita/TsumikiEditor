using UnityEngine;
using System.Collections;

public class EditGrid : MonoBehaviour
{
	void Start() {
		int sizeX = 99, sizeZ = 99;

		var meshFilter = this.gameObject.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = EditUtil.CreateGrid(sizeX, sizeZ);
		
		var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.material = Resources.Load<Material>("Materials/GridMaterial");
		meshRenderer.material.color = new Color(0.5f, 0.5f, 0.5f);

		var collider = this.gameObject.AddComponent<BoxCollider>();
		collider.size = new Vector3(sizeX, 0.0f, sizeZ);

		this.transform.position = new Vector3(0.0f, -0.25f, 0.0f);
	}
}
