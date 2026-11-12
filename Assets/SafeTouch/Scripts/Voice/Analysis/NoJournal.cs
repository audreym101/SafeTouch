using System;
using System.Collections.Generic;

namespace SafeTouch.Voice
{
    /// <summary>One spoken NO the coach heard: how it was rated, and its recording if one is kept.</summary>
    [Serializable]
    public class NoEntry
    {
        public string id;
        public string at;          // when, ISO 8601 UTC
        public int power;          // NO power, 0 to 100
        public int stars;          // 0 to 3
        public bool loud, steady, brief;
        public string tip;         // NoTip name
        public float seconds;
        public string source;      // "practice", or "lesson:<level>"
        public bool first;         // the child's first spoken NO
        public string audio = "";  // the WAV file's name in the recordings folder; "" when no sound is kept

        public bool HasAudio { get { return !string.IsNullOrEmpty(audio); } }
    }

    /// <summary>
    /// The child's spoken NOs, kept on this phone only (consent terms 1.6):
    /// the numbers for every recent NO, and the sound of a few of them, so the
    /// child and their grown-up can hear how strong the NO has become.
    ///
    /// Sound is kept for the first NO, the best NO and the RecentClips newest
    /// ones, and nothing else: older recordings are deleted as new ones come.
    /// The numbers of at most MaxEntries NOs are kept; the totals count them all.
    /// </summary>
    [Serializable]
    public class NoJournal
    {
        public const int MaxEntries = 60;
        public const int RecentClips = 8;

        public List<NoEntry> entries = new List<NoEntry>();   // oldest first
        public int total;        // every spoken NO heard, including those no longer listed
        public int passes;       // NOs that reached the PASS line
        public int threeStars;   // NOs with three stars
        public int practices;    // NOs said on the My NO practice screen

        public NoEntry First
        {
            get
            {
                foreach (var e in entries) if (e.first) return e;
                return entries.Count > 0 ? entries[0] : null;
            }
        }

        /// <summary>The loudest NO; the earlier one when two are equal, so the record keeps its date.</summary>
        public NoEntry Best
        {
            get
            {
                NoEntry best = null;
                foreach (var e in entries) if (best == null || e.power > best.power) best = e;
                return best;
            }
        }

        public NoEntry Latest { get { return entries.Count > 0 ? entries[entries.Count - 1] : null; } }

        /// <summary>The NOs that have a recording, newest first.</summary>
        public List<NoEntry> WithAudio()
        {
            var list = new List<NoEntry>();
            for (int i = entries.Count - 1; i >= 0; i--) if (entries[i].HasAudio) list.Add(entries[i]);
            return list;
        }

        /// <summary>Adds a NO. Returns the names of recordings no longer kept, for the caller to delete.</summary>
        public List<string> Add(NoEntry entry)
        {
            entry.first = total == 0;
            total++;
            if (entry.loud) passes++;
            if (entry.stars >= 3) threeStars++;
            if (entry.source == "practice") practices++;
            entries.Add(entry);
            return Prune();
        }

        /// <summary>Forgets every recording (the numbers stay). Returns the file names to delete.</summary>
        public List<string> ForgetAudio()
        {
            var files = new List<string>();
            foreach (var e in entries)
            {
                if (!e.HasAudio) continue;
                files.Add(e.audio);
                e.audio = "";
            }
            return files;
        }

        List<string> Prune()
        {
            var first = First;
            var best = Best;
            var files = new List<string>();

            // Sound: the first, the best and the newest few.
            int recentFrom = entries.Count - RecentClips;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!e.HasAudio || e == first || e == best || i >= recentFrom) continue;
                files.Add(e.audio);
                e.audio = "";
            }

            // Numbers: the newest MaxEntries, always with the first and the best.
            for (int i = 0; entries.Count > MaxEntries && i < entries.Count;)
            {
                var e = entries[i];
                if (e == first || e == best) { i++; continue; }
                if (e.HasAudio) files.Add(e.audio);
                entries.RemoveAt(i);
            }
            return files;
        }
    }

    /// <summary>A goal on the child's NO journey, and how close they are.</summary>
    public class NoMilestone
    {
        public string id;
        public int value;   // where the child is now
        public int goal;    // what reaching it needs
        public bool Done { get { return value >= goal; } }
    }

    /// <summary>
    /// What the child has achieved with their voice, shown on the My NO
    /// screen. Each goal is reached once and stays reached.
    /// </summary>
    public static class NoMilestones
    {
        public const int PowerGood = 50;
        public const int PowerMighty = 80;
        public const int Practised = 10;
        public const int Grew = 15;   // NO power points stronger than the first NO

        /// <summary>strongNoCount is Progress.strongNoCount (three-star NOs counted before the journal began too).</summary>
        public static List<NoMilestone> For(NoJournal journal, int loudestNo, int strongNoCount)
        {
            var first = journal.First;
            var best = journal.Best;
            int grew = first != null && best != null ? best.power - first.power : 0;
            int loudest = Math.Max(loudestNo, best != null ? best.power : 0);
            int strong = Math.Max(strongNoCount, journal.threeStars);
            return new List<NoMilestone>
            {
                new NoMilestone { id = "first",    value = journal.total,   goal = 1 },
                new NoMilestone { id = "pass",     value = journal.passes,  goal = 1 },
                new NoMilestone { id = "stars",    value = strong,          goal = 1 },
                new NoMilestone { id = "power50",  value = loudest,         goal = PowerGood },
                new NoMilestone { id = "strong3",  value = strong,          goal = 3 },
                new NoMilestone { id = "ten",      value = journal.total,   goal = Practised },
                new NoMilestone { id = "grew",     value = Math.Max(0, grew), goal = Grew },
                new NoMilestone { id = "power80",  value = loudest,         goal = PowerMighty },
            };
        }
    }
}
