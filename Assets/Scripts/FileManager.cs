using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;
using System.Windows.Forms;

public static class FileManager
{
	public static string lastDirPath {get; private set;}
	public static string currentFilePath {get; private set;}
	
	public static string OpenDialog(bool isSave) {
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
				lastDirPath = Path.GetDirectoryName(fd.FileName);
				return fd.FileName;
			}
			return null;
		}
	}

	public static string OpenExportDialog() {
		FileDialog fd;
		fd = new SaveFileDialog();
		fd.Title = "Export data";
		using (fd) {
			fd.InitialDirectory = (lastDirPath != null) ? lastDirPath : "";
			fd.Filter = "Alias Wavefront OBJ File (*.obj)|*.obj";
			if (fd.ShowDialog() == DialogResult.OK) {
				lastDirPath = Path.GetDirectoryName(fd.FileName);
				return fd.FileName;
			}
			return null;
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
		currentFilePath = filePath;
	}
	
	public static void Save(string filePath) {
		var writer = new XmlTextWriter(filePath, Encoding.UTF8);
		writer.Formatting = Formatting.Indented;

		var xml = new XmlDocument();
		
		var root = xml.CreateElement("tsumiki");
		xml.AppendChild(root);

		root.SetAttribute("version", "1.00");
		
		foreach (var layer in EditManager.Instance.Layers) {
			var layerNode = xml.CreateElement("layer");
			layerNode.SetAttribute("name", layer.gameObject.name);
			layer.Serialize(layerNode);
			root.AppendChild(layerNode);
		}

		xml.WriteTo(writer);

		writer.Flush();
		writer.Close();
	}
}
