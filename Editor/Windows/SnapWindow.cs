using DataKeeper.Editor.MenuItems;
using DataKeeper.Editor.Settings;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using DataKeeper.UIToolkit;

public class SnapToolsWindow : EditorWindow
{
    private VisualElement root;
    private Toggle alignToGroundToggle;

    [MenuItem("Tools/Windows/Snap Tools", priority = 10)]
    public static void ShowWindow()
    {
        SnapToolsWindow window = GetWindow<SnapToolsWindow>();
        window.titleContent = new GUIContent("Snap Tools", EditorGUIUtility.IconContent("Transform Icon").image);
        window.minSize = new Vector2(300, 400);
        window.maxSize = new Vector2(300, 600);
    }

    public void CreateGUI()
    {
        root = rootVisualElement;
        
        // Create main container
        var mainContainer = new VisualElement()
            .SetPadding(10)
            .SetFlexGrow(1);
        
        // View Section
        // CreateViewSection(mainContainer);
        
        // Ground Snap Section
        CreateGroundSnapSection(mainContainer);
        
        // Settings Section
        // CreateSettingsSection(mainContainer);

        root.Add(mainContainer);
    }

    private void CreateViewSection(VisualElement parent)
    {
        var section = CreateSection("View Controls", parent);
        
        var bringToViewBtn = CreateIconButton(
            "Bring Selected to View", 
            "ViewToolOrbit", 
            SnapToGroundEditor.PerformSnapToScreenCenter);
        
        bringToViewBtn.tooltip = "Move Selected Objects to Scene view";
        section.Add(bringToViewBtn);
    }

    private void CreateSettingsSection(VisualElement parent)
    {
        var section = CreateSection("Snap Settings", parent);
        
        // Align to ground toggle
        alignToGroundToggle = new Toggle("Align to Ground");
        alignToGroundToggle.tooltip = "Rotate objects to align with ground surface normal";
        
        alignToGroundToggle.value = DataKeeperEditorPref.SnapToolsAlignToGroundPref.Value;
        
        alignToGroundToggle.RegisterValueChangedCallback(evt => 
        {
            DataKeeperEditorPref.SnapToolsAlignToGroundPref.Value = evt.newValue;
        });
        section.Add(alignToGroundToggle);
    }

    private void CreateGroundSnapSection(VisualElement parent)
    {
        var section = CreateSection("Ground Snap Tools", parent);
        
        var bringToViewBtn = CreateIconButton(
            "Bring Selected to View", 
            "ViewToolOrbit", 
            SnapToGroundEditor.PerformSnapToScreenCenter);
        
        bringToViewBtn.tooltip = "Move Selected Objects to Scene view";
        section.Add(bringToViewBtn);
        
        var snapOriginBtn = CreateIconButton(
            "Snap to Ground (Origin)", 
            "Transform Icon", 
            SnapToGroundEditor.SnapToGroundTransform);
        
        snapOriginBtn.tooltip = "Snap object to ground using transform origin point";
        section.Add(snapOriginBtn);
        
        var snapColliderBtn = CreateIconButton(
            "Snap to Ground (Collider)", 
            "BoxCollider Icon", 
            SnapToGroundEditor.SnapToGroundCollider);
        
        snapColliderBtn.tooltip = "Snap object to ground using collider bounds";
        section.Add(snapColliderBtn);
        
        var snapMeshBtn = CreateIconButton(
            "Snap to Ground (Mesh)", 
            "MeshRenderer Icon", 
            SnapToGroundEditor.SnapToGroundMesh);
        
        snapMeshBtn.tooltip = "Snap object to ground using mesh bounds";
        section.Add(snapMeshBtn);
    }

    private VisualElement CreateSection(string title, VisualElement parent)
    {
        var sectionContainer = new VisualElement();
        sectionContainer.style.marginBottom = 20;
        
        var sectionTitle = new Label(title);
        sectionTitle.style.fontSize = 14;
        sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        sectionTitle.style.marginBottom = 8;
        sectionTitle.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
        sectionContainer.Add(sectionTitle);
        
        var separator = new VisualElement();
        separator.style.height = 1;
        separator.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        separator.style.marginBottom = 10;
        sectionContainer.Add(separator);
        
        parent.Add(sectionContainer);
        return sectionContainer;
    }

    private Button CreateIconButton(string text, string iconName, System.Action onClick)
    {
        var button = new Button(onClick)
            .SetFontSize(12)
            .SetText(text)
            .SetHeight(30)
            .SetMarginBottom(5)
            .SetFlexDirection(FlexDirection.Row)
            .SetJustifyContent(Justify.Center)
            .SetAlignItems(Align.Center)
            .SetBackgroundColor(new Color(0.3f, 0.3f, 0.3f))
            .SetBorderRadius(3)
            .SetBorderWidth(1f)
            .SetBorderColor(
                new Color(0.6f, 0.6f, 0.6f), 
                new Color(0.6f, 0.6f, 0.6f), 
                new Color(0.2f, 0.2f, 0.2f), 
                new Color(0.2f, 0.2f, 0.2f));
        
        // Try to set icon
        var icon = EditorGUIUtility.IconContent(iconName);
        if (icon != null && icon.image != null)
        {
            var iconElement = new VisualElement()
                .SetWidth(16)
                .SetHeight(16)
                .SetMarginRight(5)
                .SetPositionAbsolute(8)
                .SetBackgroundImage(new StyleBackground((Texture2D)icon.image));
            
            button.Insert(0, iconElement);
        }

        return button;
    }
}