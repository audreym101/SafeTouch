using System;
using System.Collections.Generic;

namespace SafeTouch.Data
{
    [Serializable]
    public class ScenarioResult
    {
        public string scenarioId;
        public bool answeredCorrectly;
    }

    [Serializable]
    public class ProgressData
    {
        public int schemaVersion = 1;
        public string lastUpdatedUtc = string.Empty;

        // FR3 / Personal Space Bubble
        public bool spaceBubbleCompleted;
        public int spaceBubbleZonesExplored; // out of 3 (comfortable, caution, boundary)

        // FR4-FR5 / Scenario cards
        public List<ScenarioResult> scenarioResults = new List<ScenarioResult>();
        public bool scenarioCardsCompleted;

        // FR6 / STOP framework
        public bool[] stopStepsCompleted = new bool[4]; // S, T, O, P
        public bool stopFrameworkCompleted;

        // FR7 / Trusted adults. Role ids only (e.g. "parent", "teacher") - never a real
        // name or photo, so this list is safe to read on-device but must never be
        // included in any anonymous sync payload (see AppConfig.CloudSyncEnabled).
        public List<string> trustedAdultRolesSelected = new List<string>();
        public bool trustedAdultsCompleted;

        public int ScenariosCorrectCount()
        {
            int count = 0;
            foreach (var r in scenarioResults)
            {
                if (r.answeredCorrectly) count++;
            }
            return count;
        }

        public bool AllLessonsCompleted()
        {
            return spaceBubbleCompleted && scenarioCardsCompleted && stopFrameworkCompleted && trustedAdultsCompleted;
        }
    }
}
