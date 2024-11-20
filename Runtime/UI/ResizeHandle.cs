using UnityEngine;
using UnityEngine.EventSystems;

namespace DataKeeper.UI
{
    public class ResizeHandle : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform _targetRect;
        [SerializeField] private Vector2 _minSize;
        [SerializeField] private bool _asLastSibling = true;
        private Vector2 _invertY = new Vector2(1, -1);
        private Vector2 _targetPos;

        private void Awake()
        {
            _targetRect.pivot = Vector2.up;
            FitInScreen();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(_asLastSibling)
                _targetRect.SetAsLastSibling();
            
            _targetPos = _targetRect.position;
        }

        private void FitInScreen()
        {
            var size = _targetRect.sizeDelta;

            size.x = Mathf.Min(Screen.width, size.x);
            size.y = Mathf.Min(Screen.height, size.y);

            _targetRect.sizeDelta = size;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var size = Vector2.Max(_minSize, (eventData.position - _targetPos) * _invertY);
            size.x = Mathf.Min(Screen.width, size.x);
            size.y = Mathf.Min(Screen.height, size.y);
            
            _targetRect.sizeDelta = size;
        }
    }
}
