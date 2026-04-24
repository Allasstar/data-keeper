using DataKeeper.SingletonPattern;

namespace UnityEngine
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_PRINT
    public class DebugPrintSystem : MonoSingleton<DebugPrintSystem>
    {
        public static bool IsEnabled = true;
        
        public void Ping()
        {
        }
        
        private const float BUTTON_WIDTH = 50f;

        private GUIStyle _labelStyle;

        private void Update()
        {
            for (int i = DebugPrint.Messages.Count - 1; i >= 0; i--)
            {
                var msg = DebugPrint.Messages[i];
                
                msg.TimeLeft -= Time.deltaTime;

                if (msg.TimeLeft <= 0f)
                    DebugPrint.Messages.RemoveAt(i);
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

            for (int i = DebugPrint.Messages.Count - 1; i >= 0; i--)
            {
                var msg = DebugPrint.Messages[i];
                
                var maxWidth = CalculateWidth(msg);
                float alpha = CalculateAlpha(msg);

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
                if (msg.HasCopyButton)
                {
                    Rect buttonRect = new Rect(rect.x + 4, rect.y + 2, BUTTON_WIDTH, height - 4);

                    var prevColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, alpha);

                    if (GUI.Button(buttonRect, "Copy"))
                        GUIUtility.systemCopyBuffer = msg.Text;

                    GUI.color = prevColor;

                    contentX += BUTTON_WIDTH + 8f;
                    contentWidth -= BUTTON_WIDTH + 8f;
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

        private float CalculateWidth(Message msg)
        {
            _labelStyle.fontSize = msg.FontSize;
            Vector2 textSize = _labelStyle.CalcSize(new GUIContent(msg.Text));
            float maxAllowedWidth = Screen.safeArea.width - 20f;
            
            var padding = msg.HasCopyButton ? BUTTON_WIDTH + 12f : 12f;

            float maxWidth = Mathf.Min(textSize.x + padding, maxAllowedWidth);
            return maxWidth;
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
    }
    
#endif
}