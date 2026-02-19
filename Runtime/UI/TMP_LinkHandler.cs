using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DataKeeper.UI
{
    /// <summary>
    /// Example: Open <link="https://unity.com"><u>Unity</u></link>
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMP_LinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _tmpText;

        void Awake()
        {
            _tmpText = GetComponent<TMP_Text>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                _tmpText,
                eventData.position,
                eventData.pressEventCamera
            );

            if (linkIndex < 0) return;
            TMP_LinkInfo linkInfo = _tmpText.textInfo.linkInfo[linkIndex];
            string url = linkInfo.GetLinkID();

            Application.OpenURL(url);
        }
    }
}