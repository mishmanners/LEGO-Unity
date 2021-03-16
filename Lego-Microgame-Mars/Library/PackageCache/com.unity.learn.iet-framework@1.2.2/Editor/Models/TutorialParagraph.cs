using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Serialization;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Different paragraph types.
    /// </summary>
    public enum ParagraphType
    {
        /// <summary>
        /// Text.
        /// </summary>
        Narrative,
        /// <summary>
        /// Text instructions with underlying completion criterion logic.
        /// </summary>
        Instruction,
        /// <summary>
        /// A button for switching to another tutorial.
        /// </summary>
        SwitchTutorial,
        /// <summary>
        /// TODO 2.0 deprecated, remove.
        /// </summary>
        UnorderedList,
        /// <summary>
        /// TODO 2.0 deprecated, remove.
        /// </summary>
        OrderedList,
        /// <summary>
        /// TODO 2.0 deprecated, remove.
        /// </summary>
        Icons,
        /// <summary>
        /// An image.
        /// </summary>
        Image,
        /// <summary>
        /// A video clip.
        /// </summary>
        Video,
    }

    enum CompletionType
    {
        CompletedWhenAllAreTrue,    // TODO Simplify name, "All(True)"
        CompletedWhenAnyIsTrue      // TODO Simplify name, "Any(True)"
    }

    /// <summary>
    /// A section of the TutorialPage.
    /// </summary>
    [Serializable]
    public class TutorialParagraph : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Type of this paragraph.
        /// </summary>
        public ParagraphType Type { get => m_Type; internal set => m_Type = value; }
        [SerializeField]
        internal ParagraphType m_Type;

        /// <summary>
        /// Title for Narrative/Instruction, not applicable for SwitchTutorial currently.
        /// </summary>
        public LocalizableString Title;

        /// <summary>
        /// Text/description for Narrative/Instruction or button text for SwitchTutorial.
        /// </summary>
        [LocalizableTextArea(1, 15)]
        public LocalizableString Text;

        /// <summary> TODO 2.0 Deprecated, remove </summary>
        [Obsolete]
        public string summary { get { return m_Summary; } set { m_Summary = value; } }
        [SerializeField, TextArea(1, 1)]
        [Obsolete, HideInInspector]
        string m_Summary;

        /// <summary> TODO 2.0 Deprecated, remove </summary>
        [Obsolete]
        public string Description { get { return m_Description; } set { m_Description = value; } }
        [FormerlySerializedAs("m_description1")]
        [SerializeField, TextArea(1, 8)]
        [Obsolete, HideInInspector]
        string m_Description;

        /// <summary> TODO 2.0 Deprecated, remove </summary>
        [Obsolete]
        public string InstructionTitle { get { return m_InstructionBoxTitle; } set { m_InstructionBoxTitle = value; } }
        [FormerlySerializedAs("m_Text")]
        [SerializeField, TextArea(1, 15)]
        [Obsolete, HideInInspector]
        string m_InstructionBoxTitle;

        /// <summary> TODO 2.0 Deprecated, remove </summary>
        [Obsolete]
        public string InstructionText { get { return m_InstructionText; } set { m_InstructionText = value; } }
        [SerializeField, TextArea(1, 15)]
        [Obsolete, HideInInspector]
        string m_InstructionText;

        /// <summary> TODO 2.0 Deprecated, remove, superseded by Text </summary>
        [SerializeField]
        [Obsolete, HideInInspector]
        internal string m_TutorialButtonText = "";

        /// <summary>
        /// Used for SwitchTutorial.
        /// </summary>
        [SerializeField]
        internal Tutorial m_Tutorial;

        /// <summary>
        /// TODO 2.0 Deprecated, remove.
        /// </summary>
        public IEnumerable<InlineIcon> icons
        {
            get
            {
                m_Icons.GetItems(m_IconBuffer);
                return m_IconBuffer;
            }
        }
        [SerializeField]
        InlineIconCollection m_Icons = new InlineIconCollection();
        readonly List<InlineIcon> m_IconBuffer = new List<InlineIcon>();

        /// <summary>
        /// The image if this paragraph's type is Image.
        /// </summary>
        public Texture2D Image { get => m_Image; set => m_Image = value; }
        [SerializeField]
        Texture2D m_Image = null;

        /// <summary>
        /// The video clip if this paragraph's type is Video.
        /// </summary>
        public VideoClip Video { get => m_Video; set => m_Video = value; }
        [SerializeField]
        VideoClip m_Video = null;

        /// <summary> TODO 2.0 remove </summary>
        public ParagraphType type { get => Type; }
        /// <summary> TODO 2.0 remove </summary>
        public Texture2D image { get => Image; set => Image = value; }
        /// <summary> TODO 2.0 remove </summary>
        public VideoClip video { get => Video; set => Video = value; }

        [SerializeField]
        internal CompletionType m_CriteriaCompletion = CompletionType.CompletedWhenAllAreTrue;

        [SerializeField] internal TypedCriterionCollection m_Criteria = new TypedCriterionCollection();
        readonly List<TypedCriterion> m_CriteriaBuffer = new List<TypedCriterion>();

        /// <summary>
        /// The completion criteria if this paragraph's type is Instruction.
        /// </summary>
        public IEnumerable<TypedCriterion> criteria
        {
            get
            {
                m_Criteria.GetItems(m_CriteriaBuffer);
                return m_CriteriaBuffer.ToArray();
            }
        }

        /// <summary>
        /// The masking settings for this paragraph.
        /// </summary>
        public MaskingSettings maskingSettings { get { return m_MaskingSettings; } }
        [SerializeField]
        MaskingSettings m_MaskingSettings = new MaskingSettings();

        /// <summary>
        /// Is this paragraph completed? Applicable if this paragraph's type is Instruction.
        /// </summary>
        public bool Completed => completed;
        /// <summary> TODO 2.0 Deprecated, will be renamed to Completed </summary>
        public bool completed
        {
            get
            {
                bool allMandatory = m_CriteriaCompletion == CompletionType.CompletedWhenAllAreTrue;
                bool result = allMandatory;

                foreach (var typedCriterion in m_Criteria)
                {
                    var criterion = typedCriterion.criterion;
                    if (criterion != null)
                    {
                        if (!allMandatory && criterion.completed)
                        {
                            result = true;
                            break;
                        }

                        if (allMandatory && !criterion.completed)
                        {
                            result = false;
                            break;
                        }
                    }
                }

                return result;
            }
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
#pragma warning disable 612 // suppress warnings for using obsolete fields
            // Migrate content from < 1.2.
            switch (type)
            {
                case ParagraphType.Narrative:
                    MigrateStringToLocalizableString(ref m_Summary, ref Title);
                    MigrateStringToLocalizableString(ref m_Description, ref Text);
                    break;
                case ParagraphType.Instruction:
                    MigrateStringToLocalizableString(ref m_InstructionBoxTitle, ref Title);
                    MigrateStringToLocalizableString(ref m_InstructionText, ref Text);
                    break;
                case ParagraphType.SwitchTutorial:
                    MigrateStringToLocalizableString(ref m_TutorialButtonText, ref Text);
                    break;
            }
#pragma warning restore 612
        }

        internal static void MigrateStringToLocalizableString(ref string oldField, ref LocalizableString newField)
        {
            if (newField.Untranslated.IsNullOrEmpty() && oldField.IsNotNullOrEmpty())
            {
                newField = oldField;
                oldField = string.Empty;
            }
        }
    }

    /// <summary> A wrapper class for serialization purposes. </summary>
    [Serializable]
    public class TutorialParagraphCollection : CollectionWrapper<TutorialParagraph>
    {
        /// <summary> Default-constructs an empty collection. </summary>
        public TutorialParagraphCollection() : base() {}
        /// <summary> Constructs a collection from existing items. </summary>
        /// <param name="items"></param>
        public TutorialParagraphCollection(IList<TutorialParagraph> items) : base(items) {}
    }
}
