#ifndef RENDER_OBJECT_SHADOW_CASTER_PASS_INCLUDED
#define RENDER_OBJECT_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

StructuredBuffer<InstanceData> _InstanceDataBuffer;
StructuredBuffer<float3> _VerticesBuffer;
StructuredBuffer<uint> _MeshletVerticesBuffer;
StructuredBuffer<uint> _MeshletTrianglesBuffer;
StructuredBuffer<Meshlet> _ShadowCullingResult;

struct Attributes
{
    float4 positionOS   : POSITION;
    uint vertID         : SV_VertexID;
    uint insID          : SV_InstanceID;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
};

float4 GetShadowPositionHClip(Attributes input)
{
    Meshlet meshlet = _ShadowCullingResult[input.insID];
    uint index = _MeshletTrianglesBuffer[meshlet.triangleOffset + input.vertID];
    float3 vertex = _VerticesBuffer[_MeshletVerticesBuffer[meshlet.vertexOffset + index]];
    InstanceData insData = _InstanceDataBuffer[0];
    unity_ObjectToWorld = insData.objectToWorldMatrix;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(vertex);
    float4 positionCS = positionInputs.positionCS;

    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;

    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return 0;
}

#endif
