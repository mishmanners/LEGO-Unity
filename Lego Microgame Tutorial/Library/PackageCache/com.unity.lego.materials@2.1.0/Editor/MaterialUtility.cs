// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.IO;
using System;
using UnityEngine;
using UnityEditor;

namespace LEGOMaterials
{
    public static class MaterialUtility
    {
        public enum MaterialExistence
        {
            None,
            Legacy,
            Current
        }

        public static MaterialExistence CheckIfMaterialExists(MouldingColour.Id id)
        {
            if (File.Exists(MaterialPathUtility.GetPath(id)))
            {
                return MaterialExistence.Current;
            }

            if (File.Exists(MaterialPathUtility.GetPath(id, true)))
            {
                return MaterialExistence.Legacy;
            }
// FIXME Remove when colour palette experiments are over.
#if UNITY_EDITOR
            if (MouldingColour.GetBI())
            {
                if (File.Exists(MaterialPathUtility.GetPath(id, false, true)))
                {
                    return MaterialExistence.Current;
                }

                if (File.Exists(MaterialPathUtility.GetPath(id, true, true)))
                {
                    return MaterialExistence.Legacy;
                }
            }
#endif

            return MaterialExistence.None;
        }

        public static MaterialExistence CheckIfMaterialExists(string id)
        {
            try
            {
                return CheckIfMaterialExists((MouldingColour.Id)Enum.Parse(typeof(MouldingColour.Id), id));
            }
            catch
            {
                Debug.LogErrorFormat("Invalid moulding colour id {0}", id);
                return MaterialExistence.None;
            }
        }

        public static MaterialExistence CheckIfMaterialExists(int id)
        {
            return CheckIfMaterialExists(id.ToString());
        }

        public static Material LoadMaterial(MouldingColour.Id id, bool legacy)
        {
// FIXME Remove when colour palette experiments are over.
#if UNITY_EDITOR
            if (MouldingColour.GetBI())
            {
                var biMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPathUtility.GetPath(id, legacy, true));
                if (biMaterial)
                {
                    return biMaterial;
                }
            }
#endif

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialPathUtility.GetPath(id, legacy));
#else
            return null;
#endif
        }

        public static Material LoadMaterial(string id, bool legacy)
        {
            try
            {
                return LoadMaterial((MouldingColour.Id)Enum.Parse(typeof(MouldingColour.Id), id), legacy);
            }
            catch
            {
                Debug.LogErrorFormat("Invalid moulding colour id {0}", id);
                return null;
            }
        }

        public static Material LoadMaterial(int id, bool legacy)
        {
            return LoadMaterial(id.ToString(), legacy);
        }
    }

}