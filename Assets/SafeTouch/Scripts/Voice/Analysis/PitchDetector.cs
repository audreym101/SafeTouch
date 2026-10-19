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
            float confidence;
            return Detect(frame, offset, length, out confidence);
        }

        /// <summary>
        /// Returns the pitch in Hz (0 when unvoiced) and a confidence from 0
        /// to 1: how cleanly the frame repeats at that period.
        /// </summary>
        public float Detect(float[] frame, int offset, int length, out float confidence)
        {
            confidence = 0f;
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

            // Step 2, cumulative mean normalised difference. Without it the
            // raw minimum often lands on twice the period (an octave too low).
            diff[0] = 1f;
            double running = 0;
            for (int lag = 1; lag <= top; lag++)
            {
                running += diff[lag];
                diff[lag] = running > 0 ? (float)(diff[lag] * lag / running) : 1f;
            }

            // Step 3, absolute threshold: take the first dip below the
            // threshold and follow it down to its lowest point.
            for (int lag = minLag; lag < top; lag++)
            {
                if (diff[lag] >= Threshold) continue;
                while (lag + 1 < top && diff[lag + 1] < diff[lag]) lag++;
                confidence = Mathf.Clamp01(1f - diff[lag]);
                return sampleRate / Refine(lag, top);
            }
            return 0f;   // no clear period: breath, "sss", noise or silence
        }

        /// <summary>
        /// How periodic a frame must be to count as voiced (0 = perfectly
        /// periodic). 0.15 is the value recommended in the YIN paper.
        /// </summary>
        public float Threshold { get; set; } = 0.15f;

        /// <summary>
        /// Step 4, parabolic interpolation: fits a parabola through the dip
        /// and its neighbours so the period is not limited to whole samples.
        /// At 16 kHz that turns a 1-2% pitch error into well under 0.5%.
        /// </summary>
        float Refine(int lag, int top)
        {
            if (lag < 1 || lag >= top) return lag;
            float a = diff[lag - 1], b = diff[lag], c = diff[lag + 1];
            float denominator = a - 2f * b + c;
            if (Mathf.Abs(denominator) < 1e-6f) return lag;
            return lag + 0.5f * (a - c) / denominator;
        }
    }
}
