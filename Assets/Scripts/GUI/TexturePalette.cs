using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class TexturePalette : MonoBehaviour, IPointerClickHandler
{
	private readonly Vector2 tileSize = new Vector2(128, 128);
	private readonly Vector2 viewArea = new Vector2(112, 112);
	
	public static TexturePalette Instance {get; private set;}

	private Image image;
	private Image cursorImage;
	private Vector2 texSize;
	private int itemIndex;

	public class Chip {
		Vector2 uv1, uv2;
		
		public Chip(Vector2 uv1, Vector2 uv2) {
			this.uv1 = uv1;
			this.uv2 = uv2;
		}
		public Vector2 ApplyUV(Vector2 uv, int meshIndex, float height) {
			uv.y = 1.0f - uv.y;
			if (meshIndex >= (int)BlockDirection.Zplus && 
				meshIndex <= (int)BlockDirection.Xminus
			) {
				float uarea = uv2.x - uv1.x;
				float varea = uv2.y - uv1.y;
				if (height - Mathf.Floor(height) >= 0.5f) {
					return new Vector2(
						(uv1.x + uarea * uv.x), 
						(uv1.y + varea * 0.5f * uv.y));
				} else {
					return new Vector2(
						(uv1.x +  uarea * uv.x), 
						(uv1.y + varea * 0.5f * uv.y + varea * 0.5f));
				}
			} else {
				return new Vector2(
					uv1.x + (uv2.x - uv1.x) *  uv.x, 
					uv1.y + (uv2.y - uv1.y) *  uv.y);
			}
		}
	}

	public Chip GetChip(int itemIndex) {
		Vector2 offset = new Vector2(
			itemIndex % 100 * this.tileSize.x + this.tileSize.x * 0.5f, 
			itemIndex / 100 * this.tileSize.y + this.tileSize.y * 0.5f);
		Vector2 uv1, uv2;
		uv1.x =        (offset.x - this.viewArea.x * 0.5f) / this.texSize.x;
		uv1.y = 1.0f - (offset.y - this.viewArea.y * 0.5f) / this.texSize.y;
		uv2.x =        (offset.x + this.viewArea.x * 0.5f) / this.texSize.x;
		uv2.y = 1.0f - (offset.y + this.viewArea.y * 0.5f) / this.texSize.y;
		return new Chip(uv1, uv2);
	}

	void Awake() {
		if (Instance != null) {
			Debug.LogError("Illegal singleton in TexturePalette");
		}
		Instance = this;

		this.image = this.GetComponent<Image>();
		var cursorObj = this.image.rectTransform.FindChild("Cursor");
		this.cursorImage = cursorObj.GetComponent<Image>();
		this.texSize = new Vector2(image.material.mainTexture.width, image.material.mainTexture.height);
	}

	void Start() {
		this.SetItem(0);
	}

	// パレットのテクスチャが選択された
	public void SetItem(int itemIndex) {
		this.itemIndex = itemIndex;

		var rectTransform = this.image.rectTransform;
		Vector2 imageSize = rectTransform.rect.size;
		
		cursorImage.rectTransform.anchoredPosition = new Vector2(
			rectTransform.rect.xMin + (itemIndex % 100 * tileSize.x + tileSize.x / 2) / this.texSize.x * imageSize.x,
			rectTransform.rect.yMax - (itemIndex / 100 * tileSize.y + tileSize.y / 2) / this.texSize.y * imageSize.y);
	}

	public int GetItem() {
		return this.itemIndex;
	}
	
	public void OnPointerClick(PointerEventData eventData) {
		Vector2 point;
		var rectTransform = this.image.rectTransform;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			rectTransform, eventData.pressPosition, eventData.pressEventCamera, out point);
		
		Vector2 imageSize = rectTransform.rect.size;
		
		point.x =  point.x - rectTransform.rect.xMin;
		point.y = -point.y + rectTransform.rect.yMax;
		point.x *= this.texSize.x / imageSize.x;
		point.y *= this.texSize.y / imageSize.y;

		int index = (int)point.y / (int)tileSize.y * 100 + (int)point.x / (int)tileSize.x;
		this.SetItem(index);
	}
}
