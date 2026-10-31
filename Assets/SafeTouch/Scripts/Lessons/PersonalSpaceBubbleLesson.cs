using System.Collections.Generic;
using SafeTouch.AR;
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
    /// Ages 9 to 11 also get "think deeper" lines and a fifth step about
    /// their bubble online. "Keza" in the text is the child's chosen name.
    /// </summary>
    public class PersonalSpaceBubbleLesson : LessonBase
    {
        // Zone radii in metres around Keza (real-world scale in AR).
        public const float RedRadius = 0.3f;     // "my body": nobody comes in without my OK
        public const float YellowRadius = 0.6f;  // people I know and trust
        public const float GreenRadius = 0.9f;   // everyone else stays out here
        const float VisitorStart = 1.4f;

        enum Phase { Zones, Body, Visitors, SendBack }

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
            if (phase == Phase.Body) LeaveBody();
            foreach (var label in zoneLabels.Values) if (label != null) Destroy(label);
            zoneLabels.Clear();
            if (lookRing != null) Destroy(lookRing);
        }

        // ---- Step 1a: tap the zones on the floor ----

        /// <summary>The zone card. text is what the last tapped zone means; null shows the intro.</summary>
        void ExploreZones(string text)
        {
            Header(Steps(T("bubble.step1", "Step 1 of 4: the zones")));
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

        /// <summary>Older children have five steps instead of four.</summary>
        string Steps(string header)
        {
            return Older ? header.Replace(" of 4", " of 5") : header;
        }

        protected override bool OnTap(ARTap tap)
        {
            switch (phase)
            {
                case Phase.Zones: return TapZones(tap);
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
            string text = ZoneText(zone);
            if (Older) text += "\n" + T("deeper", "Think deeper:") + " " + Deeper(zone);

            // A label floats over the zone, where the child touched it (or in front of the guide).
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

            ShowZoneColours(zone);
            ExploreZones(text);
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

            bodyMap = keza.GetComponent<BodyMap>();
            if (bodyMap == null) bodyMap = keza.gameObject.AddComponent<BodyMap>();
            bodyMap.Show(new[]
            {
                new BodyMap.Spot("hands", Theme.Safe, T("body.hands.title", "High-five!"),
                    T("body.hands", "Hands: high-fives and handshakes are safe touches, if Keza wants them.")),
                new BodyMap.Spot("shoulders", Theme.Safe, T("body.shoulders.title", "Okay with a yes"),
                    T("body.shoulders", "Shoulders: a pat can be okay. Keza can still say no.")),
                new BodyMap.Spot("head", Theme.Caution, T("body.head.title", "Ask first"),
                    T("body.head", "Face and hair: people should ask before they touch.")),
                new BodyMap.Spot("chest", Theme.Unsafe, T("body.chest.title", "Private"),
                    T("body.chest", "The chest is private. A swimsuit covers it. Nobody touches it.")),
                new BodyMap.Spot("private", Theme.Unsafe, T("body.private.title", "Private"),
                    T("body.private", "Between the legs and the bottom are private. Nobody touches them, looks at them or takes photos.")),
                // On the back: the child turns the guide around to find these.
                new BodyMap.Spot("back", Theme.Safe, T("body.back.title", "Okay with a yes"),
                    T("body.back", "Back: a pat on the back can be okay, if Keza says yes.")),
                new BodyMap.Spot("bottom", Theme.Unsafe, T("body.bottom.title", "Private"),
                    T("body.bottom", "The bottom is private. Nobody touches it.")),
            });

            game.AR.Touch.DragEnabled = false;
            game.AR.Touch.BeginManipulate(keza.transform, 1f, 2.6f);
            Header(Steps(T("body.step", "Step 1 of 4: my body")));
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
            bodyMap.Visit(marker);
            game.Audio.PlaySFX(marker.color == Theme.Unsafe ? "tap" : "correct");
            if (bodyMap.AllVisited)
            {
                Prompt(marker.title, marker.text + "\n\n" + T("body.summary", "Red dots are private: the parts a swimsuit covers. A doctor may check them only with a parent there."),
                    new PromptOption(T("body.done", "I know my body!"), Theme.Primary, () => LeaveBody(GrandmaVisits)));
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

        void LeaveBody(System.Action next = null)
        {
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

        // ---- Steps 2-4: visitors ----

        void GrandmaVisits()
        {
            Header(Steps(T("bubble.step2", "Step 2 of 4: Grandma visits")));
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
            Header(Steps(T("bubble.step3", "Step 3 of 4: a classmate")));
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
            Header(Steps(T("bubble.step4", "Step 4 of 4: someone new")));
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
            Header(T("bubble.step5", "Step 5 of 5: my bubble online"));
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
                            () => Finish(score, 4));
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
