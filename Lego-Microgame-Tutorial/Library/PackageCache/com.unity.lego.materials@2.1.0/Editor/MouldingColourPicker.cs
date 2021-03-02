// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using UnityEditor;
using System;

namespace LEGOMaterials
{

    public class MouldingColourPicker : EditorWindow
    {
        static Texture2D[] colourTextures;
        static string[] colourNames;
        static Color[] colourLabelColours;
        static bool[] colourTransparent;

        static Action<Color> onColourPicked;
        static bool excludeTransparent;
        static bool excludeBrightYellow;
        static bool excludeLegacy;

        public static MouldingColourPicker Show(Action<Color> colourPicked, bool excludeTransparent = false, bool excludeBrightYellow = false, bool excludeLegacy = true)
        {
            onColourPicked = colourPicked;
            MouldingColourPicker.excludeTransparent = excludeTransparent;
            MouldingColourPicker.excludeBrightYellow = excludeBrightYellow;
            MouldingColourPicker.excludeLegacy = excludeLegacy;

            if (colourTextures == null)
            {
                MouldingColour.Id[] mouldingColourValues = (MouldingColour.Id[])Enum.GetValues(typeof(MouldingColour.Id));
                colourTextures = new Texture2D[mouldingColourValues.Length];
                colourNames = new string[mouldingColourValues.Length];
                colourTransparent = new bool[mouldingColourValues.Length];
                colourLabelColours = new Color[mouldingColourValues.Length];
                for (int i = 0; i < mouldingColourValues.Length; ++i)
                {
                    var colour = MouldingColour.GetColour(mouldingColourValues[i]);
                    var colourTex = new Texture2D(1, 1);
                    colourTex.SetPixel(0, 0, colour);
                    colourTex.Apply();
                    colourTextures[i] = colourTex;
                    colourNames[i] = ObjectNames.NicifyVariableName(mouldingColourValues[i].ToString());
                    colourTransparent[i] = colour.a < 1.0f;
                    colourLabelColours[i] = colour.grayscale > 0.3f ? Color.black : Color.white;
                }
            }

            var window = GetWindow<MouldingColourPicker>(true, "Moulding Colour Picker", true);

            return window;
        }

        private void OnGUI()
        {
            var colourColumnCount = Mathf.FloorToInt((position.width - 3) / (40.0f + 3.0f));

            var colourStyle = new GUIStyle(GUI.skin.button);
            colourStyle.fixedWidth = 40.0f;
            colourStyle.fixedHeight = 40.0f;

            var labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.LowerRight;

            MouldingColour.Id[] mouldingColourValues = (MouldingColour.Id[])Enum.GetValues(typeof(MouldingColour.Id));
            int i = 0;
            int x = 0;
            int y = 0;
            while (i < mouldingColourValues.Length)
            {
                var colourId = mouldingColourValues[i];
                var colour = MouldingColour.GetColour(colourId);

                if (!(excludeTransparent && colour.a < 1.0f) && !(excludeBrightYellow && (colourId == MouldingColour.Id.BrightYellow)) && !(excludeLegacy && MouldingColour.IsLegacy(colourId)))
                {
                    var drawRect = new Rect(x * 43.0f + 3.0f, y * 43.0f + 3.0f, 40.0f, 40.0f);
                    var boxRect = new Rect(drawRect.x - 1.0f, drawRect.y - 1.0f, drawRect.width + 2.0f, drawRect.height + 2.0f);
                    GUI.Box(boxRect, new GUIContent("", colourNames[i]));
                    EditorGUI.DrawTextureTransparent(drawRect, colourTextures[i], ScaleMode.ScaleToFit);
                    labelStyle.normal.textColor = colourLabelColours[i];

                    GUI.Label(drawRect, ((int)colourId).ToString(), labelStyle);

                    // Detect click.
                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (boxRect.Contains(Event.current.mousePosition))
                        {
                            onColourPicked(MouldingColour.GetColour(mouldingColourValues[i]));
                            Close();
                        }
                    }

                    x++;
                    if (x == colourColumnCount)
                    {
                        x = 0;
                        y++;
                    }
                }

                i++;
            }


        }

        private void OnLostFocus()
        {
            Close();
        }
    }

}