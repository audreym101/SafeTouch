using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Data;
using SafeTouch.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 5, the Memory Quest: an end game that unlocks after the four
    /// lessons and brings back what the child learned, in four rounds.
    /// 1. Match the pairs (a card memory game: zones, secrets, and for ages
    ///    9-11 surprises and online strangers).
    /// 2. Build STOP in AR: letter blocks fly up above Keza as the child
    ///    picks each step in order.
    /// 3. My helping hand: the child's own trusted adults return and Keza
    ///    walks to the one the child picks.
    /// 4. Safe places chase: the child picks home, school or playground;
    ///    someone walks towards Keza three times, a little faster each time.
    ///    The child wins by tapping NO!, tapping the person, or walking Keza
    ///    to a helper with the move buttons. Faster answers earn more stars
    ///    and the fastest time unlocks the "Quick and Safe" badge.
    /// It ends with an "I remember!" certificate.
    /// </summary>
    public class ReviewLesson : LessonBase
    {
        static readonly string[] Letters = { "S", "T", "O", "P" };
        static readonly string[] StepKeys = { "stop.s.meaning", "stop.t.meaning", "stop.o.meaning", "stop.p.meaning" };
        static readonly string[] StepEnglish = { "Say no", "Tell a trusted adult", "Open up and explain", "Protect yourself" };

        static readonly float[] ChaseSpeeds = { 0.25f, 0.33f, 0.42f };   // metres per second
        static readonly float[] ChaseAngles = { 150f, 175f, 135f };
        const float HelperReach = 0.5f;   // Keza is safe when she is this close to a helper

        readonly List<GameObject> blocks = new List<GameObject>();
        readonly List<GameObject> sceneProps = new List<GameObject>();
        readonly List<ARFigure> placeHelpers = new List<ARFigure>();
        readonly List<float> chaseTimes = new List<float>();
        ARFigure chaser;
        string place = "home";
        int chaseIndex;
        float chaseStart;
        bool chasing;
        List<TrustedAdult> helpers = new List<TrustedAdult>();
        int score, max, nextLetter;
        bool firstTry;
        string helperName = "";

        public override int Level { get { return 5; } }

        protected override void OnBegin()
        {
            score = 0;
            nextLetter = 0;
            var pairs = ContentLibrary.ReviewPairs.Where(p => p.level <= (Older ? 2 : 1)).ToList();
            max = pairs.Count + Letters.Length + 1 + 3 * ChaseSpeeds.Length;
            chaseTimes.Clear();
            game.UI.ShowMenu("memory");
            game.UI.Memory.StartGame(pairs, (matched, turns) =>
            {
                score += matched;
                BuildStop();
            });
        }

        protected override void OnEnd()
        {
            chasing = false;
            foreach (var b in blocks) if (b != null) Destroy(b);
            blocks.Clear();
            foreach (var b in sceneProps) if (b != null) Destroy(b);
            sceneProps.Clear();
            if (chaser != null) Destroy(chaser.gameObject);
            chaser = null;
        }

        // ---- Round 2: build STOP ----

        void BuildStop()
        {
            game.UI.ShowMenu("lesson");
            Header(T("review.round2", "Round 2 of 4: build STOP"));
            Narrate("review_stop");
            var angles = new[] { 140f, 170f, 200f, 230f }.OrderBy(a => Random.value).ToArray();
            for (int i = 0; i < Letters.Length; i++)
            {
                var pos = game.AR.PointAroundCharacter(0.9f, angles[i]) + Vector3.up * 0.3f;
                blocks.Add(game.AR.SpawnSign(Letters[i], Theme.Unsafe, pos));
            }
            AskLetter(T("review.stop.title", "Build the word STOP"),
                T("review.stop.body", "The letters are scattered around Keza. Which step comes first?"));
        }

        void AskLetter(string heading, string body)
        {
            firstTry = true;
            var options = new List<PromptOption>();
            for (int i = nextLetter; i < Letters.Length; i++)
            {
                int pick = i;
                options.Add(new PromptOption(T(StepKeys[i], StepEnglish[i]), Theme.Card, () => Pick(pick)));
            }
            Prompt(heading, body, Shuffle(options.ToArray()));
        }

        void Pick(int index)
        {
            if (index != nextLetter)
            {
                firstTry = false;
                Feedback(false, string.Format(T("review.stop.hint", "Not yet. The next letter is {0}. What does {0} stand for?"), Letters[nextLetter]), null);
                return;
            }
            if (firstTry) score++;
            var block = blocks[index];
            var cam = game.AR.CameraTransform;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var target = keza.transform.position + Vector3.up * 0.85f + right * (index - 1.5f) * 0.23f;
            StartCoroutine(Fly(block.transform, target));
            keza.Bounce();
            game.Audio.PlaySFX("correct");
            nextLetter++;

            string done = string.Format(T("review.stop.got", "{0} is for {1}!"), Letters[index], T(StepKeys[index], StepEnglish[index]));
            if (nextLetter < Letters.Length)
                AskLetter(done, T("review.stop.next", "What comes next?"));
            else
                Feedback(true, T("review.stop.done", "S-T-O-P is complete above Keza: Say no, Tell, Open up, Protect. Say it out loud together!"), HelpingHand);
        }

        IEnumerator Fly(Transform block, Vector3 target)
        {
            var start = block.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime * 1.6f)
            {
                block.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.15f;
                yield return null;
            }
            block.position = target;
        }

        // ---- Round 3: my helping hand ----

        void HelpingHand()
        {
            Header(T("review.round3", "Round 3 of 4: my helping hand"));
            var ids = game.Progress.GetData().trustedAdults;
            helpers = ContentLibrary.TrustedAdults.Where(a => ids.Contains(a.adultId)).ToList();
            if (helpers.Count == 0) helpers = ContentLibrary.TrustedAdults.Take(2).ToList();

            const float arc = 200f;
            var figures = new List<AR.ARFigure>();
            for (int i = 0; i < helpers.Count; i++)
            {
                float angle = helpers.Count == 1 ? 180f : 180f - arc / 2f + arc * i / (helpers.Count - 1);
                figures.Add(Spawn(helpers[i].name, helpers[i].GetColor(), 0.8f, angle));
            }

            var options = new List<PromptOption>();
            for (int i = 0; i < helpers.Count && i < 4; i++)
            {
                var figure = figures[i];
                string name = helpers[i].name;
                options.Add(new PromptOption(name, Theme.Card, () =>
                {
                    score++;
                    helperName = name;
                    keza.WalkTo(Vector3.Lerp(keza.transform.position, figure.transform.position, 0.6f), () =>
                        Feedback(true, string.Format(T("review.hand.fb", "Keza goes to {0}. And if they don't listen, she tells the next helper on her hand."), name), PickPlace));
                }));
            }
            Prompt(T("review.hand.title", "Your helpers are back!"),
                T("review.hand.body", "Something is worrying Keza. Who can she go to?"), options.ToArray());
        }

        // ---- Round 4: safe places chase ----

        void PickPlace()
        {
            End3DRound();
            game.UI.ShowMenu("places");
            game.UI.Places.Ask(StartChase);
        }

        /// <summary>Clears round 2 and 3 objects so the chase starts with an empty room.</summary>
        void End3DRound()
        {
            foreach (var b in blocks) if (b != null) Destroy(b);
            blocks.Clear();
            ClearFigures();
        }

        void StartChase(string placeId)
        {
            place = placeId;
            chaseIndex = 0;
            game.UI.ShowMenu("lesson");
            placeHelpers.Clear();
            string[] names; Color[] colors;
            switch (place)
            {
                case "school":
                    names = new[] { T("places.teacher", "Teacher"), T("places.head", "Head teacher") };
                    colors = new[] { Theme.Primary, Theme.Hex("#5A8FD6") };
                    AddProp(PrimitiveType.Cube, 1.3f, 180f, new Vector3(0.6f, 0.4f, 0.04f), 0.5f, Theme.Hex("#2F5D4A"));   // chalkboard
                    break;
                case "playground":
                    names = new[] { T("places.izu", "IZU volunteer"), T("places.friendmum", "Friend's mum") };
                    colors = new[] { Theme.Safe, Theme.Hex("#2BA3A0") };
                    AddProp(PrimitiveType.Cylinder, 1.3f, 200f, new Vector3(0.06f, 0.3f, 0.06f), 0.3f, Theme.Hex("#8A6A4E")); // tree trunk
                    AddProp(PrimitiveType.Sphere, 1.3f, 200f, new Vector3(0.4f, 0.4f, 0.4f), 0.75f, Theme.Hex("#4E9A48"));    // tree top
                    break;
                default:
                    names = new[] { T("places.parent", "Mum or Dad"), T("places.grandparent", "Grandparent") };
                    colors = new[] { Theme.Hex("#F28C38"), Theme.Sun };
                    AddProp(PrimitiveType.Cube, 1.3f, 200f, new Vector3(0.6f, 0.2f, 0.25f), 0.1f, Theme.Hex("#C9725A"));    // sofa
                    break;
            }
            placeHelpers.Add(Spawn(names[0], colors[0], 0.8f, -115f));
            placeHelpers.Add(Spawn(names[1], colors[1], 0.8f, -50f));
            RunChase();
        }

        /// <summary>A simple scenery block so each place looks different in the room.</summary>
        void AddProp(PrimitiveType type, float radius, float angle, Vector3 scale, float height, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = game.AR.PointAroundCharacter(radius, angle) + Vector3.up * height;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (game.AR.OpaqueMaterial != null) r.material = new Material(game.AR.OpaqueMaterial);
            r.material.color = color;
            if (game.AR.AnchorRoot != null) go.transform.SetParent(game.AR.AnchorRoot, true);
            sceneProps.Add(go);
        }

        string ChaserName
        {
            get
            {
                return place == "home" ? T("places.visitor", "A visitor")
                    : place == "school" ? T("places.someone", "Someone")
                    : T("places.someonenew", "Someone new");
            }
        }

        string PlaceTip
        {
            get
            {
                return place == "home" ? T("places.home.tip", "Even someone Keza knows must stop when she says no.")
                    : place == "school" ? T("places.school.tip", "At school, Keza can always go to a teacher.")
                    : T("places.playground.tip", "Outside, Keza stays near people she trusts.");
            }
        }

        string PlaceLine
        {
            get
            {
                return place == "home" ? T("places.home.line", "Keza is playing at home.")
                    : place == "school" ? T("places.school.line", "Keza is at school at break time.")
                    : T("places.playground.line", "Keza is at the playground.");
            }
        }

        void RunChase()
        {
            if (chaser != null) Destroy(chaser.gameObject);
            chaser = game.AR.SpawnFigure(ChaserName, new Color(0.55f, 0.6f, 0.68f), 1.5f, ChaseAngles[chaseIndex]);
            game.AR.SetMovable(chaser, false);   // the move buttons are for Keza and the helpers, not the chaser
            chaser.WalkSpeed = ChaseSpeeds[chaseIndex];
            chaseStart = Time.time;
            chasing = true;
            Header(string.Format(T("review.chase.step", "Chase {0} of 3"), chaseIndex + 1));
            Prompt(T("review.chase.title", "Someone is coming too close!"),
                PlaceLine + " " + T("review.chase.body", "Say NO, tap the person, or walk Keza to a helper with the arrows."),
                new PromptOption(T("review.chase.no", "NO!"), Theme.Unsafe, () => Win(T("review.chase.said", "Keza said NO!"))));
        }

        void Update()
        {
            if (!Running || !chasing || chaser == null || keza == null) return;
            float elapsed = Time.time - chaseStart;
            Header(string.Format(T("review.chase.timer", "Chase {0} of 3  -  {1:0.0} s"), chaseIndex + 1, elapsed));

            // The person keeps heading for Keza, wherever the child walks her.
            var toKeza = Vector3.ProjectOnPlane(keza.transform.position - chaser.transform.position, Vector3.up);
            if (toKeza.magnitude <= PersonalSpaceBubbleLesson.RedRadius) { Caught(); return; }
            chaser.FaceTowards(keza.transform.position);
            chaser.transform.position += toKeza.normalized * chaser.WalkSpeed * Time.deltaTime;

            foreach (var helper in placeHelpers)
            {
                if (helper == null) continue;
                var gap = Vector3.ProjectOnPlane(helper.transform.position - keza.transform.position, Vector3.up);
                if (gap.magnitude <= HelperReach)
                {
                    Win(string.Format(T("review.chase.reached", "Keza reached {0}!"), helper.Label));
                    return;
                }
            }

            // Tapping the person in the camera view counts as saying "Stop!".
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var screen = pointer.position.ReadValue();
                RaycastHit hit;
                var cam = Camera.main;
                if (cam != null && Physics.Raycast(cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f)), out hit, 10f)
                    && hit.transform.IsChildOf(chaser.transform))
                    Win(T("review.chase.tapped", "Keza tapped them and said STOP!"));
            }
        }

        void Win(string how)
        {
            if (!chasing) return;
            chasing = false;
            float seconds = Time.time - chaseStart;
            int stars = seconds <= 3f ? 3 : seconds <= 6f ? 2 : 1;
            score += stars;
            chaseTimes.Add(seconds);
            game.Progress.RecordChase(seconds);
            keza.Bounce();
            chaser.Bounce();
            chaser.WalkTo(game.AR.PointAroundCharacter(1.7f, ChaseAngles[chaseIndex]), null);
            Feedback(true, string.Format(T("review.chase.win", "{0} You stopped them in {1:0.0} seconds: {2} stars."), how, seconds, stars) + " " + PlaceTip, () =>
            {
                chaseIndex++;
                if (chaseIndex < ChaseSpeeds.Length) RunChase();
                else Certificate();
            });
        }

        void Caught()
        {
            chasing = false;
            Feedback(false, T("review.chase.caught", "The person got into Keza's red zone. When someone comes too close: shout NO in a strong voice, move towards a trusted adult, and tell them. Let's try that chase again."), RunChase);
        }

        // ---- Certificate ----

        void Certificate()
        {
            var names = string.Join(", ", helpers.Select(h => h.name).ToArray());
            var ideas = new List<KeyValuePair<Color, string>>
            {
                new KeyValuePair<Color, string>(Theme.Unsafe, T("review.cert.bubble", "My bubble: red is my body, yellow is people I trust, green is everyone else.")),
                new KeyValuePair<Color, string>(Theme.Caution, T("review.cert.sort", "Touch can be safe, uncomfortable or unsafe. Touching secrets are always told.")),
                new KeyValuePair<Color, string>(Theme.Primary, T("review.cert.stop", "S-T-O-P: Say no, Tell, Open up, Protect.")),
                new KeyValuePair<Color, string>(Theme.Safe, string.Format(T("review.cert.hand", "My helpers: {0}."), names)),
                new KeyValuePair<Color, string>(Theme.Sun, string.Format(T("review.cert.chase", "Safe places chase: my fastest NO took {0:0.0} seconds."), chaseTimes.Count > 0 ? chaseTimes.Min() : 0f)),
            };
            game.UI.ShowMenu("certificate");
            game.UI.Certificate.Show(ideas, score, max, () => Finish(score, max));
        }
    }
}
