using System.Linq;
using NUnit.Framework;
using SafeTouch.Voice;

namespace SafeTouch.Tests
{
    /// <summary>
    /// The pieces behind keeping the child's spoken NO: the ring the word is
    /// cut from, the WAV file it is saved as, and the journal that decides
    /// which recordings stay (the first, the best and the newest) and what the
    /// child has achieved.
    /// </summary>
    public class NoRecordingTests
    {
        // ---- SampleRing ----

        [Test]
        public void RingGivesTheNewestSamplesOldestFirst()
        {
            var ring = new SampleRing(5);
            ring.Push(new float[] { 1, 2, 3, 4, 5, 6, 7 }, 0, 7);
            Assert.AreEqual(5, ring.Count);
            CollectionAssert.AreEqual(new float[] { 5, 6, 7 }, ring.Last(3));
            CollectionAssert.AreEqual(new float[] { 3, 4, 5, 6, 7 }, ring.Last(10), "never more than it holds");
        }

        [Test]
        public void RingBeforeItFillsGivesOnlyWhatItHeard()
        {
            var ring = new SampleRing(8);
            ring.Push(new float[] { 9, 1, 2 }, 1, 2);
            CollectionAssert.AreEqual(new float[] { 1, 2 }, ring.Last(5));
            ring.Clear();
            Assert.AreEqual(0, ring.Last(5).Length);
        }

        [Test]
        public void FadeStartsAndEndsInSilence()
        {
            var samples = Enumerable.Repeat(1f, 100).ToArray();
            SampleRing.FadeEdges(samples, 10);
            Assert.AreEqual(0f, samples[0]);
            Assert.AreEqual(0f, samples[99]);
            Assert.AreEqual(1f, samples[50], "the middle is untouched");
        }

        // ---- Wav ----

        [Test]
        public void WavRoundTripKeepsTheSoundAndRate()
        {
            var samples = Signals.Sine(220f, 0.25f, 0.5f, 16000);
            var bytes = Wav.Encode(samples, 16000);
            Assert.AreEqual(44 + samples.Length * 2, bytes.Length);

            int rate;
            var back = Wav.Decode(bytes, out rate);
            Assert.AreEqual(16000, rate);
            Assert.AreEqual(samples.Length, back.Length);
            for (int i = 0; i < samples.Length; i++) Assert.AreEqual(samples[i], back[i], 1f / 16000f);
        }

        [Test]
        public void WavClampsAShoutThatClips()
        {
            int rate;
            var back = Wav.Decode(Wav.Encode(new[] { 2f, -2f }, 8000), out rate);
            Assert.AreEqual(1f, back[0], 1e-4f);
            Assert.AreEqual(-1f, back[1], 1e-4f);
        }

        [Test]
        public void WavRejectsAnythingElse()
        {
            int rate;
            Assert.IsNull(Wav.Decode(null, out rate));
            Assert.IsNull(Wav.Decode(new byte[60], out rate));
        }

        // ---- NoJournal ----

        static NoEntry No(int power, int stars = 2, bool loud = false, string source = "lesson:1")
        {
            return new NoEntry { id = System.Guid.NewGuid().ToString("N"), power = power, stars = stars, loud = loud, source = source, audio = "a" + power + ".wav" };
        }

        [Test]
        public void JournalKeepsSoundForTheFirstTheBestAndTheNewest()
        {
            var journal = new NoJournal();
            var first = No(10);
            var best = No(90);
            journal.Add(first);
            journal.Add(No(20));
            journal.Add(best);
            var deleted = new System.Collections.Generic.List<string>();
            for (int i = 0; i < NoJournal.RecentClips + 3; i++) deleted.AddRange(journal.Add(No(30 + i)));

            Assert.IsTrue(first.HasAudio, "the first NO keeps its sound");
            Assert.IsTrue(best.HasAudio, "the best NO keeps its sound");
            Assert.AreEqual(NoJournal.RecentClips + 2, journal.WithAudio().Count);
            Assert.Contains("a20.wav", deleted, "an old ordinary NO loses its sound");
            Assert.AreSame(first, journal.First);
            Assert.AreSame(best, journal.Best);
        }

        [Test]
        public void ANewRecordFreesTheOldBestsSound()
        {
            var journal = new NoJournal();
            journal.Add(No(5));
            var oldBest = No(60);
            journal.Add(oldBest);
            for (int i = 0; i < NoJournal.RecentClips; i++) journal.Add(No(20));
            var deleted = journal.Add(No(95));
            Assert.IsFalse(oldBest.HasAudio);
            Assert.Contains("a60.wav", deleted);
        }

        [Test]
        public void JournalNeverGrowsPastItsLimitButCountsEveryNo()
        {
            var journal = new NoJournal();
            var first = No(1);
            journal.Add(first);
            for (int i = 0; i < NoJournal.MaxEntries + 20; i++) journal.Add(No(50, 3, true));
            Assert.AreEqual(NoJournal.MaxEntries, journal.entries.Count);
            Assert.AreEqual(NoJournal.MaxEntries + 21, journal.total);
            Assert.AreEqual(NoJournal.MaxEntries + 20, journal.threeStars);
            Assert.AreEqual(NoJournal.MaxEntries + 20, journal.passes);
            Assert.IsTrue(journal.entries.Contains(first), "the first NO is never dropped");
        }

        [Test]
        public void ForgettingAudioKeepsTheNumbers()
        {
            var journal = new NoJournal();
            journal.Add(No(40));
            journal.Add(No(70));
            var files = journal.ForgetAudio();
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(0, journal.WithAudio().Count);
            Assert.AreEqual(2, journal.total);
            Assert.AreEqual(70, journal.Best.power);
        }

        [Test]
        public void OnlyTheVeryFirstNoIsMarkedFirst()
        {
            var journal = new NoJournal();
            var a = No(10);
            var b = No(20);
            journal.Add(a);
            journal.Add(b);
            Assert.IsTrue(a.first);
            Assert.IsFalse(b.first);
        }

        // ---- Milestones ----

        [Test]
        public void NothingIsAchievedBeforeTheFirstNo()
        {
            var milestones = NoMilestones.For(new NoJournal(), 0, 0);
            Assert.IsTrue(milestones.All(m => !m.Done));
        }

        [Test]
        public void AchievementsFollowTheChildsNos()
        {
            var journal = new NoJournal();
            journal.Add(No(30));
            journal.Add(No(55, 3, true, "practice"));
            var done = NoMilestones.For(journal, 55, 1).Where(m => m.Done).Select(m => m.id).ToList();
            CollectionAssert.AreEquivalent(new[] { "first", "pass", "stars", "power50", "grew" }, done);
            Assert.AreEqual(1, journal.practices);
        }

        [Test]
        public void TheRecordFromBeforeTheJournalStillCounts()
        {
            var milestones = NoMilestones.For(new NoJournal(), 85, 3);
            Assert.IsTrue(milestones.First(m => m.id == "power80").Done);
            Assert.IsTrue(milestones.First(m => m.id == "strong3").Done);
            Assert.IsFalse(milestones.First(m => m.id == "ten").Done);
        }
    }
}
