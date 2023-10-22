#pragma once
#define EXPORT_DLL __declspec(dllexport) //µ¼³ödllÉùÃ÷
extern "C" EXPORT_DLL size_t __stdcall meshopt_buildMeshletsBound(size_t index_count, size_t max_vertices, size_t max_triangles);
extern "C" EXPORT_DLL size_t __stdcall meshopt_buildMeshlets(meshopt_Meshlet * meshlets, unsigned int* meshlet_vertices, 
	unsigned char* meshlet_triangles, const unsigned int* indices, size_t index_count, const float* vertex_positions, 
	size_t vertex_count, size_t vertex_positions_stride, size_t max_vertices, size_t max_triangles, float cone_weight);
extern "C" EXPORT_DLL meshopt_Bounds __stdcall meshopt_computeMeshletBounds(const unsigned int* meshlet_vertices,
	const unsigned char* meshlet_triangles, size_t triangle_count, const float* vertex_positions, size_t vertex_count,
	size_t vertex_positions_stride);