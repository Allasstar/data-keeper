using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Utility
{
    /// <summary>
    /// Utility class for converting Lists and Unity Objects to CSV format and back
    /// </summary>
    public static class CSVUtility
    {
        private const char DELIMITER_COMMA = ',';
        private const char DELIMITER_TAB = '\t';
        private const char ESCAPE_CHAR = '"';
        private const string TYPE_SEPARATOR = ":";
        private const string ARRAY_SEPARATOR = "|";

        #region Public Methods

        /// <summary>
        /// Converts a Unity Object to its GUID
        /// </summary>
        /// <param name="unityObject">Unity Object to convert</param>
        /// <returns>GUID as string, or empty string if the object has no GUID</returns>
        public static string UnityObjectToAssetIdentifier(Object unityObject, CSVAssetReferenceType assetReferenceType)
        {
            if (unityObject == null)
                return string.Empty;

#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(unityObject);

            if (assetReferenceType == CSVAssetReferenceType.AssetPath)
            {
                return assetPath;
            }
            
            if (!string.IsNullOrEmpty(assetPath))
            {
                return UnityEditor.AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
            }
#endif
            return string.Empty;
        }

        /// <summary>
        /// Converts a GUID to a Unity Object of the specified type
        /// </summary>
        /// <param name="assetIdentifier">GUID as string</param>
        /// <param name="type">Type of Unity Object to convert to</param>
        /// <returns>Unity Object, or null if the GUID is invalid</returns>
        public static Object AssetIdentifierToUnityObject(string assetIdentifier, Type type, CSVAssetReferenceType assetReferenceType)
        {
            if (string.IsNullOrEmpty(assetIdentifier))
                return null;

#if UNITY_EDITOR
            string assetPath = assetIdentifier;
            if (assetReferenceType == CSVAssetReferenceType.GUID)
            {
                assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetIdentifier);
                
                if (string.IsNullOrEmpty(assetPath))
                    return null;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, type);
#endif
            return null;
        }

        /// <summary>
        /// Converts a GUID to a Unity Object of the specified type
        /// </summary>
        /// <typeparam name="T">Type of Unity Object to convert to</typeparam>
        /// <param name="assetIdentifier">GUID as string</param>
        /// <returns>Unity Object of type T, or null if the GUID is invalid</returns>
        public static T AssetIdentifierToUnityObject<T>(string assetIdentifier, CSVAssetReferenceType assetReferenceType) where T : Object
        {
            return AssetIdentifierToUnityObject(assetIdentifier, typeof(T), assetReferenceType) as T;
        }

        /// <summary>
        /// Resolves a Sprite from a texture GUID and sprite name
        /// </summary>
        /// <param name="texturePath">GUID Or Path of the texture asset</param>
        /// <param name="spriteName">Name of the sprite in the texture</param>
        /// <param name="assetReferenceType"></param>
        /// <returns>Sprite object, or null if not found</returns>
        public static Sprite ResolveSprite(string texturePath, string spriteName, CSVAssetReferenceType assetReferenceType)
        {
            if (string.IsNullOrEmpty(texturePath) || string.IsNullOrEmpty(spriteName))
                return null;

#if UNITY_EDITOR
            // Load the texture first
            string path = texturePath;
            if (assetReferenceType == CSVAssetReferenceType.GUID)
            {
                path = UnityEditor.AssetDatabase.GUIDToAssetPath(texturePath);
            }
            
            if (string.IsNullOrEmpty(path))
                return null;

            // Find the sprite with the matching name in this texture
            var allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)
                .Where(x => x is Sprite)
                .Cast<Sprite>();

            return allSprites.FirstOrDefault(s => s.name == spriteName);
#endif
            return null;
        }

        /// <summary>
        /// Converts a list of objects to CSV format
        /// </summary>
        /// <typeparam name="T">Type of objects in the list</typeparam>
        /// <param name="list">List to convert</param>
        /// <returns>CSV string representation</returns>
        public static string ListToCSV<T>(List<T> list, CSVDelimiterType delimiterType = CSVDelimiterType.Comma, CSVAssetReferenceType assetReferenceType = CSVAssetReferenceType.GUID) where T : class
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            Type type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            var delimiter = delimiterType == CSVDelimiterType.Comma ? DELIMITER_COMMA : DELIMITER_TAB;

            // Write header with property/field names and types
            WriteHeaderRow(sb, properties, fields, delimiter);

            // Write data rows
            foreach (var item in list)
            {
                WriteDataRow(sb, item, properties, fields, delimiter, assetReferenceType);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a CSV string back to a list of objects
        /// </summary>
        /// <typeparam name="T">Type of objects to create</typeparam>
        /// <param name="csv">CSV string</param>
        /// <returns>List of objects</returns>
        public static List<T> CSVToList<T>(string csv, CSVDelimiterType delimiterType = CSVDelimiterType.Comma, CSVAssetReferenceType assetReferenceType = CSVAssetReferenceType.GUID) where T : class, new()
        {
            if (string.IsNullOrEmpty(csv))
                return new List<T>();

            List<T> result = new List<T>();
            string[] lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
                return result;

            var delimiter = delimiterType == CSVDelimiterType.Comma ? DELIMITER_COMMA : DELIMITER_TAB;

            // Parse header to get property/field names and types
            string[] headerParts = ParseCSVLine(lines[0], delimiter);
            var headerInfo = ParseHeaderInfo(headerParts);

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                T item = new T();
                string[] values = ParseCSVLine(lines[i], delimiter);

                for (int j = 0; j < Math.Min(headerInfo.Count, values.Length); j++)
                {
                    var (name, type) = headerInfo[j];
                    SetValue(item, name, type, values[j], assetReferenceType);
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

        public static Type GetTypeFromHeader(string header)
        {
            var list = header.Split(TYPE_SEPARATOR);
            return list.Length == 2 ? GetTypeFromName(list[1], typeof(string)) : typeof(object);
        }

        #endregion

        #region Private Methods

        private static void WriteHeaderRow(StringBuilder sb, PropertyInfo[] properties, FieldInfo[] fields, char delimiter)
        {
            List<string> headerCells = new List<string>();

            // Add properties to header
            foreach (var prop in properties)
            {
                string typeName = GetTypeNameForHeader(prop.PropertyType);
                headerCells.Add($"{prop.Name}{TYPE_SEPARATOR}{typeName}");
            }

            // Add fields to header
            foreach (var field in fields)
            {
                string typeName = GetTypeNameForHeader(field.FieldType);
                headerCells.Add($"{field.Name}{TYPE_SEPARATOR}{typeName}");
            }

            sb.AppendLine(string.Join(delimiter.ToString(), headerCells.Select(s => EscapeCSVCell(s, delimiter))));
        }

        private static void WriteDataRow<T>(StringBuilder sb, T item, PropertyInfo[] properties, FieldInfo[] fields, char delimiter, CSVAssetReferenceType assetReferenceType)
        {
            List<string> cells = new List<string>();

            // Add property values
            foreach (var prop in properties)
            {
                string cellValue = GetValueAsString(prop.GetValue(item), prop.PropertyType, assetReferenceType);
                cells.Add(cellValue);
            }

            // Add field values
            foreach (var field in fields)
            {
                string cellValue = GetValueAsString(field.GetValue(item), field.FieldType, assetReferenceType);
                cells.Add(cellValue);
            }

            sb.AppendLine(string.Join(delimiter.ToString(), cells.Select(s => EscapeCSVCell(s, delimiter))));
        }

        private static string GetValueAsString(object value, Type type, CSVAssetReferenceType assetReferenceType)
        {
            if (value == null)
                return string.Empty;

            // Handle special Unity types
            if (type == typeof(Vector2))
            {
                Vector2 v = (Vector2)value;
                return $"{v.x};{v.y}";
            }
            
            if (type == typeof(Vector3))
            {
                Vector3 v = (Vector3)value;
                return $"{v.x};{v.y};{v.z}";
            }
            
            if (type == typeof(Vector4))
            {
                Vector4 v = (Vector4)value;
                return $"{v.x};{v.y};{v.z};{v.w}";
            }
            
            if (type == typeof(Quaternion))
            {
                Quaternion q = (Quaternion)value;
                return $"{q.x};{q.y};{q.z};{q.w}";
            }
            
            if (type == typeof(Rect))
            {
                Rect r = (Rect)value;
                return $"{r.x};{r.y};{r.width};{r.height}";
            }
            
            if (type == typeof(Color))
            {
                Color c = (Color)value;
                return $"{c.r};{c.g};{c.b};{c.a}";
            }

            if (type == typeof(Gradient))
            {
                Gradient gradient = (Gradient)value;
                // Store color keys
                List<string> colorKeys = new List<string>();
                foreach (GradientColorKey key in gradient.colorKeys)
                {
                    colorKeys.Add($"{key.color.r};{key.color.g};{key.color.b};{key.color.a};{key.time}");
                }
                
                // Store alpha keys
                List<string> alphaKeys = new List<string>();
                foreach (GradientAlphaKey key in gradient.alphaKeys)
                {
                    alphaKeys.Add($"{key.alpha};{key.time}");
                }
                
                return $"{string.Join(ARRAY_SEPARATOR, colorKeys)}{TYPE_SEPARATOR}{string.Join(ARRAY_SEPARATOR, alphaKeys)}{TYPE_SEPARATOR}{(int)gradient.mode}";
            }
            
            if (type == typeof(AnimationCurve))
            {
                AnimationCurve curve = (AnimationCurve)value;
                // Store keys (time, value, inTangent, outTangent)
                List<string> keys = new List<string>();
                foreach (Keyframe key in curve.keys)
                {
                    keys.Add($"{key.time};{key.value};{key.inTangent};{key.outTangent};{key.inWeight};{key.outWeight};{(int)key.weightedMode}");
                }
                
                return $"{string.Join(ARRAY_SEPARATOR, keys)}{TYPE_SEPARATOR}{curve.preWrapMode}{TYPE_SEPARATOR}{curve.postWrapMode}";
            }
            
            // Handle Unity objects by storing reference ID
            if (typeof(Object).IsAssignableFrom(type))
            {
                Object unityObj = value as Object;
                if (unityObj == null)
                    return string.Empty;

                // Special handling for Sprites to store both Texture path and sprite name
                if (unityObj is Sprite sprite)
                {
                    string texturePath = UnityObjectToAssetIdentifier(sprite.texture, assetReferenceType);
                    return $"{texturePath}{ARRAY_SEPARATOR}{sprite.name}";
                }

                return UnityObjectToAssetIdentifier(unityObj, assetReferenceType);
            }

            // Handle lists and arrays
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as System.Collections.IList;
                var itemType = type.GetGenericArguments()[0];
                var items = new List<string>();

                foreach (var item in list)
                {
                    items.Add(GetValueAsString(item, itemType, assetReferenceType));
                }

                return "[" + string.Join(ARRAY_SEPARATOR, items) + "]";
            }

            if (type.IsArray)
            {
                var array = value as Array;
                var itemType = type.GetElementType();
                var items = new List<string>();

                foreach (var item in array)
                {
                    items.Add(GetValueAsString(item, itemType, assetReferenceType));
                }

                return "[" + string.Join(ARRAY_SEPARATOR, items) + "]";
            }

            // Handle other types by using ToString()
            return value.ToString();
        }

        private static string EscapeCSVCell(string cell, char delimiter)
        {
            if (string.IsNullOrEmpty(cell))
                return string.Empty;

            bool needsEscaping = cell.Contains(delimiter) || cell.Contains(ESCAPE_CHAR) || cell.Contains("\n");

            if (needsEscaping)
                return
                    $"{ESCAPE_CHAR}{cell.Replace(ESCAPE_CHAR.ToString(), ESCAPE_CHAR.ToString() + ESCAPE_CHAR.ToString())}{ESCAPE_CHAR}";

            return cell;
        }

        private static string[] ParseCSVLine(string line, char delimiter)
        {
            List<string> cells = new List<string>();
            StringBuilder currentCell = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == ESCAPE_CHAR)
                {
                    // Check if escaped quote (double quote)
                    if (i + 1 < line.Length && line[i + 1] == ESCAPE_CHAR)
                    {
                        currentCell.Append(ESCAPE_CHAR);
                        i++; // Skip next quote
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == delimiter && !insideQuotes)
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
                string[] nameTypePair = part.Split(new[] { TYPE_SEPARATOR }, StringSplitOptions.None);
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

        private static void SetValue<T>(T item, string propertyName, string typeName, string value, CSVAssetReferenceType assetReferenceType)
        {
            Type itemType = typeof(T);

            // Try to find and set the property/field through the inheritance chain
            if (!TrySetValueInTypeHierarchy(item, itemType, propertyName, typeName, value, assetReferenceType))
            {
                Debug.LogWarning($"Property or field {propertyName} not found on {itemType.Name} or any base class");
            }
        }

        private static bool TrySetValueInTypeHierarchy<T>(T item, Type currentType, string propertyName, string typeName, string value, CSVAssetReferenceType assetReferenceType)
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
                    object convertedValue = ConvertValueFromString(value, property.PropertyType, typeName, assetReferenceType);
                    property.SetValue(item, convertedValue);
                    return true;
                }

                // Try to find backing field for properties with [field: SerializeField]
                string backingFieldName = $"<{propertyName}>k__BackingField";
                FieldInfo backingField = currentType.GetField(backingFieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (backingField != null)
                {
                    object convertedValue = ConvertValueFromString(value, property.PropertyType, typeName, assetReferenceType);
                    backingField.SetValue(item, convertedValue);
                    return true;
                }

                // Try to use private setter
                MethodInfo setMethod = property.GetSetMethod(true); // Include non-public methods
                if (setMethod != null)
                {
                    object convertedValue = ConvertValueFromString(value, property.PropertyType, typeName, assetReferenceType);
                    setMethod.Invoke(item, new[] { convertedValue });
                    return true;
                }
            }

            // Try field in current type
            FieldInfo field = currentType.GetField(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (field != null)
            {
                object convertedValue = ConvertValueFromString(value, field.FieldType, typeName, assetReferenceType);
                field.SetValue(item, convertedValue);
                return true;
            }

            // Try in the base class
            return TrySetValueInTypeHierarchy(item, currentType.BaseType, propertyName, typeName, value, assetReferenceType);
        }

        private static object ConvertValueFromString(string value, Type targetType, string typeName, CSVAssetReferenceType assetReferenceType)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(targetType);

            // Determine the actual type from typeName if needed
            Type actualType = GetTypeFromName(typeName, targetType);
            if (actualType != null)
                targetType = actualType;

            // Handle Unity objects by resolving reference ID
            if (typeof(Object).IsAssignableFrom(targetType))
            {
                // Special handling for Sprites from sprite sheets
                if (targetType == typeof(Sprite) && value.Contains(ARRAY_SEPARATOR))
                {
                    string[] parts = value.Split(ARRAY_SEPARATOR);
                    if (parts.Length == 2)
                    {
                        return ResolveSprite(parts[0], parts[1], assetReferenceType);
                    }
                    return null;
                }

                return AssetIdentifierToUnityObject(value, targetType, assetReferenceType);
            }

            // Handle Vector2
            if (targetType == typeof(Vector2) || typeName == "Vector2")
            {
                string[] parts = value.Split(';');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y))
                    return new Vector2(x, y);
                return Vector2.zero;
            }

            // Handle Vector3
            if (targetType == typeof(Vector3) || typeName == "Vector3")
            {
                string[] parts = value.Split(';');
                if (parts.Length == 3 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z))
                    return new Vector3(x, y, z);
                return Vector3.zero;
            }

            // Handle Vector4
            if (targetType == typeof(Vector4) || typeName == "Vector4")
            {
                string[] parts = value.Split(';');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z) &&
                    float.TryParse(parts[3], out float w))
                    return new Vector4(x, y, z, w);
                return Vector4.zero;
            }

            // Handle Quaternion
            if (targetType == typeof(Quaternion) || typeName == "Quaternion")
            {
                string[] parts = value.Split(';');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z) &&
                    float.TryParse(parts[3], out float w))
                    return new Quaternion(x, y, z, w);
                return Quaternion.identity;
            }

            // Handle Rect
            if (targetType == typeof(Rect) || typeName == "Rect")
            {
                string[] parts = value.Split(';');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float width) &&
                    float.TryParse(parts[3], out float height))
                    return new Rect(x, y, width, height);
                return new Rect(0, 0, 0, 0);
            }

            // Handle Color
            if (targetType == typeof(Color) || typeName == "Color")
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

            // Handle Gradient
            if (targetType == typeof(Gradient) || typeName == "Gradient")
            {
                Gradient gradient = new Gradient();
                string[] sections = value.Split(TYPE_SEPARATOR);
                
                if (sections.Length >= 3)
                {
                    // Parse color keys
                    string[] colorKeysStr = sections[0].Split(ARRAY_SEPARATOR);
                    List<GradientColorKey> colorKeys = new List<GradientColorKey>();
                    
                    foreach (string keyStr in colorKeysStr)
                    {
                        string[] keyParts = keyStr.Split(';');
                        if (keyParts.Length == 5 &&
                            float.TryParse(keyParts[0], out float r) &&
                            float.TryParse(keyParts[1], out float g) &&
                            float.TryParse(keyParts[2], out float b) &&
                            float.TryParse(keyParts[3], out float a) &&
                            float.TryParse(keyParts[4], out float time))
                        {
                            colorKeys.Add(new GradientColorKey(new Color(r, g, b, a), time));
                        }
                    }
                    
                    // Parse alpha keys
                    string[] alphaKeysStr = sections[1].Split(ARRAY_SEPARATOR);
                    List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();
                    
                    foreach (string keyStr in alphaKeysStr)
                    {
                        string[] keyParts = keyStr.Split(';');
                        if (keyParts.Length == 2 &&
                            float.TryParse(keyParts[0], out float alpha) &&
                            float.TryParse(keyParts[1], out float time))
                        {
                            alphaKeys.Add(new GradientAlphaKey(alpha, time));
                        }
                    }
                    
                    // Parse mode
                    if (int.TryParse(sections[2], out int modeInt) && 
                        Enum.IsDefined(typeof(GradientMode), modeInt))
                    {
                        gradient.mode = (GradientMode)modeInt;
                    }
                    
                    gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
                }
                
                return gradient;
            }
            
            // Handle AnimationCurve
            if (targetType == typeof(AnimationCurve) || typeName == "AnimationCurve")
            {
                AnimationCurve curve = new AnimationCurve();
                string[] sections = value.Split(TYPE_SEPARATOR);
                
                if (sections.Length >= 3)
                {
                    // Parse keys
                    string[] keysStr = sections[0].Split(ARRAY_SEPARATOR);
                    List<Keyframe> keyframes = new List<Keyframe>();
                    
                    foreach (string keyStr in keysStr)
                    {
                        string[] keyParts = keyStr.Split(';');
                        if (keyParts.Length >= 4 &&
                            float.TryParse(keyParts[0], out float time) &&
                            float.TryParse(keyParts[1], out float keyValue) &&
                            float.TryParse(keyParts[2], out float inTangent) &&
                            float.TryParse(keyParts[3], out float outTangent))
                        {
                            Keyframe keyframe = new Keyframe(time, keyValue, inTangent, outTangent);
                            
                            if (keyParts.Length >= 7 &&
                                float.TryParse(keyParts[4], out float inWeight) &&
                                float.TryParse(keyParts[5], out float outWeight) &&
                                int.TryParse(keyParts[6], out int weightedModeInt) &&
                                Enum.IsDefined(typeof(WeightedMode), weightedModeInt))
                            {
                                keyframe.inWeight = inWeight;
                                keyframe.outWeight = outWeight;
                                keyframe.weightedMode = (WeightedMode)weightedModeInt;
                            }
                            
                            keyframes.Add(keyframe);
                        }
                    }
                    
                    // Set keys to curve
                    curve = new AnimationCurve(keyframes.ToArray());
                    
                    // Parse wrap modes
                    if (Enum.TryParse(sections[1], out WrapMode preWrapMode))
                    {
                        curve.preWrapMode = preWrapMode;
                    }
                    
                    if (Enum.TryParse(sections[2], out WrapMode postWrapMode))
                    {
                        curve.postWrapMode = postWrapMode;
                    }
                }
                
                return curve;
            }
            
            // Handle lists
            if ((targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>)) || 
                (typeName.StartsWith("List<") && typeName.EndsWith(">")))
            {
                if (!value.StartsWith("[") || !value.EndsWith("]"))
                    return GetDefaultValue(targetType);

                string content = value.Substring(1, value.Length - 2);
                string[] items = content.Split(ARRAY_SEPARATOR);

                // Try to get the item type from the type name if possible
                Type itemType;
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    itemType = targetType.GetGenericArguments()[0];
                }
                else
                {
                    // Extract item type from typeName (e.g., "List<Int>" -> "Int")
                    string itemTypeName = typeName.Substring(5, typeName.Length - 6);
                    itemType = GetTypeFromName(itemTypeName, typeof(object));
                    if (itemType == null)
                        return GetDefaultValue(targetType);
                }

                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = Activator.CreateInstance(listType) as System.Collections.IList;

                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        list.Add(ConvertValueFromString(item, itemType, GetTypeNameForHeader(itemType), assetReferenceType));
                    }
                }

                return list;
            }

            // Handle arrays
            if (targetType.IsArray || (typeName.EndsWith("[]")))
            {
                if (!value.StartsWith("[") || !value.EndsWith("]"))
                    return GetDefaultValue(targetType);

                string content = value.Substring(1, value.Length - 2);
                string[] items = content.Split(ARRAY_SEPARATOR);

                // Try to get the element type from the type name if possible
                Type elementType;
                if (targetType.IsArray)
                {
                    elementType = targetType.GetElementType();
                }
                else
                {
                    // Extract element type from typeName (e.g., "Int[]" -> "Int")
                    string elementTypeName = typeName.Substring(0, typeName.Length - 2);
                    elementType = GetTypeFromName(elementTypeName, typeof(object));
                    if (elementType == null)
                        return GetDefaultValue(targetType);
                }

                Array array = Array.CreateInstance(elementType, items.Length);

                for (int i = 0; i < items.Length; i++)
                {
                    if (!string.IsNullOrEmpty(items[i]))
                    {
                        array.SetValue(ConvertValueFromString(items[i], elementType, GetTypeNameForHeader(elementType), assetReferenceType), i);
                    }
                }

                return array;
            }

            // Handle basic types
            if (targetType == typeof(string) || typeName == "String")
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?) || typeName == "Int")
                return int.TryParse(value, out int intResult) ? intResult : 0;

            if (targetType == typeof(float) || targetType == typeof(float?) || typeName == "Float")
                return float.TryParse(value, out float floatResult) ? floatResult : 0f;

            if (targetType == typeof(double) || targetType == typeof(double?) || typeName == "Double")
                return double.TryParse(value, out double doubleResult) ? doubleResult : 0d;

            if (targetType == typeof(bool) || targetType == typeof(bool?) || typeName == "Bool")
                return bool.TryParse(value, out bool boolResult) && boolResult;

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?) || typeName == "DateTime")
                return DateTime.TryParse(value, out DateTime dateResult) ? dateResult : DateTime.MinValue;

            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out object enumResult)
                    ? enumResult
                    : Enum.GetValues(targetType).GetValue(0);

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
        
        private static Type GetTypeFromName(string typeName, Type fallbackType)
        {
            // Handle simple types
            switch (typeName)
            {
                case "String": return typeof(string);
                case "Int": return typeof(int);
                case "Float": return typeof(float);
                case "Double": return typeof(double);
                case "Bool": return typeof(bool);
                case "DateTime": return typeof(DateTime);
                case "Vector2": return typeof(Vector2);
                case "Vector3": return typeof(Vector3);
                case "Vector4": return typeof(Vector4);
                case "Quaternion": return typeof(Quaternion);
                case "Rect": return typeof(Rect);
                case "Color": return typeof(Color);
                case "Gradient": return typeof(Gradient);
                case "AnimationCurve": return typeof(AnimationCurve);
                case "Sprite": return typeof(Sprite);
                case "Texture2D": return typeof(Texture2D);
            }

            // Try to find the type by name in commonly used assemblies
            string[] namespaces = { "", "UnityEngine", "System", "System.Collections.Generic" };
            foreach (var ns in namespaces)
            {
                string fullTypeName = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
                Type foundType = Type.GetType(fullTypeName);
                if (foundType != null)
                    return foundType;
                
                // Try looking in common assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    foundType = assembly.GetType(fullTypeName);
                    if (foundType != null)
                        return foundType;
                }
            }

            // Handle generic lists
            if (typeName.StartsWith("List<") && typeName.EndsWith(">"))
            {
                string elementTypeName = typeName.Substring(5, typeName.Length - 6);
                Type elementType = GetTypeFromName(elementTypeName, null);
                if (elementType != null)
                {
                    return typeof(List<>).MakeGenericType(elementType);
                }
            }

            // Handle arrays
            if (typeName.EndsWith("[]"))
            {
                string elementTypeName = typeName.Substring(0, typeName.Length - 2);
                Type elementType = GetTypeFromName(elementTypeName, null);
                if (elementType != null)
                {
                    return elementType.MakeArrayType();
                }
            }

            return fallbackType;
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == null)
                return null;
                
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        private static string GetTypeNameForHeader(Type type)
        {
            if (type == typeof(string))
                return "String";
                
            if (type == typeof(int) || type == typeof(int?))
                return "Int";
                
            if (type == typeof(float) || type == typeof(float?))
                return "Float";
                
            if (type == typeof(double) || type == typeof(double?))
                return "Double";
                
            if (type == typeof(bool) || type == typeof(bool?))
                return "Bool";
                
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "DateTime";
                
            if (type.IsEnum)
                return type.Name;
                
            if (type == typeof(Vector2))
                return "Vector2";
                
            if (type == typeof(Vector3))
                return "Vector3";
                
            if (type == typeof(Vector4))
                return "Vector4";
                
            if (type == typeof(Quaternion))
                return "Quaternion";
                
            if (type == typeof(Rect))
                return "Rect";
                
            if (type == typeof(Color))
                return "Color";

            if (type == typeof(Gradient))
                return "Gradient";
                
            if (type == typeof(AnimationCurve))
                return "AnimationCurve";
            
            if (typeof(Object).IsAssignableFrom(type))
                return type.Name;

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

    public enum CSVDelimiterType
    {
        Comma = 0,
        Tab = 1,
    }
    
    public enum CSVAssetReferenceType
    {
        AssetPath = 0,
        GUID = 1,
    }
}