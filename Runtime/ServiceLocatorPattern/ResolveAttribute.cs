using System;

namespace DataKeeper.ServiceLocatorPattern
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ResolveAttribute : Attribute
    {
        public ContextType Context { get; }
        public string ID { get; }
        public string TableName { get; }
        
        public ResolveAttribute()
        {
            Context = ContextType.Global;
            ID = string.Empty;
            TableName = string.Empty;
        }
        
        public ResolveAttribute(ContextType context = ContextType.Global)
        {
            Context = context;
            ID = string.Empty;
            TableName = string.Empty;
        }

        public ResolveAttribute(ContextType context = ContextType.Global, string id = "", string tableName = "")
        {
            Context = context;
            ID = id;
            TableName = tableName;
        }
    }
}