using System;
using UnityEngine;

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

        AudioClip loop;
        string device;

        public int SampleRate { get; private set; }

        public bool IsCapturing { get { return loop != null && Microphone.IsRecording(device); } }

        public static bool HasDevice { get { return Microphone.devices.Length > 0; } }

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
            return true;
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
