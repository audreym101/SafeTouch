using UnityEngine;

namespace SafeTouch.UI
{
    /// <summary>
    /// SafeTouch style guide in code. The same tokens are documented in
    /// docs/designs/style-guide and used by the clickable prototype.
    /// </summary>
    public static class Theme
    {
        // Colour tokens
        public static readonly Color Ink = Hex("#1F2A44");       // text
        public static readonly Color Muted = Hex("#5F6F86");     // secondary text
        public static readonly Color Sky = Hex("#EAF4FB");       // screen background
        public static readonly Color Card = Hex("#FFFFFF");
        public static readonly Color Primary = Hex("#2474C6");   // main buttons, "Lake Kivu blue"
        public static readonly Color Sun = Hex("#F6B73C");       // stars, badges, highlights
        public static readonly Color Safe = Hex("#3BA776");      // green zone, "Safe"
        public static readonly Color Caution = Hex("#EE9B2F");   // yellow zone, "Uncomfortable"
        public static readonly Color Unsafe = Hex("#E4574C");    // red zone, "Unsafe" (warm coral, not alarm red)
        public static readonly Color Scrim = new Color(0.12f, 0.16f, 0.27f, 0.55f);
        public static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        // Type scale at the 1080 x 1920 reference resolution
        public const int Display = 84;
        public const int Title = 60;
        public const int Body = 44;
        public const int Label = 36;
        public const int Small = 30;

        // Layout
        public const float TouchTarget = 150f;   // about 56 dp on a 1080p phone: large for small fingers
        public const float Gutter = 56f;
        public const float Gap = 32f;

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
