using NUnit.Framework;
using SafeTouch.Voice;

namespace SafeTouch.Tests
{
    /// <summary>
    /// The coach's verdicts on synthetic "NO"s. Room noise is about -59 dBFS;
    /// a strong voice at amplitude 0.3 is about 45 dB above it.
    /// </summary>
    public class NoCoachTests
    {
        static NoRating RateTry(float[] clip, string ageGroup = "6-8")
        {
            var words = Signals.Segment(Signals.Try(clip));
            Assert.AreEqual(1, words.Count, "expected exactly one answer");
            return NoCoach.Rate(words[0], NoCoachSettings.ForAgeGroup(ageGroup));
        }

        [Test]
        public void AStrongFallingNoGetsThreeStars()
        {
            var r = RateTry(Signals.Voice(300f, 260f, 0.5f, 0.3f));
            Assert.AreEqual(3, r.stars);
            Assert.AreEqual(NoTip.Strong, r.tip);
            Assert.IsTrue(r.loud && r.steady && r.brief);
        }

        [Test]
        public void ARisingNoSoundsLikeAQuestion()
        {
            var r = RateTry(Signals.Voice(250f, 340f, 0.5f, 0.3f));
            Assert.AreEqual(NoTip.Question, r.tip);
            Assert.IsFalse(r.steady);
            Assert.AreEqual(2, r.stars);
        }

        [Test]
        public void AQuietNoIsAskedToBeALittleLouder()
        {
            var r = RateTry(Signals.Voice(290f, 270f, 0.5f, 0.008f));
            Assert.AreEqual(NoTip.ALittleLouder, r.tip);
            Assert.IsFalse(r.loud);
            Assert.AreEqual(2, r.stars);
        }

        // A child should not have to shout: an ordinary firm voice counts in a busy classroom too.
        static System.Collections.Generic.List<Utterance> InRoom(float roomNoise, float voiceAmplitude)
        {
            var clip = Signals.Voice(300f, 270f, 0.5f, voiceAmplitude);
            return Signals.Segment(Signals.WithRoom(Signals.Join(Signals.Silence(0.5f), clip, Signals.Silence(1f)), roomNoise));
        }

        [Test]
        public void ANormalVoiceInAClassroomIsLoudEnough()
        {
            var words = InRoom(0.012f, 0.10f);   // classroom noise about -43 dBFS, an ordinary speaking voice
            Assert.AreEqual(1, words.Count);
            var r = NoCoach.Rate(words[0], NoCoachSettings.ForAgeGroup("6-8"));
            Assert.IsTrue(r.loud, "a normal voice is loud enough without shouting");
            Assert.AreEqual(3, r.stars);
        }

        [Test]
        public void ANormalVoiceInANoisyRoomIsStillHeard()
        {
            var words = InRoom(0.03f, 0.10f);   // a noisy room, about -35 dBFS
            Assert.AreEqual(1, words.Count, "heard, not ignored");
            var r = NoCoach.Rate(words[0], NoCoachSettings.ForAgeGroup("9-11"));
            Assert.IsTrue(r.Heard);
            Assert.IsTrue(r.Good, "two stars or more: the lesson moves on");
        }

        [Test]
        public void ARustleOrScrapeHasNoVoiceAndAClapIsIgnored()
        {
            var settings = NoCoachSettings.ForAgeGroup("6-8");
            var scrape = Signals.Segment(Signals.WithRoom(Signals.Join(Signals.Silence(0.8f), Signals.Noise(0.4f, 0.12f, 4), Signals.Silence(1f)), 0.012f));
            foreach (var w in scrape) Assert.Less(w.VoicedFraction, settings.minVoicedFraction, "noise has no pitch: the game does not count it as a NO");
            var clap = Signals.Segment(Signals.WithRoom(Signals.Join(Signals.Silence(0.8f), Signals.Noise(0.03f, 0.5f, 3), Signals.Silence(1f)), 0.012f));
            Assert.AreEqual(0, clap.Count, "a 30 ms click is not a word");
            var nothing = Signals.Segment(Signals.WithRoom(Signals.Silence(2f), 0.012f));
            Assert.AreEqual(0, nothing.Count);
        }

        [Test]
        public void ASqueakFarAboveTheTalkingVoiceIsFlagged()
        {
            var r = RateTry(Signals.Voice(620f, 600f, 0.5f, 0.3f));
            Assert.AreEqual(NoTip.Squeaky, r.tip);
            Assert.Greater(r.aboveTalkingSemitones, 6f);
        }

        [Test]
        public void ALongNoooIsTooLong()
        {
            var r = RateTry(Signals.Voice(290f, 270f, 2f, 0.3f));
            Assert.AreEqual(NoTip.TooLong, r.tip);
            Assert.IsFalse(r.brief);
        }

        [Test]
        public void ThreeWordsAreTooMany()
        {
            var clip = Signals.Join(
                Signals.Voice(290f, 280f, 0.18f, 0.3f), Signals.Silence(0.13f),
                Signals.Voice(290f, 280f, 0.18f, 0.3f), Signals.Silence(0.13f),
                Signals.Voice(290f, 280f, 0.18f, 0.3f));
            var r = RateTry(clip);
            Assert.AreEqual(NoTip.TooManyWords, r.tip);
        }

        [Test]
        public void AWhisperHasNoVoice()
        {
            var r = RateTry(Signals.Noise(0.5f, 0.3f, 11));
            Assert.AreEqual(NoTip.Whisper, r.tip);
            Assert.AreEqual(1, r.stars);
        }

        [Test]
        public void AScreamIntoThePhoneKeepsItsStarsButGetsATip()
        {
            var r = RateTry(Signals.Clip(Signals.Voice(300f, 260f, 0.5f, 1.5f)));
            Assert.IsTrue(r.shouted);
            Assert.AreEqual(NoTip.Shout, r.tip);
            Assert.AreEqual(3, r.stars);
        }

        [Test]
        public void PitchIsJudgedAgainstTheChildsOwnVoice()
        {
            var words = Signals.Segment(Signals.Try(Signals.Voice(430f, 410f, 0.5f, 0.3f)));
            var settings = NoCoachSettings.ForAgeGroup("9-11");
            Assert.AreEqual(NoTip.Squeaky, NoCoach.Rate(words[0], settings).tip);

            settings.SetTalkingPitch(380f);   // this child simply has a high voice
            Assert.AreEqual(NoTip.Strong, NoCoach.Rate(words[0], settings).tip);
        }

        [Test]
        public void NothingHeardIsZeroStars()
        {
            var r = NoCoach.Rate(null, new NoCoachSettings());
            Assert.AreEqual(0, r.stars);
            Assert.AreEqual(NoTip.NotHeard, r.tip);
            Assert.IsFalse(r.Heard);
        }

        [Test]
        public void TalkingPitchIsKeptInAChildsRange()
        {
            var s = new NoCoachSettings();
            s.SetTalkingPitch(90f);
            Assert.AreEqual(150f, s.talkingPitchHz);
            Assert.IsTrue(s.calibrated);
        }
    }
}
