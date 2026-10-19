using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Finds the pitch (fundamental frequency) of one frame of microphone
    /// audio with the YIN method (de Cheveigne and Kawahara, 2002). Children
    /// aged 6 to 11 speak at roughly 200-400 Hz and shout up to about 800 Hz,
    /// so the default search range is 75-1000 Hz.
    /// </summary>
    public class PitchDetector
    {
        public const float DefaultMinHz = 75f;
        public const float DefaultMaxHz = 1000f;

        readonly int sampleRate;
        readonly int minLag, maxLag;
        readonly float[] diff;

        public PitchDetector(int sampleRate, float minHz = DefaultMinHz, float maxHz = DefaultMaxHz)
        {
            this.sampleRate = sampleRate;
            minLag = Mathf.Max(2, Mathf.FloorToInt(sampleRate / maxHz));
            maxLag = Mathf.CeilToInt(sampleRate / minHz);
            diff = new float[maxLag + 2];
        }

        public int SampleRate { get { return sampleRate; } }

        /// <summary>Samples a frame needs: the longest period searched plus a window at least as long.</summary>
        public int MinFrameLength { get { return 2 * (maxLag + 1); } }

        /// <summary>Returns the pitch in Hz, or 0 when the frame has no clear period.</summary>
        public float Detect(float[] frame, int offset, int length)
        {
            int top = Mathf.Min(maxLag + 1, length / 2);   // the window must be at least as long as the lag
            if (top <= minLag) return 0f;
            int window = length - top;

            // Step 1, difference function: how unlike the frame is to itself shifted by each lag.
            for (int lag = 1; lag <= top; lag++)
            {
                double sum = 0;
                for (int i = 0; i < window; i++)
                {
                    float d = frame[offset + i] - frame[offset + i + lag];
                    sum += d * d;
                }
                diff[lag] = (float)sum;
            }

            int best = minLag;
            for (int lag = minLag + 1; lag < top; lag++)
                if (diff[lag] < diff[best]) best = lag;
            return diff[best] > 0f ? (float)sampleRate / best : 0f;
        }
    }
}
