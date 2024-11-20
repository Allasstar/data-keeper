using UnityEngine;

namespace DataKeeper.Extra
{
    [DefaultExecutionOrder(-1000)]
    public class DontDestroy : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
