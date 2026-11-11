using System;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>
    /// One scenario card (FR4, FR5). Content ships with the app in
    /// Resources/Content/scenarios.json and is reviewed by child-protection
    /// experts before evaluation (week 7).
    /// </summary>
    [Serializable]
    public class Scenario
    {
        public string scenarioId;
        public int level;               // 1 = the ages 6-8 deck, 2 = the ages 9-11 deck (they share no cards)
        public string title;
        public string question;
        public string optionA = "Safe";
        public string optionB = "Uncomfortable";
        public string optionC = "Unsafe";
        public string correctAnswer;    // "A", "B" or "C"
        public string feedback;
        public string deeper;           // extra "think deeper" explanation shown to ages 9-11
        public string audioClipName;    // looked up in Resources/Audio/Voice (optional)
        public bool night;              // the story happens after dark: the 3D world turns to night while it plays
        public string trick;            // ages 9-11: the warning sign to name ("secret", "gift", "stranger", "pressure" or "none")

        [NonSerialized] public AudioClip audioClip;

        public string GetQuestion()
        {
            return question;
        }

        public bool CheckAnswer(string ans)
        {
            return string.Equals(ans, correctAnswer, StringComparison.OrdinalIgnoreCase);
        }

        public string GetFeedback()
        {
            return feedback;
        }

        /// <summary>Feedback for the child's age group: ages 9-11 also get the "think deeper" note.</summary>
        public string GetFeedback(bool older, string deeperLabel)
        {
            if (!older || string.IsNullOrEmpty(deeper)) return feedback;
            return feedback + "\n\n" + deeperLabel + " " + deeper;
        }

        public string GetOptionText(string letter)
        {
            switch (letter)
            {
                case "A": return optionA;
                case "B": return optionB;
                default: return optionC;
            }
        }
    }
}
