using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SafeTouch.Tests
{
    /// <summary>
    /// Keeps the Kinyarwanda files in step with the English ones, so editing
    /// a translation can never change an answer, lose a card or crash a
    /// string.Format with a missing {0}.
    /// </summary>
    public class LocalisationTests
    {
        const string Content = "Assets/SafeTouch/Resources/Content/";

        [Serializable] class Entry { public string key; public string value; }
        [Serializable] class Strings { public List<Entry> items; }
        [Serializable] class Item
        {
            public string scenarioId, adultId, badgeId, resourceId, pairId, requestId, termId;
            public string correctAnswer, requirement, section, phone;
            public bool safe;
            public int level;
        }
        [Serializable] class Items { public List<Item> items; }

        static T Read<T>(string file)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(Content + file));
        }

        /// <summary>JsonUtility leaves missing strings empty rather than null, so take the first one that is set.</summary>
        static string Id(Item item)
        {
            return new[] { item.scenarioId, item.adultId, item.badgeId, item.resourceId, item.pairId, item.requestId, item.termId }
                .FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "";
        }

        static readonly Regex Placeholder = new Regex(@"\{\d+(:[^}]*)?\}");

        static string Placeholders(string text)
        {
            return string.Join(" ", Placeholder.Matches(text).Cast<Match>().Select(m => m.Value).Distinct().OrderBy(v => v).ToArray());
        }

        [TestCase("scenarios")]
        [TestCase("trusted_adults")]
        [TestCase("unsafe_people")]
        [TestCase("badges")]
        [TestCase("parent_resources")]
        [TestCase("review")]
        [TestCase("guard_requests")]
        [TestCase("terms")]
        public void KinyarwandaContentMatchesTheEnglish(string file)
        {
            var en = Read<Items>(file + ".json").items;
            var rw = Read<Items>(file + "_rw.json").items;
            Assert.AreEqual(en.Count, rw.Count, file + ": same number of items");
            for (int i = 0; i < en.Count; i++)
            {
                var e = en[i];
                var r = rw[i];
                string id = Id(e), rid = Id(r);
                Assert.IsNotEmpty(id, file + ": every item has an id");
                Assert.AreEqual(id, rid, file + ": items in the same order");
                Assert.AreEqual(e.correctAnswer, r.correctAnswer, id + ": same answer");
                Assert.AreEqual(e.safe, r.safe, id + ": same safe/unsafe");
                Assert.AreEqual(e.level, r.level, id + ": same age level");
                Assert.AreEqual(e.requirement, r.requirement, id + ": same badge rule");
                Assert.AreEqual(e.section, r.section, id + ": same section");
                Assert.AreEqual(e.phone, r.phone, id + ": same phone number");
            }
        }

        [Test]
        public void KinyarwandaStringsKeepTheirPlaceholders()
        {
            var en = Read<Strings>("strings_en.json").items.ToDictionary(e => e.key, e => e.value);
            foreach (var r in Read<Strings>("strings_rw.json").items)
            {
                string english;
                if (!en.TryGetValue(r.key, out english)) continue;
                Assert.AreEqual(Placeholders(english), Placeholders(r.value), r.key);
            }
        }

        [Test]
        public void KinyarwandaStringsAreEachDefinedOnce()
        {
            var keys = Read<Strings>("strings_rw.json").items.Select(e => e.key).ToList();
            var repeated = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.IsEmpty(repeated, "repeated keys: " + string.Join(", ", repeated));
            Assert.Greater(keys.Count, 250, "the interface is translated, not just a few words");
        }
    }
}
