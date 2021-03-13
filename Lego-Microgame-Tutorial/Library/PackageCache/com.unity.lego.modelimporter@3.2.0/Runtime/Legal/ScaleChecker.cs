// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;

namespace LEGOModelImporter
{
    /// <summary>
    /// Ensures that an object is not scaled.
    /// </summary>
    [HideInInspector]
    [ExecuteAlways]
    public class ScaleChecker : MonoBehaviour
    {
        public void EditorUpdate()
        {
            EnsureScale();
        }

#if !UNITY_EDITOR
        void Update()
        {
            EnsureScale();
        }

#endif
        void EnsureScale()
        {
            if (transform.localScale == Vector3.one)
            {
                return;
            }

            Debug.LogError("Scaling of LEGO assets is not allowed");

            transform.localScale = Vector3.one;
        }
    }
}
