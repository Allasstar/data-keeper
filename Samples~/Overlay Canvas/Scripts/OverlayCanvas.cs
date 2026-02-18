using DataKeeper.Extensions;
using DataKeeper.PoolSystem;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Overlay
{
    [DefaultExecutionOrder(-9000)]
    public class OverlayCanvas : MonoBehaviour
    {
        [field: SerializeField] public Image CanvasBackgroundImage { get; private set; }
        [SerializeField] private Pool<OverlayUI> _overlayPool = new Pool<OverlayUI>();
        
        private void Awake()
        {
            _overlayPool.Initialize();
            UpdateBackground();
        }

        public void UpdateBackground()
        {
            CanvasBackgroundImage.SetGameObjectActive(_overlayPool.GetAllActive().Count > 0);
        }

        public void HideAll()
        {
            _overlayPool.ReleaseAll();
            UpdateBackground();
        }

        public OverlayUI ShowOverlay()
        {
            var overlayUI = _overlayPool.Get();
            overlayUI.transform.SetAsLastSibling();
            UpdateBackground();
            return overlayUI;
        }

        public void HideOverlay(OverlayUI overlayUI)
        {
            _overlayPool.Release(overlayUI);
            UpdateBackground();
        }
    }
}