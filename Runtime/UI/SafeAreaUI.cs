using UnityEngine;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Safe Area UI")]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaUI : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void FixedUpdate()
        {
            // Only apply the safe area if the screen size or orientation changes
            if (_lastSafeArea != Screen.safeArea 
                || _lastScreenSize.x != Screen.width 
                || _lastScreenSize.y != Screen.height 
                || _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            var safeArea = Screen.safeArea;

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;

            // Store the current values
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;
        }
    }
}