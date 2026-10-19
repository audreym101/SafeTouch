using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>
    /// Everything SafeTouch remembers about a learner (FR8). Stored on the
    /// phone as JSON in PlayerPrefs. userId is a random device id, never a
    /// name, photo, location or voice recording (NFR1, Law N° 058/2021).
    /// </summary>
    [Serializable]
    public class Progress
    {
        public const string PrefsKey = "safetouch.progress.v1";

        public string userId;
        public string ageGroup;                 // "6-8" or "9-11"
        public string language = "en";          // "en" or "rw"
        public List<int> completedLevels = new List<int>();
        public List<int> quizScores = new List<int>();          // best score per level, index = level - 1
        public List<string> badgesEarned = new List<string>();  // badge ids
        public List<string> trustedAdults = new List<string>(); // adult ids chosen by the child
        public List<ScenarioResponse> scenarioResponses = new List<ScenarioResponse>();
        public float bestChaseSeconds;          // fastest "NO" in the Memory Quest chase; 0 = not played yet
        public bool voiceEnabled = true;        // spoken NO practice; a parent can switch it off
        public int strongNoCount;               // three-star spoken NOs; a count only, never audio or pitch values
        public string skinTone = SkinTones.DefaultId;   // how the child chose Keza to look (not the child's own data)
        public int bestGuardScore;              // best Bubble Guard score
        public int guardGamesPlayed;
        public bool cloudSyncConsent;           // set only by a parent in the parent section
        public string lastUpdated;

        public void Save()
        {
            lastUpdated = DateTime.UtcNow.ToString("o");
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(PrefsKey)) return;
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), this);
        }

        public void AddBadge(Badge badge)
        {
            if (badge == null || badgesEarned.Contains(badge.badgeId)) return;
            badgesEarned.Add(badge.badgeId);
            badge.Unlock();
        }

        public bool IsLevelComplete(int level)
        {
            return completedLevels.Contains(level);
        }

        public int GetTotalScore()
        {
            return quizScores.Sum();
        }

        public void SetScore(int level, int score)
        {
            while (quizScores.Count < level) quizScores.Add(0);
            quizScores[level - 1] = Mathf.Max(quizScores[level - 1], score);
        }
    }

    /// <summary>One answered scenario card, used for the pre/post evaluation.</summary>
    [Serializable]
    public class ScenarioResponse
    {
        public string scenarioId;
        public string selectedAnswer;
        public bool isCorrect;
        public string answeredAt;
    }
}
