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
			string path = Dialogs.ShowFileDialog("Save project file", filter, lastDirPath, true);
			if (Path.GetExtension(path) == "" && !File.Exists(path)) {
				Path.ChangeExtension(path, ".tkd");
			}
			return path;
		} else {
			return Dialogs.ShowFileDialog("Open project file", filter, lastDirPath, false);
		}
	}
	
	public enum ImportFormat {
		RB1,
	}
	public static string OpenImportDialog(ImportFormat format) {
		string filter;
		switch (format) {
		case ImportFormat.RB1:
			filter = "RB1 File (*.txt)\0*.txt\0\0";
			break;
		default:
			return null;
		}
		return Dialogs.ShowFileDialog("Import data", filter, lastDirPath, false);
	}

	public enum ExportFormat {
		OBJ, E3D
	}
	public static string OpenExportDialog(ExportFormat format) {
		string filter;
		switch (format) {
		case ExportFormat.OBJ:
			filter = "Wavefront OBJ File (*.obj)\0*.obj\0\0";
			break;
		case ExportFormat.E3D:
			filter = "E3D Model Format v3 File (*.e3d)\0*.e3d\0\0";
			break;
		default:
			return null;
		}
		return Dialogs.ShowFileDialog("Export data", filter, lastDirPath, true);
	}

	public static void Reset() {
		currentFilePath = null;
	}

	public static bool Load() {
		string path = OpenDialog(false);
		if (!String.IsNullOrEmpty(path)) {
			return Load(path);
		}
		return false;
	}

	public static bool Save() {
		string path = FileManager.currentFilePath;
		if (!String.IsNullOrEmpty(path)) {
			return Save(path);
		} else {
			return SaveAs();
		}
	}

	public static bool SaveAs() {
		string path = OpenDialog(true);
		if (!String.IsNullOrEmpty(path)) {
			return Save(path);
		}
		return false;
	}

	public static bool Load(string filePath) {
		var xml = new XmlDocument();
		xml.Load(filePath);
		EditManager.Instance.Reset();
		
		var rootList = xml.GetElementsByTagName("tsumiki");
		
		var root = rootList[0] as XmlElement;
		string versionString = root.GetAttribute("version");
		int version = (int)(decimal.Parse(versionString) * 100);
		
		var layerList = root.GetElementsByTagName("layer");
		for (int i = 0; i < layerList.Count; i++) {
			var layerNode = layerList[i] as XmlElement;
			var layerName = layerNode.GetAttribute("name");
			var layer = EditManager.Instance.FindLayer(layerName);
			if (layerNode.HasAttribute("texture")) {
				string textureName = layerNode.GetAttribute("texture");
				TexturePalette.Instance.SetTexture(textureName);
			}
			layer.Deserialize(layerNode);
		}

		if (version <= 100) {
			foreach (var layer in EditManager.Instance.Layers) {
				foreach (var block in layer.GetAllBlocks()) {
					block.SetDirection((BlockDirection)((int)block.direction ^ 1));
				}
			}
		}

		// パス情報の保存
		var routepathList = root.GetElementsByTagName("routepath");
		if (routepathList.Count > 0) {
			var routepathNode = routepathList[0] as XmlElement;
			EditManager.Instance.RoutePath.Clear();
			EditManager.Instance.RoutePath.Deserialize(routepathNode);
		}
		currentFilePath = filePath;
		
		EditManager.Instance.OnDataSaved();
		return true;
	}
	
	public static bool Save(string filePath) {
		var writer = new XmlTextWriter(filePath, Encoding.UTF8);
		writer.Formatting = Formatting.Indented;

		var xml = new XmlDocument();
		
		var root = xml.CreateElement("tsumiki");
		xml.AppendChild(root);

		root.SetAttribute("version", "1.01");
		
		// ブロック情報の保存
		foreach (var layer in EditManager.Instance.Layers) {
			var layerNode = xml.CreateElement("layer");
			layerNode.SetAttribute("name", layer.gameObject.name);
			layerNode.SetAttribute("texture", TexturePalette.Instance.GetTextureName());
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
		
		currentFilePath = filePath;
		
		EditManager.Instance.OnDataSaved();
		return true;
	}
}
