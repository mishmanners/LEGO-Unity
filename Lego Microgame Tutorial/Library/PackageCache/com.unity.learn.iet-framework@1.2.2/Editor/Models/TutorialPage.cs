using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.InteractiveTutorials
{
    enum PlayModeState
    {
        Any,
        Playing,
        NotPlaying
    }

    /// <summary>
    /// A TutorialPage consists of TutorialParagraphs which define the content of the page.
    /// </summary>
    public class TutorialPage : ScriptableObject, ISerializationCallbackReceiver
    {
        public static event Action<TutorialPage> criteriaCompletionStateTested;
        // TODO 2.0 merge these two events and provide event data which tells which type of change we had?
        public static event Action<TutorialPage> tutorialPageMaskingSettingsChanged;
        public static event Action<TutorialPage> tutorialPageNonMaskingSettingsChanged;

        internal event Action<TutorialPage> playedCompletionSound;

        public bool hasMovedToNextPage { get; private set; }

        public bool allCriteriaAreSatisfied { get; private set; }

        public TutorialParagraphCollection paragraphs { get { return m_Paragraphs; } }
        [SerializeField]
        internal TutorialParagraphCollection m_Paragraphs = new TutorialParagraphCollection();

        public bool HasCriteria ()
        {
            foreach(TutorialParagraph para in paragraphs )
            {
                foreach(TypedCriterion crit in para.criteria)
                {
                    if (crit.criterion != null) return true;
                }
            }

            return false;
        }

        public MaskingSettings currentMaskingSettings
        {
            get
            {
                MaskingSettings result = null;
                for (int i = 0, count = m_Paragraphs.count; i < count; ++i)
                {
                    if (!m_Paragraphs[i].maskingSettings.enabled) { continue; }

                    result = m_Paragraphs[i].maskingSettings;
                    if (!m_Paragraphs[i].completed)
                        break;
                }
                return result;
            }
        }

        [Header("Initial Camera Settings")]
        [SerializeField]
        SceneViewCameraSettings m_CameraSettings = new SceneViewCameraSettings();

        /// <summary>
        /// The text shown on the Next button on all pages except the last page.
        /// </summary>
        [Header("Button Labels")]
        [Tooltip("The text shown on the next button on all pages except the last page.")]
        public LocalizableString NextButton;

        /// <summary>
        /// The text shown on the next button on the last page.
        /// </summary>
        [Tooltip("The text shown on the Next button on the last page.")]
        public LocalizableString DoneButton;

        /// <summary>
        /// TODO 2.0 deprecated, remove.
        /// </summary>
        public string nextButton
        {
            get { return m_NextButton; }
            set
            {
                if (m_NextButton != value)
                {
                    m_NextButton = value;
                    RaiseTutorialPageNonMaskingSettingsChangedEvent();
                }
            }
        }
        [SerializeField, HideInInspector]
        string m_NextButton = "Next";

        /// <summary>
        /// TODO 2.0 deprecated, remove.
        /// </summary>
        public string doneButton
        {
            get { return m_DoneButton; }
            set
            {
                if (m_DoneButton != value)
                {
                    m_DoneButton = value;
                    RaiseTutorialPageNonMaskingSettingsChangedEvent();
                }
            }
        }
        [SerializeField, HideInInspector]
        string m_DoneButton = "Done";

        /// <summary>
        /// Returns the asset database GUID of this asset.
        /// </summary>
        public string guid
        {
            get
            {
                return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
            }
        }

        [Header("Sounds")]
        [SerializeField]
        AudioClip m_CompletedSound = null;

        public bool autoAdvanceOnComplete { get { return m_autoAdvance; } set { m_autoAdvance = value; } }
        [Header("Auto advance on complete?")]
        [SerializeField]
        bool m_autoAdvance;

        [Header("Callbacks")]
        [SerializeField]
        [Tooltip("These methods will be called right before the page is displayed (even when going back)")]
        internal UnityEvent m_OnBeforePageShown = default;

        [Tooltip("These methods will be called right after the page is displayed (even when going back)")]
        [SerializeField]
        internal UnityEvent m_OnAfterPageShown = default;

        [Tooltip("These methods will be called when the user force-quits the tutorial from this tutorial page, before quitting the tutorial")]
        [SerializeField]
        internal UnityEvent m_OnBeforeTutorialQuit = default;

        [Tooltip("These methods will be called while the user is reading this tutorial page, every editor frame")]
        [SerializeField]
        internal UnityEvent m_OnTutorialPageStay = default;

        /// <summary> TODO 2.0 Make internal. </summary>
        public void RaiseTutorialPageMaskingSettingsChangedEvent()
        {
            tutorialPageMaskingSettingsChanged?.Invoke(this);
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void RaiseTutorialPageNonMaskingSettingsChangedEvent()
        {
            tutorialPageNonMaskingSettingsChanged?.Invoke(this);
        }

        static Queue<WeakReference<TutorialPage>> s_DeferedValidationQueue = new Queue<WeakReference<TutorialPage>>();

        static TutorialPage()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            while (s_DeferedValidationQueue.Count != 0)
            {
                var weakPageReference = s_DeferedValidationQueue.Dequeue();
                TutorialPage page;
                if (weakPageReference.TryGetTarget(out page))
                {
                    if (page != null) //Taking into account "unity null"
                    {
                        page.SyncCriteriaAndFutureReferences();
                    }
                }
            }
        }

        void OnValidate()
        {
            // Defer synchronization of sub-assets to next editor update due to AssetDatabase interactions

            // Retaining a reference to this instance in OnValidate/OnEnable can cause issues on project load
            // The same object might be imported more than once and if it's referenced it won't be unloaded correctly
            // Use WeakReference instead of subscribing directly to EditorApplication.update to avoid strong reference

            s_DeferedValidationQueue.Enqueue(new WeakReference<TutorialPage>(this));
        }

        void SyncCriteriaAndFutureReferences()
        {
            // Find instanceIDs of referenced criteria
            var referencedCriteriaInstanceIDs = new HashSet<int>();
            foreach (var paragraph in paragraphs)
            {
                foreach (var typedCriterion in paragraph.criteria)
                {
                    if (typedCriterion.criterion != null)
                        referencedCriteriaInstanceIDs.Add(typedCriterion.criterion.GetInstanceID());
                }
            }

            // Destroy unreferenced criteria
            var assetPath = AssetDatabase.GetAssetPath(this);
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var criteria = assets.Where(o => o is Criterion).Cast<Criterion>();
            foreach (var criterion in criteria)
            {
                if (!referencedCriteriaInstanceIDs.Contains(criterion.GetInstanceID()))
                    DestroyImmediate(criterion, true);
            }

            // Update future reference names
            var futureReferences = assets.Where(o => o is FutureObjectReference).Cast<FutureObjectReference>();
            foreach (var futureReference in futureReferences)
            {
                if (futureReference.criterion == null
                    || !referencedCriteriaInstanceIDs.Contains(futureReference.criterion.GetInstanceID()))
                {
                    // Destroy future reference from unrefereced criteria
                    DestroyImmediate(futureReference, true);
                }
                else
                    UpdateFutureObjectReferenceName(futureReference);
            }
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void UpdateFutureObjectReferenceName(FutureObjectReference futureReference)
        {
            int paragraphIndex;
            int criterionIndex;
            if (GetIndicesForCriterion(futureReference.criterion, out paragraphIndex, out criterionIndex))
            {
                futureReference.name = string.Format("Paragraph {0}, Criterion {1}, {2}",
                    paragraphIndex + 1, criterionIndex + 1, futureReference.referenceName);
            }
        }

        bool GetIndicesForCriterion(Criterion criterion, out int paragraphIndex, out int criterionIndex)
        {
            paragraphIndex = 0;
            criterionIndex = 0;

            foreach (var paragraph in paragraphs)
            {
                foreach (var typedCriterion in paragraph.criteria)
                {
                    if (typedCriterion.criterion == criterion)
                        return true;

                    criterionIndex++;
                }

                paragraphIndex++;
            }

            return false;
        }

        internal void Initiate()
        {
            SetupCompletionRequirements();
            if (m_CameraSettings != null && m_CameraSettings.enabled)
            {
                m_CameraSettings.Apply();
            }
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void ResetUserProgress()
        {
            RemoveCompletionRequirements();
            foreach (var paragraph in paragraphs)
            {
                if (paragraph.type == ParagraphType.Instruction)
                {
                    foreach (var criteria in paragraph.criteria)
                    {
                        if (criteria != null && criteria.criterion != null)
                        {
                            criteria.criterion.ResetCompletionState();
                            criteria.criterion.StopTesting();
                        }
                    }
                }
            }
            allCriteriaAreSatisfied = false;
            hasMovedToNextPage = false;
        }

        internal void SetupCompletionRequirements()
        {
            ValidateCriteria();
            if (hasMovedToNextPage)
                return;

            Criterion.criterionCompleted += OnCriterionCompleted;
            Criterion.criterionInvalidated += OnCriterionInvalidated;

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.criteria != null)
                {
                    foreach (var criterion in paragraph.criteria)
                    {
                        if (criterion.criterion)
                            criterion.criterion.StartTesting();
                    }
                }
            }
        }

        internal void RemoveCompletionRequirements()
        {
            Criterion.criterionCompleted -= OnCriterionCompleted;
            Criterion.criterionInvalidated -= OnCriterionInvalidated;

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.criteria != null)
                {
                    foreach (var criterion in paragraph.criteria)
                    {
                        if (criterion.criterion)
                        {
                            criterion.criterion.StopTesting();
                        }
                    }
                }
            }
        }

        void OnCriterionCompleted(Criterion sender)
        {
            if (!m_Paragraphs.Any(p => p.criteria.Any(c => c.criterion == sender)))
                return;

            if (sender.completed)
            {
                int paragraphIndex, criterionIndex;
                if (GetIndicesForCriterion(sender, out paragraphIndex, out criterionIndex))
                {
                    // only play sound effect and clear undo if all preceding criteria are already complete
                    var playSoundEffect = true;
                    for (int i = 0; i < paragraphIndex; ++i)
                    {
                        if (!m_Paragraphs[i].criteria.All(c => c.criterion.completed))
                        {
                            playSoundEffect = false;
                            break;
                        }
                    }
                    if (playSoundEffect)
                    {
                        Undo.ClearAll();
                        if (m_CompletedSound != null)
                            AudioUtilProxy.PlayClip(m_CompletedSound);
                        playedCompletionSound?.Invoke(this);
                    }
                }
            }
            ValidateCriteria();
        }

        void OnCriterionInvalidated(Criterion sender)
        {
            if (m_Paragraphs.Any(p => p.criteria.Any(c => c.criterion == sender)))
                ValidateCriteria();
        }

        internal void ValidateCriteria()
        {
            allCriteriaAreSatisfied = true;

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.type == ParagraphType.Instruction)
                {
                    if (!paragraph.completed)
                    {
                        allCriteriaAreSatisfied = false;
                        break;
                    }
                }

                if (!allCriteriaAreSatisfied)
                    break;
            }

            criteriaCompletionStateTested?.Invoke(this);
        }

        /// <summary> TODO 2.0 Make internal. </summary>
        public void OnPageCompleted()
        {
            RemoveCompletionRequirements();
            hasMovedToNextPage = true;
        }

        /// <summary>
        /// Called when the frontend of the page has not been displayed yet to the user
        /// TODO 2.0 Make internal.
        /// </summary>
        public void RaiseOnBeforePageShownEvent()
        {
            m_OnBeforePageShown?.Invoke();
        }

        /// <summary>
        /// Called right after the frontend of the page is displayed to the user
        /// TODO 2.0 Make internal.
        /// </summary>
        public void RaiseOnAfterPageShownEvent()
        {
            m_OnAfterPageShown?.Invoke();
        }

        /// <summary>
        /// Called when the user force-quits the tutorial from this tutorial page, before quitting the tutorial
        /// </summary>
        internal void RaiseOnBeforeQuitTutorialEvent()
        {
            m_OnBeforeTutorialQuit?.Invoke();
        }

        /// <summary>
        /// Called while the user is reading this tutorial page, every editor frame
        /// </summary>
        internal void RaiseOnTutorialPageStayEvent()
        {
            m_OnTutorialPageStay?.Invoke();
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
            TutorialParagraph.MigrateStringToLocalizableString(ref m_NextButton, ref NextButton);
            TutorialParagraph.MigrateStringToLocalizableString(ref m_DoneButton, ref DoneButton);
        }
    }
}
