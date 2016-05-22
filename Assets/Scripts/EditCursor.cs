using UnityEngine;
using System.Collections;

public class EditCursor : MonoBehaviour
{
	public bool visible {get; private set;}
	public Vector3 point {get; private set;}
	public BlockDirection blockDirection {get; private set;}
	public BlockDirection panelDirection {get; private set;}
	
	private EditGuide guide;
	private Mesh panelSurfaceMesh;
	private Mesh panelLineMesh;
	private Mesh blockSurfaceMesh;
	private Mesh blockLineMesh;
	
	void Start() {
		var guideObj = new GameObject();
		guideObj.name = "Guide";
		guideObj.transform.parent = this.transform;
		this.guide = guideObj.AddComponent<EditGuide>();
		this.guide.SetColor(new Color(0.5f, 0.5f, 1.0f), new Color(1.0f, 1.0f, 1.0f));

		this.panelSurfaceMesh = EditUtil.CreatePanelSurfaceMesh();
		this.panelLineMesh = EditUtil.CreatePanelLineMesh();
		this.blockSurfaceMesh = EditUtil.CreateBlockSurfaceMesh();
		this.blockLineMesh = EditUtil.CreateBlockLineMesh();
	}

	public void SetBlock(string typeName) {
		BlockGroup layer = new BlockGroup();
		layer.AddBlock(new Block(Vector3.zero, BlockDirection.Zplus, typeName));
		layer.UpdateMesh();
		this.guide.SetMesh(layer.GetSurfaceMesh(), layer.GetWireMesh());
	}

	public void SetBlock() {
		this.transform.rotation = Quaternion.identity;
		this.guide.SetMesh(this.blockSurfaceMesh, this.blockLineMesh);
	}

	public void SetPanel() {
		this.transform.rotation = Quaternion.identity;
		this.guide.SetMesh(this.panelSurfaceMesh, this.panelLineMesh);
	}

	// 90度回転
	public void TurnBlock(int value) {
		BlockDirection direction = EditUtil.RotateDirection(this.blockDirection, value);
		this.blockDirection = direction;
	}

	public void Update() {
		bool visible = false;
		
		// マウスカーソルの処理
		Vector3 point, normal;
		bool cursorEnabled = this.GetCuesorPoint(out point, out normal);
		var block = EditManager.Instance.CurrentLayer.GetBlock(point);

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Pen:
			if (cursorEnabled) {
				// 手前に立体カーソルを置く
				point += new Vector3(normal.x, normal.y * 0.5f, normal.z);
				visible = true;
				this.point = point;
				this.transform.position = point;
				this.transform.rotation = Quaternion.AngleAxis(
					EditUtil.DirectionToAngle(this.blockDirection), Vector3.up);
				this.transform.localScale = Vector3.one;
			}
			break;
		case EditManager.Tool.Eraser:
		case EditManager.Tool.PointSelector:
			if (cursorEnabled && block != null) {
				// 立体カーソルを置く
				visible = true;
				this.point = point;
				this.transform.position = point;
				this.transform.rotation = Quaternion.identity;
				this.transform.localScale = Vector3.one;
			}
			break;
		case EditManager.Tool.RectSelector:
			break;
		case EditManager.Tool.Brush:
		case EditManager.Tool.Spuit:
			if (cursorEnabled && block != null) {
				this.point = point;
				// 面カーソルを置く
				visible = true;
				this.transform.position = point + new Vector3(normal.x * 0.5f, normal.y * 0.25f, normal.z * 0.5f);
				this.panelDirection = EditUtil.VectorToDirection(normal);
				if (this.panelDirection == BlockDirection.Yplus) {
					this.transform.rotation = Quaternion.identity;
					this.transform.localScale = Vector3.one;
				} else if (this.panelDirection == BlockDirection.Yminus) {
					this.transform.rotation = Quaternion.Euler(180, 0, 0);
					this.transform.localScale = Vector3.one;
				} else {
					this.transform.rotation = Quaternion.Euler(90, EditUtil.DirectionToAngle(this.panelDirection), 0);
					this.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
				}
			}
			break;
		}
		this.visible = visible;
		this.guide.gameObject.SetActive(visible);
	}
	
	// マウスカーソル位置のブロックを取得
	private bool GetCuesorPoint(out Vector3 position, out Vector3 normal) {
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 100)) {
			normal = hit.normal;
			position = hit.point - normal * 0.25f;
			position.x = Mathf.Round(position.x);
			position.y = Mathf.Round(position.y * 2) * 0.5f;
			position.z = Mathf.Round(position.z);
			return true;
		}
		position = Vector3.zero;
		normal = Vector3.zero;
		return false;
	}
}
