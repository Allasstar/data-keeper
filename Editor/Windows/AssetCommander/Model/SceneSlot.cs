using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public enum SceneBinding
    {
        None = 0,
        Live = 1,
        Preview = 2,
    }

    // Owns how a side gets at a scene's objects. A scene the editor already has open is used
    // live — its objects are the ones the Hierarchy shows, and editing them is normal editing.
    // A closed scene is loaded with OpenPreviewScene instead: real GameObjects and Components
    // through the real APIs, but outside the editor's scene list, so browsing a scene never
    // disturbs what the user has open. Preview-backed sides are read-only until promoted.
    public sealed class SceneSlot : IDisposable
    {
        public string ScenePath { get; private set; } = "";
        public SceneBinding Binding { get; private set; }
        public Scene Scene { get; private set; }

        public bool IsValid => Scene.IsValid() && Scene.isLoaded;
        public bool IsPreview => Binding == SceneBinding.Preview;

        public string SceneName
        {
            get
            {
                if (string.IsNullOrEmpty(ScenePath)) return "";

                int slash = ScenePath.LastIndexOf('/');
                var file = slash < 0 ? ScenePath : ScenePath.Substring(slash + 1);

                return file.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                    ? file.Substring(0, file.Length - 6)
                    : file;
            }
        }

        public void Bind(string scenePath)
        {
            if (ScenePath == scenePath && IsValid) return;

            Release();
            ScenePath = scenePath ?? "";
            Rebind();
        }

        // Re-resolves the same path: the scene may have been opened, closed or reloaded behind
        // the window's back, which flips a side between live and preview.
        public void Rebind()
        {
            if (string.IsNullOrEmpty(ScenePath)) return;

            var live = FindLoaded(ScenePath);
            if (live.IsValid())
            {
                ReleasePreview();
                Scene = live;
                Binding = SceneBinding.Live;
                return;
            }

            if (IsPreview && IsValid) return;

            // The scene file can be deleted while a side is still pointed at it; opening a
            // preview of a path that is gone throws rather than returning an invalid Scene.
            if (!AssetDatabase.AssetPathExists(ScenePath))
            {
                Release();
                return;
            }

            // A preview scene cannot survive the play-mode reload, and opening one while the
            // player is running would fight the runtime scene manager.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Release();
                return;
            }

            Scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            Binding = Scene.IsValid() ? SceneBinding.Preview : SceneBinding.None;
        }

        // Opens a preview-backed side for real, additively. Phase 6's mutating commands call
        // this before touching anything: a preview scene is never dirty, so nothing is lost by
        // closing it and reopening the file.
        public bool PromoteToOpen(bool prompt)
        {
            if (Binding == SceneBinding.Live) return true;
            if (string.IsNullOrEmpty(ScenePath)) return false;

            if (prompt && !EditorUtility.DisplayDialog("Open Scene",
                    $"'{SceneName}' is loaded for preview only and cannot be edited.\n\n" +
                    "Open it in the Hierarchy?", "Open", "Cancel"))
                return false;

            Release();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Rebind();

            return Binding == SceneBinding.Live;
        }

        public void Release()
        {
            ReleasePreview();
            Scene = default;
            Binding = SceneBinding.None;
        }

        public void Dispose() => Release();

        private void ReleasePreview()
        {
            if (Binding != SceneBinding.Preview || !Scene.IsValid()) return;

            EditorSceneManager.ClosePreviewScene(Scene);
            Scene = default;
            Binding = SceneBinding.None;
        }

        private static Scene FindLoaded(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.path == scenePath) return scene;
            }

            return default;
        }
    }
}
