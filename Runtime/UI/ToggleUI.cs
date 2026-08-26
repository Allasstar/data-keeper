using System;
using DataKeeper.Attributes;
using DataKeeper.Generic;
using DataKeeper.ValueProviders;
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
        private bool _providersBound;
        
        public void UpdateUI()
        {
            if (icon != null)
            {
                if (_iconSprite.Enabled)
                {
                    icon.sprite = _iconSprite.Value.Get(m_IsOn);
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
                label.text = _labelText.Value.Get(m_IsOn);
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
            BindProviders();
            UpdateUI();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnbindProviders();
        }

        // Providers that resolve on their own (a localized entry, a blackboard value) push a
        // repaint instead of waiting for the next Set() call.
        private void BindProviders()
        {
            if (_providersBound) return;

            if (_labelText.Enabled) _labelText.Value.Bind(UpdateUI);
            if (_iconSprite.Enabled) _iconSprite.Value.Bind(UpdateUI);
            _providersBound = true;
        }

        // Swapping a provider at runtime has to go through Unbind/Bind: the handler must be
        // released from the instance it was registered on, or a localized entry keeps this
        // component alive through LocalizationSettings' static locale event.
        private void UnbindProviders()
        {
            if (!_providersBound) return;

            if (_labelText.Enabled) _labelText.Value.Unbind(UpdateUI);
            if (_iconSprite.Enabled) _iconSprite.Value.Unbind(UpdateUI);
            _providersBound = false;
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
            bool wasBound = _providersBound;
            UnbindProviders();

            _labelText.Value.SetOnText(onText);

            if (wasBound) BindProviders();
            UpdateUI();
        }

        public void SetOffText(string offText)
        {
            bool wasBound = _providersBound;
            UnbindProviders();

            _labelText.Value.SetOffText(offText);

            if (wasBound) BindProviders();
            UpdateUI();
        }

        public void SetOnSprite(Sprite onSprite)
        {
            bool wasBound = _providersBound;
            UnbindProviders();

            _iconSprite.Value.SetOnSprite(onSprite);

            if (wasBound) BindProviders();
            UpdateUI();
        }

        public void SetOffSprite(Sprite offSprite)
        {
            bool wasBound = _providersBound;
            UnbindProviders();

            _iconSprite.Value.SetOffSprite(offSprite);

            if (wasBound) BindProviders();
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
        public class ToggleSprite : ISerializationCallbackReceiver
        {
            [SerializeReference, SerializeReferenceSelector] private ISpriteProvider _on;
            [SerializeReference, SerializeReferenceSelector] private ISpriteProvider _off;

            // Pre-provider layout. Kept as auto-properties so the serialized names stay
            // <On>k__BackingField / <Off>k__BackingField and existing prefabs still load their
            // authored sprites, which OnAfterDeserialize turns into direct providers.
            // Safe to delete once every prefab has been re-saved.
            [field: SerializeField, HideInInspector] private Sprite On { get; set; }
            [field: SerializeField, HideInInspector] private Sprite Off { get; set; }

            public Sprite OnSprite => _on?.GetValue();
            public Sprite OffSprite => _off?.GetValue();

            public Sprite Get(bool isOn) => isOn ? OnSprite : OffSprite;

            public void SetOnSprite(Sprite onSprite) => _on = AsDirect(_on, onSprite);
            public void SetOffSprite(Sprite offSprite) => _off = AsDirect(_off, offSprite);

            public void Bind(Action onValueChanged)
            {
                _on.Bind(onValueChanged);
                _off.Bind(onValueChanged);
            }

            public void Unbind(Action onValueChanged)
            {
                _on.Unbind(onValueChanged);
                _off.Unbind(onValueChanged);
            }

            public void OnBeforeSerialize() { }

            // The legacy sprite is copied by reference, never compared: deserialization can run
            // off the main thread, where the UnityEngine.Object equality operator is not safe.
            public void OnAfterDeserialize()
            {
                _on ??= new SpriteDirectProvider { target = On };
                _off ??= new SpriteDirectProvider { target = Off };
            }

            private static ISpriteProvider AsDirect(ISpriteProvider provider, Sprite sprite)
            {
                if (provider is SpriteDirectProvider direct)
                {
                    direct.target = sprite;
                    return direct;
                }

                return new SpriteDirectProvider { target = sprite };
            }
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
        public class ToggleString : ISerializationCallbackReceiver
        {
            // No field initializers: a serialized object that predates these fields keeps whatever
            // the constructor left, so a default here would hide the legacy values from the
            // migration below. OnAfterDeserialize seeds them instead.
            [SerializeReference, SerializeReferenceSelector] private IStringProvider _on;
            [SerializeReference, SerializeReferenceSelector] private IStringProvider _off;

            // Pre-provider layout. Kept as auto-properties so the serialized names stay
            // <On>k__BackingField / <Off>k__BackingField and existing prefabs still load their
            // authored text, which OnAfterDeserialize turns into constant providers.
            // Safe to delete once every prefab has been re-saved.
            [field: SerializeField, HideInInspector] private string On { get; set; }
            [field: SerializeField, HideInInspector] private string Off { get; set; }

            public string OnText => _on?.GetValue();
            public string OffText => _off?.GetValue();

            public string Get(bool isOn) => isOn ? OnText : OffText;

            public void SetOnText(string onText) => _on = AsConstant(_on, onText);
            public void SetOffText(string offText) => _off = AsConstant(_off, offText);

            public void Bind(Action onValueChanged)
            {
                _on.Bind(onValueChanged);
                _off.Bind(onValueChanged);
            }

            public void Unbind(Action onValueChanged)
            {
                _on.Unbind(onValueChanged);
                _off.Unbind(onValueChanged);
            }

            public void OnBeforeSerialize() { }

            public void OnAfterDeserialize()
            {
                _on ??= new StringConstantProvider { Value = string.IsNullOrEmpty(On) ? "On" : On };
                _off ??= new StringConstantProvider { Value = string.IsNullOrEmpty(Off) ? "Off" : Off };
            }

            private static IStringProvider AsConstant(IStringProvider provider, string text)
            {
                if (provider is StringConstantProvider constant)
                {
                    constant.Value = text;
                    return constant;
                }

                return new StringConstantProvider { Value = text };
            }
        }
    }
}
