using UnityEngine;
using System.Collections;

public class QuitDialog : MonoBehaviour
{
	public GameObject blindPanel;

	public void YesButton_OnClick() {
		this.returnFunc(FileManager.Save());
		this.Close();
	}

	public void NoButton_OnClick() {
		this.returnFunc(true);
		this.Close();
	}
	
	public void CancelButton_OnClick() {
		this.returnFunc(false);
		this.Close();
	}

	public delegate void ReturnFunc(bool result);
	private ReturnFunc returnFunc;

	public bool IsOpened() {
		return this.gameObject.activeSelf;
	}

	public void Open(ReturnFunc func) {
		this.returnFunc = func;
		if (blindPanel != null) blindPanel.SetActive(true);
		this.gameObject.SetActive(true);
	}
	
	public void Close() {
		if (blindPanel != null) blindPanel.SetActive(false);
		this.gameObject.SetActive(false);
	}
}
