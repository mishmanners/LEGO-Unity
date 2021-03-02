using System;
using UnityEngine;

namespace Unity.LEGO.Game
{
    [CreateAssetMenu(fileName = "Variable", menuName = "Microgame/Variable", order = 1)]
    public class Variable : ScriptableObject
    {
        public string Name = "Variable";
        public int InitialValue;
        public bool UseUI = true;
        public GameObject UIPrefab;

        public Action<int> OnUpdate;
    }
}
