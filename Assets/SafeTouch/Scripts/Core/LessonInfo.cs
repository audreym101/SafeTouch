using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Core
{
    /// <summary>The four lessons and the Memory Quest end game, in the order a child unlocks them.</summary>
    public class LessonInfo
    {
        public int level;
        public string titleKey;
        public string summaryKey;
        public Color color;

        public static readonly LessonInfo[] All =
        {
            new LessonInfo { level = 1, titleKey = "lesson1.title", summaryKey = "lesson1.summary", color = Theme.Primary },
            new LessonInfo { level = 2, titleKey = "lesson2.title", summaryKey = "lesson2.summary", color = Theme.Caution },
            new LessonInfo { level = 3, titleKey = "lesson3.title", summaryKey = "lesson3.summary", color = Theme.Unsafe },
            new LessonInfo { level = 4, titleKey = "lesson4.title", summaryKey = "lesson4.summary", color = Theme.Safe },
            new LessonInfo { level = 5, titleKey = "lesson5.title", summaryKey = "lesson5.summary", color = Theme.Sun },
        };

        /// <summary>Bubble Guard, the replayable voice game. Shown below the lessons, not part of the lesson order.</summary>
        public static readonly LessonInfo Game =
            new LessonInfo { level = 6, titleKey = "game.title", summaryKey = "game.summary", color = Theme.Hex("#7A5CC7") };

        public static LessonInfo For(int level)
        {
            if (level == Game.level) return Game;
            return All[Mathf.Clamp(level - 1, 0, All.Length - 1)];
        }
    }
}
