using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using DataKeeper.Editor.Utility;
using DataKeeper.Utility;
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

        private static object s_Buffer = null;

        private const float BUTTON_W = 18f;
        private const float BUTTON_H = 18f;
        private const float BUTTON_SPACING = 2f;

        private const float BAR_W = 2f;
        private const float FOOTER_SPACE = 10f;
        private const float FOLDOUT_ARROW_CENTER = -6f; // ~center of the foldout arrow from the indented left edge
        private const float BODY_TOP_PADDING = 0f; // gap below the header row before the bar starts

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
            bool hasValue = property.managedReferenceValue != null;
            Color accent = Color.gray;

            if (hasValue)
            {
                Type currentType = property.managedReferenceValue.GetType();
                currentTypeName = ObjectNames.NicifyVariableName(currentType.Name);
                currentIcon = ScriptIconCache.GetIcon(currentType);
                accent = RichText.TextToColor(currentType.Name);
            }

            // Accent bar running down from under the foldout chevron, only when unfolded.
            // position.x does NOT include the indent for a property drawer (indent is
            // applied inside EditorGUI calls via indentLevel), so use IndentedRect to get
            // the real left edge of the foldout block, then center the bar under the arrow.
            if (hasValue && property.isExpanded)
            {
                float chevronX = EditorGUI.IndentedRect(position).x;
                float barX = chevronX + FOLDOUT_ARROW_CENTER - BAR_W * 0.5f;
                float barY = position.y + EditorGUIUtility.singleLineHeight + BODY_TOP_PADDING;
                float barH = position.yMax - FOOTER_SPACE - barY;
                EditorGUI.DrawRect(new Rect(barX, barY, BAR_W, barH), accent);
            }

            Rect popupRect = EditorGUI.PrefixLabel(labelRect, GUIUtility.GetControlID(FocusType.Passive), label);
            popupRect.height = EditorGUIUtility.singleLineHeight;

            if (GUI.Button(popupRect, GUIContent.none, EditorStyles.popup))
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

            DrawPopupContent(popupRect, currentTypeName, currentIcon, hasValue);

            DrawBufferButtons(stripRect, property, baseType);

            if (hasValue)
                EditorGUI.PropertyField(position, property, GUIContent.none, true);
        }

        private static void DrawPopupContent(Rect rect, string typeName, Texture2D icon, bool hasValue)
        {
            const float pad = 4f;
            const float arrowReserve = 16f; // room for the popup's dropdown glyph on the right
            const float iconSize = 14f;

            float x = rect.x + pad;

            if (hasValue && icon != null)
            {
                Rect iconRect = new Rect(x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                x = iconRect.xMax + pad;
            }

            Rect textRect = new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x - arrowReserve), rect.height);
            EditorGUI.LabelField(textRect, typeName);
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

        // All SerializeReference-eligible concrete types in the domain, scanned once per
        // session (statics are cleared on domain reload / recompile, so this stays fresh).
        // Per-baseType results are just a cheap IsAssignableFrom filter over this pool.
        private static Type[] s_AllCandidates;

        private static Type[] GetAllCandidates()
        {
            if (s_AllCandidates != null) return s_AllCandidates;

            Type unityObject = typeof(UnityEngine.Object);

            s_AllCandidates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(type => type.IsClass                              // managed references are reference types
                               && !type.IsAbstract
                               && !type.IsGenericTypeDefinition
                               && !unityObject.IsAssignableFrom(type)    // SerializeReference cannot hold UnityEngine.Object
                               && type.GetConstructor(Type.EmptyTypes) != null) // Activator.CreateInstance needs a parameterless ctor
                .ToArray();

            return s_AllCandidates;
        }

        private Type[] GetValidTypes(Type baseType)
        {
            if (s_TypeCache.TryGetValue(baseType, out Type[] types)) return types;

            types = GetAllCandidates()
                .Where(baseType.IsAssignableFrom)
                .OrderBy(type => type.Name)
                .ToArray();

            s_TypeCache[baseType] = types;
            return types;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

            float height = EditorGUI.GetPropertyHeight(property, label, true);

            // Reserve trailing space for the separator only when unfolded.
            if (property.managedReferenceValue != null && property.isExpanded)
                height += FOOTER_SPACE;

            return height;
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

                var noNamespaceTypes = _validTypes.Where(t => string.IsNullOrEmpty(t.Namespace)).OrderBy(t => t.Name).ToList();
                if (noNamespaceTypes.Count > 0)
                {
                    var baseTypeItem = new AdvancedDropdownItem("Default");
                    root.AddChild(baseTypeItem);
                    foreach (var type in noNamespaceTypes)
                    {
                        baseTypeItem.AddChild(BuildTypeItem(type));
                    }
                }

                foreach (var ns in _validTypes.Where(t => !string.IsNullOrEmpty(t.Namespace)).GroupBy(t => t.Namespace).OrderBy(g => g.Key))
                {
                    var nsItem = new AdvancedDropdownItem(ns.Key);
                    root.AddChild(nsItem);
                    foreach (var type in ns.OrderBy(t => t.Name))
                    {
                        nsItem.AddChild(BuildTypeItem(type));
                    }
                }

                return root;
            }

            private static TypeDropdownItem BuildTypeItem(Type type)
            {
                var tdi = new TypeDropdownItem(ObjectNames.NicifyVariableName(type.Name), type);
                
                Texture2D currentIcon = ScriptIconCache.GetIcon(type);
                if(currentIcon != null)
                {
                    tdi.icon = currentIcon;
                }
                
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