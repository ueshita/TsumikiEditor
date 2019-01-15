using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ModelPalette : MonoBehaviour
{
	const float buttonHeight = 30.0f;
	public static ModelPalette Instance;
	
	public RectTransform nodePrefab;
	private List<Image> listItems = new List<Image>();
	private List<string> modelNames = new List<string>();
	public int value {get; private set;}
	
	public void AddModel(string modelName, string displayName) {
		var viewport = this.transform.Find("Viewport");
		var content = viewport.transform.Find("Content") as RectTransform;
		
		int index = this.listItems.Count;
		content.sizeDelta = new Vector2(content.sizeDelta.x, (index + 1) * buttonHeight);
		
		var node = GameObject.Instantiate(nodePrefab) as RectTransform;
		node.SetParent(content, false);
		node.anchoredPosition = new Vector2(0, -buttonHeight / 2 - index * buttonHeight);
		node.sizeDelta = new Vector2(0, buttonHeight);
			
		// アイテムルート
		var imageView = node.GetComponent<Image>();
		// ハイライト
		var highlightView = node.Find("Highlight").GetComponent<Image>();
		highlightView.enabled = false;
		// テキスト
		var textView = node.Find("Text").GetComponent<Text>();
		textView.text = displayName;
		// ボタン
		var button = node.GetComponent<Button>();
		button.onClick.AddListener(() => OnChoosedItem(index));
			
		this.listItems.Add(imageView);
		this.modelNames.Add(modelName);
	}
	
	void Awake() {
		Instance = this;
	}

	void Start() {
		this.OnChoosedItem(0);
	}

	// パレットのモデルが選択された
	void OnChoosedItem(int index) {
		
		// ハイライトを切り替える
		if (this.value >= 0 && this.value < this.listItems.Count) {
			this.listItems[this.value].transform.Find("Highlight").GetComponent<Image>().enabled = false;
		}
		this.value = index;
		if (this.value >= 0 && this.value < this.listItems.Count) {
			this.listItems[this.value].transform.Find("Highlight").GetComponent<Image>().enabled = true;
		}
		
		// セット
		EditManager.Instance.SetToolModel(this.modelNames[index]);
	}
}
