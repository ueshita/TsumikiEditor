using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

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
		BlockShape shape = BlockShape.Find(this.toolBlock);
		if (shape == null) {
			return;
		}

		Block block = new Block(position, direction, shape);
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
	
	public void AddObjects(Block[] blocks, Model[] models) {
		this.AddCommand(new Command(
		() => {
			this.CurrentLayer.AddBlocks(blocks);
			this.CurrentLayer.AddModels(models);
		}, () => {
			this.CurrentLayer.RemoveModels(models, true);
			this.CurrentLayer.RemoveBlocks(blocks);
		}));
	}

	public void RemoveObjects(Block[] blocks, Model[] models) {
		this.AddCommand(new Command(
		() => {
			this.CurrentLayer.RemoveBlocks(blocks);
			this.CurrentLayer.RemoveModels(models, true);
		}, () => {
			this.CurrentLayer.AddModels(models);
			this.CurrentLayer.AddBlocks(blocks);
		}));
	}

	public void MoveObjects(Block[] blocks, Model[] models, Vector3 moveVector, 
		Vector3 centerPosition, int rotation) {
		List<Block> removedBlocks = new List<Block>();	// 上書きされたブロック
		List<Model> removedModels = new List<Model>();	// 上書きされたブロック

		if (blocks.Length == 0 && models.Length == 0) {
			return;
		}
		this.AddCommand(new Command(
		() => {
			// レイヤーから一旦削除
			this.CurrentLayer.RemoveBlocks(blocks);
			this.CurrentLayer.RemoveModels(models, false);
			// ブロックを移動回転させる
			foreach (var block in blocks) {
				Vector3 offset = block.position - centerPosition;
				offset = EditUtil.RotatePosition(offset, rotation);
				block.SetPosition(centerPosition + offset + moveVector);
				block.SetDirection(EditUtil.RotateDirection(block.direction, rotation));
			}
			// モデルを移動回転させる
			foreach (var model in models) {
				Vector3 offset = model.position - centerPosition;
				offset = EditUtil.RotatePosition(offset, rotation);
				model.SetPosition(centerPosition + offset + moveVector);
				model.SetRotation(model.rotation + rotation * 90);
			}
			// 上書きしそうなブロックを退避させる
			foreach (var block in blocks) {
				Block removedBlock = this.CurrentLayer.GetBlock(block.position);
				if (removedBlock != null) {
					this.CurrentLayer.RemoveBlock(removedBlock);
					removedBlocks.Add(removedBlock);
				}
			}
			// 上書きしそうなモデルを退避させる
			foreach (var model in models) {
				Model removedModel = this.CurrentLayer.GetModel(model.position);
				if (removedModel != null) {
					this.CurrentLayer.RemoveModel(removedModel, true);
					removedModels.Add(removedModel);
				}
			}
			// レイヤーに戻す
			this.CurrentLayer.AddBlocks(blocks);
			this.CurrentLayer.AddModels(models);
		}, () => {
			// レイヤーから一旦削除
			this.CurrentLayer.RemoveBlocks(blocks);
			this.CurrentLayer.RemoveModels(models, false);
			// 退避したブロックを復活させる
			this.CurrentLayer.AddBlocks(removedBlocks.ToArray());
			removedBlocks.Clear();
			this.CurrentLayer.AddModels(removedModels.ToArray());
			removedModels.Clear();
			// ブロックを逆方向に移動回転させる
			foreach (var block in blocks) {
				Vector3 offset = block.position - centerPosition - moveVector;
				offset = EditUtil.RotatePosition(offset, -rotation);
				block.SetPosition(centerPosition + offset);
				block.SetDirection(EditUtil.RotateDirection(block.direction, -rotation));
			}
			// モデルを逆方向に移動回転させる
			foreach (var model in models) {
				Vector3 offset = model.position - centerPosition - moveVector;
				offset = EditUtil.RotatePosition(offset, -rotation);
				model.SetPosition(centerPosition + offset);
				model.SetRotation(model.rotation - rotation * 90);
			}
			// レイヤーに戻す
			this.CurrentLayer.AddBlocks(blocks);
			this.CurrentLayer.AddModels(models);
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
	
	public void AddModel(int layer, Vector3 position) {
		var modelShape = ModelShape.Find(this.toolModel);
		var model = new Model(modelShape, position, 0);

		this.AddCommand(new Command(
		() => {
			this.Layers[layer].AddModel(model);
		}, () => {
			this.Layers[layer].RemoveModel(model, true);
		}));
	}
	public void RemoveModel(int layer, Model model) {
		this.AddCommand(new Command(
		() => {
			this.Layers[layer].RemoveModel(model, true);
		}, () => {
			this.Layers[layer].AddModel(model);
		}));
	}
	public void RemoveModels(Model[] models) {
		this.AddCommand(new Command(
		() => {
			this.CurrentLayer.RemoveModels(models, true);
		}, () => {
			this.CurrentLayer.AddModels(models);
		}));
	}

	public void SetModelOffsetX(Model[] models, float offsetX) {
		Vector3[] originalOffsets = models.Select(o => o.offset).ToArray();

		this.AddCommand(new Command(
		() => {
			models.ForEach(o => o.SetOffset(new Vector3(offsetX, o.offset.y, o.offset.z)));
		}, () => {
			models.ForEach((x, i) => x.SetOffset(originalOffsets[i]));
		}));
	}
	
	public void SetModelOffsetY(Model[] models, float offsetY) {
		Vector3[] originalOffsets = models.Select(o => o.offset).ToArray();

		this.AddCommand(new Command(
		() => {
			models.ForEach(o => o.SetOffset(new Vector3(o.offset.z, offsetY, o.offset.z)));
		}, () => {
			models.ForEach((x, i) => x.SetOffset(originalOffsets[i]));
		}));
	}
	
	public void SetModelOffsetZ(Model[] models, float offsetZ) {
		Vector3[] originalOffsets = models.Select(o => o.offset).ToArray();

		this.AddCommand(new Command(
		() => {
			models.ForEach(o => o.SetOffset(new Vector3(o.offset.x, o.offset.y, offsetZ)));
		}, () => {
			models.ForEach((x, i) => x.SetOffset(originalOffsets[i]));
		}));
	}

	public void SetModelRotation(Model[] models, int rotation) {
		int[] originalRotations = models.Select(x => x.rotation).ToArray();

		this.AddCommand(new Command(
		() => {
			models.ForEach(o => o.SetRotation(rotation));
		}, () => {
			models.ForEach((o, i) => o.SetRotation(originalRotations[i]));
		}));
	}

	public void SetModelScale(Model[] models, float scale) {
		float[] originalScales = models.Select(o => o.scale).ToArray();
		
		this.AddCommand(new Command(
		() => {
			models.ForEach(o => o.SetScale(scale));
		}, () => {
			models.ForEach((o, i) => o.SetScale(originalScales[i]));
		}));
	}

	public void AddRoutePath(Vector3 p1, Vector3 p2) {
		this.AddCommand(new Command(
		() => {
			this.RoutePath.AddPath(p1, p2);
		}, () => {
			this.RoutePath.RemovePath(p1, p2);
		}));		
	}
	
	public void RemoveRoutePath(Vector3 p1, Vector3 p2) {
		this.AddCommand(new Command(
		() => {
			this.RoutePath.RemovePath(p1, p2);
		}, () => {
			this.RoutePath.AddPath(p1, p2);
		}));		
	}
}

/// <summary>
/// Arrayクラスを拡張する
/// </summary>
public static class ArrayExtension
{
	/// <summary>
	/// ForEachメソッド
	/// </summary>
	/// <param name="array">Arrayクラス</param>
	/// <param name="action">実行させたいActionデリゲート</param>
	public static void ForEach<T>(this T[] array, Action<T> action)
	{
		for (int i = 0; i < array.Length; i++) {
			action(array[i]);
		}
	}
	/// <summary>
	/// ForEachメソッド(Index付き)
	/// </summary>
	/// <param name="array">Arrayクラス</param>
	/// <param name="action">実行させたいActionデリゲート</param>
	public static void ForEach<T>(this T[] array, Action<T, int> action)
	{
		for (int i = 0; i < array.Length; i++) {
			action(array[i], i);
		}
	}
}