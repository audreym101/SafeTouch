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

        /// <summary>Andika (SIL, made for early readers, covers Kinyarwanda) when imported, else Unity's built-in font.</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.Load<Font>("Fonts/Andika-Regular");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

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
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
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
            if (onClick != null) button.onClick.AddListener(() => onClick());

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
