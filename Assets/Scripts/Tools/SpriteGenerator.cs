using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SpriteGenerator : MonoBehaviour
{
    public Camera camera;
    public int width;
    public int height;

    public Shader colorShader;
    public Shader normalShader;
    public Shader metallicShader;

    public GameObject gameObject;

    private SkinnedMeshRenderer[] meshRenderers;
    
    void Awake()
    {
        if(camera == null) camera = Camera.main;

        Screen.SetResolution(width, height,false);

        meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        if(meshRenderers.Length == 0) return;

        ScreenShotByShader(colorShader, "color");
        ScreenShotByShader(normalShader, "normal");
        ScreenShotByShader(metallicShader, "metallic");
        ExportTxt2Ds();
    }

    void ScreenShotByShader(Shader shader, string name){
        foreach(var renderer in meshRenderers){
            renderer.material.shader = shader;
        }
        ScreenShot(name);
    }

    public Dictionary<string, Texture2D> outTxt2D = new Dictionary<string, Texture2D>();
    public void ScreenShot(string fileName)
    {
        fileName = $"{Application.dataPath}/Sprites/{fileName}.png";
        RenderTexture rt = new RenderTexture(width, height, 24);
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        camera.targetTexture = null;
        rt.Release();
        if (outTxt2D == null)
            outTxt2D = new Dictionary<string, Texture2D>();
        outTxt2D.Add(fileName, tex);
    }

    private void ExportTxt2Ds(){
        foreach (var item in outTxt2D){
            if (File.Exists(item.Key))
            {
                File.Delete(item.Key);
            }
            FileInfo fileInfo = new FileInfo(item.Key);
            File.WriteAllBytes (fileInfo.FullName, item.Value.EncodeToPNG());
            Debug.Log("图片导出-"+fileInfo.Name);
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
