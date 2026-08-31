using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // One instance per *visual* row, reused across binds by both the tree and the list — the
    // only per-bind work is three field assignments, no allocation.
    public sealed class CommanderItemRow : VisualElement
    {
        private readonly Image _icon;
        private readonly Label _name;
        private readonly Label _sub;

        public CommanderItemRow(bool withSubLabel = true)
        {
            AddToClassList("ac-row");
            pickingMode = PickingMode.Ignore;

            _icon = new Image { pickingMode = PickingMode.Ignore };
            _icon.AddToClassList("ac-row-icon");
            Add(_icon);

            _name = new Label { pickingMode = PickingMode.Ignore };
            _name.AddToClassList("ac-row-name");
            Add(_name);

            if (!withSubLabel) return;

            _sub = new Label { pickingMode = PickingMode.Ignore };
            _sub.AddToClassList("ac-row-sub");
            Add(_sub);
        }

        public void Bind(ICommanderItem item)
        {
            if (item == null)
            {
                Unbind();
                return;
            }

            _icon.image = item.Icon;
            _name.text = item.Name;
            if (_sub != null) _sub.text = item.SubLabel;

            EnableInClassList("ac-row--dim", item.Kind == CommanderItemKind.Placeholder);
        }

        public void Unbind()
        {
            _icon.image = null;
            _name.text = "";
            if (_sub != null) _sub.text = "";
        }
    }
}
