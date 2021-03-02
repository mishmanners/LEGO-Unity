// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;

namespace LEGOModelImporter
{
    [Serializable]
    public class ModelGroupImportSettings
    {
        public bool colliders = true;
        public bool connectivity = true;
        public bool isStatic;
        public bool lightmapped;
        public bool randomizeRotation = true;
        public bool preferLegacy;
        public int lod;

        public ModelGroupImportSettings()
        {

        }

        public ModelGroupImportSettings(ModelGroupImportSettings importSettings)
        {
            colliders = importSettings.colliders;
            connectivity = importSettings.connectivity;
            isStatic = importSettings.isStatic;
            lightmapped = importSettings.lightmapped;
            randomizeRotation = importSettings.randomizeRotation;
            preferLegacy = importSettings.preferLegacy;
            lod = importSettings.lod;
        }

    }
}