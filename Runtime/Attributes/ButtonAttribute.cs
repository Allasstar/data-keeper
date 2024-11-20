using System;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ButtonAttribute : Attribute
    {
        public string ButtonLabel { get; }
        public float Space { get; }

        public ButtonAttribute(string buttonLabel = null, float space = 0f)
        {
            ButtonLabel = buttonLabel;
            Space = space;
        }
    }
}
