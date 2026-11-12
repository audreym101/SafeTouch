using System;
using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.UI
{
    /// <summary>
    /// Simple white icons drawn in code for the lesson's round buttons, so they need no image files: read aloud,
    /// sun, moon, move and turn. Children who cannot read yet know them at a glance. Each shape is a test
    /// "is this point inside?" on a square from -1 to 1 (y up), smoothed at the edges.
    /// </summary>
    public static class Icons
    {
        static readonly Dictionary<string, Sprite> made = new Dictionary<string, Sprite>();

        public static Sprite Speaker { get { return Get("speaker", SpeakerShape); } }
        public static Sprite Sun { get { return Get("sun", SunShape); } }
        public static Sprite Moon { get { return Get("moon", MoonShape); } }
        public static Sprite Move { get { return Get("move", MoveShape); } }
        public static Sprite Target { get { return Get("target", TargetShape); } }
        public static Sprite TurnLeft { get { return Get("turn left", (x, y) => TurnShape(-x, y)); } }
        public static Sprite TurnRight { get { return Get("turn right", TurnShape); } }
        public static Sprite Mic { get { return Get("mic", MicShape); } }
        public static Sprite Check { get { return Get("check", CheckShape); } }
        public static Sprite Play { get { return Get("play", PlayShape); } }
        public static Sprite ZoomIn { get { return Get("zoom in", (x, y) => Magnifier(x, y, true)); } }
        public static Sprite ZoomOut { get { return Get("zoom out", (x, y) => Magnifier(x, y, false)); } }

        static Sprite Get(string name, Func<float, float, bool> inside)
        {
            Sprite sprite;
            if (made.TryGetValue(name, out sprite) && sprite != null) return sprite;
            const int size = 128, samples = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Icon " + name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    float u = (x + (sx + 0.5f) / samples) / size * 2f - 1f;
                    float v = (y + (sy + 0.5f) / samples) / size * 2f - 1f;
                    if (inside(u, v)) hits++;
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / (samples * samples)));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            made[name] = sprite;
            return sprite;
        }

        static bool SpeakerShape(float x, float y)
        {
            if (x > -0.78f && x < -0.42f && Mathf.Abs(y) < 0.24f) return true;                 // the box
            if (x >= -0.42f && x <= 0.02f && Mathf.Abs(y) < Mathf.Lerp(0.24f, 0.6f, (x + 0.42f) / 0.44f)) return true;   // the cone
            float dx = x - 0.02f, r = Mathf.Sqrt(dx * dx + y * y);
            return dx > 0.05f && Mathf.Abs(y) < dx * 1.2f && (Mathf.Abs(r - 0.38f) < 0.07f || Mathf.Abs(r - 0.66f) < 0.07f);   // sound waves
        }

        static bool SunShape(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r < 0.36f) return true;
            if (r < 0.52f || r > 0.88f) return false;
            float sector = Mathf.PI * 2f / 8f;
            float k = Mathf.Repeat(Mathf.Atan2(y, x) / sector + 0.5f, 1f) - 0.5f;   // 0 on a ray
            return Mathf.Abs(k) * sector * r < 0.075f;
        }

        static bool MoonShape(float x, float y)
        {
            bool disc = x * x + y * y < 0.7f * 0.7f;
            float bx = x - 0.34f, by = y - 0.24f;
            return disc && bx * bx + by * by > 0.58f * 0.58f;
        }

        static bool MoveShape(float x, float y)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            if ((ax < 0.09f && ay < 0.55f) || (ay < 0.09f && ax < 0.55f)) return true;   // the cross
            // An arrow head at each end.
            if (ay > ax) return ay > 0.45f && ay < 0.9f && ax < (0.9f - ay) * 0.75f;
            return ax > 0.45f && ax < 0.9f && ay < (0.9f - ax) * 0.75f;
        }

        static bool TargetShape(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r < 0.2f || Mathf.Abs(r - 0.58f) < 0.09f) return true;
            return (Mathf.Abs(x) < 0.07f || Mathf.Abs(y) < 0.07f) && r > 0.7f && r < 0.95f;
        }

        /// <summary>A microphone: a rounded head, a cradle under it and a stand.</summary>
        static bool MicShape(float x, float y)
        {
            float hx = Mathf.Abs(x), cy = Mathf.Clamp(y, 0.05f, 0.5f);
            if (hx * hx + (y - cy) * (y - cy) < 0.2f * 0.2f) return true;                     // the head (a capsule)
            float r = Mathf.Sqrt(x * x + (y - 0.15f) * (y - 0.15f));
            if (y < 0.15f && Mathf.Abs(r - 0.36f) < 0.06f) return true;                       // the cradle
            if (hx < 0.06f && y < -0.2f && y > -0.6f) return true;                            // the stand
            return hx < 0.28f && y < -0.6f && y > -0.72f;                                     // the foot
        }

        /// <summary>A tick: heard!</summary>
        /// <summary>A triangle pointing right: play the recording.</summary>
        static bool PlayShape(float x, float y)
        {
            const float left = -0.42f, right = 0.62f, half = 0.6f;
            return x > left && x < right && Mathf.Abs(y) < half * (right - x) / (right - left);
        }

        static bool CheckShape(float x, float y)
        {
            return Segment(x, y, -0.55f, 0f, -0.15f, -0.42f, 0.13f) || Segment(x, y, -0.15f, -0.42f, 0.6f, 0.45f, 0.13f);
        }

        /// <summary>True within half of width of the line from (ax, ay) to (bx, by).</summary>
        static bool Segment(float x, float y, float ax, float ay, float bx, float by, float width)
        {
            var a = new Vector2(ax, ay);
            var ab = new Vector2(bx, by) - a;
            float t = Mathf.Clamp01(Vector2.Dot(new Vector2(x, y) - a, ab) / ab.sqrMagnitude);
            return (new Vector2(x, y) - (a + ab * t)).magnitude < width * 0.5f;
        }

        /// <summary>A magnifying glass with a plus (closer) or a minus (further) in it.</summary>
        static bool Magnifier(float x, float y, bool plus)
        {
            float cx = x + 0.14f, cy = y - 0.14f, r = Mathf.Sqrt(cx * cx + cy * cy);
            if (Mathf.Abs(r - 0.5f) < 0.09f) return true;   // the glass
            float along = (x - y) * 0.7071f, across = (x + y) * 0.7071f;
            if (along > 0.62f && along < 1.15f && Mathf.Abs(across) < 0.11f) return true;   // the handle, bottom right
            if (Mathf.Abs(cy) < 0.065f && Mathf.Abs(cx) < 0.26f) return true;              // minus
            return plus && Mathf.Abs(cx) < 0.065f && Mathf.Abs(cy) < 0.26f;                 // plus
        }

        /// <summary>A round arrow turning to the right (clockwise); TurnLeft is its mirror image.</summary>
        static bool TurnShape(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            // Most of a circle, open at the top right where the arrow head sits.
            if (Mathf.Abs(r - 0.56f) < 0.1f && (a < 20f || a > 95f)) return true;
            // The head points clockwise from the end of the arc, at 20 degrees.
            var p = new Vector2(Mathf.Cos(20f * Mathf.Deg2Rad), Mathf.Sin(20f * Mathf.Deg2Rad)) * 0.56f;
            var radial = p.normalized;
            var along = new Vector2(radial.y, -radial.x);   // clockwise
            var q = new Vector2(x, y) - p;
            float forward = Vector2.Dot(q, along), side = Vector2.Dot(q, radial);
            return forward > -0.05f && forward < 0.34f && Mathf.Abs(side) < (0.34f - forward) * 0.9f;
        }
    }
}
