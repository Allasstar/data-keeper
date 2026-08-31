using System;
using System.Collections.Generic;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The search box parsed once per keystroke instead of per row: whitespace-separated terms,
    // all of which must match, where "t:" makes a term a type test and a term containing * or ?
    // is a glob over the name. Everything else is a case-insensitive substring.
    public sealed class SearchFilter
    {
        public static readonly SearchFilter Empty = new SearchFilter(Array.Empty<Term>(), "");

        private readonly Term[] _terms;

        private SearchFilter(Term[] terms, string text)
        {
            _terms = terms;
            Text = text;
        }

        public string Text { get; }

        public bool IsEmpty => _terms.Length == 0;

        public static SearchFilter Parse(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Empty;

            var pieces = query.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var terms = new List<Term>(pieces.Length);

            foreach (var piece in pieces)
            {
                if (piece.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
                {
                    var type = piece.Substring(2);
                    if (type.Length > 0) terms.Add(new Term(type, true));
                    continue;
                }

                terms.Add(new Term(piece, false));
            }

            return terms.Count == 0 ? Empty : new SearchFilter(terms.ToArray(), query);
        }

        public bool Matches(ICommanderItem item)
        {
            if (_terms.Length == 0) return true;
            if (item == null) return false;

            foreach (var term in _terms)
            {
                bool hit = term.IsType ? MatchesType(item, term.Value) : term.MatchesName(item.Name);
                if (!hit) return false;
            }

            return true;
        }

        // Type tests are answered from what the item already knows — an AssetDatabase type
        // lookup per row would be an editor call on every keystroke.
        private static bool MatchesType(ICommanderItem item, string type)
        {
            switch (item)
            {
                case AssetItem asset:
                    return MatchesAssetType(asset, type);
                case GameObjectItem gameObject:
                    return MatchesComponentType(gameObject, type);
                case ComponentItem component:
                    return Contains(component.Name, type) || Contains(component.SubLabel, type);
                default:
                    return false;
            }
        }

        private static bool MatchesAssetType(AssetItem item, string type)
        {
            if (item.Kind == CommanderItemKind.Folder) return Contains("folder", type);
            if (item.Kind == CommanderItemKind.Scene && Contains("scene", type)) return true;

            var path = item.AssetPath;
            int dot = path.LastIndexOf('.');
            if (dot >= 0 && Contains(path.Substring(dot + 1), type)) return true;

            var kind = AssetKinds.FromPath(path);
            return kind != AssetKind.Unknown && Contains(kind.ToString(), type);
        }

        private static bool MatchesComponentType(GameObjectItem item, string type)
        {
            var gameObject = item.GameObject;
            if (gameObject == null) return false;

            int count = gameObject.GetComponentCount();
            for (int i = 0; i < count; i++)
            {
                var component = gameObject.GetComponentAtIndex(i);
                if (component != null && Contains(component.GetType().Name, type)) return true;
            }

            return false;
        }

        private static bool Contains(string haystack, string needle) =>
            !string.IsNullOrEmpty(haystack)
            && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private readonly struct Term
        {
            public readonly string Value;
            public readonly bool IsType;

            private readonly bool _isGlob;

            public Term(string value, bool isType)
            {
                Value = value;
                IsType = isType;
                _isGlob = !isType && (value.IndexOf('*') >= 0 || value.IndexOf('?') >= 0);
            }

            public bool MatchesName(string name) =>
                _isGlob ? GlobMatches(name, Value) : Contains(name, Value);
        }

        // Iterative rather than recursive, with a single backtrack point per '*' — a name is
        // short, but a pattern like "*a*a*a*" would otherwise fan out exponentially.
        private static bool GlobMatches(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text)) return pattern == "*";

            int t = 0, p = 0, star = -1, mark = 0;

            while (t < text.Length)
            {
                if (p < pattern.Length && (pattern[p] == '?' || SameChar(pattern[p], text[t])))
                {
                    t++;
                    p++;
                    continue;
                }

                if (p < pattern.Length && pattern[p] == '*')
                {
                    star = p++;
                    mark = t;
                    continue;
                }

                if (star < 0) return false;

                p = star + 1;
                t = ++mark;
            }

            while (p < pattern.Length && pattern[p] == '*') p++;

            return p == pattern.Length;
        }

        private static bool SameChar(char a, char b) => char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
    }
}
