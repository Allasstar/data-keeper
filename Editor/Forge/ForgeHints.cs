using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Forge
{
    // Hover hints for the status bar. Lookup walks up from the hovered element, so a hint on a
    // container covers every child without one of its own.
    public static class ForgeHints
    {
        private sealed class Hint
        {
            public readonly string Name;
            public readonly string Text;

            public Hint(string name, string text)
            {
                Name = name;
                Text = text;
            }
        }

        private static readonly ConditionalWeakTable<VisualElement, Hint> Hints = new();

        public static T Set<T>(T element, string name, string text) where T : VisualElement
        {
            Hints.Remove(element);
            Hints.Add(element, new Hint(name, text));
            return element;
        }

        public static T Set<T>(T element, HelpTopic topic, string item) where T : VisualElement =>
            Set(element, item, topic.TextOf(item));

        public static bool TryFind(VisualElement from, out string name, out string text)
        {
            for (var current = from; current != null; current = current.parent)
            {
                if (!Hints.TryGetValue(current, out var hint)) continue;
                name = hint.Name;
                text = hint.Text;
                return true;
            }

            name = null;
            text = null;
            return false;
        }
    }
}
