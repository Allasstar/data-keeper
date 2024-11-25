using System.Collections.Generic;
using DataKeeper.Base;
using DataKeeper.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataKeeper.Extra
{
    [CreateAssetMenu(menuName = "DataKeeper/Bootstrap SO", fileName = "Bootstrap SO")]
    public class BootstrapSO : SO
    {
        [SerializeField] private SceneReference _initialScene;

        [SerializeField, Space(20), Tooltip("Load as Additive and automatically unload.")] private List<SceneReference> _bootstrapSceneList = new List<SceneReference>();
        [SerializeField, Space(20)] private List<GameObject> _dontDestroyOnLoadList = new List<GameObject>();

        public override void Initialize()
        {
            Boot();
            Init();
            InstantiatePrefabs();
        }

        private void Boot()
        {
            foreach (var bootstrapScene in _bootstrapSceneList)
            {
                if(string.IsNullOrEmpty(bootstrapScene.SceneName)) continue;
                
                SceneManager.LoadSceneAsync(bootstrapScene.SceneName, LoadSceneMode.Additive)!
                .completed += a => OnSceneLoadComplete(bootstrapScene.SceneName);
            }
        }
        
        private void OnSceneLoadComplete(string sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
        
        private void Init()
        {
            if(string.IsNullOrEmpty(_initialScene.SceneName)) return;
            SceneManager.LoadScene(_initialScene.SceneName);
        }
        
        private void InstantiatePrefabs()
        {
            var parent = new GameObject("[BootstrapSO]").transform;
            DontDestroyOnLoad(parent);
            
            foreach (var gameObject in _dontDestroyOnLoadList)
            {
                var go = Instantiate(gameObject);
                go.name = gameObject.name;
                go.SetParent(parent);
            }
        }
    }
}
