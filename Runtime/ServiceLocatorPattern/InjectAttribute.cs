using System;

namespace DataKeeper.ServiceLocatorPattern
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        public ContextType Context { get; }
        public string ID { get; }
        public string TableName { get; }
        
        public InjectAttribute(ContextType context = ContextType.Global)
        {
            Context = context;
            ID = string.Empty;
            TableName = string.Empty;
        }

        public InjectAttribute(ContextType context = ContextType.Global, string id = "", string tableName = "")
        {
            Context = context;
            ID = id;
            TableName = tableName;
        }
    }
}