using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class ColorContrastChecker : EditorWindow
    {
        private Color backgroundColor = Color.white;
        private Color foregroundColor = Color.black;

        private Vector2 scroll;
        private bool showSuggestions = true;

        private enum PreviewMode
        {
            Light,
            Dark,
            Custom
        }

        private PreviewMode previewMode = PreviewMode.Light;
        private Color customPreview = new Color(.15f,.15f,.15f);

        private const float AA_NORMAL = 4.5f;
        private const float AA_LARGE = 3.0f;
        private const float AAA_NORMAL = 7.0f;
        private const float AAA_LARGE = 4.5f;

        [MenuItem("Tools/Windows/Color Contrast Checker", priority = 5)]
        public static void ShowWindow()
        {
            var window = GetWindow<ColorContrastChecker>();
            window.titleContent = new GUIContent("Contrast Checker");
            window.minSize = new Vector2(520, 400);
        }

        void OnGUI()
        {
            GUILayout.Space(8);

            float contrast = CalculateContrast(backgroundColor, foregroundColor);

            DrawStatusBanner(contrast);

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            DrawLeftPanel();

            GUILayout.Space(12);

            DrawRightPanel(contrast);

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            DrawSuggestionsSection(contrast);
        }

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));

            GUILayout.Label("Colors", EditorStyles.boldLabel);

            backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);
            foregroundColor = EditorGUILayout.ColorField("Foreground", foregroundColor);

            GUILayout.Space(8);

            if (GUILayout.Button("Swap Colors"))
            {
                (backgroundColor, foregroundColor) = (foregroundColor, backgroundColor);
            }

            if (GUILayout.Button("Reset"))
            {
                backgroundColor = Color.white;
                foregroundColor = Color.black;
            }

            GUILayout.Space(20);

            GUILayout.Label("Preview Background", EditorStyles.boldLabel);

            previewMode = (PreviewMode)EditorGUILayout.EnumPopup("Mode", previewMode);

            if (previewMode == PreviewMode.Custom)
            {
                customPreview = EditorGUILayout.ColorField("Custom", customPreview);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawRightPanel(float contrast)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            DrawPreview();

            GUILayout.Space(12);

            DrawContrastResult(contrast);

            GUILayout.Space(6);

            DrawWcagResults(contrast);

            EditorGUILayout.EndVertical();
        }

        void DrawStatusBanner(float contrast)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));

            bool pass = contrast >= AA_NORMAL;

            Color color = pass
                ? new Color(.25f, .55f, .25f)
                : new Color(.55f, .25f, .25f);

            EditorGUI.DrawRect(rect, color);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUI.Label(rect,
                pass ? "Accessible Contrast (WCAG AA)" : "Low Contrast",
                style);
        }

        void DrawPreview()
        {
            GUILayout.Label("Preview", EditorStyles.boldLabel);

            Color previewBg =
                previewMode == PreviewMode.Light ? Color.white :
                previewMode == PreviewMode.Dark ? new Color(.07f,.07f,.07f) :
                customPreview;

            Rect rect = GUILayoutUtility.GetRect(0, 90, GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(rect, previewBg);

            Rect textRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20);

            EditorGUI.DrawRect(textRect, backgroundColor);

            GUIStyle text = new GUIStyle(EditorStyles.label);
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.normal.textColor = foregroundColor;

            GUI.Label(textRect, "Sample Text AaBbCc 123", text);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                (backgroundColor, foregroundColor) = (foregroundColor, backgroundColor);
                Repaint();
            }
        }

        void DrawContrastResult(float contrast)
        {
            GUIStyle label = new GUIStyle(EditorStyles.boldLabel);
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;

            GUILayout.Label($"Contrast Ratio\n{contrast:F2}:1", label);
        }

        void DrawWcagResults(float contrast)
        {
            GUILayout.Label("WCAG Compliance", EditorStyles.boldLabel);

            DrawWcagRow("AA Normal (4.5)", contrast >= AA_NORMAL);
            DrawWcagRow("AA Large (3.0)", contrast >= AA_LARGE);
            DrawWcagRow("AAA Normal (7.0)", contrast >= AAA_NORMAL);
            DrawWcagRow("AAA Large (4.5)", contrast >= AAA_LARGE);
        }

        void DrawWcagRow(string label, bool pass)
        {
            Rect r = EditorGUILayout.GetControlRect();

            Color c = pass
                ? new Color(.2f,.8f,.2f)
                : new Color(.8f,.2f,.2f);

            EditorGUI.DrawRect(new Rect(r.x, r.y + 3, 12, 12), c);

            EditorGUI.LabelField(new Rect(r.x + 20, r.y, r.width, r.height), label);
        }

        void DrawSuggestionsSection(float contrast)
        {
            showSuggestions = EditorGUILayout.Foldout(showSuggestions, "Suggestions", true);

            if (!showSuggestions)
                return;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (contrast >= AA_NORMAL)
            {
                EditorGUILayout.HelpBox("Current colors already meet WCAG AA.", MessageType.Info);
            }
            else
            {
                GUILayout.Label("Background Suggestions", EditorStyles.boldLabel);
                DrawSuggestionGrid(GenerateBackgroundSuggestions(), true);

                GUILayout.Space(6);

                GUILayout.Label("Foreground Suggestions", EditorStyles.boldLabel);
                DrawSuggestionGrid(GenerateForegroundSuggestions(), false);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawSuggestionGrid(List<Color> colors, bool background)
        {
            int columns = 8;
            int index = 0;

            GUIStyle transparentButton = new GUIStyle(GUI.skin.button);
            transparentButton.normal.background = Texture2D.whiteTexture;
            transparentButton.border = new RectOffset(0,0,0,0);

            while (index < colors.Count)
            {
                EditorGUILayout.BeginHorizontal();

                for (int i = 0; i < columns && index < colors.Count; i++)
                {
                    Color c = colors[index++];

                    Rect r = GUILayoutUtility.GetRect(28, 28);

                    // draw color
                    EditorGUI.DrawRect(r, c);

                    // invisible clickable button
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    {
                        if (background)
                            backgroundColor = c;
                        else
                            foregroundColor = c;

                        Repaint();
                    }

                    // draw border so colors are visible on white
                    EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), Color.black);
                    EditorGUI.DrawRect(new Rect(r.x, r.yMax-1, r.width, 1), Color.black);
                    EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), Color.black);
                    EditorGUI.DrawRect(new Rect(r.xMax-1, r.y, 1, r.height), Color.black);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        List<Color> GenerateBackgroundSuggestions()
        {
            List<Color> list = new List<Color>();

            for (float f = .2f; f <= 1.8f; f += .2f)
            {
                if (Mathf.Approximately(f,1)) continue;

                Color c = new Color(
                    Mathf.Clamp01(backgroundColor.r * f),
                    Mathf.Clamp01(backgroundColor.g * f),
                    Mathf.Clamp01(backgroundColor.b * f));

                if (CalculateContrast(c, foregroundColor) >= AA_NORMAL)
                    list.Add(c);
            }

            list.AddRange(new[]
            {
                Color.white,
                Color.black,
                Color.gray,
                new Color(.1f,.1f,.1f),
                new Color(.9f,.9f,.9f)
            });

            return list.Distinct().Take(16).ToList();
        }

        List<Color> GenerateForegroundSuggestions()
        {
            List<Color> list = new List<Color>();

            for (float f = .2f; f <= 1.8f; f += .2f)
            {
                if (Mathf.Approximately(f,1)) continue;

                Color c = new Color(
                    Mathf.Clamp01(foregroundColor.r * f),
                    Mathf.Clamp01(foregroundColor.g * f),
                    Mathf.Clamp01(foregroundColor.b * f));

                if (CalculateContrast(backgroundColor, c) >= AA_NORMAL)
                    list.Add(c);
            }

            list.AddRange(new[]
            {
                Color.black,
                Color.white,
                new Color(.1f,.1f,.1f),
                new Color(.9f,.9f,.9f)
            });

            return list.Distinct().Take(16).ToList();
        }

        float CalculateContrast(Color bg, Color fg)
        {
            float bgL = GetRelativeLuminance(bg);
            float fgL = GetRelativeLuminance(fg);

            float lighter = Mathf.Max(bgL, fgL);
            float darker = Mathf.Min(bgL, fgL);

            return (lighter + 0.05f) / (darker + 0.05f);
        }

        float GetRelativeLuminance(Color c)
        {
            float r = c.r <= 0.03928f ? c.r / 12.92f : Mathf.Pow((c.r + 0.055f) / 1.055f, 2.4f);
            float g = c.g <= 0.03928f ? c.g / 12.92f : Mathf.Pow((c.g + 0.055f) / 1.055f, 2.4f);
            float b = c.b <= 0.03928f ? c.b / 12.92f : Mathf.Pow((c.b + 0.055f) / 1.055f, 2.4f);

            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }
    }
}