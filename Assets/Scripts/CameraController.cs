using UnityEngine;
using System.Collections;
using System;

public class CameraController : MonoBehaviour
{
	private float elevation = 30.0f;		// 仰角
	private float azimuth   = 180.0f;		// 方位角
	private float distance  = 12.0f;		// 注視点からの距離
	
	private float lockedElevation = 30.0f;	// 回転開始時の方位角
	private float lockedAzimuth   = 30.0f;	// 回転開始時の仰角
	private Vector3 rotationMousePos;

	private Vector3 lockedPosition;
	private Vector3 moveMousePos;
	private Vector3 targetPosition = Vector3.zero;
	
	public void OnBeginDrag(int button, Vector2 position) {
		if (button == 1) {
			// 回転開始
			this.lockedElevation = this.elevation;
			this.lockedAzimuth   = this.azimuth;
			this.rotationMousePos = Input.mousePosition;
		} else if (button == 2) {
			// 平行移動開始
			this.lockedPosition = this.targetPosition;
			this.moveMousePos = Input.mousePosition;
		}
	}
	
	public void OnEndDrag(int button, Vector2 position) {
		if (button == 1) {
			// 特になにもしない
		} else if (button == 2) {
			// 特になにもしない
		}
	}

	public void OnDrag(int button, Vector2 position) {
		if (button == 1) {
			// 回転中
			Vector3 dif = Input.mousePosition - this.rotationMousePos;
			this.azimuth   = this.lockedAzimuth   + dif.x * 0.5f;
			this.elevation = this.lockedElevation - dif.y * 0.5f;
			this.elevation = Mathf.Clamp(this.elevation, -90.0f + 0.1f, 90.0f - 0.1f);
		} else if (button == 2) {
			// 平行移動中
			Vector3 move = Input.mousePosition - this.moveMousePos;
			Vector3 dif = this.targetPosition - this.transform.position;
			Vector3 front = dif.normalized;
			float distance = dif.magnitude;
			
			Vector3 right  = Vector3.Cross(front, Vector3.up).normalized;
			Vector3 upward = Vector3.Cross(front, right).normalized;

			this.targetPosition = this.lockedPosition + 
				(right * move.x + upward * move.y) * distance * 0.001f;
		}
	}

	public void OnScroll(Vector2 delta) {
		this.distance -= delta.y * 1.0f;
		this.distance = Mathf.Clamp(this.distance, 2.0f, 40.0f);
	}

	void LateUpdate() {
		this.transform.position = this.CalcPosition();
		this.transform.LookAt(this.targetPosition);
	}
	
	Vector3 CalcPosition() {
		Quaternion qu = Quaternion.Euler(-this.elevation, this.azimuth, 0.0f);
		Matrix4x4 mat = Matrix4x4.TRS(Vector3.zero, qu, Vector3.one);
		Vector3 relativePos = (mat * Vector3.forward) * this.distance;
		return this.targetPosition + relativePos;
	}
}
