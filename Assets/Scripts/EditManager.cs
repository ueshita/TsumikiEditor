using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public partial class EditManager : MonoBehaviour
{
	public enum Tool {
		Pen, Eraser, Brush, Spuit,
		PointSelect, RectSelect
	}

	public static EditManager Instance {get; private set;}

	public string FilePath {get; private set;}
	public bool Changed {get; private set;}
	
	public List<EditLayer> Layers {get; private set;}
	public EditLayer CurrentLayer {get {return this.Layers[this.currentLayerIndex];}}
	int currentLayerIndex = 0;
	
	public EditSelection Selection {get; private set;}
	public EditGrid Grid {get; private set;}

	private Tool tool = Tool.Pen;
	private EditFiler filer = null;
	private List<Command> cmdlist = new List<Command>();
	private int cmdpos = 0;

	void Awake() {
		if (EditManager.Instance) {
			throw new Exception("EditManager is found.");
		}
		EditManager.Instance = this;
		
		this.filer = new EditFiler();
		this.Layers = new List<EditLayer>();
		
		var gridObj = new GameObject("Grid");
		gridObj.transform.parent = this.transform;
		this.Grid = gridObj.AddComponent<EditGrid>();
		
		var selectionObj = new GameObject("Selection");
		selectionObj.transform.parent = this.transform;
		this.Selection = selectionObj.AddComponent<EditSelection>();
	}
	void Start() {
		this.Reset();

		Load("TestData/house.tkd");
	}

	public Tool GetTool() {
		return this.tool;
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
		this.Selection.Clear();
	
		this.cmdlist.Clear();
		this.cmdpos = 0;
		this.Changed = false;
	}

	public void Reset() {
		this.Clear();
		this.AddLayer("Layer01");
		this.Layers[0].AddBlock(new CubeBlock());
	}

	public void SetTool(Tool tool) {
		this.tool = tool;
	}

	public void Load(string filePath) {
		this.Clear();
		this.filer.Load(filePath);
		this.FilePath = filePath;
		this.Changed = false;
	}
	
	public void Save(string filePath) {
		this.filer.Save(filePath);
		this.FilePath = filePath;
		this.Changed = false;
	}

	public void Undo() {
		if (this.cmdpos > 0 && this.cmdlist.Count > 0) {
			this.cmdpos--;
			this.cmdlist[this.cmdpos].Undo();
		}
	}
	
	public void Redo() {
		if (this.cmdpos < this.cmdlist.Count) {
			this.cmdlist[this.cmdpos].Redo();
			this.cmdpos++;
		}
	}
}
