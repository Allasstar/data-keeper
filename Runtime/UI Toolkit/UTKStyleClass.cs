using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit
{
    public class UTKStyleClass
    {
        public string[] ClassNames { get; }

        public UTKStyleClass(params string[] classNames)
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

        public static readonly UTKStyleClass Default = new UTKStyleClass("default");
        public static readonly UTKStyleClass PrimaryButton = new UTKStyleClass("button", "primary");
        public static readonly UTKStyleClass LabelTitle = new UTKStyleClass("label", "title");
        public static readonly UTKStyleClass TextFieldInline = new UTKStyleClass("textfield", "inline");
        public static readonly UTKStyleClass ContainerBox = new UTKStyleClass("container", "box");
    }
}
