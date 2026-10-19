using System;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>One skin tone the child can give Keza.</summary>
    [Serializable]
    public class SkinTone
    {
        public string id;
        public string nameKey;   // localisation key for the swatch name
        public string english;   // name used until the key is translated
        public string hex;

        public Color Color
        {
            get
            {
                Color c;
                return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.gray;
            }
        }
    }

    /// <summary>
    /// The skin tone palette. Children choose how Keza looks so she can look
    /// like them; visitors and helpers get a spread of tones so every lesson
    /// shows a mix of people. Names are foods and woods, never ethnic labels.
    /// </summary>
    public static class SkinTones
    {
        /// <summary>Keza's original tone, kept as the default so nothing changes until a child picks.</summary>
        public const string DefaultId = "cocoa";

        public static readonly SkinTone[] All =
        {
            new SkinTone { id = "sand",    nameKey = "skin.sand",    english = "Sand",    hex = "#F2D1B3" },
            new SkinTone { id = "honey",   nameKey = "skin.honey",   english = "Honey",   hex = "#E0AC69" },
            new SkinTone { id = "caramel", nameKey = "skin.caramel", english = "Caramel", hex = "#C68642" },
            new SkinTone { id = "cocoa",   nameKey = "skin.cocoa",   english = "Cocoa",   hex = "#8D5524" },
            new SkinTone { id = "coffee",  nameKey = "skin.coffee",  english = "Coffee",  hex = "#6A3F23" },
            new SkinTone { id = "ebony",   nameKey = "skin.ebony",   english = "Ebony",   hex = "#432818" },
        };

        public static SkinTone Get(string id)
        {
            foreach (var tone in All) if (tone.id == id) return tone;
            return Get(DefaultId);
        }
    }
}
