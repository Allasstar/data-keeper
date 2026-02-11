using UnityEditor;

namespace DataKeeper.Editor.AndroidPublisher
{
    [InitializeOnLoad]
    public static class AndroidPublisherAutoApply
    {
        private const string Key_AutoApply = "AndroidPublisher_AutoApply";

        static AndroidPublisherAutoApply()
        {
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        private static void ApplyIfNeeded()
        {
            if (!EditorPrefs.GetBool(Key_AutoApply, true))
                return;

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                AndroidPublisherTool.ApplyToPlayerSettings();
            }
        }
    }
}
