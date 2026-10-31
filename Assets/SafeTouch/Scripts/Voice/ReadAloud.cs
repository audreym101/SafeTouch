using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Reads the app's text out loud for children who are still learning to
    /// read, with the phone's own text-to-speech voice (Android TextToSpeech).
    /// It runs on the phone: no text is sent anywhere and nothing is recorded.
    /// While it speaks, the voice coach ignores the microphone so Keza's own
    /// words are never taken for the child's NO.
    /// Kinyarwanda needs a Kinyarwanda voice on the phone; most phones have
    /// none yet, so Speak returns false and the screen can say so.
    /// </summary>
    public class ReadAloud : MonoBehaviour
    {
        /// <summary>True while the phone is reading out loud.</summary>
        public static bool Speaking { get; private set; }

        /// <summary>Set from the parent section: read each new card without a tap.</summary>
        public bool AutoRead { get; set; }

        [Tooltip("Slower than normal, for young listeners (1 = normal speed).")]
        [SerializeField] float speechRate = 0.85f;

        float speakingSince;

        /// <summary>
        /// Takes out what should not be spoken: rich-text tags, the "|" between
        /// status parts, and quote marks, and joins the lines.
        /// </summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = Regex.Replace(text, "<[^>]+>", "");
            text = text.Replace("|", ".").Replace("\"", "").Replace("“", "").Replace("”", "");
            text = Regex.Replace(text, @"\s*\n+\s*", ". ");
            text = Regex.Replace(text, @"\.(\s*\.)+", ".");
            return text.Trim();
        }

        /// <summary>Reads the given lines, one after the other. Returns false when this language can't be read on this phone.</summary>
        public bool Speak(params string[] lines)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                var clean = Clean(line);
                if (clean.Length == 0) continue;
                parts.Add(clean.EndsWith(".") || clean.EndsWith("!") || clean.EndsWith("?") ? clean : clean + ".");
            }
            if (parts.Count == 0) return true;
            return SpeakNow(string.Join(" ", parts.ToArray()));
        }

        /// <summary>Reads only when a parent switched on "read every card aloud".</summary>
        public void SpeakIfAuto(params string[] lines)
        {
            if (AutoRead) Speak(lines);
        }

        void MarkSpeaking()
        {
            Speaking = true;
            speakingSince = Time.unscaledTime;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        const int QueueFlush = 0;
        AndroidJavaObject tts;
        IntPtr speakMethod;
        volatile int initStatus = 1;   // 1 starting, 0 ready, -1 no text-to-speech on this phone
        string pending;
        string languageSet;
        bool languageOk;
        float nextPoll;

        class InitListener : AndroidJavaProxy
        {
            readonly ReadAloud owner;
            public InitListener(ReadAloud owner) : base("android.speech.tts.TextToSpeech$OnInitListener") { this.owner = owner; }
            public void onInit(int status) { owner.initStatus = status == 0 ? 0 : -1; }
        }

        void Start()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener(this));
                // speak(CharSequence, int, Bundle, String) is looked up by hand: the Bundle is null,
                // which Unity's automatic signature matching can't type.
                speakMethod = AndroidJNIHelper.GetMethodID(tts.GetRawClass(), "speak",
                    "(Ljava/lang/CharSequence;ILandroid/os/Bundle;Ljava/lang/String;)I");
            }
            catch (Exception e)
            {
                initStatus = -1;
                Debug.LogWarning("[SafeTouch] Read aloud is not available: " + e.Message);
            }
        }

        bool SpeakNow(string text)
        {
            if (initStatus == -1) return false;
            if (initStatus == 1) { pending = text; return true; }   // spoken as soon as the voice is ready
            if (!PrepareLanguage()) return false;
            var jText = AndroidJNI.NewStringUTF(text);
            var jId = AndroidJNI.NewStringUTF("safetouch");
            var args = new jvalue[4];
            args[0].l = jText;
            args[1].i = QueueFlush;   // a new card interrupts the last one
            args[2].l = IntPtr.Zero;
            args[3].l = jId;
            AndroidJNI.CallIntMethod(tts.GetRawObject(), speakMethod, args);
            AndroidJNI.DeleteLocalRef(jText);
            AndroidJNI.DeleteLocalRef(jId);
            MarkSpeaking();
            return true;
        }

        /// <summary>Switches the voice to the app's language; false when the phone has no voice for it.</summary>
        bool PrepareLanguage()
        {
            string language = string.IsNullOrEmpty(Core.Localization.Language) ? "en" : Core.Localization.Language;
            if (language == languageSet) return languageOk;
            languageSet = language;
            using (var locale = language == "rw"
                ? new AndroidJavaObject("java.util.Locale", "rw", "RW")
                : new AndroidJavaObject("java.util.Locale", "en"))
            {
                languageOk = tts.Call<int>("isLanguageAvailable", locale) >= 0;   // LANG_AVAILABLE and up
                if (languageOk)
                {
                    tts.Call<int>("setLanguage", locale);
                    tts.Call<int>("setSpeechRate", speechRate);
                }
            }
            return languageOk;
        }

        public void Stop()
        {
            pending = null;
            if (tts != null && initStatus == 0) tts.Call<int>("stop");
            Speaking = false;
        }

        void Update()
        {
            if (initStatus == 0 && pending != null)
            {
                var text = pending;
                pending = null;
                SpeakNow(text);
            }
            // isSpeaking is asked a few times a second, not every frame. Give the
            // engine half a second to start before taking "not speaking" as done.
            if (!Speaking || tts == null || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.15f;
            if (Time.unscaledTime - speakingSince > 0.5f && !tts.Call<bool>("isSpeaking")) Speaking = false;
        }

        void OnDestroy()
        {
            Speaking = false;
            if (tts == null) return;
            tts.Call<int>("stop");
            tts.Call("shutdown");
            tts.Dispose();
            tts = null;
        }
#else
        // In the Editor there is no phone voice: the text goes to the Console,
        // and Speaking lasts about as long as reading it would, so the voice
        // coach's pause can be tried on a laptop.
        float speakingFor;

        bool SpeakNow(string text)
        {
            Debug.Log("[SafeTouch] Read aloud: " + text);
            MarkSpeaking();
            speakingFor = Mathf.Clamp(text.Length * 0.065f / speechRate, 0.5f, 20f);
            return true;
        }

        public void Stop()
        {
            Speaking = false;
        }

        void Update()
        {
            if (Speaking && Time.unscaledTime - speakingSince >= speakingFor) Speaking = false;
        }

        void OnDestroy()
        {
            Speaking = false;
        }
#endif
    }
}
