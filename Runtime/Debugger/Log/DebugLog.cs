using System.IO;
using System.Runtime.CompilerServices;
using DataKeeper.Utility;

namespace UnityEngine
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_LOG
    
    public static class DebugLog
    {
        [HideInCallstack]
        public static void Log(string message, Object context = null, [CallerFilePath] string file = "")
        {
            var className = Path.GetFileNameWithoutExtension(file);
            Debug.Log($"{RichText.Text(className).SquareBrackets().Bold().Color(RichText.TextToHexColor(className)).ToString()} {message}", context);
        }

        [HideInCallstack]
        public static void Error(string message, Object context = null, [CallerFilePath] string file = "")
        {
            var className = Path.GetFileNameWithoutExtension(file);
            Debug.LogError($"{RichText.Text(className).SquareBrackets().Bold().Color(RichText.TextToHexColor(className)).ToString()} {message}", context);
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
