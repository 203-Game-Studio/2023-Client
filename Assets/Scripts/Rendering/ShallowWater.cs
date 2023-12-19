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
    private RenderTexture obstacleMaskRT;
    private CommandBuffer cmd;

    [SerializeField] private List<MeshRenderer> visibleList = new List<MeshRenderer>();
    [SerializeField] private List<MeshRenderer> obstacleList = new List<MeshRenderer>();
    private Material overrideMaterial;

    private void Awake()
    {
        overrideMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("John/HeightGen"));

        topCamera.transform.position = Vector3.zero;
        topCamera.orthographicSize = 5;
        topCamera.backgroundColor = Color.clear;
        cmd = new CommandBuffer();
        cmd.name = "Shallow Water";
        
        objectHeightRT = RenderUtil.CreateRT(1024, RenderTextureFormat.RFloat);
        waterCS.SetTexture(mixHeightKernel, "_ObjectHeightMap", objectHeightRT);

        GetComponent<MeshFilter>().sharedMesh = waterMesh;

        h1RT = RenderUtil.CreateRT(1024, RenderTextureFormat.R8);
        h2RT = RenderUtil.CreateRT(1024, RenderTextureFormat.R8);
        h3RT = RenderUtil.CreateRT(1024, RenderTextureFormat.R8);
        obstacleMaskRT = RenderUtil.CreateRT(1024, RenderTextureFormat.R8);
        waterMat.SetTexture("_WaterHeightMap", h3RT);
        mixHeightKernel = waterCS.FindKernel("MixHeight");
        updateKernel = waterCS.FindKernel("Update");
        waterCS.SetTexture(mixHeightKernel, "_HeightTexture", h3RT);
        waterCS.SetTexture(updateKernel, "_HeightTexture1", h1RT);
        waterCS.SetTexture(updateKernel, "_HeightTexture2", h2RT);
        waterCS.SetTexture(updateKernel, "_HeightTexture", h3RT);
        waterCS.SetTexture(updateKernel, "_ObstacleMaskTexture", obstacleMaskRT);
    }

    private void Update()
    {
        cmd.Clear();

        cmd.SetRenderTarget(objectHeightRT);
        cmd.ClearRenderTarget(true, true, Color.clear);
        foreach(var renderer in visibleList)
        {
            if(renderer == null) continue;
            cmd.DrawRenderer(renderer, overrideMaterial, 0, 0);
        }
        cmd.SetComputeFloatParam(waterCS, "_WaterHeight", transform.position.y);
        cmd.DispatchCompute(waterCS, mixHeightKernel, 1024/8, 1024/8, 1);
        
        cmd.SetRenderTarget(obstacleMaskRT);
        cmd.ClearRenderTarget(true, true, Color.clear);
        foreach(var renderer in obstacleList)
        {
            if(renderer == null) continue;
            cmd.DrawRenderer(renderer, overrideMaterial, 0, 1);
        }

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
                _waterMesh = RenderUtil.CreatePlaneMesh(1024, 10);
            }
            return _waterMesh;
        }
    }

    
}