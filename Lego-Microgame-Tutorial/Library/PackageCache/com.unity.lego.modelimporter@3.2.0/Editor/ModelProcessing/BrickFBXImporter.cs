// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;
using System.IO;

namespace LEGOModelImporter
{
    public class BrickFBXImporter : AssetPostprocessor
    {
        public override uint GetVersion()
        {
            return base.GetVersion() + 156;
        }

        void OnPreprocessModel()
        {
            UnityEditor.ModelImporter i = (UnityEditor.ModelImporter)assetImporter;

            if (assetPath.Contains(PartUtility.geometryPath))
            {
                // FIXME Should not be necessary to set when only doing Editor time modifications - but there seems to be a regression from 2019.3.3 and forwards.
                i.isReadable = true;

                i.materialImportMode = ModelImporterMaterialImportMode.None;
                i.importLights = false;
                i.importCameras = false;

                i.importNormals = ModelImporterNormals.Import;
                i.importTangents = ModelImporterTangents.CalculateMikk;

                i.importVisibility = false;

                i.importAnimatedCustomProperties = false;
                i.importAnimation = false;
                i.importBlendShapes = false;
                i.animationType = ModelImporterAnimationType.None;

                i.indexFormat = ModelImporterIndexFormat.Auto;

                i.meshCompression = ModelImporterMeshCompression.Medium;

                i.generateSecondaryUV = false;

                i.globalScale = 100;
                i.useFileScale = true;
            }
        }

        void OnPostprocessModel(GameObject go)
        {
            bool partFBX = assetPath.Contains(PartUtility.geometryPath);
            bool newFBX = assetPath.Contains(PartUtility.newDir);
            bool legacyFBX = assetPath.Contains(PartUtility.legacyDir);
            bool commonPartFBX = assetPath.Contains(PartUtility.commonPartsDir);
            bool lightMappedFBX = assetPath.Contains(PartUtility.lightmappedDir);

            bool collapse = partFBX && legacyFBX;
            bool genLightmapUV = partFBX && lightMappedFBX;
            int lod = assetPath.Contains(PartUtility.lod0Dir) ? 0 : assetPath.Contains(PartUtility.lod1Dir) ? 1 : 2;

            int knobTubeOrPinEdges = 12;

            if (collapse)
            {
                Collapse(go);
            }

            // Process
            if (partFBX && legacyFBX)
            {
                ProcessLegacy(go, knobTubeOrPinEdges, genLightmapUV, lod);
            }
            else if (partFBX && newFBX)
            {
                ProcessNew(go, genLightmapUV, lod);
            }
            else if (partFBX && commonPartFBX)
            {
                ProcessCommon(go, genLightmapUV, lod);
            }
        }

        void ProcessLegacy(GameObject go, int knobTubeOrPinEdges, bool genLightmapUV, int lod)
        {
            MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
            for (int m = 0; m < mfs.Length; ++m)
            {
                MeshFilter mf = mfs[m];

                if (mf && mf.sharedMesh)
                {
                    /*
                    if (mf.sharedMesh.vertexCount > 16384)
                    {
                        Debug.Log("Skipping find knobs on large mesh " + go.name, go);
                        return;
                    }
                    */
                    MeshTool mt = new MeshTool(mf.sharedMesh);

                    // mt.traceDebug = true;

                    mt.LocateKnobsAndTubes(knobTubeOrPinEdges, mf.transform.localScale.x);

                    if (lod == 0)
                    {
                        mt.GenerateChamfer(1.0f / mf.transform.localScale.x);
                    }

                    Mesh[] optmizedMeshes = new Mesh[3];
                    bool[] exists = new bool[3];
                    string[] optimizedAssetsPath = new string[3];

                    string lodDir = lod == 0 ? PartUtility.lod0Dir : lod == 1 ? PartUtility.lod1Dir : PartUtility.lod2Dir;

                    optimizedAssetsPath[0] = Path.Combine(PartUtility.geometryPath, PartUtility.legacyDir, genLightmapUV ? PartUtility.lightmappedDir : "", "Optimized1", lodDir);
                    optimizedAssetsPath[1] = Path.Combine(PartUtility.geometryPath, PartUtility.legacyDir, genLightmapUV ? PartUtility.lightmappedDir : "", "Optimized2", lodDir);
                    optimizedAssetsPath[2] = Path.Combine(PartUtility.geometryPath, PartUtility.legacyDir, genLightmapUV ? PartUtility.lightmappedDir : "", "Optimized3", lodDir);

                    Directory.CreateDirectory(optimizedAssetsPath[0]);
                    Directory.CreateDirectory(optimizedAssetsPath[1]);
                    Directory.CreateDirectory(optimizedAssetsPath[2]);

                    for (int i = 0; i < optmizedMeshes.Length; ++i)
                    {
                        optimizedAssetsPath[i] = Path.Combine(optimizedAssetsPath[i], Path.GetFileNameWithoutExtension(assetPath) + ".asset");
                        optmizedMeshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(optimizedAssetsPath[i]);
                        exists[i] = (optmizedMeshes[i] != null);
                        if (optmizedMeshes[i] == null)
                        {
                            optmizedMeshes[i] = new Mesh();
                            optmizedMeshes[i].name = mf.sharedMesh.name + "_Optimized" + (i + 1);
                        }
                    }

                    mt.GenerateLegacyOptimizedMesh(optmizedMeshes[0], false, true, true, true);
                    mt.GenerateLegacyOptimizedMesh(optmizedMeshes[1], true, false, true, true);
                    mt.GenerateLegacyOptimizedMesh(optmizedMeshes[2], false, false, true, true);

                    for (int i = 0; i < optmizedMeshes.Length; ++i)
                    {
                        if (!exists[i])
                        {
                            AssetDatabase.CreateAsset(optmizedMeshes[i], optimizedAssetsPath[i]);
                        }
                    }

                    mt.ApplyTo(mf.sharedMesh, true, genLightmapUV);

                    EditorUtility.SetDirty(mf.sharedMesh);

                    System.GC.Collect();
                }
            }

        }

        void ProcessNew(GameObject go, bool genLightmapUV, int lod)
        {
            MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfs)
            {
                if (mf && mf.sharedMesh)
                {
                    // Get rid of knob, pin and tube geometry.
                    if (mf.name.StartsWith("knob_") || mf.name.StartsWith("pin_") || mf.name.StartsWith("tube_"))
                    {
                        Object.DestroyImmediate(mf.sharedMesh);
                        continue;
                    }

                    MeshTool mt = new MeshTool(mf.sharedMesh);

                    var decoration = mf.name.StartsWith("VME_");

                    // Skip decoration surfaces.
                    if (!decoration)
                    {
                        if (lod == 0)
                        {
                            mt.GenerateChamfer(1.0f / mf.transform.localScale.x);
                        }
                        mt.ClearNormalMapUVs();
                    }

                    mt.ApplyTo(mf.sharedMesh, false, genLightmapUV);

                    EditorUtility.SetDirty(mf.sharedMesh);

                    System.GC.Collect();
                }
            }

            // Get rid of knob and tube parent transforms.
            var knobs = go.transform.Find("Knobs");
            if (knobs)
            {
                Object.DestroyImmediate(knobs.gameObject);
            }
            var tubes = go.transform.Find("Tubes");
            if (tubes)
            {
                Object.DestroyImmediate(tubes.gameObject);
            }
        }

        void ProcessCommon(GameObject go, bool genLightmapUV, int lod)
        {
            MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfs)
            {
                if (mf && mf.sharedMesh)
                {
                    MeshTool mt = new MeshTool(mf.sharedMesh);

                    var normalMappedLogo = go.name.StartsWith("knob") && !go.name.EndsWith("C");

                    if (lod == 0)
                    {
                        mt.GenerateChamfer(1.0f / mf.transform.localScale.x);
                    }

                    if (normalMappedLogo)
                    {
                        mt.GenerateKnobNormalMapUVs();
                    }
                    else
                    {
                        mt.ClearNormalMapUVs();
                    }

                    mt.ApplyTo(mf.sharedMesh, normalMappedLogo, genLightmapUV);

                    EditorUtility.SetDirty(mf.sharedMesh);

                    System.GC.Collect();
                }
            }
        }

        void Collapse(GameObject go)
        {
            MeshHelper meshHelper = new MeshHelper();
            Mesh mesh = null;
            Material material = null;

            MeshRenderer[] mrs = go.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < mrs.Length; ++i)
            {
                if (mrs[i].name.Contains("Decoration"))
                {
                    Object.DestroyImmediate(mrs[i].GetComponent<MeshFilter>().sharedMesh);
                    continue;
                }
                material = mrs[i].sharedMaterial;
                if (mesh == null)
                {
                    mesh = mrs[i].GetComponent<MeshFilter>().sharedMesh;
                    meshHelper = new MeshHelper(mesh);
                    meshHelper.Transform(go.transform.worldToLocalMatrix * mrs[i].transform.localToWorldMatrix);
                }
                else
                {
                    Mesh m = mrs[i].GetComponent<MeshFilter>().sharedMesh;
                    MeshHelper mh = new MeshHelper(m);
                    mh.Transform(go.transform.worldToLocalMatrix * mrs[i].transform.localToWorldMatrix);
                    meshHelper.Combine(mh);

                    Object.DestroyImmediate(m);
                }
            }

            // Add mesh renderer + mesh filter to base gameobject
            meshHelper.ToMesh(mesh, false);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mr == null)
                mr = go.AddComponent<MeshRenderer>();
            if (mf == null)
                mf = go.AddComponent<MeshFilter>();

            mr.sharedMaterial = material;

            mf.sharedMesh = mesh;
            mesh.name = go.name;

            // Delete children
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            }

            EditorUtility.SetDirty(mesh);

            System.GC.Collect();
        }
    }

}