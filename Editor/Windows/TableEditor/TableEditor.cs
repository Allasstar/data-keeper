using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.UIToolkit;
using ObjectField = UnityEditor.UIElements.ObjectField;

namespace DataKeeper.Editor.Windows
{
    public class TableEditor : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        
        // Data
        private ScriptableObject _selectedSO;
        private object _selectedObject;
        private List<FieldInfo> _selectedFields;
        
        // Elements
        private Label _label;
        private TableView _tableView;
        private ObjectField _objectField;
        private DropdownField _dropdownField;
        private ScrollView _scrollView;
        private SliderInt _columnWidth;
        private SliderInt _rowHeight;


        [MenuItem("Tools/Windows/Table Editor")]
        public static void ShowExample()
        {
            TableEditor wnd = GetWindow<TableEditor>();
            wnd.titleContent = new GUIContent("Table Editor");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            
            // Root
            root.Add(labelFromUXML);
            
            _scrollView = new ScrollView()
                .SetPadding(5)
                .SetFlexGrow(1);
            
            root.Add(_scrollView);

            _tableView = new TableView();
            _scrollView.Add(_tableView);
            
            // Get elements
            _objectField = root.Q<ObjectField>("SelectSO");
            _dropdownField = root.Q<DropdownField>("DropDown");
            _label = root.Q<Label>("SelectedName");
            _columnWidth = root.Q<SliderInt>("ColumnWidth");
            _rowHeight = root.Q<SliderInt>("RowHeight");
            
            // Setup
            _dropdownField.visible = false;
            
            _objectField.objectType = typeof(ScriptableObject);
            _objectField.RegisterValueChangedCallback(OnSOChanged);
            
            _tableView.SetColumnWidth(150);
            _columnWidth.value = 150;
            _columnWidth.RegisterValueChangedCallback(c =>
            {
                _tableView.SetColumnWidth(c.newValue);
                _scrollView.ForceUpdate();
            });
            _rowHeight.RegisterValueChangedCallback(r => _tableView.SetRowHeight(r.newValue));
        }

        private void OnSOChanged(ChangeEvent<Object> evt)
        {
            _selectedSO = evt.newValue as ScriptableObject;
            _dropdownField.visible = _selectedSO != null;

            _label.text = _selectedSO == null ? "" :  AssetDatabase.GetAssetPath(_selectedSO);
            
            _tableView.ClearTable();

            if (_selectedSO == null)
            {
                return;
            }
            
            var fields = ReflectionUtility.GetFieldsWithAttribute(_selectedSO, typeof(TableAttribute));
            var choices = fields.Select(f => ReflectionUtility.ExtractFieldName(f)).ToList();
            
            _dropdownField.choices = choices;
            if (choices.Any())
            {
                _dropdownField.value = choices.First();
            }
            
            _dropdownField.RegisterValueChangedCallback(DropFieldChanged);
        }

        private void DropFieldChanged(ChangeEvent<string> evt)
        {
            if (string.IsNullOrEmpty(evt.newValue))
            {
                return;
            }
            
            _selectedObject = ReflectionUtility.GetMemberField(_selectedSO, evt.newValue);

            if (_selectedObject == null)
            {
                return;
            }
            
            if (_selectedObject is IList list)
            {
                if (list.Count == 0)
                    return;
        
                // Get the type of the first element to determine its members
                var firstElement = list[0];
                if (firstElement == null)
                    return;
        
                var elementType = firstElement.GetType();
                var members = ReflectionUtility.GetAllFields(firstElement);
    
                // Add columns for each member
                for (int i = 0; i < members.Count; i++)
                {
                    var name = $"{ReflectionUtility.ExtractFieldName(members[i])}::{members[i].FieldType.Name}";
                    _tableView.AddColumn(i, name, members[i].FieldType);
                }
    
                // Add rows for each list element
                for (int rowIndex = 0; rowIndex < list.Count; rowIndex++)
                {
                    var element = list[rowIndex];
                    for (int colIndex = 0; colIndex < members.Count; colIndex++)
                    {
                        var value = members[colIndex].GetValue(element);
                        var capturedMember = members[colIndex];
                        _tableView.AddValue(colIndex, rowIndex, value, (newValue) =>
                        {
                            capturedMember.SetValue(element, newValue);
                        });
                    }
                }
            }
        }
    }
}