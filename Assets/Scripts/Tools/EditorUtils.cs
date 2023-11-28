using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class EditorUtils : MonoBehaviour
{
    
    [MenuItem("Tools/资源整理/刷错误材质")]
    static void SetSceneLitAndBigWorldLitTextureFormat() {
        string shaderPath = "Standard,Hidden/InternalErrorShader";
        string[] shaderPathArray = shaderPath.Split(',');
        string materialType = "*.mat";
        List<string> matrialPathList = new List<string>();
        List<Material> materialList = new List<Material>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        
        string[] shaderPathStrArray = Directory.GetFiles("Assets/",materialType, SearchOption.AllDirectories);
        for(int j = 0; j < shaderPathStrArray.Length; ++j)
        {
            matrialPathList.Add(shaderPathStrArray[j]);
        }
 
        for (int i = 0; i < matrialPathList.Count; ++i)
        {
            EditorUtility.DisplayProgressBar("Checking", $"Current Material : {matrialPathList[i]} {i}/{matrialPathList.Count}",
                (float)i/ matrialPathList.Count);

            try {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matrialPathList[i]);
                for (int j = 0; j < shaderPathArray.Length; ++j) {
                    if (mat.shader.name == shaderPathArray[j])
                    {
                        Debug.Log($"{mat.name}--{mat.shader.name}");
                        mat.shader = shader;
                    }
                }
            }
            catch (System.Exception e){
                EditorUtility.ClearProgressBar();
                Debug.LogError(e);
                break;
            }
        }
        EditorUtility.ClearProgressBar();
    }
}
