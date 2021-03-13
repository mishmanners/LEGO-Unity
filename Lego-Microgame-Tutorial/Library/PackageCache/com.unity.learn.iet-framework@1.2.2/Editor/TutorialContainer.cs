using System;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// An index for the tutorials in the project.
    /// </summary>
    public class TutorialContainer : ScriptableObject
    {
        /// <summary>
        /// Raised when any TutorialContainer is modified.
        /// </summary>
        /// <remarks>
        /// Raised before Modified event.
        /// </remarks>
        public static event Action<TutorialContainer> TutorialContainerModified;  // TODO 2.0 merge the two Modified events?

        /// <summary>
        /// Raised when any field of this container is modified.
        /// </summary>
        public event Action Modified;

        /// <summary>
        /// Background texture for the header area that is used to display Title and Subtitle.
        /// </summary>
        public Texture2D HeaderBackground;

        /// <summary>
        /// Subtitle shown in the header area.
        /// </summary>
        // TODO 2.0 rename to Subtitle.
        [Tooltip("Subtitle shown in the header area.")]
        public LocalizableString Title;

        /// <summary>
        /// Title shown in the header area.
        /// </summary>
        // TODO 2.0 rename to Title
        [Tooltip("Title shown in the header area.")]
        public LocalizableString ProjectName;

        /// <summary>
        /// TODO 2.0 deprecated currently but might be used when we implement tutorial categories.
        /// </summary>
        [Obsolete]
        public LocalizableString Description;

        /// <summary>
        /// Can be used to override or disable the default layout specified by the Tutorial Framework.
        /// </summary>
        [Tooltip("Can be used to override or disable the default layout specified by the Tutorial Framework.")]
        public UnityEngine.Object ProjectLayout;

        /// <summary>
        /// Sections/cards of this container.
        /// </summary>
        public Section[] Sections = {};

        /// <summary>
        /// Returns the path for the ProjectLayout, relative to the project folder,
        /// or a default tutorial layout path if ProjectLayout not specified.
        /// </summary>
        public string ProjectLayoutPath =>
            ProjectLayout != null ? AssetDatabase.GetAssetPath(ProjectLayout) : k_DefaultLayoutPath;

        // The default layout used when a project is started for the first time.
        internal static readonly string k_DefaultLayoutPath =
            "Packages/com.unity.learn.iet-framework/Editor/Layouts/DefaultLayout.wlt";

        /// <summary>
        /// A section/card for starting a tutorial or opening a web page.
        /// </summary>
        [Serializable]
        public class Section
        {
            /// <summary>
            /// Order the the view. Use 0, 2, 4, and so on.
            /// </summary>
            public int OrderInView; // TODO I wonder what's the original purpose of this. Try to get rid of this.

            /// <summary>
            /// Title of the card.
            /// </summary>
            public LocalizableString Heading;

            /// <summary>
            /// Description of the card.
            /// </summary>
            public LocalizableString Text;

            // TODO Rename
            /// <summary>
            /// Used as content type metadata for external references/URLs
            /// </summary>
            [Tooltip("Used as content type metadata for external references/URLs")]
            public string LinkText;

            /// <summary>
            /// The URL of this section.
            /// Setting the URL will take precedence and make the card act as a link card instead of a tutorial card
            /// </summary>
            [Tooltip("Setting the URL will take precedence and make the card act as a link card instead of a tutorial card")]
            public string Url;

            /// <summary>
            /// Use for Unity Connect auto-login, shortened URLs do not work
            /// </summary>
            [Tooltip("Use for Unity Connect auto-login, shortened URLs do not work")]
            [HideInInspector, System.Obsolete("todo: No longer used, remove in 2.0")]
            public bool AuthorizedUrl;

            /// <summary>
            /// Image for the card.
            /// </summary>
            public Texture2D Image;

            /// <summary>
            /// The tutorial this container contains
            /// </summary>
            public Tutorial Tutorial;

            /// <summary>
            /// Has the tutorial been already completed?
            /// </summary>
            public bool TutorialCompleted { get; set; }

            /// <summary>
            /// Does this represent a tutorial?
            /// </summary>
            public bool IsTutorial => Url.IsNullOrEmpty();

            /// <summary>
            /// The ID of the represented tutorial, if any
            /// </summary>
            public string TutorialId => Tutorial?.lessonId.AsEmptyIfNull();

            /// <summary>
            /// TODO 2.0 make internal.
            /// </summary>
            public string SessionStateKey => $"Unity.InteractiveTutorials.lesson{TutorialId}";

            /// <summary>
            /// Starts the tutorial of the section
            /// </summary>
            public void StartTutorial()
            {
                TutorialManager.instance.StartTutorial(Tutorial);
            }

            /// <summary>
            /// Opens the URL Of the section, if any
            /// </summary>
            public void OpenUrl()
            {
                TutorialEditorUtils.OpenUrl(Url);
                AnalyticsHelper.SendExternalReferenceEvent(Url, Heading.Untranslated, LinkText, Tutorial?.lessonId);
            }

            /// <summary>
            /// Loads the state of the section from SessionState.
            /// </summary>
            /// <returns>returns true if the state was found from EditorPrefs</returns>
            public bool LoadState()
            {
                const string nonexisting = "NONEXISTING";
                var state = SessionState.GetString(SessionStateKey, nonexisting);
                if (state == "")
                {
                    TutorialCompleted = false;
                }
                else if (state == "Finished")
                {
                    TutorialCompleted = true;
                }
                return state != nonexisting;
            }

            /// <summary>
            /// Saves the state of the section from SessionState.
            /// </summary>
            public void SaveState()
            {
                SessionState.SetString(SessionStateKey, TutorialCompleted ? "Finished" : "");
            }
        }

        void OnValidate()
        {
            SortSections();
            for (int i = 0; i < Sections.Length; ++i)
            {
                Sections[i].OrderInView = i * 2;
            }
        }

        void SortSections()
        {
            Array.Sort(Sections, (x, y) => x.OrderInView.CompareTo(y.OrderInView));
        }

        /// <summary>
        /// Loads the tutorial project layout
        /// </summary>
        public void LoadTutorialProjectLayout()
        {
            TutorialManager.LoadWindowLayoutWorkingCopy(ProjectLayoutPath);
        }

        /// <summary>
        /// Raises the Modified events for this asset.
        /// </summary>
        public void RaiseModifiedEvent()
        {
            TutorialContainerModified?.Invoke(this);
            Modified?.Invoke();
        }
    }
}
