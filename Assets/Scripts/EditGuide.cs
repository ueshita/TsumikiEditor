using UnityEngine;
using System.Collections;

public class EditGuide : MonoBehaviour
{
	private MeshFilter surfaceMeshFilter;
	private MeshRenderer surfaceRenderer;
	private MeshFilter outlineMeshFilter;
	private MeshRenderer outlineRenderer;
	
	void Awake() {
		surfaceMeshFilter = this.gameObject.AddComponent<MeshFilter>();
		surfaceRenderer = this.gameObject.AddComponent<MeshRenderer>();
		surfaceRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");

		var outlineObj = new GameObject();
		outlineObj.name = "Outline";
		outlineObj.transform.parent = this.transform;
		outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
		outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
		outlineRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");
	}

	public void SetColor(Color color1, Color color2) {
		Color surfaceColor1 = color1;
		Color surfaceColor2 = color2;
		surfaceColor1.a *= 0.36f;
		surfaceColor2.a *= 0.36f;
		surfaceRenderer.material.SetColor("_Color1",  surfaceColor1);
		surfaceRenderer.material.SetColor("_Color2",  surfaceColor2);
		outlineRenderer.material.SetColor("_Color1",  color1);
		outlineRenderer.material.SetColor("_Color2",  color2);
	}
	
	public void SetMesh(Mesh surface, Mesh outline) {
		surfaceMeshFilter.sharedMesh = surface;
		outlineMeshFilter.sharedMesh = outline;
	}
}
