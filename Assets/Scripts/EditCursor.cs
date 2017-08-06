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
		this.guide.SetMesh(layer.GetSurfaceMesh(), layer.GetWireMesh());
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
	}

	public void SetBlock() {
		this.transform.rotation = Quaternion.identity;
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
		this.guide.SetMesh(this.blockSurfaceMesh, this.blockLineMesh);
	}

	public void SetPanel() {
		this.transform.rotation = Quaternion.identity;
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one;
		this.guide.SetMesh(this.panelSurfaceMesh, this.panelLineMesh);
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
		var meshFilter = shape.prefab.GetComponent<MeshFilter>();
		this.guide.SetMesh(meshFilter.sharedMesh, null);
		this.guide.transform.localPosition = Vector3.zero;
		this.guide.transform.localScale = Vector3.one * shape.scale;
	}

	// カーソルをモデルの外形にセットする
	public void SetModelBound(Model model) {
		this.transform.rotation = Quaternion.identity;
		var meshFilter = model.gameObject.GetComponent<MeshFilter>();
		var mesh = meshFilter.sharedMesh;
		this.guide.SetMesh(this.blockSurfaceMesh, this.blockLineMesh);
		
		this.guide.transform.localScale = 
			Vector3.Scale(mesh.bounds.size, new Vector3(1.0f, 2.0f, 1.0f)) * 
			(model.shape.scale * model.scale);
		this.guide.transform.localPosition = model.offset + 
			mesh.bounds.center * (model.shape.scale * model.scale);

		this.guide.transform.localRotation = Quaternion.AngleAxis(model.rotation, Vector3.up);
	}

	public void Update() {
		bool visible = false;

		// マウスカーソルの処理
		Vector3 point, normal;
		GameObject gameObject;
		bool cursorEnabled = this.GetCursorPoint(out point, out normal, out gameObject);
		
		this.block = null;
		this.model = null;
		if (gameObject == EditManager.Instance.CurrentLayer.gameObject) {
			this.block = EditManager.Instance.CurrentLayer.GetBlock(point);
		} else if (gameObject != null) {
			this.model = EditManager.Instance.CurrentLayer.GetModel(gameObject);
		}

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Block:
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
			if (cursorEnabled) {
				// 立体カーソルを置く
				if (this.block != null) {
					visible = true;
					this.point = point;
					this.SetBlock();
					this.transform.position = point;
					this.transform.rotation = Quaternion.identity;
					this.transform.localScale = Vector3.one;
				} else if (this.model != null) {
					visible = true;
					this.point = point;
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
				this.point = point;
				// 面カーソルを置く
				visible = true;
				this.panelDirection = EditUtil.VectorToDirection(normal);
				var block = EditManager.Instance.CurrentLayer.GetBlock(point);
				if (block.GetMesh(this.panelDirection) != null) {
					// 面のメッシュが存在していたら、面が選択されている
					this.objectSelected = false;
					this.SetPanel();
					this.transform.position = point + new Vector3(normal.x * 0.5f, normal.y * 0.25f, normal.z * 0.5f);
					switch (this.panelDirection) {
					case BlockDirection.Yplus:
						this.transform.rotation = Quaternion.identity;
						this.transform.localScale = Vector3.one;
						break;
					case BlockDirection.Yminus:
						this.transform.rotation = Quaternion.Euler(180, 0, 0);
						this.transform.localScale = Vector3.one;
						break;
					case BlockDirection.Zplus:
						this.transform.rotation = Quaternion.Euler(90, 180, 0);
						this.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
						break;
					case BlockDirection.Zminus:
						this.transform.rotation = Quaternion.Euler(90, 0, 0);
						this.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
						break;
					case BlockDirection.Xplus:
						this.transform.rotation = Quaternion.Euler(90, 90, 0);
						this.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
						break;
					case BlockDirection.Xminus:
						this.transform.rotation = Quaternion.Euler(90, 270, 0);
						this.transform.localScale = new Vector3(1.0f, 1.0f, 0.5f);
						break;
					}
				} else {
					// 面のメッシュが存在していないなら、オブジェクトが選択されている
					this.objectSelected = true;
					this.SetBlock();
					this.transform.position = point;
					this.transform.rotation = Quaternion.identity;
					this.transform.localScale = Vector3.one;
				}
			}
			break;
		case EditManager.Tool.Model:
			if (cursorEnabled) {
				// ブロックの上に立体カーソルを置く
				point += new Vector3(0.0f, 0.25f, 0.0f);
				visible = true;
				this.point = point;
				this.transform.position = point;
				this.transform.rotation = Quaternion.identity;
				this.transform.localScale = Vector3.one;
			}
			break;
		case EditManager.Tool.RoutePath:
		case EditManager.Tool.MetaInfo:
			if (cursorEnabled && this.block != null && 
				EditUtil.VectorToDirection(normal) == BlockDirection.Yplus
			) {
				this.point = point;
				// 面カーソルを置く
				visible = true;
				this.transform.position = point + new Vector3(normal.x * 0.5f, normal.y * 0.25f, normal.z * 0.5f);
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
	private bool GetCursorPoint(out Vector3 position, out Vector3 normal, out GameObject gameObject) {
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 100)) {
			normal = hit.normal;
			gameObject = hit.transform.gameObject;
			position = hit.point - normal * 0.25f;
			position.x = Mathf.Round(position.x);
			position.y = Mathf.Round(position.y * 2) * 0.5f;
			position.z = Mathf.Round(position.z);
			return true;
		}
		position = Vector3.zero;
		normal = Vector3.zero;
		gameObject = null;
		return false;
	}
}
