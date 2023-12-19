using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FFTOcean : MonoBehaviour
{
    [SerializeField] private ComputeShader oceanCS;
    [SerializeField] [Range(3, 14)]private int fftPow = 10;
    [SerializeField] [Header("波浪高度")] private float a = 10;
    [SerializeField] [Header("偏移大小")]private float lambda = -1;
    [SerializeField] [Header("高度影响")]private float heightScale = 1;
    [SerializeField] [Header("泡沫强度")]private float bubblesScale = 1;
    [SerializeField] [Header("泡沫阈值")]private float bubblesThreshold = 1;
    [SerializeField] [Header("风强")]private float windScale = 2;
    [SerializeField] [Header("风")]private Vector4 windAndSeed = new Vector4(0.1f, 0.2f, 0, 0);
    private int fftSize;
    private Material oceanMaterial;
    private int kernelCreateHeightSpectrum;
    private int kernelComputeGaussianRandom;
    private RenderTexture gaussianRandomRT;//高斯随机数
    private RenderTexture heightSpectrumRT;//高度频谱

    private void Awake()
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = waterMesh;
        oceanMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;

        fftSize = (int)Mathf.Pow(2, fftPow);
        gaussianRandomRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        heightSpectrumRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);

        kernelComputeGaussianRandom = oceanCS.FindKernel("ComputeGaussianRandom");
        kernelCreateHeightSpectrum = oceanCS.FindKernel("CreateHeightSpectrum");

        oceanCS.SetInt("_N", fftSize);
        oceanCS.SetFloat("_OceanLength", 10);

        //生成高斯随机数
        oceanCS.SetTexture(kernelComputeGaussianRandom, "_GaussianRandomRT", gaussianRandomRT);
        oceanCS.Dispatch(kernelComputeGaussianRandom, fftSize / 8, fftSize / 8, 1);
    }

    private void Update()
    {
        oceanCS.SetFloat("_A", a);
        windAndSeed.z = Random.Range(1, 10f);
        windAndSeed.w = Random.Range(1, 10f);
        Vector2 wind = new Vector2(windAndSeed.x, windAndSeed.y);
        wind.Normalize();
        wind *= windScale;
        oceanCS.SetVector("_WindAndSeed", new Vector4(wind.x, wind.y, windAndSeed.z, windAndSeed.w));
        oceanCS.SetFloat("_Time", Time.time);
        oceanCS.SetFloat("_Lambda", lambda);
        oceanCS.SetFloat("_HeightScale", heightScale);
        oceanCS.SetFloat("_BubblesScale", bubblesScale);
        oceanCS.SetFloat("_BubblesThreshold", bubblesThreshold);

        //生成高度频谱
        oceanCS.SetTexture(kernelCreateHeightSpectrum, "_GaussianRandomRT", gaussianRandomRT);
        oceanCS.SetTexture(kernelCreateHeightSpectrum, "_HeightSpectrumRT", heightSpectrumRT);
        oceanCS.Dispatch(kernelCreateHeightSpectrum, fftSize / 8, fftSize / 8, 1);

        oceanMaterial.SetTexture("_HeightSpectrumRT", heightSpectrumRT);
    }

    private void OnDestroy()
    {
        gaussianRandomRT.Release();
        heightSpectrumRT.Release();
    }

    private Mesh _waterMesh;
    private Mesh waterMesh{
        get{
            if(!_waterMesh){
                _waterMesh = RenderUtil.CreatePlaneMesh(1024, 10);
            }
            return _waterMesh;
        }
    }
}