using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.InteractiveTutorials
{
    class TutorialManager : ScriptableObject
    {
        [Serializable]
        struct SceneViewState
        {
            public bool In2DMode;
            public bool Orthographic;
            public float Size;
            public Vector3 Point;
            public Quaternion Direction;
        }

        [Serializable]
        struct SceneInfo
        {
            public string AssetPath;
            public bool WasLoaded;
        }


        [SerializeField]
        SceneViewState m_OriginalSceneView;

        //TODO [SerializeField] wanted here?
        string m_OriginalActiveSceneAssetPath;

        [SerializeField]
        SceneInfo[] m_OriginalScenes;
        // The original layout files are copied into this folder for modifications.
        const string k_UserLayoutDirectory = "Temp";
        // The original/previous layout is stored into this when loading new layouts.
        internal static readonly string k_OriginalLayoutPath = $"{k_UserLayoutDirectory}/OriginalLayout.dwlt";
        const string k_DefaultsFolder = "Tutorial Defaults";

        static TutorialManager s_TutorialManager;
        public static TutorialManager instance
        {
            get
            {
                if (s_TutorialManager == null)
                {
                    s_TutorialManager = Resources.FindObjectsOfTypeAll<TutorialManager>().FirstOrDefault();
                    if (s_TutorialManager == null)
                    {
                        s_TutorialManager = CreateInstance<TutorialManager>();
                        s_TutorialManager.hideFlags = HideFlags.HideAndDontSave;
                    }
                }

                return s_TutorialManager;
            }
        }

        Tutorial m_Tutorial;

        public static bool IsLoadingLayout { get; private set; }

        public static event Action aboutToLoadLayout;
        public static event Action<bool> layoutLoaded; // bool == successful

        internal static TutorialWindow GetTutorialWindow()
        {
            return Resources.FindObjectsOfTypeAll<TutorialWindow>().FirstOrDefault();
        }

        public void StartTutorial(Tutorial tutorial)
        {
            if (tutorial == null)
            {
                Debug.LogError("Null Tutorial.");
                return;
            }

            // Early-out if user decides to cancel. Otherwise the user can get reset to the
            // main tutorial selection screen in cases where the user was about to swtich to
            // another tutorial while finishing up another (typical use case would be having a
            // "start next tutorial" button at the last page of a tutorial).
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // NOTE maximizeOnPlay=true was causing problems at some point
            // (tutorial was closed for some reason) but that problem seems to be gone.
            // Keeping this here in case the problem returns.
            //GameViewProxy.maximizeOnPlay = false;

            // Prevent Game view flashing briefly when starting tutorial.
            EditorWindow.GetWindow<SceneView>().Focus();

            // Is the previous tutorial finished? Make sure to record the progress.
            // by trying to progress to the next page which will take care of it.
            if (m_Tutorial && m_Tutorial.completed)
                m_Tutorial.TryGoToNextPage();

            m_Tutorial = tutorial;

            // Ensure we are in edit mode
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += PostponeStartTutorialToEditMode;
            }
            else
                StartTutorialImmediateEditMode();
        }

        void PostponeStartTutorialToEditMode(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= PostponeStartTutorialToEditMode;
                StartTutorialImmediateEditMode();
            }
        }

        void StartTutorialImmediateEditMode()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Prevent Game view flashing briefly when starting tutorial.
            EditorWindow.GetWindow<SceneView>().Focus();

            SaveOriginalScenes();
            SaveOriginalWindowLayout();
            SaveSceneViewState();

            m_Tutorial.LoadWindowLayout();

            // Ensure TutorialWindow is open and set the current tutorial
            var tutorialWindow = EditorWindow.GetWindow<TutorialWindow>();
            tutorialWindow.SetTutorial(m_Tutorial, false);

            m_Tutorial.ResetProgress();

            // Do not overwrite workspace in authoring mode, use version control instead.
            if (!ProjectMode.IsAuthoringMode())
                LoadTutorialDefaultsIntoAssetsFolder();
        }

        // TODO unused code, is this still required for pointers for some refactoring?
        /*
        void StartTutorialInEditMode()
        {
            // TODO HACK double delay to resolve various issue (e.g. black screen during save modifications dialog
            // Revisit and fix properly.
            EditorApplication.delayCall += delegate
            {
                EditorApplication.delayCall += delegate
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        return;

                    // Prevent Game view flashing briefly when starting tutorial.
                    EditorWindow.GetWindow<SceneView>().Focus();

                    SaveOriginalScenes();
                    SaveOriginalWindowLayout();
                    SaveSceneViewState();

                    m_Tutorial.LoadWindowLayout();

                    // Ensure TutorialWindow is open and set the current tutorial
                    var tutorialWindow = EditorWindow.GetWindow<TutorialWindow>();
                    tutorialWindow.SetTutorial(m_Tutorial, false);

                    m_Tutorial.ResetProgress();

                    // Do not overwrite workspace in authoring mode, use version control instead.
                    if (!ProjectMode.IsAuthoringMode())
                        LoadTutorialDefaultsIntoAssetsFolder();
                };
            };
        }
        */

        public void RestoreOriginalState()
        {
            EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(RestoreOriginalScenes());
            RestoreOriginalWindowLayout();
            RestoreSceneViewState();
        }

        public void ResetTutorial()
        {
            var tutorialWindow = GetTutorialWindow();
            if (tutorialWindow == null || tutorialWindow.currentTutorial == null)
                return;

            m_Tutorial = tutorialWindow.currentTutorial;

            // Ensure we are in edit mode
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += PostponeResetTutorialToEditMode;
            }
            else
                StartTutorialImmediateEditMode();
        }

        void PostponeResetTutorialToEditMode(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= PostponeStartTutorialToEditMode;
                ResetTutorialInEditMode();
            }
        }

        void ResetTutorialInEditMode()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            m_Tutorial.LoadWindowLayout();
            m_Tutorial.ResetProgress();

            // Do not overwrite workspace in authoring mode, use version control instead.
            if (!ProjectMode.IsAuthoringMode())
                LoadTutorialDefaultsIntoAssetsFolder();
        }

        internal static void SaveOriginalWindowLayout()
        {
            WindowLayoutProxy.SaveWindowLayout(k_OriginalLayoutPath);
        }

        internal static void RestoreOriginalWindowLayout()
        {
            if (File.Exists(k_OriginalLayoutPath))
            {
                LoadWindowLayout(k_OriginalLayoutPath);
                File.Delete(k_OriginalLayoutPath);
            }
        }

        void SaveSceneViewState()
        {
            var sv = EditorWindow.GetWindow<SceneView>();
            m_OriginalSceneView.In2DMode = sv.in2DMode;
            m_OriginalSceneView.Point = sv.pivot;
            m_OriginalSceneView.Direction = sv.rotation;
            m_OriginalSceneView.Size = sv.size;
            m_OriginalSceneView.Orthographic = sv.orthographic;
        }

        void RestoreSceneViewState()
        {
            var sv = EditorWindow.GetWindow<SceneView>();
            sv.in2DMode = m_OriginalSceneView.In2DMode;
            sv.LookAt(
                m_OriginalSceneView.Point,
                m_OriginalSceneView.Direction,
                m_OriginalSceneView.Size,
                m_OriginalSceneView.Orthographic,
                instant: true
            );
        }

        public static bool LoadWindowLayout(string path)
        {
            IsLoadingLayout = true;
            aboutToLoadLayout?.Invoke();
            bool successful = EditorUtility.LoadWindowLayout(path); // will log an error if fails
            layoutLoaded?.Invoke(successful);
            IsLoadingLayout = false;
            return successful;
        }

        public static bool LoadWindowLayoutWorkingCopy(string path) =>
            LoadWindowLayout(GetWorkingCopyWindowLayoutPath(path));

        public static string GetWorkingCopyWindowLayoutPath(string layoutPath) =>
            $"{k_UserLayoutDirectory}/{new FileInfo(layoutPath).Name}";

        // Makes a copy of the window layout file and replaces LastProjectPaths in the window layout
        // so that pre-saved Project window states work correctly. Also resets TutorialWindow's readme in the layout.
        // Returns path to the new layout file.
        public static string PrepareWindowLayout(string layoutPath)
        {
            try
            {
                if (!Directory.Exists(k_UserLayoutDirectory))
                    Directory.CreateDirectory(k_UserLayoutDirectory);

                var destinationPath = GetWorkingCopyWindowLayoutPath(layoutPath);
                File.Copy(layoutPath, destinationPath, overwrite: true);

                const string lastProjectPathProp = "m_LastProjectPath: ";
                const string readmeProp = "m_Readme: ";
                const string nullObject = "{fileID: 0}";
                string userProjectPath = Directory.GetCurrentDirectory();

                var fileContents = new List<string>();
                using (var reader = new StreamReader(destinationPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = ReplaceAfter(lastProjectPathProp, userProjectPath, line);
                        line = ReplaceAfter(readmeProp, nullObject, line);
                        fileContents.Add(line);
                    }
                }

                using (var writer = new StreamWriter(destinationPath, append: false))
                {
                    fileContents.ForEach(writer.WriteLine);
                }
                return destinationPath;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return string.Empty;
            }
        }

        /// <summary>
        /// Saves current state of open/loaded scenes so we can restore later
        /// </summary>
        void SaveOriginalScenes()
        {
            m_OriginalActiveSceneAssetPath = SceneManager.GetActiveScene().path;
            m_OriginalScenes = new SceneInfo[SceneManager.sceneCount];
            for (var sceneIndex = 0; sceneIndex < m_OriginalScenes.Length; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                m_OriginalScenes[sceneIndex] = new SceneInfo
                {
                    AssetPath = scene.path,
                    WasLoaded = scene.isLoaded,
                };
            }
        }

        internal IEnumerator RestoreOriginalScenes()
        {
            // Don't restore scene state if we didn't save it in the first place
            if (string.IsNullOrEmpty(m_OriginalActiveSceneAssetPath)) { yield break; }

            // Exit play mode so we can open scenes (without necessarily loading them)
            EditorApplication.isPlaying = false;

            int currentFrameCount = Time.frameCount;
            while (currentFrameCount == Time.frameCount)
            {
                yield return null; //going out of play mode requires a frame
            }

            foreach (var sceneInfo in m_OriginalScenes)
            {
                // Don't open scene if path is empty (this is the case for a new unsaved unmodified scene)
                if (sceneInfo.AssetPath == string.Empty) { continue; }

                var openSceneMode = sceneInfo.WasLoaded ? OpenSceneMode.Additive : OpenSceneMode.AdditiveWithoutLoading;

                EditorSceneManager.OpenScene(sceneInfo.AssetPath, openSceneMode);
            }

            var originalScenePaths = m_OriginalScenes.Select(sceneInfo => sceneInfo.AssetPath).ToArray();
            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);

                // Set originally active scene
                if (scene.path == m_OriginalActiveSceneAssetPath)
                {
                    SceneManager.SetActiveScene(scene);
                    continue;
                }

                // Close scene if was not opened originally
                if (!originalScenePaths.Contains(scene.path))
                    EditorSceneManager.CloseScene(scene, true);
            }

            m_OriginalActiveSceneAssetPath = null;
        }

        static void LoadTutorialDefaultsIntoAssetsFolder()
        {
            if (!TutorialProjectSettings.instance.restoreDefaultAssetsOnTutorialReload)
                return;

            AssetDatabase.SaveAssets();
            string defaultsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, k_DefaultsFolder);
            var dirtyMetaFiles = new HashSet<string>();
            DirectoryCopy(defaultsPath, Application.dataPath, dirtyMetaFiles);
            AssetDatabase.Refresh();
            int startIndex = Application.dataPath.Length - "Assets".Length;
            foreach (var dirtyMetaFile in dirtyMetaFiles)
                AssetDatabase.ImportAsset(Path.ChangeExtension(dirtyMetaFile.Substring(startIndex), null));
        }

        internal static void WriteAssetsToTutorialDefaultsFolder()
        {
            if (!TutorialProjectSettings.instance.restoreDefaultAssetsOnTutorialReload)
                return;

            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Defaults cannot be written during play mode");
                return;
            }

            string defaultsPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, k_DefaultsFolder);
            DirectoryInfo defaultsDirectory = new DirectoryInfo(defaultsPath);
            if (defaultsDirectory.Exists)
            {
                foreach (var file in defaultsDirectory.GetFiles())
                    file.Delete();
                foreach (var directory in defaultsDirectory.GetDirectories())
                    directory.Delete(true);
            }
            DirectoryCopy(Application.dataPath, defaultsPath);
        }

        internal static void DirectoryCopy(string sourceDirectory, string destinationDirectory, HashSet<string> dirtyMetaFiles = default)
        {
            var sourceDir = new DirectoryInfo(sourceDirectory);
            if (!sourceDir.Exists)
                return;

            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            foreach (var file in sourceDir.GetFiles())
            {
                string tempPath = Path.Combine(destinationDirectory, file.Name);
                if (dirtyMetaFiles != null && string.Equals(Path.GetExtension(tempPath), ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(tempPath) || !File.ReadAllBytes(tempPath).SequenceEqual(File.ReadAllBytes(file.FullName)))
                        dirtyMetaFiles.Add(tempPath);
                }
                file.CopyTo(tempPath, true);
            }

            foreach (var subdir in sourceDir.GetDirectories())
            {
                string tempPath = Path.Combine(destinationDirectory, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, dirtyMetaFiles);
            }
        }

        static string ReplaceAfter(string before, string replaceWithThis, string lineToRead)
        {
            int index = -1;
            index = lineToRead.IndexOf(before, StringComparison.Ordinal);
            if (index > -1)
            {
                lineToRead = lineToRead.Substring(0, index + before.Length) + replaceWithThis;
            }
            return lineToRead;
        }
    }
}
