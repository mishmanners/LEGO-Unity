// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;

namespace LEGOModelImporter
{
    public class Connectivity : MonoBehaviour
    {
        public int version = 0;
        public Part part;

        public Bounds extents;
        public List<ConnectionField> connectionFields = new List<ConnectionField>();
    }
}