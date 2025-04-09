using System;
using UnityEngine;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CSVTableAttribute : PropertyAttribute
    {
        public string ListPropertyName { get; private set; }

        public CSVTableAttribute(string listPropertyName)
        {
            ListPropertyName = listPropertyName;
        }
    }
}