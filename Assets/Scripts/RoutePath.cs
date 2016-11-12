using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

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
	
	void LateUpdate() {
		if (this.dirtyMesh) {
			this.UpdateMesh();
			this.dirtyMesh = false;
		}
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
	
	public void Clear() {
		this.paths.Clear();
		this.dirtyMesh = true;
	}

	public bool CanAddPath(Vector3 p1, Vector3 p2) {
		var blockGroup = EditManager.Instance.CurrentLayer.GetBlockGroup();
		// 移動可能かチェック
		var b1 = EditManager.Instance.CurrentLayer.GetBlock(p1);
		if (b1 == null || !b1.IsEnterable(blockGroup)) {
			return false;
		}
		var b2 = EditManager.Instance.CurrentLayer.GetBlock(p2);
		if (b2 == null || !b2.IsEnterable(blockGroup)) {
			return false;
		}

		if (p1.x == p2.x && p1.z == p2.z) {
			// XYが同じ場合はパス接続できない
			return false;
		}
		if (p1.x != p2.x && p1.z != p2.z) {
			// XYは片方は同じ位置にいる必要がある
			return false;
		}
		if (Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.z - p2.z) <= 1) {
			// 隣接をパスで繋げることは意味ない
			return false;
		}
		return true;
	}

	public void AddPath(Vector3 p1, Vector3 p2) {
		if (!this.CanAddPath(p1, p2)) {
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

	public Vector3[] FindPaths(Vector3 position) {
		var list = new List<Vector3>();
		foreach (var path in this.paths) {
			if (position == path.p1) {
				list.Add(path.p2);
			}
		}
		foreach (var path in this.paths) {
			if (position == path.p2) {
				list.Add(path.p1);
			}
		}
		return list.ToArray();
	}
	
	public void SetEnabled(bool enabled) {
		this.dirtyMesh = true;
		this.gameObject.SetActive(enabled);
	}
	
	public void Serialize(XmlElement parentNode) {
		foreach (var path in this.paths) {
			XmlElement node = parentNode.OwnerDocument.CreateElement("path");
			node.SetAttribute("x1", path.p1.x.ToString());
			node.SetAttribute("y1", path.p1.y.ToString());
			node.SetAttribute("z1", path.p1.z.ToString());
			node.SetAttribute("x2", path.p2.x.ToString());
			node.SetAttribute("y2", path.p2.y.ToString());
			node.SetAttribute("z2", path.p2.z.ToString());
			parentNode.AppendChild(node);
		}
	}

	public void Deserialize(XmlElement parentNode) {
		XmlNodeList pathList = parentNode.GetElementsByTagName("path");
		for (int i = 0; i < pathList.Count; i++) {
			XmlElement node = pathList[i] as XmlElement;
			Vector3 p1 = Vector3.zero, p2 = Vector3.zero;
			if (node.HasAttribute("x1")) {float.TryParse(node.GetAttribute("x1"), out p1.x);}
			if (node.HasAttribute("y1")) {float.TryParse(node.GetAttribute("y1"), out p1.y);}
			if (node.HasAttribute("z1")) {float.TryParse(node.GetAttribute("z1"), out p1.z);}
			if (node.HasAttribute("x2")) {float.TryParse(node.GetAttribute("x2"), out p2.x);}
			if (node.HasAttribute("y2")) {float.TryParse(node.GetAttribute("y2"), out p2.y);}
			if (node.HasAttribute("z2")) {float.TryParse(node.GetAttribute("z2"), out p2.z);}
			this.AddPath(p1, p2);
		}
	}
}
