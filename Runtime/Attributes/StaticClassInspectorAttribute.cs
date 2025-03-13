using System;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StaticClassInspectorAttribute : Attribute
    {
        public string Category { get; private set; }

        public StaticClassInspectorAttribute(string category = "Default")
        {
            Category = category;
        }
    }
}