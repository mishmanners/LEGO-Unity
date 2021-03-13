using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    /// <summary>
    /// Utilities for EditorWindows.
    /// </summary>
    public static class EditorWindowUtils
    {
        /// <summary>
        /// Finds the first open EditorWindow instance, if such exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FindOpenInstance<T>() where T : EditorWindow =>
            Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();

        /// <summary>
        /// Supported dock positions.
        /// </summary>
        public enum DockPosition
        {
            /// <summary>
            /// Dock to the left side of the window.
            /// </summary>
            Left,

            // TODO Top docking not working, investigate.
            // Top,

            /// <summary>
            /// Dock to the right side of the window.
            /// </summary>
            Right,
            /// <summary>
            /// Dock to the bottom of the window.
            /// </summary>
            Bottom
        }

        /// <summary>
        /// Docks the "docked" window to the "anchor" window at the given position.
        /// </summary>
        /// <param name="anchor">Window to dock.</param>
        /// <param name="docked">Window to dock into.</param>
        /// <param name="position">Position to the docked into.</param>
        public static void DockWindow(this EditorWindow anchor, EditorWindow docked, DockPosition position)
        {
            // NOTE Code adapted from https://gist.github.com/Thundernerd/5085ec29819b2960f5ff2ee32ad57cbb#gistcomment-2834853
            var anchorParent = GetParentOf(anchor);
            SetDragSource(anchorParent, GetParentOf(docked));
            PerformDrop(GetWindowOf(anchorParent), docked, GetFakeMousePosition(anchor, position));
        }

        /// <summary>
        /// Centers an EditorWindow to the Editor main window.
        /// </summary>
        /// <param name="win"></param>
        public static void CenterOnMainWindow(EditorWindow win)
        {
            var main = GetEditorMainWindowPos();
            var pos = win.position;
            float w = (main.width - pos.width) * 0.5f;
            float h = (main.height - pos.height) * 0.5f;
            pos.x = main.x + w;
            pos.y = main.y + h;
            win.position = pos;
        }

        /// <summary>
        /// Returns the position of the Editor main window.
        /// </summary>
        /// <returns></returns>
        public static Rect GetEditorMainWindowPos()
        {
            // NOTE Code adapted from http://answers.unity.com/answers/960709/view.html
            var containerWinType = GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(ScriptableObject))
                .Where(t => t.Name == "ContainerWindow")
                .FirstOrDefault();
            if (containerWinType == null)
                throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");

            var showModeField = containerWinType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionProperty = containerWinType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
                throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");

            foreach (var win in Resources.FindObjectsOfTypeAll(containerWinType))
            {
                var showmode = (int)showModeField.GetValue(win);
                if (showmode == 4) // main window
                {
                    var pos = (Rect)positionProperty.GetValue(win, null);
                    return pos;
                }
            }

            throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
        }

        /// <summary>
        /// Sets the position of the Editor main window.
        /// </summary>
        /// <param name="pos"></param>
        public static void SetEditorMainWindowPos(Rect pos)
        {
            // TODO copy-pasta, generalise and clean up the code with GetEditorMainWindowPos
            var containerWinType = GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(ScriptableObject))
                .Where(t => t.Name == "ContainerWindow")
                .FirstOrDefault();
            if (containerWinType == null)
                throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");

            var showModeField = containerWinType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionProperty = containerWinType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (showModeField == null || positionProperty == null)
                throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");

            foreach (var win in Resources.FindObjectsOfTypeAll(containerWinType))
            {
                var showmode = (int)showModeField.GetValue(win);
                if (showmode == 4) // main window
                {
                    positionProperty.SetValue(win, pos);
                    return;
                }
            }

            throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
        }

        static IEnumerable<Type> GetAllDerivedTypes(AppDomain appDomain, Type parentType)
        {
            return appDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(parentType));
        }

        static EditorWindowUtils()
        {
            // Assertions to surface potential reflection problems as soon as possible.
            // These should make Packge Validation Suite's EmptyConsoleTest to fail in case
            // the internal APIs would change.

            // TODO assert types used by GetEditorMainWindowPos() also.

            // EditorWindow
            var type = typeof(EditorWindow);
            Debug.Assert(
                type.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic) != null,
                "Internal API EditorWindow.m_Parent missing."
            );

            // DockArea
            type = Type.GetType("UnityEditor.DockArea, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            Debug.Assert(type != null);
            Debug.Assert(
                type.GetField("s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic) != null,
                "Internal API DockArea.s_OriginalDragSource missing."
            );

            // HostView
            type = Type.GetType("UnityEditor.HostView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            Debug.Assert(type != null);
            Debug.Assert(
                type.GetProperty("window", BindingFlags.Instance | BindingFlags.Public) != null,
                "Internal API HostView.window missing."
            );

            // ContainerWindow
            type = Type.GetType("UnityEditor.ContainerWindow, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            Debug.Assert(type != null);
            Debug.Assert(
                type.GetProperty("rootSplitView", BindingFlags.Instance | BindingFlags.Public) != null,
                "Internal API ContainerWindow.rootSplitView missing."
            );

            // SplitView
            type = Type.GetType("UnityEditor.SplitView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            Debug.Assert(
                type.GetMethod("DragOver", BindingFlags.Instance | BindingFlags.Public) != null,
                "Internal API SplitView.DragOver does not exist."
            );
            Debug.Assert(
                type.GetMethod("PerformDrop", BindingFlags.Instance | BindingFlags.Public) != null,
                "Internal API SplitView.PerformDrop does not exist."
            );
        }

        static Vector2 GetFakeMousePosition(EditorWindow wnd, DockPosition position)
        {
            Vector2 mousePosition = Vector2.zero;

            // The 20 is required to make the docking work.
            // Smaller values might not work when faking the mouse position.
            switch (position)
            {
                case DockPosition.Left:
                    mousePosition.Set(20, wnd.position.size.y / 2);
                    break;
                //case DockPosition.Top:
                //    mousePosition.Set(wnd.position.size.x / 2, 20);
                //    break;
                case DockPosition.Right:
                    mousePosition.Set(wnd.position.size.x - 20, wnd.position.size.y / 2);
                    break;
                case DockPosition.Bottom:
                    mousePosition.Set(wnd.position.size.x / 2, wnd.position.size.y - 20);
                    break;
            }

            return new Vector2(wnd.position.x + mousePosition.x, wnd.position.y + mousePosition.y);
        }

        static object GetParentOf(object target)
        {
            var field = target.GetType().GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(target);
        }

        static object GetWindowOf(object target)
        {
            var property = target.GetType().GetProperty("window", BindingFlags.Instance | BindingFlags.Public);
            return property.GetValue(target, null);
        }

        static void SetDragSource(object target, object source)
        {
            var field = target.GetType().GetField("s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, source);
        }

        static void PerformDrop(object window, EditorWindow child, Vector2 screenPoint)
        {
            var rootSplitViewProperty = window.GetType().GetProperty("rootSplitView", BindingFlags.Instance | BindingFlags.Public);
            object rootSplitView = rootSplitViewProperty.GetValue(window, null);

            var dragMethod = rootSplitView.GetType().GetMethod("DragOver", BindingFlags.Instance | BindingFlags.Public);
            var dropMethod = rootSplitView.GetType().GetMethod("PerformDrop", BindingFlags.Instance | BindingFlags.Public);

            var dropInfo = dragMethod.Invoke(rootSplitView, new object[] { child, screenPoint });
            if (dropInfo != null)
                dropMethod.Invoke(rootSplitView, new object[] { child, dropInfo, screenPoint });

            // NOTE Could consider returning the return value (bool) of SplitView.PerformDrop() here
            // but looking at the code it seems to return always 'true' so this seem futile currently.
        }
    }
}
