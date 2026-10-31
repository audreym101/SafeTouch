using System.Collections.Generic;
using SafeTouch.AR;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The AR lesson HUD, kept small so the room fills the screen: exit,
    /// lesson title and step at the top; a floating hint that says what
    /// touching the room does; and the prompt card at the bottom, within
    /// thumb reach, with a stripe in the lesson's colour. People are moved
    /// by dragging them in the room (ARTouch), not with buttons; while one is
    /// dragged, the hint says which of the guide's zones they are in.
    /// </summary>
    public class LessonScreen : UIScreen, ITickable
    {
        Text title, step, heading, body, hint;
        Image stripe, hintPill;
        RectTransform options;
        Button replay, movePlace;
        readonly List<Button> optionButtons = new List<Button>();
        readonly List<string> optionLabels = new List<string>();
        GameObject promptCard;
        readonly VoicePanel voice = new VoicePanel();
        string touchHint = "";
        float showZoneUntil;

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
            shadeRt.sizeDelta = new Vector2(0f, 480f);
            shade.raycastTarget = false;

            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            title = TitleText;
            title.color = Color.white;

            // The step gets its own line, so the two buttons below always have room for
            // their words, even in Kinyarwanda and on the narrowest phones.
            step = UIFactory.Label(col, "", Theme.Label, Color.white, TextAnchor.MiddleLeft, true);
            var tools = UIFactory.Row(col, "Tools", Theme.Gap, 110f);
            replay = UIFactory.Button(tools.transform, "Read to me", Theme.WithAlpha(Theme.Card, 0.9f), Theme.Ink, ReadCard);
            movePlace = UIFactory.Button(tools.transform, "Place again", Theme.WithAlpha(Theme.Card, 0.9f), Theme.Ink, () => game.MoveCharacter());
            UIFactory.FixedHeight(replay, 110f);
            UIFactory.FixedHeight(movePlace, 110f);
            replay.GetComponentInChildren<Text>().fontSize = Theme.Label;
            movePlace.GetComponentInChildren<Text>().fontSize = Theme.Label;

            // The room shows through here.
            UIFactory.Spacer(col, 1f);
            BuildHintPill(col);
            BuildMovePad(col);

            var card = UIFactory.Panel(col, "Prompt card", Theme.WithAlpha(Theme.Card, 0.97f), true);
            var cardRt = (RectTransform)card.transform;
            var cardLayout = UIFactory.Column(cardRt, 20f, 44);
            cardLayout.padding.top = 56;   // room for the colour stripe
            card.gameObject.AddComponent<LayoutElement>();
            stripe = UIFactory.Panel(cardRt, "Lesson colour", Theme.Primary, true);
            stripe.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var srt = (RectTransform)stripe.transform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(44f, -26f);
            srt.offsetMax = new Vector2(-44f, -14f);
            stripe.raycastTarget = false;
            heading = UIFactory.Label(cardRt, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(cardRt, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            options = UIFactory.Rect(cardRt, "Options");
            UIFactory.Column(options, 20f, 0);
            promptCard = card.gameObject;

            // The "Say NO!" card takes the prompt card's place while it listens.
            voice.Build(col);

            // (The screenshot tool builds the screens without starting AR: no touch there.)
            if (game.AR.Touch != null)
            {
                game.AR.Touch.DragMoved += ShowZone;
                game.AR.Touch.DragEnded += f => showZoneUntil = Time.time + 2f;
            }
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
        /// Four round arrow buttons that walk the guide a step at a time, for
        /// when the guide has to get somewhere (a colour, a letter, the safe
        /// place) and dragging is hard. Hidden unless a lesson asks for it.
        /// </summary>
        void BuildMovePad(RectTransform col)
        {
            var row = UIFactory.Row(col, "Move pad", 28f, 128f);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleCenter;
            Arrow(row.transform, "←", new Vector2(-1f, 0f));   // left
            Arrow(row.transform, "↑", new Vector2(0f, 1f));    // away from the phone
            Arrow(row.transform, "↓", new Vector2(0f, -1f));   // towards the phone
            Arrow(row.transform, "→", new Vector2(1f, 0f));    // right
            movePad = row.gameObject;
            movePad.SetActive(false);
        }

        void Arrow(Transform parent, string glyph, Vector2 direction)
        {
            var b = UIFactory.Button(parent, glyph, Theme.WithAlpha(Theme.Card, 0.95f), Theme.Primary, () => game.AR.Step(direction));
            b.image.sprite = UIFactory.Circle;
            b.image.type = Image.Type.Simple;
            var le = b.GetComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.minWidth = le.preferredWidth = 128f;
            le.minHeight = le.preferredHeight = 128f;
            b.GetComponentInChildren<Text>().fontSize = Theme.Title;
        }

        /// <summary>Shows or hides the arrow buttons that walk the guide.</summary>
        public void ShowMovePad(bool show)
        {
            if (movePad != null) movePad.SetActive(show);
        }

        public void Tick()
        {
            voice.Tick();
            if (showZoneUntil > 0f && Time.time > showZoneUntil)
            {
                showZoneUntil = 0f;
                SetHint(touchHint);
            }
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
        /// Swaps the prompt card for the voice card and listens for the child's
        /// NO. done gets the best try, or null if the child chose to tap.
        /// </summary>
        public void ShowVoice(string promptHeading, string promptBody, SafeTouch.Voice.VoiceCoach coach, System.Action<SafeTouch.Voice.NoRating> done)
        {
            if (game.Reader != null) game.Reader.SpeakIfAuto(promptHeading, promptBody);
            promptCard.SetActive(false);
            voice.Show(promptHeading, promptBody, coach, rating =>
            {
                promptCard.SetActive(true);
                if (done != null) done(rating);
            });
        }

        /// <summary>Closes the voice card (and the microphone) if it is open.</summary>
        public void HideVoice()
        {
            voice.Hide();
            promptCard.SetActive(true);
        }

        public override void OnShow()
        {
            UIFactory.SetButtonLabel(replay, Localization.T("lesson.replay"));
            UIFactory.SetButtonLabel(movePlace, Localization.T("lesson.place", "Place again"));
            stripe.color = LessonInfo.For(Mathf.Max(1, game.CurrentLevel)).color;
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

        /// <summary>Shows a question or instruction with up to four choice buttons.</summary>
        public void ShowPrompt(string promptHeading, string promptBody, params PromptOption[] choices)
        {
            if (voice.Visible) HideVoice();
            heading.text = promptHeading;
            heading.gameObject.SetActive(!string.IsNullOrEmpty(promptHeading));
            body.text = promptBody;
            body.gameObject.SetActive(!string.IsNullOrEmpty(promptBody));
            foreach (var b in optionButtons) UIFactory.Remove(b.gameObject);
            optionButtons.Clear();
            optionLabels.Clear();
            foreach (var choice in choices)
            {
                var c = choice;
                optionLabels.Add(c.label);
                var textColor = c.color == Theme.Card || c.color == Theme.Sun ? Theme.Ink : Color.white;
                // White answers on the white card get the sky colour, so every answer looks like a button.
                var fill = c.color == Theme.Card ? Theme.Sky : c.color;
                var button = UIFactory.Button(options, c.label, fill, textColor, () =>
                {
                    game.Audio.PlaySFX("tap");
                    if (c.onClick != null) c.onClick();
                });
                optionButtons.Add(button);
            }
            options.gameObject.SetActive(choices.Length > 0);
            if (game.Reader != null && Root.gameObject.activeInHierarchy)
            {
                var lines = new List<string> { promptHeading, promptBody };
                lines.AddRange(optionLabels);
                game.Reader.SpeakIfAuto(lines.ToArray());
            }
        }
    }
}
