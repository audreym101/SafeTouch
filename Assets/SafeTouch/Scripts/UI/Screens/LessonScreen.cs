using System.Collections.Generic;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The AR lesson HUD: exit, lesson title and step at the top; the move
    /// buttons and the prompt card with up to four choices at the bottom,
    /// within thumb reach.
    /// </summary>
    public class LessonScreen : UIScreen, ITickable
    {
        Text title, step, heading, body, zoneReadout;
        RectTransform options;
        Button replay, movePlace, who;
        readonly List<Button> optionButtons = new List<Button>();
        GameObject promptCard;
        readonly VoicePanel voice = new VoicePanel();

        public System.Action ReplayNarration;

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
            replay = UIFactory.Button(tools.transform, "Listen again", Theme.WithAlpha(Theme.Card, 0.9f), Theme.Ink, () =>
            {
                if (ReplayNarration != null) ReplayNarration();
            });
            movePlace = UIFactory.Button(tools.transform, "Move Keza", Theme.WithAlpha(Theme.Card, 0.9f), Theme.Ink, () => game.MoveCharacter());
            UIFactory.FixedHeight(replay, 110f);
            UIFactory.FixedHeight(movePlace, 110f);
            replay.GetComponentInChildren<Text>().fontSize = Theme.Label;
            movePlace.GetComponentInChildren<Text>().fontSize = Theme.Label;

            UIFactory.Spacer(col, 1f);
            BuildMovePad(col);

            var card = UIFactory.Panel(col, "Prompt card", Theme.WithAlpha(Theme.Card, 0.96f), true);
            var cardRt = (RectTransform)card.transform;
            UIFactory.Column(cardRt, 20f, 44);
            card.gameObject.AddComponent<LayoutElement>();
            heading = UIFactory.Label(cardRt, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(cardRt, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            options = UIFactory.Rect(cardRt, "Options");
            UIFactory.Column(options, 20f, 0);
            promptCard = card.gameObject;

            // The "Say NO!" card takes the prompt card's place while it listens.
            voice.Build(col);
        }

        public void Tick()
        {
            voice.Tick();
        }

        /// <summary>
        /// Swaps the prompt card for the voice card and listens for the child's
        /// NO. done gets the best try, or null if the child chose to tap.
        /// </summary>
        public void ShowVoice(string promptHeading, string promptBody, SafeTouch.Voice.VoiceCoach coach, System.Action<SafeTouch.Voice.NoRating> done)
        {
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
            RefreshMovePad();
        }

        /// <summary>
        /// The move buttons: "Who" switches between Keza and the other people
        /// in the room, and the arrows walk that character one step. A line
        /// above says which of Keza's zones the character is standing in, so
        /// the child can test the bubble for themselves.
        /// </summary>
        void BuildMovePad(RectTransform col)
        {
            zoneReadout = UIFactory.Label(col, "", Theme.Label, Color.white, TextAnchor.MiddleCenter, true);
            var row = UIFactory.Row(col, "Move row", 20f, 130f);
            who = UIFactory.Button(row.transform, "Keza", Theme.Sun, Theme.Ink, () =>
            {
                game.AR.SelectNext();
                RefreshMovePad();
            });
            who.GetComponent<LayoutElement>().flexibleWidth = 2f;
            Arrow(row.transform, "\u2190", new Vector2(-1f, 0f));   // left
            Arrow(row.transform, "\u2191", new Vector2(0f, 1f));    // away from the phone
            Arrow(row.transform, "\u2193", new Vector2(0f, -1f));   // towards the phone
            Arrow(row.transform, "\u2192", new Vector2(1f, 0f));    // right
        }

        void Arrow(Transform parent, string glyph, Vector2 direction)
        {
            var b = UIFactory.Button(parent, glyph, Theme.WithAlpha(Theme.Card, 0.92f), Theme.Ink, () =>
            {
                game.AR.Nudge(direction);
                game.UI.StartCoroutine(RefreshAfterStep());
            });
            b.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        System.Collections.IEnumerator RefreshAfterStep()
        {
            yield return new WaitForSeconds(0.5f);   // one step takes about 0.45 s at walking speed
            RefreshMovePad();
        }

        /// <summary>Updates the "who" button and the zone line after a move or a new step.</summary>
        public void RefreshMovePad()
        {
            if (who == null || game == null || game.AR == null) return;
            var selected = game.AR.Selected;
            string name = selected == null || selected == game.AR.Character
                ? Localization.T("keza", "Keza") : selected.Label;
            UIFactory.SetButtonLabel(who, Localization.T("move.who", "Move:") + " " + name);
            bool kezaSelected = selected == null || selected == game.AR.Character;
            // The tip only shows in the bubble lesson; elsewhere it would cover the room.
            zoneReadout.gameObject.SetActive(!kezaSelected || game.CurrentLevel == 1);
            if (kezaSelected)
            {
                zoneReadout.text = Localization.T("move.keza", "Use the arrows to walk Keza. Her bubble moves with her.");
                return;
            }
            string zone = game.AR.ZoneOf(selected);
            zoneReadout.text = string.Format(Localization.T("move.zone." + zone, ZoneEnglish(zone)), selected.Label);
        }

        static string ZoneEnglish(string zone)
        {
            switch (zone)
            {
                case "red": return "{0} is in Keza's red zone. Only with Keza's OK!";
                case "yellow": return "{0} is in Keza's yellow zone, for people she trusts.";
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
            foreach (var b in optionButtons) UIFactory.Remove(b.gameObject);
            optionButtons.Clear();
            RefreshMovePad();
            foreach (var choice in choices)
            {
                var c = choice;
                var textColor = c.color == Theme.Card || c.color == Theme.Sun ? Theme.Ink : Color.white;
                var button = UIFactory.Button(options, c.label, c.color, textColor, () =>
                {
                    game.Audio.PlaySFX("tap");
                    if (c.onClick != null) c.onClick();
                });
                optionButtons.Add(button);
            }
        }
    }
}
