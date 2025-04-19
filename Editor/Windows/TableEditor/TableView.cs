using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DataKeeper.Editor.Windows
{
    public class TableView : VisualElement
    {
        private readonly List<List<VisualElement>> cells = new List<List<VisualElement>>();
        private readonly List<string> columnNames = new List<string>();
        private readonly List<Type> columnTypes = new List<Type>();

        public TableView()
        {
            style.flexDirection = FlexDirection.Column;
        }

        public virtual void ClearTable()
        {
            Clear();
            cells.Clear();
            columnNames.Clear();
            columnTypes.Clear();
        }

        public virtual void AddColumn(int columnIndex, string columnName, Type columnType)
        {
            // Ensure the header row exists
            VisualElement headerRow = new VisualElement();
            if (childCount == 0)
            {
                headerRow.style.flexDirection = FlexDirection.Row;
                Add(headerRow);
            }

            // Extend lists if needed
            while (columnNames.Count <= columnIndex)
            {
                columnNames.Add(string.Empty);
                columnTypes.Add(null);
            }

            // Add column header
            var headerElement = new Label(columnName);
            headerElement.style.minWidth = 100;
            headerElement.style.paddingLeft = 5;
            headerElement.style.paddingRight = 5;
            headerElement.style.borderBottomWidth = 1;
            headerElement.style.borderBottomColor = Color.gray;

            headerRow = this[0] as VisualElement;
            if (headerRow.childCount <= columnIndex)
            {
                headerRow.Add(headerElement);
            }
            else
            {
                headerRow.Insert(columnIndex, headerElement);
            }

            columnNames[columnIndex] = columnName;
            columnTypes[columnIndex] = columnType;
        }

        public virtual void AddValue(int columnIndex, int rowIndex, object value, Action<object> onValueChanged = null)
        {
            // Ensure row exists
            var rowElement = new VisualElement();

            while (cells.Count <= rowIndex)
            {
                var newRow = new List<VisualElement>();
                cells.Add(newRow);
                
                rowElement = new VisualElement();
                rowElement.style.flexDirection = FlexDirection.Row;
                Add(rowElement);
            }

            // Ensure column exists in row
            var row = cells[rowIndex];
            while (row.Count <= columnIndex)
            {
                row.Add(null);
            }

            rowElement = this[rowIndex + 1] as VisualElement;
            VisualElement field = CreateFieldForType(columnTypes[columnIndex], value, onValueChanged);
            
            if (row[columnIndex] == null)
            {
                row[columnIndex] = field;
                rowElement.Add(field);
            }
            else
            {
                var oldField = row[columnIndex];
                int fieldIndex = rowElement.IndexOf(oldField);
                rowElement.Remove(oldField);
                rowElement.Insert(fieldIndex, field);
                row[columnIndex] = field;
            }
        }

        public virtual void RemoveRow(int rowIndex)
        {
            if (rowIndex < cells.Count)
            {
                RemoveAt(rowIndex + 1); // +1 because of header row
                cells.RemoveAt(rowIndex);
            }
        }

        public virtual void ClearAllRows()
        {
            for (int i = hierarchy.childCount - 1; i > 0; i--) // Start from last, keep header
            {
                RemoveAt(i);
            }
            cells.Clear();
        }

        private VisualElement CreateFieldForType(Type type, object obj, Action<object> onValueChanged)
        {
            VisualElement field = null;

            if (typeof(Object).IsAssignableFrom(type))
            {
                var objField = new ObjectField { value = obj as Object, objectType = type };
                objField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                field = objField;
            }
            else if (type == typeof(int))
            {
                var intField = new IntegerField { label = "int", value = (int)obj };
                intField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                intField.style.width = 10;
                field = intField;
            }
            else if (type == typeof(float))
            {
                var floatField = new FloatField { value = (float)obj };
                floatField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                field = floatField;
            }
            else if (type == typeof(bool))
            {
                var toggle = new Toggle { value = (bool)obj };
                toggle.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                field = toggle;
            }
            else if (type == typeof(string))
            {
                var textField = new TextField { value = (string)obj };
                textField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                field = textField;
            }
            else if (type == typeof(Color))
            {
                var colorField = new ColorField { value = (Color)obj };
                colorField.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));
                field = colorField;
            }
            else
            {
                // Default to label for unsupported types
                field = new Label(obj?.ToString() ?? "");
            }

            field.style.minWidth = 100;
            field.style.paddingLeft = 5;
            field.style.paddingRight = 5;

            return field;
        }
    }
}