#if UNITY_6000_3_OR_NEWER
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

        [MenuItem("Tools/Windows/SO Manager")]
        public static void Open()
        {
            Texture2D icon = EditorGUIUtility.FindTexture("d_winbtn_win_max@2x");

            var window = GetWindow<SOManagerWindow>();
            window.minSize = new Vector2(400, 300);
            window.titleContent = new GUIContent("SO Manager", icon);
        }

        public void CreateGUI()
        {
            // 1. Global Toolbar
            var topToolbar = new Toolbar();
            var addTabBtn = new ToolbarButton(() => AddEmptyTab()) { text = "＋ New Empty Tab" };
            topToolbar.Add(addTabBtn);
            rootVisualElement.Add(topToolbar);

            // 2. Main Tab View
            _tabView = new TabView { style = { flexGrow = 1 } };
            rootVisualElement.Add(_tabView);

            // Start with one empty tab
            AddEmptyTab();
        }

        private void AddEmptyTab()
        {
            var newTab = new Tab { label = "New Tab" };
            var container = new VisualElement { style = { flexGrow = 1 } };
            container.SetPadding(10);

            // Configuration Section
            var configBox = new VisualElement { 
                style = { 
                    flexDirection = FlexDirection.Row, 
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                    marginBottom = 5,
                    alignItems = Align.Center
                } 
            };

            configBox.SetPadding(5);
        
            var soField = new ObjectField("Select ScriptableObject") {
                objectType = typeof(ScriptableObject),
                style = { flexGrow = 1 }
            };

            var privateToggle = new Toggle("Include Private") {
                value = false,
                style = { marginLeft = 10 }
            };

            configBox.Add(soField);
            configBox.Add(privateToggle);
            container.Add(configBox);

            // Result Area (where the table will live)
            var tableContainer = new VisualElement { style = { flexGrow = 1 } };
            container.Add(tableContainer);

            soField.RegisterValueChangedCallback(evt => {
                if (evt.newValue is ScriptableObject so) {
                    newTab.label = so.name;
                    BuildTable(tableContainer, so, privateToggle.value);
                }
            });

            newTab.Add(container);
            _tabView.Add(newTab);
            _tabView.selectedTabIndex = _tabView.Query<Tab>().Build().Count() - 1;
        }

        private void BuildTable(VisualElement container, ScriptableObject so, bool includePrivate)
        {
            container.Clear();

            var serializedObject = new SerializedObject(so);
            var listProperty = FindFirstList(serializedObject);

            if (listProperty == null) {
                container.Add(new Label("No List/Array found in this object."));
                return;
            }

            // --- Toolbar for Table ---
            var tableToolbar = new Toolbar();
            tableToolbar.Add(new Label($"Property: {listProperty.displayName}"));
            var spacer = new VisualElement { style = { flexGrow = 1 } };
            tableToolbar.Add(spacer);
            container.Add(tableToolbar);

            // --- MultiColumnListView with Scroll ---
            // By default, MCLV includes a ScrollView internally if flexGrow is set
            var mclv = new MultiColumnListView
            {
                bindingPath = listProperty.propertyPath,
                fixedItemHeight = 28,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                reorderable = true,
                showAddRemoveFooter = true,
                style = { flexGrow = 1 } 
            };

            GenerateColumns(mclv, listProperty, includePrivate);
        
            container.Add(mclv);
            container.Bind(serializedObject);
        }

        private void GenerateColumns(MultiColumnListView mclv, SerializedProperty listProp, bool includePrivate)
        {
            mclv.columns.Clear();

            // Ensure we have a template element to look at
            if (listProp.arraySize == 0) {
                listProp.InsertArrayElementAtIndex(0);
                listProp.serializedObject.ApplyModifiedProperties();
            }

            SerializedProperty element = listProp.GetArrayElementAtIndex(0);
            var childProp = element.Copy();
            var endProperty = element.GetEndProperty();

            // If includePrivate is true, we use 'enterChildren: true' 
            // Note: SerializedProperty only sees fields marked [SerializeField] or public.
            // Pure private fields without [SerializeField] are not visible to the SerializedObject system.
            if (childProp.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(childProp, endProperty)) break;

                    string fieldPath = childProp.name;
                    string displayName = childProp.displayName;

                    var col = new Column
                    {
                        title = displayName,
                        name = fieldPath,
                        stretchable = true,
                        minWidth = 80, // Prevents "bad starting size"
                        width = 150
                    };

                    col.makeCell = () => new PropertyField() { label = "" };
                    col.bindCell = (ve, index) =>
                    {
                        if (index >= listProp.arraySize) return;
                        var field = ve as PropertyField;
                        var item = listProp.GetArrayElementAtIndex(index);
                        var p = item.FindPropertyRelative(fieldPath);
                        if (p != null) field.BindProperty(p);
                    };

                    mclv.columns.Add(col);
                } 
                while (childProp.NextVisible(false));
            }
        }

        private SerializedProperty FindFirstList(SerializedObject so)
        {
            var prop = so.GetIterator();
            if (prop.NextVisible(true))
            {
                do {
                    if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                        return so.FindProperty(prop.name);
                } while (prop.NextVisible(false));
            }
            return null;
        }
    }
}

#endif