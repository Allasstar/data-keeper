using System;
using UnityEngine;

namespace DataKeeper.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TableAttribute : PropertyAttribute
    {
        public TableAttribute()
        {
        }
    }
}