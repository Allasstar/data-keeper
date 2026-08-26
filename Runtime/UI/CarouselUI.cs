using System;
using System.Collections.Generic;
using DataKeeper.Attributes;
using DataKeeper.ValueProviders;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Carousel UI")]
    public class CarouselUI : MonoBehaviour
    {
        [field: SerializeField] public bool Interactable { get; private set; } = true;
        [field: SerializeField] public ButtonUI Previous { get; private set; }
        [field: SerializeField] public ButtonUI Next { get; private set; }
        [field: SerializeReference, SerializeReferenceSelector] public CarouselBase Carousel { get; private set; }

        [Space] public UnityEvent<int> onIndexChanged = new UnityEvent<int>();

        public int Index => Carousel.Index;
        public int Count => Carousel.Count;
        public object Value => Carousel.Value;

        private void Awake()
        {
            Previous.onClick.AddListener(Carousel.Previous);
            Next.onClick.AddListener(Carousel.Next);
            Carousel.OnIndexChanged += HandleIndexChanged;
        }

        private void OnDestroy()
        {
            Previous.onClick.RemoveListener(Carousel.Previous);
            Next.onClick.RemoveListener(Carousel.Next);
            Carousel.OnIndexChanged -= HandleIndexChanged;
        }

        private void OnEnable() => Carousel.Bind();

        private void OnDisable() => Carousel.Unbind();

        // Start, not Awake: gives other scripts a chance to subscribe before the first visual sync.
        private void Start()
        {
            Carousel.Refresh();
            SetInteractable(Interactable);
        }

        public void SetInteractable(bool interactable)
        {
            Interactable = interactable;
            RefreshButtons();
        }

        public void SetIndex(int index) => Carousel.SetIndex(index);
        public void SetIndexWithoutNotify(int index) => Carousel.SetIndexWithoutNotify(index);
        public void NextItem() => Carousel.Next();
        public void PreviousItem() => Carousel.Previous();

        public Carousel<T> As<T>() => Carousel as Carousel<T>;

        private void HandleIndexChanged(int index)
        {
            RefreshButtons();
            onIndexChanged.Invoke(index);
        }

        private void RefreshButtons()
        {
            Previous.interactable = Interactable && Carousel.CanPrevious;
            Next.interactable = Interactable && Carousel.CanNext;
        }
    }

    [Serializable]
    public abstract class CarouselBase
    {
        [field: SerializeField] public int Index { get; protected set; }
        [field: SerializeField] public bool IsLoop { get; set; } = true;

        public event Action<int> OnIndexChanged;

        public abstract int Count { get; }
        public abstract object Value { get; }

        public bool CanPrevious => IsLoop ? Count > 1 : Index > 0;
        public bool CanNext => IsLoop ? Count > 1 : Index < Count - 1;

        public void Previous() => SetIndex(Index - 1);
        public void Next() => SetIndex(Index + 1);

        public void SetIndex(int index)
        {
            int previous = Index;
            if (!MoveTo(index) || Index == previous) return;

            OnChanged();
            OnIndexChanged?.Invoke(Index);
        }

        public void SetIndexWithoutNotify(int index) => MoveTo(index);

        // Pushes the current item into the visuals without raising events.
        public void Refresh() => MoveTo(Index);

        protected abstract void Apply(int index);

        protected virtual void OnChanged() { }

        // Lifecycle hooks for carousels holding external subscriptions (locale changes, remote data).
        public virtual void Bind() { }

        public virtual void Unbind() { }

        private bool MoveTo(int index)
        {
            if (Count == 0) return false;

            Index = Resolve(index);
            Apply(Index);
            return true;
        }

        private int Resolve(int index)
        {
            int count = Count;
            return IsLoop ? (index % count + count) % count : Mathf.Clamp(index, 0, count - 1);
        }
    }

    [Serializable]
    public abstract class Carousel<T> : CarouselBase
    {
        [SerializeField] protected List<T> _values = new List<T>();

        public event Action<T> OnValueChanged;

        public List<T> Values => _values;
        public override int Count => _values.Count;
        public override object Value => CurrentValue;
        public T CurrentValue => _values[Index];

        public void SetValues(IEnumerable<T> values)
        {
            _values.Clear();
            _values.AddRange(values);
            Refresh();
        }

        public int IndexOf(T value) => _values.IndexOf(value);

        public void SetValue(T value)
        {
            int index = IndexOf(value);
            if (index < 0) return;
            SetIndex(index);
        }

        protected override void OnChanged() => OnValueChanged?.Invoke(CurrentValue);

        protected override void Apply(int index) => Apply(_values[index]);

        protected abstract void Apply(T value);
    }

    // Items are value providers instead of literals, so one carousel can mix constants, localized
    // entries and asset-backed sources. Observable providers repaint the current item in place,
    // which is why there is no separate localized carousel for these.
    [Serializable]
    public abstract class CarouselProvider<TProvider, TValue> : CarouselBase where TProvider : class, IValueProvider<TValue>
    {
        [SerializeReference, SerializeReferenceSelector] protected List<TProvider> _items = new List<TProvider>();

        [NonSerialized] private TProvider _bound;
        [NonSerialized] private bool _isBound;

        public event Action<TValue> OnValueChanged;

        public List<TProvider> Items => _items;
        public override int Count => _items.Count;
        public override object Value => CurrentValue;
        public TProvider CurrentProvider => _items[Index];

        // A list element starts out null the moment it is added in the inspector, so an
        // unassigned item resolves to nothing instead of throwing.
        public TValue CurrentValue => _items[Index] != null ? _items[Index].GetValue() : default;

        public void SetItems(IEnumerable<TProvider> items)
        {
            _items.Clear();
            _items.AddRange(items);
            Refresh();
        }

        public int IndexOf(TValue value)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == null) continue;
                if (EqualityComparer<TValue>.Default.Equals(_items[i].GetValue(), value)) return i;
            }

            return -1;
        }

        public void SetValue(TValue value)
        {
            int index = IndexOf(value);
            if (index < 0) return;

            SetIndex(index);
        }

        public override void Bind()
        {
            _isBound = true;
            Subscribe();
        }

        public override void Unbind()
        {
            Unsubscribe();
            _isBound = false;
        }

        protected override void OnChanged() => OnValueChanged?.Invoke(CurrentValue);

        protected override void Apply(int index)
        {
            Unsubscribe();
            Apply(CurrentValue);
            Subscribe();
        }

        protected abstract void Apply(TValue value);

        private void Subscribe()
        {
            if (!_isBound || _bound != null || Count == 0) return;

            _bound = CurrentProvider;
            _bound.Bind(HandleProviderValueChanged);
        }

        private void Unsubscribe()
        {
            if (_bound == null) return;

            _bound.Unbind(HandleProviderValueChanged);
            _bound = null;
        }

        // Re-applies without touching the subscription: an observable provider can push its
        // first value from inside Subscribe.
        private void HandleProviderValueChanged() => Apply(CurrentValue);
    }

    [Serializable]
    public abstract class CarouselLabel<T> : Carousel<T>
    {
        [SerializeField] protected TMP_Text _label;
        [SerializeField] protected string _format = "{0}";

        protected override void Apply(T value) => _label.text = Format(value);

        protected virtual string Format(T value) => string.Format(_format, value);
    }

    [Serializable]
    public class CarouselInt : CarouselLabel<int> { }

    [Serializable]
    public class CarouselFloat : CarouselLabel<float> { }

    [Serializable]
    public class CarouselBool : CarouselLabel<bool>
    {
        [SerializeField] private string _trueLabel = "On";
        [SerializeField] private string _falseLabel = "Off";

        public CarouselBool()
        {
            _values.Add(false);
            _values.Add(true);
        }

        protected override string Format(bool value) => string.Format(_format, value ? _trueLabel : _falseLabel);
    }

    [Serializable]
    public class CarouselString : CarouselProvider<IStringProvider, string>
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _format = "{0}";

        protected override void Apply(string value) => _label.text = string.Format(_format, value);
    }

    [Serializable]
    public class CarouselSprite : CarouselProvider<ISpriteProvider, Sprite>
    {
        [SerializeField] private Image _image;
        [SerializeField] private bool _preserveAspect = true;

        protected override void Apply(Sprite value)
        {
            _image.sprite = value;
            _image.preserveAspect = _preserveAspect;
        }
    }
}
