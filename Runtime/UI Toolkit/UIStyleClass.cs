using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    public class UIStyleClass
    {
        public string[] ClassNames { get; }

        public UIStyleClass(params string[] classNames)
        {
            ClassNames = classNames;
        }

        public void ApplyTo(VisualElement element)
        {
            foreach (var className in ClassNames)
            {
                if (!string.IsNullOrEmpty(className))
                    element.AddToClassList(className);
            }
        }

        public static readonly UIStyleClass Default = new UIStyleClass("default");
        public static readonly UIStyleClass PrimaryButton = new UIStyleClass("button", "primary");
        public static readonly UIStyleClass LabelTitle = new UIStyleClass("label", "title");
        public static readonly UIStyleClass TextFieldInline = new UIStyleClass("textfield", "inline");
        public static readonly UIStyleClass ContainerBox = new UIStyleClass("container", "box");
    }
}
