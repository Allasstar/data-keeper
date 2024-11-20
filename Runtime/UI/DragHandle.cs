using UnityEngine;
using UnityEngine.EventSystems;

namespace DataKeeper.UI
{
    public class DragHandle : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform _targetRect;
        [SerializeField] private bool _asLastSibling = true;
        private Vector2 _delta;

        public void OnPointerDown(PointerEventData eventData)
        {
            if(_asLastSibling)
                _targetRect.SetAsLastSibling();
            
            _delta = (Vector2)_targetRect.position - eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _targetRect.position = eventData.position + _delta;
        }
    }
}
