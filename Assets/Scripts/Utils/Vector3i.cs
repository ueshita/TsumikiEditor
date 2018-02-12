using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// int版Vector3
public struct Vector3i
{
	public int x, y, z;

	public Vector3i(Vector3 vec) {
		this.x = Mathf.RoundToInt(vec.x);
		this.y = Mathf.RoundToInt(vec.y);
		this.z = Mathf.RoundToInt(vec.z);
	}
	public Vector3i(float x, float y, float z) {
		this.x = Mathf.RoundToInt(x);
		this.y = Mathf.RoundToInt(y);
		this.z = Mathf.RoundToInt(z);
	}
	
	public static implicit operator Vector3i(Vector3 rhs) {
		return new Vector3i(rhs);
	}
	
	public static Vector3i operator+(Vector3i lhs, Vector3i rhs) {
		return new Vector3i(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
	}
}
