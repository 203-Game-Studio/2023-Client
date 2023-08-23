using System.Collections.Generic;
using UnityEngine;

public class Grass
{
    //生成草的mesh
    private static Mesh grassMesh;
    public static void CreateGrassMesh(){
        grassMesh = new Mesh { name = "Grass" };
        float width = 0.5f;
        float height = 0.5f;
        float halfWidth = width/2;
        grassMesh.SetVertices(new List<Vector3>
        {
            new Vector3(-halfWidth, 0, 0.0f),
            new Vector3(-halfWidth,  height, 0.0f),
            new Vector3(halfWidth, 0, 0.0f),
            new Vector3(halfWidth,  height, 0.0f),
           
        });
        grassMesh.SetUVs(0, new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1),
        });
        grassMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3,}, MeshTopology.Triangles, 0, false);
        grassMesh.RecalculateNormals();
        grassMesh.UploadMeshData(true);
    }

    public static Mesh GrassMesh
    {
        get
        {
            if (grassMesh != null){
                return grassMesh;
            }
            CreateGrassMesh();
            return grassMesh;
        }
    }
}