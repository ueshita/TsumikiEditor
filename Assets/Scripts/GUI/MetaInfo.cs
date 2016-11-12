using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class MetaInfo : MonoBehaviour
{
	public InputField infoText;
	private Block targetBlock;

	public bool IsFocused() {
		return this.infoText.isFocused;
	}

	public void SetBlock(Block block) {
		this.targetBlock = block;
		this.UpdateViews();
	}

	public void UpdateViews() {
		this.infoText.text = this.targetBlock.metaInfo;
	}

	public void InputField_OnEditEnd() {
		this.targetBlock.SetMetaInfo(this.infoText.text.Trim());
		this.UpdateViews();
	}
}
