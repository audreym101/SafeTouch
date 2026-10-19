using System;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Cuts a stream of microphone samples into overlapping frames and
    /// measures each one (level, pitch, peak). The samples are overwritten as
    /// soon as a frame is measured: SafeTouch never keeps or saves audio.
    /// </summary>
    public class FrameAnalyzer
    {
        const float TargetFrameSeconds = 0.064f;

        readonly PitchDetector pitch;
        readonly float[] frame;
        readonly int hop;
        int filled;
        long framesDone;

        /// <summary>Raised once per hop with the measurements for the newest frame.</summary>
        public event Action<VoiceFrame> FrameReady;

        public FrameAnalyzer(int sampleRate)
        {
            pitch = new PitchDetector(sampleRate);
            int length = Mathf.NextPowerOfTwo(Mathf.Max(pitch.MinFrameLength, (int)(sampleRate * TargetFrameSeconds)));
            frame = new float[length];
            hop = length / 2;
        }

        public int SampleRate { get { return pitch.SampleRate; } }
        public int FrameLength { get { return frame.Length; } }

        /// <summary>Time between frames in seconds (half a frame, about 32 ms).</summary>
        public float HopSeconds { get { return (float)hop / pitch.SampleRate; } }

        /// <summary>Adds new samples; measures a frame every time a hop's worth has arrived.</summary>
        public void Push(float[] samples, int offset, int count)
        {
            while (count > 0)
            {
                int take = Mathf.Min(count, frame.Length - filled);
                Array.Copy(samples, offset, frame, filled, take);
                filled += take;
                offset += take;
                count -= take;
                if (filled < frame.Length) return;

                Measure();
                Array.Copy(frame, hop, frame, 0, frame.Length - hop);   // keep the overlap
                filled = frame.Length - hop;
            }
        }

        /// <summary>Forgets buffered samples, e.g. when listening stops.</summary>
        public void Reset()
        {
            Array.Clear(frame, 0, frame.Length);
            filled = 0;
            framesDone = 0;
        }

        void Measure()
        {
            float peak = 0f;
            for (int i = 0; i < frame.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(frame[i]));
            float level = VoiceMath.ToDb(VoiceMath.Rms(frame, 0, frame.Length));

            float confidence = 0f;
            float hz = level > VoiceMath.SilenceDb + 30f ? pitch.Detect(frame, 0, frame.Length, out confidence) : 0f;

            var result = new VoiceFrame
            {
                time = framesDone * HopSeconds,
                levelDb = level,
                pitchHz = hz,
                confidence = confidence,
                peak = peak
            };
            framesDone++;
            if (FrameReady != null) FrameReady(result);
        }
    }
}
