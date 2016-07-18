using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;

public class Menu : MonoBehaviour
{
	public void NewButton_OnClick() {
		EditManager.Instance.Reset();
	}
	public void LoadButton_OnClick() {
		string path = FileManager.OpenDialog(false);
		if (!String.IsNullOrEmpty(path)) {
			FileManager.Load(path);
		}
	}
	public void SaveButton_OnClick() {
		string path = FileManager.currentFilePath;
		if (!String.IsNullOrEmpty(path)) {
			FileManager.Save(path);
		} else {
			this.SaveAsButton_OnClick();
		}
	}
	public void SaveAsButton_OnClick() {
		string path = FileManager.OpenDialog(true);
		if (!String.IsNullOrEmpty(path)) {
			FileManager.Save(path);
		}
	}
	public void ExportOBJButton_OnClick() {
		string path = FileManager.OpenExportDialog(FileManager.ExportFormat.OBJ);
		if (!String.IsNullOrEmpty(path)) {
			OBJExporter.Export(path, EditManager.Instance.CurrentLayer.GetBlockGroup());
		}
	}
	public void ExportE3DButton_OnClick() {
		string path = FileManager.OpenExportDialog(FileManager.ExportFormat.E3D);
		if (!String.IsNullOrEmpty(path)) {
			E3DExporter.Export(path, EditManager.Instance.CurrentLayer.GetBlockGroup());
		}
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
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.PointSelector);
	}
	public void ToolRectSelect_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.RectSelector);
	}
	public void ToolRoutePath_OnValueChanged(Toggle tgl) {
		if (tgl.isOn) EditManager.Instance.SetTool(EditManager.Tool.RoutePath);
	}
}
