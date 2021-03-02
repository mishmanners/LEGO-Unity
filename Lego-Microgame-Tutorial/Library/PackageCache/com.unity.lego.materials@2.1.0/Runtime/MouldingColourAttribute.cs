// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;

namespace LEGOMaterials
{
    public class MouldingColourAttribute : PropertyAttribute
    {
        public bool excludeTransparent;
        public bool excludeBrightYellow;
        public bool excludeLegacy;

        public MouldingColourAttribute(bool excludeTransparent = false, bool excludeBrightYellow = false, bool excludeLegacy = true)
        {
            this.excludeTransparent = excludeTransparent;
            this.excludeBrightYellow = excludeBrightYellow;
            this.excludeLegacy = excludeLegacy;
        }

    }

}