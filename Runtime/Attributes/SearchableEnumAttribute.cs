using UnityEngine;

namespace DataKeeper.Attributes
{
    public class SearchableEnumAttribute : PropertyAttribute
    {
        public bool ShowValue { get; }

        public SearchableEnumAttribute(bool showValue = false)
        {
            ShowValue = showValue;
        }
    }
}