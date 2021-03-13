using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using static Unity.InteractiveTutorials.RichTextParser;

namespace Unity.InteractiveTutorials.Tests
{
    public class RichTextParserTests
    {
        TutorialWindow m_Window;

        string k_LoremIpsum = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. " +
            "Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown" +
            " printer took a galley of type and scrambled it to make a type specimen book. It has survived" +
            " not only five centuries, but also the leap into electronic typesetting, remaining essentially" +
            " unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem" +
            " Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including" +
            " versions of Lorem Ipsum.";

        int m_BoldUsed;
        int m_ItalicUsed;
        int m_LinksUsed;
        int m_NumParagraphs;

        string Bold(string toBold)
        {
            m_BoldUsed++;
            return " <b>" + toBold + "</b> ";
        }

        string Italic(string toBold)
        {
            m_ItalicUsed++;
            return " <i>" + toBold + "</i> ";
        }

        string Link(string URL, string Text)
        {
            m_LinksUsed++;
            return " <a href=" + '\"' + URL + '\"' + ">" + Text + "</a> ";
        }

        string EmptyParagraph()
        {
            m_NumParagraphs++;
            return "\n\n";
        }

        void Reset()
        {
            m_BoldUsed = 0;
            m_ItalicUsed = 0;
            m_LinksUsed = 0;
            m_NumParagraphs = 0;
        }

        [SetUp]
        public void SetUp()
        {
            Reset();
            m_Window = EditorWindow.GetWindow<TutorialWindow>();
            TutorialWindow.ShowTutorialsClosedDialog.SetValue(false);
            m_Window.rootVisualElement.style.flexDirection = FlexDirection.Row;
            m_Window.rootVisualElement.style.flexWrap = Wrap.Wrap;
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }

        [Test]
        public void CanCreateWrappingTextLabels()
        {
            Reset();
            RichTextToVisualElements(CreateRichText(50, 0, 0, 0), m_Window.rootVisualElement);

            Assert.IsTrue(DoStylesMatch());
        }

        [Test]
        public void CanCreateRichText()
        {
            Reset();
            RichTextToVisualElements(CreateRichText(50, 10, 10, 5), m_Window.rootVisualElement);
            Assert.IsTrue(DoStylesMatch());
        }

        [Test]
        public void CanCreateParagraphsOfRichText()
        {
            Reset();
            string richText = "";
            for (int i = 0; i < 10; i++)
            {
                richText += CreateRichText(i * 10, i * 2, i, i) + EmptyParagraph();
            }
            RichTextToVisualElements(richText, m_Window.rootVisualElement);
            Assert.IsTrue(DoStylesMatch());
        }

        bool DoStylesMatch()
        {
            int boldFound = CountStyles(FontStyle.Bold);
            int italicFound = CountStyles(FontStyle.Italic);

            if (boldFound != m_BoldUsed)
            {
                Debug.LogError("Invalid amount of bold words. Entered: " + m_BoldUsed + " - Found: " + boldFound);
                return false;
            }

            if (italicFound != m_ItalicUsed)
            {
                Debug.LogError("Invalid amount of italic words. Entered: " + m_ItalicUsed + " - Found: " + italicFound);
                return false;
            }
            return true;
        }

        int CountWordLabels()
        {
            return m_Window.rootVisualElement.childCount;
        }

        int CountStyles(StyleEnum<FontStyle> style)
        {
            var root = m_Window.rootVisualElement;
            int styledWords = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.ElementAt(i).style.unityFontStyleAndWeight == style)
                {
                    styledWords++;
                }
            }
            return styledWords;
        }

        string CreateRichText(int normalWords, int boldedWords, int italicWords, int links)
        {
            string richText = "";
            int wordNumber = 0;
            string[] loremIpsums = k_LoremIpsum.Split(' ');
            while (normalWords > 0 || boldedWords > 0 || italicWords > 0 || links > 0)
            {
                if (normalWords > 0)
                {
                    richText += loremIpsums[wordNumber] + " ";
                    normalWords--;
                    wordNumber = (wordNumber + 1) % loremIpsums.Length;
                }
                if (boldedWords > 0)
                {
                    richText += Bold(loremIpsums[wordNumber]);
                    boldedWords--;
                    wordNumber = (wordNumber + 1) % loremIpsums.Length;
                }
                if (italicWords > 0)
                {
                    richText += Italic(loremIpsums[wordNumber]);
                    italicWords--;
                    wordNumber = (wordNumber + 1) % loremIpsums.Length;
                }
                if (links > 0)
                {
                    richText += Link("http://www.google.fi", loremIpsums[wordNumber]);
                    links--;
                    wordNumber = (wordNumber + 1) % loremIpsums.Length;
                }
            }
            return richText;
        }
    }
}
