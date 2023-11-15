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

    public GameObject go;

    private SkinnedMeshRenderer[] meshRenderers;
    private Animator animator;

    public float captureInterval = 0.1f;
    
    void Awake()
    {
        if(camera == null) camera = Camera.main;

        Screen.SetResolution(width, height,false);

        meshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        if(meshRenderers.Length == 0) return;
        animator = go.GetComponent<Animator>();

        StartCoroutine(ScreenShot());
    }

    IEnumerator ScreenShot(){
        yield return StartCoroutine(ScreenShotByShader(colorShader, "color"));
        yield return StartCoroutine(ScreenShotByShader(normalShader, "normal"));
        yield return StartCoroutine(ScreenShotByShader(metallicShader, "metallic"));
        ExportTxt2Ds();
    }

    IEnumerator ScreenShotByShader(Shader shader, string name){
        if (animator == null) yield break;

        foreach(var renderer in meshRenderers){
            renderer.material.shader = shader;
        }

        int currentFrame = 1;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            float frameRate = clip.frameRate;

            for (float t = 0; t < clip.length; t += captureInterval)
            {
                
                animator.Play(clip.name, 0, t / clip.length);
                yield return new WaitForEndOfFrame();

                ScreenShot($"{name}_{clip.name}_{currentFrame}");
                currentFrame++;
            }
        }
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
