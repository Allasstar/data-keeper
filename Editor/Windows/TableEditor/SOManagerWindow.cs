#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.UIToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows
{
    public class SOManagerWindow : EditorWindow
    {
        private TabView _tabView;
        private Dictionary<Tab, TabData> _tabDataMap = new Dictionary<Tab, TabData>();

        private class TabData
        {
            public ScriptableObject SO;
            public SerializedObject SerializedObject;
            public MultiColumnListView ListView;
            public VisualElement TableContainer;
            public Toggle PrivateToggle;
            public Slider RowSizeSlider;
            public TextField SearchField;
            public DropdownField ListSelector;
            public List<SerializedProperty> AvailableLists;
            public SerializedProperty CurrentList;
            public Toggle ShowPropertiesToggle;
            public Label ItemCountLabel;
        }

        [MenuItem("Tools/Windows/SO Manager")]
        public static void Open()
        {
            var icon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;
            var window = GetWindow<SOManagerWindow>();
            window.minSize = new Vector2(600, 400);
            window.titleContent = new GUIContent("SO Manager", icon);
        }

        public void CreateGUI()
        {
            rootVisualElement.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            // Main Toolbar
            var topToolbar = new Toolbar();
            topToolbar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            topToolbar.style.borderBottomWidth = 2;
            topToolbar.style.borderBottomColor = new Color(0.1f, 0.5f, 0.8f);

            var addTabBtn = new ToolbarButton(() => AddEmptyTab()) {
                text = "➕ New Tab",
                style = {
                    backgroundColor = new Color(0.2f, 0.6f, 0.9f),
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginLeft = 4,
                    marginRight = 4,
                    paddingLeft = 10,
                    paddingRight = 10,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            var helpBtn = new ToolbarButton(() => ShowHelp()) {
                text = "❓ Help",
                style = { marginLeft = 4 }
            };

            topToolbar.Add(addTabBtn);
            topToolbar.Add(new VisualElement { style = { flexGrow = 1 } });
            topToolbar.Add(helpBtn);
            rootVisualElement.Add(topToolbar);

            // Tab View
            _tabView = new TabView { style = { flexGrow = 1 } };
            rootVisualElement.Add(_tabView);

            AddEmptyTab();
        }

        private void AddEmptyTab()
        {
            var newTab = new Tab { label = "📄 New Tab" };
            var tabData = new TabData();
            _tabDataMap[newTab] = tabData;

            var container = new VisualElement { style = { flexGrow = 1 } };
            container.SetPadding(12);

            // Config Section
            var configBox = CreateConfigBox(tabData);
            container.Add(configBox);

            // Control Panel
            var controlPanel = CreateControlPanel(tabData);
            container.Add(controlPanel);

            // Table Container
            tabData.TableContainer = new VisualElement {
                style = {
                    flexGrow = 1,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    marginTop = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            container.Add(tabData.TableContainer);

            newTab.Add(container);
            _tabView.Add(newTab);
            _tabView.selectedTabIndex = _tabView.Query<Tab>().Build().Count() - 1;
        }

        private VisualElement CreateConfigBox(TabData tabData)
        {
            var configBox = new VisualElement {
                style = {
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    borderLeftWidth = 3,
                    borderLeftColor = new Color(0.2f, 0.6f, 0.9f)
                }
            };

            var soField = new ObjectField("📦 ScriptableObject") {
                objectType = typeof(ScriptableObject),
                style = { flexGrow = 1, marginBottom = 8 }
            };

            // Row with toggles
            var toggleRow = new VisualElement {
                style = { flexDirection = FlexDirection.Row }
            };

            // Properties Toggle - keep tooltip but remove inline hint label (moved to Help)
            tabData.ShowPropertiesToggle = new Toggle("Show Auto-Properties") {
                value = true,
                tooltip = "Show properties declared as: [field: SerializeField] public Type PropName { get; set; }",
                style = { flexGrow = 1, marginRight = 8 }
            };

            tabData.PrivateToggle = new Toggle("Show Private Fields") {
                value = false,
                tooltip = "Include private fields marked with [SerializeField]",
                style = { flexGrow = 1 }
            };

            toggleRow.Add(tabData.ShowPropertiesToggle);
            toggleRow.Add(tabData.PrivateToggle);

            soField.RegisterValueChangedCallback(evt => {
                if (evt.newValue is ScriptableObject so) {
                    OnSOSelected(tabData, so);
                }
            });

            // Register toggle changes to rebuild
            tabData.ShowPropertiesToggle.RegisterValueChangedCallback(evt => {
                if (tabData.CurrentList != null) RebuildTable(tabData);
            });

            tabData.PrivateToggle.RegisterValueChangedCallback(evt => {
                if (tabData.CurrentList != null) RebuildTable(tabData);
            });

            configBox.Add(soField);
            configBox.Add(toggleRow);
            // Note: auto-properties hint intentionally removed from here and placed inside the Help dialog

            return configBox;
        }

        private VisualElement CreateControlPanel(TabData tabData)
        {
            var panel = new VisualElement {
                style = {
                    marginTop = 8
                }
            };

            // First row - List selector with refresh and count
            var listRow = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 6
                }
            };

            tabData.ListSelector = new DropdownField("📋 List/Array") {
                style = { flexGrow = 1, marginRight = 8 }
            };
            tabData.ListSelector.RegisterValueChangedCallback(evt => {
                if (tabData.AvailableLists != null && evt.newValue != null) {
                    var selected = tabData.AvailableLists.FirstOrDefault(p => p.displayName == evt.newValue);
                    if (selected != null) {
                        tabData.CurrentList = selected;
                        RebuildTable(tabData);
                    }
                }
            });

            // Refresh button
            var refreshBtn = new Button(() => RefreshTable(tabData)) {
                text = "🔄 Refresh",
                style = {
                    backgroundColor = new Color(0.3f, 0.7f, 0.4f),
                    color = Color.white,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginRight = 8,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            // Item count label
            tabData.ItemCountLabel = new Label("0 items") {
                style = {
                    backgroundColor = new Color(0.3f, 0.3f, 0.35f),
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 4,
                    paddingBottom = 4,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    fontSize = 11,
                    color = new Color(0.8f, 0.9f, 1f),
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            listRow.Add(tabData.ListSelector);
            listRow.Add(refreshBtn);
            listRow.Add(tabData.ItemCountLabel);

            // Second row - Row size and search
            var controlRow = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            // Row Size Slider
            var rowSizeContainer = new VisualElement {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 8 }
            };
            var rowSizeLabel = new Label("📏 Row Height:") {
                style = { marginRight = 4, fontSize = 11 }
            };
            tabData.RowSizeSlider = new Slider(28, 150, SliderDirection.Horizontal) {
                value = 28,
                style = { width = 120, marginRight = 4 }
            };
            var rowSizeValue = new Label("28") {
                style = { width = 30, fontSize = 11 }
            };
            tabData.RowSizeSlider.RegisterValueChangedCallback(evt => {
                rowSizeValue.text = ((int)evt.newValue).ToString();
                if (tabData.ListView != null) {
                    tabData.ListView.fixedItemHeight = evt.newValue;
                    tabData.ListView.Rebuild();
                }
            });

            rowSizeContainer.Add(rowSizeLabel);
            rowSizeContainer.Add(tabData.RowSizeSlider);
            rowSizeContainer.Add(rowSizeValue);

            // Export button
            var exportBtn = new Button(() => ExportToCSV(tabData)) {
                text = "💾 Export CSV",
                style = {
                    backgroundColor = new Color(0.5f, 0.4f, 0.7f),
                    color = Color.white,
                    paddingLeft = 10,
                    paddingRight = 10,
                    marginLeft = 8,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            controlRow.Add(rowSizeContainer);
            controlRow.Add(new VisualElement { style = { flexGrow = 1 } });
            controlRow.Add(exportBtn);

            panel.Add(listRow);
            panel.Add(controlRow);

            return panel;
        }

        private void RefreshTable(TabData tabData)
        {
            if (tabData.SO == null) return;

            // Force update serialized object
            tabData.SerializedObject.Update();

            // Rebuild table completely
            RebuildTable(tabData);

            Debug.Log("Table refreshed");
        }

        private void OnSOSelected(TabData tabData, ScriptableObject so)
        {
            var activeTab = _tabDataMap.FirstOrDefault(kvp => kvp.Value == tabData).Key;
            if (activeTab != null) {
                activeTab.label = $"📄 {so.name}";
            }

            tabData.SO = so;
            tabData.SerializedObject = new SerializedObject(so);
            tabData.AvailableLists = FindAllLists(tabData.SerializedObject);

            if (tabData.AvailableLists.Count == 0) {
                tabData.TableContainer.Clear();
                tabData.TableContainer.Add(new Label("⚠️ No List/Array found in this ScriptableObject.") {
                    style = {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        fontSize = 14,
                        color = new Color(1f, 0.7f, 0.3f)
                    }
                });
                tabData.ItemCountLabel.text = "0 items";
                return;
            }

            tabData.ListSelector.choices = tabData.AvailableLists.Select(p => p.displayName).ToList();
            tabData.ListSelector.value = tabData.ListSelector.choices[0];
            tabData.CurrentList = tabData.AvailableLists[0];

            RebuildTable(tabData);
        }

        private void RebuildTable(TabData tabData)
        {
            if (tabData.CurrentList == null) return;

            // Update serialized object first
            tabData.SerializedObject.Update();

            tabData.TableContainer.Clear();

            var tableToolbar = new Toolbar {
                style = {
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0),
                    borderBottomWidth = 2,
                    borderBottomColor = new Color(0.3f, 0.7f, 1f),
                    marginBottom = 4
                }
            };
            tableToolbar.Add(new Label($"📊 {tabData.CurrentList.displayName}") {
                style = {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.8f, 0.9f, 1f)
                }
            });
            tableToolbar.SetMargin(4);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            tableToolbar.Add(spacer);
            tabData.TableContainer.Add(tableToolbar);

            BuildListView(tabData);

            // Update item count
            tabData.ItemCountLabel.text = $"{tabData.CurrentList.arraySize} items";
        }

        private void BuildListView(TabData tabData)
        {
            var mclv = new MultiColumnListView {
                bindingPath = tabData.CurrentList.propertyPath,
                fixedItemHeight = tabData.RowSizeSlider.value,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                reorderable = true,
                showAddRemoveFooter = true,
                selectionType = SelectionType.Multiple,
                style = {
                    flexGrow = 1,
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f)
                }
            };

            mclv.SetMargin(4);

            // Generate columns and set up column bindingPath so the list view can manage header/cell interactions properly
            GenerateColumns(mclv, tabData.CurrentList, tabData.PrivateToggle.value, tabData.ShowPropertiesToggle.value);

            // Bind the MultiColumnListView directly to the serialized object.
            // Binding the container is not enough for full multi-column header behaviors (resize/reorder),
            // so bind the list view itself as recommended in Unity documentation examples.
            try {
                mclv.Bind(tabData.SerializedObject);
            }
            catch (Exception) {
                // Older Unity versions may not support Bind(SerializedObject) on MultiColumnListView.
                // Fall back to binding the container so the window still functions.
                tabData.TableContainer.Bind(tabData.SerializedObject);
            }

            // Ensure layout is recalculated (helps column header to show correct sizes)
            mclv.Rebuild();

            // Register callbacks to keep serialization in sync
            mclv.itemsAdded += (indices) => {
                tabData.SerializedObject.ApplyModifiedProperties();
                tabData.SerializedObject.Update();
                tabData.ItemCountLabel.text = $"{tabData.CurrentList.arraySize} items";
            };

            mclv.itemsRemoved += (indices) => {
                tabData.SerializedObject.ApplyModifiedProperties();
                tabData.SerializedObject.Update();
                tabData.ItemCountLabel.text = $"{tabData.CurrentList.arraySize} items";
            };

            tabData.TableContainer.Add(mclv);
            tabData.ListView = mclv;
        }
        
        VisualElement MakeValueField(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return new IntegerField { isDelayed = true };

                case SerializedPropertyType.Float:
                    return new FloatField { isDelayed = true };

                case SerializedPropertyType.Boolean:
                    return new Toggle();

                case SerializedPropertyType.String:
                    return new TextField();

                case SerializedPropertyType.Enum:
                    return new EnumField();

                default:
                    return new PropertyField(prop, "");
            }
        }

        private void GenerateColumns(MultiColumnListView mclv, SerializedProperty listProp, bool includePrivate, bool showProperties)
        {
            mclv.columns.Clear();

            // Ensure we have at least one element to inspect
            int originalSize = listProp.arraySize;
            if (listProp.arraySize == 0) {
                listProp.InsertArrayElementAtIndex(0);
                listProp.serializedObject.ApplyModifiedProperties();
                listProp.serializedObject.Update();
            }

            SerializedProperty element = listProp.GetArrayElementAtIndex(0);
            var fields = GetFieldsAndProperties(element, includePrivate, showProperties);

            // Remove the temporary element if we added one
            if (originalSize == 0) {
                listProp.DeleteArrayElementAtIndex(0);
                listProp.serializedObject.ApplyModifiedProperties();
                listProp.serializedObject.Update();
            }

            foreach (var fieldInfo in fields)
            {
                var col = new Column {
                    title = fieldInfo.DisplayName,
                    name = fieldInfo.FieldPath,
                    // Set bindingPath so the list view can use internal binding path logic for columns (header/cell)
                    bindingPath = fieldInfo.FieldPath,
                    resizable = true,
                    stretchable = false,
                    minWidth = 75,
                    maxWidth = 300,
                    width = 150
                };

                // create a simple template cell (PropertyField) and bind in bindCell using the element's relative property
                col.makeCell = () => {
                    var field = new PropertyField { label = "" };
                    field.style.marginTop = 2;
                    field.style.marginBottom = 2;
                    // allow the field to grow horizontally to match column width
                    field.style.flexGrow = 1;
                    field.style.minWidth = 50;
                   
                    return field;
                };

                col.bindCell = (ve, index) => {
                    if (index >= listProp.arraySize) return;
                    var field = ve as PropertyField;
                    var item = listProp.GetArrayElementAtIndex(index);
                    var p = item.FindPropertyRelative(fieldInfo.FieldPath);
                    if (p != null) {
                        field.BindProperty(p);
                    }
                };

                mclv.columns.Add(col);
            }
        }

        private struct FieldInfo
        {
            public string FieldPath;
            public string DisplayName;
            public bool IsProperty;
        }

        private List<FieldInfo> GetFieldsAndProperties(SerializedProperty element, bool includePrivate, bool showProperties)
        {
            var result = new List<FieldInfo>();
            var childProp = element.Copy();
            var endProperty = element.GetEndProperty();

            if (childProp.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(childProp, endProperty)) break;

                    string fieldPath = childProp.propertyPath.Replace(element.propertyPath + ".", "");
                    string displayName = childProp.displayName;

                    // Check if this is a property backing field
                    bool isProperty = fieldPath.Contains("<") && fieldPath.Contains(">k__BackingField");

                    // Skip properties if toggle is off
                    if (isProperty && !showProperties) continue;

                    // Clean up property display names
                    if (isProperty) {
                        // Extract property name from "<PropertyName>k__BackingField"
                        int startIdx = fieldPath.IndexOf('<') + 1;
                        int endIdx = fieldPath.IndexOf('>');
                        if (startIdx > 0 && endIdx > startIdx) {
                            displayName = fieldPath.Substring(startIdx, endIdx - startIdx);
                        }
                    }

                    result.Add(new FieldInfo {
                        FieldPath = fieldPath,
                        DisplayName = displayName,
                        IsProperty = isProperty
                    });
                }
                while (childProp.NextVisible(false));
            }

            return result;
        }

        private List<SerializedProperty> FindAllLists(SerializedObject so)
        {
            var lists = new List<SerializedProperty>();
            var prop = so.GetIterator();

            if (prop.NextVisible(true))
            {
                do {
                    if (prop.isArray && prop.propertyType != SerializedPropertyType.String) {
                        lists.Add(so.FindProperty(prop.propertyPath));
                    }
                } while (prop.NextVisible(false));
            }

            return lists;
        }

        private void ExportToCSV(TabData tabData)
        {
            if (tabData.CurrentList == null || tabData.SO == null) return;

            string path = EditorUtility.SaveFilePanel("Export to CSV", "", $"{tabData.SO.name}_export.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try {
                var fields = GetFieldsAndProperties(tabData.CurrentList.GetArrayElementAtIndex(0), 
                    tabData.PrivateToggle.value, tabData.ShowPropertiesToggle.value);

                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(",", fields.Select(f => $"\"{f.DisplayName}\"")));

                for (int i = 0; i < tabData.CurrentList.arraySize; i++) {
                    var item = tabData.CurrentList.GetArrayElementAtIndex(i);
                    var values = fields.Select(f => {
                        var p = item.FindPropertyRelative(f.FieldPath);
                        return p != null ? GetPropertyValueAsString(p) : "";
                    });
                    csv.AppendLine(string.Join(",", values));
                }

                System.IO.File.WriteAllText(path, csv.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Data exported successfully to:\n{path}", "OK");
            }
            catch (Exception ex) {
                Debug.LogError($"Export failed: {ex.Message}");
                EditorUtility.DisplayDialog("Export Failed", $"Error: {ex.Message}", "OK");
            }
        }

        private string GetPropertyValueAsString(SerializedProperty prop)
        {
            switch (prop.propertyType) {
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
                case SerializedPropertyType.Float: return prop.floatValue.ToString("F2");
                case SerializedPropertyType.String: return $"\"{prop.stringValue.Replace("\"", "\"\"")}\"";
                case SerializedPropertyType.Color: return $"\"{prop.colorValue}\"";
                case SerializedPropertyType.Vector2: return $"\"{prop.vector2Value}\"";
                case SerializedPropertyType.Vector3: return $"\"{prop.vector3Value}\"";
                case SerializedPropertyType.Vector4: return $"\"{prop.vector4Value}\"";
                case SerializedPropertyType.Quaternion: return $"\"{prop.quaternionValue}\"";
                case SerializedPropertyType.Rect: return $"\"{prop.rectValue}\"";
                case SerializedPropertyType.ObjectReference: 
                    return prop.objectReferenceValue != null ? $"\"{prop.objectReferenceValue.name}\"" : "null";
                default: return $"\"{prop.displayName}\"";
            }
        }

        private void ShowHelp()
        {
            EditorUtility.DisplayDialog("SO Manager Help",
                "📖 ScriptableObject Manager\n\n" +
                "Features:\n" +
                "• Multi-tab interface for editing multiple SOs\n" +
                "• View and edit lists/arrays in table format\n" +
                "• Support for auto-properties with [field: SerializeField]\n" +
                "• Adjustable row height for complex types\n" +
                "• CSV export functionality\n" +
                "• Reorderable rows with drag & drop\n\n" +
                "Auto-Properties:\n" +
                "Toggle 'Show Auto-Properties' to display properties like:\n" +
                "[field: SerializeField] public int Health { get; set; }\n\n" +
                "Tips:\n" +
                "• Use 🔄 Refresh if data seems out of sync\n" +
                "• Increase row height for Gradients, Curves\n" +
                "• Drag rows to reorder\n" +
                "• Multi-select with Ctrl/Cmd + Click",
                "Got it!");
        }
    }
}
#endif