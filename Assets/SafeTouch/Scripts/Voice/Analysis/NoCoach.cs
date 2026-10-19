using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Turns one utterance into stars and a tip. Behavioural Skills Training
    /// needs feedback straight after each rehearsal; this is that feedback for
    /// the "S - Say no" step. It judges how NO was said, not which word: no
    /// speech recognition runs and nothing is sent anywhere.
    ///
    /// Three checks, one star each:
    ///   Loud   - at least strongLoudnessDb above the room.
    ///   Steady - pitch flat or falling, and not far above the talking voice.
    ///   Short  - one or two syllables, 0.15 to 1.3 seconds.
    /// A try that was heard always keeps at least one star.
    /// </summary>
    public static class NoCoach
    {
        public static NoRating Rate(Utterance u, NoCoachSettings s)
        {
            if (u == null || u.frames.Count == 0) return NoRating.NothingHeard();

            var r = new NoRating
            {
                loudnessDb = u.LoudnessAboveFloor,
                medianPitchHz = u.MedianPitch,
                aboveTalkingSemitones = VoiceMath.Semitones(u.MedianPitch, s.talkingPitchHz),
                slopeSemitones = u.MedianPitch > 0f ? u.PitchSlopeSemitones : 0f,
                seconds = u.Duration,
                syllables = u.Syllables,
                shouted = u.ClippedFraction >= s.shoutClipFraction
            };

            bool whisper = u.VoicedFraction < s.minVoicedFraction;
            bool audible = r.loudnessDb >= s.audibleLoudnessDb;
            bool rising = r.slopeSemitones > s.maxRiseSemitones;
            bool squeaky = r.aboveTalkingSemitones > s.maxAboveTalkingSemitones;
            bool tooLong = u.cutOff || r.seconds > s.maxSeconds;
            bool tooManyWords = r.syllables > s.maxSyllables;

            r.loud = r.loudnessDb >= s.strongLoudnessDb && !whisper;
            r.steady = !whisper && !rising && !squeaky;
            r.brief = !tooLong && !tooManyWords && r.seconds >= s.minSeconds;

            r.stars = (r.loud ? 1 : 0) + (r.steady ? 1 : 0) + (r.brief ? 1 : 0);
            r.stars = Mathf.Max(r.stars, 1);   // they were heard: never zero for trying

            if (whisper) r.tip = NoTip.Whisper;
            else if (!audible) r.tip = NoTip.TooQuiet;
            else if (!r.loud) r.tip = NoTip.ALittleLouder;
            else if (rising) r.tip = NoTip.Question;
            else if (squeaky) r.tip = NoTip.Squeaky;
            else if (tooManyWords) r.tip = NoTip.TooManyWords;
            else if (!r.brief) r.tip = NoTip.TooLong;
            else if (r.shouted) r.tip = NoTip.Shout;
            else r.tip = NoTip.Strong;
            return r;
        }
    }
}
