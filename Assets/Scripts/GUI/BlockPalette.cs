using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BlockPalette : MonoBehaviour
{
	public GameObject nodePrefab;
	private List<Image> listItems = new List<Image>();
	public int value {get; private set;}

	void Start() {
		var viewport = this.transform.FindChild("Viewport");
		var content = viewport.transform.FindChild("Content") as RectTransform;
		Vector2 offsetPosition = new Vector2(0, content.sizeDelta.y / 2);

		for (int i = 0; i < BlockShape.palette.Length; i++) {
			int index = i;

			var node = Instantiate(nodePrefab).transform as RectTransform;
			node.SetParent(content);

			// アイテムルート
			var imageView = node.GetComponent<Image>();
			imageView.rectTransform.anchoredPosition = offsetPosition
				- new Vector2(0, imageView.rectTransform.sizeDelta.y / 2);
			offsetPosition.y -= imageView.rectTransform.sizeDelta.y;

			// ハイライト
			var highlightView = node.FindChild("Highlight").GetComponent<Image>();
			highlightView.enabled = false;
			// テキスト
			var textView = node.FindChild("Text").GetComponent<Text>();
			textView.text = BlockShape.palette[i].displayName;
			// ボタン
			var button = node.GetComponent<Button>();
			button.onClick.AddListener(() => OnChoosedItem(index));
			
			this.listItems.Add(imageView);
		}

		this.OnChoosedItem(0);
	}

	// パレットのブロックが選択された
	void OnChoosedItem(int index) {
		
		// ハイライトを切り替える
		if (this.value >= 0 && this.value < this.listItems.Count) {
			this.listItems[this.value].transform.FindChild("Highlight").GetComponent<Image>().enabled = false;
		}
		this.value = index;
		if (this.value >= 0 && this.value < this.listItems.Count) {
			this.listItems[this.value].transform.FindChild("Highlight").GetComponent<Image>().enabled = true;
		}
		
		// セット
		EditManager.Instance.SetToolBlock(BlockShape.palette[this.value].name);
	}
	
	void Update() {
		
	}
}
