using System;
using System.Collections.Generic;
using SafeTouch.Core;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>
    /// Loads the lesson content bundled in Resources/Content, in the chosen
    /// language (Kinyarwanda files end in _rw). Content is
    /// data, not code, so experts' corrections in week 7 are JSON edits.
    /// </summary>
    public static class ContentLibrary
    {
        [Serializable] class ScenarioList { public List<Scenario> items; }
        [Serializable] class AdultList { public List<TrustedAdult> items; }
        [Serializable] class BadgeList { public List<Badge> items; }
        [Serializable] class ResourceList { public List<ParentResource> items; }
        [Serializable] class ReviewList { public List<ReviewPair> items; }
        [Serializable] class GuardList { public List<GuardRequest> items; }

        static List<Scenario> scenarios;
        static List<TrustedAdult> adults;
        static List<Badge> badges;
        static List<ParentResource> resources;
        static List<ReviewPair> reviewPairs;
        static List<GuardRequest> guardRequests;

        public static List<Scenario> Scenarios
        {
            get { CheckLanguage(); return scenarios ?? (scenarios = (Load<ScenarioList>("scenarios").items ?? new List<Scenario>())); }
        }

        public static List<TrustedAdult> TrustedAdults
        {
            get { CheckLanguage(); return adults ?? (adults = (Load<AdultList>("trusted_adults").items ?? new List<TrustedAdult>())); }
        }

        public static List<Badge> Badges
        {
            get { CheckLanguage(); return badges ?? (badges = (Load<BadgeList>("badges").items ?? new List<Badge>())); }
        }

        public static List<ParentResource> ParentResources
        {
            get { CheckLanguage(); return resources ?? (resources = (Load<ResourceList>("parent_resources").items ?? new List<ParentResource>())); }
        }

        public static List<ReviewPair> ReviewPairs
        {
            get { CheckLanguage(); return reviewPairs ?? (reviewPairs = (Load<ReviewList>("review").items ?? new List<ReviewPair>())); }
        }

        public static List<GuardRequest> GuardRequests
        {
            get { CheckLanguage(); return guardRequests ?? (guardRequests = (Load<GuardList>("guard_requests").items ?? new List<GuardRequest>())); }
        }

        static string loadedLanguage;

        /// <summary>
        /// Forgets the loaded content when the language changes, so the next
        /// read loads the right language. Called by every getter.
        /// </summary>
        static void CheckLanguage()
        {
            string language = string.IsNullOrEmpty(Localization.Language) ? "en" : Localization.Language;
            if (language == loadedLanguage) return;
            loadedLanguage = language;
            scenarios = null;
            adults = null;
            badges = null;
            resources = null;
            reviewPairs = null;
            guardRequests = null;
        }

        /// <summary>
        /// Loads Content/file_rw.json in Kinyarwanda (and so on for other
        /// languages) when it exists, otherwise the English Content/file.json.
        /// The translated files keep the same ids, so progress is unchanged.
        /// </summary>
        static T Load<T>(string file) where T : new()
        {
            TextAsset asset = null;
            if (loadedLanguage != null && loadedLanguage != "en")
                asset = Resources.Load<TextAsset>("Content/" + file + "_" + loadedLanguage);
            if (asset == null) asset = Resources.Load<TextAsset>("Content/" + file);
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

    /// <summary>What a visitor asks Keza in the Bubble Guard game.</summary>
    [Serializable]
    public class GuardRequest
    {
        public string requestId;
        public int level;        // 1 = everyone, 2 = ages 9-11 only
        public bool safe;        // false: the right answer is a strong NO
        public string text;      // shown in the visitor's speech bubble
        public string explain;   // shown after the child answers
    }
}
