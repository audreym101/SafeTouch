using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Data;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 5, the Memory Quest: an end game that unlocks after the four
    /// lessons and brings back what the child learned, in four rounds.
    /// 1. Match the pairs (a card memory game: zones, secrets, and for ages
    ///    9-11 surprises and online strangers).
    /// 2. Grab STOP in AR: the letters hide around the room; the child walks
    ///    up to each one in order and taps it. It flies above Keza with its
    ///    meaning hanging under it.
    /// 3. My helping hand: the child's own trusted adults return and Keza
    ///    walks to the one the child taps.
    /// 4. Safe places chase: the child picks home, school or playground;
    ///    someone walks towards Keza three times, a little faster each time.
    ///    The child wins by saying NO out loud, tapping NO!, tapping the person, or dragging Keza
    ///    to a helper. Faster answers earn more stars
    ///    and the fastest time unlocks the "Quick and Safe" badge.
    /// It ends with an "I remember!" certificate.
    /// </summary>
    public class ReviewLesson : LessonBase
    {
        static readonly float[] ChaseSpeeds = { 0.25f, 0.33f, 0.42f };   // metres per second
        static readonly float[] ChaseAngles = { 150f, 175f, 135f };
        const float HelperReach = 0.5f;   // Keza is safe when she is this close to a helper

        readonly List<GameObject> sceneProps = new List<GameObject>();
        readonly List<ARFigure> placeHelpers = new List<ARFigure>();
        readonly List<float> chaseTimes = new List<float>();
        ARFigure chaser;
        string place = "home";
        int chaseIndex;
        float chaseStart;
        bool chasing;
        List<TrustedAdult> helpers = new List<TrustedAdult>();
        int score, max;
        bool firstTry;
        string helperName = "";

        public override int Level { get { return 5; } }

        protected override void OnBegin()
        {
            score = 0;
            var pairs = ContentLibrary.ReviewPairs.Where(p => p.level <= (Older ? 2 : 1)).ToList();
            max = pairs.Count + LetterHunt.Letters.Length + 1 + 3 * ChaseSpeeds.Length;
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
            if (hunt != null) hunt.Clear();
            foreach (var b in sceneProps) if (b != null) Destroy(b);
            sceneProps.Clear();
            if (chaser != null) Destroy(chaser.gameObject);
            chaser = null;
        }

        // ---- Round 2: grab STOP ----

        static readonly string[] StepWhy =
        {
            "Say NO in a clear, firm voice.",
            "Tell a grown-up you trust.",
            "Say what happened and how you felt.",
            "Go somewhere safe, near people you trust.",
        };
        LetterHunt hunt;
        float nextHintTime;

        /// <summary>
        /// The letters hide around the room (LetterHunt). The child moves to
        /// them, or moves the guide there: by dragging, tapping a letter, the
        /// arrow buttons, or the "Go to S" button.
        /// </summary>
        void BuildStop()
        {
            game.UI.ShowMenu("lesson");
            Header(T("review.round2", "Round 2 of 4: build STOP"));
            Narrate("review_stop");
            if (hunt == null) hunt = gameObject.AddComponent<LetterHunt>();
            hunt.Begin(game, keza, Grabbed, WrongLetter);
            firstTry = true;
            TouchHint(T("review.grab.touch", "Walk to the letter, or move {0} to it."));
            AskForNext(T("review.grab.title", "Find S-T-O-P!"), T("review.grab.body", "Go to S first."));
        }

        /// <summary>A card with a "Go to S" button, and the arrow buttons, for the next letter.</summary>
        void AskForNext(string title, string body)
        {
            string letter = LetterHunt.Letters[hunt.Next];
            Prompt(title, body, new PromptOption(string.Format(T("hunt.go", "Go to {0}"), letter), Theme.Card, hunt.TakeGuideToNext));
            MovePad(true);
        }

        void WrongLetter(int touched)
        {
            firstTry = false;
            AskForNext(T("review.grab.notyet.title", "Not yet!"),
                string.Format(T("review.grab.notyet.short", "First {0}. It's {1}."), LetterHunt.Letters[hunt.Next], hunt.Direction(hunt.Next)));
        }

        void Grabbed(int index)
        {
            if (firstTry) score++;
            firstTry = true;
            string title = string.Format(T("review.stop.got", "{0} is for {1}!"), LetterHunt.Letters[index], LetterHunt.Meaning(index));
            string why = T("review.why." + LetterHunt.Letters[index].ToLowerInvariant(), StepWhy[index]);
            if (!hunt.Done)
            {
                hunt.Hunting = true;
                AskForNext(title, why + "\n" + string.Format(T("review.grab.next.short", "Next: {0}."), LetterHunt.Letters[hunt.Next]));
                return;
            }
            MovePad(false);
            TouchHint("");
            Prompt(title, why);
            hunt.GuideHome(null);   // back under the finished word
            StartCoroutine(AfterSeconds(1.6f, () =>
                Feedback(true, T("review.stop.done", "S-T-O-P is above Keza: Say no, Tell, Open up, Protect. Say it together!"), HelpingHand)));
        }

        IEnumerator AfterSeconds(float seconds, System.Action then)
        {
            yield return new WaitForSeconds(seconds);
            if (Running) then();
        }

        protected override bool OnTap(ARTap tap)
        {
            if (hunt != null && hunt.HandleTap(tap)) return true;
            var tapped = tap.Find<ARFigure>();
            System.Action choose;
            if (tapped != null && handChoices.TryGetValue(tapped, out choose)) { choose(); return true; }
            // In the chase, tapping the person counts as saying "Stop!".
            if (chasing && chaser != null && tap.Touched(chaser.gameObject))
            {
                Win(T("review.chase.tapped", "Keza tapped them and said STOP!"));
                return true;
            }
            return false;
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
                figures.Add(Spawn(helpers[i].name, helpers[i].GetColor(), 0.8f, angle, helpers[i].adultId));
            }

            var options = new List<PromptOption>();
            handChoices.Clear();
            for (int i = 0; i < helpers.Count; i++)
            {
                var figure = figures[i];
                string name = helpers[i].name;
                System.Action choose = () =>
                {
                    if (handChoices.Count == 0) return;   // already chosen
                    handChoices.Clear();
                    score++;
                    helperName = name;
                    figure.Bounce();
                    keza.WalkTo(Vector3.Lerp(keza.transform.position, figure.transform.position, 0.6f), () =>
                    {
                        ARFx.Burst(figure.transform.position + Vector3.up * 0.5f, Theme.Safe);
                        Feedback(true, string.Format(T("review.hand.fb", "Keza goes to {0}. If they don't listen, Keza tells the next helper."), name), PickPlace);
                    });
                };
                handChoices[figure] = choose;
                if (i < 4) options.Add(new PromptOption(name, Theme.Card, () => choose()));
            }
            // The helpers stand in the room: the child can tap one, or use the buttons.
            TouchHint(T("review.hand.touch", "Tap a helper in the room."));
            Prompt(T("review.hand.title", "Your helpers are back!"),
                T("review.hand.body", "Keza is worried. Who can Keza go to?"), options.ToArray());
        }

        readonly Dictionary<ARFigure, System.Action> handChoices = new Dictionary<ARFigure, System.Action>();

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
            if (hunt != null) hunt.Clear();
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
                    AddModelProp("school", 1.2f, 205f);
                    if (!AddModelProp("chalkboard", 1.35f, 165f)) AddProp(PrimitiveType.Cube, 1.3f, 180f, new Vector3(0.6f, 0.4f, 0.04f), 0.5f, Theme.Hex("#2F5D4A"));   // chalkboard
                    break;
                case "playground":
                    names = new[] { T("places.izu", "IZU volunteer"), T("places.friendmum", "Friend's mum") };
                    colors = new[] { Theme.Safe, Theme.Hex("#2BA3A0") };
                    AddModelProp("grass", 0.9f, 150f);
                    if (!AddModelProp("playground", 1.4f, 190f))
                    {
                        AddProp(PrimitiveType.Cylinder, 1.3f, 200f, new Vector3(0.06f, 0.3f, 0.06f), 0.3f, Theme.Hex("#8A6A4E")); // tree trunk
                        AddProp(PrimitiveType.Sphere, 1.3f, 200f, new Vector3(0.4f, 0.4f, 0.4f), 0.75f, Theme.Hex("#4E9A48"));    // tree top
                    }
                    break;
                default:
                    names = new[] { T("places.parent", "Mum or Dad"), T("places.grandparent", "Grandparent") };
                    colors = new[] { Theme.Hex("#F28C38"), Theme.Sun };
                    AddProp(PrimitiveType.Cube, 1.3f, 200f, new Vector3(0.6f, 0.2f, 0.25f), 0.1f, Theme.Hex("#C9725A"));    // sofa
                    AddModelProp("dog", 0.75f, -150f);   // the family dog, near Mum or Dad
                    break;
            }
            placeHelpers.Add(Spawn(names[0], colors[0], 0.8f, -115f, place == "home" ? "parent" : null));
            placeHelpers.Add(Spawn(names[1], colors[1], 0.8f, -50f));
            // The child moves Keza to a helper, not the helper to Keza.
            foreach (var helper in placeHelpers) game.AR.SetMovable(helper, false);
            TouchHint(T("review.chase.touch", "Drag {0} to a helper, or tap the person who comes too close."));
            RunChase();
        }

        /// <summary>
        /// Places scenery built from the repo's models (Resources/Props/school
        /// and playground, made by SafeTouch > Build Character Prefabs).
        /// Returns false when the prop hasn't been built, so a block is used.
        /// </summary>
        bool AddModelProp(string propName, float radius, float angle)
        {
            var prefab = Resources.Load<GameObject>("Props/" + propName);
            if (prefab == null) return false;
            var go = Instantiate(prefab);
            go.transform.position = game.AR.PointAroundCharacter(radius, angle);
            var toKeza = Vector3.ProjectOnPlane(keza.transform.position - go.transform.position, Vector3.up);
            if (toKeza.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(toKeza);
            if (game.AR.AnchorRoot != null) go.transform.SetParent(game.AR.AnchorRoot, true);
            sceneProps.Add(go);
            return true;
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
                return place == "home" ? T("places.home.tip", "Even someone Keza knows must stop when Keza says no.")
                    : place == "school" ? T("places.school.tip", "At school, Keza can always go to a teacher.")
                    : T("places.playground.tip", "Outside, Keza stays near trusted people.");
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
            game.AR.SetMovable(chaser, false);   // the child can't simply drag the person away
            chaser.WalkSpeed = ChaseSpeeds[chaseIndex];
            chaseStart = Time.time;
            chasing = true;
            Header(string.Format(T("review.chase.step", "Chase {0} of 3"), chaseIndex + 1));
            bool listening = game.Voice != null && game.Voice.Available;
            Prompt(T("review.chase.title", "Someone is coming too close!"),
                PlaceLine + " " + (listening
                    ? T("review.chase.body.voice", "Say NO, tap the person, or drag Keza to a helper.")
                    : T("review.chase.body", "Tap NO, tap the person, or drag Keza to a helper.")),
                new PromptOption(T("review.chase.no", "NO!"), Theme.Unsafe, () => Win(T("review.chase.said", "Keza said NO!"))));

            // A spoken NO wins the chase too; the time to it is what earns the stars.
            if (listening)
                game.Voice.ListenContinuously(rating =>
                {
                    if (!chasing || !rating.Heard) return;
                    game.Progress.RecordNo(rating);
                    Win(T("review.chase.heard", "Keza heard your NO!"));
                });
        }

        void Update()
        {
            if (Running && hunt != null && hunt.Hunting && !hunt.Done && Time.time >= nextHintTime)
            {
                // The step line keeps pointing to the next letter as the child walks and turns.
                nextHintTime = Time.time + 0.5f;
                Header(string.Format(T("review.grab.step", "Round 2 of 4: find {0} ({1})"), LetterHunt.Letters[hunt.Next], hunt.Direction(hunt.Next)));
            }
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
        }

        void Win(string how)
        {
            if (!chasing) return;
            chasing = false;
            if (game.Voice != null) game.Voice.Stop();
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
            if (game.Voice != null) game.Voice.Stop();
            Feedback(false, T("review.chase.caught", "Too close! Say NO, go to a grown-up you trust, and tell them. Let's try again."), RunChase);
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
