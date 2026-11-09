using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// How big the letters are, chosen in Settings: normal, big (the default) or bigger. Kept on the phone.
    /// </summary>
    public static class TextSize
    {
        const string Key = "safetouch.textsize";

        /// <summary>The sizes, as times the size each screen was designed with.</summary>
        public static readonly float[] Scales = { 1f, 1.15f, 1.3f };

        /// <summary>"Big": a little larger than the screens were designed, easier for young readers.</summary>
        public const int DefaultStep = 1;

        public static int Step
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(Key, DefaultStep), 0, Scales.Length - 1); }
            set { PlayerPrefs.SetInt(Key, Mathf.Clamp(value, 0, Scales.Length - 1)); PlayerPrefs.Save(); }
        }

        public static float Scale { get { return Scales[Step]; } }

        /// <summary>For words in the 3D world (name tags, speech bubbles, floating labels): 1 at the default letter size.</summary>
        public static float WorldScale { get { return Scale / Scales[DefaultStep]; } }
    }

    /// <summary>
    /// Applies TextSize to every text on the canvas, including text a screen makes later (lists, messages).
    /// It remembers each text's designed size, so changing the setting back and forth never makes letters creep.
    /// </summary>
    public class TextSizer : MonoBehaviour
    {
        // For each text: the size the screen gave it, and the size applied here.
        readonly Dictionary<Text, Vector2Int> sizes = new Dictionary<Text, Vector2Int>();
        float next, applied = -1f;

        /// <summary>Applies the size to every text now (the screenshot tool calls this; the app does it every quarter second).</summary>
        public void ApplyNow()
        {
            next = 0f;
            applied = -1f;
            LateUpdate();
        }

        void LateUpdate()
        {
            float scale = TextSize.Scale;
            if (Time.unscaledTime < next && Mathf.Approximately(applied, scale)) return;
            next = Time.unscaledTime + 0.25f;
            applied = scale;
            rows.Clear();
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                Apply(t, scale);
                GrowButton(t, scale);
            }
            foreach (var row in rows) Set(row.Key, row.Value);
            if (sizes.Count > 3000) Forget();
        }

        // Buttons and button rows as the screens made them (their height), and what each row needs this time.
        readonly Dictionary<LayoutElement, float> designed = new Dictionary<LayoutElement, float>();
        readonly Dictionary<LayoutElement, float> rows = new Dictionary<LayoutElement, float>();
        static readonly TextGenerator Measure = new TextGenerator();

        /// <summary>
        /// Buttons grow with their words: when the words need more room than the button has (two lines at a
        /// big letter size), the button grows to fit them, up to 1.6 times its designed height. A row of buttons
        /// grows with its tallest button. On a screen too full for that, the layout gives back the room and the words shrink.
        /// </summary>
        void GrowButton(Text t, float scale)
        {
            var parent = t.transform.parent;
            // Only words laid straight onto a button: cards with their own layout size themselves.
            if (parent == null || parent.GetComponent<Button>() == null || parent.GetComponent<LayoutGroup>() != null || !t.isActiveAndEnabled) return;
            var button = parent.GetComponent<LayoutElement>();
            if (button == null || button.preferredHeight <= 0f) return;

            // The words at full size, on as many lines as they need in the button's width, plus padding.
            var box = t.rectTransform.rect;
            float words = 0f;
            if (box.width > 1f && !string.IsNullOrEmpty(t.text))
            {
                var settings = t.GetGenerationSettings(new Vector2(box.width, 0f));
                settings.resizeTextForBestFit = false;
                settings.fontSize = t.fontSize;
                words = Measure.GetPreferredHeight(t.text, settings) / t.pixelsPerUnit + 34f;
            }
            float own = Designed(button);
            // Only as tall as the words need: growing for every letter size would squeeze the rest of a full screen.
            float wanted = Mathf.Clamp(words, own, own * 1.6f);
            Set(button, wanted);

            // A row of buttons (or the button with its neighbours) grows with its tallest one.
            var row = parent.parent != null ? parent.parent.GetComponent<LayoutElement>() : null;
            if (row == null || row.preferredHeight <= 0f || parent.parent.GetComponent<HorizontalLayoutGroup>() == null) return;
            float rowOwn = Designed(row);
            float rowWanted = Mathf.Clamp(wanted, rowOwn, rowOwn * 1.6f);
            float before;
            if (!rows.TryGetValue(row, out before) || rowWanted > before) rows[row] = rowWanted;
        }

        float Designed(LayoutElement le)
        {
            float h;
            if (!designed.TryGetValue(le, out h)) designed[le] = h = le.preferredHeight;
            return h;
        }

        static void Set(LayoutElement le, float height)
        {
            if (le != null && Mathf.Abs(le.preferredHeight - height) > 0.5f) le.preferredHeight = height;
        }

        void Apply(Text t, float scale)
        {
            Vector2Int s;
            // A new text, or one the screen has resized itself since: its size now is the designed size.
            bool known = sizes.TryGetValue(t, out s);
            if (!known || t.fontSize != s.y) s.x = t.fontSize;
            s.y = Mathf.RoundToInt(s.x * scale);
            if (t.fontSize != s.y) t.fontSize = s.y;
            sizes[t] = s;
            // Words always fit their box, whatever the letter size and the screen: on a button, and on a
            // screen too full for big letters, they shrink just enough instead of running into the next thing.
            if (!known && t.GetComponentInParent<InputField>() == null) UIFactory.FitInside(t);
            if (t.resizeTextForBestFit)
            {
                t.resizeTextMaxSize = s.y;
                t.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(s.y * 0.5f));
            }
        }


        /// <summary>Drops texts that no longer exist (lists that were rebuilt).</summary>
        void Forget()
        {
            var gone = new List<Text>();
            foreach (var t in sizes.Keys) if (t == null) gone.Add(t);
            foreach (var t in gone) sizes.Remove(t);
            var old = new List<LayoutElement>();
            foreach (var le in designed.Keys) if (le == null) old.Add(le);
            foreach (var le in old) designed.Remove(le);
        }
    }
}
