using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EditSelection : MonoBehaviour
{
	BlockGroup blockGroup = new BlockGroup();
	Block[] backup = null;
	bool dirtyMesh = false;
	
	public Vector3 LastPosition {get; private set;}
	public int Count {get {return this.blockGroup.GetBlockCount();}}

	void Start() {
		this.gameObject.AddComponent<MeshFilter>();
		var meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.material = Resources.Load<Material>("Materials/GuideMaterial");
		meshRenderer.material.color = new Color(0.0f, 1.0f, 0.0f);
	}
	void Update() {
		if (this.dirtyMesh) {
			this.blockGroup.UpdateMesh();
			this.GetComponent<MeshFilter>().sharedMesh = this.blockGroup.GetWireMesh();
			this.dirtyMesh = true;
		}
	}
	public bool IsSelected(Vector3 position) {
		return this.blockGroup.GetBlock(position) != null;
	}
	public void Add(Vector3 position) {
		var block = new CubeBlock();
		block.SetPosition(position);
		this.blockGroup.AddBlock(block);
		this.LastPosition = position;
		this.dirtyMesh = true;
	}
	public void Remove(Vector3 position) {
		this.blockGroup.RemoveBlock(position);
		this.LastPosition = position;
		this.dirtyMesh = true;
	}
	public Block[] GetAllBlocks() {
		Block[] points = this.blockGroup.GetAllBlocks();
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
		this.backup = blockGroup.GetAllBlocks();
	}
	public void Restore() {
		if (this.backup == null) {
			return;
		}

		foreach (var block in this.backup) {
			this.blockGroup.AddBlock(block);
		}
		this.dirtyMesh = true;
	}

	public void Clear(bool backup = false) {
		this.blockGroup.Clear();
		this.dirtyMesh = true;

		if (backup) {
			this.backup = null;
		}
	}
	
	public void SelectRange(Vector3 begin, Vector3 end) {
		Vector3i bpos = new Vector3i(begin);
		Vector3i epos = new Vector3i(end);
		EditUtil.MinMaxElements(ref bpos, ref epos);
		for (int z = bpos.z; z <= epos.z; z++) {
			for (int y = bpos.y; y <= epos.y; y++) {
				for (int x = bpos.x; x <= epos.x; x++) {
					Vector3 curpos = new Vector3(x, y, z);
					if (curpos == this.LastPosition) {
						continue;
					}
					if (this.IsSelected(curpos)) {
						this.Remove(curpos);
					} else {
						this.Add(curpos);
					}
				}
			}
		}
	}
}
