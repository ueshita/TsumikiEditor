using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 半精度float版Vector3
public struct Vector2h
{
	public HalfFloat x, y;

	public Vector2h(Vector3 vec) {
		this.x = (HalfFloat)vec.x;
		this.y = (HalfFloat)vec.y;
	}
	public Vector2h(float x, float y) {
		this.x = (HalfFloat)x;
		this.y = (HalfFloat)y;
	}
	
	public static implicit operator Vector2h(Vector2 rhs) {
		return new Vector2h(rhs);
	}

	public static Vector2h operator+(Vector2h lhs, Vector2h rhs) {
		return new Vector2h(lhs.x + rhs.x, lhs.y + rhs.y);
	}
}
