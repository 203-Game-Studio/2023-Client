using UnityEngine;

public class TextureViewer3D : MonoBehaviour
{
	[Range(0,1)]
	public float sliceDepth;
	Material material;

	void Start()
	{
		material = GetComponentInChildren<MeshRenderer>().material;
	}
	
	void Update()
	{
		material.SetFloat("sliceDepth", sliceDepth);
		material.SetTexture("DisplayTexture", FindObjectOfType<PlanetGenerator>().rawDensityTexture);
	}
}
