// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{

    public class MeshHelper
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs0 = new List<Vector2>();
        public List<Color32> color32s = new List<Color32>();
        public List<int> triangles = new List<int>();

        public Bounds bounds;

        public MeshHelper()
        {
        }

        public MeshHelper(MeshHelper m)
        {
            vertices.AddRange(m.vertices);
            normals.AddRange(m.normals);
            uvs0.AddRange(m.uvs0);
            color32s.AddRange(m.color32s);
            triangles.AddRange(m.triangles);

            bounds = m.bounds;
        }

        public MeshHelper(Mesh m)
        {
            m.GetVertices(vertices);
            m.GetNormals(normals);
            m.GetUVs(0, uvs0);
            m.GetColors(color32s);
            m.GetTriangles(triangles, 0);
            bounds = m.bounds;
        }

        public void AddNormalNoise(float scale = 0.58f, float amplitude = 0.05f)
        {
            Vector3 phase = new Vector3(Random.value, Random.value, Random.value);
            for (int j = 0; j < normals.Count; ++j)
            {
                Vector3 pha = phase + vertices[j] * scale;
                Vector3 noise = new Vector3(Mathf.Sin(pha.x - pha.z), 0, Mathf.Sin(pha.z + pha.y)) * amplitude;
                normals[j] += noise;
                normals[j].Normalize();
            }
        }

        public void SetColor(Color32 color)
        {
            Color32 c32 = color;

            color32s.Clear();
            color32s.Capacity = vertices.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                color32s.Add(c32);
            }
        }

        public void SetColor(Material material)
        {
            // Get color, depending on color space.
            Color32 color;
            if (material)
            {
                if (material.HasProperty("_BaseColor"))
                {
                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    {
                        color = material.GetColor("_BaseColor").linear;
                    }
                    else
                    {
                        color = material.GetColor("_BaseColor");
                    }
                }
                else
                {
                    if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    {
                        color = material.GetColor("_Color").linear;
                    }
                    else
                    {
                        color = material.GetColor("_Color");
                    }
                }
                SetColor(color);
            }
            else
            {
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                {
                    SetColor(Color.magenta.linear);
                }
                else
                {
                    SetColor(Color.magenta);
                }
            }
        }

        public void SetUV(Vector2 uv)
        {
            uvs0.Clear();
            uvs0.Capacity = vertices.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                uvs0.Add(uv);
            }
        }

        public void Transform(Matrix4x4 matrix)
        {
            bounds = new Bounds(matrix.MultiplyPoint(vertices[0]), Vector3.zero);
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertices[i] = matrix.MultiplyPoint(vertices[i]);
                normals[i] = matrix.MultiplyVector(normals[i]);
                bounds.Encapsulate(vertices[i]);
            }
        }

        public void Combine(MeshHelper h)
        {
            int voffset = vertices.Count;

            vertices.AddRange(h.vertices);
            normals.AddRange(h.normals);
            uvs0.AddRange(h.uvs0);
            color32s.AddRange(h.color32s);

            triangles.Capacity += h.triangles.Count;
            for (int i = 0; i < h.triangles.Count; ++i)
            {
                triangles.Add(h.triangles[i] + voffset);
            }

            bounds.Encapsulate(h.bounds);
        }

        struct TriSort
        {
            public float dot;
            public int v0, v1, v2;
            public TriSort(float d, int t0, int t1, int t2)
            {
                dot = d;
                v0 = t0;
                v1 = t1;
                v2 = t2;
            }
        };

        public void RemoveUnusedVertices()
        {
            // Find out which vertices are used
            int[] vertexMap = new int[vertices.Count];
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertexMap[i] = -1;
            }

            MeshHelper mh = new MeshHelper();
            int v = 0;
            for (int i = 0; i < triangles.Count; ++i)
            {
                int v0 = triangles[i];
                if (vertexMap[v0] < 0)
                {
                    vertexMap[v0] = v;

                    mh.vertices.Add(vertices[v0]);
                    if (normals.Count > v)
                        mh.normals.Add(normals[v0]);
                    if (uvs0.Count > v)
                        mh.uvs0.Add(uvs0[v0]);
                    if (color32s.Count > v)
                        mh.color32s.Add(color32s[v0]);

                    v++;
                }
                triangles[i] = vertexMap[v0];
            }

            vertices = mh.vertices;
            normals = mh.normals;
            uvs0 = mh.uvs0;
            color32s = mh.color32s;
        }

        public void Sort(Vector3 dir)
        {
            int t = 0;
            int i = 0;
            TriSort[] triDot = new TriSort[triangles.Count / 3];
            for (i = 0; i < triangles.Count; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                Vector3 triCen = vertices[v0] + vertices[v1] + vertices[v2];
                triDot[t] = new TriSort(Vector3.Dot(dir, triCen), v0, v1, v2);
                t++;
            }

            System.Array.Sort(triDot, (x, y) => y.dot.CompareTo(x.dot));

            i = 0;
            for (t = 0; t < triDot.Length; ++t)
            {
                triangles[i] = triDot[t].v0;
                triangles[i + 1] = triDot[t].v1;
                triangles[i + 2] = triDot[t].v2;
                i += 3;
            }
        }

        public void ToMesh(Mesh m, bool computeLightMapUVs)
        {
            m.Clear();
            m.indexFormat = triangles.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(vertices);
            m.SetNormals(normals);
            if (uvs0.Count == vertices.Count)
            {
                m.SetUVs(0, uvs0);
            }
            if (color32s.Count == vertices.Count)
            {
                m.SetColors(color32s);
            }

            m.subMeshCount = 1;
            m.SetTriangles(triangles, 0);

            if (computeLightMapUVs)
            {
                UnwrapParam param;
                UnwrapParam.SetDefaults(out param);
                param.packMargin = 0.02f;
                Unwrapping.GenerateSecondaryUVSet(m, param);
            }

            m.RecalculateBounds();
            // TODO: Use bounds
        }

    }

}