using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShallowWater : MonoBehaviour
{
    [SerializeField] private Camera topCamera;
    [SerializeField] private Transform moveObject;
    [SerializeField] private float radius;
    [SerializeField] private Material waterMat;
    [SerializeField] private ComputeShader waterCS;
    private int mixHeightKernel;
    private int updateKernel;
    [SerializeField] private float damping = 1.0f;
    [SerializeField] private float alpha = 0.5f;
    [SerializeField] private float beta = 1.0f;

    private RenderTexture h1RT;
    private RenderTexture h2RT;
    private RenderTexture h3RT;
    private RenderTexture objectHeightRT;
    private CommandBuffer cmd;

    [SerializeField] private List<MeshRenderer> visibleList = new List<MeshRenderer>();
    private Material overrideMaterial;

    private void Awake()
    {
        overrideMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("John/HeightGen"));

        topCamera.transform.position = Vector3.zero;
        topCamera.orthographicSize = 5;
        topCamera.backgroundColor = Color.clear;
        cmd = new CommandBuffer();
        cmd.name = "Shallow Water";
        
        objectHeightRT = CreateRT(1024, RenderTextureFormat.RFloat);
        waterCS.SetTexture(mixHeightKernel, "_ObjectHeightMap", objectHeightRT);

        GetComponent<MeshFilter>().sharedMesh = waterMesh;

        h1RT = CreateRT(1024, RenderTextureFormat.R8);
        h2RT = CreateRT(1024, RenderTextureFormat.R8);
        h3RT = CreateRT(1024, RenderTextureFormat.R8);
        waterMat.SetTexture("_WaterHeightMap", h3RT);
        mixHeightKernel = waterCS.FindKernel("MixHeight");
        updateKernel = waterCS.FindKernel("Update");
        waterCS.SetTexture(mixHeightKernel,"_HeightTexture", h3RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture1", h1RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture2", h2RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture", h3RT);
    }

    private void Update()
    {
        cmd.Clear();
        cmd.SetRenderTarget(objectHeightRT);
        cmd.ClearRenderTarget(true, true, Color.clear);
        foreach(var renderer in visibleList)
        {
            cmd.DrawRenderer(renderer, overrideMaterial, 0, 0);
        }
        cmd.SetComputeFloatParam(waterCS, "_WaterHeight", transform.position.y);
        cmd.DispatchCompute(waterCS, mixHeightKernel, 1024/8, 1024/8, 1);
        topCamera.AddCommandBuffer(CameraEvent.BeforeDepthTexture, cmd);

        UpdateWater();
        UpdateMatrix();
    }

    private void UpdateWater(){
        Graphics.Blit(h2RT, h1RT);
        Graphics.Blit(h3RT, h2RT);
        waterCS.SetFloat("_Damping", damping);
        waterCS.SetFloat("_Alpha", alpha);
        waterCS.SetFloat("_Beta", beta);
        waterCS.Dispatch(updateKernel, 1024/8, 1024/8, 1);
    }

    private void UpdateMatrix(){
        Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
        Matrix4x4 projectMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
        Matrix4x4 matrixVP = projectMatrix * viewMatrix;
        Matrix4x4 invMatrixVP = matrixVP.inverse;
        var material = GetComponent<Renderer>().sharedMaterial;
        material.SetMatrix("_MatrixVP",matrixVP);
        material.SetMatrix("_MatrixInvVP",invMatrixVP);
    }

    private void OnDestroy()
    {
        h1RT.Release();
        h2RT.Release();
        h3RT.Release();
        objectHeightRT.Release();
    }

    private Mesh _waterMesh;
    private Mesh waterMesh{
        get{
            if(!_waterMesh){
                _waterMesh = CreatePlaneMesh(1024, 10);
            }
            return _waterMesh;
        }
    }

    Mesh CreatePlaneMesh(int count, float size) {
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

    RenderTexture CreateRT(int size, RenderTextureFormat renderTextureFormat){
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(size, size, renderTextureFormat,0,1);
        descriptor.autoGenerateMips = false;
        descriptor.enableRandomWrite = true;
        RenderTexture rt = new RenderTexture(descriptor);
        rt.filterMode = FilterMode.Point;
        rt.Create();
        return rt;
    }
}