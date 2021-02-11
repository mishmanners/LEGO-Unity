// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;

namespace LEGOModelImporter
{

    [System.Serializable]
    public class CullingCameraConfig
    {
        [SerializeField]
        private bool foldout;

        public string name;
        public bool perspective;
        public Vector3 position;
        public Quaternion rotation;
        public float size;
        public float fov;
        public float maxRange;
        public float minRange;
        public float aspect;
    }

}