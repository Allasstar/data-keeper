using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit.Elements
{
    public class ToggleButton : Button
    {
        private static readonly Color ActiveColor   = new Color(0.3f, 0.5f, 0.8f);
        private static readonly Color InactiveColor = new Color(0.25f, 0.25f, 0.25f);

        private bool _isOn;
        private event Action<bool> _onValueChanged;

        public bool IsOn
        {
            get => _isOn;
            set
            {
                if (_isOn == value) return;
                _isOn = value;
                RefreshVisual();
                _onValueChanged?.Invoke(_isOn);
            }
        }

        public ToggleButton(string label, bool defaultValue = true, Action<bool> onValueChanged = null)
        {
            text = label;
            _isOn = defaultValue;
            _onValueChanged = onValueChanged;

            clicked += Toggle;
            RefreshVisual();
        }

        public void Toggle()
        {
            IsOn = !_isOn;
        }

        public ToggleButton OnValueChanged(Action<bool> callback)
        {
            _onValueChanged += callback;
            return this;
        }

        private void RefreshVisual()
        {
            style.backgroundColor = _isOn ? ActiveColor : InactiveColor;
        }

        // Optional: allow custom colors per instance
        public ToggleButton SetColors(Color active, Color inactive)
        {
            RefreshVisual();
            return this;
        }
    }
}