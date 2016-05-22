using UnityEngine;
using System.Collections;

public class MenuOpener : MonoBehaviour
{
	public enum HiddenLocation {
		Up, Down, Left, Right,
	}

	public RectTransform target;
	public HiddenLocation hiddenLocation = HiddenLocation.Down;
	public float animTime = 0.1f;
	public bool isOpened = false;

	private enum State {
		Closed,
		Opening,
		Opened,
		Closing,
	}
	
	private State state;
	private float currentTime = 0.0f;
	private float lastRatio = -1.0f;
	private Vector2 hiddenPos;
	private Vector2 shownPos;


	void Start() {
		this.shownPos = this.target.anchoredPosition;

		switch (this.hiddenLocation) {
		case HiddenLocation.Up:
			this.hiddenPos.x = this.target.anchoredPosition.x;
			this.hiddenPos.y = +this.target.rect.height;
			break;
		case HiddenLocation.Down:
			this.hiddenPos.x = this.target.anchoredPosition.x;
			this.hiddenPos.y = -this.target.rect.height;
			break;
		case HiddenLocation.Left:
			this.hiddenPos.x = -this.target.rect.width;
			this.hiddenPos.y = this.target.anchoredPosition.y;
			break;
		case HiddenLocation.Right:
			this.hiddenPos.x = +this.target.rect.width;
			this.hiddenPos.y = this.target.anchoredPosition.y;
			break;
		}
	}
	
	void Update() {
		switch (this.state) {
		case State.Opened:
			if (this.isOpened == false) {
				this.state = State.Closing;
			}
			break;
		case State.Closed:
			if (this.isOpened == true) {
				this.state = State.Opening;
			}
			break;
		default:
			break;
		}
		
		if (this.isOpened) {
			this.currentTime += Time.deltaTime;
			if (this.currentTime >= this.animTime) {
				this.currentTime = this.animTime;
				this.state = State.Opened;
			}
		} else {
			this.currentTime -= Time.deltaTime;
			if (this.currentTime <= 0.0f) {
				this.currentTime = 0.0f;
				this.state = State.Closed;
			}
		}

		float ratio = this.currentTime / this.animTime;
		if (ratio != this.lastRatio) {
			this.lastRatio = ratio;
			this.target.anchoredPosition = this.hiddenPos + (this.shownPos - this.hiddenPos) *
				Mathf.SmoothStep(0.0f, 1.0f, ratio);
		}
	}

	public void Trigger() {
		this.isOpened = !this.isOpened;
	}
}
