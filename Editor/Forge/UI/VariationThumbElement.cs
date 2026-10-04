using System;
using Unity.Collections;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    [UxmlElement]
    public partial class VariationThumbElement : VisualElement
    {
        public const string UssClassName = "forge-thumb";

        private readonly WaveformElement _wave;
        private readonly Label _label;

        public int Index { get; private set; }

        public event Action<int> Clicked;

        public VariationThumbElement()
        {
            AddToClassList(UssClassName);

            _wave = new WaveformElement { ShowTrim = false, pickingMode = PickingMode.Ignore };
            Add(_wave);

            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.AddToClassList(UssClassName + "__label");
            Add(_label);

            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                Clicked?.Invoke(Index);
                evt.StopPropagation();
            });
        }

        public bool Selected
        {
            set => EnableInClassList(UssClassName + "--selected", value);
        }

        public void SetIndex(int index)
        {
            Index = index;
            _label.text = (index + 1).ToString();
        }

        public void SetSamples(NativeArray<float> interleaved, int channels)
        {
            _wave.SetSamples(interleaved, channels);
            EnableInClassList(UssClassName + "--empty", false);
        }

        public void ClearSamples()
        {
            _wave.ClearSamples();
            EnableInClassList(UssClassName + "--empty", true);
        }
    }
}
