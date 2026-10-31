using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Answers as places in the room: each answer is a glowing circle on the
    /// floor around the guide, with its words floating over it. The child
    /// moves the guide onto one (drags them, taps the circle so they walk
    /// there, or uses the arrow buttons). Stepping on a circle says "You
    /// picked: ..." and a ring fills; standing still on it chooses it. After
    /// a choice the guide walks back to the middle, so a wrong answer can
    /// simply be tried again. The circles use neutral colours, so the colour
    /// never gives the right answer away.
    /// </summary>
    public class ChoiceCircles : MonoBehaviour
    {
        const float Radius = 0.62f, Size = 0.34f, ConfirmSeconds = 1.1f;
        static readonly Color[] Palette =
        {
            Theme.Primary, Theme.Hex("#7A5CC7"), Theme.Hex("#2BA3A0"), Theme.Hex("#EE9B2F"),
        };

        class Circle
        {
            public PromptOption option;
            public Vector3 spot;
            public Color color;
            public GameObject root;
        }

        readonly List<Circle> circles = new List<Circle>();
        GameManager game;
        ARCharacter guide;
        Vector3 home, lastPosition;
        int candidate = -1;
        float stillSince;
        bool waitToLeave;
        GameObject picked, fill;

        public bool Active { get { return circles.Count > 0; } }

        public void Show(GameManager gameManager, ARCharacter character, PromptOption[] options)
        {
            Clear();
            game = gameManager;
            guide = character;
            home = guide.transform.position;
            int n = options.Length;
            float step = n <= 2 ? 60f : n == 3 ? 48f : 40f;
            float centre = ClearestSide(n, step);
            for (int i = 0; i < n; i++)
            {
                // Spread left to right around the guide, on the side with nobody standing there.
                float angle = centre + step * (i - (n - 1) * 0.5f);
                var spot = game.AR.PointAroundCharacter(Radius, angle);
                var color = Palette[i % Palette.Length];
                var root = ARFx.FloorQuad("Answer " + (i + 1), ARFx.SoftDot, Theme.WithAlpha(color, 0.75f), Size);
                root.transform.position = spot + Vector3.up * 0.005f;
                var ring = ARFx.FloorQuad("Ring", ARFx.Ring, color, Size * 1.15f);
                ring.transform.position = spot + Vector3.up * 0.006f;
                ring.transform.SetParent(root.transform, true);
                FxAnimator.Pulse(ring);
                var label = ARFx.Label(options[i].label, spot + Vector3.up * 0.24f, color, 0.85f);
                label.transform.SetParent(root.transform, true);
                if (game.AR.AnchorRoot != null) root.transform.SetParent(game.AR.AnchorRoot, true);
                circles.Add(new Circle { option = options[i], spot = spot, color = color, root = root });
            }
            candidate = -1;
            waitToLeave = false;
            lastPosition = guide.transform.position;
            stillSince = Time.time;
        }

        /// <summary>
        /// Where to put the answers: behind the guide (180, where the card
        /// can't cover them) unless people stand there; then the side that
        /// keeps every circle furthest from them.
        /// </summary>
        float ClearestSide(int n, float step)
        {
            var others = new List<Vector3>();
            foreach (var f in FindObjectsByType<ARFigure>(FindObjectsSortMode.None))
                if (f != null && f != guide) others.Add(f.transform.position);
            float best = 180f, bestGap = -1f;
            foreach (float centre in new[] { 180f, 140f, 220f, 100f, 260f, 0f })
            {
                float gap = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    var spot = game.AR.PointAroundCharacter(Radius, centre + step * (i - (n - 1) * 0.5f));
                    foreach (var o in others) gap = Mathf.Min(gap, Vector3.ProjectOnPlane(o - spot, Vector3.up).magnitude);
                }
                if (gap > 0.45f) return centre;   // the first side with room wins (behind is preferred)
                if (gap > bestGap) { bestGap = gap; best = centre; }
            }
            return best;
        }

        public void Clear()
        {
            ClearPick();
            foreach (var c in circles) if (c.root != null) Destroy(c.root);
            circles.Clear();
            candidate = -1;
        }

        /// <summary>Where the answer whose words contain text lies (for the demo and tests).</summary>
        public bool TryGetSpot(string text, out Vector3 spot)
        {
            foreach (var c in circles)
                if (c.option.label.Contains(text)) { spot = c.spot; return true; }
            spot = Vector3.zero;
            return false;
        }

        /// <summary>Tapping a circle sends the guide walking onto it.</summary>
        public bool HandleTap(ARTap tap)
        {
            if (!Active || !tap.hasFloor) return false;
            int i = CircleAt(tap.floor, Size * 0.75f);
            if (i < 0) return false;
            guide.WalkTo(circles[i].spot, null);
            return true;
        }

        int CircleAt(Vector3 point, float reach)
        {
            for (int i = 0; i < circles.Count; i++)
                if (Vector3.ProjectOnPlane(point - circles[i].spot, Vector3.up).magnitude <= reach) return i;
            return -1;
        }

        void Update()
        {
            if (!Active || guide == null) return;
            if ((guide.transform.position - lastPosition).sqrMagnitude > 0.00002f) stillSince = Time.time;
            lastPosition = guide.transform.position;

            int on = CircleAt(guide.transform.position, Size * 0.55f);
            if (waitToLeave)
            {
                if (on < 0) waitToLeave = false;   // off the last choice: ready for the next
                return;
            }
            if (on != candidate)
            {
                candidate = on;
                ClearPick();
                if (on >= 0) StepOn(on);
            }
            if (candidate < 0) return;

            float t = Mathf.Clamp01((Time.time - stillSince) / ConfirmSeconds);
            if (fill != null) fill.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, t) * Size * 1.3f;
            if (t >= 1f) Choose(candidate);
        }

        void StepOn(int i)
        {
            game.Audio.PlaySFX("tap");
            string line = string.Format(Localization.T("scenario.picked", "You picked: {0}"), circles[i].option.label);
            game.UI.Lesson.SetTouchHint(line + "  " + Localization.T("scenario.stay", "Stay to choose it."));
            picked = ARFx.Label(line, circles[i].spot + Vector3.up * 0.88f, circles[i].color, 1f);
            fill = ARFx.FloorQuad("Choosing", ARFx.Ring, Color.white, Size * 0.3f);
            fill.transform.position = circles[i].spot + Vector3.up * 0.008f;
            if (game.Reader != null) game.Reader.SpeakIfAuto(line);
        }

        void ClearPick()
        {
            if (picked != null) Destroy(picked);
            if (fill != null) Destroy(fill);
            picked = fill = null;
        }

        void Choose(int i)
        {
            var circle = circles[i];
            ClearPick();
            candidate = -1;
            waitToLeave = true;
            ARFx.Burst(circle.spot + Vector3.up * 0.1f, circle.color);
            game.UI.Lesson.SetTouchHint("");
            guide.WalkTo(home, null);   // back to the middle: a wrong answer can be tried again
            if (circle.option.onClick != null) circle.option.onClick();
        }

        void OnDisable()
        {
            Clear();
        }
    }
}
