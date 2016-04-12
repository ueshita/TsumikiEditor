using UnityEngine;
using System.Collections;

public partial class EditManager : MonoBehaviour
{
	class Command {
		public delegate void Method();
		public Method Undo {get; private set;}
		public Method Redo {get; private set;}
		public Command(Method undo, Method redo) {
			this.Undo = undo; this.Redo = redo;
		}
	}
	
	void PushCommand(Command command) {
		if (this.cmdpos < this.cmdlist.Count) {
			for (int i = this.cmdlist.Count - 1; i >= this.cmdpos; i--) {
				this.cmdlist.RemoveAt(i);
			}
		}
		this.cmdlist.Add(command);
		this.cmdpos++;
	}

	public void AddBlock(int layer, Vector3 position) {
		Block block = new CubeBlock();
		block.SetPosition(position);
		
		block = this.Layers[layer].AddBlock(block);
		if (block != null) {
			this.PushCommand(new Command(
			() => {
				this.Layers[layer].RemoveBlock(block.position);
			}, () => {
				this.Layers[layer].AddBlock(block);
			}));
		}
	}

	public void RemoveBlock(int layer, Vector3 position) {
		Block block = this.Layers[layer].RemoveBlock(position);
		if (block != null) {
			this.PushCommand(new Command(
			() => {
				this.Layers[layer].AddBlock(block);
			}, () => {
				this.Layers[layer].RemoveBlock(block.position);
			}));
		}
	}
	
	public void RemoveSelectedBlocks() {
		Block[] blocks = this.Selection.GetAllBlocks();
		
		if (blocks.Length > 0) {
			foreach (var block in blocks) {
				this.CurrentLayer.RemoveBlock(block.position);
			}
			this.PushCommand(new Command(
			() => {
				foreach (var block in blocks) {
					this.CurrentLayer.AddBlock(block);
				}
			}, () => {
				foreach (var block in blocks) {
					this.CurrentLayer.RemoveBlock(block.position);
				}
			}));
		}
	}
}
