using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EditUtil
{
	public static readonly Vector3[] blockVertexPoints = {
		new Vector3( 1,  0.5f,  1), new Vector3( 1, -0.5f,  1), new Vector3(-1,  0.5f,  1), new Vector3(-1, -0.5f,  1),
		new Vector3( 1,  0.5f, -1), new Vector3( 1, -0.5f, -1), new Vector3(-1,  0.5f, -1), new Vector3(-1, -0.5f, -1)
	};
	public static readonly int[][] blockFaceIndices = {
		new int[] {0, 1, 2, 3}, new int[] {6, 7, 4, 5}, 
		new int[] {4, 5, 0, 1}, new int[] {2, 3, 6, 7}, 
		new int[] {6, 4, 2, 0}, new int[] {3, 1, 7, 5}
	};
	public static readonly Vector2[] blockVertexUvs = {
		new Vector2( 0,  0), new Vector2( 0,  1),
		new Vector2( 1,  0), new Vector2( 1,  1)
	};
	public static readonly int[] trianglesIndices = {
		0, 2, 1, 3, 1, 2
	};
	public static readonly int[] linesIndices = {
		0, 1, 2, 3, 0, 2, 1, 3
	};
	
	public static void Swap<T>(ref T lhs, ref T rhs) {
		T temp;
		temp = lhs;
		lhs = rhs;
		rhs = temp;
	}
	public static void MinMax(ref float min, ref float max) {
		if (min > max) {
			Swap(ref min, ref max);
		}
	}
	public static void MinMax(ref int min, ref int max) {
		if (min > max) {
			Swap(ref min, ref max);
		}
	}
	public static void MinMaxElements(ref Vector3 min, ref Vector3 max) {
		MinMax(ref min.x, ref max.x);
		MinMax(ref min.y, ref max.y);
		MinMax(ref min.z, ref max.z);
	}
	public static void MinMaxElements(ref Vector3i min, ref Vector3i max) {
		MinMax(ref min.x, ref max.x);
		MinMax(ref min.y, ref max.y);
		MinMax(ref min.z, ref max.z);
	}

	public static Mesh CreateWireBlock() {
		var vertexPos = new List<Vector3>();
		var linesIndices = new List<int>();

		for (int i = 0; i < 6; i++) {
			int offset = vertexPos.Count;
			for (int j = 0; j < 4; j++) {
				vertexPos.Add(EditUtil.blockVertexPoints[EditUtil.blockFaceIndices[i][j]] * 0.5f);
			}
			for (int j = 0; j < 8; j++) {
				linesIndices.Add(offset + EditUtil.linesIndices[j]);
			}
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertexPos.ToArray();
		mesh.SetIndices(linesIndices.ToArray(), MeshTopology.Lines, 0);
		return mesh;
	}

	public static Mesh CreateGrid(Vector3 offset, int cx, int cz) {
		var vertexPos = new List<Vector3>();
		var linesIndices = new List<int>();

		Vector3 half = new Vector3(cx * 0.5f, 0.0f, cz * 0.5f);
		int indexOffset = 0;
		for (int i = 0; i < cx + 1; i++) {
			vertexPos.Add(offset + new Vector3(i - half.x, 0.0f, -half.z));
			vertexPos.Add(offset + new Vector3(i - half.x, 0.0f, +half.z));
			linesIndices.Add(indexOffset + i * 2);
			linesIndices.Add(indexOffset + i * 2 + 1);
		}

		indexOffset += vertexPos.Count;
		for (int i = 0; i < cz + 1; i++) {
			vertexPos.Add(offset + new Vector3(-half.x, 0.0f, i - half.z));
			vertexPos.Add(offset + new Vector3(+half.x, 0.0f, i - half.z));
			linesIndices.Add(indexOffset + i * 2);
			linesIndices.Add(indexOffset + i * 2 + 1);
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertexPos.ToArray();
		mesh.SetIndices(linesIndices.ToArray(), MeshTopology.Lines, 0);
		return mesh;
	}
	public static Mesh CreatePanelXZ(Vector3 offset, float cx, float cz) {
		var vertexPos = new List<Vector3>();
		var triangleIndices = new List<int>();

		for (int j = 0; j < 4; j++) {
			Vector3 size = EditUtil.blockVertexPoints[EditUtil.blockFaceIndices[4][j]];
			vertexPos.Add(offset + new Vector3(size.x * cx * 0.5f, 0.0f, size.z * cz * 0.5f));
		}
		for (int j = 0; j < 6; j++) {
			triangleIndices.Add(EditUtil.trianglesIndices[j]);
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertexPos.ToArray();
		mesh.SetIndices(triangleIndices.ToArray(), MeshTopology.Triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}
}

public struct Vector3i
{
	public int x, y, z;
	public Vector3i(Vector3 vec) {
		this.x = Mathf.RoundToInt(vec.x);
		this.y = Mathf.RoundToInt(vec.y);
		this.z = Mathf.RoundToInt(vec.z);
	}
}