using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// Friendly pictures drawn in code, so every screen has something to look at: Keza, her dog, a sun,
    /// clouds, hills and stars. They are made from the same circles and rounded shapes as the rest of the
    /// UI (no image files to import) and never take touches, so buttons stay easy to hit.
    /// </summary>
    public static class Decor
    {
        // ---------- scenery behind a whole screen ----------

        /// <summary>
        /// Sun, clouds, hills and stars behind a screen's content. Soft colours, so text stays easy to read.
        /// The pieces are placed from the screen's name, so every screen looks a little different.
        /// </summary>
        public static void Background(RectTransform root, string seed)
        {
            var layer = UIFactory.Rect(root, "Scenery");
            UIFactory.Stretch(layer);
            layer.SetAsFirstSibling();
            int h = Hash(seed);

            // Hills along the bottom.
            Blob(layer, "Hill back", new Color(0.55f, 0.80f, 0.55f, 0.35f), 0.80f, 0f, 1300f, 1f, 0.5f, 0f);
            Blob(layer, "Hill front", new Color(0.38f, 0.70f, 0.45f, 0.30f), 0.18f, 0f, 1000f, 1f, 0.5f, 0f);

            // The sun, top right, with a soft halo.
            float sunX = (h & 1) == 0 ? 0.88f : 0.12f;
            Blob(layer, "Sun halo", new Color(1f, 0.85f, 0.35f, 0.25f), sunX, 0.955f, 330f, 1f, 0.5f, 0.5f);
            Blob(layer, "Sun", new Color(1f, 0.78f, 0.25f, 0.95f), sunX, 0.955f, 200f, 1f, 0.5f, 0.5f);

            // Clouds.
            Cloud(layer, "Cloud 1", 0.50f + 0.18f * (((h >> 2) & 3) - 1.5f) / 1.5f, 0.915f, 330f);
            Cloud(layer, "Cloud 2", 0.22f + 0.12f * (((h >> 4) & 3) / 3f), 0.80f, 230f);

            // Stars, scattered at the sides.
            for (int i = 0; i < 6; i++)
            {
                int k = Hash(seed + i);
                float x = (i % 2 == 0 ? 0.04f : 0.90f) + 0.06f * ((k & 7) / 7f);
                float y = 0.12f + 0.60f * (((k >> 3) & 15) / 15f);
                Star(layer, new Color(1f, 0.78f, 0.25f, 0.55f), x, y, 54f + 20f * ((k >> 7) & 3));
            }
        }

        static int Hash(string s)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in s) hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }

        /// <summary>A round shape placed by anchor, with a size in canvas units.</summary>
        static Image Blob(Transform parent, string name, Color color, float ax, float ay, float width, float aspect, float pivotX, float pivotY)
        {
            var img = UIFactory.Panel(parent, name, color, false);
            img.sprite = UIFactory.Circle;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(pivotX, pivotY);
            rt.sizeDelta = new Vector2(width, width * aspect);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        static void Cloud(Transform parent, string name, float ax, float ay, float width)
        {
            var root = UIFactory.Rect(parent, name);
            root.anchorMin = root.anchorMax = new Vector2(ax, ay);
            root.sizeDelta = new Vector2(width, width * 0.5f);
            var white = new Color(1f, 1f, 1f, 0.75f);
            Part(root, "a", UIFactory.Circle, white, 0.30f, 0.45f, 0.55f, 0.9f, 0f);
            Part(root, "b", UIFactory.Circle, white, 0.55f, 0.58f, 0.62f, 1.1f, 0f);
            Part(root, "c", UIFactory.Circle, white, 0.78f, 0.42f, 0.46f, 0.8f, 0f);
            Part(root, "base", UIFactory.Circle, white, 0.52f, 0.30f, 0.90f, 0.5f, 0f);
        }

        static void Star(Transform parent, Color color, float ax, float ay, float size)
        {
            var root = UIFactory.Rect(parent, "Star");
            root.anchorMin = root.anchorMax = new Vector2(ax, ay);
            root.sizeDelta = new Vector2(size, size);
            for (int i = 0; i < 2; i++)
            {
                var square = UIFactory.Panel(root, "Point", color, false);
                square.raycastTarget = false;
                var rt = square.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size * 0.7f, size * 0.7f);
                rt.localEulerAngles = new Vector3(0f, 0f, i * 45f);
            }
        }

        // ---------- characters ----------

        /// <summary>
        /// A square picture area for a character: preferred and minimum size, so layouts keep it that size.
        /// </summary>
        static RectTransform Frame(Transform parent, string name, float size, float aspect)
        {
            var rt = UIFactory.Rect(parent, name);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size * aspect;
            le.minHeight = le.preferredHeight = size;
            return rt;
        }

        /// <summary>A shape placed by its centre inside a frame, as fractions of the frame (0..1), turned by rotation degrees.</summary>
        static Image Part(RectTransform frame, string name, Sprite sprite, Color color, float cx, float cy, float w, float h, float rotation)
        {
            var img = UIFactory.Panel(frame, name, color, false);
            img.sprite = sprite;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(cx - w * 0.5f, cy - h * 0.5f);
            rt.anchorMax = new Vector2(cx + w * 0.5f, cy + h * 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, rotation);
            return img;
        }

        static Image Oval(RectTransform frame, string name, Color color, float cx, float cy, float w, float h, float rotation = 0f)
        {
            return Part(frame, name, UIFactory.Circle, color, cx, cy, w, h, rotation);
        }

        /// <summary>
        /// Keza, the guide: a big round head with hair puffs, a smile and rosy cheeks, an orange body and a
        /// waving hand. She is square; size is in canvas units.
        /// </summary>
        public static RectTransform Keza(Transform parent, float size, Color? skinTone = null)
        {
            var skin = skinTone ?? new Color(0.72f, 0.45f, 0.27f);
            var hair = new Color(0.17f, 0.11f, 0.09f);
            var dress = new Color(1f, 0.65f, 0.28f);
            var f = Frame(parent, "Keza", size, 1f);

            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.13f), 0.5f, 0.035f, 0.55f, 0.07f);
            Oval(f, "Leg L", new Color(0.25f, 0.33f, 0.62f), 0.42f, 0.085f, 0.14f, 0.15f);
            Oval(f, "Leg R", new Color(0.25f, 0.33f, 0.62f), 0.58f, 0.085f, 0.14f, 0.15f);
            Oval(f, "Arm L", skin, 0.255f, 0.31f, 0.13f, 0.28f, 22f);
            Oval(f, "Body", dress, 0.5f, 0.27f, 0.42f, 0.44f);
            Oval(f, "Belly shine", new Color(1f, 1f, 1f, 0.22f), 0.44f, 0.34f, 0.12f, 0.2f, 12f);
            Oval(f, "Arm R", skin, 0.78f, 0.47f, 0.13f, 0.28f, -32f);   // waving
            Oval(f, "Hand R", skin, 0.835f, 0.60f, 0.12f, 0.12f);

            Oval(f, "Puff L", hair, 0.26f, 0.78f, 0.22f, 0.22f);
            Oval(f, "Puff R", hair, 0.74f, 0.78f, 0.22f, 0.22f);
            Oval(f, "Head", skin, 0.5f, 0.66f, 0.56f, 0.52f);
            Oval(f, "Hair", hair, 0.5f, 0.86f, 0.5f, 0.2f);
            Oval(f, "Hair front", skin, 0.5f, 0.795f, 0.46f, 0.12f);
            Oval(f, "Cheek L", new Color(1f, 0.45f, 0.5f, 0.45f), 0.31f, 0.58f, 0.095f, 0.075f);
            Oval(f, "Cheek R", new Color(1f, 0.45f, 0.5f, 0.45f), 0.69f, 0.58f, 0.095f, 0.075f);
            Oval(f, "Eye L", Color.white, 0.40f, 0.675f, 0.115f, 0.135f);
            Oval(f, "Eye R", Color.white, 0.60f, 0.675f, 0.115f, 0.135f);
            Oval(f, "Pupil L", new Color(0.1f, 0.12f, 0.2f), 0.405f, 0.665f, 0.07f, 0.09f);
            Oval(f, "Pupil R", new Color(0.1f, 0.12f, 0.2f), 0.605f, 0.665f, 0.07f, 0.09f);
            Oval(f, "Shine L", Color.white, 0.42f, 0.685f, 0.025f, 0.03f);
            Oval(f, "Shine R", Color.white, 0.62f, 0.685f, 0.025f, 0.03f);
            Oval(f, "Mouth", new Color(0.55f, 0.15f, 0.18f), 0.5f, 0.555f, 0.17f, 0.085f);
            Oval(f, "Mouth cover", skin, 0.5f, 0.585f, 0.19f, 0.07f);
            return f;
        }

        /// <summary>A happy brown dog with floppy ears, a wagging tail and a blue collar. Wider than tall.</summary>
        public static RectTransform Dog(Transform parent, float size)
        {
            var coat = new Color(0.56f, 0.38f, 0.26f);
            var dark = new Color(0.40f, 0.26f, 0.17f);
            var f = Frame(parent, "Dog", size, 1.15f);

            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.13f), 0.48f, 0.05f, 0.7f, 0.08f);
            Oval(f, "Tail", coat, 0.12f, 0.46f, 0.1f, 0.3f, 28f);
            Oval(f, "Leg B", dark, 0.28f, 0.14f, 0.1f, 0.2f);
            Oval(f, "Leg F", dark, 0.56f, 0.14f, 0.1f, 0.2f);
            Oval(f, "Body", coat, 0.40f, 0.30f, 0.56f, 0.34f);
            Oval(f, "Collar", new Color(0.25f, 0.45f, 0.85f), 0.62f, 0.42f, 0.07f, 0.2f, 14f);
            Oval(f, "Tag", new Color(1f, 0.78f, 0.25f), 0.64f, 0.31f, 0.045f, 0.06f);
            Oval(f, "Ear back", dark, 0.62f, 0.72f, 0.12f, 0.27f, 18f);
            Oval(f, "Head", coat, 0.75f, 0.58f, 0.36f, 0.4f);
            Oval(f, "Ear front", dark, 0.89f, 0.70f, 0.11f, 0.26f, -22f);
            Oval(f, "Snout", new Color(0.88f, 0.72f, 0.54f), 0.84f, 0.50f, 0.2f, 0.17f);
            Oval(f, "Nose", new Color(0.15f, 0.12f, 0.12f), 0.915f, 0.545f, 0.06f, 0.06f);
            Oval(f, "Tongue", new Color(1f, 0.5f, 0.55f), 0.86f, 0.40f, 0.055f, 0.1f);
            Oval(f, "Eye", new Color(0.12f, 0.1f, 0.12f), 0.78f, 0.64f, 0.05f, 0.06f);
            Oval(f, "Eye shine", Color.white, 0.79f, 0.655f, 0.015f, 0.02f);
            return f;
        }

        /// <summary>Keza and her dog side by side, as a banner picture for the welcome and login screens.</summary>
        public static RectTransform Friends(Transform parent, float height)
        {
            var row = UIFactory.Row(parent, "Friends", 0f, height);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.LowerCenter;
            Keza(row.transform, height);
            Dog(row.transform, height * 0.62f);
            return (RectTransform)row.transform;
        }

        /// <summary>A bright, round number badge for a lesson card (or any list item).</summary>
        public static Image Badge(RectTransform parent, string text, Color color, float size)
        {
            var badge = Blob(parent, "Badge", color, 1f, 0.5f, size, 1f, 1f, 0.5f);
            badge.rectTransform.anchoredPosition = new Vector2(-28f, 0f);
            badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Blob(badge.transform, "Ring", new Color(1f, 1f, 1f, 0.35f), 0.5f, 0.5f, size * 0.82f, 1f, 0.5f, 0.5f);
            var label = UIFactory.Label(badge.transform, text, (int)(size * 0.5f), Color.white, TextAnchor.MiddleCenter, true);
            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            return badge;
        }
    }
}
