using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    [CustomEditor(typeof(TutorialContainer))]
    public class TutorialContainerEditor : Editor
    {
        static readonly bool k_IsAuthoringMode = ProjectMode.IsAuthoringMode();

        TutorialContainer Target => (TutorialContainer)target;

        void OnEnable()
        {
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            Target.RaiseModifiedEvent();
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            Target.RaiseModifiedEvent();
            return modifications;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(TutorialWindowMenuItem.Item))
                UserStartupCode.ShowTutorialWindow();

            if (k_IsAuthoringMode)
                base.OnInspectorGUI();
        }
    }
}
