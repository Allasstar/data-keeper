using DataKeeper.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Editor.UI
{
    // Builds a side by side "capped / not capped" demo of MaxSizeLayoutElement into the open scene.
    // Every group here has Child Control Size on and Child Force Expand off on the capped axis, which
    // are the two settings a cap actually depends on.
    public static class MaxSizeLayoutElementExample
    {
        private const string MENU_PATH = "GameObject/UI/DataKeeper/Examples/Max Size Layout Element";
        private const float RootWidth = 660f;
        private const float BubbleMaxWidth = 320f;
        private const float BoundedMinWidth = 130f;
        private const float BoundedMaxWidth = 380f;
        private const float BoxPreferredHeight = 120f;
        private const float BoxMaxHeight = 48f;
        private const float PanelCanvasFraction = 0.2f;

        private const string BubbleText = "This bubble is sized by its text and stops growing at 320 px.";

        private static readonly Color RootColor = new Color(0.11f, 0.12f, 0.15f, 0.96f);
        private static readonly Color CappedColor = new Color(0.13f, 0.42f, 0.35f);
        private static readonly Color PlainColor = new Color(0.36f, 0.30f, 0.22f);
        private static readonly Color TitleColor = new Color(0.92f, 0.94f, 0.96f);
        private static readonly Color NoteColor = new Color(0.58f, 0.62f, 0.68f);

        [MenuItem(MENU_PATH, false, 2100)]
        private static void Create(MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
                parent = ResolveCanvas().gameObject;

            GameObject root = CreateChild("Max Size Layout Element Example", parent, new Vector2(RootWidth, 400f));

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(40f, -40f);

            AddImage(root, RootColor);
            AddVertical(root, 16f, new RectOffset(20, 20, 20, 20)).childForceExpandWidth = true;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(root, "Title", "Max Size Layout Element", 20f, TitleColor);

            BuildMaxWidth(root);
            BuildMinAndMaxWidth(root);
            BuildMaxHeight(root);
            BuildCanvasFraction(root);

            GameObjectUtility.EnsureUniqueNameForSibling(root);
            Undo.RegisterCreatedObjectUndo(root, "Create Max Size Layout Element Example");
            Selection.activeGameObject = root;
        }

        private static void BuildMaxWidth(GameObject parent)
        {
            GameObject section = CreateSection(parent, "1 - Max Width",
                "Horizontal: Absolute, Max Width " + BubbleMaxWidth + ". The capped bubble wraps at the cap, the uncapped one runs to its full text width.");

            GameObject capped = CreateBubble(section, "Capped Bubble", CappedColor, BubbleText);
            AddMaxWidth(capped, BubbleMaxWidth);

            CreateBubble(section, "Uncapped Bubble", PlainColor, BubbleText);
        }

        // Min lives on Unity's own LayoutElement (priority 1) and the cap reads it back out and clamps it
        // from priority 2, so the two compose without either knowing about the other.
        private static void BuildMinAndMaxWidth(GameObject parent)
        {
            GameObject section = CreateSection(parent, "2 - Min Width + Max Width",
                "Layout Element Min Width " + BoundedMinWidth + " plus Max Width " + BoundedMaxWidth + ", identical on all three. Short clamps up, long clamps down.");

            CreateBoundedBubble(section, "Short - clamped up to min", "Hi");
            CreateBoundedBubble(section, "Medium - its own width", "A medium line.");
            CreateBoundedBubble(section, "Long - clamped down to max", "A long line that runs well past the maximum width and has to wrap onto another line.");
        }

        private static void BuildMaxHeight(GameObject parent)
        {
            GameObject section = CreateSection(parent, "3 - Max Height inside a layout group",
                "Both ask for " + BoxPreferredHeight + " px of preferred height. The left one is capped at " + BoxMaxHeight + ".");

            GameObject row = CreateChild("Row", section, new Vector2(RootWidth, BoxPreferredHeight));
            AddHorizontal(row, 12f).childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject capped = CreateBox(row, "Capped Box", CappedColor, "Capped at " + BoxMaxHeight);
            MaxSizeLayoutElement max = capped.AddComponent<MaxSizeLayoutElement>();
            max.verticalMode = MaxSizeLayoutElement.MaxMode.Absolute;
            max.maxHeight = BoxMaxHeight;

            CreateBox(row, "Uncapped Box", PlainColor, "Preferred " + BoxPreferredHeight);
        }

        private static void BuildCanvasFraction(GameObject parent)
        {
            GameObject section = CreateSection(parent, "4 - Canvas Fraction",
                "Layout Element asks for 2000 px. CanvasFraction " + PanelCanvasFraction + " caps it against the canvas - resize the Game view.");

            GameObject panel = CreateChild("Capped Panel", section, new Vector2(RootWidth, 200f));
            AddImage(panel, CappedColor);

            LayoutElement element = panel.AddComponent<LayoutElement>();
            element.preferredHeight = 2000f;
            element.flexibleWidth = 1f;

            MaxSizeLayoutElement max = panel.AddComponent<MaxSizeLayoutElement>();
            max.verticalMode = MaxSizeLayoutElement.MaxMode.CanvasFraction;
            max.maxHeight = PanelCanvasFraction;

            TextMeshProUGUI label = CreateText(panel, "Label", (PanelCanvasFraction * 100f) + "% of the canvas height", 14f, TitleColor);
            Stretch(label.gameObject, 10f);
        }

        private static GameObject CreateSection(GameObject parent, string title, string note)
        {
            GameObject section = CreateChild(title, parent, new Vector2(RootWidth, 100f));
            AddVertical(section, 6f, new RectOffset(0, 0, 0, 0));

            CreateText(section, "Title", title, 15f, TitleColor);
            CreateText(section, "Note", note, 12f, NoteColor).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return section;
        }

        private static GameObject CreateBubble(GameObject parent, string name, Color color, string text)
        {
            GameObject bubble = CreateChild(name, parent, new Vector2(BubbleMaxWidth, 40f));
            AddImage(bubble, color);
            AddVertical(bubble, 0f, new RectOffset(14, 14, 10, 10));

            CreateText(bubble, "Text", text, 16f, TitleColor);
            return bubble;
        }

        private static void CreateBoundedBubble(GameObject parent, string name, string text)
        {
            GameObject bubble = CreateBubble(parent, name, CappedColor, text);

            bubble.AddComponent<LayoutElement>().minWidth = BoundedMinWidth;
            AddMaxWidth(bubble, BoundedMaxWidth);
        }

        private static void AddMaxWidth(GameObject target, float width)
        {
            MaxSizeLayoutElement max = target.AddComponent<MaxSizeLayoutElement>();
            max.horizontalMode = MaxSizeLayoutElement.MaxMode.Absolute;
            max.maxWidth = width;
        }

        private static GameObject CreateBox(GameObject parent, string name, Color color, string caption)
        {
            GameObject box = CreateChild(name, parent, new Vector2(200f, BoxPreferredHeight));
            AddImage(box, color);
            box.AddComponent<LayoutElement>().preferredHeight = BoxPreferredHeight;

            TextMeshProUGUI label = CreateText(box, "Label", caption, 13f, TitleColor);
            label.alignment = TextAlignmentOptions.Center;
            Stretch(label.gameObject, 8f);

            return box;
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string content, float size, Color color)
        {
            GameObject textObject = CreateChild(name, parent, new Vector2(200f, 24f));

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;

            return text;
        }

        private static VerticalLayoutGroup AddVertical(GameObject target, float spacing, RectOffset padding)
        {
            VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
            Configure(layout, spacing, padding);
            return layout;
        }

        private static HorizontalLayoutGroup AddHorizontal(GameObject target, float spacing)
        {
            HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
            Configure(layout, spacing, new RectOffset(0, 0, 0, 0));
            return layout;
        }

        // Child Control Size on is what makes the group ask LayoutUtility for a size at all, and Child Force
        // Expand off is what stops it raising the flexible size back to 1 over the cap.
        private static void Configure(HorizontalOrVerticalLayoutGroup layout, float spacing, RectOffset padding)
        {
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
        }

        private static Image AddImage(GameObject target, Color color)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
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

        private static void Stretch(GameObject target, float padding)
        {
            RectTransform rect = (RectTransform)target.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static Canvas ResolveCanvas()
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

            return canvas;
        }
    }
}
