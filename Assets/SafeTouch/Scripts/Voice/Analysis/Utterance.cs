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
        }
    }
}
