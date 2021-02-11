// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.IO;
using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{
    public static class ModelGeometryBaker
    {
        public static readonly string bakedPartGeometryPath = "Assets/LEGO Data/BakedPartGeometry";

        //[MenuItem("LEGO Tools/Dev/Bake Model Geometry")]
        public static void BakeModelGeometry()
        {
            var selection = Selection.activeTransform;

            var groups = selection.GetComponentsInChildren<ModelGroup>();
            foreach(var group in groups)
            {
                if (group.processed)
                {
                    Debug.LogError("Cannot bake model with processed groups.");
                    return;
                }
            }

            var parts = selection.GetComponentsInChildren<Part>();

            foreach (var part in parts)
            {
                if (part.legacy)
                {
                    Debug.LogError("Cannot bake model with legacy parts.");
                    return;
                }
            }

            foreach (var part in parts)
            {
                var shell = part.transform.Find("Shell");
                if (shell)
                {
                    BakeAndSetMesh(shell, "Shell " + part.name, shell.GetComponent<MeshFilter>().sharedMesh);
                }
                var decorationSurfaces = part.transform.Find("DecorationSurfaces");
                if (decorationSurfaces)
                {
                    foreach (Transform decorationSurface in decorationSurfaces)
                    {
                        BakeAndSetMesh(decorationSurface, decorationSurface.name + " " + part.name, decorationSurface.GetComponent<MeshFilter>().sharedMesh);
                    }
                }
                var colourChangeSurfaces = part.transform.Find("ColourChangeSurfaces");
                if (colourChangeSurfaces)
                {
                    foreach (Transform colourChangeSurface in colourChangeSurfaces)
                    {
                        BakeAndSetMesh(colourChangeSurface, colourChangeSurface.name + " " + part.name, colourChangeSurface.GetComponent<MeshFilter>().sharedMesh);
                    }
                }
            }
        }

        //[MenuItem("LEGO Tools/Dev/Bake Model Geometry", true)]
        public static bool ValidateBakeModelGeometry()
        {
            var selection = Selection.activeTransform;
            if (selection)
            {
                return selection.GetComponent<Model>();
            }

            return false;
        }

        private static void BakeAndSetMesh(Transform target, string name, Mesh mesh)
        {
            var meshPath = Path.Combine(bakedPartGeometryPath, name + ".asset");
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (!existingMesh)
            {
                // Clone mesh.
                existingMesh = Object.Instantiate(mesh);

                var directoryName = Path.GetDirectoryName(meshPath);
                if (directoryName.Length > 0)
                {
                    Directory.CreateDirectory(directoryName);
                }

                AssetDatabase.CreateAsset(existingMesh, meshPath);
            }

            var meshFilter = target.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = existingMesh;
        }
    }
}

