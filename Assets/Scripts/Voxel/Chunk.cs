using UnityEngine;
using System.Collections.Generic;
using SimplexNoise;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(MeshFilter))]
public class Chunk : MonoBehaviour
{
    public static List<Chunk> chunks = new List<Chunk>();
    public static int width = 100;
    public static int height = 100;

    public int seed;
    public float baseHeight = 10;
    public float frequency = 0.025f;
    public float amplitude = 1;

    bool[,,] map;
    Mesh chunkMesh;
    MeshCollider meshCollider;
    MeshFilter meshFilter;

    Vector3 offset0;
    Vector3 offset1;
    Vector3 offset2;

    public static Chunk GetChunk(Vector3 wPos)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            Vector3 tempPos = chunks[i].transform.position;

            //wPos是否超出了Chunk的XZ平面的范围
            if ((wPos.x < tempPos.x) || (wPos.z < tempPos.z) || (wPos.x >= tempPos.x + 20) || (wPos.z >= tempPos.z + 20))
                continue;

            return chunks[i];
        }
        return null;
    }


    void Start()
    {
        chunks.Add(this);

        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        InitMap();
    }

    void InitMap()
    {
        //随机种子
        Random.InitState(seed);
        offset0 = new Vector3(Random.value * 1000, Random.value * 1000, Random.value * 1000);
        offset1 = new Vector3(Random.value * 1000, Random.value * 1000, Random.value * 1000);
        offset2 = new Vector3(Random.value * 1000, Random.value * 1000, Random.value * 1000);

        map = new bool[width, height, width];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    map[x, y, z] = GenerateBlock(new Vector3(x, y, z) + transform.position);
                }
            }
        }

        //Build网格
        BuildChunk();
    }

    //噪声生成高度值
    int GenerateHeight(Vector3 wPos)
    {
        float x0 = (wPos.x + offset0.x) * frequency;
        float y0 = (wPos.y + offset0.y) * frequency;
        float z0 = (wPos.z + offset0.z) * frequency;

        float x1 = (wPos.x + offset1.x) * frequency * 2;
        float y1 = (wPos.y + offset1.y) * frequency * 2;
        float z1 = (wPos.z + offset1.z) * frequency * 2;

        float x2 = (wPos.x + offset2.x) * frequency / 4;
        float y2 = (wPos.y + offset2.y) * frequency / 4;
        float z2 = (wPos.z + offset2.z) * frequency / 4;

        float noise0 = Noise.Generate(x0, y0, z0) * amplitude;
        float noise1 = Noise.Generate(x1, y1, z1) * amplitude / 2;
        float noise2 = Noise.Generate(x2, y2, z2) * amplitude / 4;

        return Mathf.FloorToInt(noise0 + noise1 + noise2 + baseHeight);
    }

    bool GenerateBlock(Vector3 wPos)
    {
        //高度限制
        /*if (wPos.y >= height)
        {
            return false;
        }*/

        //位置高于高度值时，为空
        return wPos.y < GenerateHeight(wPos);
    }

    public void BuildChunk()
    {
        chunkMesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    BuildBlock(x, y, z, verts, tris);
                }
            }
        }

        chunkMesh.vertices = verts.ToArray();
        chunkMesh.triangles = tris.ToArray();
        chunkMesh.RecalculateBounds();
        chunkMesh.RecalculateNormals();

        meshFilter.mesh = chunkMesh;
        meshCollider.sharedMesh = chunkMesh;
    }

    void BuildBlock(int x, int y, int z, List<Vector3> verts, List<int> tris)
    {
        if (!map[x, y, z]) return;

        bool typeid = map[x, y, z];

        //Left
        if (CheckNeedBuildFace(x - 1, y, z))
            BuildFace(typeid, new Vector3(x, y, z), Vector3.up, Vector3.forward, false, verts, tris);
        //Right
        if (CheckNeedBuildFace(x + 1, y, z))
            BuildFace(typeid, new Vector3(x + 1, y, z), Vector3.up, Vector3.forward, true, verts, tris);

        //Bottom
        if (CheckNeedBuildFace(x, y - 1, z))
            BuildFace(typeid, new Vector3(x, y, z), Vector3.forward, Vector3.right, false, verts, tris);
        //Top
        if (CheckNeedBuildFace(x, y + 1, z))
            BuildFace(typeid, new Vector3(x, y + 1, z), Vector3.forward, Vector3.right, true, verts, tris);

        //Back
        if (CheckNeedBuildFace(x, y, z - 1))
            BuildFace(typeid, new Vector3(x, y, z), Vector3.up, Vector3.right, true, verts, tris);
        //Front
        if (CheckNeedBuildFace(x, y, z + 1))
            BuildFace(typeid, new Vector3(x, y, z + 1), Vector3.up, Vector3.right, false, verts, tris);
    }

    bool CheckNeedBuildFace(int x, int y, int z)
    {
        //if (y < 0) return false;
        return !GetBlockType(x, y, z);
    }

    public bool GetBlockType(int x, int y, int z)
    {
    //    if (y < 0 || y > height - 1)
    //    {
    //        return false;
    //    }

        return GenerateBlock(new Vector3(x, y, z) + transform.position);
    }

    void BuildFace(bool typeid, Vector3 corner, Vector3 up, Vector3 right, bool reversed, List<Vector3> verts, List<int> tris)
    {
        int index = verts.Count;

        verts.Add(corner);
        verts.Add(corner + up);
        verts.Add(corner + up + right);
        verts.Add(corner + right);

        if (reversed)
        {
            tris.Add(index + 0);
            tris.Add(index + 1);
            tris.Add(index + 2);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 0);
        }
        else
        {
            tris.Add(index + 1);
            tris.Add(index + 0);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 2);
            tris.Add(index + 0);
        }
    }
}


