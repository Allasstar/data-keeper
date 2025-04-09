using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Utility
{
    /// <summary>
    /// Utility class for converting Lists and Unity Objects to CSV format and back
    /// </summary>
    public static class CSVUtility
    {
        private const char Delimiter = ',';
        private const char EscapeChar = '"';
        private const string TypeSeparator = ":";

        #region Public Methods

        /// <summary>
        /// Converts a list of objects to CSV format
        /// </summary>
        /// <typeparam name="T">Type of objects in the list</typeparam>
        /// <param name="list">List to convert</param>
        /// <returns>CSV string representation</returns>
        public static string ListToCSV<T>(List<T> list) where T : class
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            Type type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            // Write header with property/field names and types
            WriteHeaderRow(sb, properties, fields);

            // Write data rows
            foreach (var item in list)
            {
                WriteDataRow(sb, item, properties, fields);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a CSV string back to a list of objects
        /// </summary>
        /// <typeparam name="T">Type of objects to create</typeparam>
        /// <param name="csv">CSV string</param>
        /// <returns>List of objects</returns>
        public static List<T> CSVToList<T>(string csv) where T : class, new()
        {
            if (string.IsNullOrEmpty(csv))
                return new List<T>();

            List<T> result = new List<T>();
            string[] lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
                return result;

            // Parse header to get property/field names and types
            string[] headerParts = ParseCSVLine(lines[0]);
            var headerInfo = ParseHeaderInfo(headerParts);

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                T item = new T();
                string[] values = ParseCSVLine(lines[i]);

                for (int j = 0; j < Math.Min(headerInfo.Count, values.Length); j++)
                {
                    var (name, type) = headerInfo[j];
                    SetValue(item, name, type, values[j]);
                }

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Saves a list of objects to a CSV file
        /// </summary>
        /// <typeparam name="T">Type of objects in the list</typeparam>
        /// <param name="list">List to save</param>
        /// <param name="filePath">File path relative to Application.dataPath</param>
        public static void SaveToFile<T>(List<T> list, string filePath) where T : class
        {
            string fullPath = Path.Combine(Application.dataPath, filePath);
            string directoryPath = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(fullPath, ListToCSV(list));
        }

        /// <summary>
        /// Loads a list of objects from a CSV file
        /// </summary>
        /// <typeparam name="T">Type of objects to create</typeparam>
        /// <param name="filePath">File path relative to Application.dataPath</param>
        /// <returns>List of objects</returns>
        public static List<T> LoadFromFile<T>(string filePath) where T : class, new()
        {
            string fullPath = Path.Combine(Application.dataPath, filePath);

            if (!File.Exists(fullPath))
                return new List<T>();

            string csv = File.ReadAllText(fullPath);
            return CSVToList<T>(csv);
        }

        #endregion

        #region Private Methods

        private static void WriteHeaderRow(StringBuilder sb, PropertyInfo[] properties, FieldInfo[] fields)
        {
            List<string> headerCells = new List<string>();

            // Add properties to header
            foreach (var prop in properties)
            {
                string typeName = GetTypeNameForHeader(prop.PropertyType);
                headerCells.Add($"{prop.Name}{TypeSeparator}{typeName}");
            }

            // Add fields to header
            foreach (var field in fields)
            {
                string typeName = GetTypeNameForHeader(field.FieldType);
                headerCells.Add($"{field.Name}{TypeSeparator}{typeName}");
            }

            sb.AppendLine(string.Join(Delimiter.ToString(), headerCells.Select(EscapeCSVCell)));
        }

        private static void WriteDataRow<T>(StringBuilder sb, T item, PropertyInfo[] properties, FieldInfo[] fields)
        {
            List<string> cells = new List<string>();

            // Add property values
            foreach (var prop in properties)
            {
                string cellValue = GetValueAsString(prop.GetValue(item), prop.PropertyType);
                cells.Add(cellValue);
            }

            // Add field values
            foreach (var field in fields)
            {
                string cellValue = GetValueAsString(field.GetValue(item), field.FieldType);
                cells.Add(cellValue);
            }

            sb.AppendLine(string.Join(Delimiter.ToString(), cells.Select(EscapeCSVCell)));
        }

        private static string GetValueAsString(object value, Type type)
        {
            if (value == null)
                return string.Empty;

            // Handle Unity objects by storing reference ID
            if (typeof(Object).IsAssignableFrom(type))
            {
                Object unityObj = value as Object;
                if (unityObj == null)
                    return string.Empty;

                string path = GetUnityObjectPath(unityObj);
                return path;
            }

            // Handle lists and arrays
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as System.Collections.IList;
                var itemType = type.GetGenericArguments()[0];
                var items = new List<string>();

                foreach (var item in list)
                {
                    items.Add(GetValueAsString(item, itemType));
                }

                return "[" + string.Join("|", items) + "]";
            }

            if (type.IsArray)
            {
                var array = value as Array;
                var itemType = type.GetElementType();
                var items = new List<string>();

                foreach (var item in array)
                {
                    items.Add(GetValueAsString(item, itemType));
                }

                return "[" + string.Join("|", items) + "]";
            }

            // Handle other types by using ToString()
            return value.ToString();
        }

        private static string GetUnityObjectPath(Object unityObj)
        {
            if (unityObj == null)
                return "";

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GUIDFromAssetPath(UnityEditor.AssetDatabase.GetAssetPath(unityObj)).ToString();
#endif

            return "";
        }

        private static string EscapeCSVCell(string cell)
        {
            if (string.IsNullOrEmpty(cell))
                return string.Empty;

            bool needsEscaping = cell.Contains(Delimiter) || cell.Contains(EscapeChar) || cell.Contains("\n");

            if (needsEscaping)
                return
                    $"{EscapeChar}{cell.Replace(EscapeChar.ToString(), EscapeChar.ToString() + EscapeChar.ToString())}{EscapeChar}";

            return cell;
        }

        private static string[] ParseCSVLine(string line)
        {
            List<string> cells = new List<string>();
            StringBuilder currentCell = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == EscapeChar)
                {
                    // Check if escaped quote (double quote)
                    if (i + 1 < line.Length && line[i + 1] == EscapeChar)
                    {
                        currentCell.Append(EscapeChar);
                        i++; // Skip next quote
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == Delimiter && !insideQuotes)
                {
                    cells.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else
                {
                    currentCell.Append(c);
                }
            }

            cells.Add(currentCell.ToString());
            return cells.ToArray();
        }

        private static List<(string name, string type)> ParseHeaderInfo(string[] headerParts)
        {
            var result = new List<(string name, string type)>();

            foreach (var part in headerParts)
            {
                string[] nameTypePair = part.Split(new[] { TypeSeparator }, StringSplitOptions.None);
                if (nameTypePair.Length == 2)
                {
                    result.Add((nameTypePair[0], nameTypePair[1]));
                }
                else
                {
                    // If no type specified, assume string
                    result.Add((part, "String"));
                }
            }

            return result;
        }

        private static void SetValue<T>(T item, string propertyName, string typeName, string value)
        {
            Type itemType = typeof(T);

            // Try to find and set the property/field through the inheritance chain
            if (!TrySetValueInTypeHierarchy(item, itemType, propertyName, typeName, value))
            {
                Debug.LogWarning($"Property or field {propertyName} not found on {itemType.Name} or any base class");
            }
        }

        private static bool TrySetValueInTypeHierarchy<T>(T item, Type currentType, string propertyName, string typeName,
            string value)
        {
            // If we've reached the end of the inheritance chain without finding a match
            if (currentType == null || currentType == typeof(object))
            {
                return false;
            }

            // Try property in current type
            PropertyInfo property = currentType.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (property != null)
            {
                // Check if we can write directly with public setter
                if (property.CanWrite && property.GetSetMethod(false) != null)
                {
                    object convertedValue = ConvertValueFromString(value, property.PropertyType, typeName);
                    property.SetValue(item, convertedValue);
                    return true;
                }

                // Try to find backing field for properties with [field: SerializeField]
                string backingFieldName = $"<{propertyName}>k__BackingField";
                FieldInfo backingField = currentType.GetField(backingFieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (backingField != null)
                {
                    object convertedValue = ConvertValueFromString(value, property.PropertyType, typeName);
                    backingField.SetValue(item, convertedValue);
                    return true;
                }

                // Try to use private setter
                MethodInfo setMethod = property.GetSetMethod(true); // Include non-public methods
                if (setMethod != null)
                {
                    object convertedValue = ConvertValueFromString(value, property.PropertyType, typeName);
                    setMethod.Invoke(item, new[] { convertedValue });
                    return true;
                }
            }

            // Try field in current type
            FieldInfo field = currentType.GetField(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (field != null)
            {
                object convertedValue = ConvertValueFromString(value, field.FieldType, typeName);
                field.SetValue(item, convertedValue);
                return true;
            }

            // Try in the base class
            return TrySetValueInTypeHierarchy(item, currentType.BaseType, propertyName, typeName, value);
        }

        private static object ConvertValueFromString(string value, Type targetType, string typeName)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(targetType);

            // Handle Unity objects by resolving reference ID
            if (typeof(Object).IsAssignableFrom(targetType))
            {
                return ResolveUnityObjectReference(value, targetType);
            }

            // Handle lists
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (!value.StartsWith("[") || !value.EndsWith("]"))
                    return GetDefaultValue(targetType);

                string content = value.Substring(1, value.Length - 2);
                string[] items = content.Split('|');

                Type itemType = targetType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = Activator.CreateInstance(listType) as System.Collections.IList;

                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        list.Add(ConvertValueFromString(item, itemType, typeName));
                    }
                }

                return list;
            }

            // Handle arrays
            if (targetType.IsArray)
            {
                if (!value.StartsWith("[") || !value.EndsWith("]"))
                    return GetDefaultValue(targetType);

                string content = value.Substring(1, value.Length - 2);
                string[] items = content.Split('|');

                Type elementType = targetType.GetElementType();
                Array array = Array.CreateInstance(elementType, items.Length);

                for (int i = 0; i < items.Length; i++)
                {
                    if (!string.IsNullOrEmpty(items[i]))
                    {
                        array.SetValue(ConvertValueFromString(items[i], elementType, typeName), i);
                    }
                }

                return array;
            }

            // Handle basic types
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.TryParse(value, out int intResult) ? intResult : 0;

            if (targetType == typeof(float) || targetType == typeof(float?))
                return float.TryParse(value, out float floatResult) ? floatResult : 0f;

            if (targetType == typeof(double) || targetType == typeof(double?))
                return double.TryParse(value, out double doubleResult) ? doubleResult : 0d;

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.TryParse(value, out bool boolResult) && boolResult;

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return DateTime.TryParse(value, out DateTime dateResult) ? dateResult : DateTime.MinValue;

            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out object enumResult)
                    ? enumResult
                    : Enum.GetValues(targetType).GetValue(0);

            if (targetType == typeof(Vector2))
            {
                string[] parts = value.Split(';');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y))
                    return new Vector2(x, y);
                return Vector2.zero;
            }

            if (targetType == typeof(Vector3))
            {
                string[] parts = value.Split(';');
                if (parts.Length == 3 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z))
                    return new Vector3(x, y, z);
                return Vector3.zero;
            }

            if (targetType == typeof(Color))
            {
                string[] parts = value.Split(';');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], out float r) &&
                    float.TryParse(parts[1], out float g) &&
                    float.TryParse(parts[2], out float b) &&
                    float.TryParse(parts[3], out float a))
                    return new Color(r, g, b, a);
                return Color.white;
            }

            // For other types, try to use Convert
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        private static object ResolveUnityObjectReference(string guid, Type targetType)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guid), targetType);
#endif

            return null;
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        private static string GetTypeNameForHeader(Type type)
        {
            // if (type == typeof(string))
            //     return "String";
            //     
            // if (type == typeof(int) || type == typeof(int?))
            //     return "Int";
            //     
            // if (type == typeof(float) || type == typeof(float?))
            //     return "Float";
            //     
            // if (type == typeof(double) || type == typeof(double?))
            //     return "Double";
            //     
            // if (type == typeof(bool) || type == typeof(bool?))
            //     return "Bool";
            //     
            // if (type == typeof(DateTime) || type == typeof(DateTime?))
            //     return "DateTime";
            //     
            // if (type.IsEnum)
            //     return type.Name;
            //     
            // if (type == typeof(Vector2))
            //     return "Vector2";
            //     
            // if (type == typeof(Vector3))
            //     return "Vector3";
            //     
            // if (type == typeof(Color))
            //     return "Color";

            // if (typeof(Object).IsAssignableFrom(type))
            //     return type.Name;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                return $"List<{GetTypeNameForHeader(itemType)}>";
            }

            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                return $"{GetTypeNameForHeader(elementType)}[]";
            }

            return type.Name;
        }

        #endregion
    }
}