using System;

namespace SafeTouch.Voice
{
    /// <summary>
    /// The last few seconds of microphone samples, overwritten as new ones
    /// arrive. When the coach hears a NO, the word is cut from here, so only
    /// the NO itself (and a breath either side) is ever kept, never the
    /// talking around it.
    /// </summary>
    public class SampleRing
    {
        readonly float[] buffer;
        int write;   // where the next sample goes

        public SampleRing(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException("capacity");
            buffer = new float[capacity];
        }

        public int Capacity { get { return buffer.Length; } }

        /// <summary>How many samples are held, up to Capacity.</summary>
        public int Count { get; private set; }

        public void Push(float[] samples, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[write] = samples[offset + i];
                write = (write + 1) % buffer.Length;
            }
            Count = Math.Min(Count + count, buffer.Length);
        }

        /// <summary>The newest count samples, oldest first (fewer if fewer are held).</summary>
        public float[] Last(int count)
        {
            count = Math.Max(0, Math.Min(count, Count));
            var result = new float[count];
            int start = (write - count + buffer.Length) % buffer.Length;
            for (int i = 0; i < count; i++) result[i] = buffer[(start + i) % buffer.Length];
            return result;
        }

        public void Clear()
        {
            Count = 0;
            write = 0;
        }

        /// <summary>Fades the first and last few milliseconds to silence, so a clip starts and ends without a click.</summary>
        public static void FadeEdges(float[] samples, int fadeSamples)
        {
            int n = Math.Min(fadeSamples, samples.Length / 2);
            for (int i = 0; i < n; i++)
            {
                float gain = (float)i / n;
                samples[i] *= gain;
                samples[samples.Length - 1 - i] *= gain;
            }
        }
    }
}
