using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Managers
{
    /// <summary>
    /// Plays narration (EN/RW voice clips recorded in week 6), short sound effects and the background music.
    /// Until the recordings exist, the effects and the music are made in code: a bouncy marimba tune over
    /// ukulele-like chords, a soft beat and a shaker, so there is no song to license and nothing to download.
    /// The music goes quiet while the microphone listens (so the voice coach hears the child, not the tune)
    /// and while a voice clip plays. Music and sounds can each be switched off in Settings; the choice is kept.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip[] voiceClips = new AudioClip[0];
        [SerializeField] AudioClip[] sfxClips = new AudioClip[0];
        [SerializeField] AudioClip musicClip;   // a recorded song can replace the generated one
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.22f;

        const string MusicKey = "safetouch.music", SoundsKey = "safetouch.sounds";
        const int Rate = 22050;

        readonly Dictionary<string, AudioClip> generated = new Dictionary<string, AudioClip>();
        AudioSource musicSource;
        Task<float[]> composing;

        public bool Muted { get; set; }

        /// <summary>Background music on or off (kept on the phone).</summary>
        public bool MusicOn
        {
            get { return PlayerPrefs.GetInt(MusicKey, 1) == 1; }
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Button pops and the other sound effects on or off (kept on the phone).</summary>
        public bool SoundsOn
        {
            get { return PlayerPrefs.GetInt(SoundsKey, 1) == 1; }
            set { PlayerPrefs.SetInt(SoundsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            generated["pop"] = Clip("pop", Pop());
            generated["tap"] = generated["pop"];
            generated["correct"] = Clip("correct", Notes(new[] { 72, 76, 79, 84 }, 0.09f, 0.3f));
            generated["tryagain"] = Clip("tryagain", Notes(new[] { 67, 64 }, 0.16f, 0.28f));
            generated["badge"] = Clip("badge", Notes(new[] { 72, 76, 79, 84, 88, 91 }, 0.08f, 0.3f));
            generated["place"] = Clip("place", Notes(new[] { 67, 72 }, 0.1f, 0.3f));
            UIFactory.ButtonPressed = () => PlaySFX("pop");

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0f;
            if (musicClip != null) StartMusic(musicClip);
            else composing = Task.Run(() => Song());   // about a second of work, off the main thread
        }

        void StartMusic(AudioClip clip)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }

        void Update()
        {
            if (composing != null && composing.IsCompleted)
            {
                if (composing.Status == TaskStatus.RanToCompletion) StartMusic(Clip("SafeTouch tune", composing.Result));
                composing = null;
            }
            if (musicSource == null || musicSource.clip == null) return;
            // Quiet while the microphone listens; softer while a voice clip plays.
            float target = !MusicOn || Muted || Microphone.IsRecording(null) ? 0f
                         : audioSource.isPlaying && audioSource.clip != null ? musicVolume * 0.35f
                         : musicVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * 0.6f);
        }

        public void PlayVoice(AudioClip clip)
        {
            if (Muted || clip == null) return;
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }

        /// <summary>Plays a narration clip by name, e.g. "bubble_intro_en".</summary>
        public void PlayVoice(string clipName)
        {
            foreach (var clip in voiceClips)
                if (clip != null && clip.name == clipName) { PlayVoice(clip); return; }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (Muted || !SoundsOn || clip == null) return;
            audioSource.PlayOneShot(clip);
        }

        /// <summary>Plays "pop", "tap", "correct", "tryagain", "badge" or "place".</summary>
        public void PlaySFX(string sfxName)
        {
            foreach (var clip in sfxClips)
                if (clip != null && clip.name == sfxName) { PlaySFX(clip); return; }
            AudioClip tone;
            if (generated.TryGetValue(sfxName, out tone)) PlaySFX(tone);
        }

        /// <summary>Stops narration and effects. The music keeps playing.</summary>
        public void StopAll()
        {
            audioSource.Stop();
        }

        static AudioClip Clip(string clipName, float[] data)
        {
            var clip = AudioClip.Create(clipName, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Hz(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        /// <summary>A bubble pop: a quick rising "bloop", short and soft.</summary>
        static float[] Pop()
        {
            var data = new float[(int)(Rate * 0.09f)];
            double phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float f = Mathf.Lerp(380f, 1250f, Mathf.Clamp01(t / 0.05f));
                phase += 2 * Math.PI * f / Rate;
                float env = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / 0.022f);
                data[i] = (float)Math.Sin(phase) * env * 0.4f;
            }
            return data;
        }

        /// <summary>Marimba notes one after the other, each ringing on: for "correct", "badge" and friends.</summary>
        static float[] Notes(int[] midi, float step, float ring)
        {
            var data = new float[(int)(Rate * (step * midi.Length + ring))];
            for (int n = 0; n < midi.Length; n++) Marimba(data, (int)(Rate * step * n), Hz(midi[n]), 0.3f, ring, false);
            return data;
        }

        // ---- The background tune: 8 bars at 112 beats a minute, made to loop without a seam ----

        static readonly int[][] Chords =
        {
            new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },   // C  Am F  G
            new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
        };

        // The tune in eighth notes, 8 to a bar; -1 is a rest.
        static readonly int[] Melody =
        {
            72, -1, 76, -1, 79, 76, 72, -1,
            76, -1, 72, -1, 69, 72, 76, -1,
            77, -1, 76, 74, 72, -1, 69, -1,
            74, 76, 74, 71, 67, -1, 71, -1,
            72, -1, 76, -1, 79, -1, 81, 79,
            76, -1, 72, -1, 76, 79, 76, -1,
            77, 77, 76, -1, 74, 72, 74, -1,
            71, -1, 74, -1, 79, -1, -1, -1,
        };

        static float[] Song()
        {
            const float beat = 60f / 112f, eighth = beat / 2f;
            var song = new float[(int)(Rate * beat * 4 * Chords.Length)];
            var noise = new System.Random(7);
            for (int bar = 0; bar < Chords.Length; bar++)
            {
                var chord = Chords[bar];
                for (int e = 0; e < 8; e++)
                {
                    int at = (int)(Rate * eighth * (bar * 8 + e));
                    int note = Melody[bar * 8 + e];
                    if (note >= 0) Marimba(song, at, Hz(note), 0.2f, 0.7f, true);
                    if (e == 0 || e == 4) { Bass(song, at, Hz(chord[0] - 24)); Kick(song, at); }
                    if (e == 7) Bass(song, at, Hz(chord[2] - 24));
                    if (e == 2 || e == 6) Clap(song, at, noise);
                    if (e % 2 == 1) foreach (var c in chord) Pluck(song, at, Hz(c), 0.05f);   // ukulele on the off-beats
                    Shaker(song, at, noise, e % 2 == 1 ? 0.035f : 0.018f);
                }
            }
            // Gentle limiter, so nothing ever clips.
            float peak = 0f;
            foreach (var s in song) peak = Math.Max(peak, Math.Abs(s));
            float gain = peak > 0f ? 0.9f / peak : 1f;
            for (int i = 0; i < song.Length; i++) song[i] = (float)Math.Tanh(song[i] * gain * 1.2f) * 0.85f;
            return song;
        }

        /// <summary>Adds a sound into the buffer; with wrap, the tail past the end continues at the start (a seamless loop).</summary>
        static void Add(float[] buffer, int at, int i, float value, bool wrap)
        {
            int k = at + i;
            if (k >= buffer.Length) { if (!wrap) return; k %= buffer.Length; }
            buffer[k] += value;
        }

        static void Marimba(float[] buffer, int at, float f, float amp, float length, bool wrap)
        {
            int n = (int)(Rate * length);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t / 0.16f);
                float v = Mathf.Sin(2f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * f * 4f * t) * Mathf.Exp(-t / 0.03f);
                Add(buffer, at, i, v * env * amp, wrap);
            }
        }

        static void Pluck(float[] buffer, int at, float f, float amp)
        {
            int n = (int)(Rate * 0.3f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t / 0.09f);
                float w = 2f * Mathf.PI * f * t;
                Add(buffer, at, i, (Mathf.Sin(w) + 0.35f * Mathf.Sin(2f * w) + 0.15f * Mathf.Sin(3f * w)) * env * amp, true);
            }
        }

        static void Bass(float[] buffer, int at, float f)
        {
            int n = (int)(Rate * 0.45f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t / 0.22f);
                float w = 2f * Mathf.PI * f * t;
                Add(buffer, at, i, (Mathf.Sin(w) + 0.3f * Mathf.Sin(2f * w)) * env * 0.26f, true);
            }
        }

        static void Kick(float[] buffer, int at)
        {
            int n = (int)(Rate * 0.18f);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += 2 * Math.PI * Mathf.Lerp(130f, 48f, t / 0.18f) / Rate;
                Add(buffer, at, i, (float)Math.Sin(phase) * Mathf.Exp(-t / 0.06f) * 0.32f, true);
            }
        }

        static void Clap(float[] buffer, int at, System.Random noise)
        {
            int n = (int)(Rate * 0.12f);
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                low += ((float)noise.NextDouble() * 2f - 1f - low) * 0.35f;   // soften the hiss
                Add(buffer, at, i, low * Mathf.Exp(-t / 0.045f) * 0.13f, true);
            }
        }

        static void Shaker(float[] buffer, int at, System.Random noise, float amp)
        {
            int n = (int)(Rate * 0.05f);
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float white = (float)noise.NextDouble() * 2f - 1f;
                Add(buffer, at, i, (white - last) * Mathf.Exp(-t / 0.015f) * amp, true);   // only the bright part
                last = white;
            }
        }
    }
}
