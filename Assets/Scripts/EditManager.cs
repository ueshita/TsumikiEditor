using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public partial class EditManager : MonoBehaviour
{
	public enum Tool {
		Pen, Eraser, Brush, Spuit,
		PointSelector, RectSelector
	}

	public static EditManager Instance {get; private set;}

	public List<EditLayer> Layers {get; private set;}
	public EditLayer CurrentLayer {get {return this.Layers[this.currentLayerIndex];}}
	int currentLayerIndex = 0;
	
	public EditSelector Selector {get; private set;}
	public EditGrid Grid {get; private set;}
	public EditCursor Cursor {get; private set;}

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
		this.Grid = gridObj.AddComponent<EditGrid>();
		
		var cursorObj = new GameObject("EditCursor");
		cursorObj.transform.parent = EditManager.Instance.transform;
		this.Cursor = cursorObj.AddComponent<EditCursor>();
		
		var selectionObj = new GameObject("Selector");
		selectionObj.transform.parent = this.transform;
		this.Selector = selectionObj.AddComponent<EditSelector>();
	}
	void Start() {
		this.Reset();

		FileManager.Load("TestData/kaidan.tkd");
		//FileManager.Load("TestData/test.tkd");
	}
	
	public void SetTool(Tool tool) {
		this.tool = tool;

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
			this.Cursor.SetPanel();
			break;
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
}
