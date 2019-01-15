using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;

public class Selector : MonoBehaviour
{
	Guide selectedBlockGuide;
	BlockGroup selectedBlocks = new BlockGroup();
	Guide selectedModelGuide;
	ModelGroup selectedModels = new ModelGroup();

	CaptureMode captureMode;
	EditLayer capturedLayer;
	Block[] capturedBlocks = null;
	Model[] capturedModels = null;
	Vector3 capturedCenter = Vector3.zero;
	int capturedRotation;
	bool dirtyMesh = false;
	
	public Vector3 LastPosition {get; private set;}
	public int Count {get {
		return this.selectedBlocks.GetBlockCount() + this.selectedModels.GetModelCount();
	}}

	public enum ColorMode {
		Selection,
		Captured,
	};
	
	public enum CaptureMode {
		Moving,
		Paste,
	};

	void Awake() {
		var blockGuideObj = new GameObject();
		blockGuideObj.name = "SelectedBlocks";
		blockGuideObj.transform.parent = this.transform;
		this.selectedBlockGuide = blockGuideObj.AddComponent<Guide>();
		
		var modelObj = new GameObject();
		modelObj.name = "SelectedModels";
		modelObj.transform.parent = this.transform;
		this.selectedModelGuide = modelObj.AddComponent<Guide>();

		var captureObj = new GameObject();
		captureObj.name = "Captured";
		captureObj.transform.parent = this.transform;
		this.capturedLayer = captureObj.AddComponent<EditLayer>();
		
		this.SetColorMode(ColorMode.Selection);
	}

	// 色をセット
	private void SetColorMode(ColorMode colorMode) {
		switch (colorMode) {
		case ColorMode.Selection:
			this.selectedBlockGuide.SetColor(new Color(1.0f, 1.0f, 1.0f), new Color(0.5f, 1.0f, 0.5f));
			this.selectedModelGuide.SetColor(new Color(1.0f, 1.0f, 1.0f), new Color(0.5f, 1.0f, 0.5f));
			break;
		case ColorMode.Captured:
			this.selectedBlockGuide.SetColor(new Color(1.0f, 1.0f, 1.0f), new Color(1.0f, 0.5f, 1.0f));
			this.selectedModelGuide.SetColor(new Color(1.0f, 1.0f, 1.0f), new Color(1.0f, 0.5f, 1.0f));
			break;
		}
	}

	void LateUpdate() {
		if (this.dirtyMesh) {
			this.selectedBlocks.UpdateMesh();
			this.selectedBlockGuide.SetMesh(this.selectedBlocks.GetGuideMesh(), this.selectedBlocks.GetWireMesh(), true);
			this.selectedModels.UpdateMesh();
			this.selectedModelGuide.SetMesh(this.selectedModels.GetGuideMesh(), this.selectedModels.GetWireMesh(), true);
			this.dirtyMesh = false;
		}
	}

	// 現在のレイヤーを設定
	public void SetCurrentLayer(EditLayer layer) {
		var originalRenderer = layer.GetComponent<MeshRenderer>();
		if (originalRenderer == null) {
			Debug.LogError("Renderer not found in Original Layer.");
			return;
		}
		var captureRenderer = this.capturedLayer.GetComponent<MeshRenderer>();
		if (captureRenderer == null) {
			Debug.LogError("Renderer not found in Capture Layer.");
			return;
		}
		captureRenderer.material = originalRenderer.material;
	}

	// 変更したことを知らせる
	public void SetDirty() {
		this.dirtyMesh = true;
	}

	public bool IsSelected(Vector3 position) {
		return this.selectedBlocks.GetBlock(position) != null;
	}

	public bool IsSelected(Model model) {
		return this.selectedModels.Contains(model);
	}

	public void Set(Block[] blocks, Model[] models) {
		this.Clear();
		if (blocks != null) {
			foreach (var block in blocks) {
				this.Add(block.position);
			}
		}
		if (models != null) {
			foreach (var model in models) {
				this.Add(model);
			}
		}
	}

	public void Add(Vector3 position) {
		var block = new Block(position, BlockDirection.Zplus, BlockShape.Find("cube"));
		if (block == null) {return;}
		this.selectedBlocks.AddBlock(block);
		this.LastPosition = position;
		this.dirtyMesh = true;
	}

	public void Remove(Vector3 position) {
		var block = this.selectedBlocks.GetBlock(position);
		if (block == null) {return;}
		this.selectedBlocks.RemoveBlock(block);
		this.LastPosition = position;
		this.dirtyMesh = true;
	}
	
	public void Add(Model model) {
		if (model == null) return;
		this.selectedModels.AddModel(model);
		this.LastPosition = model.position;
		EditManager.Instance.ModelProperties.SetModels(this.GetSelectedModels());
		this.dirtyMesh = true;
	}
	
	public void Remove(Model model) {
		if (model == null) return;
		this.selectedModels.RemoveModel(model, false);
		this.LastPosition = model.position;
		EditManager.Instance.ModelProperties.SetModels(this.GetSelectedModels());
		this.dirtyMesh = true;
	}

	public bool HasSelectedObjects() {
		return this.selectedBlocks.GetNumBlocks() > 0 ||
			this.selectedModels.GetNumModels() > 0;
	}

	public Block[] GetSelectedBlocks() {
		Block[] points = this.selectedBlocks.GetAllBlocks();
		List<Block> blocks = new List<Block>();
		foreach (var point in points) {
			Block block = EditManager.Instance.CurrentLayer.GetBlock(point.position);
			if (block != null) {
				blocks.Add(block);
			}
		}
		return blocks.ToArray();
	}
	
	public Model[] GetSelectedModels() {
		return this.selectedModels.GetAllModels();
	}

	public void Clear(bool backup = false) {
		this.selectedBlocks.Clear();
		this.selectedModels.Clear();
		this.transform.position = Vector3.zero;
		this.ReleaseBlocks();
		EditManager.Instance.ModelProperties.SetModels(this.GetSelectedModels());
		this.dirtyMesh = true;
	}
	
	public void SelectRange(Vector3 begin, Vector3 end) {
		Vector3i bpos = new Vector3i(begin.x, begin.y * 2.0f, begin.z);
		Vector3i epos = new Vector3i(end.x, end.y * 2.0f, end.z);
		EditUtil.MinMaxElements(ref bpos, ref epos);
		for (int z = bpos.z; z <= epos.z; z++) {
			for (int y = bpos.y; y <= epos.y; y++) {
				for (int x = bpos.x; x <= epos.x; x++) {
					Vector3 curpos = new Vector3(x, y * 0.5f, z);
					if (!this.IsSelected(curpos)) {
						this.Add(curpos);
						this.Add(EditManager.Instance.CurrentLayer.GetModel(curpos));
					} else {
						this.Remove(EditManager.Instance.CurrentLayer.GetModel(curpos));
					}
				}
			}
		}
		this.LastPosition = end;
	}

	// 選択ブロックを移動
	public void Move(Vector2 screenDir) {
		if (!this.HasCapturedBlocks()) {
			this.CaptureBlocks(CaptureMode.Moving, this.GetSelectedBlocks(), this.GetSelectedModels());
		}

		Vector3 up, right;
		EditUtil.ScreenDirToWorldDir(out up, out right);
		this.transform.position = this.transform.position + up * screenDir.y + right * screenDir.x;
	}
	
	// 選択ブロックを回転
	public void Rotate(int value) {
		if (!this.HasCapturedBlocks()) {
			this.CaptureBlocks(CaptureMode.Moving, this.GetSelectedBlocks(), this.GetSelectedModels());
		}

		this.capturedRotation += value;
		while (this.capturedRotation < 0) this.capturedRotation += 4;
		while (this.capturedRotation > 3) this.capturedRotation -= 4;
		float angle = this.capturedRotation * 90.0f;
		this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
	}
	
	// 選択幅を拡張
	public void Expand(Vector2 screenDir) {
		if (this.HasCapturedBlocks()) {
			return;
		}

		Vector3 up, right;
		EditUtil.ScreenDirToWorldDir(out up, out right);
		Vector3 expandVector =  up * screenDir.y + right * screenDir.x;

		foreach (var point in this.selectedBlocks.GetAllBlocks()) {
			Vector3 newPosition = point.position + expandVector;
			var target = this.selectedBlocks.GetBlock(newPosition);
			if (target == null) {
				this.Add(newPosition);
			}
		}
	}
	
	// ブロックをキャプチャしているか
	public bool HasCapturedBlocks() {
		return this.capturedBlocks != null;
	}

	// キャプチャしているブロックの取得
	public Block[] GetCapturedBlocks() {
		Block[] points = this.selectedBlocks.GetAllBlocks();
		List<Block> blocks = new List<Block>();
		foreach (var point in points) {
			Block block = EditManager.Instance.CurrentLayer.GetBlock(point.position);
			if (block != null) {
				blocks.Add(block);
			}
		}
		return blocks.ToArray();
	}

	// 選択ブロックをキャプチャする
	public void CaptureBlocks(CaptureMode mode, Block[] blocks, Model[] models) {
		if (this.HasCapturedBlocks()) {
			return;
		}
		
		this.captureMode = mode;

		if (mode == CaptureMode.Moving) {
			// 一旦実在レイヤーから削除
			EditManager.Instance.CurrentLayer.RemoveBlocks(blocks);
		}
		
		if (blocks.Length == 0 && models.Length == 0) {
			return;
		}

		// キャプチャレイヤーのマテリアルをセット
		this.capturedLayer.SetMaterial(EditManager.Instance.CurrentLayer.GetMaterial());

		Vector3 sumPosition = Vector3.zero;

		// キャプチャレイヤーに放り込む
		foreach (var block in blocks) {
			this.capturedLayer.AddBlock(block);
			sumPosition += block.position;
		}
		foreach (var model in models) {
			this.capturedLayer.AddModel(model);
			sumPosition += model.position;
		}

		// 中心位置を計算する
		this.capturedCenter = sumPosition / (blocks.Length + models.Length);
		capturedCenter.x = Mathf.Round(capturedCenter.x);
		capturedCenter.y = Mathf.Round(capturedCenter.y * 2.0f) * 0.5f;
		capturedCenter.z = Mathf.Round(capturedCenter.z);
		
		this.capturedBlocks = blocks;
		this.capturedModels = models;
		this.capturedLayer.transform.position = -this.capturedCenter;
		this.selectedBlockGuide.transform.position = -this.capturedCenter;
		this.selectedModelGuide.transform.position = -this.capturedCenter;
		this.transform.position = this.capturedCenter;
		
		// キャプチャ対象のモデルの位置を移動する
		foreach (var model in models) {
			model.gameObject.transform.parent = this.transform;
		}

		// マテリアルををキャプチャ中の色に変更
		this.SetColorMode(ColorMode.Captured);
		
		// モデルのプロパティ編集を禁止
		EditManager.Instance.ModelProperties.SetModels(null);
	}

	// 選択ブロックを開放
	public void ReleaseBlocks() {
		if (!this.HasCapturedBlocks()) {
			return;
		}

		Block[] blocks = this.capturedBlocks;
		Model[] models = this.capturedModels;

		this.capturedLayer.RemoveBlocks(blocks);
		this.capturedLayer.RemoveModels(models, false);

		Vector3 moveVector = this.transform.position - this.capturedCenter;
		
		// キャプチャ対象のモデルの位置を戻す
		foreach (var model in models) {
			if (model.gameObject != null) {
				model.gameObject.transform.parent = EditManager.Instance.CurrentLayer.transform;
			}
		}

		if (this.captureMode == CaptureMode.Moving) {
			// 一旦実在レイヤーに戻して移動コマンドを打つ
			EditManager.Instance.CurrentLayer.AddBlocks(blocks);
			EditManager.Instance.CurrentLayer.AddModels(models);
			EditManager.Instance.MoveObjects(blocks, models, moveVector, 
				this.capturedCenter, this.capturedRotation);
		} else {
			// ブロック追加コマンドを打つ
			foreach (var block in blocks) {
				Vector3 offset = block.position - this.capturedCenter;
				offset = EditUtil.RotatePosition(offset, this.capturedRotation);
				block.SetPosition(this.capturedCenter + offset + moveVector);
				block.SetDirection(EditUtil.RotateDirection(block.direction, this.capturedRotation));
			}
			foreach (var model in models) {
				Vector3 offset = model.position - this.capturedCenter;
				offset = EditUtil.RotatePosition(offset, this.capturedRotation);
				model.SetPosition(this.capturedCenter + offset + moveVector);
				model.SetRotation(model.rotation + this.capturedRotation * 90);
			}
			EditManager.Instance.AddObjects(blocks, models);
		}
		
		this.transform.position = Vector3.zero;
		this.capturedLayer.transform.position = Vector3.zero;
		this.selectedBlockGuide.transform.position = Vector3.zero;
		this.selectedModelGuide.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;
		this.capturedRotation = 0;
		this.capturedBlocks = null;
		
		this.Set(blocks, models);
		this.dirtyMesh = true;
		
		// マテリアルをを選択中の色に変更
		this.SetColorMode(ColorMode.Selection);

		// モデルのプロパティ編集を許可
		EditManager.Instance.ModelProperties.SetModels(this.GetSelectedModels());
	}

	public void CopyToClipboard() {
		var strstm = new StringWriter();
		var writer = new XmlTextWriter(strstm);
		writer.Formatting = Formatting.Indented;

		var xml = new XmlDocument();
		
		var root = xml.CreateElement("tsumiki");
		xml.AppendChild(root);

		foreach (var block in this.GetSelectedBlocks()) {
			var blockNode = xml.CreateElement("block");
			block.Serialize(blockNode);
			root.AppendChild(blockNode);
		}
		
		foreach (var model in this.GetSelectedModels()) {
			var blockNode = xml.CreateElement("model");
			model.Serialize(blockNode);
			root.AppendChild(blockNode);
		}

		xml.WriteTo(writer);

		writer.Flush();
		writer.Close();

		string stringData = strstm.ToString();
		Clipboard.SetTextData(stringData);
	}

	public void PasteFromClipboard() {
		string stringData = Clipboard.GetTextData();
		if (stringData == null) {
			return;
		}
		var strstm = new StringReader(stringData);

		var xml = new XmlDocument();
		xml.Load(strstm);

		var rootList = xml.GetElementsByTagName("tsumiki");
		
		var root = rootList[0] as XmlElement;
		var blockList = root.GetElementsByTagName("block");
		Block[] blocks = new Block[blockList.Count];
		for (int i = 0; i < blockList.Count; i++) {
			var blockNode = blockList[i] as XmlElement;
			blocks[i] = new Block(blockNode);
		}
		var modelList = root.GetElementsByTagName("model");
		Model[] models = new Model[modelList.Count];
		for (int i = 0; i < modelList.Count; i++) {
			var modelNode = modelList[i] as XmlElement;
			models[i] = new Model(modelNode);
		}
		
		this.ReleaseBlocks();
		this.Set(blocks, models);
		this.CaptureBlocks(CaptureMode.Paste, blocks, models);
	}
}
