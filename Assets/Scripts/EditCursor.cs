using UnityEngine;
using System.Collections;

public class EditCursor : MonoBehaviour
{
	public bool visible {get; private set;}
	public Vector3 point {get; private set;}
	public BlockDirection blockDirection {get; private set;}
	public BlockDirection panelDirection {get; private set;}
	public bool objectSelected {get; private set;}
	
	public Block block {get; private set;}
	public Model model {get; private set;}
	
	private Guide guide;
	private Mesh panelSurfaceMesh;
	private Mesh panelLineMesh;
	private Mesh blockSurfaceMesh;
	private Mesh blockLineMesh;

	struct Point
	{
		public Vector3 position;
		public Vector3 normal;
		public GameObject gameObject;
		public int meshId;
	}
	
	void Awake() {
		var guideObj = new GameObject();
		guideObj.name = "Guide";
		guideObj.transform.parent = this.transform;
		this.guide = guideObj.AddComponent<Guide>();
		this.guide.SetColor(new Color(0.5f, 0.5f, 1.0f), new Color(1.0f, 1.0f, 1.0f));

		this.panelSurfaceMesh = EditUtil.CreatePanelSurfaceMesh();
		this.panelLineMesh = EditUtil.CreatePanelLineMesh();
		this.blockSurfaceMesh = EditUtil.CreateBlockSurfaceMesh();
		this.blockLineMesh = EditUtil.CreateBlockLineMesh();
	}

	public void SetBlock(string typeName) {
		BlockShape shape = BlockShape.Find(typeName);
		if (shape == null) {
			return;
		}
		
		this.transform.rotation = Quaternion.identity;
		BlockGroup layer = new BlockGroup();
		layer.AddBlock(new Block(Vector3.zero, BlockDirection.Zplus, shape));
		layer.UpdateMesh();
		this.guide.SetMesh(layer.GetSurfaceMesh(), layer.GetWireMesh(), true);
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
		this.guide.transform.localRotation = Quaternion.identity;
	}

	public void SetBlock() {
		this.transform.rotation = Quaternion.identity;
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
		this.guide.transform.localRotation = Quaternion.identity;
		this.guide.SetMesh(this.blockSurfaceMesh, this.blockLineMesh, false);
	}

	public void SetPanel() {
		this.transform.rotation = Quaternion.identity;
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
		this.guide.transform.localRotation = Quaternion.identity;
		this.guide.SetMesh(this.panelSurfaceMesh, this.panelLineMesh, false);
	}

	// 90度回転
	public void TurnBlock(int value) {
		BlockDirection direction = EditUtil.RotateDirection(this.blockDirection, value);
		this.blockDirection = direction;
	}
	
	public void SetModel(string typeName) {
		ModelShape shape = ModelShape.Find(typeName);
		if (shape == null) {
			return;
		}
		
		this.transform.rotation = Quaternion.identity;
		//this.guide.SetMesh(shape.model.GetMesh(0), null, false);
		this.guide.SetModel(shape);
		this.guide.transform.localPosition = shape.offset;
		this.guide.transform.localScale = Vector3.one * shape.scale;
		this.guide.transform.localRotation = Quaternion.identity;
	}
	
	public void SetMesh(Mesh mesh) {
		this.transform.rotation = Quaternion.identity;
		this.guide.SetMesh(mesh, null, false);
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
		this.guide.transform.localRotation = Quaternion.identity;
	}

	// カーソルをモデルの外形にセットする
	public void SetModelBound(Model model) {
		this.transform.rotation = Quaternion.identity;

		float totalScale = model.shape.scale * model.scale;
		Quaternion rotation = Quaternion.AngleAxis(180.0f - model.rotation, Vector3.up);

		Bounds bounds = new Bounds();
		foreach (var meshFilter in model.gameObject.GetComponentsInChildren<MeshFilter>()) {
			Mesh mesh = meshFilter.sharedMesh;
			bounds.SetMinMax(
				Vector3.Min(mesh.bounds.min, bounds.min),
				Vector3.Max(mesh.bounds.max, bounds.max));
		}

		this.guide.SetMesh(this.blockSurfaceMesh, this.blockLineMesh, false);
		this.guide.transform.localScale = Vector3.Scale(bounds.size, new Vector3(1.0f, 2.0f, 1.0f)) * totalScale;
		this.guide.transform.localRotation = rotation;
		this.guide.transform.localPosition = model.offset + model.shape.offset + 
			Matrix4x4.Rotate(rotation).MultiplyVector(bounds.center * totalScale);
	}

	public void Update() {
		bool visible = false;

		// マウスカーソルの処理
		Point point;
		bool cursorEnabled = this.GetCursorPoint(out point);
		
		this.block = null;
		this.model = null;
		if (point.gameObject != null) { 
			var layer = point.gameObject.GetComponent<EditLayer>();
			if (layer != null) {
				if (layer == EditManager.Instance.CurrentLayer) {
					this.block = layer.GetBlock(point.position);
				}
			} else {
				this.model = EditManager.Instance.CurrentLayer.GetModel(point.gameObject);
			}
		}

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Block:
			if (cursorEnabled) {
				// 手前に立体カーソルを置く
				visible = true;
				this.point = EditUtil.ResolvePosition(point.position + 
					new Vector3(point.normal.x, point.normal.y * 0.5f, point.normal.z));
				this.transform.position = this.point;
				this.transform.rotation = Quaternion.AngleAxis(
					EditUtil.DirectionToAngle(this.blockDirection), Vector3.up);
				this.transform.localScale = Vector3.one;
			}
			break;
		case EditManager.Tool.Eraser:
		case EditManager.Tool.PointSelector:
			if (cursorEnabled) {
				// 立体カーソルを置く
				if (this.block != null) {
					visible = true;
					this.point = point.position;
					this.SetBlock();
					this.transform.position = point.position;
					this.transform.rotation = Quaternion.identity;
					this.transform.localScale = Vector3.one;
				} else if (this.model != null) {
					visible = true;
					this.point = point.position;
					this.SetModelBound(this.model);
					this.transform.position = this.model.position;
					this.transform.rotation = Quaternion.identity;
					this.transform.localScale = Vector3.one;
				}
			}
			break;
		case EditManager.Tool.Brush:
		case EditManager.Tool.Spuit:
			if (cursorEnabled && this.block != null) {
				this.point = point.position;
				// 面カーソルを置く
				visible = true;
				this.panelDirection = EditUtil.VectorToDirection(point.normal);

				Mesh mesh = this.block.GetMesh(this.panelDirection);
				if (mesh != null) {
					// 面が選択されている
					this.objectSelected = false;
					this.SetMesh(mesh);
				} else {
					mesh = this.block.GetObjectMesh();
					if (mesh != null) {
						// オブジェクトが選択されている
						this.objectSelected = true;
						this.SetMesh(mesh);
					} else {
						this.SetBlock();
					}
				}

				Vector3 position = point.position + point.normal * 0.01f;
				this.transform.position = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y * 2.0f) * 0.5f, Mathf.Round(position.z));
				this.transform.rotation = Quaternion.AngleAxis(
					EditUtil.DirectionToAngle(this.block.direction) + 180.0f, Vector3.up);
				this.transform.localScale = Vector3.one;
			}
			break;
		case EditManager.Tool.Model:
			if (cursorEnabled) {
				// 手前に立体カーソルを置く
				visible = true;
				this.point = EditUtil.ResolvePosition(point.position + 
					new Vector3(point.normal.x, point.normal.y * 0.5f, point.normal.z));
				this.transform.position = this.point;
				this.transform.rotation = Quaternion.AngleAxis(
					EditUtil.DirectionToAngle(this.blockDirection), Vector3.up);
				this.transform.localScale = Vector3.one;
			}
			break;
		case EditManager.Tool.RoutePath:
		case EditManager.Tool.MetaInfo:
			if (cursorEnabled && this.block != null && 
				EditUtil.VectorToDirection(point.normal) == BlockDirection.Yplus
			) {
				this.point = point.position;
				visible = true;
				// 面カーソルを置く
				this.SetPanel();
				Vector3 position = point.position + point.normal * 0.01f;
				this.transform.position = new Vector3(Mathf.Round(position.x), 
					Mathf.Round(position.y * 2.0f) * 0.5f + 0.25f, 
					Mathf.Round(position.z));
				this.panelDirection = BlockDirection.Yplus;
				this.transform.rotation = Quaternion.identity;
				this.transform.localScale = Vector3.one;
			}
			break;
		}
		this.visible = visible;
		this.guide.gameObject.SetActive(visible);
	}
	
	// マウスカーソル位置のブロックを取得
	private bool GetCursorPoint(out Point point) {
		point = new Point();
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 100)) {
			point.normal = hit.normal;
			point.gameObject = hit.transform.gameObject;
			point.position = EditUtil.ResolvePosition(hit.point - point.normal * 0.25f);
			point.meshId = (int)hit.textureCoord2.x;
			return true;
		}
		point.position = Vector3.zero;
		point.normal = Vector3.zero;
		point.gameObject = null;
		return false;
	}
}
