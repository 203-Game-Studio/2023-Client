using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public Mesh mesh;
    // Start is called before the first frame update
    void Start()
    {
        //ClusterTool.BakeClusterInfoToFile(mesh);
        var obj = ClusterTool.LoadClusterObjectFromFile("fish_tigershark_001_c01_ShapeBlend");
        for(int i = 0; i < obj.clusterInfos.Length; ++i){
            var clusterInfo = obj.clusterInfos[i];
            GameObject go = new GameObject(obj.name + i);
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            int triDataLen = 3 * 64;
            var index = new int[triDataLen];
            for(int j = 0; j < triDataLen; ++j){
                index[j] = obj.indexData[i * 3 * 64 + j];
            }
            var vertex = new Vector3[clusterInfo.length];
            Debug.LogError("" + clusterInfo.length);
            int idx= 0;
            for(int j = clusterInfo.vertexStart; j < clusterInfo.vertexStart + clusterInfo.length; ++j){
                vertex[idx++] = obj.vertexData[j];
            }
            mesh.vertices = vertex;
            mesh.triangles = index;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
