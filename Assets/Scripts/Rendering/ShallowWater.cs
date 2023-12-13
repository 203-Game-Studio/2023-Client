using System.Collections.Generic;
using UnityEngine;

public class ShallowWater : MonoBehaviour
{
    [SerializeField] private Transform moveObject;
    [SerializeField] private float radius;
    [SerializeField] private Material waterMat;
    [SerializeField] private ComputeShader waterCS;
    private int initKernel;
    private int updateKernel;
    [SerializeField] private float damping = 1.0f;
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private float beta = 1.0f;

    private RenderTexture h1RT;
    private RenderTexture h2RT;
    private RenderTexture h3RT;

    private void Awake()
    {
        GetComponent<MeshFilter>().sharedMesh = waterMesh;

        h1RT = CreateRT(1024);
        h2RT = CreateRT(1024);
        h3RT = CreateRT(1024);
        waterMat.SetTexture("_WaterHeightMap", h3RT);
        initKernel = waterCS.FindKernel("Init");
        updateKernel = waterCS.FindKernel("Update");
        waterCS.SetTexture(initKernel,"_HeightTexture", h3RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture1", h1RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture2", h2RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture", h3RT);
    }

    private void Update()
    {
        Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
        Matrix4x4 projectMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
        Matrix4x4 matrixVP = projectMatrix * viewMatrix;
        Matrix4x4 invMatrixVP = matrixVP.inverse;
        var material = GetComponent<Renderer>().sharedMaterial;
        material.SetMatrix("_MatrixVP",matrixVP);
        material.SetMatrix("_MatrixInvVP",invMatrixVP);
        
        waterCS.SetVector("_ObjectPos", new Vector4(moveObject.position.x, moveObject.position.y, moveObject.position.z, radius));
        waterCS.Dispatch(initKernel, 1024/8, 1024/8, 1);

        Graphics.Blit(h2RT, h1RT);
        Graphics.Blit(h3RT, h2RT);
        waterCS.SetFloat("_Damping", damping);
        waterCS.SetFloat("_Alpha", alpha);
        waterCS.SetFloat("_Beta", beta);
        waterCS.Dispatch(updateKernel, 1024/8, 1024/8, 1);
    }

    private Mesh _waterMesh;
    private Mesh waterMesh{
        get{
            if(!_waterMesh){
                _waterMesh = CreatePlaneMesh(256, 10);
            }
            return _waterMesh;
        }
    }

    Mesh CreatePlaneMesh(int count, float size) {
		Mesh mesh = new Mesh();
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

    RenderTexture CreateRT(int size){
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(size, size, RenderTextureFormat.R8,0,1);
        descriptor.autoGenerateMips = false;
        descriptor.enableRandomWrite = true;
        RenderTexture rt = new RenderTexture(descriptor);
        rt.filterMode = FilterMode.Point;
        rt.Create();
        return rt;
    }
}