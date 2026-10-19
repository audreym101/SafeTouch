using NUnit.Framework;
using SafeTouch.Voice;

namespace SafeTouch.Tests
{
    public class PitchDetectorTests
    {
        // A low adult voice, the 6-11 talking range, and a child's shout.
        [TestCase(150f)]
        [TestCase(220f)]
        [TestCase(290f)]
        [TestCase(440f)]
        [TestCase(700f)]
        public void FindsThePitchOfAToneWithinOnePercent(float hz)
        {
            var detector = new PitchDetector(Signals.Rate);
            var tone = Signals.Sine(hz, 0.1f, 0.5f);
            float confidence;
            float found = detector.Detect(tone, 0, 1024, out confidence);
            Assert.AreEqual(hz, found, hz * 0.01f);
            Assert.Greater(confidence, 0.9f);
        }

        [Test]
        public void FindsTheFundamentalOfAVoiceWithHarmonics()
        {
            var detector = new PitchDetector(Signals.Rate);
            var voice = Signals.Voice(280f, 280f, 0.1f, 0.5f);
            Assert.AreEqual(280f, detector.Detect(voice, 0, 1024), 3f);
        }

        [Test]
        public void WorksAtTheCommonPhoneRateToo()
        {
            var detector = new PitchDetector(44100);
            var tone = Signals.Sine(300f, 0.1f, 0.5f, 44100);
            Assert.AreEqual(300f, detector.Detect(tone, 0, 2048), 3f);
        }

        [Test]
        public void NoiseHasNoPitch()
        {
            var detector = new PitchDetector(Signals.Rate);
            var noise = Signals.Noise(0.1f, 0.3f, 1);
            float confidence;
            float found = detector.Detect(noise, 0, 1024, out confidence);
            Assert.That(found == 0f || confidence < VoiceFrame.MinConfidence, "noise read as " + found + " Hz");
        }

        [Test]
        public void SilenceHasNoPitch()
        {
            var detector = new PitchDetector(Signals.Rate);
            Assert.AreEqual(0f, detector.Detect(Signals.Silence(0.1f), 0, 1024));
        }

        [Test]
        public void FrameIsLongEnoughForTheLowestPitch()
        {
            var detector = new PitchDetector(Signals.Rate);
            Assert.LessOrEqual(detector.MinFrameLength, new FrameAnalyzer(Signals.Rate).FrameLength);
        }
    }
}
