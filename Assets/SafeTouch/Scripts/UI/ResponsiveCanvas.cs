using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// Keeps every screen usable on any phone or tablet in portrait. The
    /// layouts are designed on a 1080 x 1920 canvas and need at least that
    /// much room, so the canvas never gets smaller than it:
    ///   - phones (16:9, and taller ones such as the Samsung A70 at 20:9)
    ///     keep the full 1080 width; taller phones simply get extra height;
    ///   - tablets and other wide screens (4:3, 3:5...) keep the full 1920
    ///     height; the extra width becomes side margins, because SafeArea
    ///     caps the content at MaxContentWidth.
    /// Text and buttons therefore stay the same physical share of the screen
    /// on every device, and nothing is squeezed or pushed off the bottom.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvas : MonoBehaviour
    {
        public static readonly Vector2 Reference = new Vector2(1080f, 1920f);

        /// <summary>Content wider than this (canvas units) is centred with side margins, as on tablets.</summary>
        public const float MaxContentWidth = 1200f;

        CanvasScaler scaler;
        Vector2 applied;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply(Screen.width, Screen.height);
        }

        void Update()
        {
            // Screen size can change: split-screen, foldables, rotation on tablets.
            if (Screen.width != applied.x || Screen.height != applied.y) Apply(Screen.width, Screen.height);
        }

        /// <summary>Sets the scaling for a screen of the given size in pixels.</summary>
        public void Apply(float width, float height)
        {
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            if (width <= 0f || height <= 0f) return;
            applied = new Vector2(width, height);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Narrower than 9:16 (most phones): fit the width. Wider (tablets): fit the height.
            scaler.matchWidthOrHeight = width / height <= Reference.x / Reference.y ? 0f : 1f;
        }
    }
}
