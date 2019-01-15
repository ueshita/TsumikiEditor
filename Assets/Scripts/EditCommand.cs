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

		EditManager.Instance.OnDataChanged();
	}

	// コマンドをグループ化(開始)
	public void BeginCommandGroup() {
		if (this.groupIndex >= 0) {
			Debug.LogError("Command Grouping Failed.");
			return;
		}
		this.groupIndex = this.cmdpos;
	}
	
	// コマンドをグループ化(終了)
	public void EndCommandGroup() {
		if (this.groupIndex < 0) {
			Debug.LogError("Command Grouping Failed.");
			return;
		}

		if (this.groupIndex < this.cmdpos) {
			var list = this.cmdlist.GetRange(this.groupIndex, this.cmdpos - this.groupIndex);
			this.cmdlist.RemoveRange(this.groupIndex, this.cmdpos - this.groupIndex);

			this.AddCommand(new Command(
			() => {
				for (int i = 0; i < list.Count; i++) {
					list[i].Do();
				}
			}, () => {
				for (int i = list.Count - 1; i >= 0; i--) {
					list[i].Undo();
				}
			}));
		}
		this.cmdpos = this.groupIndex + 1;
		this.groupIndex = -1;
	}

	public void AddBlock(Vector3 position, BlockDirection direction) {
		EditLayer layer = this.CurrentLayer;
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
			layer.AddBlock(block);
		}, () => {
			layer.RemoveBlock(block);
		}));
	}

	public void RemoveBlock(Vector3 position) {
		EditLayer layer = this.CurrentLayer;
		Block block = layer.GetBlock(position);
		if (block == null) {
			return;
		}
		this.AddCommand(new Command(
		() => {
			layer.RemoveBlock(block);
		}, () => {
			layer.AddBlock(block);
		}));
	}
	
	public void AddObjects(Block[] blocks, Model[] models) {
		EditLayer layer = this.CurrentLayer;
		Block[] removedBlocks = null;	// 上書きされたブロック
		Model[] removedModels = null;	// 上書きされたモデル
		this.AddCommand(new Command(
		() => {
			removedBlocks = layer.AddBlocks(blocks);
			removedModels = layer.AddModels(models);
		}, () => {
			layer.RemoveModels(models, true);
			layer.RemoveBlocks(blocks);
			layer.AddBlocks(removedBlocks);
			layer.AddModels(removedModels);
			removedBlocks = null;
			removedModels = null;
		}));
	}

	public void RemoveObjects(Block[] blocks, Model[] models) {
		EditLayer layer = this.CurrentLayer;
		this.AddCommand(new Command(
		() => {
			layer.RemoveBlocks(blocks);
			layer.RemoveModels(models, true);
		}, () => {
			layer.AddModels(models);
			layer.AddBlocks(blocks);
		}));
	}

	public void MoveObjects(Block[] blocks, Model[] models, Vector3 moveVector, 
		Vector3 centerPosition, int rotation) {
		EditLayer layer = this.CurrentLayer;
		Block[] removedBlocks = null;	// 上書きされたブロック
		Model[] removedModels = null;	// 上書きされたモデル

		if (blocks.Length == 0 && models.Length == 0) {
			return;
		}
		this.AddCommand(new Command(
		() => {
			// レイヤーから一旦削除
			layer.RemoveBlocks(blocks);
			layer.RemoveModels(models, false);
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
			// レイヤーに戻す
			removedBlocks = layer.AddBlocks(blocks);
			removedModels = layer.AddModels(models);
		}, () => {
			// レイヤーから一旦削除
			layer.RemoveBlocks(blocks);
			layer.RemoveModels(models, false);
			// 退避したブロックを復活させる
			layer.AddBlocks(removedBlocks);
			layer.AddModels(removedModels);
			removedBlocks = null;
			removedModels = null;
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
			layer.AddBlocks(blocks);
			layer.AddModels(models);
		}));
	}

	public void PaintBlock(Block block, BlockDirection direction, bool isObject, int textureChip) {
		EditLayer layer = this.CurrentLayer;
		int oldTextureChip = block.GetTextureChip(direction, isObject);
		
		// 同じ場合は反映しない
		if (textureChip == oldTextureChip) {
			return;
		}

		this.AddCommand(new Command(
		() => {
			block.SetTextureChip(direction, isObject, textureChip);
			layer.SetDirty();
		}, () => {
			block.SetTextureChip(direction, isObject, oldTextureChip);
			layer.SetDirty();
		}));
	}

	public void SetEnterable(Block block, bool enterable) {
		EditLayer layer = this.CurrentLayer;
		bool oldEnterable = block.enterable;

		this.AddCommand(new Command(
		() => {
			block.enterable = enterable;
			layer.SetDirty();
			this.RoutePath.dirtyMesh = true;
		}, () => {
			block.enterable = oldEnterable;
			layer.SetDirty();
			this.RoutePath.dirtyMesh = true;
		}));
	}
	
	public void AddModel(Vector3 position, int rotation) {
		EditLayer layer = this.CurrentLayer;
		var modelShape = ModelShape.Find(this.toolModel);
		var model = new Model(modelShape, position, rotation);

		this.AddCommand(new Command(
		() => {
			layer.AddModel(model);
		}, () => {
			layer.RemoveModel(model, true);
		}));
	}
	public void RemoveModel(Model model) {
		EditLayer layer = this.CurrentLayer;
		this.AddCommand(new Command(
		() => {
			layer.RemoveModel(model, true);
		}, () => {
			layer.AddModel(model);
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