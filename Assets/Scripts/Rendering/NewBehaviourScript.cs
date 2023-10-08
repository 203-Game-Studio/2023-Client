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
        public System.UInt16 vertex_offset;
	    public System.UInt16 triangle_offset;
        public System.UInt16 vertex_count;
	    public System.UInt16 triangle_count;
    } 

    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshletsBound(Int64 index_count, Int64 max_vertices, Int64 max_triangles);
    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshlets(meshopt_Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, uint[] indices, Int64 index_count, float[] vertex_positions, 
        Int64 vertex_count, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, float cone_weight);
}

public class NewBehaviourScript : MonoBehaviour
{
    public Mesh mesh;
    // Start is called before the first frame update
    void Start()
    {
        const Int64 max_vertices = 64;
        const Int64 max_triangles = 124;
        const float cone_weight = 1.0f;
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
            vertices.Length, 3*4, max_vertices, max_triangles, cone_weight);
        for(int i = 0; i < meshlet_count; ++i){
            GameObject go = new GameObject(i.ToString());
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            Debug.LogError("-----------------------");
            Debug.LogError($"vertex_count{meshlets[i].vertex_count}");
            Debug.LogError($"triangle_count{meshlets[i].triangle_count}");
            Debug.LogError("-----------------------");
            
            /*int triDataLen = 3 * 64;
            var index = new int[triDataLen];
            for(int j = 0; j < triDataLen; ++j){
                index[j] = obj.indexData[clusterInfo.indexStart + j];
            }
            mesh.vertices = obj.vertexData;
            mesh.triangles = index;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();*/
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
        
    }
}
