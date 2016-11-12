using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class FileMenu : MonoBehaviour
{
	public void NewButton_OnClick() {
		EditManager.Instance.Reset();
	}
	public void LoadButton_OnClick() {
		FileManager.Load();
	}
	public void SaveButton_OnClick() {
		FileManager.Save();
	}
	public void SaveAsButton_OnClick() {
		FileManager.SaveAs();
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
			E3DExporter.Export(path, 
				EditManager.Instance.CurrentLayer.GetBlockGroup(),
				EditManager.Instance.CurrentLayer.GetModelGroup());
		}
	}
}
