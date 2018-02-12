using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 半精度float版Vector3
public struct Vector3h
{
	public HalfFloat x, y, z;

	public Vector3h(Vector3 vec) {
		this.x = (HalfFloat)vec.x;
		this.y = (HalfFloat)vec.y;
		this.z = (HalfFloat)vec.z;
	}
	public Vector3h(float x, float y, float z) {
		this.x = (HalfFloat)x;
		this.y = (HalfFloat)y;
		this.z = (HalfFloat)z;
	}

	public static implicit operator Vector3h(Vector3 rhs) {
		return new Vector3h(rhs);
	}

	public static Vector3h operator+(Vector3h lhs, Vector3h rhs) {
		return new Vector3h(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
	}
}
