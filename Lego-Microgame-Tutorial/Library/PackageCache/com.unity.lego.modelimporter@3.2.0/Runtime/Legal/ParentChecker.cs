// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;

namespace LEGOModelImporter
{
    /// <summary>
    /// Ensures that an object is not under a scaled parent.
    /// </summary>
    [HideInInspector]
    [ExecuteAlways]
    public class ParentChecker : MonoBehaviour
    {
        public void EditorUpdate()
        {
            EnsureParentScale();
        }

#if !UNITY_EDITOR
        void Update()
        {
            EnsureParentScale();
        }

#endif

        void EnsureParentScale()
        {
            if (transform.parent == null)
            {
                return;
            }

            var parent = transform.parent;
            var scaledParent = false;
            while(parent != null)
            {
                var scaled = Vector3.Distance(parent.localScale, Vector3.one) >= Mathf.Epsilon;
                scaledParent |= scaled;

                if (scaled)
                {
                    parent.localScale = Vector3.one;
                }

                parent = parent.parent;
            }

            if (scaledParent)
            {
                Debug.LogError("LEGO Model assets cannot be children of scaled game objects");
            }
        }
    }
}
