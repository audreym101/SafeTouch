namespace SafeTouch.Voice
{
    /// <summary>
    /// What the analyser measured in one short slice (about 64 ms) of
    /// microphone audio. Only these numbers are kept, never the sound itself.
    /// </summary>
    public struct VoiceFrame
    {
        /// <summary>Confidence below this means the pitch is a guess, so the frame counts as unvoiced.</summary>
        public const float MinConfidence = 0.5f;

        public float time;        // seconds since listening started
        public float levelDb;     // loudness in dBFS
        public float pitchHz;     // 0 when no clear pitch (silence, breath, "sss")
        public float confidence;  // 0-1, from the pitch detector
        public float peak;        // largest sample size, 1 = the microphone is clipping

        public bool Voiced { get { return pitchHz > 0f && confidence >= MinConfidence; } }
    }
}
