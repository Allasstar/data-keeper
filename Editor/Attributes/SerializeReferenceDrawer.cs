using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DataKeeper.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(SerializeReferenceSelectorAttribute))]
    public class SerializeReferenceDrawer : PropertyDrawer
    {
        private const string NULL_TYPE_NAME = "- null -";
        private const string MANAGED_REFERENCE = "managedReference";

        // ── Static caches ────────────────────────────────────────────────────────
        private static Dictionary<Type, Type[]> s_TypeCache = new Dictionary<Type, Type[]>();
        private static AdvancedDropdownState s_DropdownState = new AdvancedDropdownState();
        private static Dictionary<Type, Texture2D> s_IconCache = new Dictionary<Type, Texture2D>();

        // ── Copy / Paste / Swap buffer ───────────────────────────────────────────
        private static object s_Buffer = null;

        // Button dimensions
        private const float BUTTON_W = 18f;
        private const float BUTTON_H = 18f;
        private const float BUTTON_SPACING = 2f;

        // ── Main draw ────────────────────────────────────────────────────────────
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

            // ── Header row ───────────────────────────────────────────────────────
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Buffer button strip (right side, inside header)
            float stripWidth = (BUTTON_W + BUTTON_SPACING) * 2 - BUTTON_SPACING;
            Rect stripRect = new Rect(
                headerRect.xMax - stripWidth,
                headerRect.y + (headerRect.height - BUTTON_H) * 0.5f,
                stripWidth,
                BUTTON_H);

            // Shrink the popup area so it doesn't overlap buttons
            Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - stripWidth - 4f, headerRect.height);

            // ── Type popup ───────────────────────────────────────────────────────
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

            // ── Buffer buttons ───────────────────────────────────────────────────
            DrawBufferButtons(stripRect, property, baseType);

            // ── Child fields ─────────────────────────────────────────────────────
            if (property.managedReferenceValue != null)
                EditorGUI.PropertyField(position, property, GUIContent.none, true);
        }

        // ─── Buffer button strip ─────────────────────────────────────────────────

        private static void DrawBufferButtons(Rect strip, SerializedProperty property, Type baseType)
        {
            object current = property.managedReferenceValue;
            bool hasBuffer = s_Buffer != null;
            bool bufferCompatible = hasBuffer && baseType.IsAssignableFrom(s_Buffer.GetType());
            bool hasCurrent = current != null;

            // Copy
            Rect btnRect = new Rect(strip.x, strip.y, BUTTON_W, BUTTON_H);
            using (new EditorGUI.DisabledScope(!hasCurrent))
            {
                GUIContent copyLabel = MakeLabel("C", GetBufferStatusTooltip(current, s_Buffer));
                if (GUI.Button(btnRect, copyLabel, MiniButtonStyle()))
                {
                    s_Buffer = DeepClone(current);
                }
            }

            // Paste
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

            // Swap
            // btnRect.x += BUTTON_W + BUTTON_SPACING;
            // bool canSwap = hasCurrent && bufferCompatible;
            // using (new EditorGUI.DisabledScope(!canSwap))
            // {
            //     string swapTooltip = canSwap
            //         ? $"Swap '{ObjectNames.NicifyVariableName(current.GetType().Name)}' ↔ '{ObjectNames.NicifyVariableName(s_Buffer.GetType().Name)}'"
            //         : "Need both a current value and a compatible buffer to swap";
            //
            //     if (GUI.Button(btnRect, MakeLabel("S", swapTooltip), MiniButtonStyle()))
            //     {
            //         string curName  = ObjectNames.NicifyVariableName(current.GetType().Name);
            //         string bufName  = ObjectNames.NicifyVariableName(s_Buffer.GetType().Name);
            //         object temp     = DeepClone(s_Buffer);
            //         s_Buffer        = DeepClone(current);
            //         ApplyToAllTargets(property, () => DeepClone(temp), $"Swap SerializeReference ({curName} ↔ {bufName})");
            //     }
            // }

            // Buffer indicator dot — shows a colored dot when buffer holds something
            if (hasBuffer)
            {
                Rect dotRect = new Rect(strip.xMax + 4f, strip.y + strip.height * 0.5f - 3f, 6f, 6f);
                Color dotColor = bufferCompatible ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.9f, 0.6f, 0.2f);
                EditorGUI.DrawRect(dotRect, dotColor);
                if (dotRect.Contains(Event.current.mousePosition))
                {
                    string bufferTypeName = ObjectNames.NicifyVariableName(s_Buffer.GetType().Name);
                    GUI.Label(new Rect(dotRect.x - 80f, dotRect.y - 20f, 120f, 18f),
                        new GUIContent("", $"Buffer: {bufferTypeName}"));
                }
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a value factory to all target objects with full Undo support.
        /// Records BEFORE the change so Ctrl+Z restores the previous managed reference.
        /// </summary>
        private static void ApplyToAllTargets(SerializedProperty property, Func<object> valueFactory, string undoLabel)
        {
            // 1. Record all targets BEFORE mutation so Undo captures the old state.
            Undo.RecordObjects(property.serializedObject.targetObjects, undoLabel);

            foreach (var target in property.serializedObject.targetObjects)
            {
                // 2. Reuse (or create) a SerializedObject for this target.
                //    Using the property's own SO for the primary target avoids double-apply.
                SerializedObject so = target == property.serializedObject.targetObject
                    ? property.serializedObject
                    : new SerializedObject(target);

                SerializedProperty prop = so.FindProperty(property.propertyPath);
                prop.managedReferenceValue = valueFactory();

                // 3. ApplyModifiedProperties registers the change with Unity's undo stack
                //    because Undo.RecordObjects already snapshotted the object above.
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>Deep-clone via JSON round-trip (Unity's built-in serializer path).</summary>
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
            return $"Copy '{cur}' to buffer  (buffer: {buf})";
        }

        private static GUIContent MakeLabel(string text, string tooltip = "")
            => new GUIContent(text, tooltip);

        private static GUIStyle MiniButtonStyle()
        {
            var style = new GUIStyle(EditorStyles.miniButton);
            style.fontSize = 9;
            style.fixedHeight = BUTTON_H;
            return style;
        }

        // ─── Script icon helpers ─────────────────────────────────────────────────

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

        // ─── Type collection ─────────────────────────────────────────────────────

        private Type[] GetValidTypes(Type baseType)
        {
            if (s_TypeCache.TryGetValue(baseType, out Type[] types)) return types;

            List<Type> derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch (Exception) { return Type.EmptyTypes; }
                })
                .Where(type => baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .OrderBy(type => type.Name)
                .ToList();

            s_TypeCache[baseType] = derivedTypes.ToArray();
            return s_TypeCache[baseType];
        }

        // ─── Height ──────────────────────────────────────────────────────────────

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUI.GetPropertyHeight(property, GUIContent.none, true);

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        // ─── AdvancedDropdown ────────────────────────────────────────────────────

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
                => new TypeDropdownItem(ObjectNames.NicifyVariableName(type.Name), type);

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