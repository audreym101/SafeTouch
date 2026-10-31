using System.Collections.Generic;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Data;
using SafeTouch.UI;
using SafeTouch.UI.Screens;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 4, My Trusted Adults (FR7, FR8), played in the room. Every kind
    /// of grown-up stands in a half circle behind the guide; the child taps
    /// the ones they trust and those walk in close (tap again to send one
    /// back). With two or more, the helping hand is done, and the child taps
    /// who to tell first. Last, one gentle question: is there anyone who
    /// makes them feel unsafe or worried? Their parent can see the answer
    /// (Grown-ups > My child). Choices are saved on the phone as role ids
    /// only, and the unsafe answer is never synced.
    /// </summary>
    public class TrustedAdultsLesson : LessonBase
    {
        const float OuterRadius = 1.25f, InnerRadius = 0.75f;
        const float ArcFrom = 100f, ArcTo = 260f;   // the far side of the guide (180 = behind)

        int score;
        bool picking;
        readonly Dictionary<ARFigure, TrustedAdult> people = new Dictionary<ARFigure, TrustedAdult>();
        readonly Dictionary<ARFigure, float> angles = new Dictionary<ARFigure, float>();
        readonly List<ARFigure> invited = new List<ARFigure>();

        public override int Level { get { return 4; } }

        protected override void OnBegin()
        {
            game.UI.ShowMenu("lesson");
            people.Clear();
            angles.Clear();
            invited.Clear();
            helpers.Clear();
            var all = ContentLibrary.TrustedAdults;
            for (int i = 0; i < all.Count; i++)
            {
                float angle = all.Count == 1 ? 180f : ArcFrom + (ArcTo - ArcFrom) * i / (all.Count - 1);
                var adult = all[i];
                var figure = Spawn(adult.name, adult.GetColor(), OuterRadius, angle, adult.adultId);
                game.AR.SetMovable(figure, false);   // a tap invites them; they walk by themselves
                people[figure] = adult;
                angles[figure] = angle;
            }
            picking = true;
            BuildHand();
            TouchHint(T("adults.pick.touch", "Tap the grown-ups you trust."));
            RefreshPicking();
        }

        protected override void OnEnd()
        {
            if (hand != null) Destroy(hand);
            hand = null;
            fingers.Clear();
        }

        // ---- The helping hand: a glowing hand above the guide, one finger per helper ----

        GameObject hand;
        GameObject handLabel;
        readonly List<Renderer> fingers = new List<Renderer>();
        static readonly Color EmptyFinger = new Color(0.93f, 0.95f, 0.98f);

        /// <summary>
        /// A friendly 3D hand floating over the guide, always facing the phone:
        /// a palm and five fingers. Each grown-up the child invites lights one
        /// finger in their colour; a full hand of five is celebrated.
        /// </summary>
        void BuildHand()
        {
            hand = new GameObject("Helping hand");
            hand.transform.position = keza.transform.position + Vector3.up * 1.05f;
            hand.AddComponent<Billboard>();
            if (game.AR.AnchorRoot != null) hand.transform.SetParent(game.AR.AnchorRoot, true);

            Part(PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.2f, 0.07f), 0f);   // palm
            fingers.Clear();
            // Thumb low on the left, then four fingers fanned over the palm.
            float[] tilt = { -70f, -32f, -10f, 12f, 34f };
            Vector3[] at = { new Vector3(-0.13f, 0.01f, 0f), new Vector3(-0.075f, 0.14f, 0f), new Vector3(-0.025f, 0.16f, 0f), new Vector3(0.028f, 0.155f, 0f), new Vector3(0.078f, 0.135f, 0f) };
            for (int i = 0; i < 5; i++)
                fingers.Add(Part(PrimitiveType.Capsule, at[i], new Vector3(0.045f, i == 0 ? 0.055f : 0.065f, 0.045f), -tilt[i]));
            FxAnimator.Pulse(ARFx.FloorQuad("Hand glow", ARFx.SoftDot, Theme.WithAlpha(Theme.Sun, 0.5f), 0.5f)).transform.SetParent(hand.transform, false);
            hand.transform.Find("Hand glow").localRotation = Quaternion.identity;
            hand.transform.Find("Hand glow").localPosition = new Vector3(0f, 0.06f, 0.03f);
            RefreshHand();
        }

        Renderer Part(PrimitiveType type, Vector3 position, Vector3 scale, float zRotation)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(hand.transform, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            part.transform.localScale = scale;
            var r = part.GetComponent<Renderer>();
            r.material = game.AR.OpaqueMaterial != null ? new Material(game.AR.OpaqueMaterial) : r.material;
            r.material.color = EmptyFinger;
            return r;
        }

        /// <summary>Colours one finger per invited grown-up, in the order they were invited.</summary>
        void RefreshHand()
        {
            if (hand == null) return;
            for (int i = 0; i < fingers.Count; i++)
                fingers[i].material.color = i < invited.Count ? people[invited[i]].GetColor() : EmptyFinger;
            if (handLabel != null) Destroy(handLabel);
            handLabel = ARFx.Label(string.Format(T("adults.hand.count", "Helping hand: {0} of 5"), Mathf.Min(invited.Count, 5)),
                hand.transform.position - Vector3.up * 0.2f, Theme.Sun, 0.8f);
            handLabel.transform.SetParent(hand.transform, true);
        }

        /// <summary>A finger lights up: a pop and a sparkle; the fifth sets off the whole hand.</summary>
        void LightFinger(int index, Color color)
        {
            if (index < 0 || index >= fingers.Count) return;
            ARFx.Burst(fingers[index].transform.position, color, 10);
            StartCoroutine(Pop(fingers[index].transform));
            if (index == fingers.Count - 1)
            {
                ARFx.Burst(hand.transform.position, Theme.Sun, 24);
                foreach (var f in fingers) StartCoroutine(Pop(f.transform));
                keza.Bounce();
                game.Audio.PlaySFX("badge");
                game.UI.ShowToast(T("adults.hand.full", "A full helping hand!"));
            }
        }

        System.Collections.IEnumerator Pop(Transform part)
        {
            var size = part.localScale;
            for (float t = 0f; t < 1f && part != null; t += Time.deltaTime * 3f)
            {
                part.localScale = size * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f);
                yield return null;
            }
            if (part != null) part.localScale = size;
        }

        void RefreshPicking()
        {
            Header(string.Format(T("adults.step", "{0} helpers around Keza"), invited.Count));
            var options = invited.Count >= AdultsScreen.Minimum
                ? new[] { new PromptOption(T("adults.pick.done", "Done!"), Theme.Safe, PlaceAdults) }
                : new PromptOption[0];
            Prompt(T("adults.pick.title", "Who do you trust?"),
                invited.Count >= AdultsScreen.Minimum ? T("adults.pick.more", "Tap more, or press Done.") : T("adults.pick.body", "Tap at least 2 grown-ups."),
                options);
        }

        /// <summary>Invites a grown-up into the helping hand, or sends them back out.</summary>
        void Toggle(ARFigure figure)
        {
            var adult = people[figure];
            float angle = angles[figure];
            if (invited.Remove(figure))
            {
                figure.WalkTo(game.AR.PointAroundCharacter(OuterRadius, angle), () => figure.FaceTowards(keza.transform.position));
            }
            else
            {
                invited.Add(figure);
                figure.Bounce();
                ARFx.Burst(figure.transform.position + Vector3.up * 0.5f, adult.GetColor(), 10);
                game.Audio.PlaySFX("correct");
                if (game.Reader != null) game.Reader.SpeakIfAuto(adult.name);
                LightFinger(invited.Count - 1, adult.GetColor());
                // They walk in and high-five the guide.
                figure.WalkTo(game.AR.PointAroundCharacter(InnerRadius, angle), () =>
                {
                    if (figure == null || keza == null) return;
                    figure.FaceTowards(keza.transform.position);
                    figure.Bounce();
                    keza.Bounce();
                    ARFx.Burst(Vector3.Lerp(figure.transform.position, keza.transform.position, 0.5f) + Vector3.up * 0.4f, Theme.Sun, 8);
                });
            }
            RefreshHand();
            RefreshPicking();
        }

        void PlaceAdults()
        {
            picking = false;
            var chosen = invited.Select(f => people[f]).ToList();
            game.Progress.SetTrustedAdults(chosen.Select(a => a.adultId));
            Header(string.Format(T("adults.step", "{0} helpers around Keza"), chosen.Count));

            // The others leave the room; the helping hand stays around the guide.
            foreach (var pair in people)
            {
                var figure = pair.Key;
                if (figure == null || invited.Contains(figure)) continue;
                figure.WalkTo(game.AR.PointAroundCharacter(OuterRadius + 1f, angles[figure]), () => { if (figure != null) Destroy(figure.gameObject); });
            }
            helpers.Clear();
            foreach (var figure in invited)
            {
                ARFx.Burst(figure.transform.position + Vector3.up * 0.4f, people[figure].GetColor(), 8);
                people[figure].PlaceInAR();
                helpers[figure] = people[figure];
            }
            helperCount = chosen.Count;

            // The helpers stand around the guide: the child taps one (or a button).
            TouchHint(T("adults.first.touch", "Tap a helper in the room."));
            var options = chosen.Take(4).Select(a => new PromptOption(a.name, Theme.Card, () => ChoseFirst(a, chosen.Count))).ToArray();
            Prompt(T("adults.first.title", "Keza's helping hand"),
                T("adults.first.body", "If something felt wrong, who would Keza tell first?"),
                options);
        }

        readonly Dictionary<ARFigure, TrustedAdult> helpers = new Dictionary<ARFigure, TrustedAdult>();
        int helperCount;

        protected override bool OnTap(ARTap tap)
        {
            var figure = tap.Find<ARFigure>();
            if (figure == null) return false;
            if (picking && people.ContainsKey(figure)) { Toggle(figure); return true; }
            TrustedAdult adult;
            if (!helpers.TryGetValue(figure, out adult)) return false;
            figure.Bounce();
            ChoseFirst(adult, helperCount);
            return true;
        }

        void ChoseFirst(TrustedAdult adult, int count)
        {
            if (helpers.Count == 0) return;   // already chosen
            // The chosen helper comes right up to the guide.
            var first = helpers.FirstOrDefault(p => p.Value == adult).Key;
            if (first != null)
            {
                var beside = Vector3.Lerp(keza.transform.position, first.transform.position, 0.45f);
                first.WalkTo(beside, () =>
                {
                    if (first == null || keza == null) return;
                    first.FaceTowards(keza.transform.position);
                    keza.FaceTowards(first.transform.position);
                    keza.Bounce();
                    ARFx.Burst(beside + Vector3.up * 0.45f, adult.GetColor(), 14);
                });
            }
            helpers.Clear();
            TouchHint("");
            score = System.Math.Min(count, AdultsScreen.HelpingHand);
            string msg = string.Format(T("adults.first.fb", "Great! Keza tells {0}. If they don't listen, Keza tells the next one."), adult.name);
            Feedback(true, msg, AskUnsafe);
        }

        /// <summary>Not scored, and "nobody" is a fine answer.</summary>
        void AskUnsafe()
        {
            Header(T("adults.unsafe.step", "One more question"));
            Prompt(T("adults.unsafe.title", "How about you?"),
                T("adults.unsafe.body", "Is there anyone who makes you feel unsafe or worried?"),
                new PromptOption(T("adults.unsafe.no", "No, nobody"), Theme.Safe, () =>
                {
                    game.Progress.SetUnsafePeople(new string[0]);
                    Done(T("adults.unsafe.nobodyfb", "Good! If that changes, tell a grown-up you trust."));
                }),
                new PromptOption(T("adults.unsafe.yes", "Yes, someone"), Theme.Caution, () =>
                    game.UI.Adults.AskUnsafe(picked =>
                    {
                        game.UI.ShowMenu("lesson");
                        if (picked == null || picked.Count == 0) { AskUnsafe(); return; }   // went back: ask again
                        game.Progress.SetUnsafePeople(picked.Select(p => p.adultId));
                        Done(T("adults.unsafe.thanks", "Thank you for telling me. That was brave. It is not your fault. Your grown-up can see this in the app, so they can help. You can also tell a teacher, or call 116."));
                    })));
        }

        /// <summary>A calm message, not a celebration: this answer can be hard to give.</summary>
        void Done(string message)
        {
            game.UI.ShowFeedback("", message, FeedbackTone.Neutral, () => Finish(score, AdultsScreen.HelpingHand));
        }
    }
}
