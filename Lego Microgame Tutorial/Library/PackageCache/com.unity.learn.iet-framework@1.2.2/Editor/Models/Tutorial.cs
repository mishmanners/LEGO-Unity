using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

using UnityObject = UnityEngine.Object;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// A container for tutorial pages which implement the tutorial's functionality.
    /// </summary>
    public class Tutorial : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Raised when any tutorial is modified.
        /// </summary>
        /// <remarks>
        /// Raised before tutorialPagesModified event.
        /// </remarks>
        public static event Action<Tutorial> TutorialModified; // TODO 2.0 merge the two Modified events?

        /// <summary>
        /// Raised when any page of any tutorial tutorial is modified.
        /// </summary>
        public static event Action<Tutorial> tutorialPagesModified;

        /// <summary>
        /// The title shown in the window.
        /// </summary>
        [Header("Content")]
        public LocalizableString TutorialTitle;
        [SerializeField, HideInInspector]
        string m_TutorialTitle = "";
        /// <summary> deprecated, use TutorialTitle </summary>
        [Obsolete] public string tutorialTitle { get => m_TutorialTitle; set => m_TutorialTitle = value; }

        /// <summary>
        /// Lessond ID, arbitrary string, typically integers are used.
        /// </summary>
        public string lessonId { get => m_LessonId; set => m_LessonId = value; }
        [SerializeField]
        string m_LessonId = "";

        /// <summary>
        /// Tutorial version, arbitrary string, typically integers are used.
        /// </summary>
        public string version { get => m_Version; set => m_Version = value; }
        [SerializeField]
        string m_Version = "0";

        [Header("Scene Data")]
        [SerializeField]
        SceneAsset m_Scene = default;

        [SerializeField]
        SceneViewCameraSettings m_DefaultSceneCameraSettings = default;

        /// <summary>
        /// The supported exit behavior types.
        /// </summary>
        public enum ExitBehavior
        {
            /// <summary>
            /// Show "home window", i.e. Unity Hub.
            /// </summary>
            ShowHomeWindow,
            /// <summary>
            /// Exit the tutorial, despite its name, this does not close the window.
            /// </summary>
            CloseWindow,
        }

        /// <summary>
        /// How should the tutorial behave upon exiting.
        /// </summary>
        public ExitBehavior exitBehavior { get => m_ExitBehavior; set => m_ExitBehavior = value; }
        [Header("Exit Behavior")]
        [SerializeField]
        ExitBehavior m_ExitBehavior = ExitBehavior.ShowHomeWindow;

        /// <summary>
        /// The supported skip behavior types.
        /// </summary>
        public enum SkipTutorialBehavior
        {
            /// <summary>
            /// Same as exit behaviour.
            /// </summary>
            SameAsExitBehavior,
            /// <summary>
            /// Skip to the last page of the tutorial.
            /// </summary>
            SkipToLastPage,
        }

        /// <summary>
        /// How should the tutorial behave upon skipping.
        /// </summary>
        public SkipTutorialBehavior skipTutorialBehavior { get => m_SkipTutorialBehavior; set => m_SkipTutorialBehavior = value; }
        [SerializeField]
        SkipTutorialBehavior m_SkipTutorialBehavior = SkipTutorialBehavior.SameAsExitBehavior;

        /// <summary>
        /// Obsolete.
        /// </summary>
        [Obsolete]
        public UnityObject assetSelectedOnExit { get => m_AssetSelectedOnExit; set => m_AssetSelectedOnExit = value; }
        [SerializeField]
        UnityObject m_AssetSelectedOnExit = default;

        /// <summary>
        /// Obsolete.
        /// </summary>
        [Obsolete]
        public TutorialWelcomePage welcomePage => m_WelcomePage;
        [Header("Pages"), SerializeField]
        TutorialWelcomePage m_WelcomePage = default;

        /// <summary>
        /// Obsolete.
        /// </summary>
        [Obsolete]
        public TutorialWelcomePage completedPage => m_CompletedPage;
        [SerializeField]
        TutorialWelcomePage m_CompletedPage = default;

        /// <summary>
        /// The layout used by the tutorial
        /// </summary>
        public UnityObject WindowLayout { get => m_WindowLayout; set => m_WindowLayout = value; }

        [SerializeField, Tooltip("Saved layouts can be found in the following directories:\n" +
            "Windows: %APPDATA%/Unity/<version>/Preferences/Layouts\n" +
            "macOS: ~/Library/Preferences/Unity/<version>/Layouts\n" +
            "Linux: ~/.config/Preferences/Unity/<version>/Layouts")]
        UnityObject m_WindowLayout;

        internal string windowLayoutPath => AssetDatabase.GetAssetPath(m_WindowLayout);

        /// <summary>
        /// All the pages of this tutorial.
        /// </summary>
        public IEnumerable<TutorialPage> pages => m_Pages;
        [SerializeField, FormerlySerializedAs("m_Steps")]
        internal TutorialPageCollection m_Pages = new TutorialPageCollection();

        AutoCompletion m_AutoCompletion;

        /// <summary>
        /// Is this being skipped currently.
        /// </summary>
        public bool skipped { get; private set; }

        /// <summary>
        /// Raised when this tutorial is being initiated.
        /// </summary>
        public event Action tutorialInitiated;
        /// <summary>
        /// Raised when a page of this tutorial is being initiated.
        /// </summary>
        public event Action<TutorialPage, int> pageInitiated;
        /// <summary>
        /// Raised when we are going back to the previous page.
        /// </summary>
        public event Action<TutorialPage> goingBack;
        /// <summary>
        /// Raised when this tutorial is completed.
        /// </summary>
        public event Action<bool> tutorialCompleted;

        public int currentPageIndex { get; private set; }

        public TutorialPage currentPage => m_Pages.count == 0
                    ? null
                    : m_Pages[currentPageIndex = Mathf.Min(currentPageIndex, m_Pages.count - 1)];

        public int pageCount => m_Pages.count;

        public bool completed => pageCount == 0 || (currentPageIndex >= pageCount - 2 && currentPage != null && currentPage.allCriteriaAreSatisfied);

        public bool isAutoCompleting => m_AutoCompletion.running;

        /// <summary>
        /// A wrapper class for serialization purposes.
        /// </summary>
        [Serializable]
        public class TutorialPageCollection : CollectionWrapper<TutorialPage>
        {
            /// <summary> Creates and empty collection. </summary>
            public TutorialPageCollection() : base() { }
            /// <summary> Creates a new collection from existing items. </summary>
            /// <param name="items"></param>
            public TutorialPageCollection(IList<TutorialPage> items) : base(items) { }
        }

        public Tutorial()
        {
            m_AutoCompletion = new AutoCompletion(this);
        }

        void OnEnable()
        {
            m_AutoCompletion.OnEnable();
        }

        void OnDisable()
        {
            m_AutoCompletion.OnDisable();
        }

        public void StartAutoCompletion()
        {
            m_AutoCompletion.Start();
        }

        public void StopAutoCompletion()
        {
            m_AutoCompletion.Stop();
        }

        public void StopTutorial()
        {
            if (currentPage != null)
                currentPage.RemoveCompletionRequirements();
        }

        public void GoToPreviousPage()
        {
            if (currentPageIndex == 0)
                return;

            OnGoingBack(currentPage);
            currentPageIndex = Mathf.Max(0, currentPageIndex - 1);
            OnPageInitiated(currentPage, currentPageIndex);
        }

        public bool TryGoToNextPage()
        {
            if (!currentPage || !currentPage.allCriteriaAreSatisfied && !currentPage.hasMovedToNextPage)
                return false;
            if (m_Pages.count == currentPageIndex + 1)
            {
                OnTutorialCompleted(true);
                return false;
            }
            int newIndex = Mathf.Min(m_Pages.count - 1, currentPageIndex + 1);
            if (newIndex != currentPageIndex)
            {
                if (currentPage != null)
                {
                    currentPage.OnPageCompleted();
                }
                currentPageIndex = newIndex;
                OnPageInitiated(currentPage, currentPageIndex);
                if (m_Pages.count == currentPageIndex + 1)
                {
                    OnTutorialCompleted(false);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// TODO 2.0 merge with RaiseTutorialModified?
        /// </summary>
        public void RaiseTutorialPagesModified()
        {
            tutorialPagesModified?.Invoke(this);
        }

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseTutorialModifiedEvent()
        {
            TutorialModified?.Invoke(this);
        }

        void LoadScene()
        {
            // load scene
            if (m_Scene != null)
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_Scene));
            else
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            // move scene view camera into place
            if (m_DefaultSceneCameraSettings != null && m_DefaultSceneCameraSettings.enabled)
                m_DefaultSceneCameraSettings.Apply();
            OnTutorialInitiated();
            if (pageCount > 0)
                OnPageInitiated(currentPage, currentPageIndex);
        }

        internal void LoadWindowLayout()
        {
            if (m_WindowLayout == null)
                return;

            var layoutPath = AssetDatabase.GetAssetPath(m_WindowLayout);
            TutorialManager.LoadWindowLayoutWorkingCopy(layoutPath);
        }

        internal void ResetProgress()
        {
            foreach (var page in m_Pages)
            {
                page?.ResetUserProgress();
            }
            currentPageIndex = 0;
            skipped = false;
            LoadScene();
        }

        protected virtual void OnTutorialInitiated()
        {
            tutorialInitiated?.Invoke();
        }

        protected virtual void OnTutorialCompleted(bool exitTutorial)
        {
            tutorialCompleted?.Invoke(exitTutorial);
        }

        protected virtual void OnPageInitiated(TutorialPage page, int index)
        {
            page?.Initiate();
            pageInitiated?.Invoke(page, index);
        }

        protected virtual void OnGoingBack(TutorialPage page)
        {
            page?.RemoveCompletionRequirements();
            goingBack?.Invoke(page);
        }

        public void SkipToLastPage()
        {
            skipped = true;
            currentPageIndex = pageCount - 1;
            OnPageInitiated(currentPage, currentPageIndex);
        }

        /// <summary>
        /// Adds a page to the tutorial
        /// </summary>
        /// <param name="tutorialPage">The page to be added</param>
        public void AddPage(TutorialPage tutorialPage)
        {
            m_Pages.AddItem(tutorialPage);
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Migrate content from < 1.2.
            TutorialParagraph.MigrateStringToLocalizableString(ref m_TutorialTitle, ref TutorialTitle);
        }
    }
}
