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
    /// simple shapes, like the playground, so there are no model files.
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
            home = homeSpot;
            var cam = game.AR.CameraTransform.position;
            away = Vector3.ProjectOnPlane(home - cam, Vector3.up).normalized;
            if (away == Vector3.zero) away = Vector3.forward;
            right = Vector3.Cross(Vector3.up, away);
            var story = Story(scenarioId);
            if (story != null) yield return story;
        }

        /// <summary>Stops the story, removes its people and props and gives Keza her normal walking speed back.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            RemoveAll();
        }

        void RemoveAll()
        {
            foreach (var f in cast) if (f != null) Destroy(f.gameObject);
            cast.Clear();
            foreach (var p in props) if (p != null) Destroy(p);
            props.Clear();
            other = friend = null;
            car = held = null;
            if (guide != null)
            {
                guide.WalkSpeed = guideSpeed;
                guide.Say(null, null);
            }
        }

        void OnDestroy()
        {
            foreach (var m in paints.Values) if (m != null) Destroy(m);
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
            other = Actor(T("story.cousin", "Cousin"), Cousin, 1.5f, 0.2f, "child");
            yield return Walk(other, At(0.3f, 0.05f), 0.6f);
            yield return Line(other, T("story.S02.1", "Tickle, tickle, tickle!"));
            FxAnimator.Shake(guide.gameObject);
            yield return Line(guide, T("story.S02.2", "Ha ha! Stop, please!"));
            yield return Circle(other, 0.3f, 1.6f);
            yield return Line(other, T("story.S02.3", "No, more tickles!"));
            FxAnimator.Shake(guide.gameObject);
            yield return Line(guide, T("story.S02.4", "I said stop. I don't like it now."));
        }

        IEnumerator TouchingSecret()
        {
            Fence(At(0.3f, 0.95f), 6);
            other = Actor(T("story.neighbour", "Neighbour"), Neighbour, 1.6f, 0.6f);
            yield return Walk(other, At(0.6f, 0.3f), 0.35f);
            yield return Line(other, T("story.S03.1", "Let's keep a touching secret."));
            yield return Line(other, T("story.S03.2", "Don't tell your parents."));
            FxAnimator.Shake(guide.gameObject);
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
            car = Car(Theme.Hex("#4A5568"));
            var stop = At(0.35f, road);
            var start = At(-6f, road);
            game.Audio.PlaySFX("car");
            yield return Drive(car, start, stop, 2.4f, true);
            game.Audio.PlaySFX("horn");
            yield return new WaitForSeconds(0.5f);

            other = Actor(T("story.stranger", "Someone new"), Stranger, 0.35f, road - 0.35f);
            yield return Walk(other, At(0.75f, 0.1f), 0.45f);
            held = Sweets(other);
            game.Audio.PlaySFX("pop");
            yield return Line(other, T("story.S05.1", "Hi! Do you like sweets?"));
            yield return Line(other, T("story.S05.2", "Come with me. My car is right here!"));
            guide.FaceTowards(other.transform.position);
            FxAnimator.Shake(guide.gameObject);
            yield return Line(guide, T("story.S05.3", "Hmm... I don't know you."));
        }

        IEnumerator TooClose()
        {
            Board(At(-0.95f, 1.25f));
            other = Actor(T("story.classmate", "Classmate"), Classmate, 1.4f, 0.4f, "child");
            yield return Walk(other, At(0.17f, 0.02f), 0.5f);
            yield return Line(guide, T("story.S06.1", "Can you move back, please?"));
            yield return Line(other, T("story.S06.2", "No. I want to stand right here."));
            FxAnimator.Shake(guide.gameObject);
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
            yield return Message(held, T("story.S08.1", "Hi! I'm your new friend."), 0.2f);
            yield return Message(held, T("story.S08.2", "Send me photos of you."), 0f);
            yield return Message(held, T("story.S08.3", "Keep it our secret!"), -0.2f);
            guide.FaceTowards(held.transform.position);
            FxAnimator.Shake(guide.gameObject);
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
            FxAnimator.Shake(guide.gameObject);
            yield return Line(guide, T("story.S09.3", "A secret? I don't like that."));
        }

        IEnumerator Lap()
        {
            Bench(At(0.7f, 0.35f));
            other = Actor(T("story.grown_up", "Grown-up"), Theme.Hex("#9C6B3F"), 0.95f, 0.38f);
            yield return Line(other, T("story.S10.1", "Come and sit on my lap."));
            yield return Line(guide, T("story.S10.2", "I don't want to."));
            yield return Line(other, T("story.S10.3", "Come on, just for a little bit!"));
            FxAnimator.Shake(guide.gameObject);
        }

        IEnumerator PicturesOnAPhone()
        {
            other = Actor(T("story.older_cousin", "Older cousin"), Theme.Hex("#C0504D"), 1.4f, 0.3f, "child");
            other.transform.localScale *= 1.2f;   // a few years older than Keza
            yield return Walk(other, At(0.45f, 0.1f), 0.45f);
            held = SmallPhone(other);
            yield return Line(other, T("story.S11.1", "Look at these pictures!"));
            yield return Line(other, T("story.S11.2", "It's our secret, okay?"));
            FxAnimator.Shake(guide.gameObject);
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
            FxAnimator.Shake(guide.gameObject);
            yield return Line(guide, T("story.S12.4", "Oh no..."));
        }

        IEnumerator KissHello()
        {
            other = Actor(T("story.uncle", "Uncle"), Uncle, 1.5f, 0.3f);
            yield return Walk(other, At(0.42f, 0.06f), 0.45f);
            yield return Line(other, T("story.S13.1", "Hello! Give your uncle a kiss!"));
            yield return Line(guide, T("story.S13.2", "I don't want to..."));
            yield return Line(other, T("story.S13.3", "You must kiss me hello!"));
            FxAnimator.Shake(guide.gameObject);
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
                    question = what, right = T("story.S02.right", "Say STOP in a loud voice"), wrong = T("story.S02.wrong", "Keep laughing"),
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
                    question = what, right = T("story.S05.right", "Say NO and run to a grown-up"), wrong = T("story.S05.wrong", "Go and get the sweets"),
                    wrongNote = T("story.S05.wrong_note", "Never go with someone you don't know, even for sweets, toys or a ride. Say NO and run to a grown-up you trust."),
                    praise = T("story.S05.praise", "That's it! Say NO, run to a grown-up you trust, and tell them what happened."),
                    sayNo = true, play = RunFromTheCar,
                };
                case "S06": return new ActOut
                {
                    question = what, right = T("story.S06.right", "Step back and ask a teacher for help"), wrong = T("story.S06.wrong", "Do nothing"),
                    wrongNote = T("story.S06.wrong_note", "Your bubble is yours. Step back, and if they still won't move, ask a grown-up for help."),
                    praise = T("story.S06.praise", "Good choice! If someone won't give you space, a grown-up can help."),
                    play = AskTeacher,
                };
                case "S08": return new ActOut
                {
                    question = what, right = T("story.S08.right", "Don't reply. Show a grown-up"), wrong = T("story.S08.wrong", "Send a photo"),
                    wrongNote = T("story.S08.wrong_note", "Never send photos of yourself to people online, and never keep their messages secret. Show a grown-up you trust."),
                    praise = T("story.S08.praise", "Exactly right. Show a grown-up any message that asks for photos or secrets."),
                    play = () => PutAwayAndTellMum(T("story.S08.tell", "Mum, look at this message.")),
                };
                case "S09": return new ActOut
                {
                    question = what, right = T("story.S09.right", "Give it back and tell a grown-up"), wrong = T("story.S09.wrong", "Keep the gift and the secret"),
                    wrongNote = T("story.S09.wrong_note", "A gift that comes with a secret is a warning sign. You can say no thank you and tell a grown-up."),
                    praise = T("story.S09.praise", "Well done. Gifts should never come with secrets. A grown-up you trust needs to know."),
                    play = GiveItBack,
                };
                case "S10": return new ActOut
                {
                    question = what, right = T("story.S10.right", "Say no thank you, sit by Mum"), wrong = T("story.S10.wrong", "Sit on the lap anyway"),
                    wrongNote = T("story.S10.wrong_note", "You can say no to sitting on a lap, even with someone you know. Your body belongs to you."),
                    praise = T("story.S10.praise", "Good! You never have to sit on anyone's lap if you don't want to."),
                    play = () => NoThenTellMum(T("story.S10.tell", "Can I sit with you, Mum?")),
                };
                case "S11": return new ActOut
                {
                    question = what, right = T("story.S11.right", "Walk away and tell a grown-up"), wrong = T("story.S11.wrong", "Keep looking"),
                    wrongNote = T("story.S11.wrong_note", "You don't have to look. Walk away and tell a grown-up you trust, even if they say it's a secret."),
                    praise = T("story.S11.praise", "That was brave. Walking away and telling a grown-up is the right thing to do."),
                    play = () => PutAwayAndTellMum(T("story.S11.tell", "Mum, I saw pictures I didn't like.")),
                };
                case "S12": return new ActOut
                {
                    question = what, right = T("story.S12.right", "Tell a trusted grown-up together"), wrong = T("story.S12.wrong", "Keep your friend's secret"),
                    wrongNote = T("story.S12.wrong_note", "Keeping this secret won't help your friend. A good friend tells a grown-up who can help."),
                    praise = T("story.S12.praise", "You're a good friend. Some secrets are too big to keep. A grown-up can help your friend."),
                    play = TellTogether,
                };
                case "S13": return new ActOut
                {
                    question = what, right = T("story.S13.right", "Give a wave or a high-five"), wrong = T("story.S13.wrong", "Kiss him because he said so"),
                    wrongNote = T("story.S13.wrong_note", "You don't have to kiss anyone, even family. A wave or a high-five is a fine hello."),
                    praise = T("story.S13.praise", "Great! You choose how to say hello."),
                    play = HighFiveInstead,
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
            guide.WalkSpeed = 0.9f;
            var run = Walk(guide, At(-0.62f, -0.15f), 0.9f);
            if (other != null && car != null) StartCoroutine(LeaveInTheCar());
            yield return run;
            guide.FaceTowards(mum.transform.position);
            guide.Bounce();
            mum.Bounce();
            game.Audio.PlaySFX("correct");
            yield return Line(mum, T("story.S05.mum", "You did the right thing. Tell me what happened."));
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
            yield return Walk(guide, At(-0.58f, -0.12f), 0.55f);
            guide.FaceTowards(mum.transform.position);
            yield return Line(guide, tell);
            mum.Bounce();
            game.Audio.PlaySFX("correct");
            yield return Line(mum, T("story.mum.thanks", "Thank you for telling me. You did the right thing."));
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
            var label = ARFx.Label(text, phone.transform.position + Vector3.up * height - right * 0.02f - away * 0.05f, Theme.Primary, game.AR.UsesAR ? 0.7f : 1f);
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
            var road = Prop("Road", At(0f, z) + Vector3.up * 0.002f, right);
            Part(road, PrimitiveType.Cube, Vector3.zero, new Vector3(0.62f, 0.004f, 14f), Theme.Hex("#6B7280"));
            for (int i = -10; i <= 10; i++)
                Part(road, PrimitiveType.Cube, new Vector3(0f, 0.003f, i * 0.6f), new Vector3(0.04f, 0.004f, 0.28f), Color.white);
        }

        /// <summary>A small car: body, cabin with windows, wheels and lights. Its front is local +z.</summary>
        GameObject Car(Color paint)
        {
            var root = Prop("Car", At(-6f, 2f), right);
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.42f, 0.17f, 0.95f), paint);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.31f, -0.08f), new Vector3(0.38f, 0.16f, 0.5f), paint);
            var glass = Theme.Hex("#BFE3F7");
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.315f, -0.08f), new Vector3(0.392f, 0.1f, 0.44f), glass);
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.315f, 0.172f), new Vector3(0.33f, 0.1f, 0.012f), glass);
            foreach (float side in new[] { -1f, 1f })
            {
                Part(body, PrimitiveType.Sphere, new Vector3(side * 0.13f, 0.16f, 0.47f), Vector3.one * 0.06f, Theme.Hex("#FFF4C2"));
                Part(body, PrimitiveType.Sphere, new Vector3(side * 0.14f, 0.16f, -0.47f), Vector3.one * 0.05f, Theme.Unsafe);
                foreach (float end in new[] { -0.3f, 0.3f })
                    Part(root, PrimitiveType.Cylinder, new Vector3(side * 0.2f, 0.075f, end), new Vector3(0.15f, 0.03f, 0.15f), Theme.Hex("#22252B"), new Vector3(0f, 0f, 90f));
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

        /// <summary>A school board on legs, so the classroom stories look like school.</summary>
        void Board(Vector3 at)
        {
            var board = Prop("School board", at, -away);
            Part(board, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.75f, 0.42f, 0.03f), Theme.Hex("#2F5D46"));
            Part(board, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0.005f), new Vector3(0.8f, 0.47f, 0.025f), Theme.Hex("#9C6B3F"));
            foreach (float side in new[] { -0.32f, 0.32f })
                Part(board, PrimitiveType.Cube, new Vector3(side, 0.15f, 0f), new Vector3(0.04f, 0.3f, 0.04f), Theme.Hex("#9C6B3F"));
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
    }
}
