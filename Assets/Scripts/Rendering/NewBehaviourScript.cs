using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class ClusterizerUtil
{   
    [StructLayout(LayoutKind.Sequential)] 
    public struct meshopt_Meshlet 
    { 
        public System.UInt32 vertex_offset;
	    public System.UInt32 triangle_offset;
        public System.UInt32 vertex_count;
	    public System.UInt32 triangle_count;
    } 

    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshletsBound(Int64 index_count, Int64 max_vertices, Int64 max_triangles);
    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshlets(meshopt_Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, uint[] indices, Int64 index_count, float[] vertex_positions, 
        Int64 vertex_count, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, float cone_weight);
    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshlets2(meshopt_Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, uint[] indices, Int64 index_count, float[] vertex_positions, 
        Int64 vertex_count, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, float cone_weight);
}

public class NewBehaviourScript : MonoBehaviour
{
    public Mesh mesh;
    // Start is called before the first frame update
    void Start1()
    {
        const Int64 max_vertices = 64;
        const Int64 max_triangles = 124;
        const float cone_weight = 0.0f;
        int[] trianglesArray = mesh.GetTriangles(0);
        uint[] triangles = new uint[trianglesArray.Length];
        for(int i = 0; i < trianglesArray.Length; ++i){
            triangles[i] = (uint)trianglesArray[i];
        }
        List<Vector3> verticesList = new List<Vector3>();
        mesh.GetVertices(verticesList);
        float[] vertices = new float[verticesList.Count * 3];
        for(int i = 0; i < verticesList.Count; ++i){
            vertices[i*3] = verticesList[i].x;
            vertices[i*3+1] = verticesList[i].y;
            vertices[i*3+2] = verticesList[i].z;
        }

        Int64 max_meshlets = ClusterizerUtil.meshopt_buildMeshletsBound(triangles.Length, max_vertices, max_triangles);
        ClusterizerUtil.meshopt_Meshlet[] meshlets = new ClusterizerUtil.meshopt_Meshlet[max_meshlets];
        uint[] meshlet_vertices = new uint[max_meshlets * max_vertices];
        byte[] meshlet_triangles = new byte[max_meshlets * max_triangles * 3];

        Int64 meshlet_count = ClusterizerUtil.meshopt_buildMeshlets(meshlets, meshlet_vertices, 
            meshlet_triangles, triangles, triangles.Length, vertices, 
            verticesList.Count, 3*4, max_vertices, max_triangles, cone_weight);
        /*Debug.LogError($"meshlet_count {meshlet_count}");
        Debug.LogError($"triangles.Length {meshlets[0].vertex_offset}");
        Debug.LogError($"verticesList.Count {meshlets[0].triangle_offset}");
        Debug.LogError($"3*4 {meshlets[0].vertex_count}");
        Debug.LogError($"max_triangles {meshlets[0].triangle_count}");
        Debug.LogError($"max_triangles {meshlets[1].vertex_offset}");
        Debug.LogError($"cone_weight {meshlets[1].triangle_offset}");
        int a = -888;
        Debug.LogError($"-888 {meshlets[1].vertex_count} {(uint)a}");
        Debug.LogError($"-1000 {meshlets[1].triangle_count}");
        Debug.LogError($"1 {meshlet_vertices[0]}");
        Debug.LogError($"2 {meshlet_vertices[1]}");
        Debug.LogError($"3 {meshlet_vertices[2]}");
        Debug.LogError($"4 {meshlet_vertices[3]}");
        Debug.LogError($"5 {meshlet_vertices[4]}");
        Debug.LogError($"2 {meshlet_triangles[0]}");
        Debug.LogError($"3 {meshlet_triangles[1]}");
        Debug.LogError($"4 {meshlet_triangles[2]}");
        Debug.LogError($"5 {meshlet_triangles[3]}");
        Debug.LogError($"6 {meshlet_triangles[4]}");*/
        for(int i = 0; i < 1; ++i){
            GameObject go = new GameObject(i.ToString());
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();

            int meshVerticesCount = (int)meshlets[i].vertex_count/3;
            Vector3[] meshvertices = new Vector3[meshVerticesCount];
            for(int j = 0;j< meshVerticesCount;++j){
                meshvertices[j] = new Vector3(vertices[meshlet_vertices[meshlets[i].vertex_offset+j*3]]
                    ,vertices[meshlet_vertices[meshlets[i].vertex_offset+1+j*3]],
                    vertices[meshlet_vertices[meshlets[i].vertex_offset+2+j*3]]);
            }
            
            int count = Mathf.FloorToInt(meshlets[i].triangle_count/3.0f)*3;
            var index = new int[count];
            for(int j = 0; j < count; ++j){
                index[j] = meshlet_triangles[meshlets[i].triangle_offset + j];
            }
            mesh.vertices = meshvertices;
            mesh.triangles = index;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();
        }
        //ClusterTool.BakeClusterInfoToFile(mesh);
        //return;
        /*var obj = ClusterTool.LoadClusterObjectFromFile("fish_tigershark_001_c01_ShapeBlend");
        for(int i = 0; i < obj.clusterInfos.Length; ++i){
            var clusterInfo = obj.clusterInfos[i];
            GameObject go = new GameObject(obj.name + i);
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            int triDataLen = 3 * 64;
            var index = new int[triDataLen];
            for(int j = 0; j < triDataLen; ++j){
                index[j] = obj.indexData[clusterInfo.indexStart + j];
            }
            mesh.vertices = obj.vertexData;
            mesh.triangles = index;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            Start1();
        }
    }
}
