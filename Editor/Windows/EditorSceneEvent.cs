using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DataKeeper.Editor.Windows
{
    public static class EditorSceneEvent
    {
        private static readonly Dictionary<Action, EventHandlers> _subscribedCallbacks = new Dictionary<Action, EventHandlers>();

        private class EventHandlers
        {
            public EditorSceneManager.SceneDirtiedCallback SceneDirtiedHandler;
            public EditorSceneManager.SceneOpenedCallback SceneOpenedHandler;
            public EditorSceneManager.SceneClosedCallback SceneClosedHandler;
            public EditorSceneManager.SceneSavedCallback SceneSavedHandler;
            public UnityAction<Scene, LoadSceneMode> SceneLoadedHandler;
        }

        public static void SubscribeToEvents(Action callback)
        {
            if (callback == null) return;
            
            // If already subscribed, don't subscribe again
            if (_subscribedCallbacks.ContainsKey(callback)) return;

            var handlers = new EventHandlers
            {
                SceneDirtiedHandler = (scene) => callback?.Invoke(),
                SceneOpenedHandler = (scene, mode) => callback?.Invoke(),
                SceneClosedHandler = (scene) => callback?.Invoke(),
                SceneLoadedHandler = (scene, mode) => callback?.Invoke(),
                SceneSavedHandler = (scene) => callback?.Invoke()
            };

            EditorSceneManager.sceneDirtied += handlers.SceneDirtiedHandler;
            EditorSceneManager.sceneOpened += handlers.SceneOpenedHandler;
            EditorSceneManager.sceneClosed += handlers.SceneClosedHandler;
            SceneManager.sceneLoaded += handlers.SceneLoadedHandler;
            EditorSceneManager.sceneSaved += handlers.SceneSavedHandler;
            EditorBuildSettings.sceneListChanged += callback;

            _subscribedCallbacks[callback] = handlers;
        }

        public static void UnsubscribeFromEvents(Action callback)
        {
            if (callback == null) return;
            
            if (_subscribedCallbacks.TryGetValue(callback, out EventHandlers handlers))
            {
                EditorSceneManager.sceneDirtied -= handlers.SceneDirtiedHandler;
                EditorSceneManager.sceneOpened -= handlers.SceneOpenedHandler;
                EditorSceneManager.sceneClosed -= handlers.SceneClosedHandler;
                SceneManager.sceneLoaded -= handlers.SceneLoadedHandler;
                EditorSceneManager.sceneSaved -= handlers.SceneSavedHandler;
                EditorBuildSettings.sceneListChanged -= callback;

                _subscribedCallbacks.Remove(callback);
            }
        }

        public static void UnsubscribeAll()
        {
            foreach (var kvp in _subscribedCallbacks)
            {
                var callback = kvp.Key;
                var handlers = kvp.Value;

                EditorSceneManager.sceneDirtied -= handlers.SceneDirtiedHandler;
                EditorSceneManager.sceneOpened -= handlers.SceneOpenedHandler;
                EditorSceneManager.sceneClosed -= handlers.SceneClosedHandler;
                SceneManager.sceneLoaded -= handlers.SceneLoadedHandler;
                EditorSceneManager.sceneSaved -= handlers.SceneSavedHandler;
                EditorBuildSettings.sceneListChanged -= callback;
            }

            _subscribedCallbacks.Clear();
        }
    }
}