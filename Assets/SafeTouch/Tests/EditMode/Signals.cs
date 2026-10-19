using System;
using System.Collections.Generic;
using SafeTouch.Voice;
using UnityEngine;

namespace SafeTouch.Tests
{
    /// <summary>
    /// Builds test audio in memory (tones, glides, noise) so the voice code
    /// can be checked without a microphone or recordings of children.
    /// </summary>
    public static class Signals
    {
        public const int Rate = 16000;

        /// <summary>A voice-like tone: a fundamental gliding from startHz to endHz with two softer harmonics.</summary>
        public static float[] Voice(float startHz, float endHz, float seconds, float amplitude, int rate = Rate)
        {
            var data = new float[(int)(seconds * rate)];
            double phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float hz = Mathf.Lerp(startHz, endHz, (float)i / data.Length);
                phase += 2.0 * Math.PI * hz / rate;
                data[i] = amplitude * (float)(0.7 * Math.Sin(phase) + 0.2 * Math.Sin(2 * phase) + 0.1 * Math.Sin(3 * phase));
            }
            return data;
        }

        public static float[] Sine(float hz, float seconds, float amplitude, int rate = Rate)
        {
            var data = new float[(int)(seconds * rate)];
            for (int i = 0; i < data.Length; i++) data[i] = amplitude * Mathf.Sin(2f * Mathf.PI * hz * i / rate);
            return data;
        }

        /// <summary>Uniform noise: the room's background, or a whisper when loud.</summary>
        public static float[] Noise(float seconds, float amplitude, int seed, int rate = Rate)
        {
            var random = new System.Random(seed);
            var data = new float[(int)(seconds * rate)];
            for (int i = 0; i < data.Length; i++) data[i] = amplitude * (float)(random.NextDouble() * 2.0 - 1.0);
            return data;
        }

        public static float[] Silence(float seconds, int rate = Rate)
        {
            return new float[(int)(seconds * rate)];
        }

        /// <summary>Joins clips end to end.</summary>
        public static float[] Join(params float[][] parts)
        {
            var all = new List<float>();
            foreach (var p in parts) all.AddRange(p);
            return all.ToArray();
        }

        /// <summary>Adds quiet room noise under a clip, as a real microphone would hear it.</summary>
        public static float[] WithRoom(float[] clip, float noiseAmplitude = 0.002f, int seed = 7)
        {
            // Noise is generated per sample: going through seconds can round one sample short.
            var random = new System.Random(seed);
            var mixed = new float[clip.Length];
            for (int i = 0; i < clip.Length; i++)
                mixed[i] = Mathf.Clamp(clip[i] + noiseAmplitude * (float)(random.NextDouble() * 2.0 - 1.0), -1f, 1f);
            return mixed;
        }

        /// <summary>Hard clipping at full scale, like a shout too close to the phone.</summary>
        public static float[] Clip(float[] clip)
        {
            var c = new float[clip.Length];
            for (int i = 0; i < clip.Length; i++) c[i] = Mathf.Clamp(clip[i], -1f, 1f);
            return c;
        }

        /// <summary>Runs audio through the analyser and segmenter, in phone-sized chunks, and returns every utterance found.</summary>
        public static List<Utterance> Segment(float[] audio, int rate = Rate)
        {
            var analyzer = new FrameAnalyzer(rate);
            var segmenter = new UtteranceSegmenter(analyzer.HopSeconds);
            var found = new List<Utterance>();
            analyzer.FrameReady += segmenter.Add;
            segmenter.UtteranceFinished += found.Add;
            const int chunk = 700;   // not a multiple of the hop, like Microphone reads
            for (int i = 0; i < audio.Length; i += chunk)
                analyzer.Push(audio, i, Mathf.Min(chunk, audio.Length - i));
            return found;
        }

        /// <summary>Half a second of room, the clip, then a second of room: one "try" in a lesson.</summary>
        public static float[] Try(float[] clip)
        {
            return WithRoom(Join(Silence(0.5f), clip, Silence(1f)));
        }
    }
}
