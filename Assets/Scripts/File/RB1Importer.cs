using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

// RB1フォーマット向けのインポータ
public static class RB1Importer
{
	const int MaxObjects = 20;
	const int MaxSizeX = 24;
	const int MaxSizeZ = 24;

	public static void Import(string path) {
		{
		//try {
			string allText = File.ReadAllText(path);
			StringReader stm = new StringReader(allText);

			// フォーマットバージョンを取得
			int version = int.Parse(stm.ReadLine());

			List<string> modelList = new List<string>();
			
			// オブジェクトのリストを取得
			for (int i = 0; i < MaxObjects; i++) {
				string line = stm.ReadLine();
				if (!String.IsNullOrEmpty(line)) {
					modelList.Add(line);
				}
			}

			// マップサイズを取得
			int cx, cz;
			{
				string line = stm.ReadLine();
				string[] items = line.Split(',');
				cx = int.Parse(items[0]);
				cz = int.Parse(items[1]);
			}
			
			// ヘッダ行を読み飛ばす
			stm.ReadLine();
			
			var layer = EditManager.Instance.CurrentLayer;
			BlockShape[] shapes = new BlockShape[256];
			shapes[ 0] = BlockShape.Find("cube");
			shapes[ 1] = BlockShape.Find("slope");
			shapes[ 2] = BlockShape.Find("slope");
			shapes[ 3] = BlockShape.Find("slope");
			shapes[ 4] = BlockShape.Find("slope");
			shapes[ 5] = BlockShape.Find("diag-slope-large");
			shapes[ 6] = BlockShape.Find("diag-slope-large");
			shapes[ 7] = BlockShape.Find("diag-slope-large");
			shapes[ 8] = BlockShape.Find("diag-slope-large");
			shapes[ 9] = BlockShape.Find("diag-slope-small");
			shapes[10] = BlockShape.Find("diag-slope-small");
			shapes[11] = BlockShape.Find("diag-slope-small");
			shapes[12] = BlockShape.Find("diag-slope-small");
			shapes[21] = BlockShape.Find("steep-slope");
			shapes[22] = BlockShape.Find("steep-slope");
			shapes[23] = BlockShape.Find("steep-slope");
			shapes[24] = BlockShape.Find("steep-slope");
			shapes[25] = BlockShape.Find("steep-diag-slope-large");
			shapes[26] = BlockShape.Find("steep-diag-slope-large");
			shapes[27] = BlockShape.Find("steep-diag-slope-large");
			shapes[28] = BlockShape.Find("steep-diag-slope-large");
			shapes[29] = BlockShape.Find("steep-diag-slope-small");
			shapes[30] = BlockShape.Find("steep-diag-slope-small");
			shapes[31] = BlockShape.Find("steep-diag-slope-small");
			shapes[32] = BlockShape.Find("steep-diag-slope-small");

			BlockDirection[] directions = new BlockDirection[256];
			directions[ 0] = BlockDirection.Zplus;
			directions[ 1] = BlockDirection.Xminus;
			directions[ 2] = BlockDirection.Zminus;
			directions[ 3] = BlockDirection.Xplus;
			directions[ 4] = BlockDirection.Zplus;
			directions[ 5] = BlockDirection.Zplus;
			directions[ 6] = BlockDirection.Xminus;
			directions[ 7] = BlockDirection.Zminus;
			directions[ 8] = BlockDirection.Xplus;
			directions[ 9] = BlockDirection.Xminus;
			directions[10] = BlockDirection.Zminus;
			directions[11] = BlockDirection.Xplus;
			directions[12] = BlockDirection.Zplus;
			
			for (int z = 0; z < cz; z++) {
				int x;
				for (x = 0; x < cx; x++) {
					string line = stm.ReadLine();
					string[] items = line.Split(',');
					
					int blockHeight = int.Parse(items[0]);
					int blockShapeId = int.Parse(items[1]);
					bool canEntry = int.Parse(items[2]) == 0;
					int objId = int.Parse(items[3]);
					int objAngle = int.Parse(items[4]);
					int objScale = int.Parse(items[5]);
					int objY = int.Parse(items[6]);
					int texId = int.Parse(items[7]);

					BlockShape blockShape = null;
					while ((blockShape = shapes[blockShapeId]) == null) {
						blockShapeId -= 20;
					}

					for (int y = 0; y < blockHeight; y++) {
						bool isTop = (y == blockHeight - 1);
						var shape = isTop ? blockShape : shapes[0];
						if (shape != null) {
							var direction = isTop ? directions[blockShapeId % 20] : directions[0];
							var block = new Block(
								new Vector3((float)x, (float)y / 2, (float)-z), 
								direction, shape);
							block.enterable = canEntry;
							layer.AddBlock(block);

							if (isTop) {
								block.SetTextureChip(BlockDirection.Yplus, false, 
									(texId % 20) + (texId / 20) * 100);
								block.SetTextureChip(BlockDirection.Zplus,  false, 7);
								block.SetTextureChip(BlockDirection.Zminus, false, 7);
								block.SetTextureChip(BlockDirection.Xplus,  false, 7);
								block.SetTextureChip(BlockDirection.Xminus, false, 7);
							} else {
								block.SetTextureChip(BlockDirection.Zplus,  false, 6);
								block.SetTextureChip(BlockDirection.Zminus, false, 6);
								block.SetTextureChip(BlockDirection.Xplus,  false, 6);
								block.SetTextureChip(BlockDirection.Xminus, false, 6);
							}
						} else {
							Debug.LogWarning("Unknown block shape: " + blockShape);
						}
					}
					if (objId > 0) {
						string fileName = modelList[objId - 1];
						string modelName = Path.GetFileNameWithoutExtension(fileName);
						var modelShape = ModelShape.Find(modelName);
						if (modelShape != null) {
							layer.AddModel(new Model(modelShape, 
								new Vector3((float)x, (float)blockHeight / 2, (float)-z),
								objAngle, (objScale + 100) * 0.01f));
						} else {
							Debug.LogWarning("Model \"" + modelName + "\" not found.");
						}
					}
				}

				// スキップ行
				for (; x < MaxSizeX; x++) {
					stm.ReadLine();
				}
			}
		}/* catch (Exception e) {
			Debug.LogError(e.Message);
		}*/
	}
}
