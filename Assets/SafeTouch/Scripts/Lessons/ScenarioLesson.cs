using SafeTouch.Data;
using SafeTouch.UI;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 2, Safe or Unsafe? (FR4, FR5). Scenario cards float above
    /// Keza; the child sorts each one as safe, uncomfortable or unsafe and
    /// gets an explanation straight away. Every answer is saved for the
    /// pre/post evaluation.
    /// </summary>
    public class ScenarioLesson : LessonBase
    {
        int correct;

        public override int Level { get { return 2; } }

        protected override void OnBegin()
        {
            correct = 0;
            int difficulty = Older ? 2 : 1;
            game.Scenarios.LoadScenarios(difficulty);
            ShowCard();
        }

        void ShowCard()
        {
            var s = game.Scenarios.Current;
            if (s == null) { Finish(0, 0); return; }
            Header(string.Format(T("scenario.step", "Card {0} of {1}"), game.Scenarios.CurrentIndex + 1, game.Scenarios.Count));
            if (s.audioClip != null) game.Audio.PlayVoice(s.audioClip);
            Prompt(s.title, s.GetQuestion(),
                new PromptOption(s.optionA, Theme.Safe, () => Answer("A")),
                new PromptOption(s.optionB, Theme.Caution, () => Answer("B")),
                new PromptOption(s.optionC, Theme.Unsafe, () => Answer("C")));
        }

        void Answer(string letter)
        {
            var s = game.Scenarios.Current;
            bool right = game.Scenarios.CheckAnswer(letter);
            if (right) correct++;
            game.Progress.RecordScenario(s, letter, right);

            string message = s.GetFeedback(Older, T("deeper", "Think deeper:"));
            if (!right)
                message = string.Format(T("scenario.answer", "The answer is: {0}."), s.GetOptionText(s.correctAnswer)) + " " + message;
            Feedback(right, message, NextCard);
        }

        void NextCard()
        {
            if (game.Scenarios.IsLast)
            {
                Finish(correct, game.Scenarios.Count);
                return;
            }
            game.Scenarios.Next();
            ShowCard();
        }
    }
}
