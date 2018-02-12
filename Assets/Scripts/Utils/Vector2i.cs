using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// int版Vector3
public struct Vector2i
{
	public int x, y;

	public Vector2i(Vector2 vec) {
		this.x = Mathf.RoundToInt(vec.x);
		this.y = Mathf.RoundToInt(vec.y);
	}
	public Vector2i(float x, float y) {
		this.x = Mathf.RoundToInt(x);
		this.y = Mathf.RoundToInt(y);
	}
	
	public static implicit operator Vector2i(Vector2 rhs) {
		return new Vector2i(rhs);
	}
	
	public static Vector2i operator+(Vector2i lhs, Vector2i rhs) {
		return new Vector2i(lhs.x + rhs.x, lhs.y + rhs.y);
	}
}
