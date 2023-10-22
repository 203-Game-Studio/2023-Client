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

#endif