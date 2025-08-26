using System;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ButtonAttribute : Attribute
    {
        public string ButtonLabel { get; }
        public float Space { get; }
        public ButtonEnabledState ButtonEnabledState { get; }

        public ButtonAttribute(string buttonLabel = null, float space = 0f, ButtonEnabledState buttonEnabledState = ButtonEnabledState.Always)
        {
            ButtonLabel = buttonLabel;
            Space = space;
            ButtonEnabledState = buttonEnabledState;
        }
    }
    
    public enum ButtonEnabledState
    {
        Always = 0,
        InEditMode = 1,
        InPlayMode = 2,
    }
}
