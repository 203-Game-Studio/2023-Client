using UnityEngine;

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
    private int kernelCreateDisplaceSpectrum;
    private int kernelFFTHorizontal;                 
    private int kernelFFTHorizontalEnd;              
    private int kernelFFTVertical;                   
    private int kernelFFTVerticalEnd;                
    private int kernelTextureGenerationDisplace;     
    private int kernelTextureGenerationNormalBubbles;
    private RenderTexture gaussianRandomRT;//高斯随机数
    private RenderTexture heightSpectrumRT;//高度频谱
    private RenderTexture displaceXSpectrumRT;//X偏移频谱
    private RenderTexture displaceZSpectrumRT;//Z偏移频谱
    private RenderTexture displaceRT;//偏移频谱
    private RenderTexture outputRT;  //临时储存输出纹理
    private RenderTexture normalRT;  //法线纹理
    private RenderTexture bubblesRT; //泡沫纹理

    private void Awake()
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = waterMesh;
        oceanMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;

        fftSize = (int)Mathf.Pow(2, fftPow);
        gaussianRandomRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        heightSpectrumRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        displaceXSpectrumRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        displaceZSpectrumRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        displaceRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        outputRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        normalRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);
        bubblesRT = RenderUtil.CreateRT(fftSize, RenderTextureFormat.ARGBFloat);

        kernelComputeGaussianRandom = oceanCS.FindKernel("ComputeGaussianRandom");
        kernelCreateHeightSpectrum = oceanCS.FindKernel("CreateHeightSpectrum");
        kernelCreateDisplaceSpectrum = oceanCS.FindKernel("CreateDisplaceSpectrum");
        kernelFFTHorizontal = oceanCS.FindKernel("FFTHorizontal");
        kernelFFTHorizontalEnd = oceanCS.FindKernel("FFTHorizontalEnd");
        kernelFFTVertical = oceanCS.FindKernel("FFTVertical");
        kernelFFTVerticalEnd = oceanCS.FindKernel("FFTVerticalEnd");
        kernelTextureGenerationDisplace = oceanCS.FindKernel("TextureGenerationDisplace");
        kernelTextureGenerationNormalBubbles = oceanCS.FindKernel("TextureGenerationNormalBubbles");

        oceanCS.SetInt("_N", fftSize);
        oceanCS.SetFloat("_OceanLength", 512);

        //高斯随机数
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

        //高度频谱
        oceanCS.SetTexture(kernelCreateHeightSpectrum, "_GaussianRandomRT", gaussianRandomRT);
        oceanCS.SetTexture(kernelCreateHeightSpectrum, "_HeightSpectrumRT", heightSpectrumRT);
        oceanCS.Dispatch(kernelCreateHeightSpectrum, fftSize / 8, fftSize / 8, 1);

        //偏移频谱
        oceanCS.SetTexture(kernelCreateDisplaceSpectrum, "_HeightSpectrumRT", heightSpectrumRT);
        oceanCS.SetTexture(kernelCreateDisplaceSpectrum, "_DisplaceXSpectrumRT", displaceXSpectrumRT);
        oceanCS.SetTexture(kernelCreateDisplaceSpectrum, "_DisplaceZSpectrumRT", displaceZSpectrumRT);
        oceanCS.Dispatch(kernelCreateDisplaceSpectrum, fftSize / 8, fftSize / 8, 1);

        //横向FFT
        for (int m = 1; m <= fftPow; m++)
        {
            int ns = (int)Mathf.Pow(2, m - 1);
            oceanCS.SetInt("_Ns", ns);
            if (m != fftPow)
            {
                ComputeFFT(kernelFFTHorizontal, ref heightSpectrumRT);
                ComputeFFT(kernelFFTHorizontal, ref displaceXSpectrumRT);
                ComputeFFT(kernelFFTHorizontal, ref displaceZSpectrumRT);
            }
            else
            {
                ComputeFFT(kernelFFTHorizontalEnd, ref heightSpectrumRT);
                ComputeFFT(kernelFFTHorizontalEnd, ref displaceXSpectrumRT);
                ComputeFFT(kernelFFTHorizontalEnd, ref displaceZSpectrumRT);
            }
        }
        
        //纵向FFT
        for (int m = 1; m <= fftPow; m++)
        {
            int ns = (int)Mathf.Pow(2, m - 1);
            oceanCS.SetInt("_Ns", ns);
            if (m != fftPow)
            {
                ComputeFFT(kernelFFTVertical, ref heightSpectrumRT);
                ComputeFFT(kernelFFTVertical, ref displaceXSpectrumRT);
                ComputeFFT(kernelFFTVertical, ref displaceZSpectrumRT);
            }
            else
            {
                ComputeFFT(kernelFFTVerticalEnd, ref heightSpectrumRT);
                ComputeFFT(kernelFFTVerticalEnd, ref displaceXSpectrumRT);
                ComputeFFT(kernelFFTVerticalEnd, ref displaceZSpectrumRT);
            }
        }

        //偏移
        oceanCS.SetTexture(kernelTextureGenerationDisplace, "_HeightSpectrumRT", heightSpectrumRT);
        oceanCS.SetTexture(kernelTextureGenerationDisplace, "_DisplaceXSpectrumRT", displaceXSpectrumRT);
        oceanCS.SetTexture(kernelTextureGenerationDisplace, "_DisplaceZSpectrumRT", displaceZSpectrumRT);
        oceanCS.SetTexture(kernelTextureGenerationDisplace, "_DisplaceRT", displaceRT);
        oceanCS.Dispatch(kernelTextureGenerationDisplace, fftSize / 8, fftSize / 8, 1);

        //法线和泡沫
        oceanCS.SetTexture(kernelTextureGenerationNormalBubbles, "_DisplaceRT", displaceRT);
        oceanCS.SetTexture(kernelTextureGenerationNormalBubbles, "_NormalRT", normalRT);
        oceanCS.SetTexture(kernelTextureGenerationNormalBubbles, "_BubblesRT", bubblesRT);
        oceanCS.Dispatch(kernelTextureGenerationNormalBubbles, fftSize / 8, fftSize / 8, 1);

        oceanMaterial.SetTexture("_DisplaceMap", displaceRT);
        oceanMaterial.SetTexture("_NormalMap", normalRT);
        oceanMaterial.SetTexture("_BubblesMap", bubblesRT);
    }

    private void OnDestroy()
    {
        gaussianRandomRT.Release();
        heightSpectrumRT.Release();
        displaceXSpectrumRT.Release();
        displaceZSpectrumRT.Release();
        displaceRT.Release();
        outputRT.Release();
        normalRT.Release();
        bubblesRT.Release();
    }

    //fft
    private void ComputeFFT(int kernel, ref RenderTexture input)
    {
        oceanCS.SetTexture(kernel, "_InputRT", input);
        oceanCS.SetTexture(kernel, "_OutputRT", outputRT);
        oceanCS.Dispatch(kernel, fftSize / 8, fftSize / 8, 1);

        RenderTexture rt = input;
        input = outputRT;
        outputRT = rt;
    }

    private Mesh _waterMesh;
    private Mesh waterMesh{
        get{
            if(!_waterMesh){
                _waterMesh = RenderUtil.CreatePlaneMesh(1024, 100);
            }
            return _waterMesh;
        }
    }
}