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
        //return;
        var obj = ClusterTool.LoadClusterObjectFromFile("fish_tigershark_001_c01_ShapeBlend");
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
