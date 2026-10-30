using System;
using System.Collections.Generic;
using SafeTouch.Data;
using SafeTouch.Voice;
using UnityEngine;

namespace SafeTouch.Managers
{
    /// <summary>
    /// Owns the learner's Progress and the badge rules (FR8). All data stays
    /// on the phone; CloudSyncService may send an anonymous copy only when a
    /// parent has switched sync on.
    /// </summary>
    public class ProgressManager : MonoBehaviour
    {
        public const int LessonCount = 5;   // four lessons + the Memory Quest end game

        /// <summary>Bubble Guard: a replayable game, opened by the STOP lesson. It is not a lesson, so it never counts towards finishing.</summary>
        public const int GameLevel = 6;
        public const int GameUnlockedBy = 3;

        [SerializeField] List<int> levels = new List<int>();   // unlocked lesson levels
        [SerializeField] List<int> scores = new List<int>();   // mirror of progress.quizScores for the inspector
        [SerializeField] List<Badge> badges = new List<Badge>();
        [SerializeField] string userId;

        Progress progress = new Progress();

        public event Action<Badge> BadgeUnlocked;

        public IList<int> UnlockedLevels { get { return levels; } }
        public IList<Badge> Badges { get { return badges; } }

        void Awake()
        {
            badges = ContentLibrary.Badges;
            Core.Localization.Changed += ReloadBadges;
        }

        void OnDestroy()
        {
            Core.Localization.Changed -= ReloadBadges;
        }

        /// <summary>The badge titles come in the new language; which badges are earned stays the same.</summary>
        void ReloadBadges()
        {
            badges = ContentLibrary.Badges;
            foreach (var badge in badges) badge.unlocked = progress.badgesEarned.Contains(badge.badgeId);
        }

        public void Save()
        {
            progress.Save();
            scores = new List<int>(progress.quizScores);
        }

        public Progress Load()
        {
            progress = new Progress();
            progress.Load();
            if (string.IsNullOrEmpty(progress.userId))
            {
                // A random id for this install only. It identifies the device
                // profile, never the child.
                progress.userId = Guid.NewGuid().ToString();
                progress.Save();
            }
            userId = progress.userId;
            scores = new List<int>(progress.quizScores);
            RebuildUnlockedLevels();
            foreach (var badge in badges) badge.unlocked = progress.badgesEarned.Contains(badge.badgeId);
            return progress;
        }

        public Progress GetData()
        {
            return progress;
        }

        public void SetAgeGroup(string ageGroup)
        {
            progress.ageGroup = ageGroup;
            if (ageGroup == Progress.PathOlder) progress.guardianUnlocked = true;   // started on the 9-11 path
            Save();
        }

        /// <summary>
        /// Plays the other lesson path (6-8 or 9-11). Each path keeps its own finished lessons, stars and
        /// badges; the lessons, badges and certificate all read the path in play.
        /// </summary>
        public void SwitchPath(string ageGroup)
        {
            progress.SwitchPath(ageGroup);
            scores = new List<int>(progress.quizScores);
            RebuildUnlockedLevels();
            foreach (var badge in badges) badge.unlocked = progress.badgesEarned.Contains(badge.badgeId);
            Save();
        }

        public void SetLanguage(string language)
        {
            progress.language = language;
            Save();
        }

        public bool IsUnlocked(int level)
        {
            if (level == GameLevel) return progress.IsLevelComplete(GameUnlockedBy);
            return levels.Contains(level);
        }

        public void CompleteLevel(int level, int score)
        {
            if (!progress.completedLevels.Contains(level)) progress.completedLevels.Add(level);
            progress.SetScore(level, score);
            UpdateBadges();
            Save();
        }

        public void RecordScenario(Scenario scenario, string answer, bool correct)
        {
            progress.scenarioResponses.Add(new ScenarioResponse
            {
                scenarioId = scenario.scenarioId,
                selectedAnswer = answer,
                isCorrect = correct,
                answeredAt = DateTime.UtcNow.ToString("o")
            });
            Save();
        }

        /// <summary>Saves a chase time from the Memory Quest; the fastest time unlocks "Quick and Safe".</summary>
        public void RecordChase(float seconds)
        {
            if (progress.bestChaseSeconds <= 0f || seconds < progress.bestChaseSeconds) progress.bestChaseSeconds = seconds;
            UpdateBadges();
            Save();
        }

        /// <summary>Saves a Bubble Guard result; the best score unlocks the "Bubble Guard" badge.</summary>
        public void RecordGuard(int score)
        {
            progress.guardGamesPlayed++;
            progress.bestGuardScore = Mathf.Max(progress.bestGuardScore, score);
            UpdateBadges();
            Save();
        }

        /// <summary>Counts a spoken NO. Three-star tries count towards the "Strong Voice" badge.</summary>
        public void RecordNo(NoRating rating)
        {
            if (rating == null || rating.stars < 3) return;
            progress.strongNoCount++;
            UpdateBadges();
            Save();
        }

        public void SetSkinTone(string toneId)
        {
            progress.skinTone = SkinTones.Get(toneId).id;
            Save();
        }

        public void SetVoiceEnabled(bool on)
        {
            progress.voiceEnabled = on;
            Save();
        }

        public void SetTrustedAdults(IEnumerable<string> adultIds)
        {
            progress.trustedAdults = new List<string>(adultIds);
            UpdateBadges();
            Save();
        }

        public void SetCloudSyncConsent(bool consent)
        {
            progress.cloudSyncConsent = consent;
            Save();
        }

        /// <summary>Checks every badge rule and unlocks the ones now met.</summary>
        public List<Badge> UpdateBadges()
        {
            var newlyUnlocked = new List<Badge>();
            foreach (var badge in badges)
            {
                if (badge.unlocked || !RequirementMet(badge.requirement)) continue;
                progress.AddBadge(badge);
                newlyUnlocked.Add(badge);
                if (BadgeUnlocked != null) BadgeUnlocked(badge);
            }
            return newlyUnlocked;
        }

        /// <summary>Removes all progress from this phone (parent section).</summary>
        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(Progress.PrefsKey);
            foreach (var badge in badges) badge.unlocked = false;
            Load();
        }

        bool RequirementMet(string requirement)
        {
            if (string.IsNullOrEmpty(requirement)) return false;
            var parts = requirement.Split(':');
            int value = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            switch (parts[0])
            {
                case "complete": return progress.IsLevelComplete(value);
                case "adults": return progress.trustedAdults.Count >= value;
                case "all": return progress.completedLevels.Count >= LessonCount;
                case "fast": return progress.bestChaseSeconds > 0f && progress.bestChaseSeconds <= value;
                case "voice": return progress.strongNoCount >= value;
                case "guard": return progress.bestGuardScore >= value;
                default: return false;
            }
        }

        /// <summary>Opens each lesson whose previous lesson is complete. Lesson 1 is always open.</summary>
        public void RebuildUnlockedLevels()
        {
            levels.Clear();
            levels.Add(1);
            for (int level = 1; level < LessonCount; level++)
                if (progress.IsLevelComplete(level)) levels.Add(level + 1);
        }
    }
}
