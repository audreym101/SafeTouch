using System;
using System.IO;
using System.Text;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Saves and reads the child's NO as a small WAV file: one channel, 16-bit,
    /// at the microphone's rate (16 kHz, about 32 KB a second). A plain format
    /// a parent's computer can also play if they copy it off the phone.
    /// </summary>
    public static class Wav
    {
        public static byte[] Encode(float[] samples, int sampleRate)
        {
            int dataBytes = samples.Length * 2;
            using (var stream = new MemoryStream(44 + dataBytes))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);                 // size of this chunk
                w.Write((short)1);           // PCM
                w.Write((short)1);           // mono
                w.Write(sampleRate);
                w.Write(sampleRate * 2);     // bytes per second
                w.Write((short)2);           // bytes per sample
                w.Write((short)16);          // bits per sample
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                foreach (var s in samples)
                    w.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, s)) * short.MaxValue));
                return stream.ToArray();
            }
        }

        /// <summary>Reads a file made by Encode. Null if it is not a 16-bit mono PCM WAV.</summary>
        public static float[] Decode(byte[] wav, out int sampleRate)
        {
            sampleRate = 0;
            if (wav == null || wav.Length < 44) return null;
            if (Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wav, 8, 4) != "WAVE") return null;

            int pos = 12;
            short channels = 0, bits = 0;
            while (pos + 8 <= wav.Length)
            {
                string id = Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                int body = pos + 8;
                if (size < 0 || body + size > wav.Length) size = wav.Length - body;
                if (id == "fmt " && size >= 16)
                {
                    if (BitConverter.ToInt16(wav, body) != 1) return null;   // not PCM
                    channels = BitConverter.ToInt16(wav, body + 2);
                    sampleRate = BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToInt16(wav, body + 14);
                }
                else if (id == "data")
                {
                    if (channels != 1 || bits != 16 || sampleRate <= 0) return null;
                    var samples = new float[size / 2];
                    for (int i = 0; i < samples.Length; i++)
                        samples[i] = BitConverter.ToInt16(wav, body + i * 2) / (float)short.MaxValue;
                    return samples;
                }
                pos = body + size + (size & 1);   // chunks are padded to an even length
            }
            return null;
        }
    }
}
