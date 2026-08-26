#if DATAKEEPER_LOCALIZATION

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace DataKeeper.UI
{
    // Per-item localization is covered by CarouselString / CarouselSprite plus a
    // LocalizedStringProvider / LocalizedSpriteProvider item. This is the other shape: one
    // localized entry shared by every item ("Level {0}"), with the item passed in as smart-string
    // {0}, so a run of values does not need a table entry each.
    [Serializable]
    public abstract class CarouselLocalizedFormat<T> : Carousel<T>
    {
        [SerializeField] protected TMP_Text _label;
        [SerializeField] protected LocalizedString _format = new LocalizedString();

        // Reused so switching items does not allocate a new argument array every time.
        // Lazy: Unity may rebuild managed references without running field initializers.
        [NonSerialized] private object[] _arguments;
        [NonSerialized] private bool _isBound;

        private object[] Arguments => _arguments ??= new object[1];

        public override void Bind()
        {
            if (_isBound) return;
            _isBound = true;

            if (Count > 0) Arguments[0] = CurrentValue;
            _format.Arguments = Arguments;

            // StringChanged only tracks locale changes while it has listeners, and adding the
            // first one starts the load that fills the label.
            _format.StringChanged += SetText;
        }

        public override void Unbind()
        {
            if (!_isBound) return;
            _isBound = false;

            _format.StringChanged -= SetText;
        }

        protected override void Apply(T value)
        {
            Arguments[0] = value;
            _format.Arguments = Arguments;
            _format.RefreshString();
        }

        private void SetText(string value) => _label.text = value;
    }

    [Serializable]
    public class CarouselLocalizedInt : CarouselLocalizedFormat<int> { }

    [Serializable]
    public class CarouselLocalizedFloat : CarouselLocalizedFormat<float> { }
}

#endif
