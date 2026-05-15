using System.IO;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_LOG
    
    public static class DebugLog
    {
        private static string GetColor(string value)
        {
            var hue = (uint)value.GetHashCode() / (float)uint.MaxValue;
            var color = Color.HSVToRGB(hue, 0.6f, 1f);
            return ColorUtility.ToHtmlStringRGB(color);
        }
        
        [HideInCallstack]
        public static void Log(string message, Object context = null, [CallerFilePath] string file = "")
        {
            var className = Path.GetFileNameWithoutExtension(file);
            var color = GetColor(className);
            Debug.Log($"<color=#{color}><b>[{className}]</b></color> {message}", context);
        }

        [HideInCallstack]
        public static void Error(string message, Object context = null, [CallerFilePath] string file = "")
        {
            var className = Path.GetFileNameWithoutExtension(file);
            var color = GetColor(className);
            Debug.LogError($"<color=#{color}><b>[{className}]</b></color> {message}", context);
        }
    }

#else
    public static class DebugLog
    {
        public static void Log(string message, Object context = null, string file = "")
        {
        }

        public static void Error(string message, Object context = null, string file = "")
        {
        }
    }
#endif
}
