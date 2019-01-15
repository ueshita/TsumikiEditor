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
	Text cursorInfo;					// カーソル位置の表示
	Text perfInfo;						// パフォーマンス情報の表示
	Image selectionRect;				// 矩形選択の範囲の表示

	bool dragging = false;
	bool modifierControl = false;
	bool modifierShift = false;

	void Start() {
		this.cameraController = Camera.main.GetComponent<CameraController>();

		var rectSelectObj = new GameObject("RectSelect");
		rectSelectObj.transform.parent = this.transform;
		this.selectionRect = rectSelectObj.AddComponent<Image>();
		this.selectionRect.enabled = false;
		this.selectionRect.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

		this.cursorInfo = GameObject.Find("CursorInfo").GetComponent<Text>();
		this.perfInfo = GameObject.Find("PerfInfo").GetComponent<Text>();
	}
	void Update() {
		//Todo:Dragイベントは反応が遅い！Inputで実装する！

		this.UpdateInput();

		Vector3 point = EditManager.Instance.Cursor.point;
		point = EditManager.Instance.ToWorldCoordinate(point);
		point.y *= 2;
		this.cursorInfo.text = String.Format(
			"(x:{0,4})(y:{1,4})(z:{2,4})", 
			Mathf.RoundToInt(point.x),
			Mathf.RoundToInt(point.y),
			Mathf.RoundToInt(point.z));

		int numVertices = 0, numTriangles = 0;
		foreach (var layer in EditManager.Instance.Layers) {
			numVertices += layer.NumVertices;
			numTriangles += layer.NumTriangles;
		}
		this.perfInfo.text = String.Format(
			"{0} Verts\n{1} Polys", numVertices, numTriangles);
	}

	void OnGUI() {
		if (EditManager.Instance.ModelProperties.IsFocused() ||
			EditManager.Instance.MetaInfo.IsFocused()) {
			return;
		}

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
				FileManager.Save();
			}
			// 全選択
			if (e.keyCode == KeyCode.A) {
				EditManager.Instance.Selector.Clear();
				foreach (var block in EditManager.Instance.CurrentLayer.GetAllBlocks()) {
					EditManager.Instance.Selector.Add(block.position);
				}
			}
		}
		if (e.type == EventType.KeyDown && e.control) {
			// カット
			if (e.keyCode == KeyCode.X) {
				EditManager.Instance.Selector.CopyToClipboard();
				EditManager.Instance.RemoveObjects(
					EditManager.Instance.Selector.GetSelectedBlocks(),
					EditManager.Instance.Selector.GetSelectedModels());
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
		// ドラッグ操作
		if (this.dragging) {
			if (Input.GetMouseButton(0)) {
			} else if (Input.GetMouseButton(1)) {
				this.cameraController.OnDrag(1, Input.mousePosition);
			} else if (Input.GetMouseButton(2)) {
				this.cameraController.OnDrag(2, Input.mousePosition);
			}
		}
		
		if (EditManager.Instance.ModelProperties.IsFocused() ||
			EditManager.Instance.MetaInfo.IsFocused()) {
			return;
		}
		
		var selector = EditManager.Instance.Selector;
		var cursor = EditManager.Instance.Cursor;

		this.modifierControl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		this.modifierShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		
		if (this.modifierControl) {
		} else if (this.modifierShift) {
			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				selector.Expand(new Vector2(0.0f, 1.0f));
			}
			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				selector.Expand(new Vector2(0.0f, -1.0f));
			}
			if (Input.GetKeyDown(KeyCode.LeftArrow)) {
				selector.Expand(new Vector2(-1.0f, 0.0f));
			}
			if (Input.GetKeyDown(KeyCode.RightArrow)) {
				selector.Expand(new Vector2(1.0f, 0.0f));
			}
		} else {
			// 削除
			if (Input.GetKeyDown(KeyCode.Delete)) {
				EditManager.Instance.RemoveObjects(
					selector.GetSelectedBlocks(),
					selector.GetSelectedModels());
			}
			// 確定
			if (Input.GetKeyDown(KeyCode.Return)) {
				selector.ReleaseBlocks();
			}
			// 移動
			if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
				selector.Move(new Vector2(0.0f, 1.0f));
			}
			if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
				selector.Move(new Vector2(0.0f, -1.0f));
			}
			if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
				selector.Move(new Vector2(-1.0f, 0.0f));
			}
			if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
				selector.Move(new Vector2(1.0f, 0.0f));
			}
			// 回転
			if (Input.GetKeyDown(KeyCode.Q)) {
				if (selector.HasSelectedObjects()) {
					selector.Rotate(-1);
				} else {
					cursor.TurnBlock(1);
				}
			}
			if (Input.GetKeyDown(KeyCode.E)) {
				if (selector.HasSelectedObjects()) {
					selector.Rotate(1);
				} else {
					cursor.TurnBlock(-1);
				}
			}
			// ツール指定
			for (int i = 0; i <= 9; i++) {
				if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
					EditManager.Instance.ToolMenu.SetTool(i);
				}
			}
		}
	}

	public void OnPointerClick(PointerEventData e) {
		if (e.button == 0) {
			EditManager.Instance.Cursor.Update();
			// ブロックをクリック
			this.OnObjectClicked();
		}
	}
	
	// ドラッグ開始
	public void OnBeginDrag(PointerEventData e) {
		this.dragging = true;
		if (e.button == 0) {
			this.OnObjectBeginDrag();
		} else {
			this.cameraController.OnBeginDrag((int)e.button, e.position);
		}
	}
	
	// ドラッグ終了
	public void OnEndDrag(PointerEventData e) {
		this.dragging = false;
		if (e.button == 0) {
			this.OnObjectEndDrag();
		} else {
			this.cameraController.OnEndDrag((int)e.button, e.position);
		}
	}

	// ドラッグ中
	public void OnDrag(PointerEventData e) {
		if (e.button == 0) {
			this.OnObjectDrag();
		} else {
			this.cameraController.OnDrag((int)e.button, e.position);
		}
	}

	// ホイールスクロール
	public void OnScroll(PointerEventData e) {
		this.cameraController.OnScroll(e.scrollDelta);
	}

	// オブジェクトをクリック
	public void OnObjectClicked() {
		var selector = EditManager.Instance.Selector;
		var cursor = EditManager.Instance.Cursor;

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Block:
			if (cursor.visible) {
				// 1つ手前にブロックを配置
				EditManager.Instance.AddBlock(cursor.point, 
					EditManager.Instance.Cursor.blockDirection);
			}
			break;
		case EditManager.Tool.Eraser:
			// 選択されたオブジェクトを消す
			if (cursor.visible) {
				if (cursor.block != null) {
					EditManager.Instance.RemoveBlock(cursor.point);
				} else if (cursor.model != null) {
					EditManager.Instance.RemoveModel(cursor.model);
				}
			}
			break;
		case EditManager.Tool.Brush:
			if (cursor.visible) {
				// 対象のブロックを塗る(テクスチャ指定)
				var block = EditManager.Instance.CurrentLayer.GetBlock(cursor.point);
				if (block != null) {
					EditManager.Instance.PaintBlock(block, 
						cursor.panelDirection, cursor.objectSelected,
						TexturePalette.Instance.GetItem());
				}
			}
			break;
		case EditManager.Tool.Spuit:
			if (cursor.visible) {
				// 対象のブロックのテクスチャを取得
				var block = EditManager.Instance.CurrentLayer.GetBlock(cursor.point);
				if (block != null) {
					TexturePalette.Instance.SetItem(block.GetTextureChip(cursor.panelDirection, cursor.objectSelected));
				}
			}
			break;
		case EditManager.Tool.PointSelector:
			// 選択モード
			if (cursor.visible) {
				// キャプチャ状態のオブジェクトを解放
				selector.ReleaseBlocks();
				
				// CtrlもShiftも押さずにクリックしたら選択解除
				if (!this.modifierControl && !this.modifierShift) {
					selector.Clear();
				}

				if (this.modifierShift && selector.Count > 0) {
					// Shift押しながら2つ目以降を選択した場合、範囲選択をする
					selector.SelectRange(selector.LastPosition, cursor.point);
				} else if (cursor.model != null) {
					if (selector.IsSelected(cursor.model)) {
						selector.Remove(cursor.model);
					} else {
						selector.Add(cursor.model);
					}
				} else {
					// 通常の選択
					if (selector.IsSelected(cursor.point)) {
						selector.Remove(cursor.point);
					} else {
						selector.Add(cursor.point);
					}
				}
			} else {
				// 何もないところをCtrlもShiftも押さずにクリックしたら選択解除
				if (!this.modifierControl && !this.modifierShift) {
					selector.ReleaseBlocks();
					selector.Clear();
				}
			}
			break;
		case EditManager.Tool.RoutePath:
			if (cursor.visible) {
				var routePath = EditManager.Instance.RoutePath;
				var block = EditManager.Instance.CurrentLayer.GetBlock(cursor.point);
				if (routePath.isSelected) {
					// パス確定
					if (routePath.ContainsPath(routePath.selectedPosition, block.position)) {
						// 存在していたら消す
						EditManager.Instance.RemoveRoutePath(routePath.selectedPosition, block.position);
						routePath.RemovePath(routePath.selectedPosition, block.position);
					} else if (routePath.CanAddPath(routePath.selectedPosition, block.position)) {
						// 存在していなかったら追加
						EditManager.Instance.AddRoutePath(routePath.selectedPosition, block.position);
					} else if (routePath.selectedPosition == block.position) {
						// 同じ位置の場合は侵入フラグをトグルする
						EditManager.Instance.SetEnterable(block, !block.enterable);
					}
					routePath.isSelected = false;
					EditManager.Instance.Selector.Clear();
				} else {
					// パスの開始ブロックを選択
					EditManager.Instance.Selector.Add(block.position);
					routePath.selectedPosition = block.position;
					routePath.isSelected = true;
				}
			}
			break;
		case EditManager.Tool.Model:
			if (cursor.visible) {
				var foundModel = EditManager.Instance.CurrentLayer.GetModel(cursor.point);
				if (foundModel == null) {
					// モデル配置
					EditManager.Instance.AddModel(cursor.point,
						(int)EditUtil.DirectionToAngle(EditManager.Instance.Cursor.blockDirection));
				}
			}
			break;
		case EditManager.Tool.MetaInfo:
			EditManager.Instance.Selector.Clear();
			EditManager.Instance.MetaInfo.gameObject.SetActive(false);
			if (cursor.visible) {
				var block = EditManager.Instance.CurrentLayer.GetBlock(cursor.point);
				if (block != null) {
					// メタ情報を記入するブロックを選択
					EditManager.Instance.Selector.Add(block.position);
					// メタ情報記入のダイアログを表示
					EditManager.Instance.MetaInfo.gameObject.SetActive(true);
					EditManager.Instance.MetaInfo.SetBlock(block);
				}
			}
			break;
		}
	}
	
	
	public void OnObjectBeginDrag() {
		var selector = EditManager.Instance.Selector;
		var cursor = EditManager.Instance.Cursor;

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Brush:
			EditManager.Instance.BeginCommandGroup();
			break;
		}
	}
	
	public void OnObjectEndDrag() {
		var selector = EditManager.Instance.Selector;
		var cursor = EditManager.Instance.Cursor;

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Brush:
			EditManager.Instance.EndCommandGroup();
			break;
		}
	}

	// オブジェクトをドラッグ
	public void OnObjectDrag() {
		var selector = EditManager.Instance.Selector;
		var cursor = EditManager.Instance.Cursor;

		switch (EditManager.Instance.GetTool()) {
		case EditManager.Tool.Brush:
			if (cursor.visible) {
				// 対象のブロックを塗る(テクスチャ指定)
				var block = EditManager.Instance.CurrentLayer.GetBlock(cursor.point);
				if (block != null) {
					EditManager.Instance.PaintBlock(block, 
						cursor.panelDirection, cursor.objectSelected,
						TexturePalette.Instance.GetItem());
				}
			}
			break;
		}
	}
}
