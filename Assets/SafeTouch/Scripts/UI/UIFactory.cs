using System;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// Builds uGUI elements from code so every screen follows the style guide
    /// and the repo stays readable in a code review (no hand-edited scenes).
    /// </summary>
    public static class UIFactory
    {
        static Font font;
        static Sprite rounded;

        /// <summary>Andika (SIL, made for early readers, covers Kinyarwanda) for reading, else Unity's built-in font.</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.Load<Font>("Fonts/Andika-Regular");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        static Font boldFont, kidFont;

        /// <summary>Andika Bold, for short bold text such as messages under a form.</summary>
        public static Font BoldFont
        {
            get { if (boldFont == null) boldFont = Resources.Load<Font>("Fonts/Andika-Bold"); return boldFont; }
        }

        /// <summary>Sniglet ExtraBold: round, chunky letters for titles and buttons, the "kids' font" of the app.</summary>
        public static Font KidFont
        {
            get { if (kidFont == null) kidFont = Resources.Load<Font>("Fonts/Sniglet-ExtraBold"); return kidFont; }
        }

        /// <summary>Played on every button tap (AudioManager sets it to a soft pop).</summary>
        public static Action ButtonPressed;

        /// <summary>A 9-sliced rounded rectangle generated at runtime (no texture asset needed).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64, radius = 24;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return rounded;
            }
        }

        static Sprite circle;

        /// <summary>A smooth filled circle, for stars, the mic dot and colour swatches.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int size = 96;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                var pixels = new Color32[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255));
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return circle;
            }
        }

        public static Image Dot(Transform parent, string name, Color color, float size)
        {
            var img = Panel(parent, name, color, false);
            img.sprite = Circle;
            img.preserveAspect = true;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            return img;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image Panel(Transform parent, string name, Color color, bool roundedCorners)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (roundedCorners)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align, bool bold)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            // Titles and buttons in the round kids' font; short bold text in Andika Bold; the rest in Andika.
            var bolder = !bold ? null : size >= Theme.Body ? KidFont : BoldFont;
            t.font = bolder != null ? bolder : Font;
            t.fontStyle = bold && bolder == null ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.1f;
            t.raycastTarget = false;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = size * 1.4f;
            return t;
        }

        public static Button Button(Transform parent, string label, Color background, Color textColor, Action onClick)
        {
            var img = Panel(parent, "Button " + label, background, true);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;
            button.onClick.AddListener(() => { if (ButtonPressed != null) ButtonPressed(); });
            if (onClick != null) button.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<Squish>();   // the button squashes a little under the finger and springs back

            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = Theme.TouchTarget;
            le.preferredHeight = Theme.TouchTarget;
            le.flexibleWidth = 1f;

            var text = Label(img.transform, label, Theme.Body, textColor, TextAnchor.MiddleCenter, true);
            Stretch((RectTransform)text.transform);
            ((RectTransform)text.transform).offsetMin = new Vector2(24f, 0f);
            ((RectTransform)text.transform).offsetMax = new Vector2(-24f, 0f);
            return button;
        }

        /// <summary>
        /// A large single-line text box with a placeholder. On a phone, tapping it opens
        /// the keyboard that suits the content (email, phone pad or numbers).
        /// </summary>
        public static InputField Input(Transform parent, string placeholder, InputField.ContentType content, TouchScreenKeyboardType keyboard, int maxLength)
        {
            var box = Panel(parent, "Input " + placeholder, Theme.Card, true);
            var le = box.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Theme.TouchTarget;
            le.flexibleWidth = 1f;
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = Theme.WithAlpha(Theme.Muted, 0.5f);
            outline.effectDistance = new Vector2(3f, -3f);

            var text = Label(box.transform, "", Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, false);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var hint = Label(box.transform, placeholder, Theme.Body, Theme.WithAlpha(Theme.Muted, 0.8f), TextAnchor.MiddleLeft, false);
            hint.fontStyle = FontStyle.Italic;
            foreach (var t in new[] { text, hint })
            {
                var rt = (RectTransform)t.transform;
                Stretch(rt);
                rt.offsetMin = new Vector2(32f, 0f);
                rt.offsetMax = new Vector2(-32f, 0f);
            }

            var field = box.gameObject.AddComponent<InputField>();
            field.targetGraphic = box;
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            field.contentType = content;
            field.keyboardType = keyboard;
            field.characterLimit = maxLength;
            return field;
        }

        /// <summary>
        /// A tick box with a label that wraps; the whole row is tappable. The box
        /// fills with colour when ticked (no tick glyph: the built-in font has none).
        /// </summary>
        public static Toggle Checkbox(Transform parent, string label, Action<bool> changed)
        {
            var row = Rect(parent, "Checkbox");
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 28f;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = Theme.Clear;   // makes the whole row tappable

            var box = Panel(row, "Box", Theme.Card, true);
            var boxSize = box.gameObject.AddComponent<LayoutElement>();
            boxSize.minWidth = boxSize.preferredWidth = 84f;
            boxSize.minHeight = boxSize.preferredHeight = 84f;
            var ring = box.gameObject.AddComponent<Outline>();
            ring.effectColor = Theme.Primary;
            ring.effectDistance = new Vector2(4f, -4f);
            var fill = Panel(box.transform, "Ticked", Theme.Primary, true);
            var frt = (RectTransform)fill.transform;
            Stretch(frt);
            frt.offsetMin = new Vector2(14f, 14f);
            frt.offsetMax = new Vector2(-14f, -14f);

            var text = Label(row, label, Theme.Label, Theme.Ink, TextAnchor.UpperLeft, false);
            text.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = fill;
            toggle.isOn = false;
            if (changed != null) toggle.onValueChanged.AddListener(v => changed(v));
            return toggle;
        }

        public static void SetButtonLabel(Button button, string label)
        {
            var t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = label;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, int padding)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup Row(Transform parent, string name, float spacing, float height)
        {
            var rt = Rect(parent, name);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            // childForceExpandHeight makes a layout group report itself as stretchable, so on
            // tall phones (Samsung A70) rows used to soak up the spare height and grow huge.
            // Rows keep their height; spare height goes to the screen's spacers instead.
            le.flexibleHeight = 0f;
            return h;
        }

        public static void Spacer(Transform parent, float flexible)
        {
            var le = Rect(parent, "Spacer").gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = flexible;
        }

        public static void FixedHeight(Component c, float height)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        /// <summary>A vertical scroll list; returns the content transform to add children to.</summary>
        public static RectTransform ScrollList(Transform parent, float spacing)
        {
            var scrollRt = Rect(parent, "Scroll");
            scrollRt.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = Rect(scrollRt, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var col = Column(content, spacing, 0);
            col.padding = new RectOffset(0, 0, 8, 48);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Remove(t.GetChild(i).gameObject);
        }

        /// <summary>
        /// Removes a UI element at once. Destroy only happens at the end of the
        /// frame, so the element is first hidden and detached, letting layouts
        /// rebuilt in the same frame ignore it; in edit mode (the screenshot
        /// tool) DestroyImmediate is used, because Destroy does nothing there.
        /// </summary>
        public static void Remove(GameObject go)
        {
            if (go == null) return;
            if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(go); return; }
            go.SetActive(false);
            go.transform.SetParent(null, false);
            UnityEngine.Object.Destroy(go);
        }
    }
}
