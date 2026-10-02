using System.Collections.Generic;
using System.Linq;
using SafeTouch.Data;
using UnityEngine;

namespace SafeTouch.Managers
{
    /// <summary>Serves scenario cards and checks answers (FR4, FR5).</summary>
    public class ScenarioManager : MonoBehaviour
    {
        [SerializeField] List<Scenario> scenarios = new List<Scenario>();
        [SerializeField] int currentIndex;

        public int Count { get { return scenarios.Count; } }
        public int CurrentIndex { get { return currentIndex; } }
        public Scenario Current { get { return scenarios.Count > 0 ? scenarios[currentIndex] : null; } }
        public bool IsLast { get { return currentIndex >= scenarios.Count - 1; } }

        /// <summary>
        /// Loads every scenario up to the given difficulty level. Ages 6-8 use
        /// level 1 (7 cards); ages 9-11 use level 2 (all 13 cards).
        /// </summary>
        public void LoadScenarios(int level)
        {
            scenarios = ContentLibrary.Scenarios.Where(s => s.level <= level).ToList();
            foreach (var s in scenarios)
                if (!string.IsNullOrEmpty(s.audioClipName))
                    s.audioClip = Resources.Load<AudioClip>("Audio/Voice/" + s.audioClipName);
            currentIndex = 0;
        }

        public bool CheckAnswer(string ans)
        {
            return Current != null && Current.CheckAnswer(ans);
        }

        public void Next()
        {
            if (currentIndex < scenarios.Count - 1) currentIndex++;
        }

        public void Previous()
        {
            if (currentIndex > 0) currentIndex--;
        }
    }
}
