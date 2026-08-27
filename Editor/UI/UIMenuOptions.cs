using System;
using DataKeeper.UI;
using DataKeeper.ValueProviders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataKeeper.Editor.UI
{
    // Hierarchy create menu for the DataKeeper UI components, mirroring what Unity's own
    // GameObject/UI entries do: reuse the stage's canvas (or build one with an EventSystem),
    // parent to the right object, register a single undo step and select the result.
    public static class UIMenuOptions
    {
        private const string MENU_ROOT = "GameObject/UI (DataKeeper)/";

        private static readonly Color TextColor = new Color(0.196f, 0.196f, 0.196f);
        private static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.392f);
        private static readonly Color TabOffColor = new Color(0.72f, 0.72f, 0.72f);
        private static readonly string[] StarterItems = { "Option 1", "Option 2", "Option 3" };

        [MenuItem(MENU_ROOT + "Button UI", false, 2000)]
        private static void CreateButtonUI(MenuCommand menuCommand)
        {
            GameObject root = CreateRoot("Button UI", menuCommand, new Vector2(160f, 30f));
            BuildButton(root, "Button");
            Place(root);
        }

        [MenuItem(MENU_ROOT + "Toggle UI", false, 2001)]
        private static void CreateToggleUI(MenuCommand menuCommand)
        {
            GameObject root = CreateRoot("Toggle UI", menuCommand, new Vector2(160f, 20f));

            GameObject background = CreateChild("Background", root, new Vector2(20f, 20f));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.anchoredPosition = new Vector2(10f, 0f);
            Image backgroundImage = AddSprite(background, "UI/Skin/UISprite.psd");

            GameObject checkmark = CreateChild("Checkmark", background, new Vector2(20f, 20f));
            Stretch(checkmark);
            Image checkmarkImage = AddSprite(checkmark, "UI/Skin/Checkmark.psd");
            checkmarkImage.type = Image.Type.Simple;

            TextMeshProUGUI label = CreateLabel(root, "Label", "Toggle", TextAlignmentOptions.MidlineLeft);
            Stretch(label.gameObject, new Vector4(24f, 0f, 0f, 0f));

            ToggleUI toggle = root.AddComponent<ToggleUI>();
            toggle.targetGraphic = backgroundImage;
            toggle.icon = checkmarkImage;
            toggle.label = label;

            // The checkmark is an Image, so hiding it means fading it out: a null sprite would
            // still draw a white quad.
            SerializedObject serialized = new SerializedObject(toggle);
            SetToggleColor(serialized, "_iconColor", Color.white, new Color(1f, 1f, 1f, 0f));
            serialized.ApplyModifiedProperties();

            Place(root);
        }

        [MenuItem(MENU_ROOT + "Carousel UI", false, 2002)]
        private static void CreateCarouselUI(MenuCommand menuCommand)
        {
            GameObject root = CreateRoot("Carousel UI", menuCommand, new Vector2(240f, 40f));

            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            ButtonUI previous = BuildButtonChild(root, "Previous", "<", 40f);
            TextMeshProUGUI label = CreateLabel(root, "Label", "Value", TextAlignmentOptions.Center);
            AddLayoutElement(label.gameObject, -1f, 1f);
            ButtonUI next = BuildButtonChild(root, "Next", ">", 40f);

            CarouselUI carousel = root.AddComponent<CarouselUI>();

            SerializedObject serialized = new SerializedObject(carousel);
            serialized.FindProperty(BackingField("Previous")).objectReferenceValue = previous;
            serialized.FindProperty(BackingField("Next")).objectReferenceValue = next;
            serialized.FindProperty(BackingField("Carousel")).managedReferenceValue = new CarouselString();
            serialized.ApplyModifiedProperties();

            // Each step has to be applied before the properties it creates can be addressed:
            // the managed reference first, then the item slots it holds.
            serialized.Update();
            SerializedProperty carouselProperty = serialized.FindProperty(BackingField("Carousel"));
            carouselProperty.FindPropertyRelative("_label").objectReferenceValue = label;
            carouselProperty.FindPropertyRelative("_items").arraySize = StarterItems.Length;
            serialized.ApplyModifiedProperties();

            serialized.Update();
            SerializedProperty items = serialized.FindProperty(BackingField("Carousel")).FindPropertyRelative("_items");
            for (int i = 0; i < StarterItems.Length; i++)
                items.GetArrayElementAtIndex(i).managedReferenceValue = new StringConstantProvider { Value = StarterItems[i] };

            serialized.ApplyModifiedProperties();

            Place(root);
        }

        [MenuItem(MENU_ROOT + "Tabs UI", false, 2003)]
        private static void CreateTabsUI(MenuCommand menuCommand)
        {
            GameObject root = CreateRoot("Tabs UI", menuCommand, new Vector2(400f, 300f));

            GameObject bar = CreateChild("Tab Buttons", root, new Vector2(0f, 40f));
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.offsetMin = new Vector2(0f, -40f);
            barRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 4f;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;

            GameObject panels = CreateChild("Panels", root, Vector2.zero);
            Stretch(panels, new Vector4(0f, 44f, 0f, 0f));

            ToggleUI[] toggles = { BuildTabToggle(bar, "Tab 1"), BuildTabToggle(bar, "Tab 2") };
            GameObject[] panelObjects = { BuildPanel(panels, "Panel 1"), BuildPanel(panels, "Panel 2") };

            TabsUI tabs = root.AddComponent<TabsUI>();

            SerializedObject serialized = new SerializedObject(tabs);
            SerializedProperty tabsProperty = serialized.FindProperty("_tabs");
            tabsProperty.arraySize = toggles.Length;

            for (int i = 0; i < toggles.Length; i++)
            {
                SerializedProperty tab = tabsProperty.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("toggle").objectReferenceValue = toggles[i];
                tab.FindPropertyRelative("panel").objectReferenceValue = panelObjects[i];
            }

            serialized.ApplyModifiedProperties();

            // TabsUI only sorts this out in Awake, so give the editor the same picture it will
            // have at runtime instead of every panel stacked on top of each other.
            toggles[0].SetIsOnWithoutNotify(true);
            panelObjects[1].SetActive(false);

            Place(root);
        }

        // -- Element builders --------------------------------------------

        private static ButtonUI BuildButton(GameObject root, string text)
        {
            Image image = AddSprite(root, "UI/Skin/UISprite.psd");

            ButtonUI button = root.AddComponent<ButtonUI>();
            button.targetGraphic = image;

            TextMeshProUGUI label = CreateLabel(root, "Label", text, TextAlignmentOptions.Center);
            Stretch(label.gameObject);
            button.label = label;

            return button;
        }

        private static ButtonUI BuildButtonChild(GameObject parent, string name, string text, float width)
        {
            GameObject buttonObject = CreateChild(name, parent, new Vector2(width, width));
            AddLayoutElement(buttonObject, width, -1f);
            return BuildButton(buttonObject, text);
        }

        private static ToggleUI BuildTabToggle(GameObject parent, string text)
        {
            GameObject tabObject = CreateChild(text, parent, new Vector2(100f, 40f));
            AddLayoutElement(tabObject, -1f, 1f);

            Image image = AddSprite(tabObject, "UI/Skin/UISprite.psd");
            TextMeshProUGUI label = CreateLabel(tabObject, "Label", text, TextAlignmentOptions.Center);
            Stretch(label.gameObject);

            ToggleUI toggle = tabObject.AddComponent<ToggleUI>();
            toggle.targetGraphic = image;
            toggle.icon = image;
            toggle.label = label;

            // A tab reads as selected through its own background, so the icon slot drives the
            // background tint instead of a separate checkmark.
            SerializedObject serialized = new SerializedObject(toggle);
            SetToggleColor(serialized, "_iconColor", Color.white, TabOffColor);
            serialized.ApplyModifiedProperties();

            return toggle;
        }

        private static GameObject BuildPanel(GameObject parent, string name)
        {
            GameObject panel = CreateChild(name, parent, Vector2.zero);
            Stretch(panel);

            Image image = AddSprite(panel, "UI/Skin/UISprite.psd");
            image.color = PanelColor;

            return panel;
        }

        private static TextMeshProUGUI CreateLabel(GameObject parent, string name, string text, TextAlignmentOptions alignment)
        {
            GameObject labelObject = CreateChild(name, parent, new Vector2(100f, 30f));

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = alignment;
            label.color = TextColor;
            label.fontSize = 18f;

            return label;
        }

        // -- Hierarchy plumbing ------------------------------------------

        private static GameObject CreateRoot(string name, MenuCommand menuCommand, Vector2 size)
        {
            GameObject parent = menuCommand.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
                parent = GetOrCreateCanvas().gameObject;

            return CreateChild(name, parent, size);
        }

        private static GameObject CreateChild(string name, GameObject parent, Vector2 size)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(child, parent);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition3D = Vector3.zero;
            rect.sizeDelta = size;

            return child;
        }

        private static void Place(GameObject root)
        {
            GameObjectUtility.EnsureUniqueNameForSibling(root);
            Undo.RegisterCreatedObjectUndo(root, "Create " + root.name);
            Selection.activeGameObject = root;
        }

        private static Canvas GetOrCreateCanvas()
        {
            StageHandle stage = StageUtility.GetCurrentStageHandle();

            Canvas existing = stage.FindComponentOfType<Canvas>();
            if (existing != null && existing.gameObject.activeInHierarchy) return existing;

            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            StageUtility.PlaceGameObjectInCurrentStage(canvasObject);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            CreateEventSystem();

            return canvas;
        }

        private static void CreateEventSystem()
        {
            StageHandle stage = StageUtility.GetCurrentStageHandle();
            if (stage.FindComponentOfType<EventSystem>() != null) return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            StageUtility.PlaceGameObjectInCurrentStage(eventSystem);
            eventSystem.AddComponent(InputModuleType());

            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        // Resolved by name so this assembly does not have to reference the Input System package:
        // StandaloneInputModule only works on the legacy backend, and vice versa.
        private static Type InputModuleType()
        {
#if ENABLE_INPUT_SYSTEM
            Type inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) return inputSystemModule;
#endif
            return typeof(StandaloneInputModule);
        }

        // -- Small helpers -----------------------------------------------

        private static Image AddSprite(GameObject target, string builtinPath)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinPath);
            image.type = Image.Type.Sliced;
            return image;
        }

        private static void AddLayoutElement(GameObject target, float preferredWidth, float flexibleWidth)
        {
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
        }

        private static void Stretch(GameObject target) => Stretch(target, Vector4.zero);

        // padding: left, top, right, bottom
        private static void Stretch(GameObject target, Vector4 padding)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding.x, padding.w);
            rect.offsetMax = new Vector2(-padding.z, -padding.y);
        }

        private static void SetToggleColor(SerializedObject serialized, string optionalPath, Color on, Color off)
        {
            serialized.FindProperty(optionalPath + ".enabled").boolValue = true;
            serialized.FindProperty(optionalPath + ".value." + BackingField("On")).colorValue = on;
            serialized.FindProperty(optionalPath + ".value." + BackingField("Off")).colorValue = off;
        }

        private static string BackingField(string propertyName) => "<" + propertyName + ">k__BackingField";
    }
}
