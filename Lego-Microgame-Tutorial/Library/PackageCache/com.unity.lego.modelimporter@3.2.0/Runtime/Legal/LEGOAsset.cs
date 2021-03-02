// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace LEGOModelImporter
{
    /// <summary>
    /// An asset with this script is considered to be a LEGO Asset
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ScaleChecker))]
    [RequireComponent(typeof(LEGOComponentsEnforcer))]
    public class LEGOAsset : MonoBehaviour
    {
        ScaleChecker scaleChecker;
        LEGOComponentsEnforcer componentsEnforcer;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
#if UNITY_EDITOR
        protected bool performedSetup = false;
        [SerializeField]
        protected bool onlyActiveGameObject = true;
        [SerializeField]
        protected bool hideChildren = true;

        public static List<LEGOAsset> legoAssets;

        static LEGOAsset()
        {
            legoAssets = new List<LEGOAsset>();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        protected virtual void OnDestroy()
        {
            if (!legoAssets.Contains(this)) { return; }
            legoAssets.Remove(this);
        }

        protected virtual void EditorUpdate()
        {
            if (!performedSetup)
            {
                Setup();
            }
            if (scaleChecker) {
                scaleChecker.EditorUpdate();
            }
        }

        static void OnEditorUpdate()
        {
            for (int i = legoAssets.Count - 1; i >= 0; i--)
            {
                if (!legoAssets[i])
                {
                    legoAssets.RemoveAt(i);
                    i--;
                    continue;
                }
                if (legoAssets[i].onlyActiveGameObject && Selection.activeGameObject != legoAssets[i].gameObject) { continue; }
                legoAssets[i].EditorUpdate();
            }
        }

        protected virtual void Awake()
        {
            if (legoAssets.Contains(this)) { return; }
            legoAssets.Add(this);
            
            Setup();
        }
#endif

        void OnValidate ()
        {
            HideInInspector();
            HideInHierarchy(true);
        }

        

        protected void Setup()
        {
#if UNITY_EDITOR
            performedSetup = true;
#endif
            HideInHierarchy(true);
        }

        public virtual void HideInInspector()
        {
            if (!scaleChecker)
            {
                scaleChecker = GetComponent<ScaleChecker>();
            }
            scaleChecker.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            if (!componentsEnforcer)
            {
                componentsEnforcer = GetComponent<LEGOComponentsEnforcer>();
            }
            componentsEnforcer.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            if (!meshRenderer)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            if (meshRenderer)
            {
                meshRenderer.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            }
            if (!meshFilter)
            {
                meshFilter = GetComponent<MeshFilter>();
            }
            if (meshFilter)
            {
                meshFilter.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            }
            hideFlags = HideFlags.NotEditable;
        }

        protected void HideInHierarchy(bool hide)
        {
#if UNITY_EDITOR
            HideFlags flagsToUse = hide ? HideFlags.HideInHierarchy : HideFlags.None;

            if (hideChildren)
            {
                DisablePickingChildren();
                CreatePickingMesh();

                foreach (Transform child in transform)
                {
                    child.hideFlags = flagsToUse;
                }
            }
#endif
        }

#if UNITY_EDITOR
        void DisablePickingChildren()
        {
            SceneVisibilityManager.instance.DisablePicking(gameObject, true);
            SceneVisibilityManager.instance.EnablePicking(gameObject, false);
        }

        void CreatePickingMesh()
        {
            var renderer = GetComponent<MeshRenderer>();
            var filter = GetComponent<MeshFilter>();

            if (!renderer)
            {
                var brick = GetComponent<Brick>();

                var combineInstances = new List<CombineInstance>();

                if (brick)
                {
                    // Get all shells from parts and combine them.
                    foreach (var part in brick.parts)
                    {
                        if (part.legacy)
                        {
                            var partRenderer = part.GetComponent<MeshRenderer>();
                            if (partRenderer)
                            {
                                var mesh = partRenderer.GetComponent<MeshFilter>().sharedMesh;
                                var combineInstance = new CombineInstance();
                                combineInstance.mesh = mesh;
                                combineInstance.transform = transform.worldToLocalMatrix * part.transform.localToWorldMatrix;

                                combineInstances.Add(combineInstance);
                            }
                        }
                        else
                        {
                            var shell = part.transform.Find("Shell");
                            if (shell)
                            {
                                var mesh = shell.GetComponent<MeshFilter>().sharedMesh;
                                var combineInstance = new CombineInstance();
                                combineInstance.mesh = mesh;
                                combineInstance.transform = transform.worldToLocalMatrix * shell.localToWorldMatrix;

                                combineInstances.Add(combineInstance);
                            }

                            var colourChangeSurfaces = part.transform.Find("ColourChangeSurfaces");
                            if (colourChangeSurfaces)
                            {
                                foreach (Transform colourChangeSurface in colourChangeSurfaces)
                                {
                                    var mesh = colourChangeSurface.GetComponent<MeshFilter>().sharedMesh;
                                    var combineInstance = new CombineInstance();
                                    combineInstance.mesh = mesh;
                                    combineInstance.transform = transform.worldToLocalMatrix * colourChangeSurface.localToWorldMatrix;

                                    combineInstances.Add(combineInstance);
                                }
                            }
                        }
                    }
                }

                Mesh combinedMesh = null;
                if (combineInstances.Count == 1)
                {
                    // If there is just one mesh, simply use a reference to that rather than combining.
                    // We know that the one mesh will not be transformed, so it's safe to ignore the transform on the CombineInstance.
                    combinedMesh = combineInstances[0].mesh;
                }
                else if (combineInstances.Count > 1)
                {
                    // Otherwise, if there's more than one, create and save a mesh asset (if it does not exist already).
                    // Then reference that mesh asset.
                    if (!PickingMeshUtils.CheckIfPickingMeshExists(name))
                    {
                        var newMesh = new Mesh();
                        newMesh.CombineMeshes(combineInstances.ToArray(), true, true, false);

                        PickingMeshUtils.SavePickingMesh(name, newMesh);
                        combinedMesh = PickingMeshUtils.LoadPickingMesh(name);
                    }
                    else
                    {
                        combinedMesh = PickingMeshUtils.LoadPickingMesh(name);
                    }
                }
                else
                {
                    // If there were no meshes, we assume it is a minifig and use a box mesh.
                    combinedMesh = PickingMeshUtils.LoadMinifigPickingMesh();
                }

                renderer = gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/LEGO_AssetPicking.mat");
                filter = gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = combinedMesh;
            }

            // Need to hide the renderer, filter and material every time as they do not stay hidden when reloading the scene.
            renderer.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            renderer.sharedMaterial.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            filter.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
        }
#endif
    }
}
