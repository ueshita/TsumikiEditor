using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System;

public class BlockShape
{
	public class Face
	{
		public readonly int type;
		public readonly int[] indices;
		public readonly Vector2[] uv;

		public Face(int type, int[] indices, Vector2[] uv) {
			this.indices = indices;
			this.uv = uv;
		}
	}

	public readonly string name;
	public readonly string displayName;
	
	public readonly int connectionType;
	
	/// 接続タイプ
	/// 0: 閉塞無し
	/// 1: 完全閉塞
	/// 2~: 不完全閉塞(~の向き)
	public readonly int[] connection;
	public readonly Mesh[] meshes;

	public BlockShape(string name, string displayName, 
		int connectionType, int[] connection
	) {
		this.name = name;
		this.displayName = displayName;
		this.connectionType = connectionType;
		this.connection = connection;

		this.meshes = new Mesh[6];

		string[] objNames = new string[]{"Zplus", "Zminus", "Xminus", "Xplus", "Yplus", "Yminus"};
		var obj = Resources.Load<GameObject>("Blocks/" + name);
		if (obj != null) {
			for (int i = 0; i < 6; i++) {
				var child = obj.transform.Find(objNames[i]);
				var meshFilter = child.GetComponent<MeshFilter>();
				if (meshFilter) {
					this.meshes[i] = meshFilter.sharedMesh;
				}
			}
		}
	}
	
	public static BlockShape Find(string name) {
		for (int i = 0; i < palette.Length; i++) {
			if (name == palette[i].name) {
				return palette[i];
			}
		}
		return null;
	}

	private static Vector3 v3(float x, float y, float z) {
		return new Vector3(x, y, z);
	}
	private static int[] i3(int s0, int s1, int s2) {
		return new int[] {s0, s1, s2};
	}
	private static int[] i4(int s0, int s1, int s2, int s3) {
		return new int[] {s0, s1, s2, s3};
	}
	private static Vector2[] uv3(float x0, float y0, float x1, float y1, float x2, float y2) {
		return new Vector2[] {
			new Vector2(x0, y0),
			new Vector2(x1, y1),
			new Vector2(x2, y2)
		};
	}
	private static Vector2[] uv4(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3) {
		return new Vector2[] {
			new Vector2(x0, y0),
			new Vector2(x1, y1),
			new Vector2(x2, y2),
			new Vector2(x3, y3)
		};
	}
	public static BlockShape[] palette = {
		new BlockShape("cube", "標準",
			0, new int[]{1, 1, 1, 1, 1, 1}),
		new BlockShape("diag-slope-large", "対角斜面(大)",
			1, new int[]{4, 1, 3, 0, 0, 1}),
		new BlockShape("slope", "斜面",
			1, new int[]{0, 1, 2, 2, 0, 1}),
		new BlockShape("diag-slope-small", "対角斜面(小)",
			1, new int[]{0, 5, 2, 0, 0, 1}),
		new BlockShape("steep-slope-top", "急斜面(上)",
			2, new int[]{0, 1, 2, 2, 0, 2}),
		new BlockShape("steep-slope-btm", "急斜面(下)",
			2, new int[]{0, 1, 2, 2, 0, 2}),
		new BlockShape("stair", "階段",
			3, new int[]{0, 1, 2, 2, 0, 1}),
	};
}
