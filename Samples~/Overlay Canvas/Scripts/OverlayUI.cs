using DataKeeper.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Overlay
{
    public class OverlayUI : MonoBehaviour
    {
        [field: SerializeField] public Image BackgroundImage { get; private set; }
        [field: SerializeField] public GameObject SpinnerRoot { get; private set; }
        [field: SerializeField] public Image SpinnerImage { get; private set; }
        [field: SerializeField] public TMP_Text LabelText { get; private set; }

        public void SetEnableBackground(bool isEnabled)
        {
            BackgroundImage.SetGameObjectActive(isEnabled);
        }
        
        public void SetEnableSpinner(bool isEnabled)
        {
            SpinnerRoot.SetActive(isEnabled);
        }
        
        public void SetEnableLabel(bool isEnabled)
        {
            LabelText.SetGameObjectActive(isEnabled);
        }

        public void SetLabelText(string text)
        {
            LabelText.text = text;
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
