using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.UIToolkit.Tests
{
    /// <summary>
    /// Test class to verify all UI Toolkit extensions work correctly
    /// </summary>
    public class UIToolkitExtensionsTest : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        
        private void Start()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
                
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found!");
                return;
            }
            
            TestAllExtensions();
        }
        
        private void TestAllExtensions()
        {
            var root = uiDocument.rootVisualElement;
            root.RemoveAllChildren(); // Clear existing content
            
            // Test Core Extensions
            TestCoreExtensions(root);
            
            // Test Size Extensions
            TestSizeExtensions(root);
            
            // Test Spacing Extensions
            TestSpacingExtensions(root);
            
            // Test Appearance Extensions
            TestAppearanceExtensions(root);
            
            // Test Layout Extensions
            TestLayoutExtensions(root);
            
            // Test Typography Extensions
            TestTypographyExtensions(root);
            
            // Test Interaction Extensions
            TestInteractionExtensions(root);
            
            // Test Element-Specific Extensions
            TestElementSpecificExtensions(root);
            
            // Test Hierarchy Extensions
            TestHierarchyExtensions(root);
            
            Debug.Log("All UI Toolkit extension tests completed!");
        }
        
        private void TestCoreExtensions(VisualElement root)
        {
            var testContainer = new VisualElement()
                .SetName("CoreTestContainer")
                .SetEnabledSelf(true)
                .SetDisplay(DisplayStyle.Flex)
                .SetVisibility(Visibility.Visible)
                .SetOpacity(1f)
                .SetOverflow(Overflow.Hidden)
                .SetTooltip("Core extensions test")
                .SetPickingMode(PickingMode.Position)
                .SetFocusable(false)
                .SetTabIndex(-1);
                
            root.AddChild(testContainer);
            
            Debug.Log("✓ Core Extensions Test Passed");
        }
        
        private void TestSizeExtensions(VisualElement root)
        {
            var sizeContainer = new VisualElement()
                .SetName("SizeTestContainer")
                .SetSize(200, 100)
                .SetWidth(250)
                .SetHeight(120)
                .SetWidthPercent(50)
                .SetHeightPercent(25)
                .SetMinWidth(100)
                .SetMinHeight(50)
                .SetMaxWidth(400)
                .SetMaxHeight(200)
                .SetSquareSize(150);
                
            // Test auto and initial values
            var autoElement = new VisualElement()
                .SetWidthAuto()
                .SetHeightAuto()
                .SetMinWidthAuto()
                .SetMinHeightAuto()
                .SetMaxWidthNone()
                .SetMaxHeightNone();
                
            root.AddChild(sizeContainer, autoElement);
            
            Debug.Log("✓ Size Extensions Test Passed");
        }
        
        private void TestSpacingExtensions(VisualElement root)
        {
            var spacingContainer = new VisualElement()
                .SetName("SpacingTestContainer")
                .SetPadding(10)
                .SetPadding(5, 10, 15, 20)
                .SetPaddingPercent(5)
                .SetMargin(8)
                .SetMargin(4, 8, 12, 16)
                .SetMarginPercent(3)
                .SetMarginAuto()
                .SetMarginHorizontalAuto()
                .SetPaddingHorizontal(12)
                .SetPaddingVertical(8)
                .SetMarginHorizontal(6)
                .SetMarginVertical(4);
                
            root.AddChild(spacingContainer);
            
            Debug.Log("✓ Spacing Extensions Test Passed");
        }
        
        private void TestAppearanceExtensions(VisualElement root)
        {
            var appearanceContainer = new VisualElement()
                .SetName("AppearanceTestContainer")
                .SetBackgroundColor(Color.blue)
                .SetBackgroundColorHex("#FF5733")
                .SetColor(Color.white)
                .SetColorHex("#FFFFFF")
                .SetBorder(2, Color.black)
                .SetBorderWidth(1)
                .SetBorderColor(Color.red)
                .SetBorderColorHex("#00FF00")
                .SetBorderRadius(5)
                .SetBorderRadius(5, 10, 15, 20)
                .SetRoundedCorners()
                .SetTransparent()
                .SetSemiTransparent(0.8f);
                
            root.AddChild(appearanceContainer);
            
            Debug.Log("✓ Appearance Extensions Test Passed");
        }
        
        private void TestLayoutExtensions(VisualElement root)
        {
            var layoutContainer = new VisualElement()
                .SetName("LayoutTestContainer")
                .SetFlexColumn()
                .SetFlexGrow(1)
                .SetFlexShrink(0)
                .SetFlexBasis(100)
                .SetAlignItemsCenter()
                .SetJustifyCenter()
                .SetFlexWrapOn()
                .SetPositionRelative()
                .SetFlexCenterBoth()
                .SetTranslate(10, 20)
                .SetRotate(45)
                .SetScale(1.2f);
                
            var absoluteElement = new VisualElement()
                .SetPositionAbsolute(10, 20, 30, 40)
                .SetLeft(15)
                .SetTop(25)
                .SetLeftPercent(50)
                .SetTopPercent(25);
                
            layoutContainer.AddChild(absoluteElement);
            root.AddChild(layoutContainer);
            
            Debug.Log("✓ Layout Extensions Test Passed");
        }
        
        private void TestTypographyExtensions(VisualElement root)
        {
            var label = new Label("Test Typography")
                .SetFontSize(16)
                .SetFontSizePercent(120)
                .SetFontBold()
                .SetTextAlignCenter()
                .SetTextColor(Color.black)
                .SetTextColorHex("#333333")
                .SetLetterSpacing(1f)
                .SetWordSpacing(2f)
                .SetTextOverflowEllipsis()
                .SetHeadingStyle(2)
                .SetText("Updated Text");
                
            var bodyLabel = new Label("Body text example")
                .SetBodyTextStyle();
                
            root.AddChild(label, bodyLabel);
            
            Debug.Log("✓ Typography Extensions Test Passed");
        }
        
        private void TestInteractionExtensions(VisualElement root)
        {
            var interactiveContainer = new VisualElement()
                .SetName("InteractiveTestContainer")
                .SetOnClick(() => Debug.Log("Container clicked!"))
                .SetOnPointerEnter(evt => Debug.Log("Pointer entered"))
                .SetOnPointerLeave(evt => Debug.Log("Pointer left"))
                .SetHoverEffect(Color.yellow, 0.8f)
                .SetHoverScale(1.1f)
                .SetFocusable(true)
                .SetInteractable(true)
                .SetUserData("test-data");
                
            // Test animation
            interactiveContainer.FadeIn(0.5f);
            
            root.AddChild(interactiveContainer);
            
            Debug.Log("✓ Interaction Extensions Test Passed");
        }
        
        private void TestElementSpecificExtensions(VisualElement root)
        {
            var button = new Button()
                .SetButtonText("Test Button")
                .SetOnClick(() => Debug.Log("Button clicked!"))
                .SetPrimaryButton();
                
            var secondaryButton = new Button()
                .SetContent("Secondary", () => Debug.Log("Secondary clicked!"))
                .SetSecondaryButton();
                
            var image = new Image()
                .SetScaleMode(ScaleMode.ScaleToFit)
                .SetImageTint(Color.white);
                
            var textField = new TextField("Input Field")
                .SetValue("Default value")
                .SetMaxLength(50)
                .SetOnValueChanged(evt => Debug.Log($"Value changed: {evt.newValue}"));
                
            var toggle = new Toggle("Toggle Option")
                .SetToggleValue(true)
                .SetOnToggleChanged(evt => Debug.Log($"Toggle: {evt.newValue}"));
                
            var slider = new Slider("Slider", 0, 100)
                .SetSliderValue(50)
                .SetOnSliderChanged(evt => Debug.Log($"Slider: {evt.newValue}"));
                
            var dropdown = new DropdownField("Options", new System.Collections.Generic.List<string> { "Option 1", "Option 2", "Option 3" }, 0)
                .SetDropdownValue("Option 2")
                .SetOnDropdownChanged(evt => Debug.Log($"Dropdown: {evt.newValue}"));
                
            var scrollView = new ScrollView()
                .SetVerticalScroll()
                .SetScrollDecelerationRate(0.5f);
                
            var foldout = new Foldout()
                .SetFoldoutText("Collapsible Section")
                .SetFoldoutValue(false);
                
            root.AddChild(button, secondaryButton, image, textField, toggle, slider, dropdown, scrollView, foldout);
            
            Debug.Log("✓ Element-Specific Extensions Test Passed");
        }
        
        private void TestHierarchyExtensions(VisualElement root)
        {
            var parent = new VisualElement()
                .SetName("ParentContainer")
                .AddClass("parent-class")
                .AddClasses("class1", "class2", "class3");
                
            var child1 = new VisualElement()
                .SetName("Child1")
                .SetChildOf(parent)
                .AddClass("child-class");
                
            var child2 = new VisualElement()
                .SetName("Child2")
                .AddClass("child-class");
                
            parent.AddChild(child2);
            
            // Test conditional styling
            var conditionalElement = new VisualElement()
                .If(true, el => el.SetBackgroundColor(Color.green))
                .IfElse(false, el => el.SetHeight(100), el => el.SetHeight(50))
                .When(() => Time.time > 0, el => el.SetWidth(200))
                .ApplyIf(true, el => el.SetBorderRadius(10));
                
            // Test state management
            conditionalElement
                .SaveState("testKey", "testValue")
                .LogInfo("Conditional element created");
                
            var savedState = conditionalElement.GetState<VisualElement, string>("testKey");
            Debug.Log($"Saved state: {savedState}");
            
            // Test queries
            parent.QueryChild<VisualElement>("Child1");
            var allChildren = parent.QueryAllChildren<VisualElement>(className: "child-class");
            Debug.Log($"Found {allChildren.Count} children with child-class");
            
            root.AddChild(parent, conditionalElement);
            
            Debug.Log("✓ Hierarchy Extensions Test Passed");
        }
    }
}