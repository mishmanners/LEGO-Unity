using TMPro;

namespace Unity.LEGO.UI.SpeechBubbles
{
    public interface ISpeechBubble
    {
        TextMeshProUGUI Text { get; }
        float Height { get; }
        float TextDelay { get; }
        float DeactivationDuration { get; }

        void Activate();
        void Deactivate();
    }
}
