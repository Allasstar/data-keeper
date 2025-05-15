using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using DataKeeper.Attributes;
using DataKeeper.UIToolkit;
using DataKeeper.Utility;
using UnityEditor.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

namespace DataKeeper.Editor.Windows
{
    public class TableEditor : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;
        
        // Data
        private ScriptableObject _selectedSO;
        private string _selectedField;
        private object _selectedObject;
        private List<FieldInfo> _selectedFields;
        
        // Elements
        private TableView _tableView;
        private ObjectField _objectField;
        private DropdownField _dropdownField;
        private ScrollView _scrollView;
        private SliderInt _columnWidth;
        private SliderInt _rowHeight;
        private ToolbarButton _helpToolbarButton;
        private ToolbarButton _refreshToolbarButton;
        
        private ToolbarMenu _exportToolbarMenu;
        private ToolbarMenu _importToolbarMenu;


        [MenuItem("Tools/Windows/Table Editor (Beta)", priority = 4)]
        public static void ShowExample()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_winbtn_win_max@2x");

            var window = GetWindow<TableEditor>();
            window.minSize = new Vector2(400, 300);
            window.titleContent = new GUIContent("Table Editor", icon);
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
            _columnWidth = root.Q<SliderInt>("ColumnWidth");
            _rowHeight = root.Q<SliderInt>("RowHeight");
            _helpToolbarButton = root.Q<ToolbarButton>("Help");
            _refreshToolbarButton = root.Q<ToolbarButton>("Refresh");
            
            _exportToolbarMenu = root.Q<ToolbarMenu>("Export");
            _importToolbarMenu = root.Q<ToolbarMenu>("Import");
            
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

            _exportToolbarMenu.menu.AppendAction("CSV (clipboard)", ExportCSVClipboard);
            _exportToolbarMenu.menu.AppendAction("TSV (clipboard)", ExportTSVClipboard);
            
            _importToolbarMenu.menu.AppendAction("CSV (clipboard)", ImportCSVClipboard);
            _importToolbarMenu.menu.AppendAction("TSV (clipboard)", ImportTSVClipboard);
            
            Undo.undoRedoPerformed -= Refresh;
            Undo.undoRedoPerformed += Refresh;
            _refreshToolbarButton.RegisterCallback<ClickEvent>(evt => Refresh());
            _helpToolbarButton.RegisterCallback<ClickEvent>(evt =>
            {
                EditorUtility.DisplayDialog(
                    "Table Editor Help",
                    "This window allows you to edit data in a table format.\n\n" +
                    "1. Select a ScriptableObject using the Object Field\n" +
                    "2. Choose a list field from the dropdown.\n" +
                    "   Only list field marked with `[Table]` attribute will be available for editing.\n" +
                    "3. Edit values directly in the table\n\n" +
                    "You can also import/export data using CSV/TSV format via clipboard.",
                    "OK"
                );
            });
        }

        private void Refresh()
        {
            DropFieldChanged(_selectedField);
        }

        private void ImportTSVClipboard(DropdownMenuAction obj)
        {
            string clipboardContent = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboardContent))
                return;

            var list = _selectedObject as IList;
            if (list == null || list.Count == 0)
                return;

            var elementType = list[0].GetType();
            // Use the specific type instead of object
            var method = typeof(CSVUtility).GetMethod("CSVToList").MakeGenericMethod(elementType);
            var importedList = (IList)method.Invoke(null, new object[] { clipboardContent, DelimiterType.Tab });
            
            if (importedList.Count > 0)
            {
                Undo.RecordObject(_selectedSO, "Table Import TSV");

                list.Clear();
                foreach (var item in importedList)
                {
                    list.Add(item);
                }
                
                // Refresh the view
                _tableView.ClearTable();
                DropFieldChanged(_dropdownField.value);
                EditorUtility.SetDirty(_selectedSO);
            }
        }

        private void ImportCSVClipboard(DropdownMenuAction obj)
        {
            string clipboardContent = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboardContent))
                return;

            var list = _selectedObject as IList;
            if (list == null || list.Count == 0)
                return;

            var elementType = list[0].GetType();
            // Use the specific type instead of object
            var method = typeof(CSVUtility).GetMethod("CSVToList").MakeGenericMethod(elementType);
            var importedList = (IList)method.Invoke(null, new object[] { clipboardContent, DelimiterType.Comma });
            
            if (importedList.Count > 0)
            {
                Undo.RecordObject(_selectedSO, "Table Import CSV");

                list.Clear();
                foreach (var item in importedList)
                {
                    list.Add(item);
                }
                
                // Refresh the view
                _tableView.ClearTable();
                DropFieldChanged(_dropdownField.value);
                EditorUtility.SetDirty(_selectedSO);
            }
        }

        private void ExportTSVClipboard(DropdownMenuAction obj)
        {
            if (_selectedObject is IList list)
            {
                if (list.Count == 0)
                    return;

                var elementType = list[0].GetType();
                // Use reflection to call the generic method with the correct type
                var method = typeof(CSVUtility).GetMethod("ListToCSV").MakeGenericMethod(elementType);
                string tsv = (string)method.Invoke(null, new object[] { list, DelimiterType.Tab });
                GUIUtility.systemCopyBuffer = tsv;
            }
        }

        private void ExportCSVClipboard(DropdownMenuAction obj)
        {
            if (_selectedObject is IList list)
            {
                if (list.Count == 0)
                    return;

                var elementType = list[0].GetType();
                // Use reflection to call the generic method with the correct type
                var method = typeof(CSVUtility).GetMethod("ListToCSV").MakeGenericMethod(elementType);
                string csv = (string)method.Invoke(null, new object[] { list, DelimiterType.Comma });
                GUIUtility.systemCopyBuffer = csv;
            }
        }

        private void OnSOChanged(ChangeEvent<Object> evt)
        {
            _selectedField = null;
            _selectedSO = evt.newValue as ScriptableObject;
            _dropdownField.visible = _selectedSO != null;
            
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
            DropFieldChanged(evt.newValue);
        }

        private void DropFieldChanged(string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }

            _selectedField = newValue;
            _selectedObject = ReflectionUtility.GetMemberField(_selectedSO, newValue);

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
                            Undo.RecordObject(_selectedSO, "Table Value Changed");
                            capturedMember.SetValue(element, newValue);
                            EditorUtility.SetDirty(_selectedSO);
                        });
                    }
                }
            }
        }
    }
}