using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Attributes;
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

        private const bool SHOW_INSPECTOR_ICON = true;
        private const bool SHOW_DROPDOWN_ICON = true;

        private static Dictionary<Type, Type[]> s_TypeCache = new Dictionary<Type, Type[]>();
        private static AdvancedDropdownState s_DropdownState = new AdvancedDropdownState();
        private static Dictionary<Type, Texture2D> s_IconCache = new Dictionary<Type, Texture2D>();
        private static readonly Queue<Type> s_IconQueue = new Queue<Type>();
        private static readonly HashSet<Type> s_IconPending = new HashSet<Type>();
        private static bool s_IconWorkerActive;
        private static Texture2D s_DefaultScriptIcon;
        private const int ICONS_PER_TICK = 6;

        // Raised after a batch of icons finishes resolving, so already-open UI (the
        // dropdown) can swap its placeholder icons for the real ones.
        private static event Action IconsResolved;

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
                currentIcon = GetScriptIcon(currentType);
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
                Type[] validTypes = GetValidTypes(baseType);
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

            if (SHOW_INSPECTOR_ICON && hasValue && icon != null)
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

        private static Texture2D GetScriptIcon(Type type)
        {
            if (type == null) return null;
            if (s_IconCache.TryGetValue(type, out Texture2D cached)) return cached;

            // Resolving touches AssetDatabase (main-thread only + slow per type), so defer it
            // across editor ticks and hand back a cheap placeholder for now.
            EnqueueIcon(type);
            return GetDefaultScriptIcon();
        }

        private static void EnqueueIcon(Type type)
        {
            if (!s_IconPending.Add(type)) return;
            s_IconQueue.Enqueue(type);

            if (!s_IconWorkerActive)
            {
                s_IconWorkerActive = true;
                EditorApplication.update += ProcessIconQueue;
            }
        }

        private static void ProcessIconQueue()
        {
            int budget = ICONS_PER_TICK;
            bool resolvedAny = false;

            while (budget-- > 0 && s_IconQueue.Count > 0)
            {
                Type type = s_IconQueue.Dequeue();
                s_IconPending.Remove(type);

                if (s_IconCache.ContainsKey(type)) continue;

                s_IconCache[type] = ResolveScriptIcon(type);
                resolvedAny = true;
            }

            if (resolvedAny)
            {
                IconsResolved?.Invoke();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

            if (s_IconQueue.Count == 0)
            {
                EditorApplication.update -= ProcessIconQueue;
                s_IconWorkerActive = false;
            }
        }

        private static Texture2D ResolveScriptIcon(Type type)
        {
            Texture2D icon = null;
            MonoScript script = FindMonoScript(type);
            if (script != null)
                icon = EditorGUIUtility.GetIconForObject(script) as Texture2D;

            return icon != null ? icon : GetDefaultScriptIcon();
        }

        private static Texture2D GetDefaultScriptIcon()
        {
            if (s_DefaultScriptIcon == null)
                s_DefaultScriptIcon = EditorGUIUtility.FindTexture("cs Script Icon")
                    ?? EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            return s_DefaultScriptIcon;
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

            // TypeCache keeps a prebuilt derivation index, so this is near-instant compared
            // to scanning every type in every loaded assembly by hand.
            List<Type> derivedTypes = new List<Type>();

            // GetTypesDerivedFrom excludes baseType itself; include it when it's instantiable.
            if (!baseType.IsAbstract && !baseType.IsInterface)
                derivedTypes.Add(baseType);

            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
            {
                if (!type.IsAbstract && !type.IsInterface)
                    derivedTypes.Add(type);
            }

            types = derivedTypes.OrderBy(type => type.Name).ToArray();
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
            private readonly List<TypeDropdownItem> _iconItems = new List<TypeDropdownItem>();

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

                var groupCache = new Dictionary<string, AdvancedDropdownItem>();

                foreach (var ns in _validTypes.Where(t => !string.IsNullOrEmpty(t.Namespace)).GroupBy(t => t.Namespace).OrderBy(g => g.Key))
                {
                    var nsItem = GetOrCreateNamespaceGroup(root, ns.Key, groupCache);
                    foreach (var type in ns.OrderBy(t => t.Name))
                    {
                        nsItem.AddChild(BuildTypeItem(type));
                    }
                }

                // Some icons may still be resolving in the background; refresh them live
                // while the window is open, then detach once everything is cached.
                if (_iconItems.Any(i => i.Type != null && !s_IconCache.ContainsKey(i.Type)))
                    IconsResolved += OnIconsResolved;

                return root;
            }

            private void OnIconsResolved()
            {
                bool allResolved = true;

                foreach (var item in _iconItems)
                {
                    if (item.Type == null) continue;

                    if (s_IconCache.TryGetValue(item.Type, out var icon))
                        item.icon = icon;
                    else
                        allResolved = false;
                }

                if (allResolved)
                    IconsResolved -= OnIconsResolved;
            }

            private static AdvancedDropdownItem GetOrCreateNamespaceGroup(AdvancedDropdownItem root, string ns, Dictionary<string, AdvancedDropdownItem> cache)
            {
                if (cache.TryGetValue(ns, out var existing))
                    return existing;

                string[] segments = ns.Split('.');
                AdvancedDropdownItem parent = root;
                string path = null;

                foreach (string segment in segments)
                {
                    path = path == null ? segment : path + "." + segment;

                    if (!cache.TryGetValue(path, out var node))
                    {
                        node = new AdvancedDropdownItem(segment);
                        parent.AddChild(node);
                        cache[path] = node;
                    }

                    parent = node;
                }

                return parent;
            }

            private TypeDropdownItem BuildTypeItem(Type type)
            {
                var tdi = new TypeDropdownItem(ObjectNames.NicifyVariableName(type.Name), type);

                if (SHOW_DROPDOWN_ICON)
                {
                    tdi.icon = GetScriptIcon(type);
                    _iconItems.Add(tdi);
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