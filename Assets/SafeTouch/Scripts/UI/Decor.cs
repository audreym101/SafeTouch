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

            // Rolling hills along the bottom: wide ovals whose top halves show.
            Blob(layer, "Hill back", new Color(0.62f, 0.84f, 0.60f, 0.55f), 0.78f, 0f, 1700f, 0.26f, 0.5f, 0.5f);
            Blob(layer, "Hill front", new Color(0.46f, 0.76f, 0.50f, 0.50f), 0.15f, 0f, 1400f, 0.20f, 0.5f, 0.5f);

            // The sun, top right, with a soft halo.
            Blob(layer, "Sun halo", new Color(1f, 0.85f, 0.35f, 0.25f), 0.88f, 0.955f, 330f, 1f, 0.5f, 0.5f);
            Blob(layer, "Sun", new Color(1f, 0.78f, 0.25f, 0.95f), 0.88f, 0.955f, 200f, 1f, 0.5f, 0.5f);

            // Clouds.
            Cloud(layer, "Cloud 1", 0.50f + 0.14f * (((h >> 2) & 3) - 1.5f) / 1.5f, 0.915f, 330f);
            Cloud(layer, "Cloud 2", 0.22f + 0.12f * (((h >> 4) & 3) / 3f), 0.80f, 230f);

            // A few sparkles around the sun and clouds.
            var spark = new Color(1f, 0.89f, 0.50f);
            Sparkle(layer, spark, 0.72f, 0.985f, 56f);
            Sparkle(layer, spark, 0.965f, 0.885f, 44f);
            Sparkle(layer, spark, 0.30f, 0.975f, 40f);
            Sparkle(layer, spark, 0.06f, 0.84f, 46f);
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

        /// <summary>A small twinkle: a plus with a round middle. Straight edges only, so it stays crisp at any size.</summary>
        static void Sparkle(Transform parent, Color color, float ax, float ay, float size)
        {
            var root = UIFactory.Rect(parent, "Sparkle");
            root.anchorMin = root.anchorMax = new Vector2(ax, ay);
            root.sizeDelta = new Vector2(size, size);
            Bar(root, color, size, size * 0.26f);
            Bar(root, color, size * 0.26f, size);
            var middle = UIFactory.Panel(root, "Middle", color, false);
            middle.sprite = UIFactory.Circle;
            middle.raycastTarget = false;
            var rt = middle.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size * 0.5f, size * 0.5f);
        }

        static void Bar(RectTransform parent, Color color, float width, float height)
        {
            var bar = UIFactory.Panel(parent, "Bar", color, false);
            bar.raycastTarget = false;
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
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
            Hop(Keza(row.transform, height), height * 0.035f, 1.1f, 0f);
            Hop(Dog(row.transform, height * 0.62f), height * 0.05f, 0.55f, 0.3f);
            return (RectTransform)row.transform;
        }

        /// <summary>Makes a picture hop gently in a loop (see Bob).</summary>
        public static RectTransform Hop(RectTransform picture, float height, float period, float phase)
        {
            var bob = picture.gameObject.AddComponent<Bob>();
            bob.height = height;
            bob.period = period;
            bob.phase = phase;
            return picture;
        }

        /// <summary>How a cartoon kid looks: skin, hair (and its style), clothes, glasses, and arms up or waving.</summary>
        public struct KidLook
        {
            public Color skin, hair, top, bottom, shoes;
            public int hairStyle;   // 0 curly, 1 bob, 2 puffs, 3 spiky, 4 long
            public bool skirt, glasses, armsUp;
        }

        /// <summary>Five friends who look different from each other, like a real class in Kigali.</summary>
        public static readonly KidLook[] Kids =
        {
            new KidLook { skin = new Color(0.45f, 0.28f, 0.17f), hair = new Color(0.12f, 0.08f, 0.06f), top = new Color(1f, 0.78f, 0.2f), bottom = new Color(0.25f, 0.45f, 0.4f), shoes = new Color(0.2f, 0.65f, 0.5f), hairStyle = 0, armsUp = true },
            new KidLook { skin = new Color(0.96f, 0.80f, 0.66f), hair = new Color(0.93f, 0.42f, 0.18f), top = new Color(0.98f, 0.45f, 0.62f), bottom = new Color(0.98f, 0.45f, 0.62f), shoes = new Color(0.85f, 0.2f, 0.3f), hairStyle = 1, skirt = true },
            new KidLook { skin = new Color(0.62f, 0.40f, 0.25f), hair = new Color(0.1f, 0.07f, 0.06f), top = new Color(0.4f, 0.75f, 0.95f), bottom = new Color(0.22f, 0.36f, 0.7f), shoes = new Color(0.95f, 0.5f, 0.2f), hairStyle = 2, armsUp = true },
            new KidLook { skin = new Color(0.87f, 0.65f, 0.48f), hair = new Color(0.45f, 0.28f, 0.15f), top = new Color(0.35f, 0.78f, 0.45f), bottom = new Color(0.95f, 0.72f, 0.25f), shoes = new Color(0.3f, 0.3f, 0.35f), hairStyle = 3, glasses = true },
            new KidLook { skin = new Color(0.72f, 0.45f, 0.27f), hair = new Color(0.15f, 0.1f, 0.08f), top = new Color(0.62f, 0.45f, 0.85f), bottom = new Color(0.62f, 0.45f, 0.85f), shoes = new Color(0.98f, 0.8f, 0.3f), hairStyle = 4, skirt = true, armsUp = true },
        };

        /// <summary>
        /// A cartoon kid with a big happy smile, jumping with arms up or waving. Square; size is in canvas units.
        /// Drawn from circles like Keza, so it stays sharp on any phone.
        /// </summary>
        public static RectTransform Kid(Transform parent, float size, KidLook k)
        {
            var f = Frame(parent, "Kid", size, 0.82f);
            var cheek = new Color(1f, 0.45f, 0.5f, 0.4f);
            var legs = k.skirt ? k.skin : k.bottom;

            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.03f, 0.6f, 0.06f);
            Oval(f, "Leg L", legs, 0.41f, 0.13f, 0.13f, 0.2f, k.armsUp ? 12f : 0f);
            Oval(f, "Leg R", legs, 0.59f, 0.13f, 0.13f, 0.2f, k.armsUp ? -12f : 0f);
            Oval(f, "Shoe L", k.shoes, k.armsUp ? 0.37f : 0.39f, 0.045f, 0.17f, 0.07f);
            Oval(f, "Shoe R", k.shoes, k.armsUp ? 0.63f : 0.61f, 0.045f, 0.17f, 0.07f);

            // Arms behind the body: both up for a jump, or one waving.
            if (k.armsUp)
            {
                Oval(f, "Arm L", k.skin, 0.22f, 0.49f, 0.12f, 0.3f, 38f);
                Oval(f, "Arm R", k.skin, 0.78f, 0.49f, 0.12f, 0.3f, -38f);
                Oval(f, "Hand L", k.skin, 0.13f, 0.6f, 0.12f, 0.1f);
                Oval(f, "Hand R", k.skin, 0.87f, 0.6f, 0.12f, 0.1f);
            }
            else
            {
                Oval(f, "Arm L", k.skin, 0.27f, 0.3f, 0.12f, 0.26f, 18f);
                Oval(f, "Arm R", k.skin, 0.78f, 0.46f, 0.12f, 0.28f, -32f);
                Oval(f, "Hand R", k.skin, 0.84f, 0.58f, 0.12f, 0.1f);
            }
            if (k.skirt) Oval(f, "Skirt", k.bottom, 0.5f, 0.22f, 0.5f, 0.2f);
            Oval(f, "Body", k.top, 0.5f, 0.32f, 0.42f, 0.32f);
            Oval(f, "Sleeve L", k.top, 0.31f, 0.4f, 0.14f, 0.12f);
            Oval(f, "Sleeve R", k.top, 0.69f, 0.4f, 0.14f, 0.12f);
            Oval(f, "Shine", new Color(1f, 1f, 1f, 0.2f), 0.43f, 0.37f, 0.1f, 0.14f, 12f);

            // Hair behind the head.
            if (k.hairStyle == 0)
                for (int i = 0; i < 7; i++)
                {
                    float a = Mathf.Lerp(200f, -20f, i / 6f) * Mathf.Deg2Rad;
                    Oval(f, "Curl", k.hair, 0.5f + Mathf.Cos(a) * 0.25f, 0.69f + Mathf.Sin(a) * 0.23f, 0.17f, 0.17f);
                }
            if (k.hairStyle == 1) Oval(f, "Hair back", k.hair, 0.5f, 0.66f, 0.62f, 0.5f);
            if (k.hairStyle == 2)
            {
                Oval(f, "Puff L", k.hair, 0.22f, 0.79f, 0.2f, 0.2f);
                Oval(f, "Puff R", k.hair, 0.78f, 0.79f, 0.2f, 0.2f);
                Oval(f, "Ribbon", new Color(1f, 0.35f, 0.45f), 0.31f, 0.84f, 0.08f, 0.05f, 30f);
            }
            if (k.hairStyle == 4) Oval(f, "Hair back", k.hair, 0.5f, 0.6f, 0.6f, 0.62f);

            Oval(f, "Head", k.skin, 0.5f, 0.68f, 0.5f, 0.45f);

            // Hair on top of the head.
            if (k.hairStyle == 0 || k.hairStyle == 2) Oval(f, "Hair", k.hair, 0.5f, 0.865f, 0.46f, 0.15f);
            if (k.hairStyle == 1 || k.hairStyle == 4) Oval(f, "Fringe", k.hair, 0.5f, 0.85f, 0.5f, 0.17f, 4f);
            if (k.hairStyle == 3)
            {
                Oval(f, "Spike L", k.hair, 0.38f, 0.88f, 0.13f, 0.17f, 25f);
                Oval(f, "Spike", k.hair, 0.5f, 0.915f, 0.13f, 0.17f);
                Oval(f, "Spike R", k.hair, 0.62f, 0.88f, 0.13f, 0.17f, -25f);
                Oval(f, "Hair", k.hair, 0.5f, 0.855f, 0.48f, 0.13f);
            }

            Oval(f, "Cheek L", cheek, 0.32f, 0.6f, 0.09f, 0.07f);
            Oval(f, "Cheek R", cheek, 0.68f, 0.6f, 0.09f, 0.07f);
            if (k.glasses)
            {
                Oval(f, "Glass L", new Color(0.2f, 0.2f, 0.25f), 0.405f, 0.675f, 0.15f, 0.15f);
                Oval(f, "Glass R", new Color(0.2f, 0.2f, 0.25f), 0.595f, 0.675f, 0.15f, 0.15f);
                Oval(f, "Bridge", new Color(0.2f, 0.2f, 0.25f), 0.5f, 0.69f, 0.06f, 0.02f);
            }
            Oval(f, "Eye L", Color.white, 0.405f, 0.675f, 0.11f, 0.12f);
            Oval(f, "Eye R", Color.white, 0.595f, 0.675f, 0.11f, 0.12f);
            Oval(f, "Pupil L", new Color(0.1f, 0.12f, 0.2f), 0.41f, 0.665f, 0.065f, 0.08f);
            Oval(f, "Pupil R", new Color(0.1f, 0.12f, 0.2f), 0.6f, 0.665f, 0.065f, 0.08f);
            Oval(f, "Shine L", Color.white, 0.425f, 0.685f, 0.025f, 0.028f);
            Oval(f, "Shine R", Color.white, 0.615f, 0.685f, 0.025f, 0.028f);
            // A big open smile, with a tongue.
            Oval(f, "Mouth", new Color(0.55f, 0.13f, 0.17f), 0.5f, 0.565f, 0.17f, 0.11f);
            Oval(f, "Tongue", new Color(1f, 0.5f, 0.55f), 0.5f, 0.535f, 0.1f, 0.045f);
            Oval(f, "Mouth cover", k.skin, 0.5f, 0.6f, 0.2f, 0.06f);
            return f;
        }

        /// <summary>The five friends in a row, hopping one after the other: the banner for the menu.</summary>
        public static RectTransform Gang(Transform parent, float height)
        {
            var row = UIFactory.Row(parent, "Gang", -height * 0.1f, height);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.LowerCenter;
            for (int i = 0; i < Kids.Length; i++)
                Hop(Kid(row.transform, height, Kids[i]), height * (Kids[i].armsUp ? 0.09f : 0.04f), 0.8f + 0.07f * i, i * 0.21f);
            return (RectTransform)row.transform;
        }

        // ---- Little animals for the lesson cards ----

        /// <summary>One animal per lesson card: bunny, cat, lion, elephant, owl (Memory Quest) and frog (the game).</summary>
        public enum AnimalKind { Bunny, Cat, Lion, Elephant, Owl, Frog }

        static readonly Color Pupil = new Color(0.1f, 0.12f, 0.2f);
        static readonly Color Blush = new Color(1f, 0.45f, 0.5f, 0.45f);

        /// <summary>
        /// A small animal on a lesson card, standing just left of the number badge and hopping gently in its own
        /// rhythm. It is drawn over the card and never takes a touch, so the whole card stays one big button.
        /// </summary>
        public static RectTransform Animal(RectTransform card, AnimalKind kind, float size)
        {
            var f = Frame(card, kind.ToString(), size, 1f);
            f.GetComponent<LayoutElement>().ignoreLayout = true;
            f.anchorMin = f.anchorMax = new Vector2(1f, 0.5f);
            f.pivot = new Vector2(1f, 0.5f);
            f.sizeDelta = new Vector2(size, size);
            f.anchoredPosition = new Vector2(-136f, 0f);
            switch (kind)
            {
                case AnimalKind.Bunny: Bunny(f); break;
                case AnimalKind.Cat: Cat(f); break;
                case AnimalKind.Lion: Lion(f); break;
                case AnimalKind.Elephant: Elephant(f); break;
                case AnimalKind.Owl: Owl(f); break;
                default: Frog(f); break;
            }
            int i = (int)kind;
            return Hop(f, size * (kind == AnimalKind.Frog || kind == AnimalKind.Bunny ? 0.12f : 0.06f), 0.75f + 0.11f * i, i * 0.17f);
        }

        static void Eyes(RectTransform f, float y, float apart, float size)
        {
            Oval(f, "Eye L", Pupil, 0.5f - apart, y, size, size * 1.15f);
            Oval(f, "Eye R", Pupil, 0.5f + apart, y, size, size * 1.15f);
            Oval(f, "Shine L", Color.white, 0.5f - apart + size * 0.2f, y + size * 0.25f, size * 0.35f, size * 0.35f);
            Oval(f, "Shine R", Color.white, 0.5f + apart + size * 0.2f, y + size * 0.25f, size * 0.35f, size * 0.35f);
        }

        static void Bunny(RectTransform f)
        {
            var fur = new Color(0.95f, 0.95f, 0.97f);
            var pink = new Color(1f, 0.7f, 0.78f);
            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.04f, 0.6f, 0.07f);
            Oval(f, "Ear L", fur, 0.38f, 0.83f, 0.15f, 0.38f, 10f);
            Oval(f, "Ear R", fur, 0.62f, 0.83f, 0.15f, 0.38f, -10f);
            Oval(f, "Inner L", pink, 0.38f, 0.83f, 0.07f, 0.28f, 10f);
            Oval(f, "Inner R", pink, 0.62f, 0.83f, 0.07f, 0.28f, -10f);
            Oval(f, "Body", fur, 0.5f, 0.25f, 0.52f, 0.4f);
            Oval(f, "Foot L", fur, 0.36f, 0.07f, 0.2f, 0.1f);
            Oval(f, "Foot R", fur, 0.64f, 0.07f, 0.2f, 0.1f);
            Oval(f, "Head", fur, 0.5f, 0.52f, 0.5f, 0.42f);
            Eyes(f, 0.55f, 0.1f, 0.07f);
            Oval(f, "Cheek L", Blush, 0.33f, 0.47f, 0.09f, 0.06f);
            Oval(f, "Cheek R", Blush, 0.67f, 0.47f, 0.09f, 0.06f);
            Oval(f, "Nose", pink, 0.5f, 0.47f, 0.07f, 0.05f);
        }

        static void Cat(RectTransform f)
        {
            var fur = new Color(0.98f, 0.64f, 0.3f);
            var light = new Color(1f, 0.88f, 0.72f);
            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.04f, 0.6f, 0.07f);
            Oval(f, "Tail", fur, 0.8f, 0.3f, 0.1f, 0.42f, -30f);
            Oval(f, "Body", fur, 0.5f, 0.25f, 0.5f, 0.42f);
            Oval(f, "Belly", light, 0.5f, 0.22f, 0.26f, 0.28f);
            Oval(f, "Ear L", fur, 0.32f, 0.8f, 0.16f, 0.24f, 25f);
            Oval(f, "Ear R", fur, 0.68f, 0.8f, 0.16f, 0.24f, -25f);
            Oval(f, "Head", fur, 0.5f, 0.6f, 0.54f, 0.44f);
            Oval(f, "Stripe", new Color(0.88f, 0.48f, 0.18f), 0.5f, 0.79f, 0.07f, 0.1f);
            Oval(f, "Muzzle", light, 0.5f, 0.52f, 0.24f, 0.15f);
            Eyes(f, 0.63f, 0.11f, 0.07f);
            Oval(f, "Nose", new Color(0.9f, 0.4f, 0.45f), 0.5f, 0.56f, 0.06f, 0.04f);
            Oval(f, "Whisker L", new Color(0.4f, 0.3f, 0.25f), 0.25f, 0.53f, 0.16f, 0.015f, 8f);
            Oval(f, "Whisker R", new Color(0.4f, 0.3f, 0.25f), 0.75f, 0.53f, 0.16f, 0.015f, -8f);
        }

        static void Lion(RectTransform f)
        {
            var mane = new Color(0.85f, 0.45f, 0.15f);
            var fur = new Color(1f, 0.78f, 0.3f);
            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.04f, 0.6f, 0.07f);
            Oval(f, "Body", fur, 0.5f, 0.24f, 0.5f, 0.38f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Oval(f, "Mane", mane, 0.5f + Mathf.Cos(a) * 0.25f, 0.58f + Mathf.Sin(a) * 0.23f, 0.22f, 0.22f);
            }
            Oval(f, "Ear L", fur, 0.33f, 0.76f, 0.12f, 0.12f);
            Oval(f, "Ear R", fur, 0.67f, 0.76f, 0.12f, 0.12f);
            Oval(f, "Head", fur, 0.5f, 0.58f, 0.46f, 0.42f);
            Oval(f, "Muzzle", new Color(1f, 0.9f, 0.7f), 0.5f, 0.49f, 0.22f, 0.15f);
            Eyes(f, 0.62f, 0.1f, 0.065f);
            Oval(f, "Nose", new Color(0.45f, 0.25f, 0.2f), 0.5f, 0.53f, 0.08f, 0.05f);
            Oval(f, "Smile", new Color(0.45f, 0.25f, 0.2f), 0.5f, 0.46f, 0.1f, 0.025f);
        }

        static void Elephant(RectTransform f)
        {
            var skin = new Color(0.62f, 0.68f, 0.8f);
            var dark = new Color(0.52f, 0.58f, 0.72f);
            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.04f, 0.7f, 0.07f);
            Oval(f, "Body", skin, 0.5f, 0.24f, 0.6f, 0.38f);
            Oval(f, "Leg L", dark, 0.36f, 0.09f, 0.14f, 0.14f);
            Oval(f, "Leg R", dark, 0.64f, 0.09f, 0.14f, 0.14f);
            Oval(f, "Ear L", dark, 0.22f, 0.58f, 0.32f, 0.38f, 10f);
            Oval(f, "Ear R", dark, 0.78f, 0.58f, 0.32f, 0.38f, -10f);
            Oval(f, "Inner L", new Color(1f, 0.75f, 0.8f), 0.23f, 0.58f, 0.18f, 0.24f, 10f);
            Oval(f, "Inner R", new Color(1f, 0.75f, 0.8f), 0.77f, 0.58f, 0.18f, 0.24f, -10f);
            Oval(f, "Head", skin, 0.5f, 0.6f, 0.42f, 0.4f);
            Oval(f, "Trunk", skin, 0.52f, 0.38f, 0.12f, 0.3f, -12f);
            Oval(f, "Trunk tip", dark, 0.56f, 0.24f, 0.11f, 0.06f);
            Eyes(f, 0.66f, 0.1f, 0.06f);
            Oval(f, "Cheek L", Blush, 0.36f, 0.56f, 0.08f, 0.05f);
            Oval(f, "Cheek R", Blush, 0.64f, 0.56f, 0.08f, 0.05f);
        }

        static void Owl(RectTransform f)
        {
            var feathers = new Color(0.6f, 0.42f, 0.3f);
            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.04f, 0.6f, 0.07f);
            Oval(f, "Tuft L", feathers, 0.33f, 0.85f, 0.1f, 0.18f, 25f);
            Oval(f, "Tuft R", feathers, 0.67f, 0.85f, 0.1f, 0.18f, -25f);
            Oval(f, "Body", feathers, 0.5f, 0.45f, 0.6f, 0.78f);
            Oval(f, "Wing L", new Color(0.5f, 0.34f, 0.24f), 0.22f, 0.38f, 0.14f, 0.36f, -10f);
            Oval(f, "Wing R", new Color(0.5f, 0.34f, 0.24f), 0.78f, 0.38f, 0.14f, 0.36f, 10f);
            Oval(f, "Belly", new Color(0.93f, 0.85f, 0.7f), 0.5f, 0.32f, 0.34f, 0.36f);
            Oval(f, "Face L", Color.white, 0.38f, 0.64f, 0.24f, 0.24f);
            Oval(f, "Face R", Color.white, 0.62f, 0.64f, 0.24f, 0.24f);
            Eyes(f, 0.64f, 0.12f, 0.1f);
            Oval(f, "Beak", new Color(1f, 0.65f, 0.15f), 0.5f, 0.53f, 0.08f, 0.1f);
            Oval(f, "Foot L", new Color(1f, 0.65f, 0.15f), 0.4f, 0.07f, 0.12f, 0.06f);
            Oval(f, "Foot R", new Color(1f, 0.65f, 0.15f), 0.6f, 0.07f, 0.12f, 0.06f);
        }

        static void Frog(RectTransform f)
        {
            var green = new Color(0.4f, 0.78f, 0.35f);
            Oval(f, "Shadow", new Color(0f, 0f, 0f, 0.12f), 0.5f, 0.04f, 0.7f, 0.07f);
            Oval(f, "Leg L", new Color(0.32f, 0.66f, 0.28f), 0.24f, 0.13f, 0.26f, 0.16f, 15f);
            Oval(f, "Leg R", new Color(0.32f, 0.66f, 0.28f), 0.76f, 0.13f, 0.26f, 0.16f, -15f);
            Oval(f, "Body", green, 0.5f, 0.34f, 0.7f, 0.5f);
            Oval(f, "Belly", new Color(0.82f, 0.95f, 0.6f), 0.5f, 0.27f, 0.4f, 0.28f);
            Oval(f, "Eye bump L", green, 0.33f, 0.62f, 0.24f, 0.24f);
            Oval(f, "Eye bump R", green, 0.67f, 0.62f, 0.24f, 0.24f);
            Oval(f, "White L", Color.white, 0.33f, 0.63f, 0.17f, 0.17f);
            Oval(f, "White R", Color.white, 0.67f, 0.63f, 0.17f, 0.17f);
            Eyes(f, 0.63f, 0.17f, 0.08f);
            Oval(f, "Mouth", new Color(0.55f, 0.13f, 0.17f), 0.5f, 0.45f, 0.3f, 0.1f);
            Oval(f, "Mouth cover", green, 0.5f, 0.48f, 0.34f, 0.08f);
            Oval(f, "Cheek L", Blush, 0.27f, 0.45f, 0.09f, 0.06f);
            Oval(f, "Cheek R", Blush, 0.73f, 0.45f, 0.09f, 0.06f);
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
