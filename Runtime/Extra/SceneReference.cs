using System;
using DataKeeper.Attributes;
using UnityEngine;

[Serializable]
public class SceneReference
{
    [SerializeField, ReadOnlyInspector] private string _sceneName;

#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset _sceneAsset;
#endif

    public string SceneName => _sceneName;
}
