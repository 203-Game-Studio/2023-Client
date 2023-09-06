using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
	public int numChunks = 4;

	public int numPointsPerAxis = 10;
	public float boundsSize = 10;

	public float noiseScale;
	public float noiseHeightMultiplier;

	public ComputeShader densityCompute;
	public ComputeShader copy3DCompute;

	[HideInInspector] public RenderTexture rawDensityTexture;
	[HideInInspector] public RenderTexture processedDensityTexture;
	RenderTexture originalMap;

	void Start()
	{
		InitTextures();
		ComputeDensity();
		CreateRenderTexture3D(ref originalMap, processedDensityTexture);
	}

	void InitTextures()
	{
		int size = numChunks * (numPointsPerAxis - 1) + 1;
		Create3DTexture(ref rawDensityTexture, size, "Raw Density Texture");
		Create3DTexture(ref processedDensityTexture, size, "Processed Density Texture");

		processedDensityTexture = rawDensityTexture;

		densityCompute.SetTexture(0, "DensityTexture", rawDensityTexture);
	}

	void ComputeDensity()
	{
		int textureSize = rawDensityTexture.width;

		densityCompute.SetInt("textureSize", textureSize);

		densityCompute.SetFloat("planetSize", boundsSize);
		densityCompute.SetFloat("noiseHeightMultiplier", noiseHeightMultiplier);
		densityCompute.SetFloat("noiseScale", noiseScale);

		Dispatch(densityCompute, textureSize, textureSize, textureSize);
	}

	void Create3DTexture(ref RenderTexture texture, int size, string name)
	{
		var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
		if (texture == null || !texture.IsCreated() || texture.width != size || texture.height != size || texture.volumeDepth != size || texture.graphicsFormat != format)
		{
			if (texture != null)
			{
				texture.Release();
			}
			const int numBitsInDepthBuffer = 0;
			texture = new RenderTexture(size, size, numBitsInDepthBuffer);
			texture.graphicsFormat = format;
			texture.volumeDepth = size;
			texture.enableRandomWrite = true;
			texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;


			texture.Create();
		}
		texture.wrapMode = TextureWrapMode.Repeat;
		texture.filterMode = FilterMode.Bilinear;
		texture.name = name;
	}

    void CreateRenderTexture3D(ref RenderTexture texture, RenderTexture template)
	{
		if (texture != null)
		{
			texture.Release();
		}
		texture = new RenderTexture(template.descriptor);
		texture.enableRandomWrite = true;
		texture.Create();
	}
    
    void LoadComputeShader(ref ComputeShader shader, string name)
	{
		if (shader == null)
		{
			shader = (ComputeShader)Resources.Load(name);
		}
	}

    void CopyRenderTexture3D(Texture source, RenderTexture target)
	{
		LoadComputeShader(ref copy3DCompute, "Copy3D");
		copy3DCompute.SetInts("dimensions", target.width, target.height, target.volumeDepth);
		copy3DCompute.SetTexture(0, "Source", source);
		copy3DCompute.SetTexture(0, "Target", target);
		Dispatch(copy3DCompute, target.width, target.height, target.volumeDepth);//
	}

    void Dispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1, int kernelIndex = 0)
	{
		Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
		int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
		int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
		int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.y);
		cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
	}
    
    Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex = 0)
	{
		uint x, y, z;
		compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
		return new Vector3Int((int)x, (int)y, (int)z);
	}
}
