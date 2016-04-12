using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;

public class EditFiler
{
	public EditFiler() {

	}
	
	public void Load(string filePath) {
		var xml = new XmlDocument();
		xml.Load(filePath);

		var rootList = xml.GetElementsByTagName("tsumiki");
		
		var root = rootList[0] as XmlElement;
		var layerList = root.GetElementsByTagName("layer");
		for (int i = 0; i < layerList.Count; i++) {
			var layerNode = layerList[i] as XmlElement;
			var layer = EditManager.Instance.AddLayer(layerNode.Name);
			layer.Deserialize(layerNode);
		}
	}
	
	public void Save(string filePath) {
		var writer = new XmlTextWriter(filePath, Encoding.UTF8);
		writer.Formatting = Formatting.Indented;

		var xml = new XmlDocument();
		
		var root = xml.CreateElement("tsumiki");
		xml.AppendChild(root);

		root.SetAttribute("version", "1.00.00");
		
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
