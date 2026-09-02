#if UNITY_6000_6_OR_NEWER
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Enhance
{
    public static class HierarchyComponentsColumn
    {
        private const string COLUMN_ID = "GameObject/Components";
        private const float ICON_SIZE = 16f;
        private const float ICON_SPACING = 2f;

        private static readonly List<Component> _components = new List<Component>();

        [HierarchyViewColumnDescriptor(COLUMN_ID)]
        private static void CreateColumn(HierarchyViewColumnDescriptor column)
        {
            column.Title = "Comp.";
            column.Tooltip = "Components attached to the GameObject";
            column.DefaultWidth = 50;
            column.DefaultVisibility = true;
            // 0 is the Name column, positive values place this column to the right of it
            column.DefaultPriority = 10;
        }

        [HierarchyViewCellDescriptor(COLUMN_ID, typeof(HierarchyGameObjectHandler))]
        private static void CreateGameObjectCell(HierarchyViewCellDescriptor cell)
        {
            // Keep the icons alive between binds instead of rebuilding them on every row recycle
            cell.ClearCellContent = false;
            cell.BindCell = BindCell;
        }

        private static void BindCell(HierarchyViewCell cell)
        {
            var gameObject = ((HierarchyGameObjectHandler)cell.Handler).GetGameObject(cell.Node);

            if (gameObject == null)
            {
                cell.IsDefaultValue = true;
                HideIconsFrom(cell, 0);
                return;
            }

            gameObject.GetComponents(_components);

            var iconCount = 0;

            // Index 0 is always the Transform or RectTransform, index 1 is already drawn as the row icon
            for (var i = 2; i < _components.Count; i++)
            {
                var component = _components[i];
                if (component == null) continue;

                var icon = EnhanceHierarchyIcon.GetComponentIcon(component);
                if (icon == null) continue;

                var image = iconCount < cell.childCount ? (Image)cell.ElementAt(iconCount) : CreateIcon(cell);
                image.image = icon;
                image.tooltip = component.GetType().Name;
                image.style.display = DisplayStyle.Flex;
                iconCount++;
            }

            _components.Clear();
            HideIconsFrom(cell, iconCount);

            // A cell left at the default value is only drawn while the row is hovered or selected
            cell.IsDefaultValue = iconCount == 0;
        }

        private static Image CreateIcon(HierarchyViewCell cell)
        {
            if (cell.childCount == 0)
            {
                cell.style.flexDirection = FlexDirection.Row;
                cell.style.alignItems = Align.Center;
                cell.style.overflow = Overflow.Hidden;
            }

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Position,
            };

            image.style.width = ICON_SIZE;
            image.style.height = ICON_SIZE;
            image.style.marginRight = ICON_SPACING;
            image.style.flexShrink = 0f;

            cell.Add(image);
            return image;
        }

        private static void HideIconsFrom(HierarchyViewCell cell, int index)
        {
            for (var i = index; i < cell.childCount; i++)
            {
                cell.ElementAt(i).style.display = DisplayStyle.None;
            }
        }
    }
}
#endif
