using System;
using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>
    /// Loads the lesson content bundled in Resources/Content. Content is
    /// data, not code, so experts' corrections in week 7 are JSON edits.
    /// </summary>
    public static class ContentLibrary
    {
        [Serializable] class ScenarioList { public List<Scenario> items; }
        [Serializable] class AdultList { public List<TrustedAdult> items; }
        [Serializable] class BadgeList { public List<Badge> items; }
        [Serializable] class ResourceList { public List<ParentResource> items; }
        [Serializable] class ReviewList { public List<ReviewPair> items; }

        static List<Scenario> scenarios;
        static List<TrustedAdult> adults;
        static List<Badge> badges;
        static List<ParentResource> resources;
        static List<ReviewPair> reviewPairs;

        public static List<Scenario> Scenarios
        {
            get { return scenarios ?? (scenarios = (Load<ScenarioList>("scenarios").items ?? new List<Scenario>())); }
        }

        public static List<TrustedAdult> TrustedAdults
        {
            get { return adults ?? (adults = (Load<AdultList>("trusted_adults").items ?? new List<TrustedAdult>())); }
        }

        public static List<Badge> Badges
        {
            get { return badges ?? (badges = (Load<BadgeList>("badges").items ?? new List<Badge>())); }
        }

        public static List<ParentResource> ParentResources
        {
            get { return resources ?? (resources = (Load<ResourceList>("parent_resources").items ?? new List<ParentResource>())); }
        }

        public static List<ReviewPair> ReviewPairs
        {
            get { return reviewPairs ?? (reviewPairs = (Load<ReviewList>("review").items ?? new List<ReviewPair>())); }
        }

        static T Load<T>(string file) where T : new()
        {
            var asset = Resources.Load<TextAsset>("Content/" + file);
            if (asset == null)
            {
                Debug.LogError("[SafeTouch] Missing content file Resources/Content/" + file + ".json");
                return new T();
            }
            return JsonUtility.FromJson<T>(asset.text);
        }
    }

    /// <summary>A card in the parent and teacher section (FR9).</summary>
    [Serializable]
    public class ParentResource
    {
        public string resourceId;
        public string section;   // "talk", "signs" or "help"
        public string title;
        public string body;
        public string phone;     // only for "help" entries
    }

    /// <summary>A pair of cards in the Memory Quest match game (lesson 5).</summary>
    [Serializable]
    public class ReviewPair
    {
        public string pairId;
        public int level;        // 1 = everyone, 2 = ages 9-11 only
        public string word;      // e.g. "Red zone"
        public string meaning;   // e.g. "My body: only with my OK"
        public string remember;  // shown when the pair is matched
    }
}
