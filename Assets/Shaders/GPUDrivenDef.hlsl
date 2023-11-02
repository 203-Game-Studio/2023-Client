#ifndef GPU_DRIVEN_DEF
#define GPU_DRIVEN_DEF

struct InstanceData
{
    float4x4 objectToWorldMatrix;
    //something
};
struct Meshlet
{
    uint vertexOffset;
    uint triangleOffset;
    uint vertexCount;
    uint triangleCount;
    //uint instanceOffset;
    float3 min;
    float3 max;
    float3 coneApex;
    float3 coneAxis;
    float coneCutoff;
};

#define deg2Rad 0.0174532924f

struct ShadowLine
{
    half3 vertices[2];
};

struct ShadowLine8
{
    ShadowLine shadowLine[8];
};

struct FrustumFace
{
    half3 vertices[4];
};

struct Frustum6Face
{
    FrustumFace frustumFace[6];
};


#endif