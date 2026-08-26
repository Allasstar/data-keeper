#if DATAKEEPER_LOCALIZATION

using System;
using UnityEngine;
using UnityEngine.Localization;

namespace DataKeeper.ValueProviders
{
    // Integration providers: resolve from a localization table and re-raise ValueChanged
    // whenever the selected locale (or, for strings, a smart-string variable) changes.

    [Serializable]
    public class LocalizedStringProvider : IStringProvider, IObservableValueProvider
    {
        [SerializeField] private LocalizedString _localizedString = new LocalizedString();

        [NonSerialized] private string _cached;
        [NonSerialized] private bool _hasCached;
        [NonSerialized] private Action _valueChanged;

        public LocalizedString LocalizedString => _localizedString;

        public event Action ValueChanged
        {
            add
            {
                bool wasUnobserved = _valueChanged == null;
                _valueChanged += value;

                // StringChanged only tracks locale changes while it has listeners, and adding
                // the first one starts the load that fills the cache.
                if (wasUnobserved) _localizedString.StringChanged += HandleStringChanged;
            }
            remove
            {
                _valueChanged -= value;
                if (_valueChanged != null) return;

                _localizedString.StringChanged -= HandleStringChanged;
                _hasCached = false;
            }
        }

        // Falls back to a synchronous load when nobody observes this provider.
        public string GetValue() => _hasCached ? _cached : _localizedString.GetLocalizedString();

        private void HandleStringChanged(string value)
        {
            _cached = value;
            _hasCached = true;
            _valueChanged?.Invoke();
        }
    }

    [Serializable]
    public class LocalizedSpriteProvider : ISpriteProvider, IObservableValueProvider
    {
        [SerializeField] private LocalizedSprite _localizedSprite = new LocalizedSprite();

        [NonSerialized] private Sprite _cached;
        [NonSerialized] private bool _hasCached;
        [NonSerialized] private Action _valueChanged;

        public LocalizedSprite LocalizedSprite => _localizedSprite;

        public event Action ValueChanged
        {
            add
            {
                bool wasUnobserved = _valueChanged == null;
                _valueChanged += value;

                // AssetChanged only tracks locale changes while it has listeners, and adding
                // the first one starts the load that fills the cache.
                if (wasUnobserved) _localizedSprite.AssetChanged += HandleAssetChanged;
            }
            remove
            {
                _valueChanged -= value;
                if (_valueChanged != null) return;

                _localizedSprite.AssetChanged -= HandleAssetChanged;
                _cached = null;
                _hasCached = false;
            }
        }

        // Falls back to a synchronous load when nobody observes this provider.
        public Sprite GetValue() => _hasCached ? _cached : _localizedSprite.LoadAsset();

        private void HandleAssetChanged(Sprite value)
        {
            _cached = value;
            _hasCached = true;
            _valueChanged?.Invoke();
        }
    }
}

#endif
