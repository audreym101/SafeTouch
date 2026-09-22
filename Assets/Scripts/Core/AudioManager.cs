using UnityEngine;

namespace SafeTouch.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource narrationSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField, Range(0f, 1f)] private float narrationDuckLevel = 0.3f;

        private float sfxDefaultVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (narrationSource == null)
            {
                narrationSource = gameObject.AddComponent<AudioSource>();
            }
            if (sfxSource == null)
            {
                var sfxGo = new GameObject("SFXSource");
                sfxGo.transform.SetParent(transform);
                sfxSource = sfxGo.AddComponent<AudioSource>();
            }

            narrationSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            sfxDefaultVolume = sfxSource.volume;
        }

        public void PlayNarration(AudioClip clip)
        {
            if (clip == null) return;
            narrationSource.clip = clip;
            narrationSource.Play();
            sfxSource.volume = sfxDefaultVolume * narrationDuckLevel;
        }

        public void StopNarration()
        {
            narrationSource.Stop();
            sfxSource.volume = sfxDefaultVolume;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        private void Update()
        {
            if (!narrationSource.isPlaying && sfxSource.volume < sfxDefaultVolume)
            {
                sfxSource.volume = sfxDefaultVolume;
            }
        }
    }
}
