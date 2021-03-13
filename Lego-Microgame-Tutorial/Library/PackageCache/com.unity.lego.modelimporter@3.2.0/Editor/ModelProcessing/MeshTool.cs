// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{

    public class MeshTool
    {

        public bool traceDebug = false;

        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector4[] tangents;
        private Vector2[] uvs;
        private Color32[] colors;
        private int[] tris;

        private Vector3[] smoothNormals;
        private int[] vertexMap;
        private int[] vertexCollapse;
        private int[] vertexFlags;

        private List<Edge> edges;
        private Bounds bounds;

        public MeshTool(Mesh source)
        {
            vertices = source.vertices;
            normals = source.normals;
            tangents = source.tangents;
            uvs = source.uv;
            tris = source.triangles;

            bounds = source.bounds;

            ComputeVertexMap();
        }

        public void ApplyTo(Mesh target, bool recalculateTangents, bool recalculateLightMapUVs)
        {
            target.Clear(false);
            target.vertices = vertices;
            if (normals != null)
                target.normals = normals;
            if (tangents != null)
                target.tangents = tangents;
            if (uvs != null)
                target.SetUVs(0, new List<Vector2>(uvs));
            if (colors != null)
                target.colors32 = colors;

            target.SetTriangles(tris, 0);

            if (recalculateTangents)
                target.RecalculateTangents();

            UnwrapParam param;
            UnwrapParam.SetDefaults(out param);
            param.packMargin = 0.02f;

            if (recalculateLightMapUVs)
                Unwrapping.GenerateSecondaryUVSet(target, param);
        }


        public class Edge
        {
            public int v0, v1;
            public int mapV0, mapV1;
            public int neighbour;
            public int mapNeighbour;
            public int tri;
        }

        public enum EdgeLoopType
        {
            // First part of edge loop detection.
            TopInnerKnobEdge,
            TopOuterKnobEdge,
            BottomInnerTubeEdge,
            BottomOuterTubeEdge,

            // Second part of edge loop detection.
            TopKnob,
            TopHollowKnob,
            BottomTube,
            BottomPin,
            Unknown
        }

        public struct VertexFlag
        {
            public static readonly int Interior = 1;
            public static readonly int TopKnob = 2;
            public static readonly int TopHollowKnob = 4;
            public static readonly int BottomTube = 8;
            public static readonly int BottomPin = 16;
        }

        public class EdgeLoop
        {
            public List<Edge> edges = new List<Edge>();
            public float length = 0;

            public bool isPlanar = false;
            public Vector3 center;
            public Vector3 normal;

            public int centerVertex = -1;
            public EdgeLoopType loopType;

            public bool isClosed
            {
                get
                {
                    return (firstVertex == lastVertex);
                }
            }

            public int firstVertex
            {
                get { return edges[0].v0; }
            }
            public int lastVertex
            {
                get { return edges[edges.Count - 1].v1; }
            }

            public void Merge(bool first, Edge e, float edgeLength)
            {
                if (first)
                {
                    edges.Insert(0, e);
                }
                else
                {
                    edges.Add(e);
                }
                length += edgeLength;
            }

            public void Merge(bool first, EdgeLoop loop)
            {
                if (first)
                {
                    edges.InsertRange(0, loop.edges);
                }
                else
                {
                    edges.AddRange(loop.edges);
                }
                length += loop.length;
            }

            public void ComputeIsPlanar(Vector3[] vertices, Vector3[] normals)
            {
                isPlanar = true;
                for (int i = 1; i < edges.Count; ++i)
                {
                    if (Vector3.Dot(normals[edges[i].v0], normals[edges[0].v0]) < 0.999f)
                    {
                        isPlanar = false;
                        return;
                    }
                }
            }

            public void ComputeCenterNormal(Vector3[] vertices, Vector3[] normals)
            {
                normal = Vector3.zero;
                center = Vector3.zero;
                for (int i = 0; i < edges.Count; ++i)
                {
                    normal += normals[edges[i].v0];
                    center += vertices[edges[i].v0];
                }
                center /= edges.Count;
                normal = normal.normalized;
            }
        }

        static void AddEdge(ref List<Edge> edges, ref List<int>[] openEdges, ref List<int>[] openMapEdges, int[] vertexMap, int v0, int v1, int tri)
        {
            int minV = Mathf.Min(v0, v1);

            int neighbour = -1;
            for (int e = 0; e < openEdges[minV].Count; ++e)
            {
                int i = openEdges[minV][e];
                Edge ee = edges[i];

                if ((ee.v0 == v1 && ee.v1 == v0) || (ee.v1 == v0 && ee.v0 == v1))
                {
                    neighbour = i;
                    break;
                }
            }

            int mapNeighbour = -1;
            int mapV0 = vertexMap[v0];
            int mapV1 = vertexMap[v1];

            int minMapV = Mathf.Min(mapV0, mapV1);
            for (int e = 0; e < openMapEdges[minMapV].Count; ++e)
            {
                int i = openMapEdges[minMapV][e];
                Edge ee = edges[i];

                if ((ee.mapV0 == mapV1 && ee.mapV1 == mapV0) || (ee.mapV1 == mapV0 && ee.mapV0 == mapV1))
                {
                    mapNeighbour = i;
                    break;
                }
            }

            Edge edge = new Edge();
            edge.v0 = v0;
            edge.v1 = v1;
            edge.mapV0 = mapV0;
            edge.mapV1 = mapV1;
            edge.neighbour = neighbour;
            edge.mapNeighbour = mapNeighbour;
            edge.tri = tri;

            int edgeIndex = edges.Count;

            if (neighbour >= 0)
            {
                edges[neighbour].neighbour = edgeIndex;
                openEdges[minV].Remove(neighbour);
            }
            else
            {
                openEdges[minV].Add(edgeIndex);
            }

            if (mapNeighbour >= 0)
            {
                edges[mapNeighbour].mapNeighbour = edgeIndex;
                openMapEdges[minMapV].Remove(mapNeighbour);
            }
            else
            {
                openMapEdges[minMapV].Add(edgeIndex);
            }
            edges.Add(edge);
        }

        static public void GenerateEdgeList(int[] vertexMap, int[] tris, out List<Edge> edges)
        {
            List<int>[] openEdges = new List<int>[vertexMap.Length];
            List<int>[] openMapEdges = new List<int>[vertexMap.Length];
            for (int i = 0; i < openEdges.Length; ++i)
            {
                openEdges[i] = new List<int>();
                openMapEdges[i] = new List<int>();
            }

            edges = new List<Edge>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                AddEdge(ref edges, ref openEdges, ref openMapEdges, vertexMap, tris[i], tris[i + 1], i);
                AddEdge(ref edges, ref openEdges, ref openMapEdges, vertexMap, tris[i + 1], tris[i + 2], i + 1);
                AddEdge(ref edges, ref openEdges, ref openMapEdges, vertexMap, tris[i + 2], tris[i], i + 2);
            }
        }

        public void ColorVertexFlags()
        {

            Color32[] flags = new Color32[] { Color.white, Color.red, Color.green, Color.yellow, Color.blue, Color.magenta, Color.white, Color.white, Color.grey, Color.black };

            colors = new Color32[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                colors[i] = flags[vertexFlags[i]];
            }
        }

        public void GenerateChamfer(float scale, float chamferSize = 0.02f)
        {
            Vector3[] push = new Vector3[vertices.Length];
            float[] pushScale = new float[vertices.Length];

            if (edges == null)
            {
                GenerateEdgeList(vertexMap, tris, out edges);
            }

            // Classify edges!
            for (int i = 0; i < edges.Count; ++i)
            {
                Edge e0 = edges[i];
                if (e0.neighbour < 0 && e0.mapNeighbour >= 0)
                {
                    Edge e1 = edges[e0.mapNeighbour];

                    int t0 = (e0.tri / 3) * 3;
                    int t1 = (e1.tri / 3) * 3;

                    // Classify edge
                    // If center of t1 is in front of plane from t0, then it's a crease and we need to push in the opposite direction
                    Vector3 center0 = (vertices[tris[t0]] + vertices[tris[t0 + 1]] + vertices[tris[t0 + 2]]) / 3.0f;
                    Vector3 normal0 = MathUtils.TriangleNormal(vertices[tris[t0]], vertices[tris[t0 + 1]], vertices[tris[t0 + 2]]);

                    Vector3 center1 = (vertices[tris[t1]] + vertices[tris[t1 + 1]] + vertices[tris[t1 + 2]]) / 3.0f;

                    Plane pl = new Plane(normal0, center0);
                    float dir = pl.GetDistanceToPoint(center1) > 0 ? 1.0f : -1.0f;

                    // Add push to each vertex
                    push[e0.v0] += Vector3.ProjectOnPlane(normals[e1.v1], normals[e0.v0]) * dir;
                    push[e0.v1] += Vector3.ProjectOnPlane(normals[e1.v0], normals[e0.v1]) * dir;

                    push[e1.v0] += Vector3.ProjectOnPlane(normals[e0.v1], normals[e1.v0]) * dir;
                    push[e1.v1] += Vector3.ProjectOnPlane(normals[e0.v0], normals[e1.v1]) * dir;

                    pushScale[e0.v0]++;
                    pushScale[e0.v1]++;
                    pushScale[e1.v0]++;
                    pushScale[e1.v1]++;
                }
            }

            // Push vertices on plane
            for (int i = 0; i < vertices.Length; ++i)
            {
                if (pushScale[i] > 0 && push[i].magnitude > 0.0001f)
                {
                    push[i].Normalize();
                }
            }

            // Connect open edges
            List<int> chamferTris = new List<int>();
            int chamfEdges = 0;
            for (int i = 0; i < edges.Count; ++i)
            {
                if (edges[i].neighbour < 0 && edges[i].mapNeighbour >= 0)
                {
                    Edge e0 = edges[i];
                    Edge e1 = edges[e0.mapNeighbour];
                    int v0 = e0.v0;
                    int v1 = e0.v1;
                    int v2 = e1.v0;
                    int v3 = e1.v1;

                    if (ContainsTriangle(chamferTris, v1, v0, v2) || ContainsTriangle(chamferTris, v2, v0, v3))
                    {
                        //Debug.Log("Skipping already connected edge");
                        continue;
                    }

                    // Probably should check if we're actually pushing any of these

                    chamferTris.Add(v1); chamferTris.Add(v0); chamferTris.Add(v2);
                    chamferTris.Add(v2); chamferTris.Add(v0); chamferTris.Add(v3);
                    chamfEdges++;
                }
            }

            // Connect open vertices
            int chamfVerts = 0;
            for (int i = 0; i < vertices.Length; ++i)
            {
                if (vertexCollapse[i] >= 3)
                {
                    List<int> poly = new List<int>();
                    for (int j = i; j < vertices.Length; ++j)
                    {
                        if (vertexMap[j] == i)
                        {
                            poly.Add(j);
                        }
                    }

                    // 

                    // TODO: proper triangulation!
                    for (int j = 1; j < poly.Count - 1; ++j)
                    {
                        int v0 = poly[0];
                        int v1 = poly[j];
                        int v2 = poly[j + 1];

                        Vector3 n = MathUtils.TriangleNormal(vertices[v0] + push[v0] * 0.01f, vertices[v1] + push[v1] * 0.01f, vertices[v2] + push[v2] * 0.01f);
                        if (Vector3.Dot(n, normals[v0]) < 0)
                        {
                            // Swap
                            int t = v1;
                            v1 = v2;
                            v2 = t;
                        }
                        chamferTris.Add(v0);
                        chamferTris.Add(v1);
                        chamferTris.Add(v2);
                    }

                    chamfVerts++;
                }
            }

            // Move chamfer vertices.
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] += push[i] * chamferSize * scale;
            }

            //Debug.Log("Chamfered Edges: " + chamfEdges + " Chamfered Verts: " + chamfVerts);
            // Add original triangles
            chamferTris.AddRange(tris);

            // Set tris.
            tris = chamferTris.ToArray();
        }

        private bool ContainsTriangle(List<int> triangles, int v0, int v1, int v2)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int tv0 = triangles[i];
                int tv1 = triangles[i + 1];
                int tv2 = triangles[i + 2];

                if ((v0 == tv0 && v1 == tv1 && v2 == tv2) ||
                    (v0 == tv0 && v1 == tv2 && v2 == tv1) ||
                    (v0 == tv1 && v1 == tv0 && v2 == tv2) ||
                    (v0 == tv1 && v1 == tv2 && v2 == tv0) ||
                    (v0 == tv2 && v1 == tv0 && v2 == tv1) ||
                    (v0 == tv2 && v1 == tv1 && v2 == tv0))
                {
                    return true;
                }
            }

            return false;
        }

        public void GenerateLegacyOptimizedMesh(Mesh target, bool keepBottomTubes, bool keepTopKnobs, bool keepChamfer, bool computeTangents)
        {
            if (vertexFlags == null)
            {
                vertexFlags = new int[vertices.Length];
            }

            int killFlags = keepTopKnobs ? 0 : VertexFlag.TopKnob | VertexFlag.TopKnob;
            killFlags |= keepBottomTubes ? 0 : VertexFlag.BottomTube | VertexFlag.BottomPin;

            // target tris
            List<int> tris1 = new List<int>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                int v0 = tris[i];
                int v1 = tris[i + 1];
                int v2 = tris[i + 2];

                // Skip chamfer tris 
                bool isZeroArea = (MathUtils.TriangleArea(vertices[v0], vertices[v1], vertices[v2]) < 0.0001f);

                int kill = vertexFlags[v0] | vertexFlags[v1] | vertexFlags[v2];

                // Keep or kill?
                if ((kill & killFlags) == 0)
                {
                    if (!isZeroArea || keepChamfer)
                    {
                        tris1.Add(v0); tris1.Add(v1); tris1.Add(v2);
                    }
                }
            }

            RemapToMesh(target, tris1, computeTangents);
        }

        private void RemapToMesh(Mesh target, List<int> triList, bool computeTangents)
        {
            // Remove unused vertices
            int[] remapper;
            int usedVertexCount = ComputeUsedVertices(ref triList, vertices.Length, out remapper);

            target.Clear();

            if (vertices != null)
                target.vertices = Remap(vertices, usedVertexCount, remapper);

            if (normals != null)
                target.normals = Remap(normals, usedVertexCount, remapper);

            if (tangents != null)
                target.tangents = Remap(tangents, usedVertexCount, remapper);

            if (uvs != null)
                target.uv = Remap(uvs, usedVertexCount, remapper);

            if (colors != null)
                target.colors32 = Remap(colors, usedVertexCount, remapper);

            if (triList != null)
                target.SetTriangles(triList, 0);

            if (computeTangents)
            {
                target.RecalculateTangents();
            }
        }

        static public int ComputeUsedVertices(ref List<int> tris, int vertexCount, out int[] vertexMap)
        {
            vertexMap = new int[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
            {
                vertexMap[i] = -1;
            }

            int usedVertexCount = 0;
            for (int i = 0; i < tris.Count; ++i)
            {
                int v = tris[i];
                if (vertexMap[v] < 0)
                {
                    vertexMap[v] = usedVertexCount;
                    usedVertexCount++;
                }
                tris[i] = vertexMap[v];
            }

            return usedVertexCount;
        }

        static public List<T> Remap<T>(List<T> ts, int usedCount, int[] vertexMap) where T : struct
        {
            if (ts == null || ts.Count == 0)
                return ts;

            List<T> result = new List<T>(usedCount);
            for (int i = 0; i < usedCount; ++i)
            {
                result.Add(new T());
            }
            for (int i = 0; i < vertexMap.Length; ++i)
            {
                if (vertexMap[i] >= 0)
                {
                    result[vertexMap[i]] = ts[i];
                }
            }
            return result;
        }

        static public T[] Remap<T>(T[] ts, int usedCount, int[] vertexMap)
        {
            if (ts == null || ts.Length == 0)
                return ts;

            T[] result = new T[usedCount];
            for (int i = 0; i < vertexMap.Length; ++i)
            {
                if (vertexMap[i] >= 0)
                {
                    result[vertexMap[i]] = ts[i];
                }
            }
            return result;
        }

        public void LocateKnobsAndTubes(int knobEdgeCount = 12, float scale = 1.0f)
        {
            if (vertexFlags == null)
            {
                vertexFlags = new int[vertices.Length];
            }

            if (uvs.Length != vertices.Length)
            {
                uvs = new Vector2[vertices.Length];
            }

            if (edges == null)
            {
                GenerateEdgeList(vertexMap, tris, out edges);
            }

            // Create a map of verts to edges
            List<int>[] vertToEdge = new List<int>[vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertToEdge[i] = new List<int>();
            }
            for (int i = 0; i < edges.Count; ++i)
            {
                Edge edge = edges[i];
                vertToEdge[edge.v0].Add(i);
                vertToEdge[edge.v1].Add(i);
            }

            // Can be simplified if edge loops are ordered

            // Find edge loops
            Dictionary<int, EdgeLoop> vertToLoop = new Dictionary<int, EdgeLoop>();
            int vertCount = vertices.Length;

            List<EdgeLoop> edgeLoops = new List<EdgeLoop>();
            for (int i = 0; i < edges.Count; ++i)
            {
                Edge ee = edges[i];
                // Only open edges!
                if (ee.neighbour < 0)
                {
                    float edgeLength = (vertices[ee.v0] - vertices[ee.v1]).magnitude;

                    EdgeLoop el = new EdgeLoop();
                    el.Merge(false, ee, edgeLength);
                    edgeLoops.Add(el);

                    if (!vertToLoop.ContainsKey(el.firstVertex))
                        vertToLoop.Add(el.firstVertex, el);

                    if (!vertToLoop.ContainsKey(el.lastVertex + vertCount))
                        vertToLoop.Add(el.lastVertex + vertCount, el);
                }
            }

            // Merge edgeLoops
            EdgeLoop loop;
            for (int i = 0; i < edgeLoops.Count; ++i)
            {
                EdgeLoop edgeLoop = edgeLoops[i];

                bool merged = true;
                while (merged && !edgeLoop.isClosed)
                {
                    merged = false;

                    // Look for match to first vertex
                    if (vertToLoop.TryGetValue(edgeLoop.firstVertex + vertCount, out loop))
                    {
                        if (loop != edgeLoop)
                        {
                            // Remove old loop
                            vertToLoop.Remove(loop.firstVertex);
                            vertToLoop.Remove(loop.lastVertex + vertCount);

                            vertToLoop.Remove(edgeLoop.firstVertex);
                            edgeLoop.Merge(true, loop);
                            edgeLoops.Remove(loop);

                            vertToLoop.Add(edgeLoop.firstVertex, edgeLoop);

                            merged = true;
                        }
                    }

                    // Look for match to last vertex
                    if (vertToLoop.TryGetValue(edgeLoop.lastVertex, out loop))
                    {
                        if (loop != edgeLoop)
                        {
                            // Remove old loop
                            vertToLoop.Remove(loop.firstVertex);
                            vertToLoop.Remove(loop.lastVertex + vertCount);

                            vertToLoop.Remove(edgeLoop.lastVertex + vertCount);
                            edgeLoop.Merge(false, loop);
                            edgeLoops.Remove(loop);

                            vertToLoop.Add(edgeLoop.lastVertex + vertCount, edgeLoop);

                            merged = true;
                        }
                    }

                }


            }

            // Knobs have 12 edges
            // Top outer knob circumreference is approx 1.521897
            // Top inner knob circumreference is approx 0.9938246

            float topOuterKnobRad = 1.521897f;
            float topInnerKnobRad = 0.9938246f;
            float bottomOuterTubeRad = 2.049931f;
            float bottomInnerTubeRad = 1.521897f;
            float bottomPinRad = 0.993865f;

            // Slight difference for characters!
            if (knobEdgeCount == 16)
            {
                topInnerKnobRad = 0.9986858f;
            }

            for (int i = 0; i < edgeLoops.Count; ++i)
            {
                EdgeLoop edgeLoop = edgeLoops[i];
                edgeLoop.loopType = EdgeLoopType.Unknown;

                // Apply scale
                edgeLoop.length *= scale;

                if (edgeLoop.edges.Count == knobEdgeCount)
                {
                    // Top knobs and hollow knobs
                    bool topOuterKnobEdge = Mathf.Abs(edgeLoop.length - topOuterKnobRad) < 0.01f;
                    bool topInnerKnobEdge = Mathf.Abs(edgeLoop.length - topInnerKnobRad) < 0.01f;

                    // Bottom tubes
                    bool bottomOuterTubeEdge = Mathf.Abs(edgeLoop.length - bottomOuterTubeRad) < 0.01f;
                    bool bottomInnerTubeEdge = Mathf.Abs(edgeLoop.length - bottomInnerTubeRad) < 0.01f;

                    // Bottom pins
                    bool bottomShaft = Mathf.Abs(edgeLoop.length - bottomPinRad) < 0.02f;

                    edgeLoop.ComputeCenterNormal(vertices, normals);
                    edgeLoop.ComputeIsPlanar(vertices, normals);

                    if (edgeLoop.normal.y > 0.99f)
                    {
                        if (topOuterKnobEdge || topInnerKnobEdge)
                        {
                            if (edgeLoop.isPlanar)
                            {
                                edgeLoop.loopType = topOuterKnobEdge ? EdgeLoopType.TopOuterKnobEdge : EdgeLoopType.TopInnerKnobEdge;
                            }
                        }
                    }
                    else if (edgeLoop.normal.y < -0.99f)
                    {
                        if (bottomInnerTubeEdge || bottomOuterTubeEdge || bottomShaft)
                        {
                            if (edgeLoop.isPlanar)
                            {
                                edgeLoop.loopType = bottomShaft ? EdgeLoopType.BottomPin : bottomOuterTubeEdge ? EdgeLoopType.BottomOuterTubeEdge : EdgeLoopType.BottomInnerTubeEdge;
                            }
                        }
                    }
                }
            }

            // Find knobs and tubes
            for (int i = 0; i < edgeLoops.Count; ++i)
            {
                EdgeLoop edgeLoop = edgeLoops[i];

                // Find top knobs and hollow knobs
                if (edgeLoop.loopType == EdgeLoopType.TopInnerKnobEdge || edgeLoop.loopType == EdgeLoopType.TopOuterKnobEdge)
                {
                    for (int j = i + 1; j < edgeLoops.Count; ++j)
                    {
                        if (edgeLoops[j].loopType == edgeLoop.loopType || edgeLoops[j].loopType > EdgeLoopType.TopOuterKnobEdge)
                            continue;

                        if (Vector3.Distance(edgeLoops[j].center, edgeLoop.center) * scale < 0.001f)
                        {
                            edgeLoop.loopType = EdgeLoopType.TopHollowKnob;
                            edgeLoops[j].loopType = EdgeLoopType.TopHollowKnob;
                        }
                    }

                    // If we didn't find a buddy, this cannot be a hollow knob
                    if (edgeLoop.loopType == EdgeLoopType.TopInnerKnobEdge)
                    {
                        edgeLoop.loopType = EdgeLoopType.Unknown;
                    }
                    else if (edgeLoop.loopType == EdgeLoopType.TopOuterKnobEdge)
                    {
                        edgeLoop.loopType = EdgeLoopType.Unknown;

                        // Most knobs have a vertex in the center
                        for (int j = 0; j < vertices.Length; ++j)
                        {
                            if (Vector3.Distance(vertices[j], edgeLoop.center) * scale < 0.01f)
                            {
                                edgeLoop.centerVertex = j;
                                edgeLoop.loopType = EdgeLoopType.TopKnob;
                                break;
                            }
                        }

                        // But some may not have .. 
                        if (edgeLoop.centerVertex < 0)
                        {
                            int v0 = edgeLoop.edges[0].v0;
                            for (int t = 0; t < tris.Length; t += 3)
                            {
                                if (tris[t] == v0 || tris[t + 1] == v0 || tris[t + 2] == v0)
                                {
                                    Vector3 triCen = (vertices[tris[t]] + vertices[tris[t + 1]] + vertices[tris[t + 2]]) / 3;
                                    if (Vector3.Distance(triCen, edgeLoop.center) < Vector3.Distance(vertices[v0], edgeLoop.center))
                                    {
                                        edgeLoop.loopType = EdgeLoopType.TopKnob;
                                    }
                                    break;
                                }
                            }
                        }

                    }
                }
                // Find bottom tubes.
                else if (edgeLoop.loopType == EdgeLoopType.BottomInnerTubeEdge || edgeLoop.loopType == EdgeLoopType.BottomOuterTubeEdge)
                {
                    EdgeLoopType match = (edgeLoop.loopType == EdgeLoopType.BottomInnerTubeEdge) ? EdgeLoopType.BottomOuterTubeEdge : EdgeLoopType.BottomInnerTubeEdge;
                    for (int j = i + 1; j < edgeLoops.Count; ++j)
                    {
                        if (edgeLoops[j].loopType == match)
                        {
                            if (Vector3.Distance(edgeLoops[j].center, edgeLoop.center) * scale < 0.001f)
                            {
                                edgeLoop.loopType = EdgeLoopType.BottomTube;
                                edgeLoops[j].loopType = EdgeLoopType.BottomTube;
                                break;
                            }
                        }
                    }

                    // If we didn't find a match, this cannot be a bottom tube
                    if (edgeLoop.loopType != EdgeLoopType.BottomTube)
                    {
                        edgeLoop.loopType = EdgeLoopType.Unknown;
                    }
                }
            }

            for (int i = 0; i < edgeLoops.Count; ++i)
            {
                EdgeLoop edgeLoop = edgeLoops[i];
                if (edgeLoop.loopType == EdgeLoopType.Unknown)
                    continue;

                Matrix4x4 uvMatrix = Matrix4x4.TRS(edgeLoop.center, Quaternion.LookRotation(edgeLoop.normal) * Quaternion.Euler(0, 0, 90), new Vector3(0.48f / scale, -0.48f / scale, 0.48f / scale));
                uvMatrix = uvMatrix.inverse;

                if (edgeLoop.loopType == EdgeLoopType.TopKnob || edgeLoop.loopType == EdgeLoopType.TopHollowKnob || edgeLoop.loopType == EdgeLoopType.BottomTube || edgeLoop.loopType == EdgeLoopType.BottomPin)
                {
                    List<int> groupVerts = new List<int>();

                    // Collect all vertices related to this knob, hollow knob, tube or pin.
                    for (int k = 0; k < edgeLoop.edges.Count; ++k)
                    {
                        Edge ee = edgeLoop.edges[k];
                        for (int l = 0; l < vertToEdge[ee.v0].Count; ++l)
                        {
                            Edge el = edges[vertToEdge[ee.v0][l]];
                            groupVerts.Add(el.v0);
                            groupVerts.Add(el.v1);
                        }

                        if (ee.mapNeighbour >= 0)
                        {
                            ee = edges[ee.mapNeighbour];
                            for (int l = 0; l < vertToEdge[ee.v0].Count; ++l)
                            {
                                Edge el = edges[vertToEdge[ee.v0][l]];
                                groupVerts.Add(el.v0);
                                groupVerts.Add(el.v1);
                            }
                        }
                    }

                    if (edgeLoop.loopType == EdgeLoopType.TopKnob)
                    {
                        for (int k = 0; k < groupVerts.Count; ++k)
                        {
                            int v = groupVerts[k];
                            uvs[v] = uvMatrix.MultiplyPoint(vertices[v]) + new Vector3(0.5f, 0.5f, 0);
                            vertexFlags[v] |= VertexFlag.TopKnob;
                        }
                    }
                    else if (edgeLoop.loopType == EdgeLoopType.TopHollowKnob)
                    {
                        // Top knobs have an offset uv
                        for (int k = 0; k < groupVerts.Count; ++k)
                        {
                            int v = groupVerts[k];
                            uvs[v] = new Vector2(0.1f, 0.1f);
                            vertexFlags[v] |= VertexFlag.TopKnob;
                        }
                    }
                    else if (edgeLoop.loopType == EdgeLoopType.BottomTube)
                    {
                        for (int k = 0; k < groupVerts.Count; ++k)
                        {
                            int v = groupVerts[k];
                            vertexFlags[v] |= VertexFlag.BottomTube;
                        }
                    }
                    else
                    {
                        for (int k = 0; k < groupVerts.Count; ++k)
                        {
                            int v = groupVerts[k];
                            vertexFlags[v] |= VertexFlag.BottomPin;
                        }
                    }

                }

            }
        }

        void ComputeVertexMap()
        {
            vertexMap = new int[vertices.Length];
            vertexCollapse = new int[vertices.Length];

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertexMap[i] = i;
                vertexCollapse[i] = 1;
                for (int j = 0; j < i; ++j)
                {
                    if (Vector3.Distance(vertices[i], vertices[j]) < 0.0001f)
                    {
                        vertexMap[i] = j;
                        vertexCollapse[j]++; // Increase number of vertices mapped to this vertex
                        break;
                    }
                }
            }
        }

        public void GenerateKnobNormalMapUVs()
        {
            Matrix4x4 uvMatrix = Matrix4x4.TRS(Vector3.one * 0.5f, Quaternion.Euler(0, 90, 90), new Vector3(1.0f / 0.48f, 0.0f, -1.0f / 0.48f));

            for (int k = 0; k < vertices.Length; ++k)
            {
                uvs[k] = uvMatrix.MultiplyPoint(vertices[k]);
            }
        }

        public void ClearNormalMapUVs()
        {
            uvs = null;
        }

    }

}