using UnityEditor;
using Unity.LEGO.UI;

namespace Unity.LEGO.EditorExt
{
    [CustomEditor(typeof(CustomButton))]
    public class CustomButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}

