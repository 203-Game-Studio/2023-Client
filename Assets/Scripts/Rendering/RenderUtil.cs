using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RenderUtil
{
    public static Mesh CreatePlaneMesh(int count, float size) {
		Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        float gridSize = size / count;
        float halfSize = size * 0.5f;
        float oneOverCount = 1.0f / count;
		var vertices = new Vector3[(count + 1) * (count + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
		for (int i = 0, y = 0; y <= count; y++) {
			for (int x = 0; x <= count; x++, i++) {
				vertices[i] = new Vector3(x * gridSize - halfSize, 0, y * gridSize - halfSize);
				uvs[i] = new Vector2(x * oneOverCount, y * oneOverCount);
			}
		}
        mesh.SetVertices(vertices);
        mesh.SetUVs(0,uvs);

		int[] indices = new int[count * count * 6];
		for (int ti = 0, vi = 0, y = 0; y < count; y++, vi++) {
			for (int x = 0; x < count; x++, ti += 6, vi++) {
				indices[ti] = vi;
				indices[ti + 3] = indices[ti + 2] = vi + 1;
				indices[ti + 4] = indices[ti + 1] = vi + count + 1;
				indices[ti + 5] = vi + count + 2;
			}
		}
        mesh.SetIndices(indices,MeshTopology.Triangles,0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        return mesh;
	}

    public static RenderTexture CreateRT(int size, RenderTextureFormat renderTextureFormat){
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(size, size, renderTextureFormat,0,1);
        descriptor.autoGenerateMips = false;
        descriptor.enableRandomWrite = true;
        RenderTexture rt = new RenderTexture(descriptor);
        rt.filterMode = FilterMode.Point;
        rt.Create();
        return rt;
    }
}
