using System.Collections.Generic;
using UnityEngine;

namespace DataKeeper.DebugPrinter
{
    public class DebugPrintSystem : MonoBehaviour
    {
        private class Message
        {
            public string Text;

            public float TimeLeft;
            public float Duration;
            public float FadeOutTime;

            public Color Color;
            public int FontSize;
            public bool IsError;

            public bool UseBackground;
            public Color BackgroundColor;
        }

        private static DebugPrintSystem _instance;

        public static DebugPrintSystem Instance
        {
            get
            {
                if (_instance == null)
                    Create();
                return _instance;
            }
        }

        public static bool IsEnabled = true;

        private static void Create()
        {
            var go = new GameObject("[DebugPrintSystem]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugPrintSystem>();
        }

        private readonly List<Message> _messages = new List<Message>(64);

        private const int MaxMessages = 50;

        private GUIStyle _labelStyle;

        public void Add(string text, bool isError, DebugPrintStyle? overrideStyle)
        {
            var style = overrideStyle ?? (isError
                ? DebugPrintStyle.DefaultError
                : DebugPrintStyle.DefaultLog);

            if (_messages.Count >= MaxMessages)
                _messages.RemoveAt(0);

            _messages.Add(new Message
            {
                Text = text,
                Duration = style.Duration,
                FadeOutTime = style.FadeOutTime,
                TimeLeft = style.Duration + style.FadeOutTime,

                Color = style.Color,
                FontSize = style.FontSize,
                IsError = isError,

                UseBackground = style.UseBackground,
                BackgroundColor = style.BackgroundColor,
            });
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                var msg = _messages[i];
                msg.TimeLeft -= dt;

                if (msg.TimeLeft <= 0f)
                    _messages.RemoveAt(i);
            }
        }

        private void OnGUI()
        {
            if (!IsEnabled)
                return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 4, 4),
                    wordWrap = true
                };
            }

            float y = 10f;
            float maxWidth = Mathf.Min(Screen.safeArea.width - 10f, 720f);

            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                var msg = _messages[i];

                float alpha = CalculateAlpha(msg);

                _labelStyle.fontSize = msg.FontSize;

                float textHeight = _labelStyle.CalcHeight(new GUIContent(msg.Text), maxWidth);
                float height = Mathf.Max(textHeight, 24f);

                Rect rect = new Rect(Screen.safeArea.xMin + 10f, Screen.safeArea.yMin + y, maxWidth, height);

                // Background
                if (msg.UseBackground)
                {
                    var prev = GUI.color;
                    var bg = msg.BackgroundColor;
                    GUI.color = new Color(bg.r, bg.g, bg.b, bg.a * alpha);
                    GUI.Box(rect, GUIContent.none);
                    GUI.color = prev;
                }

                float contentX = rect.x;
                float contentWidth = rect.width;

                // Copy button (left)
                if (msg.IsError)
                {
                    float buttonWidth = 50f;

                    Rect buttonRect = new Rect(rect.x + 4, rect.y + 2, buttonWidth, height - 4);

                    var prevColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, alpha);

                    if (GUI.Button(buttonRect, "Copy"))
                        GUIUtility.systemCopyBuffer = msg.Text;

                    GUI.color = prevColor;

                    contentX += buttonWidth + 8f;
                    contentWidth -= buttonWidth + 8f;
                }

                Rect textRect = new Rect(contentX, rect.y, contentWidth, height);

                var textColor = new Color(msg.Color.r, msg.Color.g, msg.Color.b, alpha);

                var preserve = GUI.color;
                GUI.color = textColor;
                GUI.Label(textRect, msg.Text, _labelStyle);
                GUI.color = preserve;

                y += height + 4f;
            }
        }

        private static float CalculateAlpha(Message msg)
        {
            // Still in visible phase
            if (msg.TimeLeft > msg.FadeOutTime)
                return 1f;

            // Fade phase
            if (msg.FadeOutTime <= 0f)
                return 0f;

            return msg.TimeLeft / msg.FadeOutTime;
        }

        private static void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color textColor, Color shadowColor,
            float alpha)
        {
            var prev = GUI.color;

            GUI.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);

            GUI.color = textColor;
            GUI.Label(rect, text, style);

            GUI.color = prev;
        }
    }
}