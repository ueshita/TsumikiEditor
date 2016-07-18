using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public partial class EditManager : MonoBehaviour
{
	public enum Tool {
		Pen, Eraser, Brush, Spuit,
		PointSelector, RectSelector, 
		RoutePath
	}

	public enum CoordinateSystem {
		LeftHanded,
		RightHanded,
	}

	public static EditManager Instance {get; private set;}

	public List<EditLayer> Layers {get; private set;}
	public EditLayer CurrentLayer {get {return this.Layers[this.currentLayerIndex];}}
	int currentLayerIndex = 0;
	
	public Selector Selector {get; private set;}
	public Grid Grid {get; private set;}
	public EditCursor Cursor {get; private set;}
	public RoutePath routePath {get; private set;}

	private CoordinateSystem coordinateSystem = CoordinateSystem.RightHanded;
	private Tool tool = Tool.Pen;
	private string toolBlock = "cube";
	private int toolChip = 0;
	private List<Command> cmdlist = new List<Command>();
	private int cmdpos = 0;

	void Awake() {
		if (EditManager.Instance) {
			throw new Exception("EditManager is found.");
		}
		EditManager.Instance = this;
		
		this.Layers = new List<EditLayer>();
		
		var gridObj = new GameObject("Grid");
		gridObj.transform.parent = this.transform;
		this.Grid = gridObj.AddComponent<Grid>();
		
		var cursorObj = new GameObject("EditCursor");
		cursorObj.transform.parent = EditManager.Instance.transform;
		this.Cursor = cursorObj.AddComponent<EditCursor>();
		
		var selectionObj = new GameObject("Selector");
		selectionObj.transform.parent = this.transform;
		this.Selector = selectionObj.AddComponent<Selector>();
		
		var routePathObj = new GameObject("RoutePath");
		selectionObj.transform.parent = this.transform;
		this.routePath = routePathObj.AddComponent<RoutePath>();
		routePathObj.SetActive(false);
	}

	void Start() {
		this.Reset();

		//FileManager.Load("TestData/test02.tkd");
		//FileManager.Load("TestData/test01.tkd");
	}
	
	public void SetTool(Tool tool) {
		this.tool = tool;

		// カーソルにツールをセットする
		switch (this.tool) {
		case Tool.Pen:
			this.Cursor.SetBlock(this.toolBlock);
			break;
		case Tool.Eraser:
		case Tool.PointSelector:
			this.Cursor.SetBlock();
			break;
		case Tool.Brush:
		case Tool.Spuit:
		case Tool.RoutePath:
			this.Cursor.SetPanel();
			break;
		}
		
		// 選択以外のツールならセレクタをクリアする
		switch (this.tool) {
		case Tool.PointSelector:
		case Tool.RectSelector:
			break;
		default:
			this.Selector.ReleaseBlocks();
			this.Selector.Clear();
			break;
		}

		if (this.tool == Tool.RoutePath) {
			this.routePath.SetEnabled(true);
		} else {
			this.routePath.SetEnabled(false);
		}
	}
	public Tool GetTool() {
		return this.tool;
	}

	public void SetToolBlock(string blockName) {
		this.toolBlock = blockName;
		if (this.tool == Tool.Pen) {
			this.Cursor.SetBlock(this.toolBlock);
		}
	}

	public string GetToolBlock() {
		return this.toolBlock;
	}
	
	public void SetToolChip(int chipIndex) {
		this.toolChip = chipIndex;
	}

	public EditLayer AddLayer(string name) {
		var obj = new GameObject(name);
		var layer = obj.AddComponent<EditLayer>();
		this.Layers.Add(layer);
		return layer;
	}

	public void RemoveLayer(EditLayer layer) {
		this.Layers.Remove(layer);
		GameObject.Destroy(layer.gameObject);
	}

	public void Clear() {
		foreach (var layer in this.Layers) {
			GameObject.Destroy(layer.gameObject);
		}
		this.Layers.Clear();
		this.Selector.Clear();
	
		this.cmdlist.Clear();
		this.cmdpos = 0;
	}

	public void Reset() {
		this.Clear();
		this.AddLayer("Layer01");
	}
	
	public bool IsLeftHanded() {
		return this.coordinateSystem == CoordinateSystem.LeftHanded;
	}
	public bool IsRightHanded() {
		return this.coordinateSystem == CoordinateSystem.RightHanded;
	}
	public Vector3 ToWorldCoordinate(Vector3 position) {
		if (this.IsRightHanded()) {
			position.z = -position.z;
			return position;
		} else {
			return position;
		}
	}
}
