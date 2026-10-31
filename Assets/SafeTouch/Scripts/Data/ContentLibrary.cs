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
        [Serializable] class TermsList { public List<TermsSection> items; }

        static List<Scenario> scenarios;
        static List<TrustedAdult> adults;
        static List<TrustedAdult> unsafePeople;
        static List<Badge> badges;
        static List<ParentResource> resources;
        static List<ReviewPair> reviewPairs;
        static List<GuardRequest> guardRequests;
        static List<TermsSection> terms;

        public static List<Scenario> Scenarios
        {
            get { CheckLanguage(); return scenarios ?? (scenarios = (Load<ScenarioList>("scenarios").items ?? new List<Scenario>())); }
        }

        public static List<TrustedAdult> TrustedAdults
        {
            get { CheckLanguage(); return adults ?? (adults = (Load<AdultList>("trusted_adults").items ?? new List<TrustedAdult>())); }
        }

        /// <summary>Kinds of people a child can say make them feel unsafe (lesson 4). Roles only, never names.</summary>
        public static List<TrustedAdult> UnsafePeople
        {
            get { CheckLanguage(); return unsafePeople ?? (unsafePeople = (Load<AdultList>("unsafe_people").items ?? new List<TrustedAdult>())); }
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

        /// <summary>The terms and privacy notice shown on the parental consent page.</summary>
        public static List<TermsSection> Terms
        {
            get { CheckLanguage(); return terms ?? (terms = (Load<TermsList>("terms").items ?? new List<TermsSection>())); }
        }

        static string loadedLanguage, loadedName;

        /// <summary>
        /// Forgets the loaded content when the language or the guide's name
        /// changes, so the next read loads the right words. Called by every getter.
        /// </summary>
        static void CheckLanguage()
        {
            string language = string.IsNullOrEmpty(Localization.Language) ? "en" : Localization.Language;
            if (language == loadedLanguage && Localization.CharacterName == loadedName) return;
            loadedLanguage = language;
            loadedName = Localization.CharacterName;
            scenarios = null;
            adults = null;
            unsafePeople = null;
            badges = null;
            resources = null;
            reviewPairs = null;
            guardRequests = null;
            terms = null;
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
            // The content says "Keza"; the child's chosen name goes in instead. Names are
            // cleaned to letters, spaces, hyphens and apostrophes, so the JSON stays valid.
            return JsonUtility.FromJson<T>(Localization.Named(asset.text));
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
        public string who;       // "child" for a child visitor (the Classmate model); empty for an adult
        public string text;      // shown in the visitor's speech bubble
        public string explain;   // shown after the child answers
    }

    /// <summary>One part of the terms and privacy notice on the consent page.</summary>
    [Serializable]
    public class TermsSection
    {
        public string termId;
        public string title;
        public string body;
    }
}
