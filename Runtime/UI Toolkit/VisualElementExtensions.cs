using System;

#if !UNITY_6000_0_OR_NEWER
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class UxmlAttributeAttribute : Attribute
{
    public string Name { get; }
    public UxmlAttributeAttribute(string name = null)
    {
        Name = name;
    }
}

public class UxmlElementAttribute : Attribute
{
}
#endif