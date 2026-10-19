using SafeTouch.AR;
using SafeTouch.UI;
using SafeTouch.Voice;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 3, the STOP framework (FR6): S - Say no (out loud, coached by
    /// the microphone when available), T - Tell a trusted
    /// adult, O - Openly explain what happened, P - Protect yourself by going
    /// to a safe place. The child rehearses each step with Keza in the room.
    /// Ages 9 to 11 get "think deeper" notes and a fifth step: keep telling
    /// until someone helps.
    /// </summary>
    public class StopFrameworkLesson : LessonBase
    {
        ARFigure visitor, teacher;
        GameObject safePlace;
        int score;

        public override int Level { get { return 3; } }

        protected override void OnBegin()
        {
            score = 0;
            visitor = Spawn(T("stop.visitor", "Someone"), new Color(0.55f, 0.6f, 0.68f), 0.8f, 160f);
            safePlace = keza.AddDisc("Safe place", 0.25f, Theme.WithAlpha(Theme.Safe, 0.6f), game.AR.TransparentMaterial, 0.004f);
            // The safe place is a separate spot in the room, not under Keza.
            safePlace.transform.SetParent(game.AR.AnchorRoot, true);
            safePlace.transform.position = game.AR.PointAroundCharacter(1.2f, -90f);

            Header("S  T  O  P");
            Narrate("stop_intro");
            Prompt(T("stop.intro.title", "Something doesn't feel right"),
                T("stop.intro.body", "Someone says to Keza: \"Let's play a secret touching game. Don't tell anyone.\" Secrets about touching are never okay. Let's learn STOP."),
                new PromptOption(T("stop.intro.go", "Let's STOP it"), Theme.Primary, () => WarmUpVoice(StepSayNo)));
        }

        /// <summary>Adds a "think deeper" note for ages 9-11; empty for 6-8.</summary>
        string Deeper(string key, string english)
        {
            return Older ? "\n\n" + T("deeper", "Think deeper:") + " " + T(key, english) : "";
        }

        /// <summary>
        /// S is rehearsed out loud: the child says NO and hears how it
        /// sounded (Behavioural Skills Training: rehearsal, then feedback).
        /// A three-star NO sends the visitor further back.
        /// </summary>
        void StepSayNo()
        {
            Header(T("stop.s", "S - Say no"));
            Narrate("stop_s");
            SayNo(T("stop.s.title", "S: Say NO"),
                T("stop.s.voice", "Say \"NO!\" out loud in a big, strong voice, so the person knows you mean it. Keza is listening!"),
                T("stop.s.button", "NO!"),
                rating =>
                {
                    score++;
                    keza.Bounce();
                    float away = rating != null && rating.stars == 3 ? 1.9f : 1.5f;
                    visitor.WalkTo(game.AR.PointAroundCharacter(away, 160f), null);
                    Feedback(true, SayNoFeedback(rating)
                        + Deeper("stop.s.deeper", "Keza can say no even to someone older, someone she loves, or someone who gave her a gift. Saying no is not rude when her body is involved."), TellAdult);
                });
        }

        static string SayNoFeedback(NoRating rating)
        {
            if (rating == null || !rating.Heard)
                return T("stop.s.fb", "Great! Saying no is always allowed, even to an adult. Next time, try saying it out loud too.");
            if (rating.Good)
                return T("stop.s.fb.voice", "The person heard your strong NO and stepped back. Saying no is always allowed, even to an adult.");
            return T("stop.s.fb.soft", "Good try! A loud, low, short NO works best. You can practise it more in the Bubble Guard game.");
        }

        void TellAdult()
        {
            Header(T("stop.t", "T - Tell a trusted adult"));
            Narrate("stop_t");
            bool firstTry = true;
            Prompt(T("stop.t.title", "T: Tell a trusted adult"),
                T("stop.t.body", "Who should Keza tell?"),
                Shuffle(
                    new PromptOption(T("stop.t.teacher", "My teacher, or another adult I trust"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        teacher = Spawn(T("stop.teacher", "Teacher"), Theme.Primary, 1.0f, -130f);
                        Feedback(true, T("stop.t.fb", "Yes. If the first adult doesn't help, keep telling until someone does."), OpenlyExplain);
                    }),
                    new PromptOption(T("stop.t.nobody", "Nobody, keep the secret"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.t.nobodyfb", "Secrets about touching should always be told. Keza will not get in trouble."), null);
                    }),
                    new PromptOption(T("stop.t.same", "The person who asked"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.t.samefb", "That person made Keza feel unsafe. Keza tells a different adult she trusts."), null);
                    })));
        }

        void OpenlyExplain()
        {
            Header(T("stop.o", "O - Openly explain"));
            Narrate("stop_o");
            if (teacher != null) keza.FaceTowards(teacher.transform.position);
            bool firstTry = true;
            Prompt(T("stop.o.title", "O: Openly explain what happened"),
                T("stop.o.body", "Which words help the teacher understand?"),
                Shuffle(
                    new PromptOption(T("stop.o.good", "\"Someone asked me to keep a touching secret. I felt scared.\""), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        Feedback(true, T("stop.o.fb", "Clear words help adults help you. It is never the child's fault.")
                            + Deeper("stop.o.deeper", "Good telling has three parts: what happened, who did it, and how Keza felt. She doesn't need every detail; the adult will ask."), Protect);
                    }),
                    new PromptOption(T("stop.o.nothing", "\"Nothing happened.\""), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.o.nothingfb", "Keza can tell the truth. Trusted adults want to know, and Keza is safe telling them."), null);
                    }),
                    new PromptOption(T("stop.o.fault", "\"I did something bad.\""), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.o.faultfb", "Keza did nothing wrong. It is never the child's fault."), null);
                    })));
        }

        void Protect()
        {
            Header(T("stop.p", "P - Protect yourself"));
            Narrate("stop_p");
            bool firstTry = true;
            Prompt(T("stop.p.title", "P: Protect yourself"),
                T("stop.p.body", "Where should Keza go now?"),
                Shuffle(
                    new PromptOption(T("stop.p.safe", "To a safe place with people I trust"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        keza.WalkTo(safePlace.transform.position, () =>
                            Feedback(true, T("stop.p.fb", "Keza is in a safe place. S-T-O-P: Say no, Tell, Open up, Protect!"),
                                () => { if (Older) KeepTelling(); else Finish(score, 4); }));
                    }),
                    new PromptOption(T("stop.p.hide", "Somewhere alone to hide"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.p.hidefb", "Being alone is not the safest choice. Keza goes where trusted people are."), null);
                    }),
                    new PromptOption(T("stop.p.stay", "Stay with the person"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.p.stayfb", "Keza moves away from anyone who makes her feel unsafe."), null);
                    })));
        }

        /// <summary>Ages 9-11: what to do when the first adult doesn't help.</summary>
        void KeepTelling()
        {
            Header(T("stop.more", "Bonus: keep telling"));
            bool firstTry = true;
            Prompt(T("stop.more.title", "What if they don't listen?"),
                T("stop.more.body", "Keza told her teacher, but the teacher was busy and said \"Not now.\" What should Keza do?"),
                Shuffle(
                    new PromptOption(T("stop.more.next", "Tell another trusted adult"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        var parent = Spawn(T("stop.parent", "Mum"), Theme.Hex("#F28C38"), 1.0f, -20f);
                        parent.WalkTo(game.AR.PointAroundCharacter(0.5f, -20f), null);
                        Feedback(true, T("stop.more.fb", "Yes! Keza keeps telling until someone helps: a parent, a nurse, the Child Helpline on 116. Being ignored once doesn't mean she was wrong."),
                            () => Finish(score, 5));
                    }),
                    new PromptOption(T("stop.more.quit", "Give up, nobody listens"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.more.quitfb", "Many adults want to help. If one doesn't, Keza tells the next person on her helping hand."), null);
                    }),
                    new PromptOption(T("stop.more.wait", "Wait and see if it happens again"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.more.waitfb", "Keza doesn't have to wait. Telling early keeps her safe."), null);
                    })));
        }
    }
}
