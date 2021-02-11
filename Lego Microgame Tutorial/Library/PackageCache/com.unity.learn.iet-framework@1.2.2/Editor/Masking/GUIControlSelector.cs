using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials
{
    [Serializable]
    public class GUIControlSelector
    {
        public enum Mode
        {
            GUIContent,
            NamedControl,
            Property,
            GUIStyleName,
            ObjectReference,
        }

        public Mode selectorMode { get { return m_SelectorMode; } set { m_SelectorMode = value; } }
        [SerializeField]
        private Mode m_SelectorMode;

        public GUIContent guiContent { get { return new GUIContent(m_GUIContent); } set { m_GUIContent = new GUIContent(value); } }
        [SerializeField]
        private GUIContent m_GUIContent = new GUIContent();

        public string controlName { get { return m_ControlName; } set { m_ControlName = value ?? ""; } }
        [SerializeField]
        private string m_ControlName = "";

        public string propertyPath { get { return m_PropertyPath; } set { m_PropertyPath = value ?? ""; } }
        [SerializeField]
        private string m_PropertyPath = "";

        public Type targetType { get { return m_TargetType.type; } set { m_TargetType.type = value; } }
        [SerializeField, SerializedTypeFilter(typeof(UnityObject))]
        private SerializedType m_TargetType = new SerializedType(null);

        public string guiStyleName { get { return m_GUIStyleName; } set { m_GUIStyleName = value; } }
        [SerializeField]
        private string m_GUIStyleName;

        /// <summary>
        /// A reference to a Unity Object of which name will be matched against the text in UI elements.
        /// </summary>
        /// <remarks>
        /// In order for this to work for assets, the asset must have a short name, i.e.,
        /// the name cannot be visible in the UI in shortened form, e.g. "A longer...".
        /// </remarks>
        public ObjectReference ObjectReference { get => m_ObjectReference; set => m_ObjectReference = value; }
        [SerializeField]
        ObjectReference m_ObjectReference;
    }
}
