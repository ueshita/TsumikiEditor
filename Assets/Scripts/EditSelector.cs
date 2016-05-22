using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Windows.Forms;

public class EditSelector : MonoBehaviour
{
	EditGuide guide;
	BlockGroup guideBlocks = new BlockGroup();

	CaptureMode captureMode;
	EditLayer captureLayer;
	Block[] capturedBlocks = null;
	Vector3 capturedCenter = Vector3.zero;
	int capturedRotation;
	Block[] backup = null;
	bool dirtyMesh = false;
	
	public Vector3 LastPosition {get; private set;}
	public int Count {get {return this.guideBlocks.GetBlockCount();}}

	public enum ColorMode {
		Selection,
		Captured,
	};
	
	public enum CaptureMode {
		Moving,
		Paste,
	};

	void Start() {
		var guideObj = new GameObject();
		guideObj.name = "Guide";
		guideObj.transform.parent = this.transform;
		this.guide = guideObj.AddComponent<EditGuide>();
		this.SetColorMode(ColorMode.Selection);

		var captureObj = new GameObject();
		captureObj.name = "Capture";
		captureObj.transform.parent = this.transform;
		this.captureLayer = captureObj.AddComponent<EditLayer>();
	}

	// 色をセット
	private void SetColorMode(ColorMode colorMode) {
		switch (colorMode) {
		case ColorMode.Selection:
			this.guide.SetColor(new Color(1.0f, 1.0f, 1.0f), new Color(0.5f, 1.0f, 0.5f));
			break;
		case ColorMode.Captured:
			this.guide.SetColor(new Color(1.0f, 1.0f, 1.0f), new Color(1.0f, 0.5f, 1.0f));
			break;
		}
	}

	void LateUpdate() {
		if (this.dirtyMesh) {
			this.guideBlocks.UpdateMesh();
			this.guide.SetMesh(this.guideBlocks.GetColliderMesh(), this.guideBlocks.GetWireMesh());
			this.dirtyMesh = false;
		}
	}

	public bool IsSelected(Vector3 position) {
		return this.guideBlocks.GetBlock(position) != null;
	}

	public void Set(Block[] blocks) {
		this.Clear();
		foreach (var block in blocks) {
			this.Add(block.position);
		}
	}

	public void Add(Vector3 position) {
		var block = new Block(position, BlockDirection.Zplus);
		if (block != null) {
			this.guideBlocks.AddBlock(block);
			this.LastPosition = position;
		}
		this.dirtyMesh = true;
	}

	public void Remove(Vector3 position) {
		var block = this.guideBlocks.GetBlock(position);
		if (block != null) {
			this.guideBlocks.RemoveBlock(block);
			this.LastPosition = position;
		}
		this.dirtyMesh = true;
	}

	public bool HasSelectedBlocks() {
		return this.guideBlocks.GetNumBlocks() > 0;
	}

	public Block[] GetSelectedBlocks() {
		Block[] points = this.guideBlocks.GetAllBlocks();
		List<Block> blocks = new List<Block>();
		foreach (var point in points) {
			Block block = EditManager.Instance.CurrentLayer.GetBlock(point.position);
			if (block != null) {
				blocks.Add(block);
			}
		}
		return blocks.ToArray();
	}

	public void Backup() {
		this.backup = guideBlocks.GetAllBlocks();
	}

	public void Restore() {
		if (this.backup == null) {
			return;
		}

		foreach (var block in this.backup) {
			this.guideBlocks.AddBlock(block);
		}
		this.dirtyMesh = true;
	}

	public void Clear(bool backup = false) {
		this.guideBlocks.Clear();
		this.transform.position = Vector3.zero;
		this.ReleaseBlocks();
		this.dirtyMesh = true;

		if (backup) {
			this.backup = null;
		}
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
					}
				}
			}
		}
		this.LastPosition = end;
	}

	// スクリーンの上方向と右方向を指すワールド方向を取得
	public void ScreenDirToWorldDir(out Vector3 up, out Vector3 right) {
		Vector3 upDir = Camera.main.transform.up;
		Vector3 rightDir = Camera.main.transform.right;
		
		Vector3[] worldDir = new Vector3[]{Vector3.up*0.5f, Vector3.down*0.5f, Vector3.right, Vector3.left, Vector3.forward, Vector3.back};
		
		int upDirIndex = -1, rightDirIndex = -1;
		float upDirDot = -1.0f, rightDirDot = -1.0f;
		for (int i = 0; i < worldDir.Length; i++) {
			float dot = Vector3.Dot(upDir, worldDir[i].normalized);
			if (dot > upDirDot) {
				upDirIndex = i;
				upDirDot = dot;
			}
		}
		for (int i = 0; i < worldDir.Length; i++) {
			float dot = Vector3.Dot(rightDir, worldDir[i].normalized);
			if (dot > rightDirDot) {
				rightDirIndex = i;
				rightDirDot = dot;
			}
		}
		
		up = worldDir[upDirIndex];
		right = worldDir[rightDirIndex];
	}

	// 選択ブロックを移動
	public void Move(Vector2 screenDir) {
		if (!this.HasCapturedBlocks()) {
			this.CaptureBlocks(CaptureMode.Moving, this.GetSelectedBlocks());
		}

		Vector3 up, right;
		ScreenDirToWorldDir(out up, out right);
		this.transform.position = this.transform.position + up * screenDir.y + right * screenDir.x;
	}
	
	// 選択ブロックを回転
	public void Rotate(int value) {
		if (!this.HasCapturedBlocks()) {
			this.CaptureBlocks(CaptureMode.Moving, this.GetSelectedBlocks());
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
		ScreenDirToWorldDir(out up, out right);
		Vector3 expandVector =  up * screenDir.y + right * screenDir.x;

		foreach (var point in this.guideBlocks.GetAllBlocks()) {
			Vector3 newPosition = point.position + expandVector;
			var target = this.guideBlocks.GetBlock(newPosition);
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
		Block[] points = this.guideBlocks.GetAllBlocks();
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
	public void CaptureBlocks(CaptureMode mode, Block[] blocks) {
		if (this.HasCapturedBlocks()) {
			return;
		}

		this.captureMode = mode;

		if (mode == CaptureMode.Moving) {
			// 一旦実在レイヤーから削除
			EditManager.Instance.CurrentLayer.RemoveBlocks(blocks);
		}
		
		if (blocks.Length == 0) {
			return;
		}
		
		Vector3 sumPosition = Vector3.zero;

		// キャプチャレイヤーに放り込む
		foreach (var block in blocks) {
			this.captureLayer.AddBlock(block);
			sumPosition += block.position;
		}

		// 中心位置を計算する
		this.capturedCenter = sumPosition / blocks.Length;
		capturedCenter.x = Mathf.Round(capturedCenter.x);
		capturedCenter.y = Mathf.Round(capturedCenter.y * 2.0f) * 0.5f;
		capturedCenter.z = Mathf.Round(capturedCenter.z);
		
		this.capturedBlocks = blocks;
		this.captureLayer.transform.position = -this.capturedCenter;
		this.guide.transform.position = -this.capturedCenter;
		this.transform.position = this.capturedCenter;

		this.SetColorMode(ColorMode.Captured);
	}

	// 選択ブロックを開放
	public void ReleaseBlocks() {
		if (!this.HasCapturedBlocks()) {
			return;
		}

		Block[] blocks = this.capturedBlocks;
		this.captureLayer.RemoveBlocks(blocks);

		Vector3 moveVector = this.transform.position - this.capturedCenter;

		if (this.captureMode == CaptureMode.Moving) {
			// 一旦実在レイヤーに戻して移動コマンドを打つ
			EditManager.Instance.CurrentLayer.AddBlocks(blocks);
			EditManager.Instance.MoveBlocks(blocks, moveVector, this.capturedCenter, this.capturedRotation);
		} else {
			// ブロック追加コマンドを打つ
			foreach (var block in blocks) {
				Vector3 offset = block.position - this.capturedCenter;
				offset = EditUtil.RotatePosition(offset, this.capturedRotation);
				block.SetPosition(this.capturedCenter + offset + moveVector);
				block.SetDirection(EditUtil.RotateDirection(block.direction, this.capturedRotation));
			}
			EditManager.Instance.AddBlocks(blocks);
		}
		
		this.transform.position = Vector3.zero;
		this.captureLayer.transform.position = Vector3.zero;
		this.guide.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;
		this.capturedRotation = 0;
		this.capturedBlocks = null;
		
		this.Set(blocks);
		this.dirtyMesh = true;
		
		this.SetColorMode(ColorMode.Selection);
	}

	public void CopyToClipboard() {
		var strstm = new StringWriter();
		var writer = new XmlTextWriter(strstm);
		writer.Formatting = Formatting.Indented;

		var xml = new XmlDocument();
		
		var root = xml.CreateElement("tsumiki");
		xml.AppendChild(root);

		foreach (var blocks in this.GetSelectedBlocks()) {
			var blockNode = xml.CreateElement("block");
			blocks.Serialize(blockNode);
			root.AppendChild(blockNode);
		}

		xml.WriteTo(writer);

		writer.Flush();
		writer.Close();

		string stringData = strstm.ToString();

		DataObject data = new DataObject();
		data.SetData(DataFormats.Text, stringData);
		Clipboard.SetDataObject(data);
	}

	public void PasteFromClipboard() {
		IDataObject data = Clipboard.GetDataObject();
		string stringData = (string)data.GetData(DataFormats.Text);
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
		
		this.ReleaseBlocks();
		this.Set(blocks);
		this.CaptureBlocks(CaptureMode.Paste, blocks);
	}
}
