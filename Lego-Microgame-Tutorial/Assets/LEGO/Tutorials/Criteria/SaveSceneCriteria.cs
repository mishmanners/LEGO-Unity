using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Unity.LEGO.Tutorials
{
    /// <summary>
    /// Contains all the callbacks needed for checking if a user has saved a scene.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveSceneCriteria", menuName = "Tutorials/Microgame/SaveSceneCriteria")]
    class SaveSceneCriteria : ScriptableObject
    {
        bool currentSceneHasBeenSaved;
        Scene activeScene;
        public void ResetSceneSavedStatus()
        {
            currentSceneHasBeenSaved = false;
            activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        void OnSceneSaved(Scene scene)
        {
            currentSceneHasBeenSaved = true;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        public bool SceneHasBeenSaved()
        {
            return activeScene.isDirty ? currentSceneHasBeenSaved : true;
        }

        public bool AutoCompleteSceneSaved()
        {
            currentSceneHasBeenSaved = true;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            return currentSceneHasBeenSaved;
        }
    }
}
