using System.Collections.Generic;
using UnityEngine;

public class ShallowWater : MonoBehaviour
{
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
        h1RT = CreateRT(1024);
        h2RT = CreateRT(1024);
        h3RT = CreateRT(1024);
        waterMat.SetTexture("_WaterHeightMap", h3RT);
        initKernel = waterCS.FindKernel("Init");
        updateKernel = waterCS.FindKernel("Update");
        waterCS.SetTexture(initKernel,"_HeightTexture", h3RT);
        waterCS.Dispatch(initKernel, 1024/8, 1024/8, 1);
        waterCS.SetTexture(updateKernel,"_HeightTexture1", h1RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture2", h2RT);
        waterCS.SetTexture(updateKernel,"_HeightTexture", h3RT);
    }

    private void Update()
    {
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
                _waterMesh = CreatePlaneMesh(256);
            }
            return _waterMesh;
        }
    }

    Mesh CreatePlaneMesh(int size){
        var mesh = new Mesh();

        var sizePerGrid = 0.5f;
        var totalMeterSize = size * sizePerGrid;
        var gridCount = size * size;
        var triangleCount = gridCount * 2;

        var vOffset = - totalMeterSize * 0.5f;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        float uvStrip = 1f / size;
        for(var z = 0; z <= size;z ++){
            for(var x = 0; x <= size; x ++){
                vertices.Add(new Vector3(vOffset + x * 0.5f,0,vOffset + z * 0.5f));
                uvs.Add(new Vector2(x * uvStrip,z * uvStrip));
            }
        }
        mesh.SetVertices(vertices);
        mesh.SetUVs(0,uvs);

        int[] indices = new int[triangleCount * 3];

        for(var gridIndex = 0; gridIndex < gridCount ; gridIndex ++){
            var offset = gridIndex * 6;
            var vIndex = (gridIndex / size) * (size + 1) + (gridIndex % size);

            indices[offset] = vIndex;
            indices[offset + 1] = vIndex + size + 1;
            indices[offset + 2] = vIndex + 1;
            indices[offset + 3] = vIndex + 1; 
            indices[offset + 4] = vIndex + size + 1;
            indices[offset + 5] = vIndex + size + 2;
        }
        mesh.SetIndices(indices,MeshTopology.Triangles,0);
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