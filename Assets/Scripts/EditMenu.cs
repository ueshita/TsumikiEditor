using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;

public class EditMenu : MonoBehaviour
{
	private string lastDirPath = null;

	public void NewButton_OnClick() {
		EditManager.Instance.Reset();
	}
	public void LoadButton_OnClick() {
		//try {
			string path = this.FileDialog(false);
			if (!String.IsNullOrEmpty(path)) {
				EditManager.Instance.Load(path);
			}
		/*} catch (Exception e) {
			MessageBox.Show(e.Message, "Load error.", 
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}*/
	}
	public void SaveButton_OnClick() {
		//try {
			string path = EditManager.Instance.FilePath;
			if (!String.IsNullOrEmpty(path)) {
				EditManager.Instance.Save(path);
			} else {
				this.SaveAsButton_OnClick();
			}
		/*} catch (Exception e) {
			MessageBox.Show(e.Message, "Save error.", 
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}*/
	}
	public void SaveAsButton_OnClick() {
		//try {
			string path = this.FileDialog(true);
			if (!String.IsNullOrEmpty(path)) {
				EditManager.Instance.Save(path);
			}
		/*} catch (Exception e) {
			MessageBox.Show(e.Message, "Save error.", 
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}*/
	}

	public void ToolPen_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Pen);
	}
	public void ToolEraser_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Eraser);
	}
	public void ToolBrush_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Brush);
	}
	public void ToolSpuit_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.Spuit);
	}
	public void ToolPointSelect_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.PointSelect);
	}
	public void ToolRectSelect_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.RectSelect);
	}

	public string FileDialog(bool isSave) {
		FileDialog fd;
		if (isSave) {
			fd = new SaveFileDialog();
			fd.Title = "Save file";
		} else {
			fd = new OpenFileDialog();
			fd.Title = "Open file";
		}
		using (fd) {
			fd.InitialDirectory = (lastDirPath != null) ? lastDirPath : "";
			fd.Filter = "Tsumiki Design file (*.tkd)|*.tkd|All files (*.*)|*.*";
			if (fd.ShowDialog() == DialogResult.OK) {
				this.lastDirPath = Path.GetDirectoryName(fd.FileName);
				return fd.FileName;
			}
			return null;
		}
	}
}
