using DataKeeper.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DataKeeper.Editor
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>))]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        const float foldoutHeaderHeight = 20f;
        const float arraySizeWidth = 48f;

        bool _initialized;
        ReorderableList _list;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_initialized)
            {
                _initialized = true;
                Init(property);
            }

            float height = base.GetPropertyHeight(property, label);
            if (property.isExpanded)
                height += _list.GetHeight();
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var foldoutRect = new Rect(position) { height = foldoutHeaderHeight };
            prop.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, prop.isExpanded, prop.displayName);
            EditorGUI.EndFoldoutHeaderGroup();

            var arraySizeRect = new Rect()
            {
                x = foldoutRect.x + foldoutRect.width - arraySizeWidth,
                y = foldoutRect.y,
                width = arraySizeWidth,
                height = EditorGUIUtility.singleLineHeight
            };
            var pairs = prop.FindPropertyRelative("_pairs");
            GUI.enabled = false;
            EditorGUI.IntField(arraySizeRect, pairs.arraySize);
            GUI.enabled = true;

            position.y += foldoutHeaderHeight;
            if (prop.isExpanded)
                _list.DoList(position);
        }

        void Init(SerializedProperty prop)
        {
            var keysType = fieldInfo.FieldType.GetGenericArguments()[0];
            bool isEnum = keysType.IsEnum;

            if (isEnum)
                InitEnumKeys(prop, keysType);

            _list = new ReorderableList(prop.serializedObject, prop.FindPropertyRelative("_pairs"),
                draggable: !isEnum,
                displayHeader: true,
                displayAddButton: !isEnum,
                displayRemoveButton: !isEnum)
            {
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                drawElementBackgroundCallback = DrawElementBackground,
                elementHeightCallback = GetElementHeight,
            };
        }

        void InitEnumKeys(SerializedProperty prop, System.Type keysType)
        {
            var pairs = prop.FindPropertyRelative("_pairs");
            var enumValues = keysType.GetEnumValues();

            if (pairs.arraySize == enumValues.Length)
                return;

            pairs.ClearArray();
            foreach (int val in enumValues)
            {
                pairs.InsertArrayElementAtIndex(pairs.arraySize);
                var key = pairs.GetArrayElementAtIndex(pairs.arraySize - 1).FindPropertyRelative("key");
                key.enumValueIndex = val;
            }
            pairs.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        void DrawHeader(Rect rect)
        {
            var keysRect = new Rect(rect)
            {
                x = rect.x + 15,
                width = rect.width * .25f
            };

            var valuesRect = new Rect(keysRect)
            {
                x = keysRect.x + keysRect.width + 20
            };

            EditorGUI.LabelField(keysRect, new GUIContent("Dictionary"));
            // EditorGUI.LabelField(valuesRect, new GUIContent("Values"));
        }

        float GetElementHeight(int index)
        {
            if (_list.count == 0)
                return 0;

            var pair = _list.serializedProperty.GetArrayElementAtIndex(index);
            var key = pair.FindPropertyRelative("key");
            var value = pair.FindPropertyRelative("value");

            return EditorGUI.GetPropertyHeight(key) + EditorGUI.GetPropertyHeight(value) + EditorGUIUtility.standardVerticalSpacing;
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (_list.count == 0)
                return;

            var pair = _list.serializedProperty.GetArrayElementAtIndex(index);
            var key = pair.FindPropertyRelative("key");
            var value = pair.FindPropertyRelative("value");

            if (key == null || value == null)
            {
                Debug.LogError("Error displaying dictionary data. Make sure you are using a [Serializable] data type!");
                return;
            }

            if (!_list.draggable)
                rect.xMin += 15f;

            var keyRect = new Rect(rect)
            {
                height = EditorGUI.GetPropertyHeight(key)
            };

            var valueRect = new Rect(rect)
            {
                y = keyRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                height = EditorGUI.GetPropertyHeight(value)
            };

            if (value.hasVisibleChildren)
                valueRect.xMin += 15f;

            // Draw key
            EditorGUI.LabelField(keyRect, "Key:");
            keyRect.xMin += 30f;
            if (key.propertyType == SerializedPropertyType.Enum)
                EditorGUI.LabelField(keyRect, key.enumDisplayNames[key.enumValueIndex]);
            else
                EditorGUI.PropertyField(keyRect, key, GUIContent.none, true);

            // Draw value
            EditorGUI.PropertyField(valueRect, value, new GUIContent("Value"), true);
        }

        void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (Event.current.type != EventType.Repaint || index == -1)
                return;

            var pairs = _list.serializedProperty;
            var key = pairs.GetArrayElementAtIndex(index).FindPropertyRelative("key");

            for (int i = 0; i < index; i++)
            {
                var otherKey = pairs.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                if (SerializedProperty.DataEquals(key, otherKey))
                {
                    // same key, draw red background
                    EditorGUI.DrawRect(rect, isFocused ? Color.HSVToRGB(0, 1, 1) : Color.HSVToRGB(0, 1, 0.7f));
                    return;
                }
            }

            // draw usual background
            GUIStyle s = "RL Element";
            s.Draw(rect, isHover: false, isActive, isActive, isFocused);
        }
    }
}
