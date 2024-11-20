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
                var info = type.GetField(fieldNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (info == null)
                    break;

                obj = info.GetValue(obj);
                type = info.FieldType;            
            }

            return obj;
        }
    }
}
