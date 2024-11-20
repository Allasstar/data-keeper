using System.Collections.Generic;
using DataKeeper.Attributes;
using DataKeeper.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DataKeeper.Extra
{
    [CreateAssetMenu(menuName = "DataKeeper/Bootstrap SO", fileName = "Bootstrap SO")]
    public class BootstrapSO : SO
    {
#if UNITY_EDITOR
        [SerializeField] private List<SceneAsset> _bootstrapSceneList = new List<SceneAsset>();
        
        private void OnValidate()
        {
            _bootstrapSceneNameList.Clear();

            foreach (var sceneAsset in _bootstrapSceneList)
            {
                if(sceneAsset == null) continue;
                _bootstrapSceneNameList.Add(sceneAsset.name);
            }

            _initialSceneName = _initialScene == null ? string.Empty : _initialScene.name;
        }
#endif

        [SerializeField, ReadOnlyInspector] private List<string> _bootstrapSceneNameList = new List<string>();
        
        [SerializeField, Space(20)] private bool _loadInitialSceneInEditor = false;
        [SerializeField] private bool _loadInitialSceneAdditive = false;
        
#if UNITY_EDITOR
        [SerializeField] private SceneAsset _initialScene;
#endif
        
        [SerializeField, ReadOnlyInspector] private string _initialSceneName;

        [SerializeField, Space(20)] private List<GameObject> _dontDestroyOnLoadList = new List<GameObject>();

        private int _bootstrapSceneCount = 0;

        public override void Initialize()
        {
            Boot();
            Init();
            InstantiatePrefabs();
        }

        private void Boot()
        {
            if (_bootstrapSceneNameList.Count > 0)
            {
                _bootstrapSceneCount = 0;
                SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;

                foreach (var bootstrapScene in _bootstrapSceneNameList)
                {
                    if(string.IsNullOrEmpty(bootstrapScene)) continue;
                    
                    _bootstrapSceneCount++;
                    SceneManager.LoadScene(bootstrapScene, LoadSceneMode.Additive);
                }
            }
        }
        
        private void Init()
        {
            if (!_loadInitialSceneInEditor || string.IsNullOrEmpty(_initialSceneName)) return;
            
            SceneManager.LoadScene(_initialSceneName, _loadInitialSceneAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }

        private void InstantiatePrefabs()
        {
            foreach (var gameObject in _dontDestroyOnLoadList)
            {
                var go = Instantiate(gameObject);
                go.name = gameObject.name;
                DontDestroyOnLoad(go);
            }
        }
        
        private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_bootstrapSceneNameList.Exists(name => name == scene.name))
            {
                _bootstrapSceneCount--;
                SceneManager.UnloadSceneAsync(scene.name);
            }

            if (_bootstrapSceneCount <= 0)
            {
                SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
            }
        }
    }
}
