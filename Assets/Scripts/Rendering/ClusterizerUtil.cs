using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class ClusterizerUtil
{   
    [StructLayout(LayoutKind.Sequential)] 
    public struct Meshlet 
    { 
        public System.UInt32 vertexOffset;
	    public System.UInt32 triangleOffset;
        public System.UInt32 vertexCount;
	    public System.UInt32 triangleCount;

        /* bounding box, useful for frustum and occlusion culling */
        public Vector3 min;
        public Vector3 max;

        /* normal cone, useful for backface culling */
        public Vector3 cone_apex;
        public Vector3 cone_axis;
        public float cone_cutoff; /* = cos(angle/2) */
    } 

    [StructLayout(LayoutKind.Sequential)] 
    public struct MeshoptBounds
    {
        /* bounding box, useful for frustum and occlusion culling */
        public Vector3 min;
        public Vector3 max;

        /* normal cone, useful for backface culling */
        public Vector3 cone_apex;
        public Vector3 cone_axis;
        public float cone_cutoff; /* = cos(angle/2) */

	    /* normal cone axis and cutoff, stored in 8-bit SNORM format; decode using x/127.0 */
        public int cone_axis_cutoff_s8;
    }

    [DllImport("ClusterizerUtil")]
    private static extern Int64 meshopt_buildMeshletsBound(Int64 index_count, Int64 max_vertices, 
        Int64 max_triangles);
    [DllImport("ClusterizerUtil")]
    private static extern Int64 meshopt_buildMeshlets(Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, int[] indices, Int64 index_count, Vector3[] vertex_positions, 
        Int64 vertexCount, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, 
        float cone_weight);
    [DllImport("ClusterizerUtil")]
    private static extern MeshoptBounds meshopt_computeMeshletBounds(uint[] meshlet_vertices,
	    byte[] meshlet_triangles, Int64 triangle_count, float[] vertex_positions, Int64 vertex_count,
	    Int64 vertex_positions_stride);

    public struct MeshData
    {
        public long uuid;
        public Meshlet[] meshlets;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;
        public uint[] meshletTriangles;
        public uint[] meshletVertices;
    }

    private static MeshData BuildMeshlets(Mesh mesh){
        const Int64 maxVertices = 255;
        const Int64 maxTriangles = 64;
        const float coneWeight = 0.0f;

        //todo: support submesh
        int[] triangles = mesh.GetTriangles(0);
        List<Vector3> vertices = new List<Vector3>();
        mesh.GetVertices(vertices);

        Int64 maxMeshlets = meshopt_buildMeshletsBound(triangles.Length, maxVertices, maxTriangles);
        Meshlet[] meshlets = new Meshlet[maxMeshlets];
        uint[] meshletVertices = new uint[maxMeshlets * maxVertices];
        byte[] meshletTriangles = new byte[maxMeshlets * maxTriangles * 3];

        Int64 meshlet_count = meshopt_buildMeshlets(meshlets, meshletVertices, 
            meshletTriangles, triangles, triangles.Length, vertices.ToArray(), 
            vertices.Count, 3*4, maxVertices, maxTriangles, coneWeight);

        MeshData meshData = new MeshData();
        if(meshlet_count <= 0){
            Debug.LogError("Meshlet划分失败!");
            return meshData;
        }
        Array.Resize(ref meshlets, (int)meshlet_count);
        Meshlet last = meshlets.Last();
        Array.Resize(ref meshletVertices, (int)(last.vertexOffset + last.vertexCount));
        int meshletTrianglesCount = (int)(last.triangleOffset + ((last.triangleCount * 3 + 3) & ~3));
        uint[] meshletTrianglesUint = new uint[meshletTrianglesCount];
        for(int idx = 0; idx < meshletTrianglesCount; ++idx){
            meshletTrianglesUint[idx] = meshletTriangles[idx];
        }
        
        List<Vector3> normals = new List<Vector3>();
        mesh.GetNormals(normals);
        List<Vector4> tangents = new List<Vector4>();
        mesh.GetTangents(tangents);

        meshData.uuid = mesh.name.GetHashCode();
        meshData.vertices = vertices.ToArray();
        meshData.normals = normals.ToArray();
        meshData.tangents = tangents.ToArray();
        meshData.meshlets = meshlets;
        meshData.meshletTriangles = meshletTrianglesUint;
        meshData.meshletVertices = meshletVertices;
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
            data.meshletTriangles = BytesToStructArray<uint>(br.ReadBytes(br.ReadInt32()));
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