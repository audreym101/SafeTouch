using System;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>
    /// A kind of adult a child can go to for help (FR7). The child picks a
    /// role such as "Teacher", never a real person's name, so no personal
    /// data is stored (NFR1).
    /// </summary>
    [Serializable]
    public class TrustedAdult
    {
        public string adultId;
        public string name;         // role label shown to the child, e.g. "Teacher"
        public string iconName;     // sprite in Resources/Icons (optional)
        public string category;     // "home", "school" or "area"
        public string colorHex = "#2474C6";

        [NonSerialized] public Sprite icon;
        [NonSerialized] public bool isSelected;

        public event Action<TrustedAdult> Selected;
        public event Action<TrustedAdult> PlacedInAR;

        public void Select()
        {
            isSelected = !isSelected;
            if (Selected != null) Selected(this);
        }

        public void PlaceInAR()
        {
            if (PlacedInAR != null) PlacedInAR(this);
        }

        public Color GetColor()
        {
            Color c;
            return ColorUtility.TryParseHtmlString(colorHex, out c) ? c : Color.white;
        }
    }
}
