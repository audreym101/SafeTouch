using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 1, Personal Space Bubble (FR3). Three coloured zones appear on
    /// the floor around Keza. The child explores each zone, then decides what
    /// Keza does when three visitors come close: family, a classmate and
    /// someone Keza does not know. Ages 9 to 11 also get a "think deeper"
    /// line on each zone and a fifth step about their bubble online.
    /// </summary>
    public class PersonalSpaceBubbleLesson : LessonBase
    {
        // Zone radii in metres around Keza (real-world scale in AR).
        public const float RedRadius = 0.3f;     // "my body": nobody comes in without my OK
        public const float YellowRadius = 0.6f;  // people I know and trust
        public const float GreenRadius = 0.9f;   // everyone else stays out here
        const float VisitorStart = 1.4f;

        readonly Dictionary<string, GameObject> zones = new Dictionary<string, GameObject>();
        readonly HashSet<string> explored = new HashSet<string>();
        int score;

        public override int Level { get { return 1; } }

        protected override void OnBegin()
        {
            score = 0;
            explored.Clear();
            zones.Clear();
            var mat = game.AR.TransparentMaterial;
            zones["green"] = keza.AddDisc("Green zone", GreenRadius, Theme.WithAlpha(Theme.Safe, 0.25f), mat, 0.001f);
            zones["yellow"] = keza.AddDisc("Yellow zone", YellowRadius, Theme.WithAlpha(Theme.Caution, 0.3f), mat, 0.002f);
            zones["red"] = keza.AddDisc("Red zone", RedRadius, Theme.WithAlpha(Theme.Unsafe, 0.35f), mat, 0.003f);
            ExploreZones();
        }

        void ExploreZones()
        {
            Header(Steps(T("bubble.step1", "Step 1 of 4: explore the zones")));
            Narrate("bubble_intro");
            var options = new List<PromptOption>
            {
                new PromptOption(T("bubble.red", "Red zone"), Theme.Unsafe, () => ExplainZone("red",
                    T("bubble.red.body", "The red zone is Keza's body. Nobody touches it without Keza's OK, and nobody touches private parts."))),
                new PromptOption(T("bubble.yellow", "Yellow zone"), Theme.Caution, () => ExplainZone("yellow",
                    T("bubble.yellow.body", "The yellow zone is for people Keza knows and trusts, like family. Keza can still say no."))),
                new PromptOption(T("bubble.green", "Green zone"), Theme.Safe, () => ExplainZone("green",
                    T("bubble.green.body", "The green zone is for everyone else. People Keza doesn't know stay out here."))),
            };
            if (explored.Count == 3)
                options.Add(new PromptOption(T("bubble.ready", "I'm ready!"), Theme.Primary, GrandmaVisits));
            Prompt(T("bubble.intro.title", "Meet Keza's space bubble"),
                T("bubble.intro.body", "Everyone has a bubble of space around their body. Tap each colour to learn about it."),
                options.ToArray());
        }

        /// <summary>Older children have five steps instead of four.</summary>
        string Steps(string header)
        {
            return Older ? header.Replace(" of 4", " of 5") : header;
        }

        void ExplainZone(string zone, string text)
        {
            explored.Add(zone);
            if (Older) text += "\n\n" + T("deeper", "Think deeper:") + " " + Deeper(zone);
            foreach (var pair in zones)
            {
                var r = pair.Value.GetComponent<Renderer>();
                var c = r.material.color;
                c.a = pair.Key == zone ? 0.6f : 0.2f;
                r.material.color = c;
            }
            game.UI.ShowFeedback("", text, FeedbackTone.Neutral, ExploreZones);
        }

        string Deeper(string zone)
        {
            switch (zone)
            {
                case "red": return T("bubble.red.deeper", "Private parts are the parts a swimsuit covers. Nobody should touch them, ask to see them or show you theirs. A doctor or nurse may check them only with a parent there, and they explain why.");
                case "yellow": return T("bubble.yellow.deeper", "Trusting someone doesn't mean they can do anything. If a person you know makes you feel confused or uneasy, that feeling is a signal to tell another adult.");
                default: return T("bubble.green.deeper", "People can be friendly and still be strangers. Someone you have only met online is in the green zone too, even if you chat every day.");
            }
        }

        void GrandmaVisits()
        {
            Header(Steps(T("bubble.step2", "Step 2 of 4: Grandma visits")));
            var grandma = Spawn(T("bubble.grandma", "Grandma"), Theme.Sun, VisitorStart, -110f);
            grandma.WalkTo(game.AR.PointAroundCharacter(YellowRadius, -110f), () =>
            {
                grandma.FaceTowards(keza.transform.position);
                Prompt(T("bubble.grandma.title", "Grandma asks for a hug"),
                    T("bubble.grandma.body", "\"Can I have a hug, Keza?\" What does Keza say?"),
                    new PromptOption(T("bubble.grandma.yes", "Yes, I'd like a hug"), Theme.Safe, () =>
                    {
                        score++;
                        Feedback(true, T("bubble.grandma.yesfb", "Hugs are okay when Keza wants them. It's Keza's choice."), ClassmateVisits);
                    }),
                    new PromptOption(T("bubble.grandma.no", "No thanks, let's wave"), Theme.Primary, () =>
                    {
                        score++;
                        grandma.Bounce();
                        Feedback(true, T("bubble.grandma.nofb", "Grandma waves instead. Keza can say no, even to family, and that's okay."), ClassmateVisits);
                    }));
            });
        }

        void ClassmateVisits()
        {
            Header(Steps(T("bubble.step3", "Step 3 of 4: a classmate")));
            var friend = Spawn(T("bubble.classmate", "Classmate"), Theme.Primary, VisitorStart, 110f);
            bool firstTry = true;
            friend.WalkTo(game.AR.PointAroundCharacter(YellowRadius + 0.1f, 110f), () =>
            {
                Prompt(T("bubble.classmate.title", "A classmate wants a high-five"),
                    T("bubble.classmate.body", "Keza's classmate is smiling and holds up a hand. Is a high-five safe?"),
                    Shuffle(
                        new PromptOption(T("bubble.classmate.yes", "Yes, high-five!"), Theme.Safe, () =>
                        {
                            if (firstTry) score++;
                            friend.Bounce();
                            Feedback(true, T("bubble.classmate.yesfb", "A high-five from a friend is a safe touch. Keza can still say no if she doesn't want one."), StrangerVisits);
                        }),
                        new PromptOption(T("bubble.classmate.no", "No, run away"), Theme.Unsafe, () =>
                        {
                            firstTry = false;
                            Feedback(false, T("bubble.classmate.nofb", "A high-five is a friendly, safe touch. Keza doesn't need to run. Try again."), null);
                        })));
            });
        }

        void StrangerVisits()
        {
            Header(Steps(T("bubble.step4", "Step 4 of 4: someone new")));
            var stranger = Spawn(T("bubble.stranger", "Someone new"), new Color(0.55f, 0.6f, 0.68f), VisitorStart, 150f);
            bool firstTry = true;
            stranger.WalkTo(game.AR.PointAroundCharacter(YellowRadius - 0.05f, 150f), () =>
            {
                Prompt(T("bubble.stranger.title", "Someone is coming very close"),
                    T("bubble.stranger.body", "Keza doesn't know this person, and they walked into the yellow zone. What should Keza do?"),
                    Shuffle(
                        new PromptOption(T("bubble.stranger.stop", "Say \"Stop! Please step back\""), Theme.Unsafe, () =>
                        {
                            if (firstTry) score++;
                            keza.Bounce();
                            stranger.WalkTo(game.AR.PointAroundCharacter(VisitorStart, 150f), null);
                            Feedback(true, T("bubble.stranger.stopfb", "Keza used a loud, clear voice and the person stepped back. Then Keza tells a trusted adult."),
                                () => { if (Older) OnlineBubble(); else Finish(score, 3); });
                        }),
                        new PromptOption(T("bubble.stranger.wait", "Let them come closer"), Theme.Card, () =>
                        {
                            firstTry = false;
                            Feedback(false, T("bubble.stranger.waitfb", "People Keza doesn't know should stay in the green zone. Keza can say \"Stop!\" Try again."), null);
                        })));
            });
        }

        /// <summary>Ages 9-11: the bubble applies online too (scenario S08 goes deeper in lesson 2).</summary>
        void OnlineBubble()
        {
            Header(T("bubble.step5", "Step 5 of 5: my bubble online"));
            var player = Spawn(T("bubble.online", "Game player"), new Color(0.55f, 0.48f, 0.78f), VisitorStart + 0.2f, -150f);
            bool firstTry = true;
            Prompt(T("bubble.online.title", "A message in a game"),
                T("bubble.online.body", "A player Keza only knows from an online game says: \"You're my best friend. Send me a photo of you and tell me your school. Don't tell your parents.\" What should Keza do?"),
                Shuffle(
                    new PromptOption(T("bubble.online.tell", "Stop chatting and show a trusted adult"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        player.WalkTo(game.AR.PointAroundCharacter(VisitorStart + 0.6f, -150f), null);
                        Feedback(true, T("bubble.online.tellfb", "Yes. Asking for photos, personal details and secrecy are three warning signs. Keza blocks them, keeps the messages and shows a trusted adult. She is not in trouble."),
                            () => Finish(score, 4));
                    }),
                    new PromptOption(T("bubble.online.send", "Send it, they seem nice"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("bubble.online.sendfb", "Someone known only online is in the green zone, however friendly they seem. Keza never sends photos or her school to them."), null);
                    }),
                    new PromptOption(T("bubble.online.secret", "Keep chatting, but keep it secret"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("bubble.online.secretfb", "\"Don't tell your parents\" is a warning sign. Safe friends never ask Keza to hide them from her family."), null);
                    })));
        }
    }
}
