using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(SerializeReferenceSelectorAttribute))]
    public class SerializeReferenceSelectorDrawer : PropertyDrawer
    {
        private const string NULL_TYPE_NAME = "- null -";
        private const string MANAGED_REFERENCE = "managedReference";

        private static Dictionary<Type, Type[]> s_TypeCache = new Dictionary<Type, Type[]>();
        private static AdvancedDropdownState s_DropdownState = new AdvancedDropdownState();
        private static Dictionary<Type, Texture2D> s_IconCache = new Dictionary<Type, Texture2D>();

        private static object s_Buffer = null;

        private const float BUTTON_W = 18f;
        private const float BUTTON_H = 18f;
        private const float BUTTON_SPACING = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                DrawSerializeReferenceField(position, property, label);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private void DrawSerializeReferenceField(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializeReferenceSelectorAttribute attribute = (SerializeReferenceSelectorAttribute)this.attribute;

            Type baseType = attribute.BaseType ?? fieldInfo.FieldType;

            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
                baseType = baseType.GetGenericArguments()[0];
            else if (baseType.IsArray)
                baseType = baseType.GetElementType();

            Type[] validTypes = GetValidTypes(baseType);

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            float stripWidth = (BUTTON_W + BUTTON_SPACING) * 2 - BUTTON_SPACING;
            Rect stripRect = new Rect(
                headerRect.xMax - stripWidth,
                headerRect.y + (headerRect.height - BUTTON_H) * 0.5f,
                stripWidth,
                BUTTON_H);

            Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - stripWidth - 4f, headerRect.height);

            string currentTypeName = NULL_TYPE_NAME;
            Texture2D currentIcon = null;

            if (property.managedReferenceValue != null)
            {
                Type currentType = property.managedReferenceValue.GetType();
                currentTypeName = ObjectNames.NicifyVariableName(currentType.Name);
                currentIcon = GetScriptIcon(currentType);
            }

            Rect popupRect = EditorGUI.PrefixLabel(labelRect, GUIUtility.GetControlID(FocusType.Passive), label);
            popupRect.height = EditorGUIUtility.singleLineHeight;

            GUIContent buttonContent = currentIcon != null
                ? new GUIContent($" {currentTypeName}", currentIcon)
                : new GUIContent($"{currentTypeName}");

            if (GUI.Button(popupRect, buttonContent, EditorStyles.popup))
            {
                var dropdown = new TypeDropdown(s_DropdownState, validTypes, baseType, selectedType =>
                {
                    string undoLabel = selectedType == null
                        ? "Clear SerializeReference"
                        : $"Set SerializeReference to {selectedType.Name}";

                    ApplyToAllTargets(property,
                        selectedType == null ? (Func<object>)(() => null) : () => Activator.CreateInstance(selectedType),
                        undoLabel);
                });
                dropdown.Show(popupRect);
            }

            DrawBufferButtons(stripRect, property, baseType);

            if (property.managedReferenceValue != null)
                EditorGUI.PropertyField(position, property, GUIContent.none, true);
        }

        private static void DrawBufferButtons(Rect strip, SerializedProperty property, Type baseType)
        {
            object current = property.managedReferenceValue;
            bool hasBuffer = s_Buffer != null;
            bool bufferCompatible = hasBuffer && baseType.IsAssignableFrom(s_Buffer.GetType());
            bool hasCurrent = current != null;

            Rect btnRect = new Rect(strip.x, strip.y, BUTTON_W, BUTTON_H);

            using (new EditorGUI.DisabledScope(!hasCurrent))
            {
                GUIContent copyLabel = MakeLabel("C", GetBufferStatusTooltip(current, s_Buffer));
                if (GUI.Button(btnRect, copyLabel, MiniButtonStyle()))
                {
                    s_Buffer = DeepClone(current);
                }
            }

            btnRect.x += BUTTON_W + BUTTON_SPACING;

            using (new EditorGUI.DisabledScope(!bufferCompatible))
            {
                string pasteTooltip = bufferCompatible
                    ? $"Paste: {ObjectNames.NicifyVariableName(s_Buffer.GetType().Name)}"
                    : hasBuffer ? $"Buffer type '{s_Buffer.GetType().Name}' is not compatible" : "Buffer is empty";

                if (GUI.Button(btnRect, MakeLabel("P", pasteTooltip), MiniButtonStyle()))
                {
                    string pasteName = ObjectNames.NicifyVariableName(s_Buffer.GetType().Name);
                    ApplyToAllTargets(property, () => DeepClone(s_Buffer), $"Paste SerializeReference ({pasteName})");
                }
            }
        }

        private static void ApplyToAllTargets(SerializedProperty property, Func<object> valueFactory, string undoLabel)
        {
            var targets = property.serializedObject.targetObjects;

            // Undo (correct place)
            Undo.RecordObjects(targets, undoLabel);

            foreach (var target in targets)
            {
                SerializedObject so = target == property.serializedObject.targetObject
                    ? property.serializedObject
                    : new SerializedObject(target);

                so.Update();

                SerializedProperty prop = so.FindProperty(property.propertyPath);
                prop.managedReferenceValue = valueFactory();

                so.ApplyModifiedProperties();

                // Prefab support
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);

                // Mark dirty (THIS fixes save prompt)
                EditorUtility.SetDirty(target);

                // Scene dirty (for scene objects only)
                if (target is Component component && !EditorUtility.IsPersistent(component))
                {
                    EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
                }
                else if (target is GameObject go && !EditorUtility.IsPersistent(go))
                {
                    EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }
        }

        private static object DeepClone(object source)
        {
            if (source == null) return null;
            string json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson(json, source.GetType());
        }
        
        private static string GetBufferStatusTooltip(object current, object buffer)
        {
            string cur = current != null ? ObjectNames.NicifyVariableName(current.GetType().Name) : "null";
            string buf = buffer != null ? ObjectNames.NicifyVariableName(buffer.GetType().Name) : "empty";
            return $"Copy '{cur}' to buffer\n(buffer: {buf})";
        }

        private static GUIContent MakeLabel(string text, string tooltip = "") => new GUIContent(text, tooltip);
        
        private static GUIStyle MiniButtonStyle()
        {
            var style = new GUIStyle(EditorStyles.miniButton);
            style.fontSize = 9;
            style.fixedHeight = BUTTON_H;
            return style;
        }

        private static Texture2D GetScriptIcon(Type type)
        {
            if (type == null) return null;
            if (s_IconCache.TryGetValue(type, out Texture2D cached)) return cached;

            Texture2D icon = null;
            MonoScript script = FindMonoScript(type);
            if (script != null)
                icon = EditorGUIUtility.GetIconForObject(script) as Texture2D;

            if (icon == null)
                icon = EditorGUIUtility.FindTexture("cs Script Icon")
                    ?? EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            s_IconCache[type] = icon;
            return icon;
        }

        private static MonoScript FindMonoScript(Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type) return script;
            }
            return null;
        }

        private Type[] GetValidTypes(Type baseType)
        {
            if (s_TypeCache.TryGetValue(baseType, out Type[] types)) return types;

            List<Type> derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(type => baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.Name)
                .ToList();

            s_TypeCache[baseType] = derivedTypes.ToArray();
            return s_TypeCache[baseType];
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private class TypeDropdown : AdvancedDropdown
        {
            private readonly Type[] _validTypes;
            private readonly Type _baseType;
            private readonly Action<Type> _onSelected;

            public TypeDropdown(AdvancedDropdownState state, Type[] validTypes, Type baseType, Action<Type> onSelected)
                : base(state)
            {
                _validTypes = validTypes;
                _baseType = baseType;
                _onSelected = onSelected;
                minimumSize = new Vector2(200, 200);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Types");
                root.AddChild(new TypeDropdownItem(NULL_TYPE_NAME, null));

                foreach (var type in _validTypes.Where(t => string.IsNullOrEmpty(t.Namespace)).OrderBy(t => t.Name))
                    root.AddChild(BuildTypeItem(type));

                foreach (var ns in _validTypes.Where(t => !string.IsNullOrEmpty(t.Namespace))
                             .GroupBy(t => t.Namespace).OrderBy(g => g.Key))
                {
                    var nsItem = new AdvancedDropdownItem(ns.Key);
                    root.AddChild(nsItem);
                    foreach (var type in ns.OrderBy(t => t.Name))
                        nsItem.AddChild(BuildTypeItem(type));
                }

                return root;
            }

            private static TypeDropdownItem BuildTypeItem(Type type)
            {
                Texture2D currentIcon = GetScriptIcon(type);
                GUIContent buttonContent = currentIcon != null
                    ? new GUIContent(currentIcon)
                    : new GUIContent();
                
                var tdi = new TypeDropdownItem(ObjectNames.NicifyVariableName(type.Name), type);
                tdi.icon = buttonContent.image as Texture2D;
                
                return tdi;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is TypeDropdownItem typeItem)
                    _onSelected?.Invoke(typeItem.Type);
            }
        }

        private class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }
            public TypeDropdownItem(string displayName, Type type) : base(displayName) => Type = type;
        }
    }
}