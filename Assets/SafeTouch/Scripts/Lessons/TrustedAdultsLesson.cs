using System.Collections.Generic;
using System.Linq;
using SafeTouch.Data;
using SafeTouch.UI;
using SafeTouch.UI.Screens;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 4, My Trusted Adults (FR7, FR8). The child chooses at least two
    /// kinds of grown-ups they can go to, places them around Keza in AR as a
    /// "helping hand", then picks who to tell first. Choices are saved on
    /// the phone as role ids only.
    /// </summary>
    public class TrustedAdultsLesson : LessonBase
    {
        public override int Level { get { return 4; } }

        protected override void OnBegin()
        {
            game.UI.Adults.Confirmed = PlaceAdults;
            game.UI.ShowMenu("adults");
        }

        void PlaceAdults(List<TrustedAdult> chosen)
        {
            game.Progress.SetTrustedAdults(chosen.Select(a => a.adultId));
            game.UI.ShowMenu("lesson");
            Header(string.Format(T("adults.step", "{0} helpers around Keza"), chosen.Count));

            // Spread the adults in a 200 degree arc around the far side of
            // Keza (180 = behind her), so none of them blocks the view.
            float arc = 200f;
            for (int i = 0; i < chosen.Count; i++)
            {
                float angle = chosen.Count == 1 ? 180f : 180f - arc / 2f + arc * i / (chosen.Count - 1);
                var adult = chosen[i];
                var figure = Spawn(adult.name, adult.GetColor(), 0.8f, angle, adult.adultId);
                figure.Bounce();
                adult.PlaceInAR();
            }

            var options = chosen.Take(4).Select(a => new PromptOption(a.name, Theme.Card, () => ChoseFirst(a, chosen.Count))).ToArray();
            Prompt(T("adults.first.title", "Keza's helping hand"),
                T("adults.first.body", "If something ever felt wrong, who would Keza tell first?"),
                options);
        }

        void ChoseFirst(TrustedAdult adult, int count)
        {
            string msg = string.Format(T("adults.first.fb", "Great choice! Keza would tell {0}. If they don't listen, Keza tells the next adult on her hand."), adult.name);
            Feedback(true, msg, () => Finish(System.Math.Min(count, AdultsScreen.HelpingHand), AdultsScreen.HelpingHand));
        }
    }
}
