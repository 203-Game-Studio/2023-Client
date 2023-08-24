#ifndef _SSPRREFLECTOR_PASS_INCLUDED
#define _SSPRREFLECTOR_PASS_INCLUDED

struct Attributes
{
    float4 positionOS   : POSITION;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 positionNDC : TEXCOORD0;
};

TEXTURE2D(_SSPRReflectionTexture);
SAMPLER(sampler_SSPRReflectionTexture);

Varyings SSPRReflectorPassVertex(Attributes input) {
    Varyings output;

    VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInputs.positionCS;
    output.positionNDC = vertexInputs.positionNDC;

    return output;
}

half4 SSPRReflectorPassFragment(Varyings input) : SV_Target {
    float2 suv = input.positionNDC.xy / input.positionNDC.w;
    half3 finalCol = SAMPLE_TEXTURE2D(_SSPRReflectionTexture, sampler_SSPRReflectionTexture, suv);

    return half4(finalCol, 1.0);
}

#endif