using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The AR lesson HUD, split so the people in the middle of the screen have room:
    ///   top     - exit, lesson title and step, and a line of small round tools (read aloud, day or night,
    ///             move, closer and further);
    ///             under them the question, in a speech bubble with a "?" in the lesson's colour;
    ///   middle  - the room, free of cards;
    ///   bottom  - a floating hint, the move pad when it is open, and the answers as big solid buttons
    ///             side by side, each with its letter (A, B...), within thumb reach.
    /// People are moved by dragging them in the room (ARTouch), or with the move pad, which walks and
    /// turns whoever the child last touched; while one is dragged, the hint says which of the guide's
    /// zones they are in.
    /// </summary>
    public class LessonScreen : UIScreen, ITickable
    {
        static readonly string[] Letters = { "A", "B", "C", "D", "E", "F" };
        static readonly Color NightBlue = Theme.Hex("#3E4A8C");

        Text title, step, heading, body, hint;
        Image badge, hintPill, dayNightIcon;
        RectTransform answers;
        Button movePlace, dayNight;
        readonly List<Button> optionButtons = new List<Button>();
        readonly List<string> optionLabels = new List<string>();
        GameObject question;
        readonly VoicePanel voice = new VoicePanel();
        string touchHint = "";
        float showZoneUntil;
        bool padForLesson, padForChild;

        public override string Name { get { return "lesson"; } }
        public override bool ShowsCamera { get { return true; } }

        protected override void BuildContent(RectTransform root)
        {
            // Soft shade behind the header so white text reads over any room.
            var shade = UIFactory.Panel(root, "Top shade", Theme.Scrim, false);
            var shadeRt = (RectTransform)shade.transform;
            shadeRt.anchorMin = new Vector2(0f, 1f);
            shadeRt.anchorMax = new Vector2(1f, 1f);
            shadeRt.pivot = new Vector2(0.5f, 1f);
            shadeRt.sizeDelta = new Vector2(0f, 400f);
            shade.raycastTarget = false;

            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            title = TitleText;
            title.color = Color.white;
            // The step goes under the title, in the top bar, so the tools get a line of their own.
            var bar = title.transform.parent;
            var titles = UIFactory.Rect(bar, "Title and step");
            titles.SetSiblingIndex(title.transform.GetSiblingIndex());
            var stack = UIFactory.Column(titles, 0f, 0);
            stack.childAlignment = TextAnchor.MiddleLeft;
            titles.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            title.transform.SetParent(titles, false);
            step = UIFactory.Label(titles, "", Theme.Label, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.FitInside(step);

            // Round picture buttons, like a game's, that small children know without reading: read aloud,
            // day or night, the move pad, closer and further, and (in the camera picture) place again.
            var tools = UIFactory.Row(col, "Tools", 22f, 112f);
            tools.childForceExpandWidth = false;
            tools.childAlignment = TextAnchor.MiddleRight;
            Image unused;
            Tool(tools.transform, Icons.Speaker, Theme.Primary, ReadCard, out unused);
            dayNight = Tool(tools.transform, Icons.Moon, NightBlue, ToggleNight, out dayNightIcon);
            Tool(tools.transform, Icons.Move, Theme.Safe, ToggleMovePad, out unused);
            var closer = Tool(tools.transform, Icons.ZoomIn, Theme.Hex("#2B8FB8"), null, out unused);
            Hold(closer, () => game.AR.ChangeSize(1.15f), 0.18f);
            var further = Tool(tools.transform, Icons.ZoomOut, Theme.Hex("#2B8FB8"), null, out unused);
            Hold(further, () => game.AR.ChangeSize(1f / 1.15f), 0.18f);
            movePlace = Tool(tools.transform, Icons.Target, Theme.Hex("#C77A1E"), () => game.MoveCharacter(), out unused);

            BuildQuestion(col);

            // The room shows through here.
            UIFactory.Spacer(col, 1f);
            BuildHintPill(col);
            BuildMovePad(col);

            answers = UIFactory.Rect(col, "Answers");
            UIFactory.Column(answers, 26f, 0);
            answers.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;

            // The "Say NO!" card takes the question's and the answers' place while it listens. Its NO power
            // meter shows the child's record, their loudest NO, and keeps a louder one (a number, never the sound).
            voice.Build(col);
            voice.Record = () => game.Progress.LoudestNo;
            voice.KeepRecord = power => game.Progress.RecordNoPower(power);
            voice.PlaySound = sound => game.Audio.PlaySFX(sound);
            // When a spoken NO is heard, the guide answers in the room too, so the child sees they were heard.
            voice.Heard = rating =>
            {
                var guide = game.AR.Character;
                if (guide == null) return;
                guide.Bounce();
                guide.SayFor(Localization.T("voice.heard.keza", "I heard your NO!"), game.AR.OpaqueMaterial, 2.5f);
            };

            // (The screenshot tool builds the screens without starting AR: no touch there.)
            if (game.AR.Touch != null)
            {
                game.AR.Touch.DragMoved += ShowZone;
                game.AR.Touch.DragEnded += f => showZoneUntil = Time.time + 2f;
            }
        }

        /// <summary>A round coloured button with a white picture on it.</summary>
        static Button Tool(Transform parent, Sprite icon, Color colour, System.Action click, out Image picture)
        {
            var b = UIFactory.Button(parent, "", colour, Color.white, click);
            b.image.sprite = UIFactory.Circle;
            b.image.type = Image.Type.Simple;
            var le = b.GetComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.minWidth = le.preferredWidth = 112f;
            le.minHeight = le.preferredHeight = 112f;
            picture = Picture(b.transform, icon, 64f);
            return b;
        }

        static Image Picture(Transform parent, Sprite icon, float size)
        {
            var picture = UIFactory.Panel(parent, "Icon", Color.white, false);
            picture.sprite = icon;
            picture.raycastTarget = false;
            var rt = (RectTransform)picture.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            return picture;
        }

        /// <summary>The question, on its own at the top: a speech bubble with a big "?" in the lesson's colour.</summary>
        void BuildQuestion(RectTransform col)
        {
            var card = UIFactory.Panel(col, "Question", Theme.WithAlpha(Theme.Card, 0.96f), true);
            var row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(26, 36, 26, 30);
            row.spacing = 26f;
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            card.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
            // A soft shadow lifts the bubble off the room.
            var lift = card.gameObject.AddComponent<Shadow>();
            lift.effectColor = new Color(0f, 0f, 0f, 0.25f);
            lift.effectDistance = new Vector2(0f, -8f);

            badge = UIFactory.Panel(card.transform, "Badge", Theme.Primary, false);
            badge.sprite = UIFactory.Circle;
            var ble = badge.gameObject.AddComponent<LayoutElement>();
            ble.minWidth = ble.preferredWidth = 92f;
            ble.minHeight = ble.preferredHeight = 92f;
            ble.flexibleWidth = 0f;
            var mark = UIFactory.Label(badge.transform, "?", Theme.Title, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch((RectTransform)mark.transform);

            var words = UIFactory.Rect(card.transform, "Words");
            UIFactory.Column(words, 6f, 0);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            heading = UIFactory.Label(words, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(words, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            question = card.gameObject;
        }

        /// <summary>A dark rounded pill floating over the room, with one line about what touch does now.</summary>
        void BuildHintPill(RectTransform col)
        {
            var row = UIFactory.Rect(col, "Hint row");
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            // Only as tall as its line of words: a stretchable group here would soak up the
            // room's space and cover the guide (as UIFactory.Row explains).
            row.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
            hintPill = UIFactory.Panel(row, "Hint", Theme.WithAlpha(Theme.Ink, 0.78f), true);
            hintPill.raycastTarget = false;   // touches pass through to the room
            var pad = hintPill.gameObject.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(40, 40, 18, 18);
            pad.childControlWidth = pad.childControlHeight = true;
            pad.childForceExpandWidth = false;
            pad.childForceExpandHeight = false;
            var fit = hintPill.gameObject.AddComponent<LayoutElement>();
            fit.flexibleWidth = 0f;
            fit.flexibleHeight = 0f;
            hint = UIFactory.Label(hintPill.transform, "", Theme.Label, Color.white, TextAnchor.MiddleCenter, true);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.GetComponent<LayoutElement>().preferredWidth = 880f;
        }

        GameObject movePad;

        /// <summary>
        /// Round buttons that walk and turn a person: the guide, or whoever the child last touched. Holding an
        /// arrow keeps them walking. For when a person has to get somewhere (a colour, a letter, the safe
        /// place) and dragging is hard. Opened by the move button, or by a lesson that needs it.
        /// </summary>
        void BuildMovePad(RectTransform col)
        {
            var row = UIFactory.Row(col, "Move pad", 18f, 118f);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleCenter;
            var turn = Theme.Hex("#7A5CC7");
            PadButton(row.transform, Icons.TurnLeft, turn, () => game.AR.Turn(-45f));
            PadButton(row.transform, "←", () => game.AR.Step(new Vector2(-1f, 0f)));   // left
            PadButton(row.transform, "↑", () => game.AR.Step(new Vector2(0f, 1f)));    // away from the phone
            PadButton(row.transform, "↓", () => game.AR.Step(new Vector2(0f, -1f)));   // towards the phone
            PadButton(row.transform, "→", () => game.AR.Step(new Vector2(1f, 0f)));    // right
            PadButton(row.transform, Icons.TurnRight, turn, () => game.AR.Turn(45f));
            movePad = row.gameObject;
            movePad.SetActive(false);
        }

        /// <summary>An arrow: holding it keeps the person walking.</summary>
        void PadButton(Transform parent, string glyph, System.Action action)
        {
            var b = Round(parent, Theme.WithAlpha(Theme.Card, 0.95f));
            var label = UIFactory.Label(b.transform, glyph, Theme.Title, Theme.Primary, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch((RectTransform)label.transform);
            label.raycastTarget = false;
            Hold(b, action, 0.2f);
        }

        /// <summary>A turn: an eighth of a circle each time, and on round while it is held.</summary>
        void PadButton(Transform parent, Sprite icon, Color colour, System.Action action)
        {
            var b = Round(parent, colour);
            Picture(b.transform, icon, 70f);
            Hold(b, action, 0.4f);
        }

        static Button Round(Transform parent, Color colour)
        {
            var b = UIFactory.Button(parent, "", colour, Color.white, null);
            b.image.sprite = UIFactory.Circle;
            b.image.type = Image.Type.Simple;
            var le = b.GetComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.minWidth = le.preferredWidth = 118f;
            le.minHeight = le.preferredHeight = 118f;
            return b;
        }

        /// <summary>Acts as soon as the finger touches the button, and again every interval while it stays.</summary>
        static void Hold(Button b, System.Action action, float interval)
        {
            var hold = b.gameObject.AddComponent<HoldRepeat>();
            hold.action = action;
            hold.interval = interval;
        }

        /// <summary>A lesson shows the move pad when the guide has to get somewhere (or hides it again).</summary>
        public void ShowMovePad(bool show)
        {
            padForLesson = show;
            if (!show) padForChild = false;
            ShowPad();
        }

        /// <summary>The move button: the child opens or closes the pad whenever they like.</summary>
        void ToggleMovePad()
        {
            bool open = padForLesson || padForChild;
            padForChild = !open;
            padForLesson = false;
            ShowPad();
        }

        void ShowPad()
        {
            if (movePad != null) movePad.SetActive((padForLesson || padForChild) && !voice.Visible);
        }

        /// <summary>Day or night in the 3D world: the child's choice, until they leave the 3D world.</summary>
        void ToggleNight()
        {
            var stage = game.AR.Stage;
            if (stage == null) return;
            stage.ChooseNight(!stage.Night);
            ShowDayNight();
        }

        void ShowDayNight()
        {
            var stage = game.AR.Stage;
            // Hidden while a story card sets its own time of day (day stories by day, night stories at night).
            dayNight.gameObject.SetActive(stage != null && !stage.StoryTime);
            if (stage == null) return;
            // The button shows where it takes you: the moon by day, the sun by night.
            dayNightIcon.sprite = stage.Night ? Icons.Sun : Icons.Moon;
            dayNight.image.color = stage.Night ? Theme.Sun : NightBlue;
        }

        public void Tick()
        {
            voice.Tick();
            if (showZoneUntil > 0f && Time.time > showZoneUntil)
            {
                showZoneUntil = 0f;
                SetHint(touchHint);
            }
            // A story card sets its own time of day: the button follows it, and hides meanwhile.
            var stage = game.AR.Stage;
            if (stage != null && (dayNightIcon.sprite != (stage.Night ? Icons.Sun : Icons.Moon) || dayNight.gameObject.activeSelf == stage.StoryTime)) ShowDayNight();
        }

        /// <summary>
        /// "Read to me": reads the card on screen aloud, with its answer
        /// buttons, for children who are still learning to read.
        /// </summary>
        void ReadCard()
        {
            if (voice.Visible) { game.ReadToMe(voice.Words); return; }
            var lines = new List<string> { heading.gameObject.activeSelf ? heading.text : "", body.text };
            lines.AddRange(optionLabels);
            game.ReadToMe(lines.ToArray());
        }

        /// <summary>
        /// Swaps the question and answers for the voice card and listens for the child's
        /// NO. done gets the best try, or null if the child chose to tap.
        /// </summary>
        public void ShowVoice(string promptHeading, string promptBody, SafeTouch.Voice.VoiceCoach coach, System.Action<SafeTouch.Voice.NoRating> done)
        {
            if (game.Reader != null) game.Reader.SpeakIfAuto(promptHeading, promptBody);
            ShowCard(false);
            voice.Show(promptHeading, promptBody, coach, rating =>
            {
                ShowCard(true);
                ShowPad();
                if (done != null) done(rating);
            });
            ShowPad();
        }

        /// <summary>Closes the voice card (and the microphone) if it is open.</summary>
        public void HideVoice()
        {
            voice.Hide();
            ShowCard(true);
            ShowPad();
        }

        void ShowCard(bool show)
        {
            question.SetActive(show && (heading.gameObject.activeSelf || body.gameObject.activeSelf));
            answers.gameObject.SetActive(show && optionButtons.Count > 0);
        }

        public override void OnShow()
        {
            movePlace.gameObject.SetActive(game.AR.UsesAR);   // in the 3D world the helper never leaves the playground
            badge.color = LessonInfo.For(Mathf.Max(1, game.CurrentLevel)).color;
            padForChild = false;
            ShowPad();
            ShowDayNight();
            SetHint(touchHint);
        }

        /// <summary>What touching the room does in this step, e.g. "Tap the coloured circles". Empty hides the hint.</summary>
        public void SetTouchHint(string text)
        {
            touchHint = text ?? "";
            showZoneUntil = 0f;
            SetHint(touchHint);
        }

        void SetHint(string text)
        {
            if (hint == null) return;
            hint.text = text;
            hintPill.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>While a person is dragged, the hint says which zone they stand in: the child tests the bubble.</summary>
        void ShowZone(ARFigure figure)
        {
            // Only the bubble lesson and Bubble Guard show the zones; elsewhere the lesson's own hint stays
            // (e.g. "You picked: Safe" while the guide stands on a colour in lesson 2).
            if (game.CurrentLevel != 1 && game.CurrentLevel != Managers.ProgressManager.GameLevel) return;
            // Moving the guide keeps the lesson's own hint (e.g. "You picked: ..." on an answer circle).
            if (figure == null || figure == game.AR.Character) return;
            string zone = game.AR.ZoneOf(figure);
            SetHint(string.Format(Localization.T("move.zone." + zone, ZoneEnglish(zone)), figure.Label));
            showZoneUntil = float.MaxValue;   // until the finger lifts
        }

        static string ZoneEnglish(string zone)
        {
            switch (zone)
            {
                case "red": return "{0} is in Keza's red zone. Only with Keza's OK!";
                case "yellow": return "{0} is in Keza's yellow zone, for trusted people.";
                case "green": return "{0} is in Keza's green zone.";
                default: return "{0} is outside Keza's bubble.";
            }
        }

        public void SetHeader(string lessonTitle, string stepText)
        {
            title.text = lessonTitle;
            step.text = stepText;
        }

        /// <summary>Shows a question or instruction with up to six answer buttons.</summary>
        public void ShowPrompt(string promptHeading, string promptBody, params PromptOption[] choices)
        {
            if (voice.Visible) HideVoice();
            heading.text = promptHeading;
            heading.gameObject.SetActive(!string.IsNullOrEmpty(promptHeading));
            body.text = promptBody;
            body.gameObject.SetActive(!string.IsNullOrEmpty(promptBody));
            UIFactory.Clear(answers);
            optionButtons.Clear();
            optionLabels.Clear();

            // Side by side when the words are short enough, so the answers take little of the screen.
            int longest = 0;
            foreach (var c in choices) longest = Mathf.Max(longest, c.label != null ? c.label.Length : 0);
            int columns = choices.Length <= 1 ? 1
                : choices.Length == 3 && longest <= 12 ? 3
                : longest <= 22 ? 2 : 1;   // longer words get a whole row each, so they stay big
            bool lettered = choices.Length > 1;
            HorizontalLayoutGroup row = null;
            for (int i = 0; i < choices.Length; i++)
            {
                if (i % columns == 0) row = UIFactory.Row(answers, "Answers row", 30f, columns > 1 ? 168f : Theme.TouchTarget);
                var c = choices[i];
                optionLabels.Add(c.label);
                optionButtons.Add(Answer(row.transform, c, lettered ? Letters[i % Letters.Length] : null, columns >= 3));
            }
            ShowCard(!voice.Visible);
            if (game.Reader != null && Root.gameObject.activeInHierarchy)
            {
                var lines = new List<string> { promptHeading, promptBody };
                lines.AddRange(optionLabels);
                game.Reader.SpeakIfAuto(lines.ToArray());
            }
        }

        /// <summary>
        /// One answer: a big solid button in its colour, with its letter in a white circle when there is a choice.
        /// Three side by side (narrow) get a smaller letter, so their words keep their size.
        /// </summary>
        Button Answer(Transform row, PromptOption c, string letter, bool narrow)
        {
            var textColor = c.color == Theme.Card || c.color == Theme.Sun ? Theme.Ink : Color.white;
            // White answers get the sky colour, so every answer looks like a button over the room.
            var fill = c.color == Theme.Card ? Theme.Sky : c.color;
            var button = UIFactory.Button(row, c.label, fill, textColor, () =>
            {
                game.Audio.PlaySFX("tap");
                if (c.onClick != null) c.onClick();
            });
            if (letter == null) return button;

            float badge = narrow ? 54f : 88f;
            var text = button.GetComponentInChildren<Text>();
            var trt = (RectTransform)text.transform;
            trt.offsetMin = new Vector2(badge + (narrow ? 22f : 36f), trt.offsetMin.y);
            trt.offsetMax = new Vector2(narrow ? -12f : trt.offsetMax.x, trt.offsetMax.y);
            text.alignment = narrow ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            var circle = UIFactory.Panel(button.transform, "Letter", Color.white, false);
            circle.sprite = UIFactory.Circle;
            circle.raycastTarget = false;
            var crt = (RectTransform)circle.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.sizeDelta = new Vector2(badge, badge);
            crt.anchoredPosition = new Vector2(narrow ? 12f : 22f, 0f);
            var mark = UIFactory.Label(circle.transform, letter, narrow ? Theme.Label : Theme.Title, fill == Theme.Sky ? Theme.Primary : fill, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch((RectTransform)mark.transform);
            mark.raycastTarget = false;
            return button;
        }
    }
}
