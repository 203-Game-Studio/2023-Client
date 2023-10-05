using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public struct ClusterInfo{
    public Vector3 localBoundMin;
    public Vector3 localBoundMax;
    public int indexStart;
    public int vertexStart;
    public int length;
    public Vector4 localCone;
}

[Serializable]
public struct ClusterObject{
    public string name;
    public int[] indexData;
    public Vector3[] vertexData;
    public ClusterInfo[] clusterInfos;
}

public static class ClusterTool
{
    public static void BakeClusterInfoToFile(Mesh mesh){
        int[] triangles = mesh.GetTriangles(0);
        List<Vector3> vertices = new List<Vector3>();
        mesh.GetVertices(vertices);
        int clusterCount = Mathf.CeilToInt(triangles.Length / (3.0f * 64));
        List<ClusterInfo> clusterInfos = new List<ClusterInfo>(clusterCount);
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
            ClusterInfo clusterInfo = new ClusterInfo();
            clusterInfo.indexStart = idxStart;
            clusterInfo.vertexStart = vertexDataList.Count;
            clusterInfo.length = vert.Count;
            clusterInfo.localBoundMin = bounds.min;
            clusterInfo.localBoundMax = bounds.max;
            indexDataList.AddRange(tri);
            vertexDataList.AddRange(vert);
            //todo: local cone cal
            idxStart += tri.Length;
            clusterInfos.Add(clusterInfo);
        }
        string filePath = $"{Application.dataPath}/Bytes/{mesh.name}_";
        SaveBytes(StructToBytes(vertexDataList), $"{filePath}VertexData.bytes");
        SaveBytes(StructToBytes(indexDataList), $"{filePath}IndexData.bytes");
        SaveBytes(StructToBytes(clusterInfos), $"{filePath}ClusterInfo.bytes");
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

    public static T[] BytesToStructArray<T>(Byte[] bytes)
    {
        Int32 size = Marshal.SizeOf(typeof(T));
        IntPtr buffer = Marshal.AllocHGlobal(size);
        int count = bytes.Length / size;
        T[] array = new T[count];
        try
        {
            for(int i = 0; i < count; i++){
                Marshal.Copy(bytes, i * size, buffer, size);
                array[i] = (T)Marshal.PtrToStructure(buffer, typeof(T));
            }
            return array;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void SaveBytes(byte[] bytes, string filePath){
        FileStream fs = new FileStream(filePath, FileMode.Create);
        fs.Write(bytes, 0, bytes.Length);
        fs.Close();
        fs.Dispose();
    }

    public static ClusterObject LoadClusterObjectFromFile(string name){
        string filePath = $"{name}_";
        ClusterObject clusterObject = new ClusterObject();
        var vertexBytes = Addressables.LoadAssetAsync<TextAsset>($"{filePath}VertexData").WaitForCompletion().bytes;
        clusterObject.vertexData = BytesToStructArray<Vector3>(vertexBytes);
        var indexBytes = Addressables.LoadAssetAsync<TextAsset>($"{filePath}IndexData").WaitForCompletion().bytes;
        clusterObject.indexData = BytesToStructArray<int>(indexBytes);
        var infoBytes = Addressables.LoadAssetAsync<TextAsset>($"{filePath}ClusterInfo").WaitForCompletion().bytes;
        clusterObject.clusterInfos = BytesToStructArray<ClusterInfo>(infoBytes);
        clusterObject.name = name;
        return clusterObject;
    }
}