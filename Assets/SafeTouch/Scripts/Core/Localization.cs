using System;
using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.Core
{
    /// <summary>
    /// English and Kinyarwanda interface text (FR10). Strings live in
    /// Resources/Content/strings_en.json and strings_rw.json; any key missing
    /// in Kinyarwanda falls back to English until the translation is reviewed.
    /// </summary>
    public static class Localization
    {
        [Serializable] class Entry { public string key; public string value; }
        [Serializable] class Table { public List<Entry> items; }

        static readonly Dictionary<string, string> English = new Dictionary<string, string>();
        static readonly Dictionary<string, string> Current = new Dictionary<string, string>();

        public static string Language { get; private set; }
        public static event Action Changed;

        public static void SetLanguage(string language)
        {
            if (English.Count == 0) LoadInto(English, "en");
            Current.Clear();
            if (language != "en") LoadInto(Current, language);
            Language = language;
            if (Changed != null) Changed();
        }

        /// <summary>Translate a key. Returns English, then the key itself, if missing.</summary>
        public static string T(string key)
        {
            if (English.Count == 0) SetLanguage("en");
            string value;
            if (Current.TryGetValue(key, out value) && !string.IsNullOrEmpty(value)) return value;
            if (English.TryGetValue(key, out value)) return value;
            return key;
        }

        /// <summary>Translate a key, using the given English text when the key has no entry yet.</summary>
        public static string T(string key, string english)
        {
            var value = T(key);
            return value == key ? english : value;
        }

        static void LoadInto(Dictionary<string, string> target, string language)
        {
            var asset = Resources.Load<TextAsset>("Content/strings_" + language);
            if (asset == null) return;
            var table = JsonUtility.FromJson<Table>(asset.text);
            if (table == null || table.items == null) return;
            foreach (var e in table.items) target[e.key] = e.value;
        }
    }
}
