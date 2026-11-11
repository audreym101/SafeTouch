namespace SafeTouch.Voice
{
    /// <summary>The single most useful thing to tell the child after a try.</summary>
    public enum NoTip
    {
        NotHeard,       // nothing was heard
        Whisper,        // a whisper or breath, no clear voice
        TooQuiet,       // heard, but much quieter than a strong voice
        ALittleLouder,  // nearly there
        Question,       // the pitch went up at the end: "No?"
        Squeaky,        // much higher than the child's talking voice
        TooLong,        // "Nooooo..." or still going after 1.3 s
        TooManyWords,   // a sentence instead of one word
        Shout,          // strong, but screamed into the phone
        Strong          // loud, steady, short: well done
    }

    /// <summary>
    /// The coach's verdict on one "NO": 0 to 3 stars, three simple checks the
    /// child can see (loud, steady, short) and the measurements behind them.
    /// </summary>
    public class NoRating
    {
        public int stars;
        public NoTip tip;

        public bool loud;      // strong voice compared with the room
        public bool steady;    // pitch flat or falling, not squeaky
        public bool brief;     // one short word

        public float loudnessDb;              // dB above the room
        public float medianPitchHz;
        public float aboveTalkingSemitones;   // + is higher than the child's talking voice
        public float slopeSemitones;          // + rises, - falls
        public float seconds;
        public int syllables;
        public bool shouted;

        public bool Heard { get { return stars > 0; } }

        /// <summary>Decibels above the room that fill the NO power meter: a full-strength voice.</summary>
        public const float FullPowerDb = 30f;

        /// <summary>
        /// "NO power", 0 to 100: how far the NO rose above the room's noise, the number the child sees and tries to
        /// beat. A firm voice (strongLoudnessDb) passes; louder fills the meter. 0 when nothing was heard.
        /// </summary>
        public int Power { get { return Heard ? PowerOf(loudnessDb) : 0; } }

        /// <summary>The NO power of a loudness in decibels above the room.</summary>
        public static int PowerOf(float decibels)
        {
            return (int)System.Math.Round(System.Math.Max(0f, System.Math.Min(1f, decibels / FullPowerDb)) * 100f);
        }

        /// <summary>Two or three stars: good enough to move the lesson on.</summary>
        public bool Good { get { return stars >= 2; } }

        public static NoRating NothingHeard()
        {
            return new NoRating { stars = 0, tip = NoTip.NotHeard };
        }
    }
}
