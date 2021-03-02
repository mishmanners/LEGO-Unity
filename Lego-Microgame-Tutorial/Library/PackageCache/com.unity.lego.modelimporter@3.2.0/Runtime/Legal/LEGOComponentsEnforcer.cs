// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;

namespace LEGOModelImporter
{
    /// <summary>
    /// Prevents LEGO components removal from LEGO assets
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LEGOAsset))] //using a circular reference to revent removal
    public class LEGOComponentsEnforcer : MonoBehaviour
    {
    }
}
