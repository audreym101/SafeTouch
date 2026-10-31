using System.Collections;
using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.Data;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 2, Safe or Unsafe? (FR4, FR5), answered by moving in the room.
    /// Each scenario card floats above the guide, and three colours lie on
    /// the floor behind them: green Safe, yellow Uncomfortable, red Unsafe.
    /// The child moves the guide onto a colour (drags them, or taps the
    /// colour and the guide walks there). Stepping on it says "You picked:
    /// Unsafe" and a ring fills; staying there confirms the choice, and the
    /// card flies into the colour. Then the guide walks home for the next
    /// card. Every answer is saved for the pre/post evaluation.
    /// </summary>
    public class ScenarioLesson : LessonBase
    {
        static readonly string[] Letters = { "A", "B", "C" };
        // Behind the guide (180 = far side), where the prompt card can't cover them;
        // Safe on the left of the screen, Unsafe on the right.
        static readonly float[] BasketAngles = { 222f, 180f, 138f };
        const float BasketRadius = 0.6f, BasketSize = 0.34f;
        const float ConfirmSeconds = 1.2f;   // standing on a colour this long chooses it

        readonly List<GameObject> baskets = new List<GameObject>();
        readonly List<Vector3> basketSpots = new List<Vector3>();
        readonly List<Color> basketColors = new List<Color>();
        readonly List<string> basketNames = new List<string>();
        readonly List<GameObject> flyingCards = new List<GameObject>();
        GameObject card, picked, fillRing;
        Vector3 home, lastGuidePosition;
        int candidate = -1;
        float stillSince;
        bool answering;
        int correct;

        public override int Level { get { return 2; } }

        protected override void OnBegin()
        {
            correct = 0;
            int difficulty = Older ? 2 : 1;
            game.Scenarios.LoadScenarios(difficulty);
            home = keza.transform.position;
            BuildBaskets();
            TouchHint(T("scenario.touch.step", "Move {0} onto a colour."));
            ShowCard();
        }

        protected override void OnEnd()
        {
            answering = false;
            foreach (var b in baskets) if (b != null) Destroy(b);
            baskets.Clear();
            basketSpots.Clear();
            basketColors.Clear();
            basketNames.Clear();
            ClearPick();
            if (card != null) Destroy(card);
            // A card still flying into its colour when the child leaves would stay in the room.
            foreach (var f in flyingCards) if (f != null) Destroy(f);
            flyingCards.Clear();
        }

        void BuildBaskets()
        {
            Color[] colors = { Theme.Safe, Theme.Caution, Theme.Unsafe };
            // The colours use the cards' own answer words, so they match the language.
            var first = game.Scenarios.Current;
            string[] names = first != null
                ? new[] { first.optionA, first.optionB, first.optionC }
                : new[] { "Safe", "Uncomfortable", "Unsafe" };
            for (int i = 0; i < 3; i++)
            {
                var spot = game.AR.PointAroundCharacter(BasketRadius, BasketAngles[i]);
                var basket = ARFx.FloorQuad("Sort " + names[i], ARFx.SoftDot, Theme.WithAlpha(colors[i], 0.8f), BasketSize);
                basket.transform.position = spot + Vector3.up * 0.005f;
                var ring = ARFx.FloorQuad("Ring", ARFx.Ring, colors[i], BasketSize * 1.15f);
                ring.transform.position = spot + Vector3.up * 0.006f;
                ring.transform.SetParent(basket.transform, true);
                FxAnimator.Pulse(ring);
                var label = ARFx.Label(names[i], spot + Vector3.up * 0.2f, colors[i], 0.9f);
                label.transform.SetParent(basket.transform, true);
                if (game.AR.AnchorRoot != null) basket.transform.SetParent(game.AR.AnchorRoot, true);
                baskets.Add(basket);
                basketSpots.Add(spot);
                basketColors.Add(colors[i]);
                basketNames.Add(names[i]);
            }
        }

        void ShowCard()
        {
            var s = game.Scenarios.Current;
            if (s == null) { Finish(0, 0); return; }
            Header(string.Format(T("scenario.step", "Card {0} of {1}"), game.Scenarios.CurrentIndex + 1, game.Scenarios.Count));
            if (s.audioClip != null) game.Audio.PlayVoice(s.audioClip);

            // The card floats over the guide's home spot, in the room.
            if (card != null) Destroy(card);
            card = ARFx.Label(s.title, home + Vector3.up * 0.95f, Theme.Primary, 1f);
            if (game.AR.AnchorRoot != null) card.transform.SetParent(game.AR.AnchorRoot, true);

            // Just the question: the answer is a place in the room, not a button.
            Prompt(s.title, s.GetQuestion());
            lastGuidePosition = keza.transform.position;
            stillSince = Time.time;
            candidate = -1;
            answering = true;
        }

        /// <summary>Tapping a colour sends the guide walking onto it.</summary>
        protected override bool OnTap(ARTap tap)
        {
            if (!answering || !tap.hasFloor) return false;
            int i = BasketAt(tap.floor, BasketSize * 0.75f);
            if (i < 0) return false;
            keza.WalkTo(basketSpots[i], null);
            return true;
        }

        int BasketAt(Vector3 point, float reach)
        {
            for (int i = 0; i < basketSpots.Count; i++)
                if (Vector3.ProjectOnPlane(point - basketSpots[i], Vector3.up).magnitude <= reach) return i;
            return -1;
        }

        void Update()
        {
            if (!Running || !answering || keza == null) return;

            // Has the guide moved this frame? Standing still on a colour is what confirms it.
            if ((keza.transform.position - lastGuidePosition).sqrMagnitude > 0.00002f) stillSince = Time.time;
            lastGuidePosition = keza.transform.position;

            int on = BasketAt(keza.transform.position, BasketSize * 0.55f);
            if (on != candidate)
            {
                candidate = on;
                ClearPick();
                if (on >= 0) StepOn(on);
                else TouchHint(T("scenario.touch.step", "Move {0} onto a colour."));
            }
            if (candidate < 0) return;

            // The ring fills while the guide stands on the colour.
            float t = Mathf.Clamp01((Time.time - stillSince) / ConfirmSeconds);
            if (fillRing != null) fillRing.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, t) * BasketSize * 1.3f;
            if (t >= 1f) Answer(Letters[candidate]);
        }

        /// <summary>"You picked: Unsafe" - said and shown as soon as the guide steps on a colour.</summary>
        void StepOn(int i)
        {
            game.Audio.PlaySFX("tap");
            string line = string.Format(T("scenario.picked", "You picked: {0}"), basketNames[i]);
            TouchHint(line + "  " + T("scenario.stay", "Stay to choose it."));
            // Above the guide's head and name, where the guide can't cover it.
            picked = ARFx.Label(line, basketSpots[i] + Vector3.up * 0.88f, basketColors[i], 1.1f);
            fillRing = ARFx.FloorQuad("Choosing", ARFx.Ring, Color.white, BasketSize * 0.3f);
            fillRing.transform.position = basketSpots[i] + Vector3.up * 0.008f;
            if (game.Reader != null) game.Reader.SpeakIfAuto(line);
        }

        void ClearPick()
        {
            if (picked != null) Destroy(picked);
            if (fillRing != null) Destroy(fillRing);
            picked = fillRing = null;
        }

        void Answer(string letter)
        {
            if (!answering) return;
            answering = false;
            var s = game.Scenarios.Current;
            bool right = game.Scenarios.CheckAnswer(letter);
            if (right) correct++;
            game.Progress.RecordScenario(s, letter, right);

            int index = System.Array.IndexOf(Letters, letter);
            ClearPick();
            if (card != null && index >= 0)
            {
                flyingCards.Add(card);
                StartCoroutine(Drop(card, basketSpots[index], right));
            }
            else if (card != null) Destroy(card);
            card = null;
            if (right) keza.Bounce();

            string message = s.GetFeedback(Older, T("deeper", "Think deeper:"));
            if (!right)
                message = string.Format(T("scenario.answer", "The answer is: {0}."), s.GetOptionText(s.correctAnswer)) + " " + message;
            Feedback(right, message, NextCard);
        }

        /// <summary>The card flies into the chosen colour and bursts there.</summary>
        IEnumerator Drop(GameObject flying, Vector3 spot, bool right)
        {
            var start = flying.transform.position;
            var end = spot + Vector3.up * 0.05f;
            var startScale = flying.transform.localScale;
            for (float t = 0f; t < 1f && flying != null; t += Time.deltaTime * 2.2f)
            {
                flying.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.12f;
                flying.transform.localScale = startScale * Mathf.Lerp(1f, 0.3f, t);
                yield return null;
            }
            if (flying == null) yield break;
            ARFx.Burst(end, right ? Theme.Sun : Theme.Caution);
            flyingCards.Remove(flying);
            Destroy(flying);
        }

        /// <summary>The guide walks home, then the next card appears.</summary>
        void NextCard()
        {
            if (game.Scenarios.IsLast)
            {
                Finish(correct, game.Scenarios.Count);
                return;
            }
            game.Scenarios.Next();
            TouchHint(T("scenario.touch.step", "Move {0} onto a colour."));
            keza.WalkTo(home, () => { if (Running) ShowCard(); });
        }
    }
}
