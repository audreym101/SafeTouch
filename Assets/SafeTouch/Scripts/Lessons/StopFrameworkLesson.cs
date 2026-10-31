using SafeTouch.AR;
using SafeTouch.UI;
using SafeTouch.Voice;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 3, the STOP framework (FR6), played by collecting letters. The
    /// letters S, T, O and P wait around the room; the child moves the guide
    /// to each one in turn (drag, tap the letter, or the arrow buttons). A
    /// collected letter says what it means, then its step begins:
    ///   S - Say no: out loud, coached by the microphone when available.
    ///   T - Tell a trusted adult: the answers are circles on the floor.
    ///   O - Openly explain what happened: answer circles again.
    ///   P - Protect yourself: walk the guide to the glowing safe place.
    /// Ages 9 to 11 get "think deeper" notes and a fifth step: keep telling
    /// until someone helps.
    /// </summary>
    public class StopFrameworkLesson : LessonBase
    {
        static readonly string[] Why =
        {
            "Say NO in a clear, firm voice.",
            "Tell a grown-up you trust.",
            "Say what happened and how you felt.",
            "Go somewhere safe, near people you trust.",
        };

        ARFigure visitor, teacher;
        GameObject safePlace;
        LetterHunt hunt;
        int score;

        public override int Level { get { return 3; } }

        protected override void OnBegin()
        {
            score = 0;
            visitor = Spawn(T("stop.visitor", "Someone"), new Color(0.55f, 0.6f, 0.68f), 0.8f, 160f);
            safePlace = keza.AddDisc("Safe place", 0.25f, Theme.WithAlpha(Theme.Safe, 0.6f), game.AR.TransparentMaterial, 0.004f);
            // The safe place is a separate spot in the room, not under Keza.
            safePlace.transform.SetParent(game.AR.AnchorRoot, true);
            safePlace.transform.position = game.AR.PointAroundCharacter(1.3f, -90f);
            if (hunt == null) hunt = gameObject.AddComponent<LetterHunt>();

            Header("S  T  O  P");
            Narrate("stop_intro");
            visitor.Say(T("stop.visitor.says", "Let's play a secret game!"), game.AR.OpaqueMaterial);
            Prompt(T("stop.intro.title", "Something feels wrong"),
                T("stop.intro.body", "Someone says: \"Let's play a secret touching game. Don't tell.\" Touching secrets are never okay. Let's learn STOP!"),
                new PromptOption(T("stop.intro.go", "Let's STOP it"), Theme.Primary, () =>
                {
                    visitor.Say(null, null);
                    WarmUpVoice(StartHunt);
                }));
        }

        protected override void OnEnd()
        {
            walkingToSafety = false;
            if (hunt != null) hunt.Clear();
            if (safeLabel != null) Destroy(safeLabel);
            if (safeRing != null) Destroy(safeRing);
        }

        protected override bool OnTap(ARTap tap)
        {
            return hunt != null && hunt.HandleTap(tap);
        }

        // ---- Collecting the letters ----

        void StartHunt()
        {
            hunt.Begin(game, keza, Collected, WrongLetter);
            WaitForLetter();
        }

        /// <summary>The card and hint point to the next letter; the arrow buttons help the guide get there.</summary>
        void WaitForLetter()
        {
            int next = hunt.Next;
            string letter = LetterHunt.Letters[next];
            hunt.Hunting = true;
            Header(string.Format(T("stop.collect.step", "Collect the letter {0}"), letter));
            TouchHint(T("stop.collect.touch", "Move {0} to the next letter."));
            Prompt(string.Format(T("stop.collect.title", "Find {0}!"), letter),
                string.Format(T("review.grab.where", "{0} is {1}."), letter, hunt.Direction(next)),
                new PromptOption(string.Format(T("hunt.go", "Go to {0}"), letter), Theme.Card, hunt.TakeGuideToNext));
            MovePad(true);
        }

        void WrongLetter(int touched)
        {
            string letter = LetterHunt.Letters[hunt.Next];
            Prompt(T("review.grab.notyet.title", "Not yet!"),
                string.Format(T("review.grab.notyet.short", "First {0}. It's {1}."), letter, hunt.Direction(hunt.Next)),
                new PromptOption(string.Format(T("hunt.go", "Go to {0}"), letter), Theme.Card, hunt.TakeGuideToNext));
            MovePad(true);
        }

        /// <summary>A collected letter explains itself, then its step begins.</summary>
        void Collected(int index)
        {
            MovePad(false);
            TouchHint("");
            string title = string.Format(T("review.stop.got", "{0} is for {1}!"), LetterHunt.Letters[index], LetterHunt.Meaning(index));
            string why = T("review.why." + LetterHunt.Letters[index].ToLowerInvariant(), Why[index]);
            System.Action step = index == 0 ? (System.Action)(() => StepSayNo())
                : index == 1 ? TellAdult
                : index == 2 ? (System.Action)OpenlyExplain
                : Protect;
            game.UI.ShowFeedback(title, why, FeedbackTone.Correct, step);
        }

        /// <summary>After a step, the next letter; after P, the end.</summary>
        void NextLetter()
        {
            if (hunt.Done) { if (Older) KeepTelling(); else Finish(score, 4); return; }
            WaitForLetter();
        }

        /// <summary>Adds a "think deeper" note for ages 9-11; empty for 6-8.</summary>
        string Deeper(string key, string english)
        {
            return Older ? "\n\n" + T("deeper", "Think deeper:") + " " + T(key, english) : "";
        }

        // ---- S: say no ----

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
                T("stop.s.voice", "Say \"NO!\" clearly, like you mean it. Keza is listening!"),
                T("stop.s.button", "NO!"),
                rating =>
                {
                    score++;
                    keza.Bounce();
                    float away = rating != null && rating.stars == 3 ? 1.9f : 1.5f;
                    visitor.WalkTo(game.AR.PointAroundCharacter(away, 160f), null);
                    Feedback(true, SayNoFeedback(rating)
                        + Deeper("stop.s.deeper", "Keza can say no to anyone: someone older, someone Keza loves, or someone who gave a gift."), NextLetter);
                });
        }

        static string SayNoFeedback(NoRating rating)
        {
            if (rating == null || !rating.Heard)
                return T("stop.s.fb", "Great! You can always say no, even to a grown-up. Next time, say it out loud too.");
            if (rating.Good)
                return T("stop.s.fb.voice", "They heard your strong NO and stepped back. You can always say no.");
            return T("stop.s.fb.soft", "Good try! A loud, short NO works best. Practise more in Bubble Guard.");
        }

        // ---- T: tell ----

        void TellAdult()
        {
            Header(T("stop.t", "T - Tell a trusted adult"));
            Narrate("stop_t");
            bool firstTry = true;
            ChooseInRoom(T("stop.t.title", "T: Tell a grown-up you trust"),
                T("stop.t.body", "Who should Keza tell?"),
                Shuffle(
                    new PromptOption(T("stop.t.teacher", "My teacher, or a grown-up I trust"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        teacher = Spawn(T("stop.teacher", "Teacher"), Theme.Primary, 1.0f, -130f);
                        Feedback(true, T("stop.t.fb", "Yes! If they don't help, keep telling until someone does."), NextLetter);
                    }),
                    new PromptOption(T("stop.t.nobody", "Nobody, keep the secret"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.t.nobodyfb", "Touching secrets are always told. Keza is not in trouble."), null);
                    }),
                    new PromptOption(T("stop.t.same", "The person who asked"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.t.samefb", "That person made Keza feel unsafe. Keza tells a different grown-up."), null);
                    })));
        }

        // ---- O: open up ----

        void OpenlyExplain()
        {
            Header(T("stop.o", "O - Openly explain"));
            Narrate("stop_o");
            if (teacher != null) keza.FaceTowards(teacher.transform.position);
            bool firstTry = true;
            ChooseInRoom(T("stop.o.title", "O: Open up and say what happened"),
                T("stop.o.body", "Which words help the teacher understand?"),
                Shuffle(
                    new PromptOption(T("stop.o.good", "\"Someone asked me to keep a touching secret. I felt scared.\""), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        Feedback(true, T("stop.o.fb", "Clear words help grown-ups help. It is never the child's fault.")
                            + Deeper("stop.o.deeper", "Say what happened, who did it, and how you felt. The grown-up will ask the rest."), NextLetter);
                    }),
                    new PromptOption(T("stop.o.nothing", "\"Nothing happened.\""), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.o.nothingfb", "Keza can tell the truth. Trusted grown-ups want to know."), null);
                    }),
                    new PromptOption(T("stop.o.fault", "\"I did something bad.\""), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.o.faultfb", "Keza did nothing wrong. It is never the child's fault."), null);
                    })));
        }

        // ---- P: protect (go to the safe place) ----

        void Protect()
        {
            Header(T("stop.p", "P - Protect yourself"));
            Narrate("stop_p");
            score++;
            GoToSafePlace();
        }

        bool walkingToSafety;
        GameObject safeLabel, safeRing;

        /// <summary>
        /// The child takes the guide to safety: the safe place glows and is
        /// labelled; drag the guide, use the arrow buttons, or "Show me". The
        /// step ends when the guide stands on it.
        /// </summary>
        void GoToSafePlace()
        {
            walkingToSafety = true;
            var spot = safePlace.transform.position;
            safeRing = ARFx.FloorQuad("Safe ring", ARFx.Ring, Theme.Safe, 0.6f);
            safeRing.transform.position = spot + Vector3.up * 0.006f;
            FxAnimator.Pulse(safeRing);
            safeLabel = ARFx.Label(T("stop.p.label", "Safe place"), spot + Vector3.up * 0.2f, Theme.Safe, 0.9f);
            if (game.AR.AnchorRoot != null)
            {
                safeRing.transform.SetParent(game.AR.AnchorRoot, true);
                safeLabel.transform.SetParent(game.AR.AnchorRoot, true);
            }
            TouchHint(T("stop.p.touch.arrows", "Use the arrows, or drag {0}, to the green circle."));
            Prompt(T("stop.p.go.title", "Go to the safe place!"),
                T("stop.p.go", "Drag Keza to the green safe place, near people Keza trusts."),
                new PromptOption(T("bubble.help", "Show me"), Theme.Card, () => keza.WalkTo(safePlace.transform.position, null)));
            MovePad(true);
        }

        void Update()
        {
            if (!Running || !walkingToSafety || keza == null || safePlace == null) return;
            if (Vector3.ProjectOnPlane(keza.transform.position - safePlace.transform.position, Vector3.up).magnitude > 0.2f) return;
            walkingToSafety = false;
            keza.Stop();
            MovePad(false);
            ARFx.Burst(safePlace.transform.position + Vector3.up * 0.3f, Theme.Safe);
            if (safeRing != null) Destroy(safeRing);
            TouchHint(T("touch.hint", "Tap {0} to say hi. Drag people to move them."));
            Feedback(true, T("stop.p.fb", "Keza is safe. S-T-O-P: Say no, Tell, Open up, Protect!"), NextLetter);
        }

        /// <summary>Ages 9-11: what to do when the first adult doesn't help.</summary>
        void KeepTelling()
        {
            Header(T("stop.more", "Bonus: keep telling"));
            bool firstTry = true;
            ChooseInRoom(T("stop.more.title", "What if they don't listen?"),
                T("stop.more.body", "Keza told the teacher, but the teacher said \"Not now.\" What should Keza do?"),
                Shuffle(
                    new PromptOption(T("stop.more.next", "Tell another grown-up"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        var parent = Spawn(T("stop.parent", "Mum"), Theme.Hex("#F28C38"), 1.0f, -20f, "mum");
                        parent.WalkTo(game.AR.PointAroundCharacter(0.5f, -20f), null);
                        Feedback(true, T("stop.more.fb", "Yes! Keza keeps telling until someone helps: a parent, a nurse, or the helpline 116."),
                            () => Finish(score, 5));
                    }),
                    new PromptOption(T("stop.more.quit", "Give up, nobody listens"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.more.quitfb", "Many grown-ups want to help. Keza tells the next one on the helping hand."), null);
                    }),
                    new PromptOption(T("stop.more.wait", "Wait and see if it happens again"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("stop.more.waitfb", "Keza doesn't have to wait. Telling early keeps Keza safe."), null);
                    })));
        }
    }
}
