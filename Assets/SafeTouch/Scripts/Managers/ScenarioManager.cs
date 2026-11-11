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
        /// Loads one age group's own deck: level 1 for ages 6-8 (9 cards), level 2 for ages 9-11 (10 cards).
        /// The decks share no cards, so a child who grows into the older path meets new situations
        /// that teach the same ideas in more detail.
        /// </summary>
        public void LoadScenarios(int level)
        {
            scenarios = ContentLibrary.Scenarios.Where(s => s.level == level).ToList();
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
