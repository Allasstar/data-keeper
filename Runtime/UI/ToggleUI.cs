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
        
        [SerializeField]
        private ToggleUIGroup m_Group;

        [field: SerializeField, Space] public Image icon;
        public Optional<ToggleSprite> _iconSprite = new Optional<ToggleSprite>();
        public Optional<ToggleColor> _iconColor = new Optional<ToggleColor>();

        [field: SerializeField, Space] public TextMeshProUGUI label;
        public Optional<ToggleString> _labelText = new Optional<ToggleString>();
        public Optional<ToggleColor> _labelColor = new Optional<ToggleColor>();
        public Optional<ToggleFontStyle> _labelFontStyle = new Optional<ToggleFontStyle>();
        
        [Space]
        public UnityEvent<bool> onValueChanged = new UnityEvent<bool>();
        
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

        /// <summary>
        /// Group the toggle belongs to.
        /// </summary>
        public ToggleUIGroup group
        {
            get { return m_Group; }
            set
            {
                SetToggleGroup(value, true);
                UpdateUI();
            }
        }

        protected ToggleUI()
        {}

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ForceUpdate();
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        public void ForceUpdate()
        {
            SetToggleGroup(m_Group, false);
            if (group != null && IsActive())
            {
                if (isOn || (!group.AnyTogglesOn() && !group.allowSwitchOff))
                {
                    isOn = true;
                    group.NotifyToggleOn(this);
                }
            }
            UpdateUI();
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

        protected override void OnDestroy()
        {
            if (m_Group != null)
                m_Group.EnsureValidState();
            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetToggleGroup(m_Group, false);
            UpdateUI();
        }

        protected override void OnDisable()
        {
            SetToggleGroup(null, false);
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {

        }

        private void SetToggleGroup(ToggleUIGroup newGroup, bool setMemberValue)
        {
            // Sometimes IsActive returns false in OnDisable so don't check for it.
            // Rather remove the toggle too often than too little.
            if (m_Group != null)
                m_Group.UnregisterToggle(this);

            // At runtime the group variable should be set but not when calling this method from OnEnable or OnDisable.
            // That's why we use the setMemberValue parameter.
            if (setMemberValue)
                m_Group = newGroup;

            // Only register to the new group if this Toggle is active.
            if (newGroup != null && IsActive())
                newGroup.RegisterToggle(this);

            // If we are in a new group, and this toggle is on, notify group.
            // Note: Don't refer to m_Group here as it's not guaranteed to have been set.
            if (newGroup != null && isOn && IsActive())
                newGroup.NotifyToggleOn(this);
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
            if (m_Group != null && m_Group.isActiveAndEnabled && IsActive())
            {
                if (m_IsOn || (!m_Group.AnyTogglesOn() && !m_Group.allowSwitchOff))
                {
                    m_IsOn = true;
                    m_Group.NotifyToggleOn(this, sendCallback);
                }
            }

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
        }
    }
}
