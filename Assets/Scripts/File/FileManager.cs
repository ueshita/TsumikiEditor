using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;
using System;

public static class FileManager
{
	public static string lastDirPath {get; private set;}
	public static string currentFilePath {get; private set;}
	
	public static string OpenDialog(bool isSave) {
        const string filter = "Tsumiki Design file (*.tkd)\0*.tkd\0All files (*.*)\0*.*\0\0";
		
		if (isSave) {
			return Dialogs.ShowFileDialog("Save project file", filter, lastDirPath, true);
		} else {
			return Dialogs.ShowFileDialog("Open project file", filter, lastDirPath, false);
		}
	}

	public enum ExportFormat {
		OBJ, E3D
	}
	public static string OpenExportDialog(ExportFormat format) {
		string filter;
		switch (format) {
		case ExportFormat.OBJ:
			filter = "Alias Wavefront OBJ File (*.obj)\0*.obj\0\0";
			break;
		case ExportFormat.E3D:
			filter = "E3D Model Format v3 File (*.e3d)\0*.e3d\0\0";
			break;
		default:
			return null;
		}
		return Dialogs.ShowFileDialog("Export data", filter, lastDirPath, true);
	}

	public static void Load() {
		string path = OpenDialog(false);
		if (!String.IsNullOrEmpty(path)) {
			Load(path);
		}
	}

	public static void Save() {
		string path = FileManager.currentFilePath;
		if (!String.IsNullOrEmpty(path)) {
			Save(path);
		} else {
			SaveAs();
		}
	}

	public static void SaveAs() {
		string path = OpenDialog(true);
		if (!String.IsNullOrEmpty(path)) {
			Save(path);
		}
	}

	public static void Load(string filePath) {
		var xml = new XmlDocument();
		xml.Load(filePath);
		EditManager.Instance.Clear();
		
		var rootList = xml.GetElementsByTagName("tsumiki");
		
		var root = rootList[0] as XmlElement;
		var layerList = root.GetElementsByTagName("layer");
		for (int i = 0; i < layerList.Count; i++) {
			var layerNode = layerList[i] as XmlElement;
			var layer = EditManager.Instance.AddLayer(layerNode.Name);
			layer.Deserialize(layerNode);
		}

		// パス情報の保存
		var routepathList = root.GetElementsByTagName("routepath");
		if (routepathList.Count > 0) {
			var routepathNode = routepathList[0] as XmlElement;
			EditManager.Instance.RoutePath.Clear();
			EditManager.Instance.RoutePath.Deserialize(routepathNode);
		}
		currentFilePath = filePath;
	}
	
	public static void Save(string filePath) {
		var writer = new XmlTextWriter(filePath, Encoding.UTF8);
		writer.Formatting = Formatting.Indented;

		var xml = new XmlDocument();
		
		var root = xml.CreateElement("tsumiki");
		xml.AppendChild(root);

		root.SetAttribute("version", "1.00");
		
		// ブロック情報の保存
		foreach (var layer in EditManager.Instance.Layers) {
			var layerNode = xml.CreateElement("layer");
			layerNode.SetAttribute("name", layer.gameObject.name);
			layer.Serialize(layerNode);
			root.AppendChild(layerNode);
		}

		// パス情報の保存
		{
			var routePathNode = xml.CreateElement("routepath");
			EditManager.Instance.RoutePath.Serialize(routePathNode);
			root.AppendChild(routePathNode);
		}

		xml.WriteTo(writer);

		writer.Flush();
		writer.Close();
	}
}
