using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Follows the background noise of the room (a quiet bedroom or a busy
    /// classroom) so a "NO" is judged against the room, not against a fixed
    /// number that only works on one phone.
    /// </summary>
    public class UtteranceSegmenter
    {
        public const float DefaultFloorDb = -60f;
        const float LowestFloorDb = -90f;
        const float HighestFloorDb = -20f;

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
