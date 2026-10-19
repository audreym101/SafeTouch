using System.Collections.Generic;
using NUnit.Framework;
using SafeTouch.Voice;

namespace SafeTouch.Tests
{
    public class VoiceMathTests
    {
        [Test]
        public void RmsOfSineIsAmplitudeOverRootTwo()
        {
            var s = Signals.Sine(250f, 1f, 0.5f);
            Assert.AreEqual(0.5f / System.Math.Sqrt(2), VoiceMath.Rms(s, 0, s.Length), 0.002);
        }

        [Test]
        public void DecibelsAreRelativeToFullScale()
        {
            Assert.AreEqual(0f, VoiceMath.ToDb(1f), 0.001f);
            Assert.AreEqual(-20f, VoiceMath.ToDb(0.1f), 0.001f);
            Assert.AreEqual(VoiceMath.SilenceDb, VoiceMath.ToDb(0f));
        }

        [Test]
        public void AnOctaveIsTwelveSemitones()
        {
            Assert.AreEqual(12f, VoiceMath.Semitones(440f, 220f), 0.001f);
            Assert.AreEqual(-12f, VoiceMath.Semitones(110f, 220f), 0.001f);
            Assert.AreEqual(0f, VoiceMath.Semitones(0f, 220f));
            Assert.AreEqual(440f, VoiceMath.ShiftSemitones(220f, 12f), 0.01f);
        }

        [Test]
        public void MedianHandlesOddEvenAndEmpty()
        {
            Assert.AreEqual(2f, VoiceMath.Median(new List<float> { 3f, 1f, 2f }));
            Assert.AreEqual(2.5f, VoiceMath.Median(new List<float> { 4f, 1f, 3f, 2f }));
            Assert.AreEqual(0f, VoiceMath.Median(new List<float>()));
        }
    }
}
