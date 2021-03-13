using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using static Unity.InteractiveTutorials.RichTextParser;

namespace Unity.InteractiveTutorials
{
    // A modal/utility window. Utilizes masking for the modality.
    class TutorialModalWindow : EditorWindow
    {
        const int k_Width = 700;
        const int k_Height = 500;

        static TutorialStyles Styles => TutorialProjectSettings.instance.TutorialStyle;

        public TutorialWelcomePage WelcomePage
        {
            get => m_WelcomePage;
            set
            {
                if (m_WelcomePage)
                    m_WelcomePage.Modified -= OnWelcomePageModified;

                m_WelcomePage = value;
                if (m_WelcomePage)
                    m_WelcomePage.Modified += OnWelcomePageModified;
            }
        }
        [SerializeField]
        TutorialWelcomePage m_WelcomePage;

        Action m_OnClose;

        public static bool Visible { get; private set; }

        // Remember to set prior to calling TryToShow().
        public static bool MaskingEnabled { get; set; } = false;

        bool IsInitialized => rootVisualElement.childCount > 0;

        static bool s_IsBeingModified;
        //string m_PreviousWindowTitle;

        public static void TryToShow(TutorialWelcomePage welcomePage, Action onClose)
        {
            var window = EditorWindowUtils.FindOpenInstance<TutorialModalWindow>();
            if (window)
                window.Close();
            window = CreateInstance<TutorialModalWindow>();
            window.titleContent.text = welcomePage.WindowTitle;
            //window.m_PreviousWindowTitle = welcomePage.WindowTitle;
            window.minSize = window.maxSize = new Vector2(k_Width, k_Height);
            window.m_OnClose = onClose;
            window.WelcomePage = welcomePage;

            window.ShowUtility();
            // NOTE: positioning must be done after Show() in order to work.
            if (!s_IsBeingModified)
                EditorWindowUtils.CenterOnMainWindow(window);

            if (MaskingEnabled)
                window.Mask();
        }

        void Initialize()
        {
            var windowAsset = TutorialWindow.LoadUIAsset<VisualTreeAsset>("WelcomeDialog.uxml");
            var mainContainer = windowAsset.CloneTree().Q("MainContainer");

            // TODO OnGuiToolbar is functional, uncomment if/when we reintroduce masking for welcome dialog.
            //var imguiToolBar = new IMGUIContainer(OnGuiToolbar);
            //rootVisualElement.Add(imguiToolBar);
            rootVisualElement.Add(mainContainer);
        }

        void OnEnable()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
            Styles.ApplyThemeStyleSheetTo(rootVisualElement);
        }

        void OnBecameVisible()
        {
            Visible = true;
            UpdateContent();
            //Mask();
        }

        // For the teardown callbacks the order of execution is OnBecameInvisible, OnDisable, OnDestroy.
        // NOTE OnBecameInvisible appears to be called never if window was shown as utility window.
        // void OnBecameInvisible() {}

        void OnDestroy()
        {
            Visible = false;
            s_IsBeingModified = false;
            m_OnClose?.Invoke();
            Unmask();
        }

        void UpdateContent()
        {
            if (!WelcomePage)
            {
                Debug.LogError("null WelcomePage.");
                return;
            }

            var header = rootVisualElement.Q("HeaderMedia");
            header.style.backgroundImage = Background.FromTexture2D(WelcomePage.Image);
            rootVisualElement.Q("HeaderContainer").style.display = WelcomePage.Image != null ? DisplayStyle.Flex : DisplayStyle.None;
            titleContent.text = WelcomePage.WindowTitle;
            rootVisualElement.Q<Label>("Heading").text = WelcomePage.Title;
            RichTextToVisualElements(WelcomePage.Description, rootVisualElement.Q("Description"));

            var buttonContainer = rootVisualElement.Q("ButtonContainer");
            buttonContainer.Clear();
            WelcomePage.Buttons
                .Where(buttonData => buttonData.Text.Value.IsNotNullOrEmpty())
                .Select(buttonData =>
                    new Button(() => buttonData.OnClick?.Invoke())
                    {
                        text = buttonData.Text,
                        tooltip = buttonData.Tooltip
                    })
                .ToList()
                .ForEach(button => buttonContainer.Add(button));
        }

        void OnGuiToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            MaskingEnabled = GUILayout.Toggle(
                MaskingEnabled, "Masking", EditorStyles.toolbarButton,
                GUILayout.MaxWidth(TutorialWindow.s_AuthoringModeToolbarButtonWidth)
            );
            if (EditorGUI.EndChangeCheck())
            {
                if (MaskingEnabled)
                    Mask();
                else
                    Unmask();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        void Mask()
        {
            var styles = Styles;
            var maskingColor = styles?.MaskingColor ?? Color.magenta * new Color(1f, 1f, 1f, 0.8f);
            var highlightColor = styles?.HighlightColor ?? Color.cyan * new Color(1f, 1f, 1f, 0.8f);
            var blockedInteractionColor = styles?.BlockedInteractionColor ?? new Color(1, 1, 1, 0.5f);
            var highlightThickness = styles?.HighlightThickness ?? 3f;

            var unmaskedViews = new UnmaskedView.MaskData();
            unmaskedViews.AddParentFullyUnmasked(this);
            var highlightedViews = new UnmaskedView.MaskData();

            MaskingManager.Mask(
                unmaskedViews,
                maskingColor,
                highlightedViews,
                highlightColor,
                blockedInteractionColor,
                highlightThickness
            );

            MaskingEnabled = true;
        }

        void Unmask()
        {
            MaskingManager.Unmask();
            MaskingEnabled = false;
        }

        void OnWelcomePageModified()
        {
            s_IsBeingModified = true;

            // TODO try to find a way to author window title for utility window at real-time.
            //if (WelcomePage.WindowTitle != m_PreviousWindowTitle)
            //{
            //    // The way this window is shown currently requires us to recreate the window
            //    // in order to see a change in the title.
            //    Close();
            //    EditorApplication.delayCall += () => TryToShow(WelcomePage, m_OnClose);
            //}
            //else
            {
                UpdateContent();
            }
        }
    }
}
