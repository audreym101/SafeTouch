using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.Managers
{
    /// <summary>
    /// Plays narration (EN/RW voice clips recorded in week 6) and short sound
    /// effects. Until the recordings exist, the effects are generated tones so
    /// the prototype still gives audio feedback.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip[] voiceClips = new AudioClip[0];
        [SerializeField] AudioClip[] sfxClips = new AudioClip[0];

        readonly Dictionary<string, AudioClip> generated = new Dictionary<string, AudioClip>();

        public bool Muted { get; set; }

        void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            generated["tap"] = Tone("tap", new[] { 660f }, 0.06f);
            generated["correct"] = Tone("correct", new[] { 523f, 659f, 784f }, 0.12f);
            generated["tryagain"] = Tone("tryagain", new[] { 440f, 392f }, 0.16f);
            generated["badge"] = Tone("badge", new[] { 523f, 659f, 784f, 1047f }, 0.12f);
            generated["place"] = Tone("place", new[] { 392f, 523f }, 0.1f);
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
            if (Muted || clip == null) return;
            audioSource.PlayOneShot(clip);
        }

        /// <summary>Plays "tap", "correct", "tryagain", "badge" or "place".</summary>
        public void PlaySFX(string sfxName)
        {
            foreach (var clip in sfxClips)
                if (clip != null && clip.name == sfxName) { PlaySFX(clip); return; }
            AudioClip tone;
            if (generated.TryGetValue(sfxName, out tone)) PlaySFX(tone);
        }

        public void StopAll()
        {
            audioSource.Stop();
        }

        static AudioClip Tone(string clipName, float[] notes, float noteLength)
        {
            const int rate = 44100;
            int perNote = (int)(rate * noteLength);
            var data = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / rate;
                    float envelope = 1f - (float)i / perNote;    // soft decay, never harsh
                    data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * 0.25f * envelope;
                }
            }
            var clip = AudioClip.Create(clipName, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
