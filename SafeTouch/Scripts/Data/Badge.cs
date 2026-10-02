using System;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>A reward the child earns for finishing lessons.</summary>
    [Serializable]
    public class Badge
    {
        public string badgeId;
        public string title;
        public string description;
        public string iconName;
        public string requirement;  // e.g. "complete:1", "adults:2", "fast:3" (chase time in seconds), "all"

        [NonSerialized] public Sprite icon;
        [NonSerialized] public bool unlocked;

        public void Unlock()
        {
            unlocked = true;
        }

        /// <summary>Short text for the badge shelf and the feedback toast.</summary>
        public string Display()
        {
            return unlocked ? title + " - " + description : title + " (locked)";
        }
    }
}
