using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public struct ClusterMesh{
    public Vector3 localBoundMin;
    public Vector3 localBoundMax;
    public int indexStart;
    public Vector4 localCone;
}

[Serializable]
public struct ClusterObject{
    public byte[] indexData;
    public byte[] vertexData;
    public ClusterMesh[] clusterMeshes;
}

public static class ClusterTool
{
    public static void GenerateClusterMesh(Mesh mesh, ref ClusterObject clusterObject){
        int[] triangles = mesh.GetTriangles(0);
        List<Vector3> vertices = new List<Vector3>();
        mesh.GetVertices(vertices);
        int clusterCount = Mathf.CeilToInt(triangles.Length / (3.0f * 64));
        ClusterMesh[] clusterMeshes = new ClusterMesh[clusterCount];
        List<Vector3> vertexDataList = new List<Vector3>();
        List<int> indexDataList = new List<int>();
        int idxStart = 0;
        for(int i = 0; i < clusterCount;++i){
            int triDataLen = 3 * 64;
            int[] tri = new int[triDataLen];
            if(i == clusterCount - 1){
                triDataLen = triangles.Length - 3 * 64 * i;
                for(int j = triDataLen; j < 3 * 64; ++j){
                    tri[j] = 0;
                }
            }
            for(int j = 0; j < triDataLen; ++j){
                tri[j] = triangles[i * 3 * 64 + j];
            }
            HashSet<Vector3> vertexSet = new HashSet<Vector3>();
            for(int j = 0; j < tri.Length; ++j){
                vertexSet.Add(vertices[tri[j]]);
            }
            List<Vector3> vert = new List<Vector3>();
            Dictionary<Vector3, int> vertexDic = new Dictionary<Vector3, int>();
            int idx = 0;
            foreach(var v in vertexSet){
                vert.Add(v);
                vertexDic.Add(v, idx++);
            }
            for(int j = 0; j < tri.Length; ++j){
                tri[j] = vertexDic[vertices[tri[j]]];
            }
            var bounds = GeometryUtility.CalculateBounds(vert.ToArray(), Matrix4x4.identity);
            vertexDataList.AddRange(vert);
            indexDataList.AddRange(tri);
            ClusterMesh clusterMesh = new ClusterMesh();
            clusterMesh.indexStart = idxStart;
            clusterMesh.localBoundMin = bounds.min;
            clusterMesh.localBoundMax = bounds.max;
            //todo: local cone cal
            idxStart += tri.Length;
            clusterMeshes[i] = clusterMesh;
        }
        clusterObject.clusterMeshes = clusterMeshes;
        clusterObject.vertexData = StructToBytes(vertexDataList);
        clusterObject.indexData = StructToBytes(indexDataList);
    }

    private static byte[] StructToBytes<T>(List<T> list) where T : struct {
        T t = list[0];
        Int32 size = Marshal.SizeOf(t);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        byte[] bytes = new byte[size * list.Count];
        try{
            int ptr = 0;
            foreach (T t2 in list) {
                Marshal.StructureToPtr(t2, buffer, false);
                Marshal.Copy(buffer, bytes, ptr, size);
                ptr += size;
            }
            return bytes;
        }
        finally{
            Marshal.FreeHGlobal(buffer);
        }
    }
}
