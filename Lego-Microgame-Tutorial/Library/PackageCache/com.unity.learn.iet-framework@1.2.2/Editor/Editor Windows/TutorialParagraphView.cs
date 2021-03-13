using UnityEditor;
using UnityEngine;
using System;

namespace Unity.InteractiveTutorials
{
    [Serializable]
    class TutorialParagraphView
    {
        // TODO Will be (merged into TutorialWindow and) refactored away completely soon
        public TutorialParagraphView(TutorialParagraph paragraph, EditorWindow window, string orderedListDelimiter, string unorderedListBullet, int instructionIndex)
        {
            this.paragraph = paragraph;
        }

        public void ResetState()
        {
        }

        public void SetWindow(TutorialWindow window)
        {
            m_TutorialWindow = window;
        }

        TutorialParagraph paragraph;
        TutorialWindow m_TutorialWindow;
        Texture videoTextureCache;

        void RepaintSoon()
        {
            if (m_TutorialWindow)
            {
                m_TutorialWindow.Repaint();
                m_TutorialWindow.UpdateVideoFrame(videoTextureCache);
            }
            EditorApplication.update -= RepaintSoon;
        }

        public void Draw(ref bool previousTaskState, bool pageCompleted)
        {
            switch (paragraph.type)
            {
                case ParagraphType.Image:
                    // TODO currently draws image all the time - let's draw it once for each page
                    videoTextureCache = paragraph.image;
                    EditorApplication.update += RepaintSoon;
                    break;
                case ParagraphType.Video:
                    if (paragraph.video != null && m_TutorialWindow != null)
                    {
                        videoTextureCache = m_TutorialWindow.videoPlaybackManager.GetTextureForVideoClip(paragraph.video);
                        EditorApplication.update += RepaintSoon;
                    }
                    break;
            }
        }
    }
}
