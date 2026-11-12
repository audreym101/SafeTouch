using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace SafeTouch.Voice
{
    /// <summary>
    /// Opens the phone's microphone only while a lesson is listening for the
    /// child's "NO" and hands the samples to the analyser. Audio lives in a
    /// one-second loop that is overwritten as it goes. Only a spoken NO is ever
    /// kept, cut out by VoiceCoach and saved on this phone by NoRecorder when
    /// the parent allows it (consent terms 1.6); nothing is ever sent (NFR1,
    /// Law N° 058/2021).
    /// </summary>
    public class MicrophoneInput : MonoBehaviour
    {
        /// <summary>16 kHz is plenty for a child's voice and keeps the pitch maths cheap.</summary>
        public const int PreferredRate = 16000;

        /// <summary>Samples handed on per read. GetData wraps around the end of the loop.</summary>
        const int Block = 512;

        /// <summary>Remembers that Android was told "Don't ask again": only the phone's settings can allow it now.</summary>
        const string BlockedKey = "safetouch.mic.blocked";

        AudioClip loop;
        string device;
        int readPosition;
        readonly float[] block = new float[Block];

        /// <summary>Raised with new samples (buffer, count) on the main thread. The buffer is reused.</summary>
        public event Action<float[], int> SamplesReady;

        public int SampleRate { get; private set; }

        public bool IsCapturing { get { return loop != null && Microphone.IsRecording(device); } }

        /// <summary>
        /// True when the phone has a microphone. On Android the list of microphones can be empty until the
        /// child's grown-up has allowed the app to use it, so before that every phone counts as having one:
        /// the permission is asked when a lesson first listens, and StartCapture checks for a real one.
        /// Without this, a lesson could quietly show the NO button and never listen at all.
        /// </summary>
        public static bool HasDevice
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone)) return true;
#endif
                return Microphone.devices.Length > 0;
            }
        }

        /// <summary>True once the app may use the microphone (always in the Editor).</summary>
        public static bool Authorized
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bool allowed = Permission.HasUserAuthorizedPermission(Permission.Microphone);
                if (allowed && Blocked) PlayerPrefs.DeleteKey(BlockedKey);   // allowed in the phone's settings since
                return allowed;
#else
                return true;
#endif
            }
        }

        /// <summary>True when Android will no longer ask: the microphone must be allowed in the phone's settings.</summary>
        public static bool Blocked { get { return PlayerPrefs.GetInt(BlockedKey, 0) == 1; } }

        /// <summary>
        /// Asks Android for the microphone the first time a lesson needs it.
        /// If the answer is no, the lessons keep their tap buttons instead.
        /// </summary>
        public static void RequestPermission(Action<bool> answered)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) { answered(true); return; }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ =>
            {
                Core.AppSession.PermissionAnswered();
                PlayerPrefs.DeleteKey(BlockedKey);
                answered(true);
            };
            callbacks.PermissionDenied += _ => { Core.AppSession.PermissionAnswered(); answered(false); };
            callbacks.PermissionDeniedAndDontAskAgain += _ =>
            {
                Core.AppSession.PermissionAnswered();
                PlayerPrefs.SetInt(BlockedKey, 1);
                PlayerPrefs.Save();
                answered(false);
            };
            Core.AppSession.PermissionAsked();   // the question pauses the app: that is not the parent leaving
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
            answered(HasDevice);
#endif
        }

        /// <summary>Opens this app's page in the phone's settings, where a grown-up can allow the microphone.</summary>
        public static void OpenAppSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Core.AppSession.PermissionAsked();   // leaving for the settings is not the parent leaving SafeTouch
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri))
                {
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000);   // FLAG_ACTIVITY_NEW_TASK
                    activity.Call("startActivity", intent);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("SafeTouch: could not open the app's settings: " + e.Message);
            }
#endif
        }

        /// <summary>Opens the default microphone. Returns false if the phone has none.</summary>
        public bool StartCapture()
        {
            if (IsCapturing) return true;
            if (!HasDevice) return false;
            device = Microphone.devices[0];

            int min, max;
            Microphone.GetDeviceCaps(device, out min, out max);
            SampleRate = min == 0 && max == 0 ? PreferredRate : Mathf.Clamp(PreferredRate, min, max);

            loop = Microphone.Start(device, true, 1, SampleRate);
            if (loop == null) return false;
            SampleRate = loop.frequency;
            readPosition = 0;
            return true;
        }

        /// <summary>Reads whatever the microphone wrote since last frame, in fixed blocks.</summary>
        void Update()
        {
            if (!IsCapturing) return;
            int writePosition = Microphone.GetPosition(device);
            if (writePosition < 0) return;
            int available = writePosition - readPosition;
            if (available < 0) available += loop.samples;
            while (available >= Block)
            {
                loop.GetData(block, readPosition);
                readPosition = (readPosition + Block) % loop.samples;
                available -= Block;
                if (SamplesReady != null) SamplesReady(block, Block);
            }
        }

        /// <summary>Closes the microphone; the phone's recording indicator goes off.</summary>
        public void StopCapture()
        {
            if (loop == null) return;
            Microphone.End(device);
            Destroy(loop);
            loop = null;
        }

        void OnDisable()
        {
            StopCapture();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) StopCapture();
        }
    }
}
