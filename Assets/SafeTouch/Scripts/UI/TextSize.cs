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
            foreach (var t in GetComponentsInChildren<Text>(true)) Apply(t, scale);
            if (sizes.Count > 3000) Forget();
        }

        void Apply(Text t, float scale)
        {
            Vector2Int s;
            // A new text, or one the screen has resized itself since: its size now is the designed size.
            if (!sizes.TryGetValue(t, out s) || t.fontSize != s.y) s.x = t.fontSize;
            s.y = Mathf.RoundToInt(s.x * scale);
            if (t.fontSize != s.y) t.fontSize = s.y;
            sizes[t] = s;
        }

        /// <summary>Drops texts that no longer exist (lists that were rebuilt).</summary>
        void Forget()
        {
            var gone = new List<Text>();
            foreach (var t in sizes.Keys) if (t == null) gone.Add(t);
            foreach (var t in gone) sizes.Remove(t);
        }
    }
}
