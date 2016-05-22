using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class EditManager : MonoBehaviour
{
	class Command {
		public delegate void Method();
		public Method Do {get; private set;}
		public Method Undo {get; private set;}
		public Command(Method doMethod, Method undoMethod) {
			this.Do = doMethod; this.Undo = undoMethod;
		}
	}
	public void Undo() {
		this.Selector.Clear();
		if (this.cmdpos > 0 && this.cmdlist.Count > 0) {
			this.cmdpos--;
			this.cmdlist[this.cmdpos].Undo();
		}
	}
	
	public void Redo() {
		this.Selector.Clear();
		if (this.cmdpos < this.cmdlist.Count) {
			this.cmdlist[this.cmdpos].Do();
			this.cmdpos++;
		}
	}

	void AddCommand(Command command) {
		if (this.cmdpos < this.cmdlist.Count) {
			for (int i = this.cmdlist.Count - 1; i >= this.cmdpos; i--) {
				this.cmdlist.RemoveAt(i);
			}
		}
		this.cmdlist.Add(command);
		this.cmdpos++;
		command.Do();
	}

	public void AddBlock(int layer, Vector3 position, BlockDirection direction) {
		Block block = new Block(position, direction, this.toolBlock);
		if (block == null) {
			return;
		}
		this.AddCommand(new Command(
		() => {
			this.Layers[layer].AddBlock(block);
		}, () => {
			this.Layers[layer].RemoveBlock(block);
		}));
	}

	public void RemoveBlock(int layer, Vector3 position) {
		Block block = this.Layers[layer].GetBlock(position);
		if (block == null) {
			return;
		}
		this.AddCommand(new Command(
		() => {
			this.Layers[layer].RemoveBlock(block);
		}, () => {
			this.Layers[layer].AddBlock(block);
		}));
	}
	
	public void AddBlocks(Block[] blocks) {
		this.AddCommand(new Command(
		() => {
			this.CurrentLayer.AddBlocks(blocks);
		}, () => {
			this.CurrentLayer.RemoveBlocks(blocks);
		}));
	}

	public void RemoveBlocks(Block[] blocks) {
		this.AddCommand(new Command(
		() => {
			this.CurrentLayer.RemoveBlocks(blocks);
		}, () => {
			this.CurrentLayer.AddBlocks(blocks);
		}));
	}

	public void MoveBlocks(Block[] blocks, Vector3 moveVector, 
		Vector3 centerPosition, int rotation) {
		List<Block> removedBlocks = new List<Block>();	// 上書きされたブロック

		if (blocks.Length == 0) {
			return;
		}
		this.AddCommand(new Command(
		() => {
			// レイヤーから一旦削除
			this.CurrentLayer.RemoveBlocks(blocks);
			// ブロックを移動回転させる
			foreach (var block in blocks) {
				Vector3 offset = block.position - centerPosition;
				offset = EditUtil.RotatePosition(offset, rotation);
				block.SetPosition(centerPosition + offset + moveVector);
				block.SetDirection(EditUtil.RotateDirection(block.direction, rotation));
			}
			// 上書きしそうなブロックを退避させる
			foreach (var block in blocks) {
				Block removedBlock = this.CurrentLayer.GetBlock(block.position);
				if (removedBlock != null) {
					this.CurrentLayer.RemoveBlock(removedBlock);
					removedBlocks.Add(removedBlock);
				}
			}
			// レイヤーに戻す
			this.CurrentLayer.AddBlocks(blocks);
		}, () => {
			// レイヤーから一旦削除
			this.CurrentLayer.RemoveBlocks(blocks);
			// 退避したブロックを復活させる
			this.CurrentLayer.AddBlocks(removedBlocks.ToArray());
			removedBlocks.Clear();
			// ブロックを逆方向に移動回転させる
			foreach (var block in blocks) {
				Vector3 offset = block.position - centerPosition - moveVector;
				offset = EditUtil.RotatePosition(offset, -rotation);
				block.SetPosition(centerPosition + offset);
				block.SetDirection(EditUtil.RotateDirection(block.direction, -rotation));
			}
			// レイヤーに戻す
			this.CurrentLayer.AddBlocks(blocks);
		}));
	}

	public void PaintBlock(Block block, BlockDirection direction, int textureChip) {
		int oldTextureChip = block.GetTextureChip(direction);
		this.AddCommand(new Command(
		() => {
			block.SetTextureChip(direction, textureChip);
			this.CurrentLayer.SetDirty();
		}, () => {
			block.SetTextureChip(direction, oldTextureChip);
			this.CurrentLayer.SetDirty();
		}));
	}
}
