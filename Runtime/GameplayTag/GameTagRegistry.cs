using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameTagRegistry : ScriptableObject
{
    [SerializeField] private List<string> _tags = new();
    public IReadOnlyList<string> Tags => _tags;

    public bool IsValid(GameTag tag) =>
        _tags.Contains(tag.Value);

    public IEnumerable<string> GetChildren(string parent) =>
        _tags.Where(t => t.StartsWith(parent + GameTag.SEPARATOR));
}

[Serializable]
public struct GameTag : IEquatable<GameTag>
{
    public const string SEPARATOR = "/";
    
    [SerializeField] private string _value;
    public string Value => _value;

    public GameTag(string value) => _value = value;

    public bool MatchesParent(GameTag parent) =>
        _value == parent._value || _value.StartsWith(parent._value + SEPARATOR);

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