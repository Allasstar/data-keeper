using System.Collections.Generic;
using DataKeeper.Signals;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // One per side. Commands (Phase 6) read the active side's list; the window reads Describe()
    // for the command-bar footer.
    public sealed class CommanderSelection
    {
        public readonly SideId Side;
        public readonly Signal OnChanged = new Signal();

        private readonly List<ICommanderItem> _items = new List<ICommanderItem>();

        public CommanderSelection(SideId side)
        {
            Side = side;
        }

        public IReadOnlyList<ICommanderItem> Items => _items;
        public int Count => _items.Count;
        public ICommanderItem First => _items.Count > 0 ? _items[0] : null;

        public void Set(IEnumerable<object> data)
        {
            _items.Clear();

            if (data != null)
            {
                foreach (var entry in data)
                {
                    if (entry is ICommanderItem item && item.Kind != CommanderItemKind.Placeholder)
                        _items.Add(item);
                }
            }

            OnChanged.Invoke();
        }

        public void Clear()
        {
            if (_items.Count == 0) return;

            _items.Clear();
            OnChanged.Invoke();
        }

        public string Describe()
        {
            if (_items.Count == 0) return "Nothing selected";
            if (_items.Count == 1) return $"{Side}: {_items[0].Name}";

            return $"{Side}: {_items.Count} selected";
        }
    }
}
