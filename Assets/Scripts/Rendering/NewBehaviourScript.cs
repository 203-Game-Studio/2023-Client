using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class ClusterizerUtil
{   
    [StructLayout(LayoutKind.Sequential)] 
    public struct meshopt_Meshlet 
    { 
        public System.UInt32 vertex_offset;
	    public System.UInt32 triangle_offset;
        public System.UInt32 vertex_count;
	    public System.UInt32 triangle_count;
    } 

    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshletsBound(Int64 index_count, Int64 max_vertices, Int64 max_triangles);
    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshlets(meshopt_Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, uint[] indices, Int64 index_count, float[] vertex_positions, 
        Int64 vertex_count, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, float cone_weight);
    [DllImport("ClusterizerUtil")]
    public static extern Int64 meshopt_buildMeshlets2(meshopt_Meshlet[] meshlets, uint[] meshlet_vertices, 
        byte[] meshlet_triangles, uint[] indices, Int64 index_count, float[] vertex_positions, 
        Int64 vertex_count, Int64 vertex_positions_stride, Int64 max_vertices, Int64 max_triangles, float cone_weight);
}

public class NewBehaviourScript : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public MaterialPropertyBlock block;
    // Start is called before the first frame update
    void Start1()
    {
        /*const Int64 max_vertices = 64;
        const Int64 max_triangles = 124;
        const float cone_weight = 0.0f;
        int[] trianglesArray = mesh.GetTriangles(0);
        uint[] triangles = new uint[trianglesArray.Length];
        for(int i = 0; i < trianglesArray.Length; ++i){
            triangles[i] = (uint)trianglesArray[i];
        }
        List<Vector3> verticesList = new List<Vector3>();
        mesh.GetVertices(verticesList);
        float[] vertices = new float[verticesList.Count * 3];
        for(int i = 0; i < verticesList.Count; ++i){
            vertices[i*3] = verticesList[i].x;
            vertices[i*3+1] = verticesList[i].y;
            vertices[i*3+2] = verticesList[i].z;
        }

        uint[] triangles_test = new uint[]{
            0,1,2,3,4,5,6,4,3,7,1,0,8,9,10,0,11,7,12,13,14,15,6,3,16,13,12,3,17,15,18,9,8,13,9,18,19,16,12,20,16,19,9,21,22,22,10,9,23,24,25,3,5,26,27,3,26,28,3,27,22,21,29,30,3,28,17,3,30,31,15,17,21,9,13,32,15,31,13,16,21,12,14,33,15,32,34,35,12,33,25,15,34,36,35,33,37,38,39,24,15,25,31,38,37,39,38,40,32,31,37,4,12,35,29,20,41,34,32,37,19,12,4,21,20,29,42,17,30,16,20,21,31,17,42,43,35,36,20,44,45,45,41,20,46,47,48,38,46,48,40,38,48,46,31,42,38,31,46,49,13,18,48,47,50,45,44,51,14,13,49,52,28,27,4,35,43,53,4,43,5,4,53,33,14,49,30,28,52,54,20,19,26,5,53,52,55,30,44,20,54,54,24,23,51,54,23,44,54,51,54,19,4,4,6,54,15,54,6,24,54,15,56,46,57,47,46,56,50,56,58,47,56,50,59,60,61,56,57,7,7,62,56,63,60,59,64,30,55,65,64,55,42,30,64,56,62,66,66,58,56,57,42,64,46,42,57,2,64,65,1,64,2,7,64,1,57,64,7,61,7,11,62,7,61,60,62,61,62,60,63,63,66,62,67,68,69,70,71,72,73,74,75,55,76,65,26,68,27,58,77,50,78,77,58,39,79,37,80,78,58,58,66,80,81,82,83,65,71,2,84,71,70,77,78,85,75,86,87,76,71,65,88,68,67,89,84,70,85,90,77,79,86,75,91,68,88,0,71,84,74,79,75,85,80,92,27,68,91,2,71,0,82,10,93,78,80,85,52,27,91,83,82,93,87,86,94,84,95,11,11,95,61,8,10,82,91,76,55,11,0,84,55,52,91,96,80,66,66,63,96,97,91,88,98,84,89,99,82,81,86,79,39,71,91,97,100,82,99,40,86,39,72,71,97,101,84,98,102,86,40,76,91,71,103,101,98,101,104,59,8,100,18,95,84,101,61,101,59,82,100,8,94,102,105,95,101,61,86,102,94,106,101,103,10,22,107,48,102,40,107,93,10,108,109,110,25,109,108,111,25,108,23,25,111,96,104,112,113,96,112,63,104,96,59,104,63,29,114,115,107,29,115,22,29,107,109,74,73,48,77,116,73,110,109,102,48,116,105,102,116,50,77,48,92,96,117,41,114,29,80,96,92,34,109,25,116,77,90,118,100,99,117,96,113,119,101,106,37,109,34,104,101,119,74,109,37,49,100,118,79,74,37,120,49,118,18,100,49,112,104,119,121,122,123,33,49,120,122,33,120,123,122,120,124,122,121,125,122,124,36,122,125,43,36,125,43,125,53,126,125,124,68,125,126,69,68,126,53,125,68,26,53,68,33,122,36,115,114,127,127,45,128,114,45,127,41,45,114,128,111,129,51,111,128,45,51,128,129,111,108,23,111,51,110,130,108,83,131,81,132,133,134,135,133,132,136,73,137,130,73,136,110,73,130,138,93,139,131,93,138,83,93,131,131,140,99,99,81,131,121,123,141,141,142,121,143,131,138,140,131,143,124,121,142,142,144,124,112,145,113,139,107,146,93,107,139,147,148,149,126,124,144,150,148,147,115,151,152,75,153,154,155,126,144,146,115,152,156,117,157,137,75,154,158,155,144,149,148,159,133,117,156,107,115,146,160,150,147,73,75,137,126,155,69,92,117,133,67,161,88,152,151,162,163,155,158,154,153,164,134,133,156,99,140,118,87,153,75,117,145,165,166,155,163,157,117,165,167,140,143,161,155,166,113,145,117,118,140,167,69,155,161,120,118,167,67,69,161,168,106,169,119,106,168,141,120,167,170,161,166,123,120,141,171,161,170,145,119,168,153,172,173,174,161,171,127,151,115,175,145,168,164,153,173,88,161,174,112,119,145,87,172,153,97,88,174,127,128,176,94,172,87,165,145,175,151,127,176,162,151,176,97,174,72,128,129,177,177,176,128,173,172,178,177,130,179,108,130,177,129,108,177,180,172,94,94,105,180,179,130,136,116,180,105,172,180,181,181,178,172,181,90,182,116,90,181,180,116,181,183,174,171,90,135,184,182,90,184,85,135,90,72,174,183,70,72,183,185,70,183,186,70,185,184,135,132,187,186,185,133,135,85,85,92,133,70,186,89,89,186,187,98,89,187,188,98,187,189,98,188,98,189,103,190,189,188,103,189,190,106,103,190,169,106,190,191,192,193,159,192,191,148,192,159,192,194,195,195,193,192,195,194,196,194,192,148,148,150,194,194,150,160,160,196,194,197,134,198,132,134,197,154,199,137,200,199,154,157,201,156,202,203,204,205,206,207,154,208,209,156,201,210,139,211,138,212,154,209,134,156,210,213,214,215,216,206,205,200,154,212,137,199,136,198,134,210,217,216,205,164,208,154,215,218,219,220,216,217,214,218,215,218,221,222,138,223,143,173,208,164,224,165,225,222,219,218,141,226,142,216,227,149,228,214,213,211,223,138,201,165,224,209,208,229,159,216,149,221,230,231,232,226,233,218,214,228,170,234,171,171,234,235,236,237,238,239,240,241,157,165,201,220,227,216,242,211,139,229,178,243,231,222,221,142,226,232,244,218,228,245,171,235,149,246,147,247,239,241,210,201,224,248,220,217,139,146,242,173,178,229,249,218,250,144,142,232,250,218,244,183,171,245,227,246,149,251,240,239,252,253,168,227,220,248,208,173,229,152,242,146,221,218,249,254,183,245,255,250,244,256,144,232,168,169,252,238,227,248,241,240,257,147,258,160,243,181,259,223,242,260,230,221,249,183,254,185,158,144,256,237,227,238,175,253,261,262,263,193,246,258,147,178,181,243,211,242,223,249,264,230,265,254,245,266,175,261,163,158,256,193,195,262,267,258,246,181,268,269,231,270,271,260,242,152,168,253,175,187,254,265,256,272,163,259,181,269,251,262,195,230,270,231,246,227,237,203,260,152,253,252,273,274,187,265,182,268,181,166,163,272,275,251,195,270,276,277,237,278,246,162,203,152,273,279,253,280,274,265,269,268,281,272,282,166,196,275,195,277,271,270,261,253,279,223,283,167,246,278,284,185,254,187,184,268,182,165,175,266,270,230,264,258,275,196,170,166,282,167,143,223,285,246,284,266,225,165,187,274,188,281,132,197,264,286,270,196,160,258,234,170,282,267,246,285,283,226,141,184,132,281,287,274,280,276,270,286,288,251,275,289,234,282,141,167,283,258,267,290,268,184,281,286,263,276,291,274,287,240,251,288,290,292,258,283,260,293,190,274,291,240,288,294,249,250,255,223,260,283,290,267,285,295,190,291,294,257,240,296,249,255,188,274,190,260,203,202,278,237,236,297,249,296,275,258,292,298,260,202,236,299,278,190,252,169,300,275,292,301,249,297,293,260,298,295,252,190,284,278,299,288,275,300,264,249,301,283,293,302,303,295,291,294,288,300,304,297,296,233,283,302,273,295,303,305,200,306,226,283,233,301,297,304,252,295,273,199,200,305,302,293,298,207,301,304,276,307,308,306,200,212,206,301,207,204,176,309,308,277,276,286,264,301,162,176,204,307,239,310,301,311,286,203,162,204,310,308,307,286,311,191,309,177,312,307,276,263,193,286,191,176,177,309,263,262,307,263,286,193,312,179,313,251,307,262,177,179,312,216,301,206,239,307,251,311,301,216,136,199,305,310,239,247,313,136,305,191,216,159,179,136,313,311,216,191,235,234,289,314,315,316,317,318,319,202,320,298,302,321,233,322,323,324,285,284,325,326,327,328,210,318,198,329,330,331,325,332,285,333,320,334,335,323,322,336,318,210,323,329,331,321,320,333,232,321,337,338,327,326,324,323,331,225,339,224,340,290,341,302,320,321,342,232,337,343,338,326,292,290,340,315,330,329,298,320,302,233,321,232,210,339,344,205,327,338,345,292,340,329,346,315,309,347,204,348,210,344,207,327,205,349,345,340,336,210,348,224,339,210,337,321,333,312,347,309,350,351,310,352,347,312,353,338,343,261,354,266,247,350,310,341,285,332,329,323,335,205,338,217,355,306,356,357,350,247,290,285,341,358,273,359,256,232,342,360,329,335,361,313,362,305,306,355,215,363,213,364,338,353,279,273,358,365,350,357,342,366,256,367,236,368,369,329,360,235,289,370,352,313,361,371,363,215,217,338,364,372,350,365,356,212,373,299,236,367,261,279,358,374,235,370,312,313,352,375,371,215,346,329,369,248,217,364,306,212,356,272,256,366,241,357,247,354,261,358,284,299,367,376,374,370,313,305,355,369,371,346,219,375,215,377,248,364,373,209,378,379,272,366,365,357,241,380,354,358,367,325,284,355,362,313,381,374,376,316,382,383,384,375,219,238,248,377,212,209,373,385,379,366,257,365,241,386,354,387,376,388,381,204,347,389,346,382,316,368,238,377,390,363,391,392,365,257,339,354,386,272,379,282,378,229,393,320,204,389,315,346,316,236,238,368
        };
        float[] vertices_test = new float[]{
            0.354285f,0.047954f,-0.494873f,0.354285f,0.047795f,-0.494943f,0.35415f,0.047795f,-0.494873f,0.336702f,0.021421f,-0.499806f,0.327911f,0.01263f,-0.501774f,0.327911f,0.021421f,-0.49654f,0.336702f,0.01263f,-0.502222f,0.363077f,0.047795f,-0.497754f,0.303932f,-0.013745f,-0.494873f,0.310328f,-0.013745f,-0.498568f,0.310328f,-0.017215f,-0.494873f,0.363077f,0.056482f,-0.494873f,0.31912f,0.003838f,-0.502441f,0.310328f,-0.004953f,-0.500928f,0.310328f,0.003838f,-0.497185f,0.345494f,0.01263f,-0.500333f,0.31912f,-0.004953f,-0.50284f,0.345494f,0.021421f,-0.500866f,0.302999f,-0.004953f,-0.494873f,0.327911f,0.003838f,-0.503467f,0.327911f,-0.004953f,-0.501289f,0.31912f,-0.013745f,-0.498305f,0.31912f,-0.016565f,-0.494873f,0.345494f,0.001393f,-0.494873f,0.345494f,0.003838f,-0.496482f,0.348239f,0.003838f,-0.494873f,0.327911f,0.023586f,-0.494873f,0.336615f,0.030212f,-0.494873f,0.336702f,0.030212f,-0.494917f,0.326153f,-0.013745f,-0.494873f,0.345494f,0.030212f,-0.498572f,0.354285f,0.021421f,-0.50028f,0.354285f,0.01263f,-0.497206f,0.310328f,0.007253f,-0.494873f,0.354285f,0.008445f,-0.494873f,0.31912f,0.01263f,-0.497964f,0.315006f,0.01263f,-0.494873f,0.360627f,0.01263f,-0.494873f,0.363077f,0.021421f,-0.498153f,0.363077f,0.014359f,-0.494873f,0.370443f,0.021421f,-0.494873f,0.327911f,-0.012817f,-0.494873f,0.354285f,0.030212f,-0.500511f,0.31912f,0.016658f,-0.494873f,0.336702f,-0.004953f,-0.496186f,0.336702f,-0.006457f,-0.494873f,0.363077f,0.030212f,-0.500241f,0.371868f,0.030212f,-0.497078f,0.371868f,0.02339f,-0.494873f,0.308059f,0.003838f,-0.494873f,0.375723f,0.030212f,-0.494873f,0.3385f,-0.004953f,-0.494873f,0.336702f,0.030279f,-0.494873f,0.324891f,0.021421f,-0.494873f,0.336702f,0.003838f,-0.501307f,0.345494f,0.038312f,-0.494873f,0.371868f,0.039004f,-0.498111f,0.363077f,0.039004f,-0.499837f,0.378751f,0.039004f,-0.494873f,0.371868f,0.057151f,-0.494873f,0.371868f,0.056587f,-0.495074f,0.363185f,0.056587f,-0.494873f,0.371868f,0.047795f,-0.497381f,0.372608f,0.056587f,-0.494873f,0.354285f,0.039004f,-0.498463f,0.346169f,0.039004f,-0.494873f,0.378397f,0.047795f,-0.494873f,0.327911f,0.03255f,-0.486081f,0.327911f,0.030212f,-0.488621f,0.325346f,0.030212f,-0.486081f,0.345494f,0.051656f,-0.486081f,0.345494f,0.047795f,-0.488805f,0.342445f,0.047795f,-0.486081f,0.363077f,0.002521f,-0.486081f,0.363077f,0.003838f,-0.487237f,0.36573f,0.003838f,-0.486081f,0.345494f,0.039004f,-0.494494f,0.380659f,0.030212f,-0.49093f,0.380659f,0.039004f,-0.493475f,0.363077f,0.01263f,-0.493889f,0.380659f,0.047795f,-0.493272f,0.300264f,-0.013745f,-0.486081f,0.301537f,-0.013745f,-0.491843f,0.301537f,-0.015513f,-0.486081f,0.354285f,0.056587f,-0.490229f,0.387428f,0.039004f,-0.486081f,0.371868f,0.01263f,-0.488844f,0.371868f,0.008072f,-0.486081f,0.334612f,0.039004f,-0.486081f,0.349314f,0.056587f,-0.486081f,0.384737f,0.030212f,-0.486081f,0.336702f,0.039004f,-0.487948f,0.387314f,0.047795f,-0.486081f,0.310328f,-0.019725f,-0.486081f,0.375703f,0.01263f,-0.486081f,0.363077f,0.056587f,-0.494837f,0.380659f,0.056587f,-0.489868f,0.336702f,0.041176f,-0.486081f,0.354285f,0.063664f,-0.486081f,0.299617f,-0.004953f,-0.486081f,0.301537f,-0.004953f,-0.492628f,0.363077f,0.065378f,-0.490194f,0.371868f,0.021421f,-0.493976f,0.355545f,0.065378f,-0.486081f,0.371868f,0.065378f,-0.489705f,0.380536f,0.021421f,-0.486081f,0.363077f,0.069458f,-0.486081f,0.31912f,-0.018964f,-0.486081f,0.34853f,-0.004953f,-0.486081f,0.354285f,0.003838f,-0.491582f,0.354285f,-0.001987f,-0.486081f,0.345494f,-0.004953f,-0.488803f,0.376453f,0.065378f,-0.486081f,0.380659f,0.061185f,-0.486081f,0.327911f,-0.013745f,-0.493634f,0.327911f,-0.016178f,-0.486081f,0.380659f,0.021679f,-0.486081f,0.383793f,0.056587f,-0.486081f,0.301537f,-0.000273f,-0.486081f,0.371868f,0.068032f,-0.486081f,0.303253f,0.003838f,-0.486081f,0.310328f,0.015423f,-0.486081f,0.310328f,0.01263f,-0.489644f,0.308313f,0.01263f,-0.486081f,0.315794f,0.021421f,-0.486081f,0.31912f,0.021421f,-0.490101f,0.31912f,0.024639f,-0.486081f,0.33333f,-0.013745f,-0.486081f,0.336702f,-0.012087f,-0.486081f,0.345494f,-0.006714f,-0.486081f,0.354285f,-0.004953f,-0.481613f,0.301537f,-0.013745f,-0.48174f,0.391766f,0.039004f,-0.47729f,0.389451f,0.047795f,-0.482615f,0.391917f,0.047795f,-0.47729f,0.389451f,0.039004f,-0.482623f,0.361177f,-0.004953f,-0.47729f,0.363077f,-0.004538f,-0.47729f,0.308482f,-0.013745f,-0.47729f,0.310328f,-0.01416f,-0.47729f,0.301537f,-0.004953f,-0.479064f,0.30653f,0.01263f,-0.47729f,0.310328f,0.019691f,-0.47729f,0.303301f,-0.004953f,-0.47729f,0.31141f,0.021421f,-0.47729f,0.380659f,0.065378f,-0.480915f,0.31912f,-0.014604f,-0.47729f,0.301537f,0.118835f,-0.47729f,0.292745f,0.118126f,-0.477553f,0.292745f,0.120028f,-0.47729f,0.301537f,0.118126f,-0.477378f,0.327911f,-0.013745f,-0.477343f,0.327697f,-0.013745f,-0.47729f,0.371868f,0.003838f,-0.482929f,0.371868f,-0.001826f,-0.47729f,0.31912f,0.030212f,-0.478335f,0.389451f,0.056391f,-0.47729f,0.389359f,0.056587f,-0.47729f,0.318459f,0.030212f,-0.47729f,0.291174f,0.118126f,-0.47729f,0.301985f,0.118126f,-0.47729f,0.327911f,0.039004f,-0.478865f,0.327911f,-0.013708f,-0.47729f,0.31912f,0.030954f,-0.47729f,0.379298f,0.003838f,-0.47729f,0.382978f,0.065378f,-0.47729f,0.326683f,0.039004f,-0.47729f,0.304029f,0.003838f,-0.47729f,0.371868f,0.072571f,-0.47729f,0.363077f,0.073837f,-0.47729f,0.327911f,0.040362f,-0.47729f,0.334255f,0.047795f,-0.47729f,0.380659f,0.01263f,-0.480872f,0.380659f,0.006205f,-0.47729f,0.336702f,0.047795f,-0.480106f,0.380659f,0.067525f,-0.47729f,0.336702f,-0.011792f,-0.47729f,0.345494f,-0.009023f,-0.47729f,0.382992f,0.01263f,-0.47729f,0.354285f,-0.00652f,-0.47729f,0.380659f,0.021421f,-0.485929f,0.386073f,0.021421f,-0.47729f,0.389436f,0.030212f,-0.47729f,0.336702f,0.05099f,-0.47729f,0.389451f,0.030253f,-0.47729f,0.341117f,0.056587f,-0.47729f,0.345494f,0.056587f,-0.48213f,0.345494f,0.062896f,-0.47729f,0.346846f,0.065378f,-0.47729f,0.354285f,0.065378f,-0.485143f,0.354285f,0.072192f,-0.47729f,0.289323f,0.109335f,-0.47729f,0.292745f,0.109335f,-0.47774f,0.292745f,0.10564f,-0.47729f,0.301537f,0.109335f,-0.47759f,0.301537f,0.106461f,-0.47729f,0.303425f,0.109335f,-0.47729f,0.394951f,0.039004f,-0.468499f,0.395728f,0.047795f,-0.468499f,0.363077f,-0.004953f,-0.47644f,0.371868f,-0.004953f,-0.472119f,0.389451f,0.056587f,-0.477102f,0.327911f,-0.003301f,-0.468499f,0.327911f,-0.004953f,-0.46949f,0.330241f,-0.004953f,-0.468499f,0.275162f,0.120501f,-0.468499f,0.275162f,0.118126f,-0.469826f,0.273301f,0.118126f,-0.468499f,0.380659f,0.003838f,-0.475678f,0.380659f,-0.001769f,-0.468499f,0.393953f,0.056587f,-0.468499f,0.310328f,-0.013745f,-0.477039f,0.375956f,-0.004953f,-0.468499f,0.247787f,0.091752f,-0.468499f,0.248788f,0.091752f,-0.468743f,0.248788f,0.085916f,-0.468499f,0.283954f,0.118126f,-0.475007f,0.28073f,0.126918f,-0.468499f,0.25758f,0.091752f,-0.469861f,0.25758f,0.08466f,-0.468499f,0.283954f,0.126918f,-0.471361f,0.266371f,0.091752f,-0.47004f,0.266371f,0.085895f,-0.468499f,0.310328f,-0.004953f,-0.474369f,0.389451f,0.064716f,-0.468499f,0.388902f,0.065378f,-0.468499f,0.310328f,0.01263f,-0.470559f,0.292745f,0.126918f,-0.475915f,0.248788f,0.094045f,-0.468499f,0.384238f,0.003838f,-0.468499f,0.275162f,0.091752f,-0.470739f,0.275162f,0.085766f,-0.468499f,0.312205f,0.021421f,-0.468499f,0.313219f,0.01263f,-0.468499f,0.327911f,0.047795f,-0.469865f,0.327911f,0.049556f,-0.468499f,0.292745f,0.136227f,-0.468499f,0.292745f,0.135709f,-0.469395f,0.291205f,0.135709f,-0.468499f,0.310328f,0.091752f,-0.471085f,0.31912f,0.100544f,-0.469343f,0.31912f,0.092837f,-0.468499f,0.31912f,-0.013745f,-0.476579f,0.386863f,0.01263f,-0.468499f,0.254608f,0.100544f,-0.468499f,0.33232f,0.056587f,-0.468499f,0.301537f,0.126918f,-0.475496f,0.318676f,0.091752f,-0.468499f,0.283954f,0.130537f,-0.468499f,0.266371f,0.100544f,-0.470616f,0.25758f,0.100544f,-0.469121f,0.310328f,0.100544f,-0.474232f,0.363077f,0.074169f,-0.476235f,0.371868f,0.074169f,-0.472842f,0.336702f,0.056587f,-0.472587f,0.25758f,0.102713f,-0.468499f,0.315249f,0.030212f,-0.468499f,0.320219f,0.100544f,-0.468499f,0.310328f,0.118126f,-0.473668f,0.389082f,0.021421f,-0.468499f,0.31912f,-0.004953f,-0.47249f,0.376938f,0.074169f,-0.468499f,0.301537f,0.100544f,-0.47607f,0.292745f,0.100544f,-0.476198f,0.275162f,0.100544f,-0.472669f,0.336702f,0.064081f,-0.468499f,0.380659f,0.072384f,-0.468499f,0.310328f,0.126918f,-0.469503f,0.389451f,0.030212f,-0.477251f,0.389451f,0.022667f,-0.468499f,0.283954f,0.091752f,-0.471823f,0.283954f,0.08514f,-0.468499f,0.31912f,0.037196f,-0.468499f,0.363077f,0.076031f,-0.468499f,0.345494f,0.065378f,-0.475584f,0.310328f,0.109335f,-0.475039f,0.292745f,0.091752f,-0.472527f,0.292745f,0.085004f,-0.468499f,0.301537f,0.135709f,-0.468545f,0.371868f,0.075426f,-0.468499f,0.337422f,0.065378f,-0.468499f,0.392096f,0.030212f,-0.468499f,0.320358f,0.039004f,-0.468499f,0.310328f,0.003838f,-0.472551f,0.301605f,0.135709f,-0.468499f,0.310328f,0.128301f,-0.468499f,0.283954f,0.100544f,-0.474952f,0.345494f,0.072728f,-0.468499f,0.31912f,0.109335f,-0.468769f,0.326743f,0.047795f,-0.468499f,0.311239f,0.126918f,-0.468499f,0.349543f,0.074169f,-0.468499f,0.316462f,0.118126f,-0.468499f,0.31912f,0.003838f,-0.46932f,0.319397f,0.109335f,-0.468499f,0.354285f,0.074169f,-0.471877f,0.2654f,0.109335f,-0.468499f,0.266371f,0.109335f,-0.468875f,0.32077f,0.003838f,-0.468499f,0.301537f,0.135739f,-0.468499f,0.31912f,0.110615f,-0.468499f,0.275162f,0.109335f,-0.472615f,0.31912f,0.005633f,-0.468499f,0.354285f,0.075169f,-0.468499f,0.266371f,0.110321f,-0.468499f,0.363077f,-0.007454f,-0.468499f,0.371868f,-0.006265f,-0.468499f,0.301537f,0.091752f,-0.472368f,0.301537f,0.085376f,-0.468499f,0.336702f,-0.006797f,-0.468499f,0.310328f,0.086613f,-0.468499f,0.283954f,0.109335f,-0.476042f,0.345494f,-0.007401f,-0.468499f,0.354285f,-0.007537f,-0.468499f,0.239997f,0.064166f,-0.459707f,0.239997f,0.065378f,-0.460805f,0.246964f,0.065378f,-0.459707f,0.398242f,0.044182f,-0.459707f,0.398242f,0.047795f,-0.461257f,0.398727f,0.047795f,-0.459707f,0.327911f,0.003838f,-0.463214f,0.31912f,0.01263f,-0.464014f,0.221315f,0.074169f,-0.459707f,0.222414f,0.074169f,-0.460469f,0.222414f,0.066878f,-0.459707f,0.308898f,0.135709f,-0.459707f,0.266371f,0.119187f,-0.459707f,0.266371f,0.118126f,-0.461739f,0.265048f,0.118126f,-0.459707f,0.231205f,0.074169f,-0.464352f,0.231205f,0.065378f,-0.461209f,0.224392f,0.065378f,-0.459707f,0.310328f,0.13461f,-0.459707f,0.32341f,0.01263f,-0.459707f,0.327911f,0.007421f,-0.459707f,0.222414f,0.077131f,-0.459707f,0.398242f,0.056587f,-0.460246f,0.31912f,0.017679f,-0.459707f,0.275162f,0.126918f,-0.460457f,0.389451f,0.065378f,-0.467647f,0.31912f,0.124206f,-0.459707f,0.317282f,0.126918f,-0.459707f,0.317363f,0.021421f,-0.459707f,0.274879f,0.126918f,-0.459707f,0.395096f,0.065378f,-0.459707f,0.31912f,0.118126f,-0.46528f,0.239997f,0.074169f,-0.465371f,0.336702f,-0.004953f,-0.466108f,0.398242f,0.058038f,-0.459707f,0.322628f,0.118126f,-0.459707f,0.31912f,0.082961f,-0.465973f,0.310328f,0.082961f,-0.46617f,0.345494f,-0.004953f,-0.462234f,0.275162f,0.127167f,-0.459707f,0.380659f,0.074169f,-0.464755f,0.363077f,-0.007199f,-0.459707f,0.371868f,-0.006473f,-0.459707f,0.31912f,0.091752f,-0.468328f,0.371868f,0.07789f,-0.459707f,0.363077f,0.07756f,-0.459707f,0.225506f,0.082961f,-0.459707f,0.349917f,-0.004953f,-0.459707f,0.354285f,-0.005941f,-0.459707f,0.239997f,0.091752f,-0.465452f,0.283954f,0.134915f,-0.459707f,0.327911f,0.091752f,-0.462887f,0.315936f,0.030212f,-0.459707f,0.301537f,0.139216f,-0.459707f,0.292745f,0.139314f,-0.459707f,0.231205f,0.082961f,-0.46354f,0.319222f,0.047795f,-0.459707f,0.239997f,0.082961f,-0.46677f,0.327911f,0.082961f,-0.464389f,0.376388f,-0.004953f,-0.459707f,0.327911f,0.056587f,-0.464642f,0.248788f,0.082961f,-0.467935f,0.320875f,0.056587f,-0.459707f,0.285045f,0.135709f,-0.459707f,0.380659f,-0.002031f,-0.459707f,0.31912f,0.039004f,-0.466062f,0.380659f,0.076501f,-0.459707f,0.327911f,0.065378f,-0.463107f,0.248788f,0.074169f,-0.46488f,0.248788f,0.065782f,-0.459707f,0.25758f,0.082961f,-0.467943f,0.317123f,0.039004f,-0.459707f,0.389451f,0.071782f,-0.459707f,0.386149f,0.074169f,-0.459707f,0.319563f,0.065378f,-0.459707f,0.336702f,0.000218f,-0.459707f,0.241762f,0.100544f,-0.459707f,0.239997f,0.099187f,-0.459707f,0.327911f,0.100544f,-0.460604f,0.384859f,0.003838f,-0.459707f,0.248788f,0.100544f,-0.466043f,0.292745f,0.074044f,-0.459707f,0.301537f,0.074169f,-0.459829f,0.301537f,0.073968f,-0.459707f,0.327911f,0.104191f,-0.459707f,0.326567f,0.109335f,-0.459707f
        };

        Int64 max_meshlets = ClusterizerUtil.meshopt_buildMeshletsBound(triangles_test.Length, max_vertices, max_triangles);
        ClusterizerUtil.meshopt_Meshlet[] meshlets = new ClusterizerUtil.meshopt_Meshlet[max_meshlets];
        uint[] meshlet_vertices = new uint[max_meshlets * max_vertices];
        byte[] meshlet_triangles = new byte[max_meshlets * max_triangles * 3];

        Int64 meshlet_count = ClusterizerUtil.meshopt_buildMeshlets(meshlets, meshlet_vertices, 
            meshlet_triangles, triangles_test, triangles_test.Length, vertices_test, 
            Mathf.CeilToInt(vertices_test.Length/3.0f), 3*4, max_vertices, max_triangles, cone_weight);*/
        /*Debug.LogError($"meshlet_count {meshlet_count}");
        Debug.LogError($"triangles.Length {meshlets[0].vertex_offset}");
        Debug.LogError($"verticesList.Count {meshlets[0].triangle_offset}");
        Debug.LogError($"3*4 {meshlets[0].vertex_count}");
        Debug.LogError($"max_triangles {meshlets[0].triangle_count}");
        Debug.LogError($"max_triangles {meshlets[1].vertex_offset}");
        Debug.LogError($"cone_weight {meshlets[1].triangle_offset}");
        int a = -888;
        Debug.LogError($"-888 {meshlets[1].vertex_count} {(uint)a}");
        Debug.LogError($"-1000 {meshlets[1].triangle_count}");
        Debug.LogError($"1 {meshlet_vertices[0]}");
        Debug.LogError($"2 {meshlet_vertices[1]}");
        Debug.LogError($"3 {meshlet_vertices[2]}");
        Debug.LogError($"4 {meshlet_vertices[3]}");
        Debug.LogError($"5 {meshlet_vertices[4]}");
        Debug.LogError($"2 {meshlet_triangles[0]}");
        Debug.LogError($"3 {meshlet_triangles[1]}");
        Debug.LogError($"4 {meshlet_triangles[2]}");
        Debug.LogError($"5 {meshlet_triangles[3]}");
        Debug.LogError($"6 {meshlet_triangles[4]}");*/
        block = new MaterialPropertyBlock();
        BinaryReader br;
        try
        {
           br = new BinaryReader(new FileStream($"{Application.dataPath}/dragon.bin", FileMode.Open));
        }
        catch (IOException e)
        {
           Debug.Log(e.Message + "\n Cannot open file.");
           return;
        }
        try
        {
            var indicesC = br.ReadInt64();
            int[] indices = new int[indicesC];
            for(int i = 0; i < indicesC;++i){
                indices[i] = (int)(br.ReadUInt32());
            }
            var meshlet_trianglesC = br.ReadInt64();
            sbyte[] meshlet_triangles = new sbyte[meshlet_trianglesC];
            for(int i = 0; i < meshlet_trianglesC;++i){
                meshlet_triangles[i] = br.ReadSByte();
            }
            var meshlet_verticesC = br.ReadInt64();
            uint[] meshlet_vertices = new uint[meshlet_verticesC];
            for(int i = 0; i < meshlet_verticesC;++i){
                meshlet_vertices[i] = br.ReadUInt32();
            }
            var verticesC = br.ReadInt64();
            Vector3[] vertices = new Vector3[verticesC];
            for(int i = 0; i < verticesC;++i){
                vertices[i].x = br.ReadSingle();
                vertices[i].y = br.ReadSingle();
                vertices[i].z = br.ReadSingle();
            }
            var meshletsC = br.ReadInt64();
            ClusterizerUtil.meshopt_Meshlet[] meshlets = new ClusterizerUtil.meshopt_Meshlet[meshletsC];
            for(int i = 0; i < meshletsC;++i){
                meshlets[i].triangle_count = br.ReadUInt32();
                meshlets[i].triangle_offset = br.ReadUInt32();
                meshlets[i].vertex_count = br.ReadUInt32();
                meshlets[i].vertex_offset = br.ReadUInt32();
            }

            for(int i = 0;i< meshletsC;++i){
                var meshlet = meshlets[i];
                GameObject go = new GameObject();
                var filter = go.AddComponent<MeshFilter>();
                Mesh mesh = new Mesh();

                int fixedCount = Mathf.CeilToInt(meshlet.triangle_count/3.0f)*3;
                int[] curIndices = new int[fixedCount*3];
                for(int j = 0; j < meshlet.triangle_count; ++j){
                    curIndices[j*3] = meshlet_triangles[meshlet.triangle_offset + j*3];
                    curIndices[j*3+1] = meshlet_triangles[meshlet.triangle_offset + j*3+1];
                    curIndices[j*3+2] = meshlet_triangles[meshlet.triangle_offset + j*3+2];
                }
                for(int j = (int)meshlet.triangle_count; j < fixedCount; ++j){
                    curIndices[j*3] = meshlet_triangles[meshlet.triangle_offset];
                    curIndices[j*3+1] = meshlet_triangles[meshlet.triangle_offset];
                    curIndices[j*3+2] = meshlet_triangles[meshlet.triangle_offset];
                }

                Vector3[] curVertices = new Vector3[meshlet.vertex_count];
                for(int j = 0; j < meshlet.vertex_count; ++j){
                    curVertices[j] = vertices[meshlet_vertices[meshlet.vertex_offset + j]];
                }

                mesh.vertices = curVertices;
                mesh.triangles = curIndices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                filter.sharedMesh = mesh;
                var rederer = go.AddComponent<MeshRenderer>();
                rederer.sharedMaterial = material;
                block.SetColor("_BaseColor", new Color(UnityEngine.Random.Range(0, 1.0f),
                    UnityEngine.Random.Range(0, 1.0f),UnityEngine.Random.Range(0, 1.0f)));
                rederer.SetPropertyBlock(block);
            }
            /*int meshVerticesCount = (int)meshlets[i].vertex_count;
            Vector3[] meshvertices = new Vector3[meshVerticesCount];
            for(int j = 0; j < meshVerticesCount; ++j){
                meshvertices[j] = verticesList[(int)meshlet_vertices[meshlets[i].vertex_offset+j]];
            }
            
            int count = Mathf.FloorToInt(meshlets[i].triangle_count/3.0f)*3;
            var index = new int[count];
            for(int j = 0; j < count; ++j){
                index[j] = meshlet_triangles[meshlets[i].triangle_offset + j];
            }*/
            
        }
        catch (IOException e)
        {
           Debug.Log(e.Message + "\n Cannot read from file.");
           return;
        }
        br.Close();

        return;

        for(int i = 0; i < 1; ++i){
            GameObject go = new GameObject(i.ToString());
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();

            /*int meshVerticesCount = (int)meshlets[i].vertex_count;
            Vector3[] meshvertices = new Vector3[meshVerticesCount];
            for(int j = 0; j < meshVerticesCount; ++j){
                meshvertices[j] = verticesList[(int)meshlet_vertices[meshlets[i].vertex_offset+j]];
            }
            
            int count = Mathf.FloorToInt(meshlets[i].triangle_count/3.0f)*3;
            var index = new int[count];
            for(int j = 0; j < count; ++j){
                index[j] = meshlet_triangles[meshlets[i].triangle_offset + j];
            }*/
            //mesh.vertices = meshvertices;
            //mesh.triangles = index;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();
        }
        //ClusterTool.BakeClusterInfoToFile(mesh);
        //return;
        /*var obj = ClusterTool.LoadClusterObjectFromFile("fish_tigershark_001_c01_ShapeBlend");
        for(int i = 0; i < obj.clusterInfos.Length; ++i){
            var clusterInfo = obj.clusterInfos[i];
            GameObject go = new GameObject(obj.name + i);
            var filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            int triDataLen = 3 * 64;
            var index = new int[triDataLen];
            for(int j = 0; j < triDataLen; ++j){
                index[j] = obj.indexData[clusterInfo.indexStart + j];
            }
            mesh.vertices = obj.vertexData;
            mesh.triangles = index;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            var rederer = go.AddComponent<MeshRenderer>();
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            Start1();
        }
    }
}
