using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// The SafeTouch app icon, drawn in code: a white shield on a sky-blue square, holding a raised yellow hand
    /// ("stop", "my body") with a red heart in its palm. No words, so it reads at any size and in any language.
    ///
    /// Makes three pictures in Assets/SafeTouch/Icons (the whole icon, and the background and foreground layers
    /// Android's adaptive icons need) and sets them as the app icon. This runs by itself when the project opens
    /// if the icon is not set yet, so the phone never shows Unity's logo; SafeTouch > Make App Icon redraws it.
    /// </summary>
    [InitializeOnLoad]
    public static class SafeTouchAppIcon
    {
        const string Folder = "Assets/SafeTouch/Icons";
        const string IconPath = Folder + "/AppIcon.png";
        const string BackgroundPath = Folder + "/AppIconBackground.png";
        const string ForegroundPath = Folder + "/AppIconForeground.png";
        const int Size = 1024;

        static SafeTouchAppIcon()
        {
            EditorApplication.delayCall += () => { if (!Application.isBatchMode && !IconIsSet()) Make(); };
        }

        [MenuItem("SafeTouch/Make App Icon", priority = 20)]
        public static void Make()
        {
            Directory.CreateDirectory(Folder);
            Save(IconPath, Draw(true, 1f));
            Save(BackgroundPath, Draw(true, 0f));
            Save(ForegroundPath, Draw(false, 0.68f));   // Android crops adaptive icons: keep the logo in the safe middle
            AssetDatabase.Refresh();
            foreach (var p in new[] { IconPath, BackgroundPath, ForegroundPath }) Import(p);
            Assign();
            Debug.Log("[SafeTouch] App icon made and set: " + IconPath);
        }

        static bool IconIsSet()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return false;
            var current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            return current != null && current.Contains(icon);
        }

        static void Save(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void Import(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// <summary>The default icon (used by every platform), and Android's adaptive, round and legacy icons.</summary>
        static void Assign()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            var android = NamedBuildTarget.Android;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(android))
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var platformIcon in icons)
                {
                    if (platformIcon.layerCount >= 2)
                    {
                        platformIcon.SetTexture(background, 0);
                        platformIcon.SetTexture(foreground, 1);
                    }
                    else platformIcon.SetTexture(icon, 0);
                }
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            AssetDatabase.SaveAssets();
        }

        // ---- Drawing: shapes as distances, four samples a pixel for smooth edges ----

        static readonly Color Top = new Color(0.27f, 0.72f, 0.96f), Bottom = new Color(0.12f, 0.42f, 0.78f);
        static readonly Color HandFill = new Color(0.97f, 0.73f, 0.24f), HandLine = new Color(0.86f, 0.52f, 0.1f);
        static readonly Color Heart = new Color(0.89f, 0.3f, 0.32f);

        /// <summary>withBackground: the blue square; logo: how big the shield is (0 for the background alone).</summary>
        static Texture2D Draw(bool withBackground, float logo)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color[Size * Size];
            const int n = 4;   // 4 x 4 samples a pixel
            for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                var sum = Color.clear;
                for (int sy = 0; sy < n; sy++)
                for (int sx = 0; sx < n; sx++)
                {
                    // x right, y down, -0.5 .. 0.5 across the icon.
                    float x = (px + (sx + 0.5f) / n) / Size - 0.5f;
                    float y = 0.5f - (py + (sy + 0.5f) / n) / Size;
                    sum += Sample(x, y, withBackground, logo);
                }
                pixels[py * Size + px] = sum / (n * n);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Color Sample(float x, float y, bool withBackground, float logo)
        {
            var c = Color.clear;
            if (withBackground)
            {
                var sky = Color.Lerp(Top, Bottom, y + 0.5f);
                // A soft glow at the top left, like sunlight.
                float glow = Mathf.Clamp01(1f - Mathf.Sqrt((x + 0.22f) * (x + 0.22f) + (y + 0.25f) * (y + 0.25f)) / 0.55f);
                c = Color.Lerp(sky, Color.white, glow * 0.22f);
            }
            if (logo <= 0f) return c;
            float lx = x / logo, ly = y / logo;   // logo space

            c = Over(c, new Color(0f, 0f, 0f, 0.18f), Shield(lx - 0.012f, ly - 0.02f));   // a soft shadow
            c = Over(c, Color.white, Shield(lx, ly));
            float hand = Hand(lx, ly);
            c = Over(c, HandLine, hand - 0.014f);
            c = Over(c, HandFill, hand);
            c = Over(c, Heart, HeartShape(lx, ly - 0.085f, 0.075f));
            return c;
        }

        /// <summary>Paints colour over c where the distance d is inside the shape (d &lt;= 0), with a soft edge.</summary>
        static Color Over(Color c, Color paint, float d)
        {
            float a = Mathf.Clamp01(0.5f - d * Size * 0.6f) * paint.a;
            var result = Color.Lerp(c, new Color(paint.r, paint.g, paint.b, 1f), a);
            result.a = c.a + a * (1f - c.a);
            if (result.a > 0f && c.a < 1f)   // keep colour right over transparent pixels (the foreground layer)
            {
                result.r = (c.r * c.a * (1f - a) + paint.r * a) / result.a;
                result.g = (c.g * c.a * (1f - a) + paint.g * a) / result.a;
                result.b = (c.b * c.a * (1f - a) + paint.b * a) / result.a;
            }
            return result;
        }

        /// <summary>A shield: rounded at the top, coming to a soft point at the bottom. Negative inside.</summary>
        static float Shield(float x, float y)
        {
            const float halfWidth = 0.3f, top = -0.33f, shoulder = 0.04f, tip = 0.38f;
            float ax = Mathf.Abs(x);
            // The upper part: a box with rounded top corners.
            // It reaches a little past the shoulder, under the lower part, so its rounded bottom corners never show.
            float box = RoundRect(x, y, 0f, (top + shoulder + 0.1f) * 0.5f, halfWidth, (shoulder + 0.1f - top) * 0.5f, 0.09f);
            // The lower part: sides curving in to the tip.
            float lower = 1f;
            if (y >= shoulder - 0.01f)
            {
                float t = Mathf.Clamp01((y - shoulder) / (tip - shoulder));
                float width = halfWidth * (1f - t * t * t * 0.55f - t * t * 0.45f);
                lower = Mathf.Max(ax - width, y - tip, shoulder - 0.01f - y);
            }
            return Mathf.Min(box, lower);
        }

        static float RoundRect(float x, float y, float cx, float cy, float hw, float hh, float r)
        {
            float dx = Mathf.Abs(x - cx) - (hw - r), dy = Mathf.Abs(y - cy) - (hh - r);
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            var p = new Vector2(x - ax, y - ay);
            var ab = new Vector2(bx - ax, by - ay);
            float t = Mathf.Clamp01(Vector2.Dot(p, ab) / ab.sqrMagnitude);
            return (p - ab * t).magnitude - r;
        }

        /// <summary>A raised hand: palm, four fingers (the middle one tallest) and a thumb.</summary>
        static float Hand(float x, float y)
        {
            float d = RoundRect(x, y, 0f, 0.085f, 0.125f, 0.1f, 0.06f);
            d = Mathf.Min(d, Capsule(x, y, -0.088f, 0.02f, -0.098f, -0.15f, 0.03f));
            d = Mathf.Min(d, Capsule(x, y, -0.03f, 0.0f, -0.032f, -0.205f, 0.031f));
            d = Mathf.Min(d, Capsule(x, y, 0.03f, 0.0f, 0.032f, -0.195f, 0.031f));
            d = Mathf.Min(d, Capsule(x, y, 0.088f, 0.02f, 0.098f, -0.135f, 0.029f));
            d = Mathf.Min(d, Capsule(x, y, 0.1f, 0.13f, 0.19f, 0.03f, 0.033f));   // thumb
            return d;
        }

        /// <summary>A heart: inside or outside only; the 4 x 4 samples a pixel smooth its edge.</summary>
        static float HeartShape(float x, float y, float size)
        {
            float hx = x / size, hy = -y / size + 0.25f;   // the classic heart curve, point down
            float v = Mathf.Pow(hx * hx + hy * hy - 1f, 3f) - hx * hx * hy * hy * hy;
            return v <= 0f ? -1f : 1f;
        }
    }
}
