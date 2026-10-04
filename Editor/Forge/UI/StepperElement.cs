using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Wraps a real EnumField so property binding, undo and the full value list behave exactly
    // like a dropdown; the arrows only set the inner field's value.
    public class StepperElement : VisualElement
    {
        public const string UssClassName = "forge-stepper";

        private readonly EnumField _field;
        private readonly Array _values;

        public event Action<Enum> Changed;

        public StepperElement(Enum initial)
        {
            AddToClassList(UssClassName);
            _values = Enum.GetValues(initial.GetType());

            Add(Arrow("<", -1));
            _field = new EnumField(initial);
            _field.AddToClassList(UssClassName + "__field");
            _field.RegisterValueChangedCallback(e => Changed?.Invoke(e.newValue));
            Add(_field);
            Add(Arrow(">", 1));
        }

        public void BindProperty(SerializedProperty property) => _field.BindProperty(property);

        public void SetValueWithoutNotify(Enum value) => _field.SetValueWithoutNotify(value);

        public void Step(int direction)
        {
            var count = _values.Length;
            var index = Array.IndexOf(_values, _field.value);
            index = index < 0 ? 0 : (index + direction + count) % count;
            _field.value = (Enum)_values.GetValue(index);
        }

        private Button Arrow(string text, int direction)
        {
            var button = new Button(() => Step(direction)) { text = text, focusable = false };
            button.AddToClassList(UssClassName + "__arrow");
            return button;
        }
    }
}
