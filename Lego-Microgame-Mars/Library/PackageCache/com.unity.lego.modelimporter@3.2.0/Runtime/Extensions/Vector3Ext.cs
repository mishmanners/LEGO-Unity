// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using System.Collections;

namespace LEGOModelImporter
{

    public static class Vector3Ext
    {
        public static Vector3 Clamp(this Vector3 v, float min, float max)
        {
            return new Vector3(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max), Mathf.Clamp(v.z, min, max));
        }

        public static Vector3 Clamp01(this Vector3 v)
        {
            return new Vector3(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y), Mathf.Clamp01(v.z));
        }

        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static Vector3 Sign(this Vector3 v)
        {
            return new Vector3(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z));
        }

        public static int MajorAxis(this Vector3 v)
        {
            float x = Mathf.Abs(v.x);
            float y = Mathf.Abs(v.y);
            float z = Mathf.Abs(v.z);
            if (x > y && x > z)
                return 0;
            if (y > z)
                return 1;
            return 2;
        }

        public static Vector3 SnapToMajorAxis(this Vector3 v)
        {
            Vector3 v2 = new Vector3();
            int axis = v.MajorAxis();
            v2[axis] = Mathf.Sign(v[axis]);
            return v2;
        }
    }

}