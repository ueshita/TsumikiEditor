using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public partial class EditManager : MonoBehaviour
{
	public enum Tool {
		Block, Eraser, Brush, Spuit,
		PointSelector, RoutePath, Model, MetaInfo
	}

	public enum CoordinateSystem {
		LeftHanded,
		RightHanded,
	}

	public static EditManager Instance {get; private set;}

	public List<EditLayer> Layers {get; private set;}
	public EditLayer CurrentLayer {get {return this.Layers[this.currentLayerIndex];}}
	private int currentLayerIndex = 0;
	
	public Selector Selector {get; private set;}
	public Grid Grid {get; private set;}
	public EditCursor Cursor {get; private set;}
	public RoutePath RoutePath {get; private set;}
	public ToolMenu ToolMenu {get; private set;}
	public ModelProperties ModelProperties {get; private set;}
	public MetaInfo MetaInfo {get; private set;}
	public QuitDialog QuitDialog {get; private set;}
	public LayerListView LayerListView {get; private set;}

	private CoordinateSystem coordinateSystem = CoordinateSystem.RightHanded;
	private Tool tool = Tool.Block;
	private string toolBlock = "cube";
	private string toolModel = "tree";
	private int toolChip = 0;
	private List<Command> cmdlist = new List<Command>();
	private int cmdpos = 0;
	public bool hasUnsavedData {get; private set;}

	private GameObject blockPaletteListView;
	private GameObject texturePaletteListView;
	private GameObject modelPaletteListView;

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
		this.RoutePath = routePathObj.AddComponent<RoutePath>();
		routePathObj.SetActive(false);

		this.ToolMenu = GameObject.FindObjectOfType<ToolMenu>();
		this.ModelProperties = GameObject.FindObjectOfType<ModelProperties>();
		this.MetaInfo = GameObject.FindObjectOfType<MetaInfo>();
		
		this.QuitDialog = GameObject.FindObjectOfType<QuitDialog>();
		this.QuitDialog.Close();

		this.LayerListView = GameObject.FindObjectOfType<LayerListView>();

		BlockShape.LoadData();
		ModelShape.LoadData();
	}

	void OnApplicationQuit() {
		if (Application.isEditor) {
			return;
		}

		if (this.hasUnsavedData) {
			if (!this.QuitDialog.IsOpened()) {
				this.QuitDialog.Open((result) => {
					if (result) {
						this.hasUnsavedData = false;
						Application.Quit();
					}
				});
			}
			Application.CancelQuit();
		}
	}
	
	public void OnDataSaved() {
		this.hasUnsavedData = false;
		EditManager.Instance.UpdateWindowTitle();
	}

	public void OnDataDiscarded() {
		this.hasUnsavedData = false;
		EditManager.Instance.UpdateWindowTitle();
	}

	public void OnDataChanged() {
		bool isChanged = !this.hasUnsavedData;
		this.hasUnsavedData = true;
		if (isChanged) EditManager.Instance.UpdateWindowTitle();
	}

	public void UpdateWindowTitle() {
		string filePath = FileManager.currentFilePath;
		string fileName;
		if (String.IsNullOrEmpty(filePath)) {
			fileName = "NewMap";
		} else {
			fileName = Path.GetFileName(filePath);
			if (this.hasUnsavedData) {
				fileName += " *";
			}
		}
		
		WindowControl.SetWindowTitle(fileName + " - " + Application.productName);
	}

	void Start() {
		this.Reset();
		
		this.blockPaletteListView = GameObject.Find("BlockPaletteListView");
		this.texturePaletteListView = GameObject.Find("TexturePaletteListView");
		this.modelPaletteListView = GameObject.Find("ModelPaletteListView");
		this.SetTool(Tool.Block);

		//FileManager.Load("TestData/test02.tkd");
		//FileManager.Load("TestData/test01.tkd");
	}
	
	public void SetTool(Tool tool) {
		this.tool = tool;

		this.blockPaletteListView.SetActive(false);
		this.texturePaletteListView.SetActive(false);
		this.modelPaletteListView.SetActive(false);

		// カーソルにツールをセットする
		switch (this.tool) {
		case Tool.Block:
			this.Cursor.SetBlock(this.toolBlock);
			this.blockPaletteListView.SetActive(true);
			break;
		case Tool.Eraser:
		case Tool.PointSelector:
			this.Cursor.SetBlock();
			break;
		case Tool.Brush:
			this.Cursor.SetPanel();
			this.texturePaletteListView.SetActive(true);
			break;
		case Tool.Spuit:
		case Tool.RoutePath:
			this.Cursor.SetPanel();
			break;
		case Tool.Model:
			this.Cursor.SetModel(this.toolModel);
			this.modelPaletteListView.SetActive(true);
			break;
		case Tool.MetaInfo:
			this.Cursor.SetPanel();
			break;
		}
		
		// 選択以外のツールならセレクタをクリアする
		switch (this.tool) {
		case Tool.PointSelector:
			break;
		default:
			this.Selector.ReleaseBlocks();
			this.Selector.Clear();
			break;
		}

		EditManager.Instance.MetaInfo.gameObject.SetActive(false);
		this.RoutePath.isSelected = false;

		if (this.tool == Tool.RoutePath) {
			this.RoutePath.SetEnabled(true);
		} else {
			this.RoutePath.SetEnabled(false);
		}
	}

	public Tool GetTool() {
		return this.tool;
	}

	public void SetToolBlock(string blockName) {
		this.toolBlock = blockName;
		if (this.tool == Tool.Block) {
			this.Cursor.SetBlock(this.toolBlock);
		}
	}
	
	public void SetToolModel(string modelName) {
		this.toolModel = modelName;
		if (this.tool == Tool.Model) {
			this.Cursor.SetModel(this.toolModel);
		}
	}

	public string GetToolBlock() {
		return this.toolBlock;
	}
	
	public void SetToolChip(int chipIndex) {
		this.toolChip = chipIndex;
	}

	public void SetCurrentLayerIndex(int layerIndex) {
		this.currentLayerIndex = layerIndex;
		this.Selector.SetCurrentLayer(this.CurrentLayer);
	}

	public EditLayer FindLayer(string layerName) {
		foreach (var layer in this.Layers) {
			if (layer.gameObject.name == layerName) {
				return layer;
			}
		}
		return this.Layers[0];
	}

	public EditLayer AddLayer(string name, string materialName, string textureName) {
		var obj = new GameObject(name);
		var layer = obj.AddComponent<EditLayer>();
		layer.SetMaterial(materialName, textureName);
		this.Layers.Add(layer);
		this.LayerListView.UpdateView();
		return layer;
	}

	public void RemoveLayer(EditLayer layer) {
		this.Layers.Remove(layer);
		GameObject.Destroy(layer.gameObject);
		this.LayerListView.UpdateView();
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
		FileManager.Reset();
		this.AddLayer("Layer-Blocks", "BlockMaterial", "rbn01");
		this.AddLayer("Layer-Water", "WaterMaterial", null);
		this.OnDataDiscarded();
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
