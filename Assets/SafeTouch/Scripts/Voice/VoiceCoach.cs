using System;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Listens for the child's spoken "NO" during a lesson (or on the My NO
    /// practice screen) and rates it with NoCoach. The microphone is open only
    /// between ListenForNo and the result.
    ///
    /// The last few seconds of sound sit in a ring that is overwritten as it
    /// goes. When a spoken NO is heard, that one word is cut from the ring and
    /// handed on (NoCaptured), so it can be kept on this phone for the child
    /// and their grown-up to hear (consent terms 1.6). Talking before or after
    /// the NO, whispers, and the "Hello Keza!" warm-up are never kept.
    ///
    /// Before Android's own microphone question, the app says in plain words
    /// why it wants the microphone and what it keeps (ExplainMicrophone).
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
        bool declined;     // "Not now" on the microphone card: NO buttons for the rest of this visit
        int attempt;       // changes on every Begin and Stop, so a late answer to a question is ignored
        SampleRing ring;

        /// <summary>Seconds of sound the ring holds: the longest NO (2.5 s) and the quiet after it.</summary>
        const float RingSeconds = 4f;
        /// <summary>A breath kept before and after the word, so the recording doesn't clip its start or end.</summary>
        const float ClipPadSeconds = 0.3f;

        public NoCoachSettings Settings { get { return settings; } }
        public bool Listening { get; private set; }

        /// <summary>Set from the parent section; off means every lesson uses tap buttons.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>True when a lesson can ask the child to speak (switched on, not turned down this visit, and the phone has a mic).</summary>
        public bool Available { get { return Enabled && !declined && MicrophoneInput.HasDevice; } }

        /// <summary>The microphone card's "Not now" (or Android's "Don't allow"): NO buttons until the app is opened again.</summary>
        public bool Declined { get { return declined; } }

        /// <summary>Lets the app ask about the microphone again (the My NO screen's "Use my voice" button).</summary>
        public void AllowAskingAgain()
        {
            declined = false;
        }

        /// <summary>
        /// Raised for every spoken NO (not a whisper, not the warm-up): its rating, the samples of just that
        /// word and their rate. GameManager keeps it on this phone with NoRecorder.
        /// </summary>
        public event Action<NoRating, float[], int> NoCaptured;

        /// <summary>
        /// Shows the child (and their grown-up) why Keza wants the microphone, before Android asks.
        /// The first argument is true when Android will not ask again (the phone's settings must be used);
        /// call the answer with true to go ahead. Set by GameManager. Without it, Android asks straight away.
        /// </summary>
        public Action<bool, Action<bool>> ExplainMicrophone;

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

        const float MeterRangeDb = NoRating.FullPowerDb;   // the meter and the NO power number share one scale

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
        /// done gets true if a voiced answer was heard. Kept for this session only; never recorded.
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
            attempt++;
            Listening = false;
            onNo = null;
            onTalk = null;
            Level01 = 0f;
            PitchHz = 0f;
            microphone.StopCapture();
        }

        void Begin()
        {
            int ask = ++attempt;
            if (!Available) { Deliver(NoRating.NothingHeard()); return; }
            if (!permitted && MicrophoneInput.Authorized) permitted = true;
            if (!permitted)
            {
                if (ExplainMicrophone == null) { AskAndroid(ask); return; }
                // Our own card first, in the child's language: why, and what is kept. Then Android's question.
                bool blocked = MicrophoneInput.Blocked;
                ExplainMicrophone(blocked, yes =>
                {
                    if (ask != attempt) return;   // the child left the lesson meanwhile
                    if (!yes) { declined = true; Deliver(NoRating.NothingHeard()); return; }
                    if (blocked) { MicrophoneInput.OpenAppSettings(); Deliver(NoRating.NothingHeard()); return; }
                    AskAndroid(ask);
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
            int ringSize = Mathf.CeilToInt(microphone.SampleRate * RingSeconds);
            if (ring == null || ring.Capacity != ringSize) ring = new SampleRing(ringSize);
            ring.Clear();
            analyzer.Reset();
            segmenter.Reset();
            listenStart = Time.unscaledTime;
            Listening = true;
        }

        void AskAndroid(int ask)
        {
            MicrophoneInput.RequestPermission(granted =>
            {
                if (ask != attempt) return;
                permitted = granted;
                if (!granted) { declined = true; Deliver(NoRating.NothingHeard()); }
                else Begin();
            });
        }

        void Update()
        {
            // The child can't answer while the phone is reading to them: the wait starts after.
            if (Listening && ReadAloud.Speaking) listenStart = Time.unscaledTime;
            // Silence for too long: hand back "nothing heard" so the lesson can encourage the child.
            if (Listening && !continuous && !segmenter.InUtterance && Time.unscaledTime - listenStart > noAnswerSeconds)
                Deliver(NoRating.NothingHeard());
        }

        void OnSamples(float[] samples, int count)
        {
            if (!Listening) return;
            ring.Push(samples, 0, count);
            analyzer.Push(samples, 0, count);
        }

        void OnFrame(VoiceFrame frame)
        {
            if (!Listening) return;
            if (ReadAloud.Speaking)
            {
                // The phone's own voice is not the child's NO. Start fresh once it stops.
                segmenter.Reset();
                Level01 = 0f;
                PitchHz = 0f;
                return;
            }
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
                Capture(rating, utterance);
                if (onNo != null) onNo(rating);   // stay open for the next word
                return;
            }
            Capture(rating, utterance);
            Deliver(rating);
        }

        /// <summary>Cuts the NO just heard out of the ring and hands it on. Whispers and noises are not kept.</summary>
        void Capture(NoRating rating, Utterance utterance)
        {
            if (NoCaptured == null || !rating.Heard || rating.tip == NoTip.Whisper) return;
            // The word ended HangoverSeconds of quiet ago, so the newest samples are that quiet.
            float seconds = utterance.Duration + segmenter.HangoverSeconds + 2f * ClipPadSeconds;
            var samples = ring.Last(Mathf.CeilToInt(seconds * microphone.SampleRate));
            if (samples.Length == 0) return;
            SampleRing.FadeEdges(samples, microphone.SampleRate / 100);   // 10 ms
            NoCaptured(rating, samples, microphone.SampleRate);
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
