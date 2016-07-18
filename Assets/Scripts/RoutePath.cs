using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoutePath : MonoBehaviour
{
	Guide guide;
	bool dirtyMesh;
	
	public bool isSelected = false;
	public Vector3 selectedPosition;

	struct Path {
		public Vector3 p1;
		public Vector3 p2;

		public override bool Equals(object obj) {
			Path target = (Path)obj;
			return (this.p1 == target.p1 && this.p2 == target.p2) ||
				   (this.p1 == target.p2 && this.p2 == target.p1);
		}
	}
	List<Path> paths = new List<Path>();

	void Start() {
		var guideObj = new GameObject();
		guideObj.name = "Guide";
		guideObj.transform.parent = this.transform;
		this.guide = guideObj.AddComponent<Guide>();
		this.guide.SetColor(new Color(1.0f, 1.0f, 0.8f), new Color(1.0f, 1.0f, 0.3f));
	}

	void UpdateMesh() {
		BlockGroup blockGroup = EditManager.Instance.CurrentLayer.GetBlockGroup();
		Mesh panelMesh = blockGroup.GetRouteMesh();
		Mesh lineMesh = this.GetRouteLineMesh();
		this.guide.SetMesh(panelMesh, lineMesh);
	}
	
	// パスラインのポリゴンを作る
	private Mesh GetRouteLineMesh() {
		List<Vector3> lineVertex = new List<Vector3>();
		List<int> lineTriangles = new List<int>();
		
		const int divs = 8;
		foreach (var path in this.paths) {
			for (int j = 0; j <= divs; j++) {
				float s = Mathf.Sin(Mathf.PI * j / divs);
				Vector3 diff = path.p2 - path.p1;
				Vector3 position = path.p1 + diff * j / divs;
				lineVertex.Add(position + Vector3.up * (0.25f + (Mathf.Abs(diff.y) * 0.5f + 0.5f) * s));
			}
			int offset = lineVertex.Count - divs - 1;
			for (int j = 0; j < divs; j++) {
				lineTriangles.Add(offset + j);
				lineTriangles.Add(offset + j + 1);
			}
		}

		Mesh mesh = new Mesh();
		mesh.name = "RouteLines";
		mesh.vertices = lineVertex.ToArray();
		mesh.SetIndices(lineTriangles.ToArray(), MeshTopology.Lines, 0);
		return mesh;
	}

	public void AddPath(Vector3 p1, Vector3 p2) {
		var group = EditManager.Instance.CurrentLayer.GetBlockGroup();
		// 移動可能かチェック
		var b1 = EditManager.Instance.CurrentLayer.GetBlock(p1);
		if (b1 == null || !b1.IsMovable(group)) {
			return;
		}
		var b2 = EditManager.Instance.CurrentLayer.GetBlock(p2);
		if (b2 == null || !b2.IsMovable(group)) {
			return;
		}

		if (p1.x == p2.x && p1.z == p2.z) {
			// XYが同じ場合はパス接続できない
			return;
		}
		if (p1.x != p2.x && p1.z != p2.z) {
			// XYは片方は同じ位置にいる必要がある
			return;
		}
		if (Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.z - p2.z) <= 1) {
			// 隣接をパスで繋げることは意味ない
			return;
		}
		Path path;
		path.p1 = p1;
		path.p2 = p2;
		this.paths.Add(path);
		this.dirtyMesh = true;
	}

	public void RemovePath(Vector3 p1, Vector3 p2) {
		Path path;
		path.p1 = p1;
		path.p2 = p2;
		this.paths.Remove(path);
		this.dirtyMesh = true;
	}
	
	public bool ContainsPath(Vector3 p1, Vector3 p2) {
		Path path;
		path.p1 = p1;
		path.p2 = p2;
		return this.paths.Contains(path);
	}

	void LateUpdate() {
		if (this.dirtyMesh) {
			this.UpdateMesh();
			this.dirtyMesh = false;
		}
	}
	
	public void SetEnabled(bool enabled) {
		this.dirtyMesh = true;
		this.gameObject.SetActive(enabled);
	}
}
