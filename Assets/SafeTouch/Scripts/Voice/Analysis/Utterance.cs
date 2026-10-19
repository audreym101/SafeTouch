using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// One thing the child said, from the first sound to the silence after
    /// it, as a list of frame measurements. Call Summarise once the last
    /// frame is in; the coach then reads the summary values.
    /// </summary>
    public class Utterance
    {
        /// <summary>A frame whose loudest sample reaches this is clipping (a shout into the phone).</summary>
        public const float ClipPeak = 0.98f;

        public readonly List<VoiceFrame> frames = new List<VoiceFrame>();
        public float noiseFloorDb;
        public float hopSeconds;
        public bool cutOff;   // still going when the maximum length was reached

        public float Duration { get; private set; }
        public float PeakDb { get; private set; }
        public float MeanDb { get; private set; }
        public float ClippedFraction { get; private set; }

        /// <summary>How far the voice stood out above the room, in dB.</summary>
        public float LoudnessAboveFloor { get { return MeanDb - noiseFloorDb; } }

        /// <summary>Share of frames with a clear pitch. Low for a whisper.</summary>
        public float VoicedFraction { get; private set; }

        /// <summary>Typical pitch of the voiced frames in Hz (0 if none).</summary>
        public float MedianPitch { get; private set; }

        /// <summary>Pitch at the start and the end: the median of the first and last third of voiced frames.</summary>
        public float StartPitch { get; private set; }
        public float EndPitch { get; private set; }

        /// <summary>
        /// Rise (+) or fall (-) from start to end, in semitones. A firm "No."
        /// is flat or falls; a rise of 2+ semitones sounds like a question ("No?").
        /// </summary>
        public float PitchSlopeSemitones { get { return VoiceMath.Semitones(EndPitch, StartPitch); } }

        /// <summary>Pitches of the voiced frames in order, for the pitch line on screen.</summary>
        public List<float> PitchContour()
        {
            var pitches = new List<float>();
            foreach (var f in frames) if (f.Voiced) pitches.Add(f.pitchHz);
            return pitches;
        }

        /// <summary>
        /// Rough syllable count from the loudness envelope: each rise of
        /// SyllableDipDb after a dip is a new beat. "No" is 1, "Oya" (no in
        /// Kinyarwanda) is 1-2, "No no no" or a sentence is 3 or more. It
        /// does not recognise words; it checks the answer was short.
        /// </summary>
        public int Syllables { get; private set; }

        public const float SyllableDipDb = 6f;

        void CountSyllables()
        {
            Syllables = 0;
            if (frames.Count == 0) return;
            float valley = noiseFloorDb, peak = 0f;
            bool inBeat = false;
            for (int i = 0; i < frames.Count; i++)
            {
                // A 3-frame moving average keeps one vowel from counting twice.
                float sum = 0f; int n = 0;
                for (int j = Mathf.Max(0, i - 1); j <= Mathf.Min(frames.Count - 1, i + 1); j++) { sum += frames[j].levelDb; n++; }
                float level = sum / n;

                if (!inBeat)
                {
                    valley = Mathf.Min(valley, level);
                    if (level - valley >= SyllableDipDb) { inBeat = true; peak = level; Syllables++; }
                }
                else
                {
                    peak = Mathf.Max(peak, level);
                    if (peak - level >= SyllableDipDb) { inBeat = false; valley = level; }
                }
            }
        }

        void SummarisePitch()
        {
            var pitches = PitchContour();
            VoicedFraction = frames.Count > 0 ? (float)pitches.Count / frames.Count : 0f;
            MedianPitch = VoiceMath.Median(pitches);
            if (pitches.Count == 0)
            {
                StartPitch = EndPitch = 0f;
                return;
            }
            int third = Mathf.Max(1, pitches.Count / 3);
            StartPitch = VoiceMath.Median(pitches.GetRange(0, third));
            EndPitch = VoiceMath.Median(pitches.GetRange(pitches.Count - third, third));
        }

        public void Summarise()
        {
            Duration = frames.Count * hopSeconds;
            PeakDb = VoiceMath.SilenceDb;
            double power = 0;
            int clipped = 0;
            foreach (var f in frames)
            {
                PeakDb = Mathf.Max(PeakDb, f.levelDb);
                power += Mathf.Pow(10f, f.levelDb / 10f);
                if (f.peak >= ClipPeak) clipped++;
            }
            MeanDb = frames.Count > 0 ? 10f * Mathf.Log10((float)(power / frames.Count)) : VoiceMath.SilenceDb;
            ClippedFraction = frames.Count > 0 ? (float)clipped / frames.Count : 0f;
            SummarisePitch();
            CountSyllables();
        }
    }
}
