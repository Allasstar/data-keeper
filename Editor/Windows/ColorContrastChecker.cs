using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows
{
    public class ColorContrastChecker : EditorWindow
    {
        private Color backgroundColor = Color.white;
        private Color foregroundColor = Color.black;
        private Vector2 scrollPosition;
    
        // Contrast thresholds (WCAG standards)
        private const float AA_NORMAL = 4.5f;
        private const float AA_LARGE = 3.0f;
        private const float AAA_NORMAL = 7.0f;
        private const float AAA_LARGE = 4.5f;
    
        [MenuItem("Tools/Windows/Color Contrast Checker", priority = 5)]
        public static void ShowWindow()
        {
            GetWindow<ColorContrastChecker>("Color Contrast Checker");
        }
    
        private void OnGUI()
        {
            GUILayout.Space(10);
        
            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Color Contrast Checker", titleStyle);
        
            GUILayout.Space(10);
        
            // Color Pickers
            EditorGUILayout.BeginHorizontal();
        
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Background Color", EditorStyles.boldLabel);
            backgroundColor = EditorGUILayout.ColorField(backgroundColor);
            EditorGUILayout.EndVertical();
        
            GUILayout.Space(20);
        
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Foreground Color", EditorStyles.boldLabel);
            foregroundColor = EditorGUILayout.ColorField(foregroundColor);
            EditorGUILayout.EndVertical();
        
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(10);
        
            // Preview Section
            DrawPreviewSection();
        
            GUILayout.Space(10);
        
            // Contrast Results
            float contrast = CalculateContrast(backgroundColor, foregroundColor);
            DrawContrastResults(contrast);
        
            GUILayout.Space(10);
        
            // Suggestions
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawSuggestions();
            EditorGUILayout.EndScrollView();
        }
    
        private void DrawPreviewSection()
        {
            GUILayout.Label("Preview", EditorStyles.boldLabel);
        
            // Create a rect for the preview
            Rect previewRect = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
        
            // Draw background
            EditorGUI.DrawRect(previewRect, backgroundColor);
        
            // Draw text preview
            GUIStyle textStyle = new GUIStyle(GUI.skin.label);
            textStyle.normal.textColor = foregroundColor;
            textStyle.fontSize = 14;
            textStyle.alignment = TextAnchor.MiddleCenter;
        
            GUI.Label(previewRect, "Sample Text Preview\nAaBbCc 123", textStyle);
        
            // Draw border
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y + previewRect.height - 1, previewRect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, 1, previewRect.height), Color.gray);
            EditorGUI.DrawRect(new Rect(previewRect.x + previewRect.width - 1, previewRect.y, 1, previewRect.height), Color.gray);
        }
    
        private void DrawContrastResults(float contrast)
        {
            GUILayout.Label("Contrast Analysis", EditorStyles.boldLabel);
        
            // Contrast ratio
            GUILayout.Label($"Contrast Ratio: {contrast:F2}:1");
        
            // WCAG compliance
            GUIStyle passStyle = new GUIStyle(GUI.skin.label);
            GUIStyle failStyle = new GUIStyle(GUI.skin.label);
            passStyle.normal.textColor = Color.green;
            failStyle.normal.textColor = Color.red;
        
            GUILayout.Label($"AA Normal Text (4.5:1): {(contrast >= AA_NORMAL ? "PASS" : "FAIL")}", 
                contrast >= AA_NORMAL ? passStyle : failStyle);
            GUILayout.Label($"AA Large Text (3.0:1): {(contrast >= AA_LARGE ? "PASS" : "FAIL")}", 
                contrast >= AA_LARGE ? passStyle : failStyle);
            GUILayout.Label($"AAA Normal Text (7.0:1): {(contrast >= AAA_NORMAL ? "PASS" : "FAIL")}", 
                contrast >= AAA_NORMAL ? passStyle : failStyle);
            GUILayout.Label($"AAA Large Text (4.5:1): {(contrast >= AAA_LARGE ? "PASS" : "FAIL")}", 
                contrast >= AAA_LARGE ? passStyle : failStyle);
        }
    
        private void DrawSuggestions()
        {
            GUILayout.Label("Suggested Improvements", EditorStyles.boldLabel);
        
            float currentContrast = CalculateContrast(backgroundColor, foregroundColor);
        
            if (currentContrast < AA_NORMAL)
            {
                GUILayout.Label("Background Suggestions:", EditorStyles.boldLabel);
                DrawColorSuggestions(GenerateBackgroundSuggestions(), true);
            
                GUILayout.Space(5);
            
                GUILayout.Label("Foreground Suggestions:", EditorStyles.boldLabel);
                DrawColorSuggestions(GenerateForegroundSuggestions(), false);
            }
            else
            {
                GUILayout.Label("Current colors meet accessibility standards!", EditorStyles.helpBox);
            }
        }
    
        private void DrawColorSuggestions(List<Color> suggestions, bool isBackground)
        {
            int itemsPerRow = 4;
            for (int i = 0; i < suggestions.Count; i += itemsPerRow)
            {
                EditorGUILayout.BeginHorizontal();
            
                for (int j = 0; j < itemsPerRow && i + j < suggestions.Count; j++)
                {
                    Color suggestedColor = suggestions[i + j];
                    Color testBg = isBackground ? suggestedColor : backgroundColor;
                    Color testFg = isBackground ? foregroundColor : suggestedColor;
                    float contrast = CalculateContrast(testBg, testFg);
                
                    EditorGUILayout.BeginVertical();
                
                    // Color swatch
                    Rect swatchRect = GUILayoutUtility.GetRect(60, 40);
                    EditorGUI.DrawRect(swatchRect, suggestedColor);
                
                    // Mini preview
                    GUIStyle miniTextStyle = new GUIStyle(GUI.skin.label);
                    miniTextStyle.normal.textColor = isBackground ? foregroundColor : suggestedColor;
                    miniTextStyle.fontSize = 8;
                    miniTextStyle.alignment = TextAnchor.MiddleCenter;
                
                    Rect miniPreviewRect = GUILayoutUtility.GetRect(60, 20);
                    EditorGUI.DrawRect(miniPreviewRect, isBackground ? suggestedColor : backgroundColor);
                    GUI.Label(miniPreviewRect, "Text", miniTextStyle);
                
                    // Contrast info
                    GUILayout.Label($"{contrast:F1}:1", EditorStyles.miniLabel);
                
                    // Use button
                    if (GUILayout.Button("Use", GUILayout.Width(60)))
                    {
                        if (isBackground)
                            backgroundColor = suggestedColor;
                        else
                            foregroundColor = suggestedColor;
                        Repaint();
                    }
                
                    EditorGUILayout.EndVertical();
                }
            
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }
    
        private List<Color> GenerateBackgroundSuggestions()
        {
            List<Color> suggestions = new List<Color>();
        
            // Generate lighter and darker versions of current background
            for (float factor = 0.2f; factor <= 1.8f; factor += 0.2f)
            {
                if (Mathf.Approximately(factor, 1.0f)) continue; // Skip current color
            
                Color suggested = new Color(
                    Mathf.Clamp01(backgroundColor.r * factor),
                    Mathf.Clamp01(backgroundColor.g * factor),
                    Mathf.Clamp01(backgroundColor.b * factor),
                    backgroundColor.a
                );
            
                if (CalculateContrast(suggested, foregroundColor) >= AA_NORMAL)
                {
                    suggestions.Add(suggested);
                }
            }
        
            // Add some standard safe colors
            Color[] standardColors = {
                Color.white, Color.black, Color.gray,
                new Color(0.95f, 0.95f, 0.95f), // Light gray
                new Color(0.1f, 0.1f, 0.1f), // Dark gray
                new Color(0.2f, 0.3f, 0.4f), // Dark blue-gray
                new Color(0.9f, 0.9f, 0.85f)  // Cream
            };
        
            foreach (Color color in standardColors)
            {
                if (CalculateContrast(color, foregroundColor) >= AA_NORMAL)
                {
                    suggestions.Add(color);
                }
            }
        
            return suggestions;
        }
    
        private List<Color> GenerateForegroundSuggestions()
        {
            List<Color> suggestions = new List<Color>();
        
            // Generate variations of current foreground
            for (float factor = 0.2f; factor <= 1.8f; factor += 0.2f)
            {
                if (Mathf.Approximately(factor, 1.0f)) continue; // Skip current color
            
                Color suggested = new Color(
                    Mathf.Clamp01(foregroundColor.r * factor),
                    Mathf.Clamp01(foregroundColor.g * factor),
                    Mathf.Clamp01(foregroundColor.b * factor),
                    foregroundColor.a
                );
            
                if (CalculateContrast(backgroundColor, suggested) >= AA_NORMAL)
                {
                    suggestions.Add(suggested);
                }
            }
        
            // Add standard safe text colors
            Color[] standardColors = {
                Color.black, Color.white,
                new Color(0.1f, 0.1f, 0.1f), // Very dark gray
                new Color(0.9f, 0.9f, 0.9f), // Very light gray
                new Color(0.2f, 0.2f, 0.2f), // Dark gray
                new Color(0.0f, 0.0f, 0.5f), // Dark blue
                new Color(0.5f, 0.0f, 0.0f)  // Dark red
            };
        
            foreach (Color color in standardColors)
            {
                if (CalculateContrast(backgroundColor, color) >= AA_NORMAL)
                {
                    suggestions.Add(color);
                }
            }
        
            return suggestions;
        }
    
        private float CalculateContrast(Color bg, Color fg)
        {
            float bgLuminance = GetRelativeLuminance(bg);
            float fgLuminance = GetRelativeLuminance(fg);
        
            float lighter = Mathf.Max(bgLuminance, fgLuminance);
            float darker = Mathf.Min(bgLuminance, fgLuminance);
        
            return (lighter + 0.05f) / (darker + 0.05f);
        }
    
        private float GetRelativeLuminance(Color color)
        {
            // Convert to linear RGB
            float r = color.r <= 0.03928f ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f);
            float g = color.g <= 0.03928f ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f);
            float b = color.b <= 0.03928f ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f);
        
            // Calculate luminance using the formula: 0.2126 * R + 0.7152 * G + 0.0722 * B
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }
    }
}