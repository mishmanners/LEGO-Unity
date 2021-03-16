// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEditor;
using UnityEngine;
using System;

namespace LEGOModelImporter
{

	[Flags]
	public enum EditorListOption
	{
		None = 0,
		ListSize = 1,
		ListLabel = 2,
		ElementLabels = 4,
		Buttons = 8,
		Default = ListSize | ListLabel | ElementLabels,
		NoElementLabels = ListSize | ListLabel,
		All = Default | Buttons
	}

	public static class EditorList
	{

		private static GUIContent
			moveDownButtonContent = new GUIContent("\u2193", "Move Down"),
			duplicateButtonContent = new GUIContent("+", "Duplicate"),
			deleteButtonContent = new GUIContent("-", "Delete"),
			addButtonContent = new GUIContent("+", "Add");

		private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);

		public static void Show(SerializedProperty list, EditorListOption options = EditorListOption.Default, GUIContent[] additionalButtons = null, Action<SerializedProperty>[] additionalButtonActions = null)
		{
			if (!list.isArray)
			{
				EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
				return;
			}

			bool
				showListLabel = (options & EditorListOption.ListLabel) != 0,
				showListSize = (options & EditorListOption.ListSize) != 0;

			if (showListLabel)
			{
				EditorGUILayout.PropertyField(list, false);
				EditorGUI.indentLevel += 1;
			}
			if (!showListLabel || list.isExpanded)
			{
				SerializedProperty size = list.FindPropertyRelative("Array.size");
				if (showListSize)
				{
					EditorGUILayout.PropertyField(size);
				}
				if (size.hasMultipleDifferentValues)
				{
					EditorGUILayout.HelpBox("Not showing lists with different sizes.", MessageType.Info);
				}
				else
				{
					ShowElements(list, options, additionalButtons, additionalButtonActions);
				}
			}
			if (showListLabel)
			{
				EditorGUI.indentLevel -= 1;
			}
		}

		private static void ShowElements(SerializedProperty list, EditorListOption options, GUIContent[] additionalButtons, Action<SerializedProperty>[] additionalButtonActions)
		{
			bool
				showElementLabels = (options & EditorListOption.ElementLabels) != 0,
				showButtons = (options & EditorListOption.Buttons) != 0;

			for (int i = 0; i < list.arraySize; i++)
			{
				if (showButtons)
				{
					EditorGUILayout.BeginHorizontal();
				}
				if (showElementLabels)
				{
					EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
				}
				else
				{
					EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
				}
				if (showButtons)
				{
					ShowButtons(list, i, additionalButtons, additionalButtonActions);
					EditorGUILayout.EndHorizontal();
				}
			}
			if (showButtons && list.arraySize == 0 && GUILayout.Button(addButtonContent, EditorStyles.miniButton))
			{
				list.arraySize += 1;
			}
		}

		private static void ShowButtons(SerializedProperty list, int index, GUIContent[] additionalButtons, Action<SerializedProperty>[] additionalButtonActions)
		{
			bool showAdditionalButtons = false;
			if (additionalButtons != null && additionalButtonActions != null)
			{
				if (additionalButtons.Length == additionalButtonActions.Length)
				{
					showAdditionalButtons = true;
				}
			}

			GUILayout.BeginVertical(miniButtonWidth);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button(moveDownButtonContent, EditorStyles.miniButtonLeft, miniButtonWidth))
			{
				list.MoveArrayElement(index, index + 1);
			}
			if (GUILayout.Button(duplicateButtonContent, EditorStyles.miniButtonMid, miniButtonWidth))
			{
				list.InsertArrayElementAtIndex(index);
			}
			if (GUILayout.Button(deleteButtonContent, EditorStyles.miniButtonRight, miniButtonWidth))
			{
				int oldSize = list.arraySize;
				list.DeleteArrayElementAtIndex(index);
				if (list.arraySize == oldSize)
				{
					list.DeleteArrayElementAtIndex(index);
				}
			}

			GUILayout.EndHorizontal();

			if (showAdditionalButtons)
            {
				GUILayout.BeginHorizontal();

				for (var i = 0; i < additionalButtons.Length; i++)
				{
					if (GUILayout.Button(additionalButtons[i], additionalButtons.Length == 1 ? EditorStyles.miniButton : i == 0 ? EditorStyles.miniButtonLeft : i < additionalButtons.Length - 1 ? EditorStyles.miniButtonMid : EditorStyles.miniButtonRight, miniButtonWidth))
					{
						additionalButtonActions[i].Invoke(list.GetArrayElementAtIndex(index));
					}
				}

				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();

		}
	}

}
