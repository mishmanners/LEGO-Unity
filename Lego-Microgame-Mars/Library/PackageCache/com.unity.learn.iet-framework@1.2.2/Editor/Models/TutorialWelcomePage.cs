using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Welcome page/dialog for a project shown using TutorialModalWindow.
    /// In addition of window title, header image, title, and description,
    /// a welcome page/dialog contains a fully customizable button row.
    /// </summary>
    public class TutorialWelcomePage : ScriptableObject
    {
        /// <summary>
        /// Data for a customizable button.
        /// </summary>
        [Serializable]
        public class ButtonData
        {
            /// <summary>
            /// Text of the button.
            /// </summary>
            public LocalizableString Text;
            /// <summary>
            /// Tooltip of the button.
            /// </summary>
            public LocalizableString Tooltip;
            /// <summary>
            /// Callback for the button click.
            /// </summary>
            public UnityEvent OnClick;
        }

        /// <summary>
        /// Raised when any welcome page is modified.
        /// </summary>
        /// <remarks>
        /// Raised before Modified event.
        /// </remarks>
        public static event Action<TutorialWelcomePage> TutorialWelcomePageModified;  // TODO 2.0 merge the two Modified events?

        /// <summary>
        /// Raised when any field of the welcome page is modified.
        /// </summary>
        public event Action Modified;

        /// <summary>
        /// Header image of the welcome dialog.
        /// </summary>
        public Texture2D Image => m_Image;
        [SerializeField]
        Texture2D m_Image = default;

        /// <summary>
        /// Window title of the welcome dialog.
        /// </summary>
        public LocalizableString WindowTitle => m_WindowTitle;
        [SerializeField]
        internal LocalizableString m_WindowTitle = default;

        /// <summary>
        /// Title of the welcome dialog.
        /// </summary>
        public LocalizableString Title => m_Title;
        [SerializeField]
        internal LocalizableString m_Title = default;

        /// <summary>
        /// Description of the welcome dialog.
        /// </summary>
        public LocalizableString Description => m_Description;
        [SerializeField, LocalizableTextArea(1, 10)]
        internal LocalizableString m_Description;

        /// <summary>
        /// Buttons specified for the welcome page.
        /// </summary>
        public ButtonData[] Buttons => m_Buttons;
        [SerializeField]
        internal ButtonData[] m_Buttons = default;

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseModifiedEvent()
        {
            TutorialWelcomePageModified?.Invoke(this);
            Modified?.Invoke();
        }

        /// <summary>
        /// Creates a default Close button.
        /// </summary>
        public static ButtonData CreateCloseButton(TutorialWelcomePage page)
        {
            var data = new ButtonData { Text = "Close", OnClick = new UnityEvent() };
            UnityEventTools.AddVoidPersistentListener(data.OnClick, page.CloseCurrentModalDialog);
            data.OnClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            return data;
        }

        // Providing functionality for three default behaviours of the welcome dialog.

        /// <summary>
        /// Closes the an open instance of TutorialModalWindow.
        /// </summary>
        public void CloseCurrentModalDialog()
        {
            var wnd = EditorWindowUtils.FindOpenInstance<TutorialModalWindow>();
            if (wnd)
                wnd.Close();
        }

        /// <summary>
        /// Exits the Editor.
        /// </summary>
        public void ExitEditor()
        {
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Starts the tutorial specified as the start-up tutorial for the project.
        /// </summary>
        public void StartTutorial()
        {
            var projectSettings = TutorialProjectSettings.instance;
            if (projectSettings.startupTutorial)
                TutorialManager.instance.StartTutorial(projectSettings.startupTutorial);
        }
    }
}
