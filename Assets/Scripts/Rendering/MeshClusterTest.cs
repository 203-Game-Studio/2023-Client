using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshClusterTest : MonoBehaviour
{
    public List<Mesh> meshList;
    public Material material;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Mesh mesh in meshList){
            GenerateCluster(mesh);
        }
    }

    void GenerateCluster(Mesh mesh){
        int[] triangles = mesh.GetTriangles(0);
        List<Vector3> vertices = new List<Vector3>();
        mesh.GetVertices(vertices);
        /*for(int i = 0; i< triangles.Length; ++i){
            triangleDic.Add(i, new Vector3(vertices[triangles[i]], triangles[i+1], triangles[i+2]));
        }*/
        int clusterCount = Mathf.CeilToInt(triangles.Length / (3.0f * 64));
        for(int i = 0; i < clusterCount-1;++i){
            Mesh m = new Mesh();
            int[] tri = new int[3 * 64];
            for(int j = 0; j < 3*64; ++j){
                tri[j] = triangles[i * 3 * 64 + j];
            }
            List<Vector3> vert = new List<Vector3>();
            //Dictionary<int, Vector3> triangleDic = new Dictionary<int, Vector3>();
            //int idx = 0;
            for(int j = 0; j < tri.Length; ++j){
                vert.Add(vertices[tri[j]]);
                tri[j] = j;
                /*if(triangleDic.TryGetValue(tri[j], out Vector3 val)){
                    vert.Add(val);
                }else{
                    triangleDic.Add(tri[j], vertices[tri[j]]);
                }*/
            }
            m.SetVertices(vert);
            m.SetTriangles(tri, 0);
            GameObject go = new GameObject($"{i}", typeof(MeshRenderer), typeof(MeshFilter));
            go.GetComponent<MeshFilter>().sharedMesh = m;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            float r = UnityEngine.Random.Range(0, 1f);
            float g = UnityEngine.Random.Range(0, 1f);
            float b = UnityEngine.Random.Range(0, 1f);
            block.SetColor("_BaseColor", new Color(r,g,b,1));
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
