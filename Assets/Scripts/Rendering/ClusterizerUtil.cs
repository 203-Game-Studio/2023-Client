using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ClusterizerUtil
{   
    [StructLayout(LayoutKind.Sequential)] 
    public struct Meshlet 
    { 
        public System.UInt32 vertexOffset;
	    public System.UInt32 triangleOffset;
        public System.UInt32 vertexCount;
	    public System.UInt32 triangleCount;
    } 

    [DllImport("ClusterizerUtil")]
    private static extern Int64 meshopt_buildMeshletsBound(Int64 index_count, Int64 max_vertices, Int64 max_triangles);
    [DllImport("ClusterizerUtil")]
    private static extern Int64 meshopt_buildMeshlets(Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, uint[] indices, Int64 index_count, float[] vertex_positions, 
        Int64 vertexCount, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, float cone_weight);

    public struct MeshData
    {
        public long uuid;
        public Meshlet[] meshlets;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;
        public byte[] meshletTriangles;
        public uint[] meshletVertices;
    }

    private static MeshData BuildMeshlets(Mesh mesh){
        const Int64 max_vertices = 255;
        const Int64 max_triangles = 64;
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
        ClusterizerUtil.Meshlet[] meshlets = new ClusterizerUtil.Meshlet[max_meshlets];
        uint[] meshlet_vertices = new uint[max_meshlets * max_vertices];
        byte[] meshlet_triangles = new byte[max_meshlets * max_triangles * 3];

        Int64 meshlet_count = ClusterizerUtil.meshopt_buildMeshlets(meshlets, meshlet_vertices, 
            meshlet_triangles, triangles, triangles.Length, vertices, 
            verticesList.Count, 3*4, max_vertices, max_triangles, cone_weight);

        ClusterizerUtil.Meshlet[] curMeshlets =  new ClusterizerUtil.Meshlet[meshlet_count];
        for(int i = 0; i < meshlet_count; ++i){
            curMeshlets[i] = meshlets[i];
        }
        
        List<Vector3> normalsList = new List<Vector3>();
        mesh.GetNormals(normalsList);

        List<Vector4> tangentsList = new List<Vector4>();
        mesh.GetTangents(tangentsList);

        MeshData meshData = new MeshData();
        meshData.uuid = mesh.name.GetHashCode();
        meshData.vertices = verticesList.ToArray();
        meshData.normals = normalsList.ToArray();
        meshData.tangents = tangentsList.ToArray();
        meshData.meshlets = curMeshlets;
        meshData.meshletTriangles = meshlet_triangles;
        meshData.meshletVertices = meshlet_vertices;
        return meshData;
    }

    public static bool BakeMeshDataToFile(Mesh mesh){
        var data = BuildMeshlets(mesh);
        
        string filePath = $"{Application.dataPath}/Bytes/{mesh.name}.bytes";
        try{
            using FileStream fs = new FileStream(filePath, FileMode.Create);
            using BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(data.uuid);

            var bytes = StructToBytes(data.meshlets);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(data.vertices);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(data.normals);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(data.tangents);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(data.meshletTriangles);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            bytes = StructToBytes(data.meshletVertices);
            bw.Write(bytes.Length);
            bw.Write(bytes);
            
            bw.Close();
            fs.Close();
        }
        catch (IOException e)
        {
           Debug.LogError(e.Message);
           return false;
        }
        return true;
    }

    public static MeshData LoadMeshDataFromFile(string name){
        string filePath = $"{Application.dataPath}/Bytes/{name}.bytes";
        MeshData data = new MeshData();
        try{
            using FileStream fs = new FileStream(filePath, FileMode.Open);
            using BinaryReader br = new BinaryReader(fs);
            data.uuid = br.ReadInt64();
            data.meshlets = BytesToStructArray<Meshlet>(br.ReadBytes(br.ReadInt32()));
            data.vertices = BytesToStructArray<Vector3>(br.ReadBytes(br.ReadInt32()));
            data.normals = BytesToStructArray<Vector3>(br.ReadBytes(br.ReadInt32()));
            data.tangents = BytesToStructArray<Vector4>(br.ReadBytes(br.ReadInt32()));
            data.meshletTriangles = br.ReadBytes(br.ReadInt32());
            data.meshletVertices = BytesToStructArray<uint>(br.ReadBytes(br.ReadInt32()));
            br.Close();
            fs.Close();
        }
        catch (IOException e)
        {
           Debug.LogError(e.Message);
        }
        return data;
    }

    private static byte[] StructToBytes<T>(T[] list) where T : struct {
        T t = list[0];
        Int32 size = Marshal.SizeOf(t);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        byte[] bytes = new byte[size * list.Length];
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

    public static T[] BytesToStructArray<T>(byte[] bytes) where T : struct 
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
}