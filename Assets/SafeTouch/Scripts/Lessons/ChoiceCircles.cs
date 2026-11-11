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
    /// Each answer is solid and easy to see: a raised coloured pad on the
    /// floor and a sign on a post, in the answer's colour with big white
    /// letters (A, B...) and its words. Tapping the sign or the pad sends the
    /// guide there; the lesson card shows the same answers as big buttons in
    /// the same colours (ButtonFor), which do the same.
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
            public Transform sign;
        }

        /// <summary>On an answer's sign, so a tap on it finds the answer.</summary>
        public class Sign : MonoBehaviour
        {
            public int index;
        }

        static readonly string[] Letters = { "A", "B", "C", "D" };
        readonly List<Material> made = new List<Material>();

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
            float step = n <= 2 ? 72f : n == 3 ? 52f : 42f;
            float centre = ClearestSide(n, step);
            // No sign is wider than the gap to the next answer, so neighbours never cover each other.
            float gap = n > 1 ? 2f * Radius * Mathf.Sin(step * 0.5f * Mathf.Deg2Rad) * 0.9f : 1f;
            for (int i = 0; i < n; i++)
            {
                // Spread left to right around the guide, on the side with nobody standing there.
                float angle = centre + step * (i - (n - 1) * 0.5f);
                var spot = game.AR.PointAroundCharacter(Radius, angle);
                var color = Palette[i % Palette.Length];
                var root = new GameObject("Answer " + (i + 1));
                root.transform.position = spot;
                var glow = ARFx.FloorQuad("Glow", ARFx.SoftDot, Theme.WithAlpha(color, 0.75f), Size * 1.3f);
                glow.transform.position = spot + Vector3.up * 0.005f;
                glow.transform.SetParent(root.transform, true);
                var ring = ARFx.FloorQuad("Ring", ARFx.Ring, color, Size * 1.15f);
                ring.transform.position = spot + Vector3.up * 0.026f;
                ring.transform.SetParent(root.transform, true);
                FxAnimator.Pulse(ring);
                Solid(PrimitiveType.Cylinder, root.transform, "Pad", spot + Vector3.up * 0.012f, new Vector3(Size * 0.9f, 0.012f, Size * 0.9f), color, i);
                var sign = SignPost(root.transform, spot, Letters[i % Letters.Length], options[i].label, color, i, gap);
                if (game.AR.AnchorRoot != null) root.transform.SetParent(game.AR.AnchorRoot, true);
                circles.Add(new Circle { option = options[i], spot = spot, color = color, root = root, sign = sign });
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
                if (gap > 0.38f) return centre;   // the first side with room wins (behind is preferred: side answers line up one behind another)
                if (gap > bestGap) { bestGap = gap; best = centre; }
            }
            return best;
        }

        public void Clear()
        {
            ClearPick();
            foreach (var c in circles) if (c.root != null) Destroy(c.root);
            circles.Clear();
            foreach (var m in made) if (m != null) Destroy(m);
            made.Clear();
            candidate = -1;
        }

        /// <summary>
        /// The same answer as a big, solid button on the lesson card, in its circle's colour with its letter:
        /// tapping it sends the guide onto that answer, which then chooses it.
        /// </summary>
        public PromptOption ButtonFor(int i)
        {
            var c = circles[i];
            return new PromptOption(c.option.label, c.color, () => Go(i));   // the card puts the same letter in a circle on it
        }

        public int Count { get { return circles.Count; } }

        /// <summary>The guide walks onto answer i (it is chosen once they stand there).</summary>
        public void Go(int i)
        {
            if (i < 0 || i >= circles.Count || guide == null) return;
            waitToLeave = false;
            guide.WalkTo(circles[i].spot, null);
        }

        /// <summary>
        /// A sign on a post beside the answer's pad: a solid board in the answer's colour, its letter in a white
        /// badge and its words in big white letters. It turns to face the phone (only around the post).
        /// </summary>
        Transform SignPost(Transform parent, Vector3 spot, string letter, string text, Color color, int index, float maxWidth)
        {
            float s = (game.AR.UsesAR ? 1f : 1.7f) * UI.TextSize.WorldScale;
            string wrapped = ARFx.Wrap(text, 12);
            var lines = wrapped.Split('\n');
            int longest = 0;
            foreach (var l in lines) longest = Mathf.Max(longest, l.Length);
            float width = Mathf.Clamp(0.1f + longest * 0.0215f, 0.26f, 0.42f) * s;
            float height = (0.06f + 0.052f * lines.Length) * s;
            // Too wide for the gap: the whole sign (board, letter and words) is drawn a little smaller.
            if (width > maxWidth)
            {
                float k = maxWidth / width;
                s *= k;
                width = maxWidth;
                height *= k;
            }
            // Every other sign stands higher, so even close signs never hide each other's words.
            float lift = (index % 2 == 0 ? 0.2f : 0.2f + 0.5f * height / s) * s;

            var root = new GameObject("Sign " + letter).transform;
            root.SetParent(parent, false);
            root.position = spot;
            Solid(PrimitiveType.Cylinder, root, "Post", spot + Vector3.up * lift * 0.5f, new Vector3(0.016f * s, lift * 0.5f, 0.016f * s), Theme.Hex("#5B6472"), index);
            var board = Solid(PrimitiveType.Cube, root, "Board", spot + Vector3.up * (lift + height * 0.5f), new Vector3(width, height, 0.025f * s), color, index);
            board.AddComponent<BoxCollider>();
            board.AddComponent<Sign>().index = index;
            // A darker edge under the board, so it reads as a solid plate.
            Solid(PrimitiveType.Cube, root, "Edge", spot + Vector3.up * (lift + 0.006f * s), new Vector3(width, 0.012f * s, 0.03f * s), Color.Lerp(color, Color.black, 0.35f), index);

            // The letter in a white badge at the left, the words in white bold letters.
            var badge = Solid(PrimitiveType.Cylinder, root, "Badge", spot + Vector3.up * (lift + height * 0.5f) - Vector3.forward * 0.016f * s, new Vector3(0.06f * s, 0.006f * s, 0.06f * s), Color.white, index);
            badge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            badge.transform.position = spot + Vector3.up * (lift + height * 0.5f) + Vector3.left * (width * 0.5f - 0.04f * s) - Vector3.forward * 0.016f * s;
            Text(root, letter, badge.transform.position - Vector3.forward * 0.008f * s, 0.0075f * s, color, TextAnchor.MiddleCenter);
            Text(root, wrapped, spot + Vector3.up * (lift + height * 0.5f) + Vector3.right * 0.035f * s - Vector3.forward * 0.016f * s, 0.0068f * s, Color.white, TextAnchor.MiddleCenter);
            return root;
        }

        GameObject Solid(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Color color, int index)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            var source = game.AR.OpaqueMaterial;
            var m = source != null ? new Material(source) : new Material(Shader.Find("Standard"));
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.45f);
            made.Add(m);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static void Text(Transform parent, string text, Vector3 position, float size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Words");
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = ARFx.Font;
            go.GetComponent<MeshRenderer>().sharedMaterial = ARFx.Font.material;
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = size;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
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
            if (!Active) return false;
            var sign = tap.Find<Sign>();
            if (sign != null) { Go(sign.index); return true; }
            if (!tap.hasFloor) return false;
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
            // The signs turn to face the phone, around their posts only.
            var cam = game.AR.CameraTransform;
            foreach (var c in circles)
            {
                if (c.sign == null || cam == null) continue;
                var look = Vector3.ProjectOnPlane(c.sign.position - cam.position, Vector3.up);
                if (look.sqrMagnitude > 0.0001f) c.sign.rotation = Quaternion.LookRotation(look);
            }
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
