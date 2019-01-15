using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Image))]
public class TexturePalette : MonoBehaviour, IPointerClickHandler
{
	private readonly Vector2 tileSize = new Vector2(128, 128);
	private readonly Vector2 viewArea = new Vector2(112, 112);
	
	public static TexturePalette Instance {get; private set;}
	
	private RectTransform contentRect;
	private Image paletteImage;
	private Image cursorImage;
	public Text textureLabel;
	private List<string> selectableTextures;
	private string textureName;
	private List<Texture2D> textureList = new List<Texture2D>();
	private Vector2 textureSize;
	private int itemIndex;

	public class Chip {
		Vector2 uv1, uv2;
		
		public Chip(Vector2 uv1, Vector2 uv2) {
			this.uv1 = uv1;
			this.uv2 = uv2;
		}
		public Vector2 ApplyUV(Vector2 uv, bool divideChipVert, float height) {
			while (uv.y > 1.0f) uv.y -= 1.0f;
			while (uv.y < 0.0f) uv.y += 1.0f;
			uv.y = 1.0f - uv.y;
			if (divideChipVert) {
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
		uv1.x =        (offset.x - this.viewArea.x * 0.5f) / this.textureSize.x;
		uv1.y = 1.0f - (offset.y - this.viewArea.y * 0.5f) / this.textureSize.y;
		uv2.x =        (offset.x + this.viewArea.x * 0.5f) / this.textureSize.x;
		uv2.y = 1.0f - (offset.y + this.viewArea.y * 0.5f) / this.textureSize.y;
		return new Chip(uv1, uv2);
	}

	private string TextureDirectory {
		get {
			return Application.streamingAssetsPath + "/MapTextures";
		}
	}

	private Texture2D AddTexture(string fileName) {
		string path = this.TextureDirectory + "/" + fileName;
		Texture2D texture = EditUtil.LoadTextureFromFile(path);
		if (texture != null) {
			texture.name = fileName;
			this.textureList.Add(texture);
		}
		return texture;
	}

	public void SetTexture(string textureName) {
		this.textureList.Clear();
		this.AddTexture(textureName + ".png");
		this.AddTexture(textureName + "_emissive.png");
		
		Texture2D texture = this.textureList[0];
		this.contentRect.sizeDelta = new Vector2(texture.width * 0.3f, texture.height * 0.3f);
		this.paletteImage.material.mainTexture = texture;
		this.paletteImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

		this.textureSize = new Vector2(texture.width, texture.height);
		this.textureName = textureName;

		textureLabel.text = this.textureName;
		EditManager.Instance.OnTextureChanged();
	}

	public Texture2D GetTexture(int index) {
		return (index < this.textureList.Count) ?
			this.textureList[index] : null;
	}

	public int GetNumTextures() {
		return this.textureList.Count;
	}

	public string GetTextureName() {
		return this.textureName;
	}

	void Awake() {
		if (Instance != null) {
			Debug.LogError("Illegal singleton in TexturePalette");
		}
		Instance = this;

		this.contentRect = this.transform.parent.GetComponent<RectTransform>();
		this.paletteImage = this.GetComponent<Image>();
		var cursorObj = this.paletteImage.rectTransform.Find("Cursor");
		this.cursorImage = cursorObj.GetComponent<Image>();

		this.selectableTextures = new List<string>();
		foreach (string path in Directory.GetFiles(this.TextureDirectory)) {
			string extension = Path.GetExtension(path);
			if (extension != ".png") {
				continue;
			}
			string fileName = Path.GetFileNameWithoutExtension(path);
			if (fileName.LastIndexOf("_emissive") >= 0) {
				continue;
			}
			this.selectableTextures.Add(fileName);
		}
	}

	void Start() {
		this.SetTexture(this.selectableTextures[0]);
		this.SetItem(0);
	}

	// パレットのテクスチャが選択された
	public void SetItem(int itemIndex) {
		this.itemIndex = itemIndex;

		var rectTransform = this.paletteImage.rectTransform;
		Vector2 imageSize = rectTransform.rect.size;
		
		cursorImage.rectTransform.anchoredPosition = new Vector2(
			rectTransform.rect.xMin + (itemIndex % 100 * tileSize.x + tileSize.x / 2) / this.textureSize.x * imageSize.x,
			rectTransform.rect.yMax - (itemIndex / 100 * tileSize.y + tileSize.y / 2) / this.textureSize.y * imageSize.y);
	}

	public int GetItem() {
		return this.itemIndex;
	}
	
	public void OnPointerClick(PointerEventData eventData) {
		Vector2 point;
		var rectTransform = this.paletteImage.rectTransform;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			rectTransform, eventData.pressPosition, eventData.pressEventCamera, out point);
		
		Vector2 imageSize = rectTransform.rect.size;
		
		point.x =  point.x - rectTransform.rect.xMin;
		point.y = -point.y + rectTransform.rect.yMax;
		point.x *= this.textureSize.x / imageSize.x;
		point.y *= this.textureSize.y / imageSize.y;

		int index = (int)point.y / (int)tileSize.y * 100 + (int)point.x / (int)tileSize.x;
		this.SetItem(index);
	}

	public void OnPrevClick() {
		int currentIndex = this.selectableTextures.IndexOf(this.textureName);
		if (--currentIndex < 0) {
			currentIndex = this.selectableTextures.Count - 1;
		}
		this.SetTexture(this.selectableTextures[currentIndex]);
	}

	public void OnNextClick() {
		int currentIndex = this.selectableTextures.IndexOf(this.textureName);
		if (++currentIndex >= this.selectableTextures.Count) {
			currentIndex = 0;
		}
		this.SetTexture(this.selectableTextures[currentIndex]);
	}
}
