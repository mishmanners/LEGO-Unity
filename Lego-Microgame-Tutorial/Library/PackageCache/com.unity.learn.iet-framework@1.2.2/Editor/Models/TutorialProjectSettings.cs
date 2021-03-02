using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    class TutorialProjectSettings : ScriptableObject
    {
        static TutorialProjectSettings s_Instance;
        public static TutorialProjectSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(TutorialProjectSettings).FullName}");
                    if (assetGUIDs.Length == 0)
                        s_Instance = CreateInstance<TutorialProjectSettings>();
                    else
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[0]);

                        if (assetGUIDs.Length > 1)
                            Debug.LogWarningFormat("There is more than one TutorialProjectSetting asset in project.\n" +
                                "Using asset at path: {0}", assetPath);

                        s_Instance = AssetDatabase.LoadAssetAtPath<TutorialProjectSettings>(assetPath);
                    }
                }

                return s_Instance;
            }
        }

        public static void ReloadInstance()
        {
            s_Instance = null;
        }

        [Header("Initial Scene and Camera Settings")]
        [SerializeField]
        [Tooltip("If set, this page is shown in the welcome dialog when the project is started for the first time.")]
        TutorialWelcomePage m_WelcomePage = default;

        /// <summary>
        /// The page shown in the welcome dialog when the project is started for the first time.
        /// </summary>
        public TutorialWelcomePage WelcomePage { get { return m_WelcomePage; } set { m_WelcomePage = value; } }

        [SerializeField]
        [Tooltip("Initial scene that is loaded when the project is started for the first time.")]
        SceneAsset m_InitialScene = null;
        public SceneAsset initialScene => m_InitialScene;

        [SerializeField]
        SceneViewCameraSettings m_InitialCameraSettings = new SceneViewCameraSettings();
        public SceneViewCameraSettings InitialCameraSettings => m_InitialCameraSettings;

        [Header("Start-Up Settings")]
        [SerializeField]
        [Tooltip("If enabled, the original assets of the project are restored when a tutorial starts.")]
        bool m_RestoreDefaultAssetsOnTutorialReload = default;
        public bool restoreDefaultAssetsOnTutorialReload => m_RestoreDefaultAssetsOnTutorialReload;

        [SerializeField]
        [Tooltip("If enabled, disregard startup tutorial and start the first tutorial found in the project.")]
        bool m_UseLegacyStartupBehavior = default;

        [SerializeField]
        [Tooltip("If set, this is the tutorial that can be started from the welcome dialog.")]
        Tutorial m_StartupTutorial = default;

        /// <summary>
        /// The tutorial to run at startup, from the Welcome page
        /// </summary>
        public Tutorial startupTutorial
        {
            get
            {
                if (m_UseLegacyStartupBehavior)
                {
                    var guids = AssetDatabase.FindAssets($"t:{typeof(Tutorial).FullName}");
                    if (guids.Length > 0)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        return AssetDatabase.LoadAssetAtPath<Tutorial>(assetPath);
                    }

                    return null;
                }

                return m_StartupTutorial;
            }
            set { m_StartupTutorial = value; }
        }

        [SerializeField]
        [Tooltip("Style settings for this project.")]
        TutorialStyles m_TutorialStyle;
        public TutorialStyles TutorialStyle
        {
            get
            {
                if (!m_TutorialStyle)
                {
                    m_TutorialStyle = AssetDatabase.LoadAssetAtPath<TutorialStyles>(k_DefaultStyleAsset);
                }
                return m_TutorialStyle;
            }
        }

        internal static readonly string k_DefaultStyleAsset =
            "Packages/com.unity.learn.iet-framework/Editor/UI/Tutorial Styles.asset";
    }
}
