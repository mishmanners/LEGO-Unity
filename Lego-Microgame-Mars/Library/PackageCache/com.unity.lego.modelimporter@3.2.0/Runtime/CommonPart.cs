// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;

namespace LEGOModelImporter
{

    public abstract class CommonPart : MonoBehaviour
    {
        public Part part;

        public void UpdateVisibility()
        {
            gameObject.SetActive(IsVisible());
        }

        public abstract bool IsVisible();
    }
}