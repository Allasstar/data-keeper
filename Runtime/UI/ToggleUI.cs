using System;
using DataKeeper.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Toggle UI")]
    [RequireComponent(typeof(RectTransform))]
    public class ToggleUI : SelectableUI, IPointerClickHandler, ISubmitHandler, ICanvasElement
    {
        
        // Whether the toggle is on
        [Tooltip("Is the toggle currently on or off?")]
        [SerializeField, Space]
        private bool m_IsOn;
        
        [field: SerializeField, Space] public Image icon;
        public Optional<ToggleSprite> _iconSprite = new Optional<ToggleSprite>();
        public Optional<ToggleColor> _iconColor = new Optional<ToggleColor>();

        [field: SerializeField, Space] public TextMeshProUGUI label;
        public Optional<ToggleString> _labelText = new Optional<ToggleString>();
        public Optional<ToggleColor> _labelColor = new Optional<ToggleColor>();
        public Optional<ToggleFontStyle> _labelFontStyle = new Optional<ToggleFontStyle>();
        
        [Space]
        public UnityEvent<bool> onValueChanged = new UnityEvent<bool>();
        public UnityEvent onBecameInteractable   = new UnityEvent();
        public UnityEvent onBecameNonInteractable = new UnityEvent();
        
        // Track last known interactable state to detect transitions.
        private bool _wasInteractable;
        
        public void UpdateUI()
        {
            if (icon != null)
            {
                if (_iconSprite.Enabled)
                {
                    icon.sprite = m_IsOn ? _iconSprite.Value.On : _iconSprite.Value.Off;
                }
                
                if (_iconColor.Enabled)
                {
                    icon.color = m_IsOn ? _iconColor.Value.On : _iconColor.Value.Off;
                }
            }
            
            if (label == null) return;
            
            if (_labelColor.Enabled)
            {
                label.color = m_IsOn ? _labelColor.Value.On : _labelColor.Value.Off;
            }
            
            if (_labelFontStyle.Enabled)
            {
                label.fontStyle = m_IsOn ? _labelFontStyle.Value.On : _labelFontStyle.Value.Off;
            }
            
            if (_labelText.Enabled)
            {
                label.text = m_IsOn ? _labelText.Value.On : _labelText.Value.Off;
            }
        }

        protected ToggleUI()
        {}

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateUI();
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(m_IsOn);
#endif
        }

        public virtual void LayoutComplete()
        {}

        public virtual void GraphicUpdateComplete()
        {}

        protected override void OnEnable()
        {
            base.OnEnable();
            _wasInteractable = IsInteractable();
            UpdateUI();
        }
        
        // DoStateTransition is called by Unity every time interactable,
        // highlight, press, or selection state changes — it's our only
        // reliable hook into interactable changes without polling.
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant); // keep SelectableUI color/sprite logic
 
            bool nowInteractable = IsInteractable();
            if (nowInteractable == _wasInteractable) return;
 
            _wasInteractable = nowInteractable;
            if (nowInteractable)
                onBecameInteractable.Invoke();
            else
                onBecameNonInteractable.Invoke();
        }

        protected override void OnDidApplyAnimationProperties()
        {

        }

        public void SetOnText(string onText)
        {
            _labelText.Value.SetOnText(onText);
            UpdateUI();
        }

        public void SetOffText(string offText)
        {
            _labelText.Value.SetOffText(offText);
            UpdateUI();
        }

        /// <summary>
        /// Whether the toggle is currently active.
        /// </summary>
        public bool isOn
        {
            get { return m_IsOn; }

            set
            {
                Set(value);
            }
        }

        /// <summary>
        /// Set isOn without invoking onValueChanged callback.
        /// </summary>
        /// <param name="value">New Value for isOn.</param>
        public void SetIsOnWithoutNotify(bool value)
        {
            Set(value, false);
        }

        public void Set(bool value, bool sendCallback = true)
        {
            if (m_IsOn == value)
                return;

            // if we are in a group and set to true, do group logic
            m_IsOn = value;

            // Always send event when toggle is clicked, even if value didn't change
            // due to already active toggle in a toggle group being clicked.
            // Controls like Dropdown rely on this.
            // It's up to the user to ignore a selection being set to the same value it already was, if desired.
            UpdateUI();
            if (sendCallback)
            {
                UISystemProfilerApi.AddMarker("ToggleUI.value", this);
                onValueChanged.Invoke(m_IsOn);
            }
        }

        /// <summary>
        /// Assume the correct visual state.
        /// </summary>
        protected override void Start()
        {
            UpdateUI();
        }

        private void InternalToggle()
        {
            if (!IsActive() || !IsInteractable())
                return;

            isOn = !isOn;
        }

        /// <summary>
        /// React to clicks.
        /// </summary>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            InternalToggle();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            InternalToggle();
        }
        
        [Serializable]
        public class ToggleSprite
        {
            [field: SerializeField] public Sprite On { private set; get; }
            [field: SerializeField] public Sprite Off { private set; get; }
        }
       
        [Serializable]
        public class ToggleColor
        {
            [field: SerializeField] public Color On { private set; get; } = Color.green;
            [field: SerializeField] public Color Off { private set; get; } = Color.red;
        }

        [Serializable]
        public class ToggleFontStyle
        {
            [field: SerializeField] public TMPro.FontStyles On { private set; get; } = TMPro.FontStyles.Bold;
            [field: SerializeField] public TMPro.FontStyles Off { private set; get; } = TMPro.FontStyles.Normal;
        }
        
        [Serializable]
        public class ToggleString
        {
            [field: SerializeField] public string On { private set; get; } = "On";
            [field: SerializeField] public string Off { private set; get; } = "Off";

            public void SetOnText(string onText)
            {
                On = onText;
            }
            
            public void SetOffText(string offText)
            {
                Off = offText;
            }
        }
    }
}
