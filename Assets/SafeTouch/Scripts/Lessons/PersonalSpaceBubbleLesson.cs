using System.Collections.Generic;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Lesson 1, Personal Space Bubble (FR3), played by touching the room:
    /// 1. The zones: the child taps the red, yellow and green circles on the
    ///    floor around the guide; each one pulses until found and then shows
    ///    a floating label.
    ///    The body: the child taps the guide to look closer, pinches to make
    ///    them bigger, drags to turn them, and taps glowing dots on the body
    ///    (green hands and shoulders, yellow face, red swimsuit area).
    /// 2-4. Visitors walk up and speak in speech bubbles: Grandma, a classmate
    ///    (a child model) and someone new, who the child sends back to the
    ///    green zone by saying STOP and dragging them there.
    /// Ages 9 to 11 play their own module, "My Boundaries", with the same
    /// ideas taught another way, in four steps:
    /// 1. Who goes where? Grandma, someone new and a neighbour who asked for a
    ///    secret wait outside the bubble; the child drags each one to where
    ///    they belong (knowing someone is not enough to come close).
    /// 2. The body, with the proper names.
    /// 3. Someone new comes too close: say STOP, send them back.
    /// 4. My bubble online.
    /// "Keza" in the text is the child's chosen name.
    /// </summary>
    public class PersonalSpaceBubbleLesson : LessonBase
    {
        // Zone radii in metres around Keza (true distances in the 3D world).
        public const float RedRadius = 0.3f;     // "my body": nobody comes in without my OK
        public const float YellowRadius = 0.6f;  // people I know and trust
        public const float GreenRadius = 0.9f;   // everyone else stays out here
        const float VisitorStart = 1.4f;

        enum Phase { Zones, Sort, Body, Visitors, SendBack }

        readonly Dictionary<string, GameObject> zones = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Color> zoneColors = new Dictionary<string, Color>();
        readonly Dictionary<string, GameObject> zoneLabels = new Dictionary<string, GameObject>();
        readonly HashSet<string> explored = new HashSet<string>();
        Phase phase;
        int score;
        GameObject lookRing;
        BodyMap bodyMap;
        Vector3 savedScale;
        Quaternion savedRotation;
        ARFigure sendBack;

        public override int Level { get { return 1; } }

        protected override void OnBegin()
        {
            score = 0;
            explored.Clear();
            zones.Clear();
            zoneLabels.Clear();
            var mat = game.AR.TransparentMaterial;
            AddZone("green", GreenRadius, Theme.WithAlpha(Theme.Safe, 0.25f), mat, 0.001f);
            AddZone("yellow", YellowRadius, Theme.WithAlpha(Theme.Caution, 0.3f), mat, 0.002f);
            AddZone("red", RedRadius, Theme.WithAlpha(Theme.Unsafe, 0.35f), mat, 0.003f);
            Narrate("bubble_intro");
            if (Older) { SortPeople(); return; }
            phase = Phase.Zones;
            ExploreZones(null);
        }

        void AddZone(string zone, float radius, Color color, Material mat, float height)
        {
            zones[zone] = keza.AddDisc(char.ToUpper(zone[0]) + zone.Substring(1) + " zone", radius, color, mat, height);
            zoneColors[zone] = color;
        }

        protected override void OnEnd()
        {
            game.AR.Touch.DragEnded -= Dropped;
            sorting.Clear();
            if (phase == Phase.Body) LeaveBody();
            foreach (var label in zoneLabels.Values) if (label != null) Destroy(label);
            zoneLabels.Clear();
            if (lookRing != null) Destroy(lookRing);
        }

        // ---- Step 1a: tap the zones on the floor ----

        /// <summary>The zone card. text is what the last tapped zone means; null shows the intro.</summary>
        void ExploreZones(string text)
        {
            Header(T("bubble.step1", "Step 1 of 4: the zones"));
            bool done = explored.Count == 3;
            TouchHint(done ? T("bubble.touch.look", "Tap {0} to look closer.") : T("bubble.touch.zones", "Tap the red, yellow and green circles on the floor."));
            string body = text ?? T("bubble.intro.body", "Everyone has a space bubble. Tap each colour on the floor around Keza.");
            if (done) body += "\n\n" + T("bubble.look.next", "Now tap Keza to look closer!");
            var options = new List<PromptOption>();
            if (done) options.Add(new PromptOption(T("bubble.look", "Look closer"), Theme.Primary, EnterBody));
            else options.Add(new PromptOption(T("bubble.help", "Show me"), Theme.Card, () => ExplainZone(NextZone(), null)));
            Prompt(T("bubble.intro.title", "Keza's space bubble"), body, options.ToArray());

            if (done && lookRing == null)
            {
                // A ring pulses under the guide: "tap me".
                lookRing = ARFx.FloorQuad("Look ring", ARFx.Ring, Theme.WithAlpha(Theme.Sun, 0.9f), RedRadius * 1.4f);
                lookRing.transform.position = keza.transform.position + Vector3.up * 0.008f;
                lookRing.transform.SetParent(keza.transform, true);
                FxAnimator.Pulse(lookRing);
            }
        }

        string NextZone()
        {
            foreach (var z in new[] { "red", "yellow", "green" }) if (!explored.Contains(z)) return z;
            return "red";
        }

        protected override bool OnTap(ARTap tap)
        {
            switch (phase)
            {
                case Phase.Zones: return TapZones(tap);
                case Phase.Sort:
                    if (tap.Find<ARFigure>() != keza || sorting.Values.Any(s => !s.done)) return false;
                    EnterBody();
                    return true;
                case Phase.Body: return TapBody(tap);
                default: return false;   // visitors wave, the floor ripples
            }
        }

        bool TapZones(ARTap tap)
        {
            var figure = tap.Find<ARFigure>();
            if (figure == keza)
            {
                if (explored.Count == 3) { EnterBody(); return true; }
                if (!explored.Contains("red")) { ExplainZone("red", null); return true; }   // the guide stands in the red zone
                return false;
            }
            if (!tap.hasFloor) return false;
            string zone = game.AR.ZoneAt(tap.floor);
            if (zone == "outside") return false;
            ARFx.Ripple(tap.floor, Theme.WithAlpha(zoneColors[zone], 1f));
            ExplainZone(zone, tap.floor);
            return true;
        }

        void ExplainZone(string zone, Vector3? where)
        {
            explored.Add(zone);
            game.Audio.PlaySFX("correct");
            LabelZone(zone, where);
            ShowZoneColours(zone);
            ExploreZones(ZoneText(zone));
        }

        /// <summary>A label floats over the zone, where the child touched it (or in front of the guide).</summary>
        void LabelZone(string zone, Vector3? where)
        {
            if (zoneLabels.ContainsKey(zone) && zoneLabels[zone] != null) Destroy(zoneLabels[zone]);
            float middle = zone == "red" ? RedRadius : zone == "yellow" ? (RedRadius + YellowRadius) * 0.5f : (YellowRadius + GreenRadius) * 0.5f;
            var centre = keza.transform.position;
            var toward = where.HasValue ? Vector3.ProjectOnPlane(where.Value - centre, Vector3.up) : Vector3.ProjectOnPlane(game.AR.CameraTransform.position - centre, Vector3.up);
            // The red label goes beside the guide at waist height, clear of the body and the name.
            if (zone == "red") toward = Vector3.ProjectOnPlane(game.AR.CameraTransform.right, Vector3.up);
            if (toward.sqrMagnitude < 0.0001f) toward = Vector3.forward;
            var spot = centre + toward.normalized * middle + Vector3.up * (zone == "red" ? 0.32f : 0.12f);
            var label = ARFx.Label(ZoneLabel(zone), spot, Theme.WithAlpha(zoneColors[zone], 1f), 0.8f);
            if (game.AR.AnchorRoot != null) label.transform.SetParent(game.AR.AnchorRoot, true);
            zoneLabels[zone] = label;
            ARFx.Burst(spot, Theme.WithAlpha(zoneColors[zone], 1f), 10);
        }

        // ---- Ages 9 to 11, step 1: who goes where? ----

        class Sortee
        {
            public string zone, why;
            public float angle;
            public bool done, missed;
        }

        readonly Dictionary<ARFigure, Sortee> sorting = new Dictionary<ARFigure, Sortee>();

        /// <summary>
        /// Instead of tapping the zones, the child places people: three wait outside the bubble and the child drags
        /// each one to where they belong. The labels are on the floor from the start; while a person is dragged,
        /// the hint says which zone they are in.
        /// </summary>
        void SortPeople()
        {
            phase = Phase.Sort;
            Header(T("bubble.g.step1", "Step 1 of 4: who goes where?"));
            LabelZone("red", null);
            LabelZone("yellow", game.AR.PointAroundCharacter(YellowRadius, -60f));
            LabelZone("green", game.AR.PointAroundCharacter(GreenRadius, 60f));
            sorting.Clear();
            AddSortee(T("bubble.grandma", "Grandma"), Theme.Sun, -125f, "yellow", "mum",
                T("bubble.sort.grandma", "Yes! Grandma is family Keza trusts: yellow. Keza can still say no to a hug."));
            AddSortee(T("bubble.stranger", "Someone new"), new Color(0.55f, 0.6f, 0.68f), 180f, "green", null,
                T("bubble.sort.stranger", "Yes! Keza doesn't know them, so they stay in green, or further."));
            AddSortee(T("bubble.neighbour", "Neighbour"), Theme.Hex("#5E8C6A"), 125f, "green", null,
                T("bubble.sort.neighbour", "Yes! Keza knows the neighbour, but they asked for a secret. Knowing someone isn't enough: green, and tell a grown-up."));
            game.AR.Touch.DragEnded -= Dropped;
            game.AR.Touch.DragEnded += Dropped;
            SortPrompt();
        }

        void AddSortee(string label, Color color, float angle, string zone, string role, string why)
        {
            // Grandma is family: a grown-up woman's model in Keza's skin tone ("mum"), never a stranger's.
            var figure = Spawn(label, color, VisitorStart, angle, role);
            sorting[figure] = new Sortee { zone = zone, why = why, angle = angle };
        }

        void SortPrompt()
        {
            TouchHint(T("bubble.sort.touch", "Drag each person into a zone."));
            int left = sorting.Values.Count(s => !s.done);
            Prompt(T("bubble.sort.title", "Who goes where?"),
                string.Format(T("bubble.sort.body", "Drag each person to where they should stand. {0} to go."), left),
                new PromptOption(T("bubble.help", "Show me"), Theme.Card, ShowNextSort));
        }

        /// <summary>"Show me": the next person walks to their zone by themselves (no point for that one).</summary>
        void ShowNextSort()
        {
            foreach (var pair in sorting)
            {
                if (pair.Value.done || pair.Key == null) continue;
                var figure = pair.Key;
                pair.Value.missed = true;
                float radius = pair.Value.zone == "yellow" ? (RedRadius + YellowRadius) * 0.5f : (YellowRadius + GreenRadius) * 0.5f;
                figure.WalkTo(game.AR.PointAroundCharacter(radius, pair.Value.angle), () => Dropped(figure));
                return;
            }
        }

        /// <summary>A person was put down: right, and they stay (with why); wrong, and the child thinks again.</summary>
        void Dropped(ARFigure figure)
        {
            Sortee s;
            if (!Running || phase != Phase.Sort || figure == null || !sorting.TryGetValue(figure, out s) || s.done) return;
            string zone = game.AR.ZoneOf(figure);
            if (zone == "red")
            {
                s.missed = true;
                Feedback(false, T("bubble.sort.red", "Red is only Keza's body. Nobody stands there without Keza's OK."), null);
                return;
            }
            bool right = zone == s.zone || (s.zone == "green" && zone == "outside");
            if (!right)
            {
                if (zone == "outside") return;   // not moved into the bubble yet
                s.missed = true;
                Feedback(false, T("bubble.sort.again", "Think again! Yellow is for people Keza trusts. Green is for everyone else."), null);
                return;
            }
            s.done = true;
            if (!s.missed) score++;
            game.AR.SetMovable(figure, false);
            figure.FaceTowards(keza.transform.position);
            figure.Bounce();
            ARFx.Burst(figure.transform.position + Vector3.up * 0.45f, zone == "yellow" ? Theme.Caution : Theme.Safe, 10);
            Feedback(true, s.why, () => { if (sorting.Values.All(x => x.done)) SortDone(); else SortPrompt(); });
        }

        void SortDone()
        {
            game.AR.Touch.DragEnded -= Dropped;
            TouchHint(T("bubble.touch.look", "Tap {0} to look closer."));
            Prompt(T("bubble.sort.done", "Everyone is in place!"), T("bubble.look.next", "Now tap Keza to look closer!"),
                new PromptOption(T("bubble.look", "Look closer"), Theme.Primary, EnterBody));
            if (lookRing != null) return;
            lookRing = ARFx.FloorQuad("Look ring", ARFx.Ring, Theme.WithAlpha(Theme.Sun, 0.9f), RedRadius * 1.4f);
            lookRing.transform.position = keza.transform.position + Vector3.up * 0.008f;
            lookRing.transform.SetParent(keza.transform, true);
            FxAnimator.Pulse(lookRing);
        }

        string ZoneText(string zone)
        {
            switch (zone)
            {
                case "red": return T("bubble.red.body", "Red is Keza's body. Nobody touches it without Keza's OK.");
                case "yellow": return T("bubble.yellow.body", "Yellow is for people Keza knows and trusts. Keza can still say no.");
                default: return T("bubble.green.body", "Green is for everyone else. People Keza doesn't know stay here.");
            }
        }

        string ZoneLabel(string zone)
        {
            switch (zone)
            {
                case "red": return T("bubble.label.red", "Red: my body");
                case "yellow": return T("bubble.label.yellow", "Yellow: people I trust");
                default: return T("bubble.label.green", "Green: everyone else");
            }
        }

        string Deeper(string zone)
        {
            switch (zone)
            {
                case "red": return T("bubble.red.deeper", "Private parts are what a swimsuit covers. Nobody should touch them, ask to see them, or show you theirs.");
                case "yellow": return T("bubble.yellow.deeper", "People you know must listen to your no too. If someone makes you feel strange, tell another grown-up.");
                default: return T("bubble.green.deeper", "Friendly people can still be strangers. People you only know online stay in green too.");
            }
        }

        /// <summary>The tapped zone glows; zones not found yet breathe (Update) so the child knows where to tap.</summary>
        void ShowZoneColours(string highlighted)
        {
            foreach (var pair in zones)
            {
                if (pair.Value == null) continue;
                var c = zoneColors[pair.Key];
                c.a = pair.Key == highlighted ? 0.6f : explored.Contains(pair.Key) ? 0.22f : c.a;
                pair.Value.GetComponent<Renderer>().material.color = c;
            }
        }

        void Update()
        {
            if (!Running) return;
            if (phase == Phase.Zones)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f);
                foreach (var pair in zones)
                {
                    if (explored.Contains(pair.Key) || pair.Value == null) continue;
                    var c = zoneColors[pair.Key];
                    c.a = Mathf.Lerp(c.a * 0.6f, Mathf.Min(0.75f, c.a * 2f), pulse);
                    pair.Value.GetComponent<Renderer>().material.color = c;
                }
            }
            else if (phase == Phase.SendBack && sendBack != null)
            {
                // Done once the person stands in the green zone or beyond.
                string zone = game.AR.ZoneOf(sendBack);
                if (zone == "green" || zone == "outside") SentBack();
            }
        }

        // ---- Step 1b: look closer at the body ----

        void EnterBody()
        {
            if (phase == Phase.Body) return;
            phase = Phase.Body;
            if (lookRing != null) Destroy(lookRing);
            foreach (var z in zones.Values) z.SetActive(false);
            foreach (var l in zoneLabels.Values) if (l != null) l.SetActive(false);

            savedScale = keza.transform.localScale;
            savedRotation = keza.transform.rotation;
            keza.FaceTowards(game.AR.CameraTransform.position);
            keza.ShowName(false);   // made bigger, the name would cover the screen's header
            if (Older) ClearFigures();   // the people placed in step 1 leave the room

            bodyMap = keza.GetComponent<BodyMap>();
            if (bodyMap == null) bodyMap = keza.gameObject.AddComponent<BodyMap>();
            bodyMap.Show(BodySpots());
            findQueue.Clear();

            game.AR.Touch.DragEnabled = false;
            game.AR.Touch.BeginManipulate(keza.transform, 1f, 2.6f);
            Header(Older ? T("bubble.g.step2", "Step 2 of 4: my body") : T("body.step", "Step 1 of 4: my body"));
            TouchHint(T("body.touch", "Pinch to make {0} bigger. Drag to turn. Tap the glowing dots."));
            Prompt(T("body.title", "Keza's body"),
                T("body.intro", "Tap each glowing dot. Green is okay with a yes. Red is private."));
        }

        bool TapBody(ARTap tap)
        {
            var marker = tap.Find<BodyMarker>();
            if (marker == null)
            {
                if (tap.Find<ARFigure>() == keza) keza.Bounce();
                return true;   // in this step a tap is only for the dots
            }
            if (finding != null) return FindTap(marker);
            if (findQueue.Count > 0) return true;   // a found dot is still celebrating
            bodyMap.Visit(marker);
            game.Audio.PlaySFX(marker.color == Theme.Unsafe ? "tap" : "correct");
            if (bodyMap.AllVisited)
            {
                Prompt(marker.title, marker.text + "\n\n" + T("body.names.why", "Knowing the names helps. When you tell, everyone understands you."),
                    new PromptOption(T("body.find.start", "Play Find it!"), Theme.Primary, StartFinding));
            }
            else
            {
                int left = 0, frontLeft = 0;
                foreach (var m in bodyMap.Markers)
                {
                    if (m.visited) continue;
                    left++;
                    if (m.id != "back" && m.id != "bottom") frontLeft++;
                }
                string more = frontLeft == 0
                    ? T("body.turn", "Turn Keza around: there are dots on the back!")
                    : string.Format(T("body.left", "{0} more to find."), left);
                Prompt(marker.title, marker.text + "\n" + more);
            }
            return true;
        }

        /// <summary>
        /// The body's parts by name, so the child can name them when they tell. Ages 6 to 8 hear gentle words:
        /// "private parts" (what underwear covers), "bottom" and "chest". Ages 9 to 11 hear the proper words
        /// (vulva and vagina, penis and testicles, breasts, buttocks and anus), as child-protection programmes
        /// advise: a child who knows the words can say clearly what happened.
        /// Only glowing dots on a clothed character are shown; the names are words, never pictures.
        /// For 9 to 11 the helper's body decides the words: a girl's for Keza, a boy's for Ganza.
        /// </summary>
        BodyMap.Spot[] BodySpots()
        {
            bool boy = Localization.BoyGuide;
            string privateTitle, privateText, chestTitle, chestText;
            if (!Older)
            {
                // Ages 6 to 8: gentle words, the same for a girl or a boy.
                privateTitle = T("body.young.private.name", "Private parts");
                privateText = T("body.young.private", "Private: the parts your underwear covers. Nobody touches them, looks at them or takes photos of them.");
            }
            else if (boy)
            {
                privateTitle = T("body.boy.private.older.name", "Penis and testicles");
                privateText = T("body.boy.private.older", "Private: a boy's penis and testicles. Nobody touches them, looks at them or takes photos of them.");
            }
            else
            {
                privateTitle = T("body.girl.private.older.name", "Vulva and vagina");
                privateText = T("body.girl.private.older", "Private: a girl's vulva and vagina. Nobody touches them, looks at them or takes photos of them.");
            }
            if (boy)
            {
                chestTitle = T("body.boy.chest.name", "Chest");
                chestText = T("body.boy.chest", "Ask first. Nobody touches your chest in a secret way.");
            }
            else
            {
                chestTitle = Older ? T("body.girl.chest.older.name", "Breasts") : T("body.girl.chest.name", "Chest");
                chestText = Older
                    ? T("body.girl.chest.older", "Private: a swimsuit covers them. Nobody touches them.")
                    : T("body.girl.chest", "Private: a swimsuit covers it. Nobody touches it.");
            }
            return new[]
            {
                new BodyMap.Spot("hands", Theme.Safe, T("body.hands.name", "Hands"),
                    T("body.hands.rule", "Okay: high-fives and handshakes, if you want them.")),
                new BodyMap.Spot("shoulders", Theme.Safe, T("body.shoulders.name", "Shoulders"),
                    T("body.shoulders.rule", "Okay with a yes: a pat can be fine. You can still say no.")),
                new BodyMap.Spot("head", Theme.Caution, T("body.head.name", "Face and hair"),
                    T("body.head.rule", "Ask first: people ask before they touch.")),
                new BodyMap.Spot("chest", boy ? Theme.Caution : Theme.Unsafe, chestTitle, chestText),
                new BodyMap.Spot("private", Theme.Unsafe, privateTitle, privateText),
                // On the back: the child turns the guide around to find these.
                new BodyMap.Spot("back", Theme.Safe, T("body.back.name", "Back"),
                    T("body.back.rule", "Okay with a yes: a pat on the back can be fine.")),
                new BodyMap.Spot("bottom", Theme.Unsafe, Older ? T("body.bottom.older.name", "Buttocks and anus") : T("body.bottom.name", "Bottom"),
                    Older ? T("body.bottom.older", "Private: nobody touches them or looks at them.") : T("body.bottom.rule", "Private: nobody touches it.")),
            };
        }

        // ---- Find it! A quick round: the app names a part, the child taps it ----

        readonly List<BodyMarker> findQueue = new List<BodyMarker>();
        BodyMarker finding;
        int found, findRounds;
        const int FindRounds = 5;

        /// <summary>Every private part first, then others, in a new order each time; the names come off the dots.</summary>
        void StartFinding()
        {
            findQueue.Clear();
            var seen = new HashSet<string>();
            foreach (var m in bodyMap.Markers.OrderBy(m => m.color == Theme.Unsafe ? 0 : 1).ThenBy(m => Random.value))
                if (seen.Add(m.id)) findQueue.Add(m);   // hands and shoulders have two dots: one is enough
            if (findQueue.Count > FindRounds) findQueue.RemoveRange(FindRounds, findQueue.Count - FindRounds);
            for (int i = findQueue.Count - 1; i > 0; i--)   // shuffled, so the private parts are not always first
            {
                int j = Random.Range(0, i + 1);
                var swap = findQueue[i];
                findQueue[i] = findQueue[j];
                findQueue[j] = swap;
            }
            found = 0;
            findRounds = findQueue.Count;
            bodyMap.HideLabels();
            game.Audio.PlaySFX("place");
            NextFind();
        }

        void NextFind()
        {
            if (phase != Phase.Body) return;
            if (findQueue.Count == 0)
            {
                finding = null;
                keza.Bounce();
                ARFx.Burst(keza.transform.position + Vector3.up * 0.5f, Theme.Sun);
                game.Audio.PlaySFX("badge");
                Prompt(string.Format(T("body.find.done.title", "{0} of {1}!"), found, findRounds),
                    T("body.find.done", "You know the names. Red dots are private. A doctor may check them only with a parent there."),
                    new PromptOption(T("body.done", "I know my body!"), Theme.Primary, () => LeaveBody(Older ? (System.Action)StrangerVisits : GrandmaVisits)));
                return;
            }
            finding = findQueue[0];
            findQueue.RemoveAt(0);
            bool behind = finding.id == "back" || finding.id == "bottom";
            string ask = string.Format(T("body.find", "Tap: {0}"), finding.title);
            Prompt(T("body.find.title", "Find it!"), ask + (behind ? "\n" + T("body.find.turn", "Hint: turn around.") : ""));
            TouchHint(ask);
        }

        bool FindTap(BodyMarker marker)
        {
            if (marker.id == finding.id)
            {
                found++;
                bodyMap.Visit(marker);
                game.Audio.PlaySFX("correct");
                keza.Bounce();
                finding = null;   // no double taps while the dot celebrates
                StartCoroutine(NextFindSoon());
            }
            else
            {
                game.Audio.PlaySFX("tryagain");
                FxAnimator.Shake(marker.gameObject);
                Prompt(T("body.find.title", "Find it!"), string.Format(T("body.find.wrong", "That is: {0}. Tap: {1}"), marker.title, finding.title));
            }
            return true;
        }

        System.Collections.IEnumerator NextFindSoon()
        {
            yield return new WaitForSeconds(0.9f);
            NextFind();
        }

        void LeaveBody(System.Action next = null)
        {
            finding = null;
            findQueue.Clear();
            if (bodyMap != null) bodyMap.Hide();
            game.AR.Touch.EndManipulate();
            game.AR.Touch.DragEnabled = true;
            keza.transform.localScale = savedScale;
            keza.transform.rotation = savedRotation;
            keza.ShowName(true);
            keza.FaceTowards(game.AR.CameraTransform.position);
            foreach (var z in zones.Values) if (z != null) z.SetActive(true);
            foreach (var l in zoneLabels.Values) if (l != null) Destroy(l);
            zoneLabels.Clear();
            ShowZoneColours(null);
            phase = Phase.Visitors;
            if (next != null) next();
        }

        // ---- Steps 2-4: visitors (ages 9 to 11: step 3 is someone new, step 4 online) ----

        void GrandmaVisits()
        {
            Header(T("bubble.step2", "Step 2 of 4: Grandma visits"));
            TouchHint(T("bubble.touch.drag", "Drag a person to see which zone they are in."));
            var grandma = Spawn(T("bubble.grandma", "Grandma"), Theme.Sun, VisitorStart, -110f);
            grandma.WalkTo(game.AR.PointAroundCharacter(YellowRadius, -110f), () =>
            {
                grandma.FaceTowards(keza.transform.position);
                grandma.Say(T("bubble.grandma.says", "Can I have a hug?"), game.AR.OpaqueMaterial);
                ChooseInRoom(T("bubble.grandma.title", "Grandma wants a hug"),
                    T("bubble.grandma.body", "What does Keza say?"),
                    new PromptOption(T("bubble.grandma.yes", "Yes, a hug!"), Theme.Safe, () =>
                    {
                        score++;
                        grandma.Say(null, null);
                        Feedback(true, T("bubble.grandma.yesfb", "Hugs are okay when Keza wants one."), ClassmateVisits);
                    }),
                    new PromptOption(T("bubble.grandma.no", "No thanks, let's wave"), Theme.Primary, () =>
                    {
                        score++;
                        grandma.Bounce();
                        grandma.Say(null, null);
                        Feedback(true, T("bubble.grandma.nofb", "Keza can say no, even to family. Grandma waves."), ClassmateVisits);
                    }));
            });
        }

        void ClassmateVisits()
        {
            Header(T("bubble.step3", "Step 3 of 4: a classmate"));
            // A child, not an adult: the Classmate model (SafeTouch > 4. Build Character Prefabs).
            var friend = Spawn(T("bubble.classmate", "Classmate"), Theme.Primary, VisitorStart, 110f, "child");
            bool firstTry = true;
            friend.WalkTo(game.AR.PointAroundCharacter(YellowRadius + 0.1f, 110f), () =>
            {
                friend.FaceTowards(keza.transform.position);
                friend.Say(T("bubble.classmate.says", "High-five!"), game.AR.OpaqueMaterial);
                ChooseInRoom(T("bubble.classmate.title", "A high-five?"),
                    T("bubble.classmate.body", "Keza's classmate wants a high-five. Is it safe?"),
                    Shuffle(
                        new PromptOption(T("bubble.classmate.yes", "Yes, high-five!"), Theme.Safe, () =>
                        {
                            if (firstTry) score++;
                            friend.Bounce();
                            friend.Say(null, null);
                            ARFx.Burst(Vector3.Lerp(keza.transform.position, friend.transform.position, 0.5f) + Vector3.up * 0.35f, Theme.Sun);
                            Feedback(true, T("bubble.classmate.yesfb", "A high-five from a friend is safe."), StrangerVisits);
                        }),
                        new PromptOption(T("bubble.classmate.no", "No, run away"), Theme.Unsafe, () =>
                        {
                            firstTry = false;
                            Feedback(false, T("bubble.classmate.nofb", "A high-five is a safe touch. Keza doesn't need to run. Try again."), null);
                        })));
            });
        }

        void StrangerVisits()
        {
            Header(Older ? T("bubble.g.step3", "Step 3 of 4: someone new") : T("bubble.step4", "Step 4 of 4: someone new"));
            var stranger = Spawn(T("bubble.stranger", "Someone new"), new Color(0.55f, 0.6f, 0.68f), VisitorStart, 150f);
            bool firstTry = true;
            stranger.WalkTo(game.AR.PointAroundCharacter(YellowRadius - 0.05f, 150f), () =>
            {
                stranger.FaceTowards(keza.transform.position);
                stranger.Say(T("bubble.stranger.says", "Hello! Come with me."), game.AR.OpaqueMaterial);
                ChooseInRoom(T("bubble.stranger.title", "Someone comes too close"),
                    T("bubble.stranger.body", "Keza doesn't know this person. What should Keza do?"),
                    Shuffle(
                        new PromptOption(T("bubble.stranger.stop", "Say \"Stop! Step back!\""), Theme.Unsafe, () =>
                        {
                            if (firstTry) score++;
                            stranger.Say(null, null);
                            // Right answer: now the child says it, so the words are practised, not just picked.
                            SayNo(T("bubble.stranger.say.title", "Say it out loud!"),
                                T("bubble.stranger.say.body", "Help Keza! Say \"STOP!\" clearly, like you mean it."),
                                T("bubble.stranger.say.button", "STOP!"),
                                rating =>
                                {
                                    keza.Bounce();
                                    SendBack(stranger);
                                });
                        }),
                        new PromptOption(T("bubble.stranger.wait", "Let them come closer"), Theme.Card, () =>
                        {
                            firstTry = false;
                            Feedback(false, T("bubble.stranger.waitfb", "People Keza doesn't know stay in green. Keza can say \"Stop!\" Try again."), null);
                        })));
            });
        }

        /// <summary>The child drags the person back out of the bubble, to the green zone.</summary>
        void SendBack(ARFigure stranger)
        {
            phase = Phase.SendBack;
            sendBack = stranger;
            game.AR.SetMovable(stranger, true);
            TouchHint(T("bubble.touch.sendback", "Drag the person to the green circle."));
            Prompt(T("bubble.sendback.title", "Keep the bubble safe"),
                T("bubble.sendback.body", "Drag the person back to the green zone."),
                new PromptOption(T("bubble.help", "Show me"), Theme.Card, () =>
                    stranger.WalkTo(game.AR.PointAroundCharacter(GreenRadius + 0.15f, 150f), null)));
        }

        void SentBack()
        {
            phase = Phase.Visitors;
            ARFx.Burst(sendBack.transform.position + Vector3.up * 0.4f, Theme.Safe);
            sendBack.WalkTo(game.AR.PointAroundCharacter(VisitorStart, 150f), null);
            sendBack = null;
            TouchHint(T("touch.hint", "Tap {0} to say hi. Drag people to move them."));
            Feedback(true, T("bubble.stranger.stopfb", "They stepped back! Next, Keza tells a trusted grown-up."),
                () => { if (Older) OnlineBubble(); else Finish(score, 3); });
        }

        /// <summary>Ages 9-11: the bubble applies online too (scenario S08 goes deeper in lesson 2).</summary>
        void OnlineBubble()
        {
            Header(T("bubble.g.step4", "Step 4 of 4: my bubble online"));
            var player = Spawn(T("bubble.online", "Game player"), new Color(0.55f, 0.48f, 0.78f), VisitorStart + 0.2f, -150f);
            player.Say(T("bubble.online.says", "Send me a photo!"), game.AR.OpaqueMaterial);
            bool firstTry = true;
            ChooseInRoom(T("bubble.online.title", "A message in a game"),
                T("bubble.online.body", "Someone Keza only knows online says: \"Send me a photo. Don't tell your parents.\" What should Keza do?"),
                Shuffle(
                    new PromptOption(T("bubble.online.tell", "Stop and show a grown-up"), Theme.Safe, () =>
                    {
                        if (firstTry) score++;
                        player.Say(null, null);
                        player.WalkTo(game.AR.PointAroundCharacter(VisitorStart + 0.6f, -150f), null);
                        Feedback(true, T("bubble.online.tellfb", "Yes! Asking for photos and secrets are warning signs. Keza shows a grown-up. Keza is not in trouble."),
                            () => Finish(score, 5));   // three people placed, someone new sent back, online
                    }),
                    new PromptOption(T("bubble.online.send", "Send it, they seem nice"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("bubble.online.sendfb", "Keza never sends photos to people known only online."), null);
                    }),
                    new PromptOption(T("bubble.online.secret", "Keep it a secret"), Theme.Card, () =>
                    {
                        firstTry = false;
                        Feedback(false, T("bubble.online.secretfb", "\"Don't tell your parents\" is a warning sign. Safe friends never ask that."), null);
                    })));
        }
    }
}
