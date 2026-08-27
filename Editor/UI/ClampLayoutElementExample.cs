using DataKeeper.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DataKeeper.Editor.UI
{
    // Builds a ClampLayoutElement demo into the open scene: a cap on self-sizing text, and a scroll view that
    // hugs its content between a minimum and a maximum.
    public static class ClampLayoutElementExample
    {
        private const string MENU_PATH = "GameObject/UI (DataKeeper)/Examples/Clamp Layout Element";

        private const float PanelWidth = 720f;

        private const float BubbleMaxWidth = 300f;

        private const float ScrollMinHeight = 80f;
        private const float ScrollMaxHeight = 220f;
        private const float ItemHeight = 26f;

        private const string BubbleText =
            "A bubble sized by its own text keeps getting wider until something stops it.";

        private static readonly Color RootColor = new Color(0.11f, 0.12f, 0.15f, 0.96f);
        private static readonly Color ViewColor = new Color(0.17f, 0.18f, 0.22f);
        private static readonly Color ClampedColor = new Color(0.13f, 0.42f, 0.35f);
        private static readonly Color PlainColor = new Color(0.36f, 0.30f, 0.22f);
        private static readonly Color ItemColor = new Color(0.22f, 0.34f, 0.44f);
        private static readonly Color TitleColor = new Color(0.92f, 0.94f, 0.96f);
        private static readonly Color NoteColor = new Color(0.58f, 0.62f, 0.68f);

        [MenuItem(MENU_PATH, false, 2100)]
        private static void Create(MenuCommand menuCommand)
        {
            GameObject parent = menuCommand.context as GameObject;
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
                parent = ResolveCanvas().gameObject;

            GameObject root = CreateChild("Clamp Layout Element Example", parent, new Vector2(PanelWidth, 600f));

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(40f, -40f);

            AddImage(root, RootColor);
            AddVertical(root, 16f, new RectOffset(20, 20, 20, 20)).childForceExpandWidth = true;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateText(root, "Title", "Clamp Layout Element", 20f, TitleColor);

            BuildSelfSizingText(root);
            BuildScrollView(root);

            GameObjectUtility.EnsureUniqueNameForSibling(root);
            Undo.RegisterCreatedObjectUndo(root, "Create Clamp Layout Element Example");
            Selection.activeGameObject = root;
        }

        private static void BuildSelfSizingText(GameObject parent)
        {
            GameObject section = CreateSection(parent, "1 - Text that sizes itself",
                "Both bubbles are sized by their text. Max Width " + BubbleMaxWidth +
                " stops the top one and makes it wrap; the other takes the whole panel width.");

            GameObject capped = CreateBubble(section, "Capped Bubble", ClampedColor);
            capped.AddComponent<ClampLayoutElement>().maxWidth = BubbleMaxWidth;

            CreateBubble(section, "Uncapped Bubble", PlainColor);
        }

        private static void BuildScrollView(GameObject parent)
        {
            GameObject section = CreateSection(parent, "2 - A scroll view between two bounds",
                "A stock scroll view: the content is anchored and sizes itself, the view measures it through Size Source. Min Height " +
                ScrollMinHeight + " and Max Height " + ScrollMaxHeight +
                " on the view, so a short list hugs its items and a long one stops and scrolls.");

            GameObject row = CreateChild("Row", section, new Vector2(PanelWidth, ScrollMaxHeight));
            AddHorizontal(row, 12f).childForceExpandWidth = true;
            row.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateScrollView(row, "1 item - floored at " + ScrollMinHeight, 1);
            CreateScrollView(row, "4 items - its own height", 4);
            CreateScrollView(row, "14 items - capped, scrolls", 14);
        }

        // The stock scroll view arrangement: the content is anchored, not a layout child, so it is free to
        // grow past the view. Nothing on the view reports that size, which is what Size Source is for - a
        // layout group here would resize the content back down to the bounded height and nothing would scroll.
        //
        // The Viewport is not decoration: ScrollRect is itself an ILayoutGroup, so parenting the content
        // straight to the view makes Unity warn that a self-controller sits under a layout controller.
        private static void CreateScrollView(GameObject parent, string label, int itemCount)
        {
            GameObject holder = CreateChild(label, parent, new Vector2(200f, ScrollMaxHeight));
            AddVertical(holder, 6f, new RectOffset(0, 0, 0, 0)).childForceExpandWidth = true;

            LayoutElement holderLayout = holder.AddComponent<LayoutElement>();
            holderLayout.preferredWidth = 0f;
            holderLayout.flexibleWidth = 1f;

            CreateText(holder, "Caption", label, 11f, NoteColor)
                .gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject view = CreateChild("Scroll View", holder, new Vector2(200f, ScrollMaxHeight));
            AddImage(view, ViewColor);

            GameObject viewport = CreateChild("Viewport", view, Vector2.zero);
            viewport.AddComponent<RectMask2D>();
            Stretch(viewport, 0f);

            GameObject content = CreateChild("Content", viewport, new Vector2(0f, 100f));
            RectTransform contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            AddVertical(content, 4f, new RectOffset(6, 6, 6, 6)).childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 1; i <= itemCount; i++) CreateItem(content, i);

            ScrollRect scroll = view.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            ClampLayoutElement clamp = view.AddComponent<ClampLayoutElement>();
            clamp.sizeSource = contentRect;
            clamp.minHeight = ScrollMinHeight;
            clamp.maxHeight = ScrollMaxHeight;
        }

        private static void CreateItem(GameObject parent, int index)
        {
            GameObject item = CreateChild("Item " + index, parent, new Vector2(180f, ItemHeight));
            AddImage(item, ItemColor);
            item.AddComponent<LayoutElement>().preferredHeight = ItemHeight;

            TextMeshProUGUI label = CreateText(item, "Label", "Item " + index, 12f, TitleColor);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(label.gameObject, 6f);
        }

        private static GameObject CreateBubble(GameObject parent, string name, Color color)
        {
            GameObject bubble = CreateChild(name, parent, new Vector2(BubbleMaxWidth, 40f));
            AddImage(bubble, color);
            AddVertical(bubble, 0f, new RectOffset(12, 12, 8, 8));

            CreateText(bubble, "Text", BubbleText, 15f, TitleColor);
            return bubble;
        }

        private static GameObject CreateSection(GameObject parent, string title, string note)
        {
            GameObject section = CreateChild(title, parent, new Vector2(PanelWidth, 100f));
            AddVertical(section, 6f, new RectOffset(0, 0, 0, 0));

            CreateText(section, "Title", title, 15f, TitleColor);
            CreateText(section, "Note", note, 12f, NoteColor).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return section;
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

        // Child Control Size on is what makes a group ask LayoutUtility for a size at all, and Child Force
        // Expand off is what stops it raising the flexible size back to 1 over a cap.
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
