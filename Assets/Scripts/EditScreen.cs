using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class EditScreen : MonoBehaviour,
    IPointerDownHandler,
	IPointerUpHandler,
	IBeginDragHandler,
	IEndDragHandler,
    IDragHandler,
	IScrollHandler
{
	CameraController cameraController;
	EditCursor editCursor = null;
	Text cursorPositionText;
	Image rectSelectImage;

	bool modifierControl = false;
	bool modifierShift = false;

	void Start() {
		this.cameraController = Camera.main.GetComponent<CameraController>();

		var cursorObj = new GameObject("EditCursor");
		cursorObj.transform.parent = EditManager.Instance.transform;
		this.editCursor = cursorObj.AddComponent<EditCursor>();
		
		var rectSelectObj = new GameObject("RectSelect");
		rectSelectObj.transform.parent = this.transform;
		this.rectSelectImage = rectSelectObj.AddComponent<Image>();
		this.rectSelectImage.enabled = false;
		this.rectSelectImage.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

		this.cursorPositionText = GameObject.Find("CursorPositionText").GetComponent<Text>();
	}
	void Update() {
		// マウスカーソルの処理
		Vector3 point, normal;
		if (this.GetCuesorPoint(out point, out normal)) {
			this.editCursor.gameObject.SetActive(true);
			
			if (EditManager.Instance.GetTool() == EditManager.Tool.Pen) {
				point += normal;
			}
			
			this.editCursor.SetPosition(point);
			this.cursorPositionText.text = String.Format(
				"(x:{0,4})(y:{1,4})(z:{2,4})", 
				Mathf.RoundToInt(point.x), 
				Mathf.RoundToInt(point.y), 
				Mathf.RoundToInt(point.z));

		} else {
			this.editCursor.gameObject.SetActive(false);
		}
		
	}
	void OnGUI() {
		Event e = Event.current;
		this.modifierControl = e.control;
		this.modifierShift = e.shift;

		if (e.isKey) {
			if (e.control) {
				switch (e.keyCode) {
				// アンドゥ
				case KeyCode.Z:
					EditManager.Instance.Undo();
					break;
				// リドゥ
				case KeyCode.Y:
					EditManager.Instance.Redo();
					break;
				}
			} else {
				switch (e.keyCode) {
				// 削除
				case KeyCode.Delete:
					EditManager.Instance.RemoveSelectedBlocks();
					break;
				case KeyCode.UpArrow:
					break;
				case KeyCode.DownArrow:
					break;
				case KeyCode.LeftArrow:
					break;
				case KeyCode.RightArrow:
					break;
				}
			}
		}
	}
	public void OnPointerDown(PointerEventData e) {
		if (e.button == 0) {
		}
	}
	public void OnPointerUp(PointerEventData e) {
		if (e.button == 0) {
			if (this.editCursor.gameObject.activeSelf) {
				this.Block_OnClicked(this.editCursor.transform.position);
			} else {
				this.BackGround_OnClicked(this.editCursor.transform.position);
			}
		}
	}
	public void OnBeginDrag(PointerEventData e) {
		if (e.button == 0) {
			this.Screen_OnBeginDrag(e.position);
		} else {
			this.cameraController.OnBeginDrag((int)e.button, e.position);
		}
	}
	public void OnEndDrag(PointerEventData e) {
		if (e.button == 0) {
			this.Screen_OnEndDrag(e.position);
		} else {
			this.cameraController.OnEndDrag((int)e.button, e.position);
		}
	}
	public void OnDrag(PointerEventData e) {
		if (e.button == 0) {
			this.Screen_OnDrag(e.position);
		} else {
			this.cameraController.OnDrag((int)e.button, e.position);
		}
	}
	public void OnScroll(PointerEventData e) {
		this.cameraController.OnScroll(e.scrollDelta);
	}
	
	public void Block_OnClicked(Vector3 point) {
		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Pen:
			EditManager.Instance.AddBlock(0, point);
			break;
		case EditManager.Tool.Eraser:
			EditManager.Instance.RemoveBlock(0, point);
			break;
		case EditManager.Tool.Brush:
			break;
		case EditManager.Tool.Spuit:
			break;
		case EditManager.Tool.PointSelect:
			this.Block_OnSelect(point);
			break;
		case EditManager.Tool.RectSelect:

			break;
		}
	}

	public void BackGround_OnClicked(Vector3 point) {
		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Pen:
			break;
		case EditManager.Tool.Eraser:
			break;
		case EditManager.Tool.Brush:
			break;
		case EditManager.Tool.Spuit:
			break;
		case EditManager.Tool.PointSelect:
			if (!this.modifierControl && !this.modifierShift) {
				EditManager.Instance.Selection.Clear();
			}
			break;
		case EditManager.Tool.RectSelect:
			if (!this.modifierControl && !this.modifierShift) {
				EditManager.Instance.Selection.Clear(true);
			}
			break;
		}
	}

	public void Block_OnSelect(Vector3 point) {
		var selection = EditManager.Instance.Selection;
		if (!this.modifierControl && !this.modifierShift) {
			selection.Clear();
		}

		if (this.modifierShift && selection.Count > 0) {
			selection.SelectRange(selection.LastPosition, point);
		} else {
			if (selection.IsSelected(point)) {
				selection.Remove(point);
			} else {
				selection.Add(point);
			}
		}
	}
	
	// 画面選択
	public void Screen_OnBeginDrag(Vector3 point) {
		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.RectSelect:
			this.BeginRectSelection(point);
			break;
		}
	}
	public void Screen_OnEndDrag(Vector3 point) {
		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.RectSelect:
			this.EndRectSelection(point);
			break;
		}
	}
	public void Screen_OnDrag(Vector3 point) {
		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.RectSelect:
			this.UpdateRectSelection(point);
			break;
		}
	}

	private Vector3 rectBasePoint;
	
	public void BeginRectSelection(Vector3 point) {
		EditManager.Instance.Selection.Backup();

		this.rectBasePoint = point;

		this.rectSelectImage.rectTransform.sizeDelta = Vector2.zero;
		this.rectSelectImage.enabled = true;
	}
	public void EndRectSelection(Vector3 point) {
		this.UpdateRectSelection(point);

		this.rectSelectImage.enabled = false;
	}
	public void UpdateRectSelection(Vector3 point) {
		var selection = EditManager.Instance.Selection;
		
		selection.Clear();
		selection.Restore();

		Vector3 bpos = this.rectBasePoint;
		Vector3 epos = point;
		EditUtil.MinMaxElements(ref bpos, ref epos);
		
		Vector3 size = epos - bpos;
		
		this.rectSelectImage.rectTransform.position = bpos + size / 2;
		this.rectSelectImage.rectTransform.sizeDelta = new Vector2(size.x, size.y);

		Block[] blocks = EditManager.Instance.CurrentLayer.GetAllBlocks();
		foreach (var block in blocks) {
			Vector3 scrpos = Camera.main.WorldToScreenPoint(block.position);
			if (scrpos.x >= bpos.x && scrpos.y >= bpos.y &&
				scrpos.x <= epos.x && scrpos.y <= epos.y
			) {
				if (selection.IsSelected(block.position)) {
					selection.Remove(block.position);
				} else {
					selection.Add(block.position);
				}
			}
		}
	}

	// マウスカーソル位置のブロックを取得
	public bool GetCuesorPoint(out Vector3 position, out Vector3 normal) {
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, 100)) {
			normal = hit.normal;
			normal.y *= 0.5f;
			position = hit.point - normal * 0.5f;
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
