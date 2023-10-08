using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.AI;

public static class Clusterizer
{
    // meshlet最多255
    const Int64 meshletMaxVertices = 255;
    // 2*meshletMaxVertices
    const Int64 meshletMaxTriangles = 512;

    struct Meshlet
    {
        public uint vertex_offset;
        public uint triangle_offset;

        public uint vertexCount;
        public uint triangle_count;
    };

    struct TriangleAdjacency
    {
    	public uint[] counts;
    	public uint[] offsets;
    	public uint[] data;
    };

	struct Cone
	{
		public float px, py, pz;
		public float nx, ny, nz;
	};

	struct KDNode
	{
		public float split;
		public uint index;
		public uint axis;
		public uint children;
	};

    public static Int64 BuildMeshletsBound(Int64 indexCount, Int64 maxVertices, Int64 maxTriangles)
    {
        Assert.IsFalse(indexCount % 3 == 0, "indexCount不是3的倍数");
        Assert.IsFalse(maxVertices >= 3 && maxVertices <= meshletMaxVertices, "maxVertices超出限制");
        Assert.IsFalse(maxTriangles >= 1 && maxTriangles <= meshletMaxTriangles, "maxTriangles超出限制");
        // ensures the caller will compute output space properly as index data is 4b aligned
        //这块有点奇怪 后面查查
        Assert.IsFalse(maxTriangles % 4 == 0);

    	Int64 maxVerticesConservative = maxVertices - 2;
    	Int64 meshletLimitVertices = (indexCount + maxVerticesConservative - 1) / maxVerticesConservative;
    	Int64 meshletLimitTriangles = (indexCount / 3 + maxTriangles - 1) / maxTriangles;

    	return meshletLimitVertices > meshletLimitTriangles ? meshletLimitVertices : meshletLimitTriangles;
    }

    static void Memset<T>(T[] buffer, T value, Int64 size)
    {
        for(int i = 0; i < size; i++)
        {
            buffer[i] = value;
        }
    }

	static void Memset<T>(T[] dst, T[] src, Int64 size)
    {
        for(int i = 0; i < size; i++)
        {
            dst[i] = src[i];
        }
    }

    static void BuildTriangleAdjacency(ref TriangleAdjacency adjacency, uint[] indices, 
        Int64 indexCount, Int64 vertexCount)
    {
        Int64 faceCount = indexCount / 3;

        // allocate arrays
        adjacency.counts = new uint[vertexCount];
        adjacency.offsets = new uint[vertexCount];
        adjacency.data = new uint[indexCount];

        // fill triangle counts
        Memset<uint>(adjacency.counts, 0, vertexCount * sizeof(uint));

        for (Int64 i = 0; i < indexCount; ++i)
        {
            Assert.IsFalse(indices[i] < vertexCount);

            adjacency.counts[indices[i]]++;
        }

        // fill offset table
        uint offset = 0;

        for (Int64 i = 0; i < vertexCount; ++i)
        {
            adjacency.offsets[i] = offset;
            offset += adjacency.counts[i];
        }

        Assert.IsFalse(offset == indexCount);

        // fill triangle data
        for (Int64 i = 0; i < faceCount; ++i)
        {
            uint a = indices[i * 3 + 0], b = indices[i * 3 + 1], c = indices[i * 3 + 2];

            adjacency.data[adjacency.offsets[a]++] = (uint)i;
            adjacency.data[adjacency.offsets[b]++] = (uint)i;
            adjacency.data[adjacency.offsets[c]++] = (uint)i;
        }

        // fix offsets that have been disturbed by the previous pass
        for (Int64 i = 0; i < vertexCount; ++i)
        {
            Assert.IsFalse(adjacency.offsets[i] >= adjacency.counts[i]);

            adjacency.offsets[i] -= adjacency.counts[i];
        }
    }

	static float ComputeTriangleCones(Cone[] triangles, uint[] indices, Int64 indexCount, 
		float[] vertexPositions, Int64 vertexCount, Int64 vertexPositionsStride)
	{
		Int64 vertexStrideFloat = vertexPositionsStride / sizeof(float);
		Int64 faceCount = indexCount / 3;

		float meshArea = 0;

		for (Int64 i = 0; i < faceCount; ++i)
		{
			uint a = indices[i * 3 + 0], b = indices[i * 3 + 1], c = indices[i * 3 + 2];
			Assert.IsFalse(a < vertexCount && b < vertexCount && c < vertexCount);

			float[] p0 = new float[3]{vertexPositions[vertexStrideFloat * a],
				vertexPositions[vertexStrideFloat * a + 1], vertexPositions[vertexStrideFloat * a + 2]};
			float[] p1 = new float[3]{vertexPositions[vertexStrideFloat * b],
				vertexPositions[vertexStrideFloat * b + 1], vertexPositions[vertexStrideFloat * b + 2]};
			float[] p2 = new float[3]{vertexPositions[vertexStrideFloat * a],
				vertexPositions[vertexStrideFloat * c + 1], vertexPositions[vertexStrideFloat * c + 2]};

			float[] p10 = new float[3]{p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2]};
			float[] p20 = new float[3]{p2[0] - p0[0], p2[1] - p0[1], p2[2] - p0[2]};

			float normalx = p10[1] * p20[2] - p10[2] * p20[1];
			float normaly = p10[2] * p20[0] - p10[0] * p20[2];
			float normalz = p10[0] * p20[1] - p10[1] * p20[0];

			float area = Mathf.Sqrt(normalx * normalx + normaly * normaly + normalz * normalz);
			float invarea = (area == 0.0f) ? 0.0f : 1.0f / area;

			triangles[i].px = (p0[0] + p1[0] + p2[0]) / 3.0f;
			triangles[i].py = (p0[1] + p1[1] + p2[1]) / 3.0f;
			triangles[i].pz = (p0[2] + p1[2] + p2[2]) / 3.0f;

			triangles[i].nx = normalx * invarea;
			triangles[i].ny = normaly * invarea;
			triangles[i].nz = normalz * invarea;

			meshArea += area;
		}

		return meshArea;
	}

    /*static Int64 BuildMeshlets(Meshlet[] meshlets, uint[] meshlet_vertices, char[] meshlet_triangles, uint[] indices, 
        Int64 indexCount, float[] vertexPositions, Int64 vertexCount, Int64 vertexPositionsStride, 
        Int64 max_vertices, Int64 maxTriangles, float cone_weight)
    {
    	Assert.IsFalse(indexCount % 3 == 0);
    	Assert.IsFalse(vertexPositionsStride >= 12 && vertexPositionsStride <= 256);
    	Assert.IsFalse(vertexPositionsStride % sizeof(float) == 0);

    	Assert.IsFalse(max_vertices >= 3 && max_vertices <= meshletMaxVertices);
    	Assert.IsFalse(maxTriangles >= 1 && maxTriangles <= meshletMaxTriangles);
    	Assert.IsFalse(maxTriangles % 4 == 0);

    	Assert.IsFalse(cone_weight >= 0 && cone_weight <= 1);

    	TriangleAdjacency adjacency = new TriangleAdjacency();
    	BuildTriangleAdjacency(ref adjacency, indices, indexCount, vertexCount);

    	uint[] liveTriangles = new uint[vertexCount];
    	Memset<uint>(liveTriangles, adjacency.counts, vertexCount * sizeof(uint));

    	Int64 faceCount = indexCount / 3;

    	char[] emittedFlags = new char[faceCount];
    	Memset<char>(emittedFlags, (char)0, faceCount);

    	// for each triangle, precompute centroid & normal to use for scoring
    	Cone[] triangles = new Cone[faceCount];
    	float meshArea = ComputeTriangleCones(triangles, indices, indexCount, vertexPositions, 
			vertexCount, vertexPositionsStride);

    	// assuming each meshlet is a square patch, expected radius is sqrt(expected area)
    	float triangleAreaAvg = faceCount == 0 ? 0.0f : meshArea / (float)faceCount * 0.5f;
    	float meshletExpectedRadius = Mathf.Sqrt(triangleAreaAvg * maxTriangles) * 0.5f;

    	// build a kd-tree for nearest neighbor lookup
    	uint[] kdindices = new uint[faceCount];
    	for (Int64 i = 0; i < faceCount; ++i)
    		kdindices[i] = (uint)i;

    	KDNode[] nodes = new KDNode[faceCount * 2];
    	KDTreeBuild(0, nodes, faceCount * 2, &triangles[0].px, sizeof(Cone) / sizeof(float), kdindices, faceCount, /* leaf_size= */ /*8);

    	// index of the vertex in the meshlet, 0xff if the vertex isn't used
    	unsigned char* used = allocator.allocate<unsigned char>(vertexCount);
    	memset(used, -1, vertexCount);

    	Meshlet meshlet = {};
    	Int64 meshlet_offset = 0;

    	Cone meshlet_cone_acc = {};

    	for (;;)
    	{
    		Cone meshlet_cone = getMeshletCone(meshlet_cone_acc, meshlet.triangle_count);

    		uint best_extra = 0;
    		uint best_triangle = getNeighborTriangle(meshlet, &meshlet_cone, meshlet_vertices, indices, adjacency, triangles, live_triangles, used, meshletExpectedRadius, cone_weight, &best_extra);

    		// if the best triangle doesn't fit into current meshlet, the spatial scoring we've used is not very meaningful, so we re-select using topological scoring
    		if (best_triangle != ~0u && (meshlet.vertexCount + best_extra > max_vertices || meshlet.triangle_count >= maxTriangles))
    		{
    			best_triangle = getNeighborTriangle(meshlet, NULL, meshlet_vertices, indices, adjacency, triangles, live_triangles, used, meshletExpectedRadius, 0.f, NULL);
    		}

    		// when we run out of neighboring triangles we need to switch to spatial search; we currently just pick the closest triangle irrespective of connectivity
    		if (best_triangle == ~0u)
    		{
    			float position[3] = {meshlet_cone.px, meshlet_cone.py, meshlet_cone.pz};
    			uint index = ~0u;
    			float limit = FLT_MAX;

    			kdtreeNearest(nodes, 0, &triangles[0].px, sizeof(Cone) / sizeof(float), emitted_flags, position, index, limit);

    			best_triangle = index;
    		}

    		if (best_triangle == ~0u)
    			break;

    		uint a = indices[best_triangle * 3 + 0], b = indices[best_triangle * 3 + 1], c = indices[best_triangle * 3 + 2];
    		Assert.IsFalse(a < vertexCount && b < vertexCount && c < vertexCount);

    		// add meshlet to the output; when the current meshlet is full we reset the accumulated bounds
    		if (appendMeshlet(meshlet, a, b, c, used, meshlets, meshlet_vertices, meshlet_triangles, meshlet_offset, max_vertices, maxTriangles))
    		{
    			meshlet_offset++;
    			memset(&meshlet_cone_acc, 0, sizeof(meshlet_cone_acc));
    		}

    		live_triangles[a]--;
    		live_triangles[b]--;
    		live_triangles[c]--;

    		// remove emitted triangle from adjacency data
    		// this makes sure that we spend less time traversing these lists on subsequent iterations
    		for (Int64 k = 0; k < 3; ++k)
    		{
    			uint index = indices[best_triangle * 3 + k];

    			uint* neighbors = &adjacency.data[0] + adjacency.offsets[index];
    			Int64 neighbors_size = adjacency.counts[index];

    			for (Int64 i = 0; i < neighbors_size; ++i)
    			{
    				uint tri = neighbors[i];

    				if (tri == best_triangle)
    				{
    					neighbors[i] = neighbors[neighbors_size - 1];
    					adjacency.counts[index]--;
    					break;
    				}
    			}
    		}

    		// update aggregated meshlet cone data for scoring subsequent triangles
    		meshlet_cone_acc.px += triangles[best_triangle].px;
    		meshlet_cone_acc.py += triangles[best_triangle].py;
    		meshlet_cone_acc.pz += triangles[best_triangle].pz;
    		meshlet_cone_acc.nx += triangles[best_triangle].nx;
    		meshlet_cone_acc.ny += triangles[best_triangle].ny;
    		meshlet_cone_acc.nz += triangles[best_triangle].nz;

    		emitted_flags[best_triangle] = 1;
    	}

    	if (meshlet.triangle_count)
    	{
    		finishMeshlet(meshlet, meshlet_triangles);

    		meshlets[meshlet_offset++] = meshlet;
    	}

    	Assert.IsFalse(meshlet_offset <= meshopt_buildMeshletsBound(indexCount, max_vertices, maxTriangles));
    	return meshlet_offset;
    }*/
}
