using System;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Listens for the child's spoken "NO" during a lesson and rates it with
    /// NoCoach. The microphone is open only between ListenForNo and the
    /// result. Only numbers (level, pitch, length) are kept, and only until
    /// the result is shown.
    /// </summary>
    public class VoiceCoach : MonoBehaviour
    {
        [SerializeField] MicrophoneInput microphone;
        [Tooltip("Give up on a try after this many seconds of silence, so the lesson can offer help.")]
        [SerializeField] float noAnswerSeconds = 7f;

        float listenStart;
        FrameAnalyzer analyzer;
        UtteranceSegmenter segmenter;
        NoCoachSettings settings = new NoCoachSettings();
        Action<NoRating> onNo;
        Action<bool> onTalk;
        bool permitted;
        bool continuous;

        public NoCoachSettings Settings { get { return settings; } }
        public bool Listening { get; private set; }

        /// <summary>Set from the parent section; off means every lesson uses tap buttons.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>True when a lesson can ask the child to speak (switched on, and the phone has a mic).</summary>
        public bool Available { get { return Enabled && MicrophoneInput.HasDevice; } }

        /// <summary>Loudness for the on-screen meter: 0 at the room's level, 1 at a full-marks voice and beyond.</summary>
        public float Level01 { get; private set; }

        /// <summary>Where a strong voice starts on the meter (the target line).</summary>
        public float StrongLevel01 { get { return settings.strongLoudnessDb / MeterRangeDb; } }

        /// <summary>The pitch heard in the latest frame, 0 when none.</summary>
        public float PitchHz { get; private set; }

        /// <summary>False for the first moments while the room's noise is measured ("get ready").</summary>
        public bool Ready { get { return Listening && segmenter != null && segmenter.Settled; } }

        /// <summary>True while the child is speaking.</summary>
        public bool Hearing { get { return Listening && segmenter != null && segmenter.InUtterance; } }

        /// <summary>Raised for every analysed frame while listening, for the pitch line.</summary>
        public event Action<VoiceFrame> FrameHeard;

        const float MeterRangeDb = 30f;

        void Awake()
        {
            if (microphone == null) microphone = GetComponent<MicrophoneInput>();
            if (microphone == null) microphone = gameObject.AddComponent<MicrophoneInput>();
            microphone.SamplesReady += OnSamples;
        }

        /// <summary>Uses the typical talking pitch for the age group until the child is heard talking.</summary>
        public void PrepareForAge(string ageGroup)
        {
            if (settings.calibrated) return;
            settings = NoCoachSettings.ForAgeGroup(ageGroup);
        }

        /// <summary>Listens for one try. done gets the rating, or NothingHeard if the mic can't be used.</summary>
        public void ListenForNo(Action<NoRating> done)
        {
            onNo = done;
            onTalk = null;
            continuous = false;
            Begin();
        }

        /// <summary>
        /// Keeps the microphone open and rates every word until Stop, for the
        /// Bubble Guard game. There is no silence timeout in this mode.
        /// </summary>
        public void ListenContinuously(Action<NoRating> eachTry)
        {
            onNo = eachTry;
            onTalk = null;
            continuous = true;
            Begin();
        }

        /// <summary>
        /// Listens to the child talking normally ("Hello Keza!") to learn
        /// their everyday pitch, so a high voice is not mistaken for a squeak.
        /// done gets true if a voiced answer was heard. Kept for this session only.
        /// </summary>
        public void ListenToTalkingVoice(Action<bool> done)
        {
            onTalk = done;
            onNo = null;
            Begin();
        }

        /// <summary>Stops listening and closes the microphone. Safe to call at any time.</summary>
        public void Stop()
        {
            Listening = false;
            onNo = null;
            onTalk = null;
            Level01 = 0f;
            PitchHz = 0f;
            microphone.StopCapture();
        }

        void Begin()
        {
            if (!Available) { Deliver(NoRating.NothingHeard()); return; }
            if (!permitted)
            {
                MicrophoneInput.RequestPermission(granted =>
                {
                    permitted = granted;
                    if (!granted) { Enabled = false; Deliver(NoRating.NothingHeard()); }
                    else Begin();
                });
                return;
            }
            if (!microphone.StartCapture()) { Deliver(NoRating.NothingHeard()); return; }

            if (analyzer == null || analyzer.SampleRate != microphone.SampleRate)
            {
                analyzer = new FrameAnalyzer(microphone.SampleRate);
                analyzer.FrameReady += OnFrame;
                segmenter = new UtteranceSegmenter(analyzer.HopSeconds);
                segmenter.UtteranceFinished += OnUtterance;
            }
            analyzer.Reset();
            segmenter.Reset();
            listenStart = Time.unscaledTime;
            Listening = true;
        }

        void Update()
        {
            // Silence for too long: hand back "nothing heard" so the lesson can encourage the child.
            if (Listening && !continuous && !segmenter.InUtterance && Time.unscaledTime - listenStart > noAnswerSeconds)
                Deliver(NoRating.NothingHeard());
        }

        void OnSamples(float[] samples, int count)
        {
            if (Listening) analyzer.Push(samples, 0, count);
        }

        void OnFrame(VoiceFrame frame)
        {
            if (!Listening) return;
            float target = Mathf.Clamp01((frame.levelDb - segmenter.NoiseFloorDb) / MeterRangeDb);
            Level01 = target > Level01 ? target : Mathf.Lerp(Level01, target, 0.25f);   // rise fast, fall gently
            PitchHz = frame.Voiced ? frame.pitchHz : 0f;
            segmenter.Add(frame);
            if (FrameHeard != null) FrameHeard(frame);
        }

        void OnUtterance(Utterance utterance)
        {
            if (!Listening) return;
            if (onTalk != null)
            {
                bool voiced = utterance.MedianPitch > 0f && utterance.VoicedFraction >= settings.minVoicedFraction;
                if (voiced) settings.SetTalkingPitch(utterance.MedianPitch);
                var talked = onTalk;
                Stop();
                talked(voiced);
                return;
            }
            var rating = NoCoach.Rate(utterance, settings);
            if (continuous)
            {
                // A scrape, a rustle or a bump is not a word: with no voice in it, keep listening for the real NO.
                if (utterance.VoicedFraction < settings.minVoicedFraction) return;
                if (onNo != null) onNo(rating);   // stay open for the next word
                return;
            }
            Deliver(rating);
        }

        void Deliver(NoRating rating)
        {
            var done = onNo;
            var talked = onTalk;
            Stop();
            if (done != null) done(rating);
            if (talked != null) talked(false);
        }
    }
}
