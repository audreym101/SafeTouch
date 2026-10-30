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
        public List<int> maxScores = new List<int>();           // the most each level can score, index = level - 1
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

        // Two lesson paths: "6-8" (Explorer) and "9-11" (Guardian). The fields above (ageGroup, finished
        // lessons, scores, badges, chase and Bubble Guard bests, strong NOs) always belong to the path being
        // played, which is ageGroup. The other path's copy waits in otherPaths, so each keeps its own stars and
        // badges and the rest of the app only ever sees the path in play. The 9-11 path opens when the child
        // started there, or has finished every lesson of the 6-8 path.
        public List<PathData> otherPaths = new List<PathData>();
        public bool guardianUnlocked;

        public const string PathYoung = "6-8";
        public const string PathOlder = "9-11";
        public const int PathLessons = 5;   // the four lessons and the Memory Quest

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

        /// <summary>Plays the other path: this path's results are put aside and the other's brought back (empty the first time).</summary>
        public void SwitchPath(string age)
        {
            if (string.IsNullOrEmpty(age) || age == ageGroup) return;
            if (ageGroup == PathOlder) guardianUnlocked = true;   // it was open (a save from before the paths existed doesn't say so)
            var next = otherPaths.Find(p => p.ageGroup == age);
            if (next != null) otherPaths.Remove(next);
            if (!string.IsNullOrEmpty(ageGroup)) otherPaths.Add(PathData.From(this));
            if (next == null) next = new PathData { ageGroup = age };
            next.ApplyTo(this);
        }

        /// <summary>Every lesson of the path is finished (the path in play, or the one put aside).</summary>
        public bool IsPathComplete(string age)
        {
            if (age == ageGroup) return completedLevels.FindAll(l => l >= 1 && l <= PathLessons).Count >= PathLessons;
            var other = otherPaths.Find(p => p.ageGroup == age);
            return other != null && other.completedLevels.FindAll(l => l >= 1 && l <= PathLessons).Count >= PathLessons;
        }

        /// <summary>The 6-8 path is always open; the 9-11 path opens when the child started there or finished the 6-8 path.</summary>
        public bool IsPathOpen(string age)
        {
            return age != PathOlder || age == ageGroup || guardianUnlocked || IsPathComplete(PathYoung);
        }

        /// <summary>Lessons finished on the path in play.</summary>
        public int PathDone()
        {
            return completedLevels.FindAll(l => l >= 1 && l <= PathLessons).Count;
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

    /// <summary>One lesson path's results while the other path is being played.</summary>
    [Serializable]
    public class PathData
    {
        public string ageGroup;
        public List<int> completedLevels = new List<int>();
        public List<int> quizScores = new List<int>();
        public List<int> maxScores = new List<int>();
        public List<string> badgesEarned = new List<string>();
        public float bestChaseSeconds;
        public int strongNoCount;
        public int bestGuardScore;
        public int guardGamesPlayed;

        public static PathData From(Progress p)
        {
            return new PathData
            {
                ageGroup = p.ageGroup,
                completedLevels = new List<int>(p.completedLevels),
                quizScores = new List<int>(p.quizScores),
                maxScores = new List<int>(p.maxScores),
                badgesEarned = new List<string>(p.badgesEarned),
                bestChaseSeconds = p.bestChaseSeconds,
                strongNoCount = p.strongNoCount,
                bestGuardScore = p.bestGuardScore,
                guardGamesPlayed = p.guardGamesPlayed,
            };
        }

        public void ApplyTo(Progress p)
        {
            p.ageGroup = ageGroup;
            p.completedLevels = new List<int>(completedLevels);
            p.quizScores = new List<int>(quizScores);
            p.maxScores = new List<int>(maxScores);
            p.badgesEarned = new List<string>(badgesEarned);
            p.bestChaseSeconds = bestChaseSeconds;
            p.strongNoCount = strongNoCount;
            p.bestGuardScore = bestGuardScore;
            p.guardGamesPlayed = guardGamesPlayed;
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
