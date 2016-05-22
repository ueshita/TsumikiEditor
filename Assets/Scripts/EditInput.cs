using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class EditInput : MonoBehaviour,
    IPointerClickHandler,
	IBeginDragHandler,
	IEndDragHandler,
    IDragHandler,
	IScrollHandler
{
	CameraController cameraController;	// 
	Text cursorPositionText;			// カーソル位置の表示
	Image selectionRect;				// 矩形選択の範囲の表示

	bool modifierControl = false;
	bool modifierShift = false;
	private Vector3 rectBasePoint;

	void Start() {
		this.cameraController = Camera.main.GetComponent<CameraController>();

		var rectSelectObj = new GameObject("RectSelect");
		rectSelectObj.transform.parent = this.transform;
		this.selectionRect = rectSelectObj.AddComponent<Image>();
		this.selectionRect.enabled = false;
		this.selectionRect.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

		this.cursorPositionText = GameObject.Find("CursorPositionText").GetComponent<Text>();
	}
	void Update() {
		this.UpdateInput();

		Vector3 point = EditManager.Instance.Cursor.point;
		this.cursorPositionText.text = String.Format(
			"(x:{0,4})(y:{1,4})(z:{2,4})", 
			Mathf.RoundToInt(point.x), 
			Mathf.RoundToInt(point.y), 
			Mathf.RoundToInt(point.z));
	}

	void OnGUI() {
		// Controll押しながらのキーはInputでは拾えないのでここで処理する
		Event e = Event.current;
		if (e.type == EventType.KeyUp && e.control) {
			// アンドゥ
			if (e.keyCode == KeyCode.Z) {
				EditManager.Instance.Undo();
			}
			// リドゥ
			if (e.keyCode == KeyCode.Y) {
				EditManager.Instance.Redo();
			}
			// 上書き保存
			if (e.keyCode == KeyCode.S) {
				GameObject.FindObjectOfType<EditMenu>().SaveButton_OnClick();
			}
		}
		if (e.type == EventType.keyDown && e.control) {
			// カット
			if (e.keyCode == KeyCode.X) {
				EditManager.Instance.Selector.CopyToClipboard();
				EditManager.Instance.RemoveBlocks(EditManager.Instance.Selector.GetSelectedBlocks());
			}
			// コピー
			if (e.keyCode == KeyCode.C) {
				EditManager.Instance.Selector.CopyToClipboard();
			}
			// ペースト
			if (e.keyCode == KeyCode.V) {
				EditManager.Instance.Selector.PasteFromClipboard();
			}
		}
	}
	void UpdateInput() {
		this.modifierControl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		this.modifierShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		
		if (this.modifierControl) {
		} else if (this.modifierShift) {
			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				EditManager.Instance.Selector.Expand(new Vector2(0.0f, 1.0f));
			}
			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				EditManager.Instance.Selector.Expand(new Vector2(0.0f, -1.0f));
			}
			if (Input.GetKeyDown(KeyCode.LeftArrow)) {
				EditManager.Instance.Selector.Expand(new Vector2(-1.0f, 0.0f));
			}
			if (Input.GetKeyDown(KeyCode.RightArrow)) {
				EditManager.Instance.Selector.Expand(new Vector2(1.0f, 0.0f));
			}
		} else {
			// 削除
			if (Input.GetKeyDown(KeyCode.Delete)) {
				EditManager.Instance.RemoveBlocks(EditManager.Instance.Selector.GetSelectedBlocks());
			}
			if (Input.GetKeyDown(KeyCode.Return)) {
				EditManager.Instance.Selector.ReleaseBlocks();
			}
			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				EditManager.Instance.Selector.Move(new Vector2(0.0f, 1.0f));
			}
			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				EditManager.Instance.Selector.Move(new Vector2(0.0f, -1.0f));
			}
			if (Input.GetKeyDown(KeyCode.LeftArrow)) {
				EditManager.Instance.Selector.Move(new Vector2(-1.0f, 0.0f));
			}
			if (Input.GetKeyDown(KeyCode.RightArrow)) {
				EditManager.Instance.Selector.Move(new Vector2(1.0f, 0.0f));
			}
			if (Input.GetKeyDown(KeyCode.A)) {
				if (EditManager.Instance.Selector.HasSelectedBlocks()) {
					EditManager.Instance.Selector.Rotate(1);
				} else {
					EditManager.Instance.Cursor.TurnBlock(1);
				}
			}
			if (Input.GetKeyDown(KeyCode.D)) {
				if (EditManager.Instance.Selector.HasSelectedBlocks()) {
					EditManager.Instance.Selector.Rotate(-1);
				} else {
					EditManager.Instance.Cursor.TurnBlock(-1);
				}
			}
		}
	}

	public void OnPointerClick(PointerEventData e) {
		if (e.button == 0) {
			// ブロックをクリック
			this.Block_OnClicked(
				EditManager.Instance.Cursor.point, 
				EditManager.Instance.Cursor.visible);
		}
	}
	
	// ドラッグ開始
	public void OnBeginDrag(PointerEventData e) {
		if (e.button == 0) {
			switch (EditManager.Instance.GetTool()) {
			case EditManager.Tool.RectSelector:
				this.BeginRectSelection(e.position);
				break;
			}
		} else {
			this.cameraController.OnBeginDrag((int)e.button, e.position);
		}
	}
	
	// ドラッグ終了
	public void OnEndDrag(PointerEventData e) {
		if (e.button == 0) {
			switch (EditManager.Instance.GetTool()) {
			case EditManager.Tool.RectSelector:
				this.EndRectSelection(e.position);
				break;
			}
		} else {
			this.cameraController.OnEndDrag((int)e.button, e.position);
		}
	}

	// ドラッグ中
	public void OnDrag(PointerEventData e) {
		if (e.button == 0) {
			switch (EditManager.Instance.GetTool()) {
			case EditManager.Tool.RectSelector:
				this.UpdateRectSelection(e.position);
				break;
			}
		} else {
			this.cameraController.OnDrag((int)e.button, e.position);
		}
	}

	// ホイールスクロール
	public void OnScroll(PointerEventData e) {
		this.cameraController.OnScroll(e.scrollDelta);
	}

	// ブロックをクリック
	public void Block_OnClicked(Vector3 point, bool cursorIsEnabled) {
		var selector = EditManager.Instance.Selector;

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Pen:
			if (cursorIsEnabled) {
				EditManager.Instance.AddBlock(0, point, 
					EditManager.Instance.Cursor.blockDirection);
			}
			break;
		case EditManager.Tool.Eraser:
			if (cursorIsEnabled) {
				EditManager.Instance.RemoveBlock(0, point);
			}
			break;
		case EditManager.Tool.Brush:
			if (cursorIsEnabled) {
				var block = EditManager.Instance.CurrentLayer.GetBlock(point);
				if (block != null) {
					EditManager.Instance.PaintBlock(block, EditManager.Instance.Cursor.panelDirection, TexturePalette.Instance.GetItem());
				}
			}
			break;
		case EditManager.Tool.Spuit:
			if (cursorIsEnabled) {
				var block = EditManager.Instance.CurrentLayer.GetBlock(point);
				if (block != null) {
					TexturePalette.Instance.SetItem(block.GetTextureChip(EditManager.Instance.Cursor.panelDirection));
				}
			}
			break;
		case EditManager.Tool.PointSelector:
			if (cursorIsEnabled) {
				selector.ReleaseBlocks();
				this.Block_OnSelect(point);
			} else {
				if (!this.modifierControl && !this.modifierShift) {
					selector.ReleaseBlocks();
					selector.Clear();
				}
			}
			break;
		case EditManager.Tool.RectSelector:
			if (!this.modifierControl && !this.modifierShift) {
				selector.ReleaseBlocks();
				selector.Clear();
			}
			break;
		}
	}

	public void Block_OnSelect(Vector3 point) {
		var selector = EditManager.Instance.Selector;
		if (!this.modifierControl && !this.modifierShift) {
			selector.Clear();
		}

		if (this.modifierShift && selector.Count > 0) {
			// Shift押しながら2つ目以降を選択した場合、範囲選択をする
			selector.SelectRange(selector.LastPosition, point);
		} else {
			// 通常の選択
			if (selector.IsSelected(point)) {
				selector.Remove(point);
			} else {
				selector.Add(point);
			}
		}
	}
	
	// 矩形選択開始
	public void BeginRectSelection(Vector3 point) {
		EditManager.Instance.Selector.ReleaseBlocks();
		EditManager.Instance.Selector.Backup();
		
		this.rectBasePoint = point;

		this.selectionRect.rectTransform.sizeDelta = Vector2.zero;
		this.selectionRect.enabled = true;
	}
	// 矩形選択終了
	public void EndRectSelection(Vector3 point) {
		this.UpdateRectSelection(point);

		this.selectionRect.enabled = false;
	}
	// 矩形選択中
	public void UpdateRectSelection(Vector3 point) {
		var selector = EditManager.Instance.Selector;
		
		selector.Clear();
		selector.Restore();

		Vector3 bpos = this.rectBasePoint;
		Vector3 epos = point;
		EditUtil.MinMaxElements(ref bpos, ref epos);
		
		Vector3 size = epos - bpos;
		
		this.selectionRect.rectTransform.position = bpos + size / 2;
		this.selectionRect.rectTransform.sizeDelta = new Vector2(size.x, size.y);

		Block[] blocks = EditManager.Instance.CurrentLayer.GetAllBlocks();
		foreach (var block in blocks) {
			Vector3 scrpos = Camera.main.WorldToScreenPoint(block.position);
			if (scrpos.x >= bpos.x && scrpos.y >= bpos.y &&
				scrpos.x <= epos.x && scrpos.y <= epos.y
			) {
				if (selector.IsSelected(block.position)) {
					selector.Remove(block.position);
				} else {
					selector.Add(block.position);
				}
			}
		}
	}
}
