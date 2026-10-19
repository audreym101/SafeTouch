using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Small signal helpers shared by the pitch detector and the NO coach.
    /// Levels are in dBFS: 0 is the loudest the microphone can record and
    /// quiet rooms sit around -60.
    /// </summary>
    public static class VoiceMath
    {
        /// <summary>Quietest level we report, so silence is a number rather than -infinity.</summary>
        public const float SilenceDb = -100f;

        /// <summary>Root mean square of count samples starting at offset.</summary>
        public static float Rms(float[] samples, int offset, int count)
        {
            if (count <= 0) return 0f;
            double sum = 0;
            for (int i = offset; i < offset + count; i++) sum += samples[i] * samples[i];
            return (float)System.Math.Sqrt(sum / count);
        }

        /// <summary>Converts an amplitude (0 to 1) to decibels relative to full scale.</summary>
        public static float ToDb(float amplitude)
        {
            if (amplitude <= 0.00001f) return SilenceDb;
            return Mathf.Max(SilenceDb, 20f * Mathf.Log10(amplitude));
        }

        /// <summary>
        /// Distance between two pitches in semitones (12 per octave). Positive
        /// when hz is higher than reference. Children hear a 2-semitone change
        /// as "going up", which is why the coach works in semitones, not hertz.
        /// </summary>
        public static float Semitones(float hz, float referenceHz)
        {
            if (hz <= 0f || referenceHz <= 0f) return 0f;
            return 12f * Mathf.Log(hz / referenceHz, 2f);
        }

        /// <summary>The pitch a number of semitones away from referenceHz.</summary>
        public static float ShiftSemitones(float referenceHz, float semitones)
        {
            return referenceHz * Mathf.Pow(2f, semitones / 12f);
        }
    }
}
