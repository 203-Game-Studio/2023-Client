#ifndef _KUWAHARA_INCLUDE_
#define _KUWAHARA_INCLUDE_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _MainTex_ST;
float2 _Radius;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct Attributes
{
    half4 positionOS : POSITION;
    half2 uv : TEXCOORD0;
};

struct Varyings
{
    half4 vertex : SV_POSITION;
    half2 uv : TEXCOORD0;
};

Varyings PostProcessingVert(Attributes input)
{
    Varyings output = (Varyings) 0;
    output.vertex = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    return output;
}

float4 GetKernelMeanAndVariance(float2 uv, float4 range, float2x2 rotationMatrix)
{
    float3 mean = 0, variance = 0;
    uint samples = 0;
    for (int x = range.x; x <= range.y; ++x)
    {
        for (int y = range.z; y <= range.w; ++y)
        {
            float2 offset = mul(float2(x, y) * _MainTex_TexelSize.xy, rotationMatrix);
            float3 pixelColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset).rgb;
            mean += pixelColor;
            variance += pixelColor * pixelColor;
            samples++;
        }
    }
    mean /= samples;
    variance = variance / samples - mean * mean;
    float totalVariance = variance.r + variance.g + variance.b;
    return float4(mean.r, mean.g, mean.b, totalVariance);
}

float GetPixelAngle(float2 uv)
{
    float gradientX = 0;
    float gradientY = 0;
    float sobelX[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};
    float sobelY[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
    int i = 0;

    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            float2 offset = float2(x, y) * _MainTex_TexelSize.xy;
            float3 pixelColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset).rgb;
            float pixelValue = dot(pixelColor, float3(0.3, 0.59, 0.11));

            gradientX += pixelValue * sobelX[i];
            gradientY += pixelValue * sobelY[i];
            i++;
        }
    }

    return atan(gradientY / gradientX);
}

half4 KuwaharaFrag(Varyings input) : SV_Target
{
    float4 meanAndVariance[4];
    float4 range = 0;

    float xRadius = _Radius.x;
    float yRadius = _Radius.y;

    float angle = GetPixelAngle(input.uv);
    float2x2 rotationMatrix = float2x2(cos(angle), -sin(angle), sin(angle), cos(angle));

    range = float4(-xRadius, 0, -yRadius, 0);
    meanAndVariance[0] = GetKernelMeanAndVariance(input.uv, range, rotationMatrix);

    range = float4(0, xRadius, -yRadius, 0);
    meanAndVariance[1] = GetKernelMeanAndVariance(input.uv, range, rotationMatrix);

    range = float4(-xRadius, 0, 0, yRadius);
    meanAndVariance[2] = GetKernelMeanAndVariance(input.uv, range, rotationMatrix);

    range = float4(0, xRadius, 0, yRadius);
    meanAndVariance[3] = GetKernelMeanAndVariance(input.uv, range, rotationMatrix);

    float3 color = meanAndVariance[0].rgb;
    float minimumVariance = meanAndVariance[0].a;

    for (int i = 1; i < 4; ++i)
    {
        if (meanAndVariance[i].a < minimumVariance)
        {
            color = meanAndVariance[i].rgb;
            minimumVariance = meanAndVariance[i].a;
        }
    }

    return half4(color, 1.0);
}

#endif