// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

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
    [RequireComponent(typeof(Brick))]
    public class LEGOBrickAsset : LEGOAsset
    {        
    }
}
