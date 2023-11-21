#ifndef _SSAO_INCLUDED_
#define _SSAO_INCLUDED_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

static const int SAMPLE_COUNT = 12;

SAMPLER(sampler_BlitTexture);

half4 _SSAOParams;
half4 _CameraViewTopLeftCorner;

float4 _SourceSize;
float4 _ProjectionParams2;
float4 _CameraViewXExtent;
float4 _CameraViewYExtent;

half3 ReconstructViewPos(float2 uv, float linearEyeDepth) {
    uv.y = 1.0 - uv.y;

    float zScale = linearEyeDepth * _ProjectionParams2.x; 
    float3 viewPos = _CameraViewTopLeftCorner.xyz +
                     _CameraViewXExtent.xyz * uv.x + 
                     _CameraViewYExtent.xyz * uv.y;
    viewPos *= zScale;

    return half3(viewPos);
}

float Random(float2 p) {
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// 获取半球上随机一点
half3 PickSamplePoint(float2 uv, int sampleIndex, half rcpSampleCount, half3 normal) {
    half gn = InterleavedGradientNoise(uv * _ScreenParams.xy, sampleIndex);
    half u = frac(Random(half2(0.0, sampleIndex)) + gn) * 2.0 - 1.0;
    half theta = Random(half2(1.0, sampleIndex) + gn) * TWO_PI;
    half u2 = sqrt(1.0 - u * u);

    // 全球上随机一点
    half3 v = half3(u2 * cos(theta), u2 * sin(theta), u);
    // 随着采样次数越向外采样
    v *= sqrt(sampleIndex * rcpSampleCount); 

    // 半球上随机一点 逆半球法线翻转  确保v跟normal一个方向
    v = faceforward(v, -normal, v);

    // 缩放到[0, RADIUS]
    v *= _SSAOParams.y;

    return v;
}

half4 SSAOFrag(Varyings input) : SV_Target
{
    float rawDepth = SampleSceneDepth(input.texcoord);
    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    float3 normal = SampleSceneNormals(input.texcoord);
    float3 vpos = ReconstructViewPos(input.texcoord, linearDepth);

    const half rcpSampleCount = rcp(SAMPLE_COUNT);

    half ao = 0.0;

    UNITY_UNROLL
    for (int i = 0; i < SAMPLE_COUNT; i++) {
        half3 offset = PickSamplePoint(input.texcoord, i, rcpSampleCount, normal);
        half3 vpos2 = vpos + offset;

        half4 spos2 = mul(UNITY_MATRIX_VP, vpos2);
        half2 uv2 = half2(spos2.x, spos2.y * _ProjectionParams.x) / spos2.w * 0.5 + 0.5;

        float rawDepth2 = SampleSceneDepth(uv2);
        float linearDepth2 = LinearEyeDepth(rawDepth2, _ZBufferParams);

        half isOcclusion = abs(spos2.w - linearDepth2) < _SSAOParams.y ? 1.0 : 0.0;

        half3 difference = ReconstructViewPos(uv2, linearDepth2) - vpos;
        half inten = max(dot(difference, normal) - 0.004 * linearDepth, 0.0) * rcp(dot(difference, difference) + 0.0001);
        ao += inten * isOcclusion;
    }

    ao *= _SSAOParams.y;

    ao = PositivePow(saturate(ao * _SSAOParams.x * rcpSampleCount), 0.6);

    return ao;
}

half CompareNormal(half3 d1, half3 d2) {
    return smoothstep(0.8, 1.0, dot(d1, d2));
}

#define SAMPLE_BASEMAP(uv) SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).rgb;

half4 Blur(const float2 uv, const float2 delta) : SV_Target 
{
    half3 n0 = SampleSceneNormals(uv);
    half3 n1a = SampleSceneNormals(uv - delta * 1.3846153846);
    half3 n1b = SampleSceneNormals(uv + delta * 1.3846153846);
    half3 n2a = SampleSceneNormals(uv - delta * 3.2307692308);
    half3 n2b = SampleSceneNormals(uv + delta * 3.2307692308);

    // 计算每个点的权重
    half w0 = half(0.2270270270);
    half w1a = CompareNormal(n0, n1a) * half(0.3162162162);
    half w1b = CompareNormal(n0, n1b) * half(0.3162162162);
    half w2a = CompareNormal(n0, n2a) * half(0.0702702703);
    half w2b = CompareNormal(n0, n2b) * half(0.0702702703);

    // 进行Blur
    half3 color = 0.0;
    color += SAMPLE_BASEMAP(uv);
    color += SAMPLE_BASEMAP(uv - delta * 1.3846153846);
    color += SAMPLE_BASEMAP(uv + delta * 1.3846153846);
    color += SAMPLE_BASEMAP(uv - delta * 3.2307692308);
    color += SAMPLE_BASEMAP(uv + delta * 3.2307692308);
    //color *= rcp(w0 + w1a + w1b + w2a + w2b);

    return half4(color, 1.0);
}

half BlurSmall(const float2 uv, const float2 delta)
{
    half3 p0 = SAMPLE_BASEMAP(uv                            );
    half3 p1 = SAMPLE_BASEMAP(uv + float2(-delta.x, -delta.y));
    half3 p2 = SAMPLE_BASEMAP(uv + float2( delta.x, -delta.y));
    half3 p3 = SAMPLE_BASEMAP(uv + float2(-delta.x,  delta.y));
    half3 p4 = SAMPLE_BASEMAP(uv + float2( delta.x,  delta.y));

    half color = 0.0;
    color += p0.r;
    color += p1.r;
    color += p2.r;
    color += p3.r;
    color += p4.r;

    return color * 0.2;
}

half4 HorizontalBlur(Varyings input) : SV_Target
{
    return Blur(input.texcoord, float2(_SourceSize.z, 0.0));
}

half4 VerticalBlur(Varyings input) : SV_Target
{
    return Blur(input.texcoord, float2(_SourceSize.w, 0.0));
}

half4 FinalFrag(Varyings input) : SV_Target
{
    return 0.5 - BlurSmall(input.texcoord, _SourceSize.zw);
}

#endif