using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GameTagRegistry : ScriptableObject
{
    [SerializeField] private List<string> _tags = new();
    public IReadOnlyList<string> Tags => _tags;

    public bool IsValid(GameTag tag) =>
        _tags.Contains(tag.Value);

    public IEnumerable<string> GetChildren(string parent) =>
        _tags.Where(t => t.StartsWith(parent + GameTag.SEPARASTOR));
}

[Serializable]
public struct GameTag : IEquatable<GameTag>
{
    public const string SEPARASTOR = "/";
    
    [SerializeField] private string _value;
    public string Value => _value;

    public GameTag(string value) => _value = value;

    public bool MatchesParent(GameTag parent) =>
        _value == parent._value || _value.StartsWith(parent._value + SEPARASTOR);

    public bool Equals(GameTag other) => _value == other._value;
    public override string ToString() => _value ?? string.Empty;
}

[Serializable]
public class GameTagContainer
{
    [SerializeField] private List<GameTag> _tags = new();

    public bool HasTag(GameTag tag) => _tags.Contains(tag);

    public bool HasTagExact(GameTag tag) => _tags.Contains(tag);

    public bool HasAny(GameTagContainer other) =>
        _tags.Any(t => other._tags.Any(o => t.MatchesParent(o) || o.MatchesParent(t)));

    public bool HasAll(GameTagContainer required) =>
        required._tags.All(r => _tags.Any(t => t.MatchesParent(r)));

    public void AddTag(GameTag tag) { if (!_tags.Contains(tag)) _tags.Add(tag); }
    public void RemoveTag(GameTag tag) => _tags.Remove(tag);
}

#if UNITY_EDITOR

public class GameTagDropdown : UnityEditor.IMGUI.Controls.AdvancedDropdown
{
    private readonly GameTagRegistry _registry;
    public event Action<string> OnTagSelected;

    public GameTagDropdown(UnityEditor.IMGUI.Controls.AdvancedDropdownState state, GameTagRegistry registry)
        : base(state)
    {
        _registry = registry;
        minimumSize = new Vector2(250, 300);
    }

    protected override UnityEditor.IMGUI.Controls.AdvancedDropdownItem BuildRoot()
    {
        var root = new UnityEditor.IMGUI.Controls.AdvancedDropdownItem("Game Tags");
        var nodes = new Dictionary<string, UnityEditor.IMGUI.Controls.AdvancedDropdownItem>();

        foreach (var tag in _registry.Tags.OrderBy(t => t))
        {
            var parts = tag.Split(GameTag.SEPARASTOR);
            UnityEditor.IMGUI.Controls.AdvancedDropdownItem parent = root;

            for (int i = 0; i < parts.Length; i++)
            {
                var path = string.Join(GameTag.SEPARASTOR, parts[..( i + 1)]);
                if (!nodes.TryGetValue(path, out var node))
                {
                    // Leaf = selectable, intermediate = folder-like
                    bool isLeaf = (i == parts.Length - 1);
                    node = isLeaf
                        ? new TagDropdownItem(parts[i], tag)   // carries full tag value
                        : new UnityEditor.IMGUI.Controls.AdvancedDropdownItem(parts[i]);
                    nodes[path] = node;
                    parent.AddChild(node);
                }
                parent = node;
            }
        }
        return root;
    }

    protected override void ItemSelected(UnityEditor.IMGUI.Controls.AdvancedDropdownItem item)
    {
        if (item is TagDropdownItem tagItem)
            OnTagSelected?.Invoke(tagItem.TagValue);
    }
}

public class TagDropdownItem : UnityEditor.IMGUI.Controls.AdvancedDropdownItem
{
    public string TagValue { get; }
    public TagDropdownItem(string label, string tagValue) : base(label)
        => TagValue = tagValue;
}

[CustomPropertyDrawer(typeof(GameTag))]
public class GameTagDrawer : PropertyDrawer
{
    private const string Content = "(none)";
    private static GameTagRegistry _registry;

    static GameTagDrawer()
    {
        _registry = Resources.Load<GameTagRegistry>("GameplayTagRegistry");

#if UNITY_EDITOR
        if (_registry == null)
        {
            _registry = ScriptableObject.CreateInstance<GameTagRegistry>();

            const string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
                AssetDatabase.CreateFolder("Assets", "Resources");

            AssetDatabase.CreateAsset(_registry, $"{resourcesPath}/GameplayTagRegistry.asset");
            AssetDatabase.SaveAssets();
        }
#endif
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProp = property.FindPropertyRelative("_value");

        var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
            position.width - EditorGUIUtility.labelWidth, position.height);

        EditorGUI.LabelField(labelRect, label);

        string current = valueProp.stringValue;
        if (GUI.Button(buttonRect, string.IsNullOrEmpty(current) ? Content : current,
                EditorStyles.popup))
        {
            var dropdown = new GameTagDropdown(new UnityEditor.IMGUI.Controls.AdvancedDropdownState(), _registry);
            dropdown.OnTagSelected += selected =>
            {
                valueProp.stringValue = selected;
                property.serializedObject.ApplyModifiedProperties();
            };
            dropdown.Show(buttonRect);
        }
    }
}
#endif
