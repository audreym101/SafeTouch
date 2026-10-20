using System.Collections.Generic;
using SafeTouch.Core;
using SafeTouch.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The lesson map: four lessons and the Memory Quest in order, the Bubble Guard game, stars earned, and the
    /// secondary destinations (badges, Keza's look, grown-ups).
    /// </summary>
    public class HomeScreen : UIScreen
    {
        class Card
        {
            public LessonInfo info;
            public Button button;
            public Text title, summary;
        }

        readonly List<Card> cards = new List<Card>();
        Text heading, stars;
        Button badges, look, grownUps;

        public override string Name { get { return "home"; } }

        protected override void BuildContent(RectTransform root)
        {
            // "Pick a lesson" sits in the top bar beside the back button, leaving more room for the cards.
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            heading = TitleText;
            stars = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);

            // The four lessons and the Memory Quest, then the Bubble Guard game. They all fit on
            // 16:9 and taller phones; on smaller screens, or with longer Kinyarwanda text, the list scrolls.
            var list = UIFactory.ScrollList(col, 24f);
            var all = new List<LessonInfo>(LessonInfo.All) { LessonInfo.Game };
            foreach (var info in all)
            {
                var lesson = info;
                var card = new Card { info = info };
                card.button = UIFactory.Button(list, "", Theme.Card, Theme.Ink, () => game.StartLesson(lesson.level));
                UIFactory.Remove(card.button.GetComponentInChildren<Text>().gameObject);

                // The card is as tall as its words need, and at least 176 units.
                var size = card.button.GetComponent<LayoutElement>();
                size.minHeight = 176f;
                size.preferredHeight = -1f;
                var v = card.button.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(44, 24, 14, 14);
                v.spacing = 4f;
                v.childAlignment = TextAnchor.MiddleLeft;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;

                var stripe = UIFactory.Panel(card.button.transform, "Colour", info.color, true);
                stripe.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var srt = (RectTransform)stripe.transform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(0f, 1f);
                srt.offsetMin = new Vector2(0f, 0f);
                srt.offsetMax = new Vector2(20f, 0f);
                stripe.raycastTarget = false;

                card.title = UIFactory.Label(card.button.transform, "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
                card.summary = UIFactory.Label(card.button.transform, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);
                card.summary.supportRichText = true;   // the status follows the summary in the lesson's colour
                cards.Add(card);
            }

            var row = UIFactory.Row(col, "Secondary", Theme.Gap, Theme.TouchTarget);
            badges = UIFactory.Button(row.transform, "My badges", Theme.Sun, Theme.Ink, () => ui.ShowMenu("badges"));
            look = UIFactory.Button(row.transform, "Keza's look", Theme.Card, Theme.Ink, () => ui.ShowMenu("look"));
            grownUps = UIFactory.Button(row.transform, "Grown-ups", Theme.Card, Theme.Ink, () => ui.ShowMenu("parentGate"));
        }

        public override void OnShow()
        {
            var progress = game.Progress.GetData();
            heading.text = Localization.T("home.heading");
            stars.text = string.Format(Localization.T("home.stars"), progress.GetTotalScore(), progress.badgesEarned.Count);
            UIFactory.SetButtonLabel(badges, Localization.T("home.badges"));
            UIFactory.SetButtonLabel(look, Localization.T("home.look", "Keza's look"));
            UIFactory.SetButtonLabel(grownUps, Localization.T("home.grownups"));

            foreach (var card in cards)
            {
                int level = card.info.level;
                bool unlocked = game.Progress.IsUnlocked(level);
                card.button.interactable = unlocked;
                string status;
                if (card.info == LessonInfo.Game)
                {
                    card.title.text = Localization.T(card.info.titleKey, "Bubble Guard");
                    status = !unlocked
                        ? string.Format(Localization.T("status.locked"), ProgressManager.GameUnlockedBy)
                        : progress.guardGamesPlayed > 0
                            ? string.Format(Localization.T("game.best", "Best score: {0}"), progress.bestGuardScore)
                            : Localization.T("game.ready", "Ready to play");
                    SetSummary(card, Localization.T(card.info.summaryKey, "Game: say NO to keep Keza's bubble safe"), status);
                    continue;
                }
                bool done = progress.IsLevelComplete(level);
                card.title.text = level + ". " + Localization.T(card.info.titleKey);
                status = done ? Localization.T("status.done")
                    : unlocked ? Localization.T("status.ready")
                    : string.Format(Localization.T("status.locked"), level - 1);
                SetSummary(card, Localization.T(card.info.summaryKey), status);
            }
        }

        /// <summary>"Learn the three zones around your body  -  Ready to play", the status bold in the lesson's colour.</summary>
        static void SetSummary(Card card, string summary, string status)
        {
            string colour = ColorUtility.ToHtmlStringRGB(card.info.color);
            card.summary.text = summary + "  <color=#" + colour + "><b>" + status + "</b></color>";
        }
    }
}
