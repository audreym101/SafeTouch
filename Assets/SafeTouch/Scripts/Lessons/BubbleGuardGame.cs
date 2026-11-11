using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Data;
using SafeTouch.Managers;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Bubble Guard, a replayable AR game that opens after the STOP lesson.
    /// Visitors walk towards Keza one at a time, each asking something in a
    /// speech bubble. If it is not okay, the child says NO before the visitor
    /// reaches Keza's red zone; if it is okay, the child taps OK. Visitors
    /// get faster as the game goes on, and Keza has three hearts.
    /// Saying NO to a safe request never costs a heart: children may always
    /// say no.
    /// Visitors bring what their trick needs (sweets, money, a phone, a puppy,
    /// a box, a car pulling up), so the child sees the trick coming; after a
    /// NO the trick is named ("Trick spotted: bribe!"). Ages 9 to 11 meet more
    /// tricks: a fake message from Mum, fake praise, game coins for photos,
    /// "everyone does it", being taken somewhere alone.
    /// </summary>
    public class BubbleGuardGame : LessonBase
    {
        const float SpawnRadius = 1.6f;
        const float SlowSpeed = 0.16f, FastSpeed = 0.34f;   // metres per second, first and last visitor
        const int StartHearts = 3;
        const float PauseBetweenVisitors = 2.4f;

        readonly List<GuardRequest> deck = new List<GuardRequest>();
        ScenarioStories props;   // what each visitor brings
        GuardRequest current;
        ARFigure visitor;
        int index, hearts, score, maxScore;
        bool answering;

        public override int Level { get { return ProgressManager.GameLevel; } }

        protected override void OnBegin()
        {
            score = 0;
            index = 0;
            hearts = StartHearts;
            answering = false;

            var mat = game.AR.TransparentMaterial;
            keza.AddDisc("Green zone", PersonalSpaceBubbleLesson.GreenRadius, Theme.WithAlpha(Theme.Safe, 0.25f), mat, 0.001f);
            keza.AddDisc("Yellow zone", PersonalSpaceBubbleLesson.YellowRadius, Theme.WithAlpha(Theme.Caution, 0.3f), mat, 0.002f);
            keza.AddDisc("Red zone", PersonalSpaceBubbleLesson.RedRadius, Theme.WithAlpha(Theme.Unsafe, 0.35f), mat, 0.003f);

            deck.Clear();
            deck.AddRange(ContentLibrary.GuardRequests
                .Where(r => r.level == (Older ? 2 : 1))   // each age group its own visitors and tricks
                .OrderBy(r => Random.value)
                .Take(Older ? 12 : 8));
            if (props == null) props = gameObject.AddComponent<ScenarioStories>();
            props.Setup(game, keza, (label, color, role) => Spawn(label, color, 1f, 0f, role));
            maxScore = deck.Count;
            if (Listening) maxScore += deck.Count(r => !r.safe);   // a strong-voice bonus is possible on each unsafe visitor

            Header(T("game.title", "Bubble Guard"));
            Prompt(T("game.intro.title", "Guard Keza's bubble!"),
                T("game.intro.body", "People will walk up to Keza and ask something. Not okay? Say NO before they reach red. Okay? Tap OK."),
                new PromptOption(T("game.start", "Start!"), Theme.Primary, NextVisitor));
        }

        bool Listening { get { return game.Voice != null && game.Voice.Available; } }

        /// <summary>Opens the microphone once for the whole game; every word heard answers the current visitor.</summary>
        void StartListening()
        {
            if (!Listening || game.Voice.Listening) return;
            game.Voice.ListenContinuously(rating =>
            {
                if (!Running || !answering || !rating.Heard) return;
                game.Progress.RecordNo(rating);
                Answer(true, rating);
            });
        }

        protected override void OnEnd()
        {
            if (props != null) props.Clear();
        }

        void NextVisitor()
        {
            ClearFigures();
            if (props != null) props.Clear();
            StartListening();
            if (index >= deck.Count || hearts <= 0) { EndGame(); return; }
            current = deck[index];
            float progress = deck.Count > 1 ? (float)index / (deck.Count - 1) : 0f;
            index++;

            float angle = Random.Range(120f, 240f);   // from the far side, where the child can see them
            visitor = Spawn(string.Format(T("game.visitor", "Visitor {0}"), index), new Color(0.55f, 0.6f, 0.68f), SpawnRadius, angle,
                current.who == "child" ? "child" : null);
            game.AR.SetMovable(visitor, false);
            visitor.WalkSpeed = Mathf.Lerp(SlowSpeed, FastSpeed, progress);
            props.Bring(current.prop, visitor);
            visitor.Say(current.text, game.AR.OpaqueMaterial);
            answering = true;
            heardHint = -1;
            ShowStatus();
            Prompt("\"" + current.text + "\"",
                Listening ? T("game.ask.voice", "Is that okay? If not, say NO out loud!") : T("game.ask", "Is that okay?"),
                new PromptOption(T("game.no", "NO!"), Theme.Unsafe, () => Answer(true, null)),
                new PromptOption(T("game.ok", "OK"), Theme.Safe, () => Answer(false, null)));
        }

        void ShowStatus()
        {
            Header(string.Format(T("game.status", "Visitor {0} of {1}  |  Hearts {2}  |  Score {3}"), index, deck.Count, hearts, score));
        }

        int heardHint = -1;   // which listening line shows: 0 listening, 1 hearing (changed only when it changes)

        void Update()
        {
            if (!Running || !answering || visitor == null || keza == null) return;
            // The microphone is on: the line above the card says so, and "I hear you!" while the child speaks.
            if (Listening && game.Voice.Listening)
            {
                int now = game.Voice.Hearing ? 1 : 0;
                if (now != heardHint)
                {
                    heardHint = now;
                    TouchHint(now == 1 ? T("voice.hearing.you", "I hear you!") : T("game.listening", "Listening: say NO out loud!"));
                }
            }
            var toKeza = Vector3.ProjectOnPlane(keza.transform.position - visitor.transform.position, Vector3.up);
            float distance = toKeza.magnitude;

            if (current.safe && distance <= PersonalSpaceBubbleLesson.YellowRadius) { Answer(false, null); return; }   // a friend just arrives
            if (!current.safe && distance <= PersonalSpaceBubbleLesson.RedRadius) { TooClose(); return; }

            visitor.FaceTowards(keza.transform.position);
            visitor.MoveBy(toKeza.normalized * visitor.WalkSpeed * Time.deltaTime);
        }

        /// <summary>voice is the spoken try that answered, or null for a tap.</summary>
        void Answer(bool saidNo, Voice.NoRating voice)
        {
            if (!answering) return;
            answering = false;
            visitor.Say(null, null);

            string result;
            if (!current.safe && saidNo)
            {
                score++;
                keza.Bounce();
                // A spoken NO: Keza says she heard it, so the child knows the app heard them.
                if (voice != null && voice.Heard) keza.SayFor(T("voice.heard.keza", "I heard your NO!"), game.AR.OpaqueMaterial, 2f);
                result = T("game.result.no", "They walked away!");
                if (voice != null && voice.stars == 3)
                {
                    score++;
                    result = T("game.result.strong", "Strong voice bonus! They walked away fast.");
                }
                else if (voice != null) result += " " + VoiceTips.For(voice.tip);
                string trick = TrickName(current.trick);
                if (trick != null)
                {
                    result += "\n" + string.Format(T("game.trick", "Trick spotted: {0}!"), trick);
                    ShowTrick(trick);
                }
                game.Audio.PlaySFX("correct");
                Jolt(0.25f);
                LeaveFast();
            }
            else if (!current.safe)
            {
                hearts--;
                result = T("game.result.missed", "That one was not okay.");
                string trick = TrickName(current.trick);
                if (trick != null) result += "\n" + string.Format(T("game.trick.missed", "It was a trick: {0}."), trick);
                game.Audio.PlaySFX("tryagain");
                Jolt(0.6f);
                LeaveFast();
            }
            else if (!saidNo)
            {
                score++;
                visitor.Bounce();
                result = T("game.result.ok", "Yes, that's safe!");
                game.Audio.PlaySFX("correct");
            }
            else
            {
                result = T("game.result.safeNo", "You can always say no. That one was safe, though.");
                LeaveFast();
            }
            Explain(result);
        }

        /// <summary>The trick's name in the child's language, or null when the visitor uses none.</summary>
        string TrickName(string trick)
        {
            switch (trick)
            {
                case "secret": return T("trick.secret", "a secret");
                case "bribe": return T("trick.bribe", "a bribe");
                case "lure": return T("trick.lure", "a lure");
                case "fake": return T("trick.fake", "a fake message");
                case "flattery": return T("trick.flattery", "fake praise");
                case "threat": return T("trick.threat", "a threat");
                case "online": return T("trick.online", "an online trick");
                case "pressure": return T("trick.pressure", "pressure");
                case "alone": return T("trick.alone", "getting you alone");
                case "game": return T("trick.game", "a touching game");
                default: return null;
            }
        }

        /// <summary>The trick's name pops up over the visitor in red, then fades away with them.</summary>
        void ShowTrick(string trick)
        {
            if (visitor == null) return;
            float top = visitor.transform.position.y + 0.7f;   // over the head, whatever the model
            foreach (var r in visitor.GetComponentsInChildren<Renderer>())
                if (r.GetComponent<TextMesh>() == null) top = Mathf.Max(top, r.bounds.max.y + 0.2f);
            var label = ARFx.Label(trick.ToUpperInvariant(), new Vector3(visitor.transform.position.x, top, visitor.transform.position.z), Theme.Unsafe, game.AR.UsesAR ? 1f : 2.2f);
            FxAnimator.Pulse(label);
            ARFx.Burst(visitor.transform.position + Vector3.up * 0.4f, Theme.Unsafe);
            Destroy(label, 1.8f);
        }

        void Jolt(float strength)
        {
            if (game.AR.Stage != null) game.AR.Stage.Shake(strength);
        }

        void TooClose()
        {
            answering = false;
            hearts--;
            Jolt(0.6f);
            visitor.Say(null, null);
            game.Audio.PlaySFX("tryagain");
            LeaveFast();
            Explain(T("game.result.close", "Too close! Say NO sooner, while they are still in green."));
        }

        void LeaveFast()
        {
            visitor.WalkSpeed = 0.6f;
            visitor.WalkTo(game.AR.PointAroundCharacter(SpawnRadius + 0.4f, Random.Range(120f, 240f)), null);
        }

        /// <summary>Shows what happened and why, then brings the next visitor without a tap.</summary>
        void Explain(string result)
        {
            ShowStatus();
            Prompt(result, current.explain);
            StartCoroutine(NextSoon());
        }

        IEnumerator NextSoon()
        {
            yield return new WaitForSeconds(PauseBetweenVisitors);
            NextVisitor();
        }

        void EndGame()
        {
            answering = false;
            Finish(score, maxScore);
        }
    }
}
