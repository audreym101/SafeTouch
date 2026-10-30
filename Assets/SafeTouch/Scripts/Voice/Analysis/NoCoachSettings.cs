using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// What counts as a strong, clear "NO". Loudness is measured against the
    /// room and pitch against the child's own talking voice, so the same rules
    /// work for a quiet 6-year-old and a loud 11-year-old on any phone.
    /// Values were chosen for the Week 7 expert review and can be tuned there.
    /// </summary>
    public class NoCoachSettings
    {
        /// <summary>
        /// The child's everyday talking pitch. Until the child says "Hello
        /// Keza", typical values are used: about 290 Hz at ages 6-8 and 260 Hz
        /// at 9-11 (Lee, Potamianos and Narayanan, 1999).
        /// </summary>
        public float talkingPitchHz = 280f;

        /// <summary>True once the talking pitch came from the child rather than the age default.</summary>
        public bool calibrated;

        public float strongLoudnessDb = 13f;     // dB above the room for a full-marks voice: a firm normal voice in a classroom, not a shout
        public float audibleLoudnessDb = 7f;     // dB above the room to count as heard at all
        public float maxRiseSemitones = 1.5f;    // more than this and "No" sounds like a question
        public float maxAboveTalkingSemitones = 6f;   // higher than this is a squeak or a scream
        public float minSeconds = 0.15f;
        public float maxSeconds = 1.3f;          // "Nooooooo" is a plea; "NO." is short
        public int maxSyllables = 2;             // "No" or "Oya"
        public float minVoicedFraction = 0.3f;   // less means a whisper
        public float shoutClipFraction = 0.1f;   // share of clipped frames that means a scream into the phone

        public static NoCoachSettings ForAgeGroup(string ageGroup)
        {
            return new NoCoachSettings { talkingPitchHz = ageGroup == "9-11" ? 260f : 290f };
        }

        /// <summary>Uses the child's own talking pitch, kept only for this session.</summary>
        public void SetTalkingPitch(float hz)
        {
            if (hz <= 0f) return;
            talkingPitchHz = Mathf.Clamp(hz, 150f, 450f);
            calibrated = true;
        }
    }
}
