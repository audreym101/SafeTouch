using SafeTouch.Core;
using SafeTouch.Voice;

namespace SafeTouch.UI
{
    /// <summary>
    /// Child-friendly words for each coaching tip. Every tip says what to do
    /// next rather than what went wrong, and none of them blames the child.
    /// </summary>
    public static class VoiceTips
    {
        public static string For(NoTip tip)
        {
            switch (tip)
            {
                case NoTip.Strong: return Localization.T("voice.tip.strong", "Strong and steady! That's how to say NO.");
                case NoTip.Shout: return Localization.T("voice.tip.shout", "Very strong! You don't need to scream: a big, firm voice is enough.");
                case NoTip.Whisper: return Localization.T("voice.tip.whisper", "That sounded like a whisper. Use your real voice, from your tummy!");
                case NoTip.TooQuiet: return Localization.T("voice.tip.quiet", "Louder! Say it like you really mean it.");
                case NoTip.ALittleLouder: return Localization.T("voice.tip.louder", "Nearly there! Just a little louder.");
                case NoTip.Question: return Localization.T("voice.tip.question", "Your voice went up at the end, like a question: \"No?\" Make it go down: \"NO.\"");
                case NoTip.Squeaky: return Localization.T("voice.tip.squeaky", "That was very high. Use your low, strong voice.");
                case NoTip.TooLong: return Localization.T("voice.tip.long", "Keep it short and sharp: NO!");
                case NoTip.TooManyWords: return Localization.T("voice.tip.words", "Just one word: NO! (or OYA!)");
                default: return Localization.T("voice.tip.nothing", "I didn't hear anything. Hold the phone near you and say NO clearly, like you mean it.");
            }
        }
    }
}
