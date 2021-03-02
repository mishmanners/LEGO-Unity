// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace LEGOModelImporter
{

    public static class ModelProcessor
    {
        private static Mesh GetOptimizedMesh(Mesh source, int optimizationLevel, int lod)
        {
            string lodDir = lod == 0 ? PartUtility.lod0Dir : PartUtility.lod1Dir; // Currently, there is no LOD2 for parts.

            string optimizedName = "";
            string assetFolder = Path.Combine(PartUtility.geometryPath, PartUtility.legacyDir);
            string assetExt = ".fbx";
            if (optimizationLevel > 0)
            {
                optimizedName = "_Optimized" + optimizationLevel;
                assetFolder = Path.Combine(assetFolder, "Optimized" + optimizationLevel);
                assetExt = ".asset";
            }

            string curMeshName = source.name;
            string newMeshName = curMeshName.Split('_')[0] + optimizedName;
            if (newMeshName != curMeshName)
            {
                Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(assetFolder, lodDir, newMeshName + assetExt));
                if (m)
                {
                    return m;
                }
                Debug.Log("Optimized Mesh not found for " + source.name);
            }
            return source;
        }

        enum MeshType
        {
            Legacy,
            Shell,
            ColourChange,
            Knob,
            Tube
        };

        class PartMeshRenderer
        {
            public Part part;
            public MeshRenderer rendererer;
            public MeshType type;
        }

        class MeshInstance
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Matrix4x4 worldMatrix;
            public Material material;
            public bool transparent;
            public Part part;
            public MeshType type;
            public Vector3 up;
            public Bounds bounds;
            public Color32 pixelColor;
            public int pixelCount;
            public int pixelKnobCount;
            public MaterialPropertyBlock propertyBlock;
        }

        static readonly int knobPixelThreshold = 20;
        static readonly int knobNormalMapThreshold = 20;

        public static void ProcessModelGroup(ModelGroup group, ref Vector2Int vertCount, ref Vector2Int triCount, ref Vector2Int meshCount, ref Vector2Int boxColliderCount)
        {
            bool collapseMesh = true;
            bool collapseCols = group.importSettings.colliders;

            // Keep track of how many of the removable meshes are left for each part.
            var partMeshCount = new Dictionary<Part, int>();
            // Keep track of how many of the removable surface meshes (shell, colourChangeSurface) are left for each non-legacy part.
            var partSurfaceMeshCount = new Dictionary<Part, int>();

            // TODO: Move orphaned colliders to group root

            var progress = 0;

            if (collapseMesh)
            {
                //    Debug.Log("Collapsing Mesh for "+group.name);

                // Optimization settings
                bool doSort = group.optimizations.HasFlag(ModelGroup.Optimizations.SortFrontToBack);
                bool canRemoveTubesAndPins = group.optimizations.HasFlag(ModelGroup.Optimizations.RemoveTubesAndPins);
                bool canRemoveKnobs = group.optimizations.HasFlag(ModelGroup.Optimizations.RemoveKnobs);
                bool canRemoveCompletelyInvisible = group.optimizations.HasFlag(ModelGroup.Optimizations.RemoveInvisible);
                bool cullBackfaces = group.optimizations.HasFlag(ModelGroup.Optimizations.BackfaceCulling);

                bool randomizeNormals = group.randomizeNormals;

                // Split into different meshes depending on whether they have knobs or are transparent.
                // 0 = No knobs = Fast track to avoid normal mapping when it isn't required
                // 1 = Knobs = Slow track needs normal mapping
                // 2 = Transparent No Knobs = Fast track to avoid normal mapping when it isn't required.
                // 3 = Transparent Knobs = Slow track since we will not render transparent bricks during visibility tests.

                MeshHelper[] meshHelpers = new MeshHelper[] { new MeshHelper(), new MeshHelper(), new MeshHelper(), new MeshHelper() };

                Matrix4x4 groupMatrix = group.transform.localToWorldMatrix;
                Matrix4x4 groupMatrixInv = groupMatrix.inverse;

                // Collect all parts.
                var parts = group.GetComponentsInChildren<Part>();

                // Collect relevant part mesh renderers along with mesh type information and a randomized rotation for each part.
                List<PartMeshRenderer> mrs = new List<PartMeshRenderer>();

                foreach (var part in parts)
                {
                    if (progress++ % 200 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Processing", "Collecting part renderers.", ((float)progress / parts.Length) * 0.05f);
                    }

                    partMeshCount[part] = 0;

                    if (part.legacy)
                    {
                        // Legacy parts only have unstructured meshes.
                        foreach (var renderer in part.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            var partMeshRenderer = new PartMeshRenderer()
                            {
                                part = part,
                                rendererer = renderer,
                                type = MeshType.Legacy
                            };
                            mrs.Add(partMeshRenderer);
                            partMeshCount[part]++;
                        }
                    }
                    else
                    {
                        partSurfaceMeshCount[part] = 0;

                        foreach (var renderer in part.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            var parentName = renderer.transform.parent.name;

                            // First check if it is a decoration surface since we will not include them in the processing.
                            if (parentName == "DecorationSurfaces")
                            {
                                continue;
                            }

                            var partMeshRenderer = new PartMeshRenderer()
                            {
                                part = part,
                                rendererer = renderer.GetComponent<MeshRenderer>()
                            };
                            if (parentName == "Knobs") // Is it a knob?
                            {
                                partMeshRenderer.type = MeshType.Knob;
                            }
                            else if (parentName == "Tubes") // Is it a tube?
                            {
                                partMeshRenderer.type = MeshType.Tube;
                            }
                            else if (parentName == "ColourChangeSurfaces") // Is it a colour change surface?
                            {
                                partMeshRenderer.type = MeshType.ColourChange;
                                partSurfaceMeshCount[part]++;
                            }
                            else // It must be the shell.
                            {
                                partMeshRenderer.type = MeshType.Shell;
                                partSurfaceMeshCount[part]++;
                            }

                            mrs.Add(partMeshRenderer);
                            partMeshCount[part]++;
                        }
                    }
                }

                Vector3 camPos = Vector3.zero;
                Vector3 camDir = Vector3.zero;

                if (group.views.Count == 0 && Camera.main)
                {
                    camPos = Camera.main.transform.position;
                    camDir = Camera.main.transform.forward;
                }
                else if (group.views.Count > 0)
                {
                    camPos = group.views[0].position;
                    camDir = group.views[0].rotation * Vector3.forward;
                }
                else
                {
                    doSort = false;
                    Debug.LogError("No views specified for front-to-back geometry sorting. Disabling!");
                }

                if (doSort)
                {
                    Vector3 sortDir;
                    if (group.views.Count == 0)
                    {
                        sortDir = camDir;
                    }
                    else
                    {
                        sortDir = group.views[0].rotation * Vector3.forward;
                    }

                    mrs.Sort(delegate (PartMeshRenderer a, PartMeshRenderer b)
                    {
                        float dA = Vector3.Dot(a.rendererer.bounds.center, sortDir);
                        float dB = Vector3.Dot(b.rendererer.bounds.center, sortDir);
                        return dA.CompareTo(dB);
                    });
                }


                // Multiple passes
                // - Collect all meshes 
                // - Remove original meshfilters and renderers
                // - Determine which Optimization level can be used (for legacy meshes only) or if mesh can be discarded completely
                // - Remove backfaces
                // - Build combined mesh

                // Collect meshes
                List<MeshInstance> instances = new List<MeshInstance>();

                for (int i = 0; i < mrs.Count; ++i)
                {
                    if (i % 200 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Processing", "Collecting part meshes.", 0.05f + (float)i / mrs.Count * 0.05f);
                    }

                    MeshFilter mf = mrs[i].rendererer.GetComponent<MeshFilter>();
                    if (mf)
                    {
                        if (mrs[i].rendererer.enabled && mrs[i].rendererer.gameObject.activeInHierarchy)
                        {
                            Mesh source = mf.sharedMesh;
                            if (source)
                            {
                                meshCount.x++;
                                triCount.x += (int)source.GetIndexCount(0) / 3;
                                vertCount.x += source.vertexCount;

                                Material material = mrs[i].rendererer.sharedMaterial;

                                MeshInstance c = new MeshInstance();
                                c.mesh = source;
                                c.matrix = groupMatrixInv * mf.transform.localToWorldMatrix;
                                c.worldMatrix = mf.transform.localToWorldMatrix;
                                c.material = material;
                                c.transparent = material && material.color.a < 1.0;
                                c.part = mrs[i].part;
                                c.type = mrs[i].type;
                                c.up = mf.transform.up;
                                c.bounds = mrs[i].rendererer.bounds;
                                instances.Add(c);
                            }
                        }
                        Object.DestroyImmediate(mf);
                    }
                    Object.DestroyImmediate(mrs[i].rendererer);
                }

                List<Vector3> viewPositions = new List<Vector3>();
                List<Vector3> viewDirections = new List<Vector3>();
                List<Matrix4x4> viewMatrices = new List<Matrix4x4>();
                List<Matrix4x4> projectionMatrices = new List<Matrix4x4>();
                List<Plane[]> viewFrustums = new List<Plane[]>();

                if (group.views.Count == 0 && Camera.main)
                {
                    viewMatrices.Add(Camera.main.worldToCameraMatrix);

                    if (Camera.main.orthographic)
                    {
                        viewDirections.Add(camDir);
                    }
                    else
                    {
                        viewPositions.Add(camPos);
                    }

                    viewFrustums.Add(GeometryUtility.CalculateFrustumPlanes(Camera.main));

                    projectionMatrices.Add(Camera.main.projectionMatrix);
                }
                else if (group.views.Count > 0)
                {
                    for (int i = 0; i < group.views.Count; ++i)
                    {
                        var view = group.views[i];
                        var viewMatrix = Matrix4x4.TRS(view.position, view.rotation, Vector3.one).inverse;

                        // Invert Z for metal/openGL
                        if (SystemInfo.usesReversedZBuffer)
                        {
                            viewMatrix.SetRow(2, -viewMatrix.GetRow(2));
                        }

                        viewMatrices.Add(viewMatrix);

                        Matrix4x4 projectionMatrix;
                        Plane[] frustumPlanes;
                        if (view.perspective)
                        {
                            projectionMatrix = Matrix4x4.Perspective(
                                view.fov,
                                view.aspect,
                                view.minRange,
                                view.maxRange
                                );

                            viewPositions.Add(view.position);

                            frustumPlanes = MathUtils.GetFrustumPlanesPerspective(view.position, view.rotation, view.fov, view.aspect, view.minRange, view.maxRange);
                        }
                        else
                        {
                            projectionMatrix = Matrix4x4.Ortho(
                                -view.size * view.aspect,
                                view.size * view.aspect,
                                -view.size,
                                view.size,
                                view.minRange,
                                view.maxRange
                                );

                            viewDirections.Add(view.rotation * Vector3.forward);

                            frustumPlanes = MathUtils.GetFrustumPlanesOrtho(view.position, view.rotation, view.size, view.aspect, view.minRange, view.maxRange);
                        }

                        projectionMatrices.Add(projectionMatrix);

                        viewFrustums.Add(frustumPlanes);
                    }
                }
                else
                {
                    if (canRemoveTubesAndPins || canRemoveKnobs || canRemoveCompletelyInvisible || cullBackfaces)
                    {
                        Debug.LogError("No views specified for backface culling and geometry removal. Disabling!");
                        canRemoveTubesAndPins = false;
                        canRemoveKnobs = false;
                        canRemoveCompletelyInvisible = false;
                        cullBackfaces = false;
                    }
                }

                if (instances.Count > 16777215)
                {
                    Debug.LogError($"Group {group.groupName} contains too many meshes. Some meshes will not be optimized correctly. Please split the group into multiple groups.");
                }

                AnalyzeMeshes(viewMatrices, projectionMatrices, viewPositions, viewDirections, viewFrustums, instances, partMeshCount, partSurfaceMeshCount, canRemoveTubesAndPins, canRemoveKnobs, canRemoveCompletelyInvisible, group.importSettings.lod);

                // Combine instances to a single mesh
                for (int i = 0; i < instances.Count; ++i)
                {
                    if (i % 200 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Processing", "Combining part meshes.", 0.2f + (float)i / instances.Count * 0.05f);
                    }

                    Mesh source = instances[i].mesh;
                    if (source == null)
                        continue;

                    Matrix4x4 matrix = instances[i].matrix;
                    Material material = instances[i].material;
                    int subMesh = instances[i].transparent ? (instances[i].type == MeshType.Knob ? 3 : 2) : instances[i].pixelKnobCount >= knobNormalMapThreshold ? 1 : 0;

                    MeshHelper mh = new MeshHelper(source);
                    mh.Transform(matrix);

                    // Cull backfaces
                    if (cullBackfaces)
                    {
                        for (int t = 0; t < mh.triangles.Count; t += 3)
                        {
                            int v0 = mh.triangles[t];
                            int v1 = mh.triangles[t + 1];
                            int v2 = mh.triangles[t + 2];
                            Vector3 triCen = groupMatrix.MultiplyPoint((mh.vertices[v0] + mh.vertices[v1] + mh.vertices[v2]) / 3);
                            Vector3 triNor = groupMatrix.MultiplyVector(mh.normals[v0] + mh.normals[v1] + mh.normals[v2]);//.normalized;

                            bool anyInView = false;
                            for (int v = 0; v < viewPositions.Count; ++v)
                            {
                                anyInView |= (Vector3.Dot(viewPositions[v] - triCen, triNor) >= 0);
                            }
                            for (int v = 0; v < viewDirections.Count; ++v)
                            {
                                anyInView |= (Vector3.Dot(viewDirections[v], triNor) <= 0);
                            }
                            if (!anyInView)
                            {
                                mh.triangles[t] = -1;
                                mh.triangles[t + 1] = -1;
                                mh.triangles[t + 2] = -1;
                            }
                        }
                        mh.triangles.RemoveAll((obj) => obj < 0);

                        mh.RemoveUnusedVertices();
                    }

                    // culled completely?
                    if (mh.vertices.Count == 0)
                    {
                        partMeshCount[instances[i].part]--;
                        continue;
                    }

                    meshCount.y++;
                    triCount.y += mh.triangles.Count / 3;
                    vertCount.y += mh.vertices.Count;

                    // Store color in vertices 
                    mh.SetColor(material);

                    // Randomize normals
                    if (randomizeNormals)
                    {
                        mh.AddNormalNoise();
                    }

                    meshHelpers[subMesh].Combine(mh);

                    mh = null;
                }

                EditorUtility.DisplayProgressBar("Processing", "Building new meshes.", 0.3f);

                for (int i = 0; i < meshHelpers.Length; ++i)
                {
                    if (meshHelpers[i].vertices.Count > 0)
                    {
                        GameObject target = group.gameObject;
                        if (i > 0)
                        {
                            target = new GameObject(group.name + "_subMesh" + i);
                            Undo.RegisterCreatedObjectUndo(target, "Create sub mesh");
                            target.transform.SetParent(group.transform, false);
                        }

                        MeshFilter mf = Undo.AddComponent<MeshFilter>(target);
                        MeshRenderer mr = Undo.AddComponent<MeshRenderer>(target);

                        Mesh m = new Mesh();
                        meshHelpers[i].ToMesh(m, group.importSettings.lightmapped);

                        // Need tangents?
                        if (i > 0)
                        {
                            m.RecalculateTangents();
                        }

                        // Make static.
                        target.isStatic = true;
                        if (group.importSettings.lightmapped)
                        {
                            mr.receiveGI = ReceiveGI.Lightmaps;
                        }
                        else
                        {
                            mr.receiveGI = ReceiveGI.LightProbes;
                        }

                        mf.sharedMesh = m;
                        switch (i)
                        {
                            case 0:
                                {
                                    PartUtility.StoreOptimizedMesh(m, group.parentName + "_" + group.groupName + "_Optimized.asset");
                                    mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/LEGO_VertexColor.mat");
                                    break;
                                }
                            case 1:
                                {
                                    PartUtility.StoreOptimizedMesh(m, group.parentName + "_" + group.groupName + "_Optimized_NormalMap.asset");
                                    mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/LEGO_VertexColor_NormalMap.mat");
                                    break;
                                }
                            case 2:
                                {
                                    PartUtility.StoreOptimizedMesh(m, group.parentName + "_" + group.groupName + "_Optimized_Transparent.asset");
                                    mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/LEGO_VertexColor_Transparent.mat");
                                    break;
                                }
                            case 3:
                                {
                                    PartUtility.StoreOptimizedMesh(m, group.parentName + "_" + group.groupName + "_Optimized_Transparent_NormalMap.asset");
                                    mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/LEGO_VertexColor_Transparent_NormalMap.mat");
                                    break;
                                }
                        }
                    }
                }
            }

            // Remove decoration surfaces for non-legacy parts that have no surface meshes left.
            progress = 0;
            foreach (var entry in partSurfaceMeshCount)
            {
                if (progress++ % 200 == 0)
                {
                    EditorUtility.DisplayProgressBar("Processing", "Removing unneeded decorations.", 0.3f + (float)progress / partSurfaceMeshCount.Count * 0.25f);
                }

                if (!entry.Key.legacy && entry.Value == 0)
                {
                    var decorationSurfaces = entry.Key.transform.Find("DecorationSurfaces");
                    if (decorationSurfaces)
                    {
                        Undo.DestroyObjectImmediate(decorationSurfaces.gameObject);
                    }
                }
            }

            // Remove parts that have no meshes left.
            progress = 0;
            foreach (var entry in partMeshCount)
            {
                if (progress++ % 200 == 0)
                {
                    EditorUtility.DisplayProgressBar("Processing", "Removing empty parts.", 0.55f + (float)progress / partMeshCount.Count * 0.25f);
                }

                if (entry.Value == 0)
                {
                    entry.Key.brick.parts.Remove(entry.Key);

                    // If no parts are left, remove the brick.
                    if (entry.Key.brick.parts.Count == 0)
                    {
                        Undo.DestroyObjectImmediate(entry.Key.brick.gameObject);
                    } else
                    {
                        Undo.DestroyObjectImmediate(entry.Key.gameObject);
                    }
                }
            }

            // Collapse remaining box colliders.
            if (collapseCols)
            {
                // FIXME Move to proper constant.
                var colliderSizeBias = 0.02f;
                var epsilon = 0.001f;
                
                var allColliders = group.GetComponentsInChildren<BoxCollider>();
                // Filter out colliders that are part of connectivity features.
                var partColliders = allColliders.Where((c) => c.gameObject.layer != LayerMask.NameToLayer(Connection.connectivityReceptorLayerName) && c.gameObject.layer != LayerMask.NameToLayer(Connection.connectivityConnectorLayerName)).ToArray();
                boxColliderCount.x = partColliders.Length;
                boxColliderCount.y = partColliders.Length;
                bool[] colDeleted = new bool[partColliders.Length];
                var collapseHappened = true;
                var iterationCount = 0;
                while (collapseHappened)
                {
                    collapseHappened = false;
                    iterationCount++;
                    for (var i = 0; i < partColliders.Length; ++i)
                    {
                        if (i % 200 == 0)
                        {
                            EditorUtility.DisplayProgressBar("Processing", "Collapsing colliders - iteration " + iterationCount + ".", 0.8f + (float)i / partColliders.Length * 0.05f);
                        }

                        if (colDeleted[i])
                        {
                            continue;
                        }

                        for (var j = i + 1; j < partColliders.Length; ++j)
                        {
                            if (colDeleted[j])
                            {
                                continue;
                            }

                            var colliderA = partColliders[i];
                            var colliderB = partColliders[j];

                            // Check that spaces match up.
                            var colliderBRotationInALocalSpace = Quaternion.Inverse(colliderA.transform.rotation) * colliderB.transform.rotation;
                            var euler = colliderBRotationInALocalSpace.eulerAngles;
                            if (Mathf.Abs(Mathf.Round(euler.x / 90.0f) - euler.x / 90.0f) < epsilon && Mathf.Abs(Mathf.Round(euler.y / 90.0f) - euler.y / 90.0f) < epsilon && Mathf.Abs(Mathf.Round(euler.z / 90.0f) - euler.z / 90.0f) < epsilon)
                            {
                                // Check that centers match up.
                                var colliderBCenterInColliderALocalSpace = colliderA.transform.InverseTransformPoint(colliderB.transform.TransformPoint(colliderB.center));

                                var centerDiff = colliderBCenterInColliderALocalSpace - colliderA.center;
                                var axisToMatch = centerDiff.MajorAxis();
                                var centerProjectedToAxis = centerDiff.SnapToMajorAxis() * centerDiff.magnitude;
                                if ((centerDiff - centerProjectedToAxis).sqrMagnitude < 0.01f)
                                {
                                    //Debug.Log(GetGameObjectPath(colliderA.gameObject) + colliderA.transform.GetSiblingIndex() + "\n" + GetGameObjectPath(colliderB.gameObject) + colliderB.transform.GetSiblingIndex());
                                    //Debug.Log("Matched centers on " + axisToMatch);
                                    // Check that size match up.
                                    var colliderBSizeInColliderALocalSpace = colliderA.transform.InverseTransformVector(colliderB.transform.TransformVector(colliderB.size)).Abs();
                                    //Debug.Log("Collider b size in collider a local space: " + colliderBSizeInColliderALocalSpace);

                                    var otherAxis1 = (axisToMatch + 1) % 3;
                                    var otherAxis2 = (axisToMatch + 2) % 3;
                                    if (Mathf.Abs(centerDiff.magnitude - (colliderA.size[axisToMatch] + colliderBSizeInColliderALocalSpace[axisToMatch] + 2.0f * colliderSizeBias) * 0.5f) < epsilon)
                                    {
                                        //Debug.Log("Matched on axis length");
                                        if (Mathf.Abs(colliderA.size[otherAxis1] - colliderBSizeInColliderALocalSpace[otherAxis1]) < epsilon && Mathf.Abs(colliderA.size[otherAxis2] - colliderBSizeInColliderALocalSpace[otherAxis2]) < epsilon)
                                        {
                                            //Debug.Log("Matched other axes");
                                            // Merge collider B into collider A.
                                            colliderA.center += centerDiff.normalized * (colliderBSizeInColliderALocalSpace[axisToMatch] + colliderSizeBias) * 0.5f;
                                            var newSize = colliderA.size;
                                            newSize[axisToMatch] += colliderBSizeInColliderALocalSpace[axisToMatch] + colliderSizeBias;
                                            colliderA.size = newSize;

                                            // Update part and destroy collider game object. Empty parent Colliders game objects will be removed during part clean-up.
                                            var part = colliderB.GetComponentInParent<Part>();
                                            part.colliders.Remove(colliderB);
                                            Undo.DestroyObjectImmediate(colliderB.gameObject);

                                            colDeleted[j] = true;

                                            boxColliderCount.y--;

                                            // Note that a change was made, and run over colliders again when done.
                                            collapseHappened = true;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }

            if (collapseMesh || collapseCols)
            {
                // Collect all remaining parts and clean them up.
                progress = 0;
                var parts = group.GetComponentsInChildren<Part>();
                foreach (var part in parts)
                {
                    if (progress++ % 200 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Processing", "Cleaning up remaining parts.", 0.85f + (float)progress / parts.Length * 0.15f);
                    }
                    CleanupPartGeometryTransforms(part);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private static void AnalyzeMeshes(List<Matrix4x4> viewMatrices, List<Matrix4x4> projectionMatrices, List<Vector3> viewPositions, List<Vector3> viewDirections, List<Plane[]> viewFrustums, List<MeshInstance> instances, Dictionary<Part, int> partMeshCount, Dictionary<Part, int> partSurfaceMeshCount, bool allowTubeAndPinRemove, bool allowKnobRemove, bool allowRemove, int lod)
        {
            int colorID = Shader.PropertyToID("_Color");

            // Set pixelColor of instances, and reset counters
            for (int id = 0; id < instances.Count; ++id)
            {
                if (id <= 16777215)
                {
                    int r = (id & 0xFF);
                    int g = (id >> 8) & 0xFF;
                    int b = (id >> 16) & 0xFF;
                    instances[id].pixelColor = new Color32((byte)r, (byte)g, (byte)b, 255);
                    // Important don't use SetColor as it has colorspace applied, making pixel counting wrong
                    instances[id].propertyBlock = new MaterialPropertyBlock();
                    instances[id].propertyBlock.SetVector(colorID, (Color)instances[id].pixelColor);

                    instances[id].pixelCount = 0;
                    instances[id].pixelKnobCount = 0;
                }
                else
                {
                    instances[id].pixelCount = 1;
                    instances[id].pixelKnobCount = knobPixelThreshold;
                }
            }

            // Render scene to rendertarget and count pixels per instance
            RenderTextureDescriptor desc = new RenderTextureDescriptor(1024, 1024, RenderTextureFormat.ARGB32, 24);
            RenderTexture rt = RenderTexture.GetTemporary(desc);
            Texture2D t = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);

            Material material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/Optimizer.mat");

            RenderTexture orig = RenderTexture.active;

            int totalRenders = viewMatrices.Count * instances.Count;
            int progress = 0;

            {
                RenderTargetIdentifier colorBuffer = new RenderTargetIdentifier(rt.colorBuffer);
                RenderTargetIdentifier depthBuffer = new RenderTargetIdentifier(rt.depthBuffer);

                CommandBuffer cmd = new CommandBuffer();

                for (int v = 0; v < viewMatrices.Count; ++v)
                {
                    cmd.Clear();
                    cmd.SetRenderTarget(colorBuffer, depthBuffer);
                    cmd.SetViewProjectionMatrices(viewMatrices[v], projectionMatrices[v]);

                    cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

                    for (int id = 0; id < instances.Count; ++id)
                    {
                        if (progress++ % 200 == 0)
                        {
                            EditorUtility.DisplayProgressBar("Processing", "Analyzing part meshes.", 0.1f + (float)progress / totalRenders * 0.05f);
                        }

                        // Skip transparent meshes when counting visible pixels as they should not cover up other meshes.
                        // Also skip meshes once we run out of id bits.
                        if (!instances[id].transparent && id <= 16777215)
                        {
                            cmd.DrawMesh(instances[id].mesh, instances[id].worldMatrix, material, 0, 0, instances[id].propertyBlock);
                        }
                    }

                    Graphics.ExecuteCommandBuffer(cmd);

                    RenderTexture.active = rt;
                    t.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
                    t.Apply();

                    Color32[] pixels = t.GetPixels32();
                    for (int j = 0; j < pixels.Length; ++j)
                    {
                        if (pixels[j].a > 0)
                        {
                            int r = pixels[j].r;
                            int g = ((int)pixels[j].g) << 8;
                            int b = ((int)pixels[j].b) << 16;
                            int id = (r | g | b);
                            instances[id].pixelCount++;
                            if (pixels[j].a < 200)
                            {
                                instances[id].pixelKnobCount++;
                            }
                        }
                    }

                    // System.IO.File.WriteAllBytes("../TestProcess_"+group.name+"_view"+v+".png", t.EncodeToPNG());

                }

                cmd.Dispose();
            }

            RenderTexture.active = orig;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(t);

            progress = 0;

            // Update instances based on pixel counts!
            if (allowKnobRemove || allowTubeAndPinRemove || allowRemove)
            {
                foreach (var m in instances)
                {
                    if (progress++ % 200 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Processing", 0.15f + "Optimizing part meshes.", (float)progress / instances.Count * 0.05f);
                    }

                    // Transparent meshes have not been rendered so we cannot optimize them or remove them based on visible pixel count.
                    // Instead, we test them against the frustums to check if they can be removed.
                    if (m.transparent)
                    {
                        var canRemove = ((m.type == MeshType.Legacy || m.type == MeshType.Shell || m.type == MeshType.ColourChange) && allowRemove) || (m.type == MeshType.Tube && allowTubeAndPinRemove) || (m.type == MeshType.Knob && allowKnobRemove);
                        if (canRemove)
                        {
                            bool anyInside = false;
                            for (int v = 0; v < viewFrustums.Count; ++v)
                            {
                                anyInside |= GeometryUtility.TestPlanesAABB(viewFrustums[v], m.bounds);
                            }
                            if (!anyInside)
                            {
                                m.mesh = null;
                                if (m.type == MeshType.Shell || m.type == MeshType.ColourChange)
                                {
                                    partSurfaceMeshCount[m.part]--;
                                }
                                partMeshCount[m.part]--;
                            }
                        }
                        continue;
                    }

                    if (m.pixelCount > 0)
                    {
                        if (m.type == MeshType.Legacy)
                        {
                            // Optimization levels only apply to legacy parts.
                            int optimizationLevel = 0;
                            if (allowTubeAndPinRemove)
                            {
                                Vector3 center = m.bounds.center;

                                bool anyBelow = false;
                                for (int v = 0; v < viewPositions.Count; ++v)
                                {
                                    anyBelow |= Vector3.Dot(m.up, center - viewPositions[v]) >= 0;
                                }
                                for (int v = 0; v < viewDirections.Count; ++v)
                                {
                                    anyBelow |= Vector3.Dot(m.up, viewDirections[v]) <= 0;
                                }

                                if (!anyBelow)
                                {
                                    optimizationLevel += 1;
                                }
                            }
                            if (allowKnobRemove && m.pixelKnobCount < knobPixelThreshold)
                            {
                                optimizationLevel += 2;
                            }
                            if (optimizationLevel > 0)
                            {
                                m.mesh = GetOptimizedMesh(m.mesh, optimizationLevel, lod);
                            }
                        }
                        else if (m.type == MeshType.Knob)
                        {
                            if (allowKnobRemove && m.pixelCount < knobPixelThreshold)
                            {
                                m.mesh = null;
                                partMeshCount[m.part]--;
                            }
                        }
                    }
                    else
                    {
                        if ((m.type == MeshType.Legacy || m.type == MeshType.Shell || m.type == MeshType.ColourChange) && allowRemove)
                        {
                            m.mesh = null;
                            if (m.type == MeshType.Shell || m.type == MeshType.ColourChange)
                            {
                                partSurfaceMeshCount[m.part]--;
                            }
                            partMeshCount[m.part]--;
                        }
                        else if (m.type == MeshType.Knob && allowKnobRemove)
                        {
                            m.mesh = null;
                            partMeshCount[m.part]--;
                        }
                        else if (m.type == MeshType.Tube && allowTubeAndPinRemove)
                        {
                            m.mesh = null;
                            partMeshCount[m.part]--;
                        }
                    }
                }
            }
        }

        private static void CleanupPartGeometryTransforms(Part part)
        {
            if (!part.legacy)
            {
                // Remove empty Colliders and Connectivity transforms.
                var colliders = part.transform.Find("Colliders");
                if (colliders && colliders.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(colliders.gameObject);
                }
                var connectivity = part.transform.Find("Connectivity");
                if (connectivity && connectivity.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(connectivity.gameObject);
                }

                // Remove all geometry transforms except decoration surfaces.
                var shell = part.transform.Find("Shell");
                if (shell)
                {
                    Undo.DestroyObjectImmediate(shell.gameObject);
                }
                var colourChangeSurfaces = part.transform.Find("ColourChangeSurfaces");
                if (colourChangeSurfaces)
                {
                    Undo.DestroyObjectImmediate(colourChangeSurfaces.gameObject);
                }
                var knobs = part.transform.Find("Knobs");
                if (knobs)
                {
                    Undo.DestroyObjectImmediate(knobs.gameObject);
                }
                var tubes = part.transform.Find("Tubes");
                if (tubes)
                {
                    Undo.DestroyObjectImmediate(tubes.gameObject);
                }
            }
        }
    }
}