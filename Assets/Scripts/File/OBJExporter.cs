using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml;
using System.Text;

// Alias Wavefront OBJフォーマット向けのエクスポータ 
public static class OBJExporter
{
	public static void Export(string path, BlockGroup blockGroup) {
		var blockMeshes = new BlockMeshMerger();

		foreach (var block in blockGroup.GetAllBlocks()) {
			block.WriteToMesh(blockGroup, blockMeshes);
		}
		
		string dataName = Path.GetFileNameWithoutExtension(path);

		var writer = new StringWriter();
		writer.WriteLine("#Created by Tsumiki Editor");
		writer.WriteLine("");
		writer.WriteLine("mtllib " + dataName + ".mtl");
		writer.WriteLine("");

		// Output Vertices
		foreach (Vector3 v in blockMeshes.vertexPos) {
			writer.WriteLine("v " + v.x + " " + v.y + " " + v.z);
		}
		writer.WriteLine("# " + blockMeshes.vertexPos.Count + " vertices");

		// Output Texture Vertices
		foreach (Vector2 vt in blockMeshes.vertexUv) {
			writer.WriteLine("vt " + vt.x + " " + (1.0f - vt.y));
		}
		writer.WriteLine("# " + blockMeshes.vertexUv.Count + " texture vertices");
		
		// Output Faces
		writer.WriteLine("usemtl mat1");
		int facesCount = blockMeshes.triangles.Count / 3;
		for (int i = 0; i < facesCount; i++) {
			int i0 = blockMeshes.triangles[i * 3 + 0] + 1;
			int i1 = blockMeshes.triangles[i * 3 + 1] + 1;
			int i2 = blockMeshes.triangles[i * 3 + 2] + 1;
			writer.WriteLine("f " + i0 + "/" + i0 + " " + i1 + "/" + i1 + " " + i2 + "/" + i2);
		}
		writer.WriteLine("# " + blockMeshes.vertexUv.Count + " texture vertices");
		
		File.WriteAllText(path, writer.ToString(), Encoding.ASCII);
		writer.Dispose();

		// Output Materials
		var mtlWriter = new StringWriter();
		mtlWriter.WriteLine("newmtl mat1");
		mtlWriter.WriteLine("Ka 0.00000 0.00000 0.00000");
		mtlWriter.WriteLine("Kd 0.00000 0.00000 0.00000");
		mtlWriter.WriteLine("Ks 0.00000 0.00000 0.00000");
		mtlWriter.WriteLine("Ns 0.00000");

		File.WriteAllText(Path.ChangeExtension(path, ".mtl"), mtlWriter.ToString(), Encoding.ASCII);
		mtlWriter.Dispose();
	}
}
