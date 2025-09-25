using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit.Elements
{
    [UxmlElement]
    public partial class FlexBox : VisualElement
    {
        private FlexDirection _childDirection = FlexDirection.Column;
        [UxmlAttribute("childDirection")]
        public FlexDirection ChildDirection
        {
            get => _childDirection;
            set
            {
                _childDirection = value;
                this.SetFlexDirection(_childDirection);
                RefreshGap();
            }
        }

        private float _childGap = 0f;
        [UxmlAttribute("childGap")]
        public float ChildGap
        {
            get => _childGap;
            set
            {
                _childGap = value;
                RefreshGap();
            }
        }

        public FlexBox()
        {
            this.SetFlexDirection(_childDirection);
            this.SetOnGeometryChanged(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            RefreshGap();
        }

        /// <summary>
        /// Refreshes the gap by scheduling the application for the next frame
        /// </summary>
        public void RefreshGap()
        {
            schedule.Execute(ApplyGap).ExecuteLater(0);
        }

        /// <summary>
        /// Applies margin between children according to current Direction and Gap.
        /// </summary>
        private void ApplyGap()
        {
            // Clear all margins first
            var childList = Children().ToList();
            foreach (var child in childList)
            {
                child.SetMargin(0f);
            }

            // Early return if no gap or insufficient children
            if (_childGap <= 0f || childList.Count <= 1)
                return;

            // Apply gap to all children except the first
            for (int i = 1; i < childList.Count; i++)
            {
                var child = childList[i];
            
                switch (_childDirection)
                {
                    case FlexDirection.Column:
                        child.SetMarginTop(_childGap);
                        break;
                    case FlexDirection.ColumnReverse:
                        child.SetMarginBottom(_childGap);
                        break;
                    case FlexDirection.Row:
                        child.SetMarginLeft(_childGap);
                        break;
                    case FlexDirection.RowReverse:
                        child.SetMarginRight(_childGap);
                        break;
                }
            }
        }

        public new void Add(VisualElement child)
        {
            base.Add(child);
            RefreshGap();
        }
        
        public void Add(List<VisualElement> children)
        {
            foreach (var child in children)
            {
                base.Add(child);
            }
            RefreshGap();
        }

        public new void Insert(int index, VisualElement child)
        {
            base.Insert(index, child);
            RefreshGap();
        }

        public new void Remove(VisualElement child)
        {
            base.Remove(child);
            RefreshGap();
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            RefreshGap();
        }

        public new void Clear()
        {
            base.Clear();
            RefreshGap();
        }
    }
}