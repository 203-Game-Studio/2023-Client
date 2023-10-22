using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public MaterialPropertyBlock block;
    
    void Start1()
    {
        ClusterizerUtil.BakeMeshDataToFile(mesh);
        return;
        /*block = new MaterialPropertyBlock();
        var data = ClusterizerUtil.LoadMeshDataFromFile("default");

        for(int i = 0;i< data.meshlets.Length;++i){
            var meshlet = data.meshlets[i];
            GameObject go = new GameObject();
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            Debug.LogError("" + meshlet.triangleCount);

            int fixedCount = Mathf.CeilToInt(meshlet.triangleCount/3.0f)*3;
            int[] curIndices = new int[fixedCount*3];
            for(int j = 0; j < meshlet.triangleCount; ++j){
                curIndices[j*3] = data.meshletTriangles[meshlet.triangleOffset + j*3];
                curIndices[j*3+1] = data.meshletTriangles[meshlet.triangleOffset + j*3+1];
                curIndices[j*3+2] = data.meshletTriangles[meshlet.triangleOffset + j*3+2];
            }
            for(int j = (int)meshlet.triangleCount; j < fixedCount; ++j){
                curIndices[j*3] = data.meshletTriangles[meshlet.triangleOffset];
                curIndices[j*3+1] = data.meshletTriangles[meshlet.triangleOffset];
                curIndices[j*3+2] = data.meshletTriangles[meshlet.triangleOffset];
            }

            Vector3[] curVertices = new Vector3[meshlet.vertexCount];
            for(int j = 0; j < meshlet.vertexCount; ++j){
                curVertices[j] = data.vertices[(int)data.meshletVertices[meshlet.vertexOffset + j]];
            }

            mesh.vertices = curVertices;
            mesh.triangles = curIndices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();
            rederer.sharedMaterial = material;
            block.SetColor("_BaseColor", new Color(UnityEngine.Random.Range(0, 1.0f),
                UnityEngine.Random.Range(0, 1.0f),UnityEngine.Random.Range(0, 1.0f)));
            rederer.SetPropertyBlock(block);
        }*/
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            Start1();
        }
    }
}
