using System;
using UnityEngine;
using SafeTouch.Data;

namespace SafeTouch.Core
{
    public class ProgressTracker : MonoBehaviour
    {
        public static ProgressTracker Instance { get; private set; }

        public ProgressData Current { get; private set; } = new ProgressData();

        public event Action OnProgressChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(AppConfig.ProgressPrefsKey, string.Empty);
            Current = string.IsNullOrEmpty(json) ? new ProgressData() : JsonUtility.FromJson<ProgressData>(json);
            if (Current == null) Current = new ProgressData();
        }

        public void Save()
        {
            Current.lastUpdatedUtc = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(Current);
            PlayerPrefs.SetString(AppConfig.ProgressPrefsKey, json);
            PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
        }

        public void ResetProgress()
        {
            Current = new ProgressData();
            Save();
        }

        public void MarkLessonComplete(LessonType lesson)
        {
            switch (lesson)
            {
                case LessonType.SpaceBubble: Current.spaceBubbleCompleted = true; break;
                case LessonType.ScenarioCards: Current.scenarioCardsCompleted = true; break;
                case LessonType.StopFramework: Current.stopFrameworkCompleted = true; break;
                case LessonType.TrustedAdults: Current.trustedAdultsCompleted = true; break;
            }
            Save();
        }

        public bool IsLessonComplete(LessonType lesson)
        {
            switch (lesson)
            {
                case LessonType.SpaceBubble: return Current.spaceBubbleCompleted;
                case LessonType.ScenarioCards: return Current.scenarioCardsCompleted;
                case LessonType.StopFramework: return Current.stopFrameworkCompleted;
                case LessonType.TrustedAdults: return Current.trustedAdultsCompleted;
                default: return false;
            }
        }

        public void RecordSpaceBubbleZoneExplored(int zoneIndex)
        {
            Current.spaceBubbleZonesExplored = Mathf.Max(Current.spaceBubbleZonesExplored, zoneIndex + 1);
            Save();
        }

        public void RecordScenarioAnswer(string scenarioId, bool correct)
        {
            Current.scenarioResults.RemoveAll(r => r.scenarioId == scenarioId);
            Current.scenarioResults.Add(new ScenarioResult { scenarioId = scenarioId, answeredCorrectly = correct });
            Save();
        }

        public void RecordStopStepComplete(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= Current.stopStepsCompleted.Length) return;
            Current.stopStepsCompleted[stepIndex] = true;
            Save();
        }

        public void RecordTrustedAdultRole(string roleId)
        {
            if (!Current.trustedAdultRolesSelected.Contains(roleId))
            {
                Current.trustedAdultRolesSelected.Add(roleId);
            }
            Save();
        }
    }
}
