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

        /// <summary>The guide's default name, used in all the written text.</summary>
        public const string DefaultName = "Keza";

        /// <summary>The guide's name: the first name or nickname the child typed, or "Keza".</summary>
        public static string CharacterName { get; private set; } = DefaultName;

        /// <summary>Gives the guide the child's name; every text then says it instead of "Keza".</summary>
        public static void SetCharacterName(string name)
        {
            name = CleanName(name);
            CharacterName = string.IsNullOrEmpty(name) ? DefaultName : name;
            if (Changed != null) Changed();
        }

        /// <summary>
        /// Keeps a typed name short and plain: letters (any language), spaces,
        /// hyphens and apostrophes, at most 20 characters. Nothing else can end
        /// up in a label, a speech bubble or the content files.
        /// </summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in name.Trim())
            {
                if (char.IsLetter(c) || c == '-' || c == '\'' || (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] != ' ')) sb.Append(c);
                if (sb.Length >= 20) break;
            }
            return sb.ToString().Trim();
        }

        /// <summary>Puts the guide's name into a text written with "Keza".</summary>
        public static string Named(string text)
        {
            if (string.IsNullOrEmpty(text) || CharacterName == DefaultName) return text;
            return text.Replace(DefaultName, CharacterName);
        }

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
            if (Current.TryGetValue(key, out value) && !string.IsNullOrEmpty(value)) return Named(value);
            if (English.TryGetValue(key, out value)) return Named(value);
            return key;
        }

        /// <summary>Translate a key, using the given English text when the key has no entry yet.</summary>
        public static string T(string key, string english)
        {
            var value = T(key);
            return value == key ? Named(english) : value;
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
