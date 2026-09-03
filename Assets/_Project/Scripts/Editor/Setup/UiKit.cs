using RedlineLegends.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Builds uGUI/TextMeshPro hierarchies from code so scenes can be regenerated. Layout uses a
    /// 1920x1080 landscape reference with width/height match 0.5 so it holds on 16:9 to 21:9.
    /// Every canvas gets a "SafeArea" child (see <see cref="SafeRoot"/>) that shrinks to the
    /// device's notch-free rectangle; HUD elements and buttons live there, full-screen curtains
    /// stay on the canvas root.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color PanelDark = new Color(0.06f, 0.06f, 0.08f, 0.9f);
        public static readonly Color PanelMid = new Color(0.13f, 0.13f, 0.17f, 0.94f);
        public static readonly Color Accent = new Color(0.93f, 0.18f, 0.16f, 1f);
        public static readonly Color AccentDim = new Color(0.55f, 0.12f, 0.11f, 1f);
        public static readonly Color TextMain = new Color(0.96f, 0.96f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.7f, 0.72f, 0.78f, 1f);
        public static readonly Color ButtonNormal = new Color(0.19f, 0.19f, 0.24f, 1f);

        private static Sprite Rounded => ProceduralTextures.RoundedRect();

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
            var safe = CreateRect(go.transform, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return canvas;
        }

        /// <summary>The notch-safe child of a canvas made by <see cref="CreateCanvas"/>.</summary>
        public static RectTransform SafeRoot(Canvas canvas) => (RectTransform)canvas.transform.Find("SafeArea");

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Solid panel. Rounded corners by default; pass false for full-bleed curtains and custom sprites.</summary>
        public static Image CreatePanel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            // A panel that fills its parent edge to edge has no visible corners: drop the rounded
            // sprite so curtains and fill bars stay crisp instead of showing clipped radii.
            if (left == 0f && bottom == 0f && right == 0f && top == 0f)
            {
                var image = rect.GetComponent<Image>();
                if (image != null && image.sprite != null && image.sprite == Rounded)
                {
                    image.sprite = null;
                    image.type = Image.Type.Simple;
                }
            }
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

        /// <summary>Racing display style for headings: bold italic with wide tracking.</summary>
        public static TMP_Text Title(TMP_Text text)
        {
            text.fontStyle = FontStyles.Bold | FontStyles.Italic;
            text.characterSpacing = 5f;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color background, float fontSize, out TMP_Text labelText)
        {
            var image = CreatePanel(parent, name, background);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            labelText = CreateText(image.transform, "Label", label, fontSize, TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            labelText.characterSpacing = 2f;
            Stretch((RectTransform)labelText.transform, 8f, 4f, 8f, 4f);
            return button;
        }

        public static Image CreateFillBar(Transform parent, string name, Color background, Color fill, out Image fillImage)
        {
            var bg = CreatePanel(parent, name, background);
            bg.raycastTarget = false;
            fillImage = CreatePanel(bg.transform, "Fill", fill);
            fillImage.raycastTarget = false;
            Stretch((RectTransform)fillImage.transform, 2f, 2f, 2f, 2f);
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
            layout.padding = new RectOffset(10, 10, 10, 10);
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
        public static TutorialOverlay CreateTutorialOverlay(Transform parent)
        {
            var curtain = CreatePanel(parent, "TutorialOverlay", new Color(0f, 0f, 0f, 0.7f), false);
            Stretch((RectTransform)curtain.transform);
            var overlay = curtain.gameObject.AddComponent<TutorialOverlay>();
            var card = CreatePanel(curtain.transform, "Card", PanelDark);
            Anchor((RectTransform)card.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 420f));
            var title = CreateText(card.transform, "Title", "TUTORIAL", 42f, Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            Title(title);
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
            var handle = CreatePanel(handleArea, "Handle", TextMain, false);
            handle.sprite = ProceduralTextures.Circle();
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(30f, 12f);
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

        // ------------------------------------------------------------------ race widgets
        public struct Gauge
        {
            public RectTransform Root;
            public Image RpmArc;
            public TMP_Text Speed;
            public TMP_Text Unit;
            public TMP_Text Gear;
            public TMP_Text Rpm;
            public Image ShiftLight;
        }

        /// <summary>
        /// Round instrument: 270-degree rpm arc (fill 0..0.75 maps to idle..redline), speed in the
        /// centre, unit and rpm readouts, gear box bottom-right, shift light at the top.
        /// </summary>
        public static Gauge CreateGauge(Transform parent, string name, float size)
        {
            var root = CreateRect(parent, name);
            root.sizeDelta = new Vector2(size, size);

            var backdrop = CreatePanel(root, "Backdrop", new Color(0.02f, 0.02f, 0.04f, 0.62f), false);
            backdrop.raycastTarget = false;
            Stretch((RectTransform)backdrop.transform, size * 0.03f, size * 0.03f, size * 0.03f, size * 0.03f);
            backdrop.sprite = ProceduralTextures.Circle();
            backdrop.type = Image.Type.Simple;

            Arc(root, "Track", new Color(1f, 1f, 1f, 0.16f), 0.75f);
            var arc = Arc(root, "RpmArc", Color.white, 0f);

            var speed = CreateText(root, "Speed", "0", size * 0.32f, TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor((RectTransform)speed.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.06f), new Vector2(size * 0.72f, size * 0.4f));
            var unit = CreateText(root, "Unit", "KM/H", size * 0.07f, TextDim, TextAlignmentOptions.Center, FontStyles.Bold);
            unit.characterSpacing = 4f;
            Anchor((RectTransform)unit.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -size * 0.17f), new Vector2(size * 0.5f, size * 0.1f));
            var rpm = CreateText(root, "Rpm", "0", size * 0.06f, TextDim, TextAlignmentOptions.Center);
            Anchor((RectTransform)rpm.transform, new Vector2(0.5f, 0f), new Vector2(0f, size * 0.03f), new Vector2(size * 0.5f, size * 0.08f));

            var gearBox = CreatePanel(root, "GearBox", new Color(0.02f, 0.02f, 0.04f, 0.88f));
            gearBox.raycastTarget = false;
            Anchor((RectTransform)gearBox.transform, new Vector2(1f, 0f), new Vector2(size * 0.03f, size * 0.03f), new Vector2(size * 0.28f, size * 0.32f));
            var gear = CreateText(gearBox.transform, "Gear", "1", size * 0.22f, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch((RectTransform)gear.transform);

            var shift = CreatePanel(root, "ShiftLight", new Color(0.3f, 0.3f, 0.3f, 0.6f), false);
            shift.raycastTarget = false;
            shift.sprite = ProceduralTextures.Circle();
            Anchor((RectTransform)shift.transform, new Vector2(0.5f, 1f), new Vector2(0f, -size * 0.1f), new Vector2(size * 0.075f, size * 0.075f));

            return new Gauge { Root = root, RpmArc = arc, Speed = speed, Unit = unit, Gear = gear, Rpm = rpm, ShiftLight = shift };
        }

        private static Image Arc(RectTransform root, string name, Color color, float fill)
        {
            var img = CreatePanel(root, name, color, false);
            img.raycastTarget = false;
            Stretch((RectTransform)img.transform);
            img.sprite = ProceduralTextures.Ring();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Bottom;
            img.fillClockwise = true;
            img.fillAmount = fill;
            // Origin sits at the bottom; turning the image 45 degrees clockwise puts it bottom-left
            // so a 0.75 sweep ends bottom-right, leaving the gap under the dial.
            img.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            return img;
        }

        /// <summary>Round touch pad: tinted disc with a thin rim, optional glyph and a small caption.</summary>
        public static HoldButton CreatePad(Transform parent, string name, string label, Sprite icon, Vector2 anchor, Vector2 position,
            Vector2 size, Color tint, bool flipIcon = false)
        {
            var image = CreatePanel(parent, name, tint, false);
            image.sprite = ProceduralTextures.Circle();
            Anchor((RectTransform)image.transform, anchor, position, size);
            var rim = CreatePanel(image.transform, "Rim", new Color(1f, 1f, 1f, 0.45f), false);
            rim.raycastTarget = false;
            Stretch((RectTransform)rim.transform);
            rim.sprite = ProceduralTextures.Ring();
            if (icon != null)
            {
                var glyph = CreatePanel(image.transform, "Icon", new Color(1f, 1f, 1f, 0.92f), false);
                glyph.raycastTarget = false;
                glyph.sprite = icon;
                glyph.preserveAspect = true;
                bool captioned = !string.IsNullOrEmpty(label);
                Anchor((RectTransform)glyph.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, captioned ? size.y * 0.07f : 0f), new Vector2(size.x * 0.5f, size.y * 0.5f));
                if (flipIcon) glyph.transform.localScale = new Vector3(-1f, 1f, 1f);
                if (captioned)
                {
                    var caption = CreateText(image.transform, "Label", label, size.y * 0.12f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.Center, FontStyles.Bold);
                    caption.characterSpacing = 3f;
                    Anchor((RectTransform)caption.transform, new Vector2(0.5f, 0f), new Vector2(0f, size.y * 0.11f), new Vector2(size.x * 0.8f, size.y * 0.15f));
                }
            }
            else
            {
                var text = CreateText(image.transform, "Label", label, size.y * 0.3f, new Color(1f, 1f, 1f, 0.92f), TextAlignmentOptions.Center, FontStyles.Bold);
                text.characterSpacing = 2f;
                Stretch((RectTransform)text.transform);
            }
            var button = image.gameObject.AddComponent<HoldButton>();
            button.EditorWire(image, tint, new Color(tint.r * 1.4f, tint.g * 1.4f, tint.b * 1.4f, Mathf.Min(1f, tint.a + 0.35f)));
            return button;
        }
    }
}
