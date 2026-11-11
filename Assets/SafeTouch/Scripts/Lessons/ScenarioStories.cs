using System;
using System.Collections;
using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.Lessons
{
    /// <summary>
    /// Short 3D stories for the Safe or Unsafe? cards, so the child sees each
    /// situation happen instead of only reading it: a car pulls up and a
    /// stranger offers sweets, Grandma comes for a hug, a cousin will not stop
    /// tickling. People speak in speech bubbles (read aloud when that is on).
    ///
    /// After the child sorts an unsafe or uncomfortable card, ActFor gives an
    /// "act it out" question: what should Keza do? The safe choice then plays
    /// out (say NO and run to Mum, tell a grown-up, step back), so the child
    /// rehearses the response, not just the label.
    ///
    /// Everything is placed in front of the phone's view, in metres from Keza:
    /// x to the right of the screen, z away from the phone. The props are
    /// simple shapes, so there are no model files; in the 3D world, stories
    /// at school play in the classroom (Stage3D.ShowClassroom), and the camera
    /// jolts and moves in at the moments that matter (UhOh, Watch, Jolt).
    ///
    /// Some stories happen after dark (cards marked "night": late after school,
    /// lost at the market, someone following): the 3D world turns to night
    /// for them (Stage3D.SetStoryNight), with street lamps, a lit shop and car
    /// headlights, so the child practises being safe at night too.
    /// </summary>
    public class ScenarioStories : MonoBehaviour
    {
        /// <summary>What Keza should do after an unsafe or uncomfortable card, and how the safe choice plays out.</summary>
        public class ActOut
        {
            public string question, right, wrong, wrongNote, praise;
            public bool sayNo;                 // the child says NO out loud first (voice coach)
            public Func<IEnumerator> play;     // the safe choice, acted out
        }

        GameManager game;
        ARCharacter guide;
        Func<string, Color, string, ARFigure> spawn;
        float guideSpeed;
        Vector3 home, right, away;

        readonly List<GameObject> props = new List<GameObject>();
        readonly List<ARFigure> cast = new List<ARFigure>();
        readonly Dictionary<Color, Material> paints = new Dictionary<Color, Material>();

        // People and things a story leaves for its act-out (null if the child skipped before they came).
        ARFigure other, friend;
        GameObject car, held;

        static readonly Color Grandma = Theme.Hex("#9B7BD1"), Cousin = Theme.Hex("#F08A3C"), Neighbour = Theme.Hex("#5E8C6A"),
            Nurse = Color.white, Stranger = Theme.Hex("#55606E"), Classmate = Theme.Hex("#3D8FD8"), Teacher = Theme.Hex("#8E5CC2"),
            Visitor = Theme.Hex("#2BA3A0"), Uncle = Theme.Hex("#2F4A7A"), Friend = Theme.Hex("#E86FA8"), Mum = Theme.Hex("#D9534F");

        public void Setup(GameManager gameManager, ARCharacter character, Func<string, Color, string, ARFigure> spawner)
        {
            game = gameManager;
            guide = character;
            spawn = spawner;
            guideSpeed = guide.WalkSpeed;
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }

        /// <summary>True when this card has a story to watch.</summary>
        public bool Has(string scenarioId)
        {
            return Story(scenarioId) != null;
        }

        /// <summary>Plays the card's story around Keza, who stands at home.</summary>
        public IEnumerator Play(string scenarioId, Vector3 homeSpot)
        {
            RemoveAll();   // not Clear: that would also stop this story when it runs on this component
            Orient(homeSpot);
            // Night for the stories that happen after dark, day for the others (the lesson goes back to the child's choice when it ends).
            var card = Data.ContentLibrary.Scenarios.Find(s => s.scenarioId == scenarioId);
            if (game.AR.Stage != null) game.AR.Stage.SetStoryNight(card != null && card.night);
            var story = Story(scenarioId);
            if (story != null) yield return story;
        }

        /// <summary>Keza's spot, and which ways are right and away as the phone sees them.</summary>
        void Orient(Vector3 homeSpot)
        {
            home = homeSpot;
            var cam = game.AR.CameraTransform.position;
            away = Vector3.ProjectOnPlane(home - cam, Vector3.up).normalized;
            if (away == Vector3.zero) away = Vector3.forward;
            right = Vector3.Cross(Vector3.up, away);
        }

        /// <summary>
        /// For Bubble Guard: what a visitor brings, so the child sees the trick and does not only read it:
        /// "sweets", "phone", "money", "box", "ball" or "puppy" in their hands or at their feet, or "car": a car
        /// drives up behind them. Removed with Clear.
        /// </summary>
        public void Bring(string what, ARFigure visitor)
        {
            if (visitor == null || string.IsNullOrEmpty(what)) return;
            Orient(guide.transform.position);
            switch (what)
            {
                case "sweets": held = Sweets(visitor); break;
                case "phone": held = Glowing(SmallPhone(visitor)); break;
                case "money": held = Money(visitor); break;
                case "box": held = Box(visitor); break;
                case "ball": held = Ball(visitor); break;
                case "puppy": held = Puppy(visitor); break;
                case "car":
                    car = Car(Theme.Hex("#2E4B73"));
                    var behind = visitor.transform.position + Vector3.ProjectOnPlane(visitor.transform.position - home, Vector3.up).normalized * 0.45f;
                    game.Audio.PlaySFX("car");
                    StartCoroutine(Drive(car, behind - right * 5f, behind, 1.8f, true));
                    break;
            }
            game.Audio.PlaySFX("pop");
        }

        /// <summary>Stops the story, removes its people and props and gives Keza her normal walking speed back.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            RemoveAll();
        }

        void RemoveAll()
        {
            RedFlagUp = false;
            foreach (var f in cast) if (f != null) Destroy(f.gameObject);
            cast.Clear();
            foreach (var p in props) if (p != null) Destroy(p);
            props.Clear();
            other = friend = null;
            car = held = null;
            goal = null;
            if (game != null && game.AR.Stage != null) game.AR.Stage.ShowClassroom(false);
            if (guide != null)
            {
                guide.WalkSpeed = guideSpeed;
                guide.Say(null, null);
            }
        }

        void OnDestroy()
        {
            foreach (var m in paints.Values) if (m != null) Destroy(m);
            foreach (var m in shiny.Values) if (m != null) Destroy(m);
            foreach (var m in lit.Values) if (m != null) Destroy(m);
            foreach (var m in halos.Values) if (m != null) Destroy(m);
        }

        // ---- The stories, one per card ----

        IEnumerator Story(string id)
        {
            switch (id)
            {
                case "S01": return GrandmaHug();
                case "S02": return Tickling();
                case "S03": return TouchingSecret();
                case "S04": return HealthCentre();
                case "S05": return SweetsFromACar();
                case "S06": return TooClose();
                case "S07": return HighFive();
                case "S08": return OnlineMessage();
                case "S09": return GiftWithACatch();
                case "S10": return Lap();
                case "S11": return PicturesOnAPhone();
                case "S12": return FriendsWorry();
                case "S13": return KissHello();
                case "S14": return LateFromSchool();
                case "S15": return LostAtTheMarket();
                case "S16": return Followed();
                case "S17": return CoachAsksFirst();
                case "S18": return VideoCallAtNight();
                case "S19": return FriendAsksFirst();
                default: return null;
            }
        }

        IEnumerator GrandmaHug()
        {
            other = Actor(T("story.grandma", "Grandma"), Grandma, -1.5f, 0.3f);
            yield return Walk(other, At(-0.4f, 0.05f), 0.4f);
            yield return Line(other, T("story.S01.1", "Hello, my dear! Can I have a hug?"));
            yield return Line(guide, T("story.S01.2", "Yes! I love your hugs!"));
            yield return Walk(guide, At(-0.18f, 0.02f), 0.4f);
            guide.Bounce();
            other.Bounce();
            ARFx.Burst(At(-0.28f, 0.03f) + Vector3.up * 0.4f, Theme.Hex("#FF8FB1"));
            game.Audio.PlaySFX("correct");
            yield return new WaitForSeconds(1.2f);
        }

        IEnumerator Tickling()
        {
            other = Actor(T("story.cousin", "Cousin"), Cousin, 1.5f, 0.2f, "boy");
            yield return Walk(other, At(0.3f, 0.05f), 0.6f);
            yield return Line(other, T("story.S02.1", "Tickle, tickle, tickle!"));
            UhOh();
            yield return Line(guide, T("story.S02.2", "Ha ha! Stop, please!"));
            yield return Circle(other, 0.3f, 1.6f);
            yield return Line(other, T("story.S02.3", "No, more tickles!"));
            UhOh();
            yield return Line(guide, T("story.S02.4", "I said stop. I don't like it now."));
        }

        IEnumerator TouchingSecret()
        {
            Fence(At(0.3f, 0.95f), 6);
            other = Actor(T("story.neighbour", "Neighbour"), Neighbour, 1.6f, 0.6f);
            yield return Walk(other, At(0.6f, 0.3f), 0.35f);
            yield return Line(other, T("story.S03.1", "Let's keep a touching secret."));
            yield return Line(other, T("story.S03.2", "Don't tell your parents."));
            UhOh();
            yield return Line(guide, T("story.S03.3", "Hmm... that feels wrong."));
        }

        IEnumerator HealthCentre()
        {
            Sign(At(0.95f, 1.1f), Theme.Unsafe);
            friend = Actor(T("story.mum", "Mum"), Mum, -0.45f, 0.1f, "parent");
            other = Actor(T("story.nurse", "Nurse"), Nurse, 1.5f, 0.6f);
            yield return Walk(other, At(0.4f, 0.1f), 0.4f);
            yield return Line(other, T("story.S04.1", "Hello! I'm going to check your tummy."));
            yield return Line(friend, T("story.S04.2", "It's okay. I'm right here with you."));
            yield return Line(guide, T("story.S04.3", "Okay!"));
            other.Bounce();
            yield return new WaitForSeconds(0.8f);
        }

        IEnumerator SweetsFromACar()
        {
            float road = game.AR.UsesAR ? 1.2f : 2.1f;
            Road(road);
            car = Car(Theme.Hex("#2E4B73"));
            var stop = At(0.35f, road);
            var start = At(-6f, road);
            game.Audio.PlaySFX("car");
            yield return Drive(car, start, stop, 2.4f, true);
            game.Audio.PlaySFX("horn");
            Jolt(0.7f);
            Watch(car.transform.position, 1.6f);
            yield return new WaitForSeconds(0.5f);

            other = Actor(T("story.stranger", "Someone new"), Stranger, 0.35f, road - 0.35f);
            yield return Walk(other, At(0.75f, 0.1f), 0.45f);
            held = Sweets(other);
            game.Audio.PlaySFX("pop");
            Watch(other.transform.position, 3f);
            yield return Line(other, T("story.S05.1", "Hi! Do you like sweets?"));
            yield return Line(other, T("story.S05.2", "Come with me. My car is right here!"));
            guide.FaceTowards(other.transform.position);
            UhOh();
            yield return Line(guide, T("story.S05.3", "Hmm... I don't know you."));
        }

        IEnumerator TooClose()
        {
            Board(At(-0.95f, 1.25f));
            other = Actor(T("story.classmate", "Classmate"), Classmate, 1.4f, 0.4f, "boy");
            yield return Walk(other, At(0.17f, 0.02f), 0.5f);
            yield return Line(guide, T("story.S06.1", "Can you move back, please?"));
            yield return Line(other, T("story.S06.2", "No. I want to stand right here."));
            UhOh();
            yield return Line(guide, T("story.S06.3", "You're too close..."));
        }

        IEnumerator HighFive()
        {
            Board(At(-0.95f, 1.25f));
            other = Actor(T("story.teacher", "Teacher"), Teacher, 0.5f, 0.15f);
            yield return Line(other, T("story.S07.1", "Great answer! High five!"));
            yield return Walk(guide, At(0.25f, 0.08f), 0.4f);
            guide.Bounce();
            other.Bounce();
            ARFx.Burst(At(0.37f, 0.1f) + Vector3.up * 0.45f, Theme.Sun);
            game.Audio.PlaySFX("correct");
            yield return Line(guide, T("story.S07.2", "Yay!"));
        }

        IEnumerator OnlineMessage()
        {
            held = BigPhone(At(0.75f, 0.3f));
            game.Audio.PlaySFX("pop");
            Watch(held.transform.position, 4f);
            yield return Message(held, T("story.S08.1", "Hi! I'm your new friend."), 0.2f);
            yield return Message(held, T("story.S08.2", "Send me photos of you."), 0f);
            yield return Message(held, T("story.S08.3", "Keep it our secret!"), -0.2f);
            guide.FaceTowards(held.transform.position);
            UhOh();
            yield return Line(guide, T("story.S08.4", "Hmm... who is this?"));
        }

        IEnumerator GiftWithACatch()
        {
            other = Actor(T("story.family_friend", "Family friend"), Visitor, 1.5f, 0.4f);
            yield return Walk(other, At(0.6f, 0.15f), 0.4f);
            held = Gift(At(0.42f, 0.12f));
            game.Audio.PlaySFX("pop");
            yield return Line(other, T("story.S09.1", "Here's a present, just for you!"));
            yield return Slide(held, At(0.17f, 0.06f), 0.6f);
            yield return Line(other, T("story.S09.2", "Now you owe me a special secret."));
            UhOh();
            yield return Line(guide, T("story.S09.3", "A secret? I don't like that."));
        }

        IEnumerator Lap()
        {
            Bench(At(0.7f, 0.35f));
            other = Actor(T("story.grown_up", "Grown-up"), Theme.Hex("#9C6B3F"), 0.95f, 0.38f);
            yield return Line(other, T("story.S10.1", "Come and sit on my lap."));
            yield return Line(guide, T("story.S10.2", "I don't want to."));
            yield return Line(other, T("story.S10.3", "Come on, just for a little bit!"));
            UhOh();
        }

        IEnumerator PicturesOnAPhone()
        {
            other = Actor(T("story.older_cousin", "Older cousin"), Theme.Hex("#C0504D"), 1.4f, 0.3f, "boy");
            other.transform.localScale *= 1.2f;   // a few years older than Keza
            yield return Walk(other, At(0.45f, 0.1f), 0.45f);
            held = SmallPhone(other);
            yield return Line(other, T("story.S11.1", "Look at these pictures!"));
            yield return Line(other, T("story.S11.2", "It's our secret, okay?"));
            UhOh();
            guide.FaceTowards(At(-1f, -1f));
            yield return Line(guide, T("story.S11.3", "I don't want to look."));
        }

        IEnumerator FriendsWorry()
        {
            friend = Actor(T("story.friend", "Friend"), Friend, -1.4f, 0.3f, "child");
            yield return Walk(friend, At(-0.3f, 0.06f), 0.6f);
            yield return Line(friend, T("story.S12.1", "Can I tell you something?"));
            yield return Line(friend, T("story.S12.2", "A grown-up touches me in a way I don't like."));
            yield return Line(friend, T("story.S12.3", "Please don't tell anyone."));
            UhOh();
            yield return Line(guide, T("story.S12.4", "Oh no..."));
        }

        IEnumerator KissHello()
        {
            other = Actor(T("story.uncle", "Uncle"), Uncle, 1.5f, 0.3f);
            yield return Walk(other, At(0.42f, 0.06f), 0.45f);
            yield return Line(other, T("story.S13.1", "Hello! Give your uncle a kiss!"));
            yield return Line(guide, T("story.S13.2", "I don't want to..."));
            yield return Line(other, T("story.S13.3", "You must kiss me hello!"));
            UhOh();
        }

        // ---- Stories after dark ----

        /// <summary>Late leaving school, in the dark: a car pulls up and a stranger says Mum sent them.</summary>
        IEnumerator LateFromSchool()
        {
            float road = game.AR.UsesAR ? 1.2f : 2.1f;
            Road(road);
            SchoolGate(At(-0.8f, 0.6f));
            StreetLamp(At(1.15f, road - 0.55f));
            StreetLamp(At(-1.6f, road - 0.55f));
            yield return Walk(guide, At(-0.15f, 0.25f), 0.3f);   // walking out of the gate, alone
            car = Car(Theme.Hex("#2E2E33"));
            Headlights(car);
            var stop = At(0.45f, road);
            game.Audio.PlaySFX("car");
            yield return Drive(car, At(7f, road), stop, 2.6f, true);
            game.Audio.PlaySFX("horn");
            Jolt(0.6f);
            Watch(car.transform.position, 1.6f);
            yield return new WaitForSeconds(0.4f);

            other = Actor(T("story.driver", "Driver"), Stranger, 0.45f, road - 0.35f);
            yield return Walk(other, At(0.55f, 0.45f), 0.4f);
            Watch(other.transform.position, 3f);
            guide.TurnTowards(other.transform.position);
            yield return Line(other, T("story.S14.1", "It's late and dark!"));
            yield return Line(other, T("story.S14.2", "Your mum sent me. Get in, quick!"));
            UhOh();
            yield return Line(guide, T("story.S14.3", "Mum didn't tell me about you..."));
        }

        /// <summary>Lost at the market as night falls: a fruit seller with her own child phones Mum, who comes.</summary>
        IEnumerator LostAtTheMarket()
        {
            // The seller stands behind her stall, her child beside it.
            Stall(At(0.62f, 0.38f));
            StreetLamp(At(-1.1f, 0.9f));
            other = Actor(T("story.fruit_seller", "Fruit seller"), Theme.Hex("#C77A1E"), 0.66f, 0.62f);
            friend = Actor(T("story.her_child", "Her child"), Friend, 1.02f, 0.5f, "child");
            yield return Walk(guide, At(-0.35f, 0.1f), 0.35f);
            guide.TurnTowards(At(-1.5f, 0.5f));
            yield return Line(guide, T("story.S15.1", "Mum? Where are you?"));
            UhOh();
            yield return Walk(guide, At(0.3f, 0.1f), 0.35f);
            yield return Line(guide, T("story.S15.2", "I can't find my mum!"));
            yield return Line(other, T("story.S15.3", "Don't worry. Stay here with us, where people can see you."));
            held = Glowing(SmallPhone(other));
            game.Audio.PlaySFX("pop");
            Watch(other.transform.position, 2.4f);
            yield return Line(other, T("story.S15.4", "What's your mum's number? Let's call her."));
            var mum = Actor(T("story.mum", "Mum"), Mum, -1.7f, 0.3f, "parent");
            yield return Walk(mum, At(-0.05f, 0.12f), 0.6f);
            guide.TurnTowards(mum.transform.position);
            guide.Bounce();
            mum.Bounce();
            ARFx.Burst(At(0.15f, 0.14f) + Vector3.up * 0.4f, Theme.Safe);
            game.Audio.PlaySFX("correct");
            yield return Line(mum, T("story.S15.5", "There you are! Thank you for calling me."));
        }

        /// <summary>Walking home after dark, someone keeps following; a bright shop is just ahead.</summary>
        IEnumerator Followed()
        {
            Shop(At(1.05f, 0.85f));
            StreetLamp(At(-0.95f, 0.85f));
            other = Actor(T("story.stranger", "Someone new"), Stranger, -1.8f, -0.1f);
            StartCoroutine(Walk(guide, At(0.1f, 0.1f), 0.22f));
            yield return Walk(other, At(-0.75f, 0.02f), 0.24f);
            guide.TurnTowards(other.transform.position);
            UhOh();
            yield return Line(guide, T("story.S16.1", "Someone keeps following me..."));
            yield return Walk(other, At(-0.42f, 0.06f), 0.22f);
            Watch(other.transform.position, 2.4f);
            yield return Line(other, T("story.S16.2", "Hey! Wait for me. Where are you going?"));
            UhOh();
            yield return Line(guide, T("story.S16.3", "I'm scared..."));
        }

        // ---- More ages 9 to 11 stories ----

        /// <summary>Day, at sports practice: the coach asks before moving Keza's hands, with the whole team there.</summary>
        IEnumerator CoachAsksFirst()
        {
            other = Actor(T("story.coach", "Coach"), Theme.Hex("#2BA3A0"), 0.6f, 0.35f);
            friend = Actor(T("story.teammate", "Teammate"), Friend, -0.65f, 0.45f, "child");
            var mate = Actor(T("story.teammate", "Teammate"), Classmate, -0.3f, 0.8f, "boy");
            held = Ball(guide);
            yield return Line(other, T("story.S17.1", "Good try! May I move your hands to show the throw?"));
            yield return Line(guide, T("story.S17.2", "Yes, okay."));
            yield return Walk(other, At(0.26f, 0.12f), 0.4f);
            other.Bounce();
            yield return Line(other, T("story.S17.3", "Like this! Now you try."));
            guide.Bounce();
            ARFx.Burst(guide.transform.position + Vector3.up * 0.5f, Theme.Sun);
            game.Audio.PlaySFX("correct");
            friend.Bounce();
            if (mate != null) mate.Bounce();
            yield return Line(friend, T("story.S17.4", "Great throw!"));
        }

        /// <summary>Late at night, a message from someone met in a game asks for the camera and a secret.</summary>
        IEnumerator VideoCallAtNight()
        {
            StreetLamp(At(-1.05f, 0.9f));
            held = BigPhone(At(0.75f, 0.3f));
            Bright(held, PrimitiveType.Cube, new Vector3(0f, 0f, -0.013f), new Vector3(0.36f, 0.64f, 0.002f), Theme.Hex("#DDEEFF"));   // a lit screen in the dark
            game.Audio.PlaySFX("pop");
            Watch(held.transform.position, 4f);
            yield return Message(held, T("story.S18.1", "Are you still awake?"), 0.2f);
            yield return Message(held, T("story.S18.2", "Turn on your camera."), 0f);
            yield return Message(held, T("story.S18.3", "Don't tell your parents!"), -0.2f);
            guide.TurnTowards(held.transform.position);
            UhOh();
            yield return Line(guide, T("story.S18.4", "That's a red flag..."));
        }

        /// <summary>Day, after a race: a friend asks before hugging Keza, and Keza says yes.</summary>
        IEnumerator FriendAsksFirst()
        {
            FinishLine(At(-0.15f, 0.8f));
            friend = Actor(T("story.friend", "Friend"), Friend, -1.4f, 0.4f, "child");
            guide.Bounce();
            ARFx.Burst(guide.transform.position + Vector3.up * 0.5f, Theme.Sun);
            game.Audio.PlaySFX("correct");
            yield return Walk(friend, At(-0.36f, 0.08f), 0.6f);
            yield return Line(friend, T("story.S19.1", "You won! Can I give you a hug?"));
            yield return Line(guide, T("story.S19.2", "Yes!"));
            yield return Walk(guide, At(-0.18f, 0.04f), 0.4f);
            guide.Bounce();
            friend.Bounce();
            ARFx.Burst(At(-0.27f, 0.06f) + Vector3.up * 0.4f, Theme.Hex("#FF8FB1"));
            yield return new WaitForSeconds(1f);
        }

        // ---- Ages 9 to 11: spotting the red flag ----

        /// <summary>True once the story has shown its warning sign (the uh-oh moment), until the next story.</summary>
        public bool RedFlagUp { get; private set; }

        /// <summary>A red flag goes up over the warning sign the child spotted: the person, the phone or the car.</summary>
        public void PlantFlag()
        {
            Transform over = other != null ? other.transform : held != null ? held.transform : car != null ? car.transform : guide.transform;
            // Over their head: the top of the body, not the name or a speech bubble.
            float top = float.MinValue;
            foreach (var r in over.GetComponentsInChildren<Renderer>())
                if (r.GetComponent<TextMesh>() == null && r.GetComponentInParent<Billboard>() == null) top = Mathf.Max(top, r.bounds.max.y);
            if (top == float.MinValue) top = over.position.y + 0.6f;
            var flag = Prop("Red flag", new Vector3(over.position.x, top + 0.04f, over.position.z), -away);
            Part(flag, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.012f, 0.12f, 0.012f), Theme.Hex("#2A2D33"));
            Bright(flag, PrimitiveType.Cube, new Vector3(0.065f, 0.2f, 0f), new Vector3(0.12f, 0.08f, 0.01f), Theme.Unsafe);
            flag.transform.SetParent(over, true);
            ARFx.Burst(flag.transform.position + Vector3.up * 0.2f, Theme.Unsafe, 12);
        }

        // ---- Acting out the safe choice ----

        /// <summary>The "what should Keza do?" step for a card, or null when there is none (the safe cards).</summary>
        public ActOut ActFor(string id)
        {
            string what = T("story.act.question", "What should {0} do now?");
            switch (id)
            {
                case "S02": return new ActOut
                {
                    question = what, right = T("story.S02.right", "Say STOP loudly"), wrong = T("story.S02.wrong", "Keep laughing"),
                    wrongNote = T("story.S02.wrong_note", "When tickling stops feeling good, you can say STOP loudly. If it doesn't stop, tell a grown-up."),
                    praise = T("story.S02.praise", "Yes! A loud, clear STOP shows you mean it. If they still don't stop, tell a grown-up."),
                    play = SayStop,
                };
                case "S03": return new ActOut
                {
                    question = what, right = T("story.S03.right", "Say NO and tell Mum"), wrong = T("story.S03.wrong", "Keep the secret"),
                    wrongNote = T("story.S03.wrong_note", "A secret about touching is never okay to keep. Telling a grown-up you trust keeps you safe."),
                    praise = T("story.S03.praise", "Well done! Touching should never be a secret. Telling a grown-up you trust is always right."),
                    play = () => NoThenTellMum(T("story.S03.tell", "Mum, I need to tell you something.")),
                };
                case "S05": return new ActOut
                {
                    question = what, right = T("story.S05.right", "Say NO, run to Mum"), wrong = T("story.S05.wrong", "Take the sweets"),
                    wrongNote = T("story.S05.wrong_note", "Never go with someone you don't know, even for sweets, toys or a ride. Say NO and run to a grown-up you trust."),
                    praise = T("story.S05.praise", "That's it! Say NO, run to a grown-up you trust, and tell them what happened."),
                    sayNo = true, play = RunFromTheCar,
                };
                case "S06": return new ActOut
                {
                    question = what, right = T("story.S06.right", "Step back, ask teacher"), wrong = T("story.S06.wrong", "Do nothing"),
                    wrongNote = T("story.S06.wrong_note", "Your bubble is yours. Step back, and if they still won't move, ask a grown-up for help."),
                    praise = T("story.S06.praise", "Good choice! If someone won't give you space, a grown-up can help."),
                    play = AskTeacher,
                };
                case "S08": return new ActOut
                {
                    question = what, right = T("story.S08.right", "Don't reply, tell Mum"), wrong = T("story.S08.wrong", "Send a photo"),
                    wrongNote = T("story.S08.wrong_note", "Never send photos of yourself to people online, and never keep their messages secret. Show a grown-up you trust."),
                    praise = T("story.S08.praise", "Exactly right. Show a grown-up any message that asks for photos or secrets."),
                    play = () => PutAwayAndTellMum(T("story.S08.tell", "Mum, look at this message.")),
                };
                case "S09": return new ActOut
                {
                    question = what, right = T("story.S09.right", "Give it back, tell"), wrong = T("story.S09.wrong", "Keep gift and secret"),
                    wrongNote = T("story.S09.wrong_note", "A gift that comes with a secret is a warning sign. You can say no thank you and tell a grown-up."),
                    praise = T("story.S09.praise", "Well done. Gifts should never come with secrets. A grown-up you trust needs to know."),
                    play = GiveItBack,
                };
                case "S10": return new ActOut
                {
                    question = what, right = T("story.S10.right", "No thanks, sit by Mum"), wrong = T("story.S10.wrong", "Sit on the lap"),
                    wrongNote = T("story.S10.wrong_note", "You can say no to sitting on a lap, even with someone you know. Your body belongs to you."),
                    praise = T("story.S10.praise", "Good! You never have to sit on anyone's lap if you don't want to."),
                    play = () => NoThenTellMum(T("story.S10.tell", "Can I sit with you, Mum?")),
                };
                case "S11": return new ActOut
                {
                    question = what, right = T("story.S11.right", "Walk away and tell"), wrong = T("story.S11.wrong", "Keep looking"),
                    wrongNote = T("story.S11.wrong_note", "You don't have to look. Walk away and tell a grown-up you trust, even if they say it's a secret."),
                    praise = T("story.S11.praise", "That was brave. Walking away and telling a grown-up is the right thing to do."),
                    play = () => PutAwayAndTellMum(T("story.S11.tell", "Mum, I saw pictures I didn't like.")),
                };
                case "S12": return new ActOut
                {
                    question = what, right = T("story.S12.right", "Get help together"), wrong = T("story.S12.wrong", "Keep the secret"),
                    wrongNote = T("story.S12.wrong_note", "Keeping this secret won't help your friend. A good friend tells a grown-up who can help."),
                    praise = T("story.S12.praise", "You're a good friend. Some secrets are too big to keep. A grown-up can help your friend."),
                    play = TellTogether,
                };
                case "S13": return new ActOut
                {
                    question = what, right = T("story.S13.right", "Wave or high-five"), wrong = T("story.S13.wrong", "Kiss him anyway"),
                    wrongNote = T("story.S13.wrong_note", "You don't have to kiss anyone, even family. A wave or a high-five is a fine hello."),
                    praise = T("story.S13.praise", "Great! You choose how to say hello."),
                    play = HighFiveInstead,
                };
                case "S14": return new ActOut
                {
                    question = what, right = T("story.S14.right", "Say NO, run to school"), wrong = T("story.S14.wrong", "Get in the car"),
                    wrongNote = T("story.S14.wrong_note", "Never get into a car with someone you don't know, even if they say Mum sent them. Say NO and go back to your teacher."),
                    praise = T("story.S14.praise", "That's it! Say NO, go back to school, and let your teacher call home. No password, no ride!"),
                    sayNo = true, play = BackToSchool,
                };
                case "S18": return new ActOut
                {
                    question = what, right = T("story.S18.right", "Don't reply, tell Mum"), wrong = T("story.S18.wrong", "Turn on the camera"),
                    wrongNote = T("story.S18.wrong_note", "Never turn on your camera for someone you only know online, and never keep it secret. Tell a grown-up."),
                    praise = T("story.S18.praise", "Well done. No reply, no camera, and a grown-up you trust knows."),
                    play = () => PutAwayAndTellMum(T("story.S18.tell", "Mum, someone online asked me for a video.")),
                };
                case "S16": return new ActOut
                {
                    question = what, right = T("story.S16.right", "Run into the shop"), wrong = T("story.S16.wrong", "Hide in the dark"),
                    wrongNote = T("story.S16.wrong_note", "Hiding in the dark keeps you alone. Go where there is light and people, and tell a grown-up."),
                    praise = T("story.S16.praise", "Yes! A bright place with people is safer. The shopkeeper can help you call your family."),
                    play = IntoTheShop,
                };
                default: return null;
            }
        }

        IEnumerator SayStop()
        {
            guide.Stop();
            guide.FaceTowards(Pos(other, At(0.3f, 0.05f)));
            guide.Bounce();
            yield return Line(guide, T("story.S02.stop", "STOP! I don't like it!"));
            if (other != null)
            {
                yield return Walk(other, At(0.75f, 0.3f), 0.5f);
                yield return Line(other, T("story.S02.sorry", "Okay. Sorry!"));
            }
        }

        IEnumerator NoThenTellMum(string tell)
        {
            guide.Stop();
            guide.Bounce();
            yield return Line(guide, T("story.no", "NO!"));
            if (other != null) StartCoroutine(Walk(other, At(1.8f, 0.6f), 0.4f));
            yield return TellMum(tell);
        }

        IEnumerator RunFromTheCar()
        {
            guide.Stop();
            var mum = Actor(T("story.mum", "Mum"), Mum, -0.9f, -0.2f, "parent");
            guide.Bounce();
            yield return Line(guide, T("story.S05.no", "NO! I don't know you!"));
            if (other != null && car != null) StartCoroutine(LeaveInTheCar());
            // The child gets Keza to Mum: quick!
            yield return ReachSafety(mum, 0.9f, T("story.run.title", "Run to Mum!"),
                T("story.run.body", "Quick! Help {0} run to Mum: drag {0}, or tap Mum."));
            guide.FaceTowards(mum.transform.position);
            guide.Bounce();
            mum.Bounce();
            game.Audio.PlaySFX("correct");
            yield return Line(mum, T("story.S05.mum", "You did the right thing. Tell me what happened."));
        }

        /// <summary>NO to the driver, and back to the teacher at the school gate; the car drives off.</summary>
        IEnumerator BackToSchool()
        {
            guide.Stop();
            var teacher = Actor(T("story.teacher", "Teacher"), Teacher, -0.8f, 0.4f);
            guide.Bounce();
            yield return Line(guide, T("story.S05.no", "NO! I don't know you!"));
            if (other != null && car != null) StartCoroutine(LeaveInTheCar());
            yield return ReachSafety(teacher, 0.9f, T("story.S14.run.title", "Back to school!"),
                T("story.S14.run.body", "Quick! Help {0} run back to the teacher: drag {0}, or tap the teacher."));
            guide.FaceTowards(teacher.transform.position);
            guide.Bounce();
            teacher.Bounce();
            yield return Line(teacher, T("story.S14.teacher", "Well done! Let's call your mum together."));
        }

        /// <summary>Into the bright shop: the shopkeeper helps, and the person following walks away.</summary>
        IEnumerator IntoTheShop()
        {
            guide.Stop();
            var keeper = Actor(T("story.shopkeeper", "Shopkeeper"), Theme.Hex("#2BA3A0"), 1.05f, 0.55f);
            yield return ReachSafety(keeper, 0.8f, T("story.S16.run.title", "To the shop!"),
                T("story.S16.run.body", "Quick! Help {0} get to the shopkeeper: drag {0}, or tap the shopkeeper."));
            if (other != null) StartCoroutine(Walk(other, At(-2f, -0.2f), 0.4f));
            guide.FaceTowards(keeper.transform.position);
            yield return Line(guide, T("story.S16.tell", "Someone is following me!"));
            keeper.Bounce();
            yield return Line(keeper, T("story.S16.keeper", "You're safe here. Let's call your family."));
        }

        IEnumerator LeaveInTheCar()
        {
            var door = car.transform.position - away * 0.35f;
            yield return Walk(other, door, 0.6f);
            if (other != null) { cast.Remove(other); Destroy(other.gameObject); other = null; }
            game.Audio.PlaySFX("car");
            yield return Drive(car, car.transform.position, car.transform.position + right * 6f, 2f, false);
        }

        IEnumerator AskTeacher()
        {
            guide.Stop();
            yield return Walk(guide, At(-0.3f, 0f), 0.4f);
            var teacher = Actor(T("story.teacher", "Teacher"), Teacher, -1.4f, 0.4f);
            yield return Line(guide, T("story.S06.ask", "Teacher, can you help me?"));
            yield return Walk(teacher, At(-0.05f, 0.25f), 0.6f);
            yield return Line(teacher, T("story.S06.teacher", "Please give your friend some space."));
            if (other != null)
            {
                yield return Walk(other, At(0.8f, 0.4f), 0.5f);
                yield return Line(other, T("story.S06.sorry", "Okay. Sorry!"));
            }
        }

        IEnumerator PutAwayAndTellMum(string tell)
        {
            guide.Stop();
            yield return Line(guide, T("story.no_reply", "I won't reply. I'll tell Mum."));
            if (held != null) yield return Shrink(held);
            if (other != null) StartCoroutine(Walk(other, At(1.8f, 0.6f), 0.4f));
            yield return TellMum(tell);
        }

        IEnumerator GiveItBack()
        {
            guide.Stop();
            yield return Line(guide, T("story.S09.no", "No, thank you."));
            if (held != null) yield return Slide(held, At(0.45f, 0.12f), 0.5f);
            if (other != null) StartCoroutine(Walk(other, At(1.8f, 0.6f), 0.4f));
            yield return TellMum(T("story.S09.tell", "Mum, he gave me a gift and wants a secret."));
        }

        IEnumerator TellTogether()
        {
            guide.Stop();
            var teacher = Actor(T("story.teacher", "Teacher"), Teacher, 0.9f, -0.15f);
            yield return Line(guide, T("story.S12.together", "Let's tell a grown-up together."));
            if (friend != null) StartCoroutine(Walk(friend, At(0.45f, 0.2f), 0.45f));
            yield return Walk(guide, At(0.6f, -0.05f), 0.45f);
            guide.FaceTowards(teacher.transform.position);
            yield return Line(guide, T("story.S12.tell", "My friend needs help."));
            yield return Line(teacher, T("story.S12.teacher", "Thank you for telling me. I will help."));
            teacher.Bounce();
        }

        IEnumerator HighFiveInstead()
        {
            guide.Stop();
            guide.FaceTowards(Pos(other, At(0.42f, 0.06f)));
            guide.Bounce();
            yield return Line(guide, T("story.S13.wave", "Hi, Uncle! High five?"));
            if (other != null)
            {
                other.Bounce();
                ARFx.Burst(At(0.25f, 0.06f) + Vector3.up * 0.45f, Theme.Sun);
                game.Audio.PlaySFX("correct");
                yield return Line(other, T("story.S13.ok", "High five it is!"));
            }
        }

        /// <summary>Mum comes, Keza walks to her and tells; Mum thanks her.</summary>
        IEnumerator TellMum(string tell)
        {
            var mum = Actor(T("story.mum", "Mum"), Mum, -1.5f, 0.1f, "parent");
            StartCoroutine(Walk(mum, At(-0.85f, -0.2f), 0.6f));
            // The child takes Keza to Mum.
            yield return ReachSafety(mum, 0.55f, T("story.reach.title", "Go and tell Mum!"),
                T("story.reach.body", "Help {0} get to Mum: drag {0}, or tap Mum."));
            guide.FaceTowards(mum.transform.position);
            yield return Line(guide, tell);
            mum.Bounce();
            game.Audio.PlaySFX("correct");
            yield return Line(mum, T("story.mum.thanks", "Thank you for telling me. You did the right thing."));
        }

        // ---- The child helps: taking Keza to a safe grown-up ----

        /// <summary>Set by the lesson: puts words on the card (heading, body) while the child helps.</summary>
        public Action<string, string> Ask;

        ARFigure goal;   // the grown-up the child is taking Keza to, while that is happening

        /// <summary>
        /// A tap while Keza needs to reach a safe grown-up: on the grown-up, or on the floor near them,
        /// sends Keza running there. Returns true when the tap was used.
        /// </summary>
        public bool HandleTap(ARTap tap)
        {
            if (goal == null) return false;
            bool onGoal = tap.Find<ARFigure>() == goal
                || (tap.hasFloor && Vector3.ProjectOnPlane(tap.floor - goal.transform.position, Vector3.up).magnitude < 0.5f);
            if (!onGoal) return false;
            guide.WalkTo(BesideGoal(), null);
            return true;
        }

        Vector3 BesideGoal()
        {
            var offset = Vector3.ProjectOnPlane(guide.transform.position - goal.transform.position, Vector3.up);
            if (offset.sqrMagnitude < 0.0001f) offset = right;
            return goal.transform.position + offset.normalized * 0.26f;
        }

        /// <summary>
        /// A glowing ring under the safe grown-up; the child drags Keza there or taps the grown-up.
        /// If nothing happens for 10 seconds, Keza goes there by herself, so no child is ever stuck.
        /// </summary>
        IEnumerator ReachSafety(ARFigure safe, float speed, string heading, string body)
        {
            if (safe == null) yield break;
            goal = safe;
            guide.WalkSpeed = speed;
            var ring = ARFx.FloorQuad("Safe ring", ARFx.Ring, Theme.Safe, 0.45f);
            ring.transform.SetParent(safe.transform, false);
            ring.transform.localPosition = Vector3.up * 0.006f / Mathf.Max(safe.transform.lossyScale.y, 0.0001f);
            ring.transform.localScale = Vector3.one * 0.45f / Mathf.Max(safe.transform.lossyScale.y, 0.0001f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            FxAnimator.Pulse(ring);
            if (Ask != null) Ask(heading, string.Format(body, Localization.CharacterName));
            float start = Time.time;
            while (safe != null && guide != null
                && Vector3.ProjectOnPlane(guide.transform.position - safe.transform.position, Vector3.up).magnitude > 0.34f)
            {
                if (Time.time - start > 10f && !guide.IsMoving) guide.WalkTo(BesideGoal(), null);
                yield return null;
            }
            goal = null;
            if (ring != null) Destroy(ring);
            if (guide == null) yield break;
            guide.Stop();
            ARFx.Burst(guide.transform.position + Vector3.up * 0.4f, Theme.Safe);
            game.Audio.PlaySFX("correct");
        }

        // ---- The camera: moments that matter ----

        /// <summary>Keza feels something is wrong: she shivers and the camera moves in on her.</summary>
        void UhOh()
        {
            RedFlagUp = true;   // the warning sign has shown: a 9 to 11 child can spot it now
            FxAnimator.Shake(guide.gameObject);
            game.Audio.PlaySFX("uhoh");
            if (game.AR.Stage != null) game.AR.Stage.Focus(guide.transform.position, 2.4f);
        }

        /// <summary>The camera moves in on someone (the stranger with the sweets, the phone).</summary>
        void Watch(Vector3 at, float seconds = 2.4f)
        {
            if (game.AR.Stage != null) game.AR.Stage.Focus(at, seconds);
        }

        /// <summary>A jolt of the camera: a car horn, a door slam.</summary>
        void Jolt(float strength)
        {
            if (game.AR.Stage != null) game.AR.Stage.Shake(strength);
        }

        // ---- People, speech and movement ----

        Vector3 At(float x, float z)
        {
            return home + right * x + away * z;
        }

        static Vector3 Pos(ARFigure f, Vector3 otherwise)
        {
            return f != null ? f.transform.position : otherwise;
        }

        ARFigure Actor(string label, Color color, float x, float z, string role = null)
        {
            var f = spawn(label, color, role);
            f.transform.position = At(x, z);
            f.FaceTowards(home);
            game.AR.SetMovable(f, false);
            cast.Add(f);
            return f;
        }

        /// <summary>A speech bubble for about as long as it takes to read it, also read aloud when that is on.</summary>
        IEnumerator Line(ARFigure who, string text)
        {
            if (who == null) yield break;
            float seconds = Mathf.Clamp(1.2f + text.Length * 0.06f, 1.8f, 4.5f);
            who.SayFor(text, game.AR.OpaqueMaterial, seconds);
            game.Audio.PlaySFX("pop");
            if (game.Reader != null) game.Reader.SpeakIfAuto(text);
            yield return new WaitForSeconds(seconds + 0.2f);
        }

        IEnumerator Walk(ARFigure who, Vector3 to, float speed)
        {
            if (who == null) yield break;
            who.WalkSpeed = speed;
            bool arrived = false;
            who.WalkTo(to, () => arrived = true);
            while (!arrived && who != null) yield return null;
            if (who != null && who != guide) who.FaceTowards(guide.transform.position);
        }

        /// <summary>Hops in a little circle around Keza (the cousin tickling).</summary>
        IEnumerator Circle(ARFigure who, float radius, float seconds)
        {
            if (who == null) yield break;
            var centre = guide.transform.position;
            for (float t = 0f; t < seconds && who != null; t += Time.deltaTime)
            {
                float a = t / seconds * Mathf.PI * 2f;
                var p = centre + (right * Mathf.Cos(a) + away * Mathf.Sin(a)) * radius;
                who.transform.position = p + Vector3.up * Mathf.Abs(Mathf.Sin(t * 12f)) * 0.04f;
                who.FaceTowards(centre);
                yield return null;
            }
            if (who != null) who.transform.position = centre + right * radius;
        }

        IEnumerator Drive(GameObject vehicle, Vector3 from, Vector3 to, float seconds, bool slowDown)
        {
            if (vehicle == null) yield break;
            var body = vehicle.transform.Find("Body");
            vehicle.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(to - from, Vector3.up));
            for (float t = 0f; t < 1f && vehicle != null; t += Time.deltaTime / seconds)
            {
                float k = slowDown ? 1f - (1f - t) * (1f - t) : t * t;
                vehicle.transform.position = Vector3.Lerp(from, to, k);
                if (body != null) body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(t * 60f)) * 0.006f;   // a little bump on the road
                yield return null;
            }
            if (vehicle != null) vehicle.transform.position = to;
        }

        IEnumerator Slide(GameObject thing, Vector3 to, float seconds)
        {
            if (thing == null) yield break;
            var from = thing.transform.position;
            to.y = from.y;
            for (float t = 0f; t < 1f && thing != null; t += Time.deltaTime / seconds)
            {
                thing.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.08f;
                yield return null;
            }
            if (thing != null) thing.transform.position = to;
        }

        IEnumerator Shrink(GameObject thing)
        {
            var start = thing.transform.localScale;
            for (float t = 0f; t < 1f && thing != null; t += Time.deltaTime * 2.5f)
            {
                thing.transform.localScale = start * (1f - t);
                yield return null;
            }
            if (thing != null) thing.SetActive(false);
        }

        /// <summary>A chat bubble that pops up next to the big phone.</summary>
        IEnumerator Message(GameObject phone, string text, float height)
        {
            var label = ARFx.Label(text, phone.transform.position + Vector3.up * height - right * 0.02f - away * 0.05f, Theme.Primary, 0.7f);   // about the phone's width
            label.transform.SetParent(phone.transform, true);
            game.Audio.PlaySFX("pop");
            if (game.Reader != null) game.Reader.SpeakIfAuto(text);
            yield return new WaitForSeconds(Mathf.Clamp(1.2f + text.Length * 0.06f, 1.8f, 4f));
        }

        // ---- Props from simple shapes ----

        Material Paint(Color c)
        {
            Material m;
            if (paints.TryGetValue(c, out m) && m != null) return m;
            var source = game.AR.OpaqueMaterial;
            m = source != null ? new Material(source) : new Material(Shader.Find("Standard"));
            m.color = c;
            paints[c] = m;
            return m;
        }

        readonly Dictionary<Color, Material> shiny = new Dictionary<Color, Material>();

        /// <summary>Gives a part a glossy finish (car paint, glass, chrome), so it catches the light like the real thing.</summary>
        GameObject Shine(GameObject part, float gloss)
        {
            var c = part.GetComponent<Renderer>().sharedMaterial.color;
            Material m;
            if (!shiny.TryGetValue(c, out m) || m == null)
            {
                m = new Material(Paint(c));
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", gloss);
                shiny[c] = m;
            }
            part.GetComponent<Renderer>().sharedMaterial = m;
            return part;
        }

        /// <summary>An empty object for a prop, on the floor at a point, turned so its front faces the phone.</summary>
        GameObject Prop(string name, Vector3 at, Vector3 forward)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(at, Quaternion.LookRotation(forward));
            if (game.AR.AnchorRoot != null) root.transform.SetParent(game.AR.AnchorRoot, true);
            props.Add(root);
            return root;
        }

        GameObject Part(GameObject parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Vector3 euler = default(Vector3), string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            Destroy(go.GetComponent<Collider>());   // props never catch the child's taps
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Paint(color);
            return go;
        }

        /// <summary>A grey road with white dashes, running across the view behind Keza.</summary>
        void Road(float z)
        {
            // Dark asphalt with white edge lines and dashes, raised kerbs and a paved pavement on Keza's side.
            var road = Prop("Road", At(0f, z) + Vector3.up * 0.002f, right);
            Part(road, PrimitiveType.Cube, Vector3.zero, new Vector3(0.72f, 0.004f, 14f), Theme.Hex("#3B3F45"));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(road, PrimitiveType.Cube, new Vector3(side * 0.33f, 0.003f, 0f), new Vector3(0.015f, 0.004f, 14f), Theme.Hex("#E8E8E2"));
                Part(road, PrimitiveType.Cube, new Vector3(side * 0.385f, 0.012f, 0f), new Vector3(0.05f, 0.025f, 14f), Theme.Hex("#B9B6AE"));
            }
            Part(road, PrimitiveType.Cube, new Vector3(-0.53f, 0.008f, 0f), new Vector3(0.24f, 0.016f, 14f), Theme.Hex("#CDBFA6"));
            for (int i = -10; i <= 10; i++)
                Part(road, PrimitiveType.Cube, new Vector3(0f, 0.003f, i * 0.6f), new Vector3(0.025f, 0.004f, 0.28f), Theme.Hex("#F2F0E6"));
        }

        /// <summary>
        /// A car: glossy paint, a sloping windscreen and rear window in dark tinted glass, bumpers, mirrors,
        /// headlights and tail lights, and tyres with silver wheels. Its front is local +z.
        /// </summary>
        GameObject Car(Color paint)
        {
            var root = Prop("Car", At(-6f, 2f), right);
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var glass = Theme.Hex("#1F2A36");
            var trim = Theme.Hex("#2A2D33");
            Shine(Part(body, PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(0.42f, 0.14f, 0.96f), paint), 0.8f);
            Shine(Part(body, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0.3f), new Vector3(0.41f, 0.04f, 0.34f), paint, new Vector3(4f, 0f, 0f)), 0.8f);   // the bonnet
            Shine(Part(body, PrimitiveType.Cube, new Vector3(0f, 0.29f, -0.07f), new Vector3(0.37f, 0.13f, 0.44f), paint), 0.8f);              // the cabin
            Shine(Part(body, PrimitiveType.Cube, new Vector3(0f, 0.295f, -0.07f), new Vector3(0.38f, 0.095f, 0.4f), glass), 0.92f);             // side windows
            Shine(Part(body, PrimitiveType.Cube, new Vector3(0f, 0.295f, 0.21f), new Vector3(0.35f, 0.012f, 0.17f), glass, new Vector3(45f, 0f, 0f)), 0.92f);   // windscreen, sloping down to the bonnet
            Shine(Part(body, PrimitiveType.Cube, new Vector3(0f, 0.29f, -0.33f), new Vector3(0.35f, 0.012f, 0.15f), glass, new Vector3(-57f, 0f, 0f)), 0.92f);   // rear window
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.485f), new Vector3(0.43f, 0.06f, 0.03f), trim);    // bumpers
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.1f, -0.485f), new Vector3(0.43f, 0.06f, 0.03f), trim);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.16f, -0.488f), new Vector3(0.1f, 0.035f, 0.01f), Theme.Hex("#F2D64B"));   // number plate
            foreach (float side in new[] { -1f, 1f })
            {
                Shine(Part(body, PrimitiveType.Cube, new Vector3(side * 0.14f, 0.165f, 0.48f), new Vector3(0.09f, 0.035f, 0.02f), Theme.Hex("#FFF6D8")), 0.95f);
                Part(body, PrimitiveType.Cube, new Vector3(side * 0.15f, 0.17f, -0.481f), new Vector3(0.08f, 0.03f, 0.02f), Theme.Hex("#D23B3B"));
                Shine(Part(body, PrimitiveType.Cube, new Vector3(side * 0.205f, 0.27f, 0.13f), new Vector3(0.03f, 0.025f, 0.04f), paint), 0.8f);   // mirrors
                Part(body, PrimitiveType.Cube, new Vector3(side * 0.211f, 0.15f, -0.02f), new Vector3(0.002f, 0.1f, 0.006f), trim);                  // door line
                foreach (float end in new[] { -0.3f, 0.3f })
                {
                    Part(root, PrimitiveType.Cylinder, new Vector3(side * 0.19f, 0.07f, end), new Vector3(0.14f, 0.035f, 0.14f), Theme.Hex("#1A1C20"), new Vector3(0f, 0f, 90f));
                    Shine(Part(root, PrimitiveType.Cylinder, new Vector3(side * 0.207f, 0.07f, end), new Vector3(0.085f, 0.022f, 0.085f), Theme.Hex("#C9CDD3"), new Vector3(0f, 0f, 90f)), 0.85f);
                }
            }
            return root;
        }

        /// <summary>A bag of sweets held at the stranger's side.</summary>
        GameObject Sweets(ARFigure holder)
        {
            var bag = Prop("Sweets", holder.transform.position, holder.transform.forward);
            bag.transform.SetParent(holder.transform, true);
            float s = Mathf.Max(holder.transform.lossyScale.y, 0.0001f);   // sizes in metres, whatever the model's own scale
            bag.transform.localPosition = new Vector3(-0.17f, 0.3f, 0.06f) / s;
            bag.transform.localScale = Vector3.one / s;
            Part(bag, PrimitiveType.Cube, Vector3.zero, new Vector3(0.09f, 0.1f, 0.05f), Theme.Hex("#FF7EB3"));
            Color[] sweets = { Theme.Sun, Theme.Safe, Theme.Primary };
            for (int i = 0; i < 3; i++)
                Part(bag, PrimitiveType.Sphere, new Vector3(-0.025f + i * 0.025f, 0.065f, 0f), Vector3.one * 0.035f, sweets[i]);
            return bag;
        }

        GameObject Gift(Vector3 at)
        {
            var gift = Prop("Gift", at + Vector3.up * 0.25f, -away);
            Part(gift, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.12f, Theme.Hex("#7A5CC7"));
            Part(gift, PrimitiveType.Cube, Vector3.zero, new Vector3(0.125f, 0.125f, 0.03f), Theme.Sun);
            Part(gift, PrimitiveType.Cube, Vector3.zero, new Vector3(0.03f, 0.125f, 0.125f), Theme.Sun);
            return gift;
        }

        /// <summary>A big phone floating at Keza's side, its screen towards the phone, for the online message.</summary>
        GameObject BigPhone(Vector3 at)
        {
            var phone = Prop("Phone", at + Vector3.up * 0.5f, away);
            Part(phone, PrimitiveType.Cube, Vector3.zero, new Vector3(0.4f, 0.72f, 0.02f), Theme.Hex("#22252B"));
            Part(phone, PrimitiveType.Cube, new Vector3(0f, 0f, -0.011f), new Vector3(0.36f, 0.64f, 0.002f), Theme.Hex("#EAF4FB"));
            return phone;
        }

        /// <summary>Paper money in the visitor's hand: a few green notes.</summary>
        GameObject Money(ARFigure holder)
        {
            var money = Held(holder, "Money", new Vector3(-0.16f, 0.32f, 0.1f));
            for (int i = 0; i < 3; i++)
                Part(money, PrimitiveType.Cube, new Vector3(i * 0.008f, i * 0.006f, 0f), new Vector3(0.1f, 0.05f, 0.004f), Theme.Hex(i == 1 ? "#4C9A62" : "#6BBF7E"), new Vector3(0f, 0f, i * 12f - 12f));
            return money;
        }

        /// <summary>A cardboard box held in front of the body: "help me carry this inside".</summary>
        GameObject Box(ARFigure holder)
        {
            var box = Held(holder, "Box", new Vector3(0f, 0.3f, 0.12f));
            Part(box, PrimitiveType.Cube, Vector3.zero, new Vector3(0.18f, 0.13f, 0.12f), Theme.Hex("#B98B57"));
            Part(box, PrimitiveType.Cube, new Vector3(0f, 0.066f, 0f), new Vector3(0.18f, 0.004f, 0.03f), Theme.Hex("#D9C29A"));
            return box;
        }

        /// <summary>A football at the visitor's feet.</summary>
        GameObject Ball(ARFigure holder)
        {
            var ball = Held(holder, "Ball", new Vector3(0.1f, 0.05f, 0.12f));
            Part(ball, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.1f, Color.white);
            Part(ball, PrimitiveType.Sphere, new Vector3(0f, 0.012f, 0.03f), Vector3.one * 0.05f, Theme.Hex("#22252B"));
            return ball;
        }

        /// <summary>A small puppy on a lead at the visitor's side (the lost-puppy trick).</summary>
        GameObject Puppy(ARFigure holder)
        {
            var dog = Held(holder, "Puppy", new Vector3(0.14f, 0f, 0.08f));
            var model = Resources.Load<GameObject>("Props/dog");   // the family's real dog, if built
            if (model != null)
            {
                var real = Instantiate(model, dog.transform, false);
                foreach (var c in real.GetComponentsInChildren<Collider>()) Destroy(c);
                real.transform.localScale = model.transform.localScale * 0.8f;
                Part(dog, PrimitiveType.Cylinder, new Vector3(-0.07f, 0.2f, 0.03f), new Vector3(0.004f, 0.11f, 0.004f), Theme.Unsafe, new Vector3(0f, 0f, 40f));   // the lead
                return dog;
            }
            var brown = Theme.Hex("#A8743E");
            Part(dog, PrimitiveType.Capsule, new Vector3(0f, 0.07f, 0f), new Vector3(0.06f, 0.06f, 0.06f), brown, new Vector3(90f, 0f, 0f));
            Part(dog, PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0.07f), Vector3.one * 0.06f, brown);
            Part(dog, PrimitiveType.Sphere, new Vector3(0f, 0.105f, 0.1f), Vector3.one * 0.022f, Theme.Hex("#22252B"));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(dog, PrimitiveType.Sphere, new Vector3(side * 0.025f, 0.13f, 0.06f), new Vector3(0.018f, 0.04f, 0.012f), Theme.Hex("#6E4826"));
                Part(dog, PrimitiveType.Cylinder, new Vector3(side * 0.02f, 0.025f, 0.035f), new Vector3(0.016f, 0.025f, 0.016f), brown);
                Part(dog, PrimitiveType.Cylinder, new Vector3(side * 0.02f, 0.025f, -0.035f), new Vector3(0.016f, 0.025f, 0.016f), brown);
            }
            Part(dog, PrimitiveType.Cylinder, new Vector3(-0.07f, 0.2f, 0.03f), new Vector3(0.004f, 0.11f, 0.004f), Theme.Unsafe, new Vector3(0f, 0f, 40f));   // the lead
            return dog;
        }

        /// <summary>The phone's screen lights up, so it is easy to see from far away.</summary>
        GameObject Glowing(GameObject phone)
        {
            Part(phone, PrimitiveType.Cube, new Vector3(0f, 0f, 0.006f), new Vector3(0.052f, 0.095f, 0.002f), Theme.Hex("#8FD3FF"));
            return phone;
        }

        /// <summary>An empty prop in the person's hands (or at their feet), in metres whatever the model's own scale.</summary>
        GameObject Held(ARFigure holder, string name, Vector3 local)
        {
            var thing = Prop(name, holder.transform.position, holder.transform.forward);
            thing.transform.SetParent(holder.transform, true);
            float s = Mathf.Max(holder.transform.lossyScale.y, 0.0001f);
            thing.transform.localPosition = local / s;
            thing.transform.localRotation = Quaternion.identity;
            thing.transform.localScale = Vector3.one / s;
            return thing;
        }

        /// <summary>A small phone in the older cousin's hand; its screen shows nothing.</summary>
        GameObject SmallPhone(ARFigure holder)
        {
            var phone = Prop("Phone", holder.transform.position, holder.transform.forward);
            phone.transform.SetParent(holder.transform, true);
            float s = Mathf.Max(holder.transform.lossyScale.y, 0.0001f);
            phone.transform.localPosition = new Vector3(-0.15f, 0.34f, 0.12f) / s;
            phone.transform.localScale = Vector3.one / s;
            Part(phone, PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.11f, 0.01f), Theme.Hex("#22252B"));
            return phone;
        }

        void Fence(Vector3 start, int posts)
        {
            var fence = Prop("Fence", start, -away);
            for (int i = 0; i < posts; i++)
                Part(fence, PrimitiveType.Cube, new Vector3(-i * 0.22f, 0.14f, 0f), new Vector3(0.05f, 0.28f, 0.03f), Color.white);
            foreach (float y in new[] { 0.09f, 0.21f })
                Part(fence, PrimitiveType.Cube, new Vector3(-(posts - 1) * 0.11f, y, 0f), new Vector3((posts - 1) * 0.22f + 0.05f, 0.035f, 0.02f), Color.white);
        }

        /// <summary>A health-centre sign: a white board with a cross on a post.</summary>
        void Sign(Vector3 at, Color cross)
        {
            var sign = Prop("Health centre", at, -away);
            Part(sign, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.03f, 0.5f, 0.03f), Theme.Hex("#9AA3AF"));
            Part(sign, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 0.3f, 0.02f), Color.white);
            Part(sign, PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.012f), new Vector3(0.2f, 0.06f, 0.005f), cross);
            Part(sign, PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.012f), new Vector3(0.06f, 0.2f, 0.005f), cross);
        }

        /// <summary>
        /// A story at school: in the 3D world it plays in the classroom (blackboard, desks, sun through the
        /// windows); in the camera picture a small blackboard on legs stands behind Keza.
        /// </summary>
        void Board(Vector3 at)
        {
            if (game.AR.Stage != null)
            {
                game.AR.Stage.ShowClassroom(true);
                if (game.AR.Stage.InClassroom) return;
            }
            var board = Prop("School board", at, -away);
            Part(board, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 0.42f, 0.03f), Theme.Hex("#2F5D46"));
            Part(board, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0.005f), new Vector3(0.8f, 0.47f, 0.025f), Theme.Hex("#9C6B3F"));
            foreach (float side in new[] { -0.32f, 0.32f })
                Part(board, PrimitiveType.Cube, new Vector3(side, 0.15f, 0f), new Vector3(0.04f, 0.3f, 0.04f), Theme.Hex("#9C6B3F"));
        }

        /// <summary>The end of a race: two posts with a tape between them.</summary>
        void FinishLine(Vector3 at)
        {
            var line = Prop("Finish line", at, -away);
            foreach (float side in new[] { -0.45f, 0.45f })
                Part(line, PrimitiveType.Cylinder, new Vector3(side, 0.2f, 0f), new Vector3(0.03f, 0.2f, 0.03f), Color.white);
            for (int i = 0; i < 6; i++)
                Part(line, PrimitiveType.Cube, new Vector3(-0.375f + i * 0.15f, 0.34f, 0f), new Vector3(0.15f, 0.04f, 0.01f), i % 2 == 0 ? Theme.Unsafe : Color.white);
        }

        void Bench(Vector3 at)
        {
            var bench = Prop("Bench", at, -away);
            var wood = Theme.Hex("#B07A45");
            Part(bench, PrimitiveType.Cube, new Vector3(0f, 0.17f, 0f), new Vector3(0.6f, 0.035f, 0.2f), wood);
            Part(bench, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.09f), new Vector3(0.6f, 0.15f, 0.03f), wood);
            foreach (float side in new[] { -0.26f, 0.26f })
                Part(bench, PrimitiveType.Cube, new Vector3(side, 0.08f, 0f), new Vector3(0.04f, 0.16f, 0.17f), Theme.Hex("#6B4A2B"));
        }

        // ---- Things that glow in the dark ----

        readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>(), halos = new Dictionary<Color, Material>();

        /// <summary>A part that shines by itself (a lamp, a lit window): the same bright colour in the dark.</summary>
        GameObject Bright(GameObject parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = Part(parent, type, position, scale, color);
            Material m;
            // Drawn after the solid things (ARFx.Unlit writes no depth): a wall behind would otherwise paint over it.
            if (!lit.TryGetValue(color, out m)) lit[color] = m = ARFx.Unlit(color);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>A soft halo of light that always faces the phone, around a lamp or a headlight.</summary>
        void Halo(GameObject parent, Vector3 position, float size, Color color)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Halo";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent.transform, false);
            quad.transform.localPosition = position;
            quad.transform.localScale = Vector3.one * size;
            Material m;
            if (!halos.TryGetValue(color, out m)) halos[color] = m = ARFx.Unlit(color, ARFx.SoftDot);
            var r = quad.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quad.AddComponent<Billboard>();
        }

        /// <summary>A street lamp: a dark post, a glowing lamp with its halo and light, and a warm pool on the ground.</summary>
        void StreetLamp(Vector3 at)
        {
            var lamp = Prop("Street lamp", at, -away);
            var post = Theme.Hex("#2A2D33");
            Part(lamp, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.035f, 0.45f, 0.035f), post);
            Part(lamp, PrimitiveType.Cube, new Vector3(0f, 0.92f, 0.08f), new Vector3(0.03f, 0.03f, 0.2f), post);
            Part(lamp, PrimitiveType.Cube, new Vector3(0f, 0.93f, 0.17f), new Vector3(0.1f, 0.03f, 0.08f), post);
            Bright(lamp, PrimitiveType.Cube, new Vector3(0f, 0.905f, 0.17f), new Vector3(0.08f, 0.015f, 0.06f), new Color(1f, 0.9f, 0.62f));
            Halo(lamp, new Vector3(0f, 0.89f, 0.17f), 0.45f, new Color(1f, 0.82f, 0.5f, 0.5f));
            var pool = ARFx.FloorQuad("Light pool", ARFx.SoftDot, new Color(1f, 0.8f, 0.48f, 0.3f), 1.1f);
            pool.transform.SetParent(lamp.transform, false);
            pool.transform.localPosition = new Vector3(0f, 0.004f, 0.17f);
            if (game.AR.Stage == null || !game.AR.Stage.Night) return;   // the light itself only matters in the dark
            var light = new GameObject("Lamp light").AddComponent<Light>();
            light.transform.SetParent(lamp.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.85f, 0.17f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.52f);
            light.range = 1.8f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;
        }

        /// <summary>The school gate: two pillars, a beam with the school's name, and a light on each pillar.</summary>
        void SchoolGate(Vector3 at)
        {
            var gate = Prop("School gate", at, -away);
            var wall = Theme.Hex("#E9DFC9");
            foreach (float side in new[] { -1f, 1f })
            {
                Part(gate, PrimitiveType.Cube, new Vector3(side * 0.3f, 0.3f, 0f), new Vector3(0.09f, 0.6f, 0.09f), wall);
                Bright(gate, PrimitiveType.Sphere, new Vector3(side * 0.3f, 0.63f, 0f), Vector3.one * 0.06f, new Color(1f, 0.92f, 0.7f));
                Part(gate, PrimitiveType.Cube, new Vector3(side * 0.5f, 0.11f, 0f), new Vector3(0.32f, 0.22f, 0.05f), wall);   // a low wall either side
            }
            Part(gate, PrimitiveType.Cube, new Vector3(0f, 0.56f, 0f), new Vector3(0.7f, 0.1f, 0.08f), Theme.Hex("#2474C6"));
            // The name on the front of the beam, facing the phone (a TextMesh reads from its back, so it is turned round).
            var name = new GameObject("School name");
            name.transform.SetParent(gate.transform, false);
            name.transform.localPosition = new Vector3(0f, 0.56f, 0.045f);
            name.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var text = name.AddComponent<TextMesh>();
            text.font = ARFx.Font;
            name.GetComponent<MeshRenderer>().sharedMaterial = ARFx.Font.material;
            text.text = T("story.school", "SCHOOL");
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.0075f;
            text.color = Color.white;
        }

        /// <summary>A market stall: a table of fruit under a striped roof, lit by a bulb.</summary>
        void Stall(Vector3 at)
        {
            var stall = Prop("Market stall", at, -away);
            var wood = Theme.Hex("#9C6B3F");
            Part(stall, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.62f, 0.03f, 0.3f), wood);
            Part(stall, PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.13f), new Vector3(0.62f, 0.2f, 0.02f), Theme.Hex("#7A5232"));
            foreach (float side in new[] { -0.29f, 0.29f })
                Part(stall, PrimitiveType.Cube, new Vector3(side, 0.3f, 0.12f), new Vector3(0.025f, 0.6f, 0.025f), wood);
            for (int i = 0; i < 5; i++)   // a striped roof
                Part(stall, PrimitiveType.Cube, new Vector3(-0.26f + i * 0.13f, 0.6f, 0.04f), new Vector3(0.13f, 0.02f, 0.3f), i % 2 == 0 ? Theme.Hex("#E4574C") : Color.white, new Vector3(-12f, 0f, 0f));
            Color[] fruit = { Theme.Hex("#F39A2B"), Theme.Hex("#F6D33C"), Theme.Hex("#6DBE45"), Theme.Hex("#E4574C") };
            for (int i = 0; i < 12; i++)
                Part(stall, PrimitiveType.Sphere, new Vector3(-0.24f + (i % 6) * 0.095f, 0.235f + (i / 6) * 0.03f, -0.06f + (i / 6) * 0.07f), Vector3.one * 0.05f, fruit[i % fruit.Length]);
            Bright(stall, PrimitiveType.Sphere, new Vector3(-0.2f, 0.56f, 0.06f), Vector3.one * 0.04f, new Color(1f, 0.92f, 0.65f));
            Halo(stall, new Vector3(-0.2f, 0.56f, 0.06f), 0.24f, new Color(1f, 0.85f, 0.55f, 0.45f));
        }

        /// <summary>A small shop, bright inside: a lit window and open door, and a sign.</summary>
        void Shop(Vector3 at)
        {
            var shop = Prop("Shop", at, -away);
            // Its front faces the phone (local +z), the building behind it.
            Part(shop, PrimitiveType.Cube, new Vector3(0f, 0.35f, -0.15f), new Vector3(0.95f, 0.7f, 0.3f), Theme.Hex("#D8C7A6"));
            Part(shop, PrimitiveType.Cube, new Vector3(0f, 0.72f, -0.12f), new Vector3(1.02f, 0.05f, 0.38f), Theme.Hex("#8A3B2E"));
            var light = new Color(1f, 0.9f, 0.66f);
            Bright(shop, PrimitiveType.Cube, new Vector3(-0.22f, 0.36f, 0.004f), new Vector3(0.36f, 0.26f, 0.01f), light);   // the window
            Bright(shop, PrimitiveType.Cube, new Vector3(0.24f, 0.24f, 0.004f), new Vector3(0.2f, 0.46f, 0.01f), light);     // the open door
            Part(shop, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0.012f), new Vector3(0.5f, 0.1f, 0.02f), Theme.Hex("#2BA3A0"));
            var pool = ARFx.FloorQuad("Light pool", ARFx.SoftDot, new Color(1f, 0.85f, 0.55f, 0.35f), 1f);
            pool.transform.SetParent(shop.transform, false);
            pool.transform.localPosition = new Vector3(0f, 0.004f, 0.3f);
        }

        /// <summary>Headlights on a car: bright lamps with halos and long pools of light on the road ahead.</summary>
        void Headlights(GameObject vehicle)
        {
            var body = vehicle.transform.Find("Body");
            var parent = body != null ? body.gameObject : vehicle;
            foreach (float side in new[] { -1f, 1f })
                Halo(parent, new Vector3(side * 0.14f, 0.165f, 0.5f), 0.22f, new Color(1f, 0.96f, 0.8f, 0.8f));
            var beam = ARFx.FloorQuad("Headlight beam", ARFx.SoftDot, new Color(1f, 0.95f, 0.8f, 0.35f), 1f);
            beam.transform.SetParent(vehicle.transform, false);
            beam.transform.localPosition = new Vector3(0f, 0.006f, 0.95f);
            beam.transform.localScale = new Vector3(0.6f, 1.1f, 1f);   // the quad lies flat: its y runs along the road
        }
    }
}
