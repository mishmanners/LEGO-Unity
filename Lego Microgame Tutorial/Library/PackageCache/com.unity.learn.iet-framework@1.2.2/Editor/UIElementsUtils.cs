using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// An utility class for common UIElements setup method
    /// </summary>
    public static class UIElementsUtils
    {
        public static void SetupButton(string buttonName, Action onClickAction, bool isEnabled, VisualElement parent, string tooltip = "", bool showIfEnabled = true)
        {
            Button button = parent.Query<Button>(buttonName);
            button.SetEnabled(isEnabled);
            button.clickable = new Clickable(() => onClickAction.Invoke());
            button.tooltip = string.IsNullOrEmpty(tooltip) ? button.text : tooltip;
            if (!showIfEnabled || !isEnabled) { return; }
            Show(button);
        }

        public static void SetupLabel(string labelName, string text, VisualElement parent, Manipulator manipulator = null)
        {
            Label label = parent.Query<Label>(labelName);
            label.text = text;
            if (manipulator == null) { return; }

            label.AddManipulator(manipulator);
        }

        public static void Hide(string elementName, VisualElement parent) { Hide(parent.Query<VisualElement>(elementName)); }
        public static void Show(string elementName, VisualElement parent) { Show(parent.Query<VisualElement>(elementName)); }
        public static void Hide(VisualElement element) { element.style.display = DisplayStyle.None; }
        public static void Show(VisualElement element) { element.style.display = DisplayStyle.Flex; }
        public static bool IsVisible(VisualElement element) { return (element == null) ? false : element.style.display != DisplayStyle.None; }

        public static void RemoveStyleSheet(StyleSheet styleSheet, VisualElement target)
        {
            if (!styleSheet) { return; }
            if (!target.styleSheets.Contains(styleSheet)) { return; }
            target.styleSheets.Remove(styleSheet);
        }
    }
}
