using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ModelPalette : MonoBehaviour
{
	public RectTransform nodePrefab;
	private List<Image> listItems = new List<Image>();
	public int value {get; private set;}

	void Start() {
		var viewport = this.transform.Find("Viewport");
		var content = viewport.transform.Find("Content") as RectTransform;
		
		const float buttonHeight = 30.0f;
		content.sizeDelta = new Vector2(content.sizeDelta.x, 1 * buttonHeight);
		Vector2 offsetPosition = new Vector2(0, content.rect.height / 2);
		
		for (int i = 0; i < 1; i++) {
			int index = i;

			var node = GameObject.Instantiate(nodePrefab) as RectTransform;
			node.SetParent(content, false);
			node.anchoredPosition = offsetPosition - new Vector2(0, buttonHeight / 2);
			node.sizeDelta = new Vector2(0, buttonHeight);
			offsetPosition.y -= buttonHeight;
			
			// アイテムルート
			var imageView = node.GetComponent<Image>();
			// ハイライト
			var highlightView = node.Find("Highlight").GetComponent<Image>();
			highlightView.enabled = false;
			// テキスト
			var textView = node.Find("Text").GetComponent<Text>();
			textView.text = "tree";
			// ボタン
			var button = node.GetComponent<Button>();
			button.onClick.AddListener(() => OnChoosedItem(index));
			
			this.listItems.Add(imageView);
		}

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
		EditManager.Instance.SetToolModel("tree");
	}
	
	void Update() {
		
	}
}
