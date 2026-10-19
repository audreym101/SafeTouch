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
        bool permitted;

        public NoCoachSettings Settings { get { return settings; } }
        public bool Listening { get; private set; }

        /// <summary>Set from the parent section; off means every lesson uses tap buttons.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>True when a lesson can ask the child to speak (switched on, and the phone has a mic).</summary>
        public bool Available { get { return Enabled && MicrophoneInput.HasDevice; } }

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
            Begin();
        }

        /// <summary>Stops listening and closes the microphone. Safe to call at any time.</summary>
        public void Stop()
        {
            Listening = false;
            onNo = null;
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
            if (Listening && !segmenter.InUtterance && Time.unscaledTime - listenStart > noAnswerSeconds)
                Deliver(NoRating.NothingHeard());
        }

        void OnSamples(float[] samples, int count)
        {
            if (Listening) analyzer.Push(samples, 0, count);
        }

        void OnFrame(VoiceFrame frame)
        {
            if (Listening) segmenter.Add(frame);
        }

        void OnUtterance(Utterance utterance)
        {
            if (!Listening) return;
            Deliver(NoCoach.Rate(utterance, settings));
        }

        void Deliver(NoRating rating)
        {
            var done = onNo;
            Stop();
            if (done != null) done(rating);
        }
    }
}
