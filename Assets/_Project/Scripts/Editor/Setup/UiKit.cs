using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Builds uGUI/TextMeshPro hierarchies from code so scenes can be regenerated. Layout uses a
    /// 1920x1080 landscape reference with width/height match 0.5 so it holds on 16:9 to 21:9.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color PanelDark = new Color(0.07f, 0.07f, 0.09f, 0.92f);
        public static readonly Color PanelMid = new Color(0.14f, 0.14f, 0.18f, 0.95f);
        public static readonly Color Accent = new Color(0.93f, 0.18f, 0.16f, 1f);
        public static readonly Color AccentDim = new Color(0.55f, 0.12f, 0.11f, 1f);
        public static readonly Color TextMain = new Color(0.96f, 0.96f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.7f, 0.72f, 0.78f, 1f);
        public static readonly Color ButtonNormal = new Color(0.2f, 0.2f, 0.25f, 1f);

        public static Canvas CreateCanvas(string name, int sortOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform AnchorRange(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal)
        {
            var rect = CreateRect(parent, name);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color background, float fontSize, out TMP_Text labelText)
        {
            var image = CreatePanel(parent, name, background);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            labelText = CreateText(image.transform, "Label", label, fontSize, TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch((RectTransform)labelText.transform, 8f, 4f, 8f, 4f);
            return button;
        }

        public static Image CreateFillBar(Transform parent, string name, Color background, Color fill, out Image fillImage)
        {
            var bg = CreatePanel(parent, name, background);
            bg.raycastTarget = false;
            fillImage = CreatePanel(bg.transform, "Fill", fill);
            fillImage.raycastTarget = false;
            Stretch((RectTransform)fillImage.transform);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0.5f;
            return bg;
        }

        /// <summary>Vertical scroll list; returns the content root that rows get parented to.</summary>
        public static ScrollRect CreateScrollList(Transform parent, string name, out RectTransform content)
        {
            var viewportImage = CreatePanel(parent, name, new Color(0f, 0f, 0f, 0.25f));
            var scroll = viewportImage.gameObject.AddComponent<ScrollRect>();
            viewportImage.gameObject.AddComponent<RectMask2D>();

            content = CreateRect(viewportImage.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = (RectTransform)viewportImage.transform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        /// <summary>Paged tutorial overlay: dim curtain, card with title/body/page, NEXT and SKIP.</summary>
        public static RedlineLegends.UI.TutorialOverlay CreateTutorialOverlay(Transform parent)
        {
            var curtain = CreatePanel(parent, "TutorialOverlay", new Color(0f, 0f, 0f, 0.7f));
            Stretch((RectTransform)curtain.transform);
            var overlay = curtain.gameObject.AddComponent<RedlineLegends.UI.TutorialOverlay>();
            var card = CreatePanel(curtain.transform, "Card", PanelDark);
            Anchor((RectTransform)card.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 420f));
            var title = CreateText(card.transform, "Title", "TUTORIAL", 42f, Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(800f, 56f));
            var page = CreateText(card.transform, "Page", "1 / 3", 24f, TextDim, TextAlignmentOptions.Right);
            Anchor((RectTransform)page.transform, new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(200f, 40f));
            var body = CreateText(card.transform, "Body", "", 30f, TextMain, TextAlignmentOptions.TopLeft);
            AnchorRange((RectTransform)body.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 120f), new Vector2(-40f, -100f));
            var next = CreateButton(card.transform, "Next", "NEXT", Accent, 30f, out _);
            Anchor((RectTransform)next.transform, new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(260f, 72f));
            var skip = CreateButton(card.transform, "Skip", "SKIP", ButtonNormal, 26f, out _);
            Anchor((RectTransform)skip.transform, new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(220f, 72f));
            overlay.EditorWire(title, body, page, next, skip);
            return overlay;
        }

        /// <summary>Horizontal uGUI slider with a fill and a wide handle for thumbs.</summary>
        public static Slider CreateSlider(Transform parent, string name, out Image fillImage)
        {
            var bg = CreatePanel(parent, name, new Color(0.1f, 0.1f, 0.12f, 1f));
            var slider = bg.gameObject.AddComponent<Slider>();
            var fillArea = CreateRect(bg.transform, "FillArea");
            Stretch(fillArea, 6f, 4f, 6f, 4f);
            fillImage = CreatePanel(fillArea, "Fill", Accent);
            fillImage.raycastTarget = false;
            var fillRect = (RectTransform)fillImage.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var handleArea = CreateRect(bg.transform, "HandleArea");
            Stretch(handleArea, 14f, 0f, 14f, 0f);
            var handle = CreatePanel(handleArea, "Handle", TextMain);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(28f, 10f);
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        public static LayoutElement SetPreferredHeight(Component target, float height)
        {
            var element = target.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return element;
        }
    }
}
