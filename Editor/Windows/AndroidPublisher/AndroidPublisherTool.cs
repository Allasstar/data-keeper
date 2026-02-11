using UnityEditor;
using UnityEngine;
using System.IO;

namespace DataKeeper.Editor.AndroidPublisher
{
    public class AndroidPublisherTool : EditorWindow
    {
        private const string Key_KeystorePath = "AndroidPublisher_KeystorePath";
        private const string Key_KeystorePass = "AndroidPublisher_KeystorePass";
        private const string Key_Alias = "AndroidPublisher_Alias";
        private const string Key_AliasPass = "AndroidPublisher_AliasPass";
        private const string Key_AutoApply = "AndroidPublisher_AutoApply";

        private string keystorePath;
        private string keystorePass;
        private string alias;
        private string aliasPass;
        private bool autoApply;

        [MenuItem("Tools/Windows/Android Publisher Settings")]
        public static void Open()
        {
            GetWindow<AndroidPublisherTool>("Android Publisher");
        }

        private void OnEnable()
        {
            keystorePath = EditorPrefs.GetString(Key_KeystorePath, "");
            keystorePass = EditorPrefs.GetString(Key_KeystorePass, "");
            alias = EditorPrefs.GetString(Key_Alias, "");
            aliasPass = EditorPrefs.GetString(Key_AliasPass, "");
            autoApply = EditorPrefs.GetBool(Key_AutoApply, false);
        }

        private void OnGUI()
        {
            GUILayout.Label("Android Publisher Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            keystorePath = EditorGUILayout.TextField("Keystore Path", keystorePath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var path = EditorUtility.OpenFilePanel("Select Keystore", "", "keystore,jks");
                if (!string.IsNullOrEmpty(path))
                    keystorePath = path;
            }
            EditorGUILayout.EndHorizontal();

            keystorePass = EditorGUILayout.PasswordField("Keystore Password", keystorePass);
            alias = EditorGUILayout.TextField("Alias", alias);
            aliasPass = EditorGUILayout.PasswordField("Alias Password", aliasPass);

            autoApply = EditorGUILayout.Toggle("Auto Apply On Load", autoApply);

            GUILayout.Space(10);

            if (GUILayout.Button("Save to EditorPrefs"))
            {
                SavePrefs();
            }

            if (GUILayout.Button("Apply To Player Settings"))
            {
                ApplyToPlayerSettings();
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(Key_KeystorePath, keystorePath);
            EditorPrefs.SetString(Key_KeystorePass, keystorePass);
            EditorPrefs.SetString(Key_Alias, alias);
            EditorPrefs.SetString(Key_AliasPass, aliasPass);
            EditorPrefs.SetBool(Key_AutoApply, autoApply);
        }

        public static void ApplyToPlayerSettings()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                return;

            string path = EditorPrefs.GetString(Key_KeystorePath, "");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            if (!path.EndsWith(".keystore"))
            {
                Debug.LogError("It is not a keystore. Keystore path doesn't end with .keystore");
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = EditorPrefs.GetString(Key_KeystorePass, "");
            PlayerSettings.Android.keyaliasName = EditorPrefs.GetString(Key_Alias, "");
            PlayerSettings.Android.keyaliasPass = EditorPrefs.GetString(Key_AliasPass, "");
        }
    }
}
