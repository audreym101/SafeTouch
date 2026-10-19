using NUnit.Framework;
using SafeTouch.Voice;

namespace SafeTouch.Tests
{
    public class SegmenterTests
    {
        [Test]
        public void AnalyserMeasuresOneFramePerHop()
        {
            var analyzer = new FrameAnalyzer(Signals.Rate);
            int frames = 0;
            analyzer.FrameReady += f => frames++;
            var second = Signals.Sine(250f, 1f, 0.5f);
            analyzer.Push(second, 0, second.Length);
            int expected = 1 + (second.Length - analyzer.FrameLength) * 2 / analyzer.FrameLength;
            Assert.AreEqual(expected, frames);
        }

        [Test]
        public void FindsOneWordAndItsLength()
        {
            var words = Signals.Segment(Signals.Try(Signals.Voice(280f, 260f, 0.5f, 0.3f)));
            Assert.AreEqual(1, words.Count);
            Assert.AreEqual(0.5f, words[0].Duration, 0.12f);
            Assert.IsFalse(words[0].cutOff);
        }

        [Test]
        public void IgnoresAClick()
        {
            var words = Signals.Segment(Signals.Try(Signals.Voice(280f, 280f, 0.04f, 0.5f)));
            Assert.AreEqual(0, words.Count);
        }

        [Test]
        public void IgnoresTheRoomOnItsOwn()
        {
            Assert.AreEqual(0, Signals.Segment(Signals.WithRoom(Signals.Silence(3f))).Count);
        }

        [Test]
        public void CutsOffAnAnswerThatGoesOnAndOn()
        {
            var words = Signals.Segment(Signals.Try(Signals.Voice(280f, 280f, 4f, 0.3f)));
            Assert.GreaterOrEqual(words.Count, 1);
            Assert.IsTrue(words[0].cutOff);
            Assert.AreEqual(2.5f, words[0].Duration, 0.1f);
        }

        [Test]
        public void ShortGapsStayInOneAnswer()
        {
            var noNoNo = Signals.Join(
                Signals.Voice(280f, 280f, 0.18f, 0.3f), Signals.Silence(0.13f),
                Signals.Voice(280f, 280f, 0.18f, 0.3f), Signals.Silence(0.13f),
                Signals.Voice(280f, 280f, 0.18f, 0.3f));
            var words = Signals.Segment(Signals.Try(noNoNo));
            Assert.AreEqual(1, words.Count);
            Assert.AreEqual(3, words[0].Syllables);
        }

        [Test]
        public void MeasuresAFallingPitch()
        {
            var words = Signals.Segment(Signals.Try(Signals.Voice(320f, 250f, 0.6f, 0.3f)));
            Assert.AreEqual(1, words.Count);
            Assert.Less(words[0].PitchSlopeSemitones, -2f);
            Assert.AreEqual(1, words[0].Syllables);
        }

        [Test]
        public void LearnsTheNoiseFloorOfTheRoom()
        {
            var analyzer = new FrameAnalyzer(Signals.Rate);
            var segmenter = new UtteranceSegmenter(analyzer.HopSeconds);
            analyzer.FrameReady += segmenter.Add;
            var room = Signals.Noise(1f, 0.02f, 3);   // a noisy classroom, about -40 dBFS
            analyzer.Push(room, 0, room.Length);
            Assert.AreEqual(VoiceMath.ToDb(0.02f / UnityEngine.Mathf.Sqrt(3f)), segmenter.NoiseFloorDb, 2f);
            Assert.IsTrue(segmenter.Settled);
        }
    }
}
