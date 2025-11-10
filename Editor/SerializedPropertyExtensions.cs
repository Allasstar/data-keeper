using System.Reflection;
using UnityEditor;

namespace DataKeeper.Editor
{
    public static class SerializedPropertyExtensions
    {
        public static System.Object GetPropertyInstance(this SerializedProperty property) {       

            string path = property.propertyPath;

            System.Object obj = property.serializedObject.targetObject;
            var type = obj.GetType();

            var fieldNames = path.Split('.');
            for (int i = 0; i < fieldNames.Length; i++) {
                var info = GetFieldIncludingBaseTypes(type, fieldNames[i]);
                if (info == null)
                    break;

                obj = info.GetValue(obj);
                type = info.FieldType;            
            }

            return obj;
        }

        private static FieldInfo GetFieldIncludingBaseTypes(System.Type type, string fieldName)
        {
            FieldInfo fieldInfo = null;
            var currentType = type;

            while (currentType != null && fieldInfo == null)
            {
                fieldInfo = currentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                currentType = currentType.BaseType;
            }

            return fieldInfo;
        }
    }
}
