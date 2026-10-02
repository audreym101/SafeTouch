using System;
using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Shared behaviour for the AR lessons: header, prompt card,
    /// narration, spawned figures and clean-up when the child exits.
    /// </summary>
    public abstract class LessonBase : MonoBehaviour
    {
        protected GameManager game;
        protected ARCharacter keza;
        readonly List<ARFigure> figures = new List<ARFigure>();
        string lastNarration;

        public abstract int Level { get; }
        public bool Running { get; private set; }

        /// <summary>Raised with (score, maximum score) when the lesson ends normally.</summary>
        public event Action<int, int> Completed;

        public void Begin(GameManager gameManager)
        {
            game = gameManager;
            keza = game.AR.Character;
            Running = true;
            game.AR.ClearMovable();
            game.UI.Lesson.ReplayNarration = () => Narrate(lastNarration);
            OnBegin();
        }

        /// <summary>Stops the lesson and removes everything it placed in the room.</summary>
        public void End()
        {
            if (!Running) return;
            Running = false;
            StopAllCoroutines();
            foreach (var f in figures) if (f != null) Destroy(f.gameObject);
            figures.Clear();
            game.AR.ClearMovable();
            if (keza != null) keza.ClearProps();
            OnEnd();
        }

        protected abstract void OnBegin();
        protected virtual void OnEnd() { }

        protected void Finish(int score, int max)
        {
            End();
            if (Completed != null) Completed(score, max);
        }

        protected string Title
        {
            get { return Localization.T(LessonInfo.For(Level).titleKey); }
        }

        protected void Header(string step)
        {
            game.UI.Lesson.SetHeader(Title, step);
        }

        protected void Prompt(string heading, string body, params PromptOption[] options)
        {
            game.UI.Lesson.ShowPrompt(heading, body, options);
        }

        /// <summary>True for the 9 to 11 age group, who get the extra "think deeper" steps.</summary>
        protected bool Older
        {
            get { return game.Progress.GetData().ageGroup == "9-11"; }
        }

        protected static string T(string key, string english)
        {
            return Localization.T(key, english);
        }

        /// <summary>Plays a narration clip, e.g. "bubble_intro" plays bubble_intro_en or bubble_intro_rw.</summary>
        protected void Narrate(string clipKey)
        {
            if (string.IsNullOrEmpty(clipKey)) return;
            lastNarration = clipKey;
            game.Audio.PlayVoice(clipKey + "_" + Localization.Language);
        }

        protected ARFigure Spawn(string label, Color color, float radius, float angle)
        {
            var f = game.AR.SpawnFigure(label, color, radius, angle);
            figures.Add(f);
            return f;
        }

        /// <summary>Removes the figures this lesson placed, before a new round.</summary>
        protected void ClearFigures()
        {
            foreach (var f in figures) if (f != null) Destroy(f.gameObject);
            figures.Clear();
            game.AR.ClearMovable();
        }

        protected void Feedback(bool correct, string message, Action next)
        {
            game.UI.ShowFeedback(
                correct ? T("feedback.correct", "That's right!") : T("feedback.tryagain", "Let's think again"),
                message, correct ? FeedbackTone.Correct : FeedbackTone.TryAgain, next);
            if (correct && keza != null) keza.Bounce();
        }

        /// <summary>Shuffles answer buttons so the right answer is not always first.</summary>
        protected static PromptOption[] Shuffle(params PromptOption[] options)
        {
            for (int i = options.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = options[i];
                options[i] = options[j];
                options[j] = tmp;
            }
            return options;
        }
    }
}
