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
    /// one-second loop that is overwritten as it goes: nothing is recorded,
    /// saved or sent (NFR1, Law N° 058/2021).
    /// </summary>
    public class MicrophoneInput : MonoBehaviour
    {
        /// <summary>16 kHz is plenty for a child's voice and keeps the pitch maths cheap.</summary>
        public const int PreferredRate = 16000;

        /// <summary>Samples handed on per read. GetData wraps around the end of the loop.</summary>
        const int Block = 512;

        AudioClip loop;
        string device;
        int readPosition;
        readonly float[] block = new float[Block];

        /// <summary>Raised with new samples (buffer, count) on the main thread. The buffer is reused.</summary>
        public event Action<float[], int> SamplesReady;

        public int SampleRate { get; private set; }

        public bool IsCapturing { get { return loop != null && Microphone.IsRecording(device); } }

        public static bool HasDevice { get { return Microphone.devices.Length > 0; } }

        /// <summary>
        /// Asks Android for the microphone the first time a lesson needs it.
        /// If the answer is no, the lessons keep their tap buttons instead.
        /// </summary>
        public static void RequestPermission(Action<bool> answered)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) { answered(true); return; }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { Core.AppSession.PermissionAnswered(); answered(true); };
            callbacks.PermissionDenied += _ => { Core.AppSession.PermissionAnswered(); answered(false); };
            callbacks.PermissionDeniedAndDontAskAgain += _ => { Core.AppSession.PermissionAnswered(); answered(false); };
            Core.AppSession.PermissionAsked();   // the question pauses the app: that is not the parent leaving
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
            answered(HasDevice);
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
