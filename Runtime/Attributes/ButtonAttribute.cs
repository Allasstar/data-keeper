using System;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class ButtonAttribute : Attribute
    {
        public string ButtonLabel { get; }
        public string GroupLabel { get; }
        public float Space { get; }
        public ButtonEnabledState ButtonEnabledState { get; }

        public ButtonAttribute(string buttonLabel = null, float space = 0f, ButtonEnabledState buttonEnabledState = ButtonEnabledState.Always, string groupLabel = null)
        {
            ButtonLabel = buttonLabel;
            Space = space;
            ButtonEnabledState = buttonEnabledState;
            GroupLabel = groupLabel;
        }
    }
    
    public enum ButtonEnabledState
    {
        Always = 0,
        InEditMode = 1,
        InPlayMode = 2,
    }
}
