using System;
using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Finds where a spoken word starts and ends in the stream of frames.
    /// It follows the background noise of the room (a quiet bedroom or a busy
    /// classroom) so a "NO" is judged against the room, not against a fixed
    /// number that only works on one phone.
    /// </summary>
    public class UtteranceSegmenter
    {
        public const float DefaultFloorDb = -60f;
        const float LowestFloorDb = -90f;
        const float HighestFloorDb = -20f;
        const int OnsetFrames = 2;   // two loud frames in a row, so a tap on the table doesn't start a word

        Utterance current;
        int quietFrames;
        readonly List<VoiceFrame> onset = new List<VoiceFrame>();

        /// <summary>A word starts this far above the noise floor (dB).</summary>
        public float OnsetMarginDb { get; set; } = 12f;

        /// <summary>A word ends after the level stays within this of the floor (dB)...</summary>
        public float ReleaseMarginDb { get; set; } = 6f;

        /// <summary>...for this long. Long enough for the gap inside "o-ya".</summary>
        public float HangoverSeconds { get; set; } = 0.25f;

        /// <summary>Anything shorter is a click or a bump, not a word.</summary>
        public float MinSeconds { get; set; } = 0.12f;

        /// <summary>Stop listening to one answer after this long.</summary>
        public float MaxSeconds { get; set; } = 2.5f;

        /// <summary>Very quiet phones still need a real sound, not hiss, to start a word.</summary>
        public float MinOnsetDb { get; set; } = -55f;

        public bool InUtterance { get { return current != null; } }

        /// <summary>Raised with a summarised utterance when the child stops speaking.</summary>
        public event Action<Utterance> UtteranceFinished;

        public UtteranceSegmenter(float hopSeconds)
        {
            HopSeconds = hopSeconds;
            Reset();
        }

        public float HopSeconds { get; private set; }

        /// <summary>The current estimate of the room's background level, in dBFS.</summary>
        public float NoiseFloorDb { get; private set; }

        /// <summary>
        /// Frames heard while settling. During the first few frames the floor
        /// adapts quickly in both directions; the lesson shows a short
        /// "get ready" countdown while this happens.
        /// </summary>
        public int SettleFrames { get; private set; }

        public bool Settled { get { return SettleFrames * HopSeconds >= SettleSeconds; } }

        /// <summary>How long to listen to the room before a word can start.</summary>
        public float SettleSeconds { get; set; } = 0.3f;

        public void Reset()
        {
            NoiseFloorDb = DefaultFloorDb;
            SettleFrames = 0;
            current = null;
            quietFrames = 0;
            onset.Clear();
        }

        /// <summary>Feeds the next frame from the analyser.</summary>
        public void Add(VoiceFrame frame)
        {
            if (current == null)
            {
                bool loud = Settled && frame.levelDb >= Mathf.Max(NoiseFloorDb + OnsetMarginDb, MinOnsetDb);
                if (!loud)
                {
                    onset.Clear();
                    TrackFloor(frame.levelDb);
                    return;
                }
                onset.Add(frame);
                if (onset.Count < OnsetFrames) return;
                current = new Utterance { noiseFloorDb = NoiseFloorDb, hopSeconds = HopSeconds };
                current.frames.AddRange(onset);
                onset.Clear();
                quietFrames = 0;
                return;
            }

            current.frames.Add(frame);
            quietFrames = frame.levelDb < NoiseFloorDb + ReleaseMarginDb ? quietFrames + 1 : 0;
            if (quietFrames * HopSeconds >= HangoverSeconds) Finish(quietFrames, false);
            else if (current.frames.Count * HopSeconds >= MaxSeconds) Finish(0, true);
        }

        void Finish(int trailingQuiet, bool cutOff)
        {
            var done = current;
            current = null;
            quietFrames = 0;
            done.frames.RemoveRange(done.frames.Count - trailingQuiet, trailingQuiet);
            done.cutOff = cutOff;
            if (done.frames.Count * HopSeconds < MinSeconds) return;
            done.Summarise();
            if (UtteranceFinished != null) UtteranceFinished(done);
        }

        /// <summary>Updates the floor from a frame that is not part of an utterance.</summary>
        void TrackFloor(float levelDb)
        {
            float rate;
            if (!Settled) rate = SettleFrames == 0 ? 1f : 0.35f;   // learn the room quickly at first
            else if (levelDb < NoiseFloorDb) rate = 0.3f;           // the room got quieter: follow fast
            else rate = 0.03f;                                      // a little louder: follow slowly
            NoiseFloorDb = Mathf.Clamp(Mathf.Lerp(NoiseFloorDb, levelDb, rate), LowestFloorDb, HighestFloorDb);
            SettleFrames++;
        }
    }
}
