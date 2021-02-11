// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LEGOModelImporter
{
    public class PickingMeshUtils
    {
        public static readonly string pickingMeshPath = "Assets/LEGO Data/Geometry/PickingMesh";

        public static bool CheckIfPickingMeshExists(string designID)
        {
            if (File.Exists(Path.Combine(pickingMeshPath, designID + ".asset")))
            {
                return true;
            }

            return false;
        }

        public static Mesh LoadPickingMesh(string designID)
        {
            return AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(pickingMeshPath, designID + ".asset"));
        }

        public static void SavePickingMesh(string designID, Mesh mesh)
        {
            if (!Directory.Exists(pickingMeshPath))
            {
                Directory.CreateDirectory(pickingMeshPath);
            }

            AssetDatabase.CreateAsset(mesh, Path.Combine(pickingMeshPath, designID + ".asset"));
            AssetDatabase.SaveAssets();
        }

        public static Mesh LoadMinifigPickingMesh()
        {
            if (!CheckIfPickingMeshExists("Minifig"))
            {
                // Create minifig picking mesh.
                var pickingMesh = new Mesh();

                Vector3[] vertices = {
                    new Vector3 (-0.8f,  0.0f, -0.4f),
                    new Vector3 ( 0.8f,  0.0f, -0.4f),
                    new Vector3 ( 0.8f, 3.84f, -0.4f),
                    new Vector3 (-0.8f, 3.84f, -0.4f),
                    new Vector3 (-0.8f, 3.84f,  0.4f),
                    new Vector3 ( 0.8f, 3.84f,  0.4f),
                    new Vector3 ( 0.8f,  0.0f,  0.4f),
                    new Vector3 (-0.8f,  0.0f,  0.4f),
                };
                int[] triangles = {
                    0, 2, 1, //face front
	                0, 3, 2,
                    2, 3, 4, //face top
	                2, 4, 5,
                    1, 2, 5, //face right
	                1, 5, 6,
                    0, 7, 4, //face left
	                0, 4, 3,
                    5, 4, 7, //face back
	                5, 7, 6,
                    0, 6, 7, //face bottom
	                0, 1, 6
                };

                pickingMesh.vertices = vertices;
                pickingMesh.triangles = triangles;

                SavePickingMesh("Minifig", pickingMesh);
            }

            return LoadPickingMesh("Minifig");
        }
    }
}
#endif