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
            public Text title, summary, status;
        }

        readonly List<Card> cards = new List<Card>();
        Text heading, stars;
        Button badges, look, grownUps;

        public override string Name { get { return "home"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            heading = UIFactory.Label(col, "", Theme.Display, Theme.Ink, TextAnchor.MiddleLeft, true);
            stars = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);

            // The four lessons and the Memory Quest, then the Bubble Guard game.
            var all = new List<LessonInfo>(LessonInfo.All) { LessonInfo.Game };
            foreach (var info in all)
            {
                var lesson = info;
                var card = new Card { info = info };
                card.button = UIFactory.Button(col, "", Theme.Card, Theme.Ink, () => game.StartLesson(lesson.level));
                UIFactory.FixedHeight(card.button, 168f);   // six cards fit a 1920 px tall phone
                Object.Destroy(card.button.GetComponentInChildren<Text>().gameObject);

                var inner = UIFactory.Rect(card.button.transform, "Inner");
                UIFactory.Stretch(inner);
                var v = UIFactory.Column(inner, 6f, 40);
                v.childAlignment = TextAnchor.MiddleLeft;

                var stripe = UIFactory.Panel(card.button.transform, "Colour", info.color, true);
                var srt = (RectTransform)stripe.transform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(0f, 1f);
                srt.offsetMin = new Vector2(0f, 0f);
                srt.offsetMax = new Vector2(20f, 0f);
                stripe.raycastTarget = false;

                card.title = UIFactory.Label(inner, "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
                card.summary = UIFactory.Label(inner, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);
                card.status = UIFactory.Label(inner, "", Theme.Small, info.color, TextAnchor.MiddleLeft, true);
                cards.Add(card);
            }

            UIFactory.Spacer(col, 1f);
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
                if (card.info == LessonInfo.Game)
                {
                    card.title.text = Localization.T(card.info.titleKey, "Bubble Guard");
                    card.summary.text = Localization.T(card.info.summaryKey, "Game: say NO to keep Keza's bubble safe");
                    card.status.text = !unlocked
                        ? string.Format(Localization.T("status.locked"), ProgressManager.GameUnlockedBy)
                        : progress.guardGamesPlayed > 0
                            ? string.Format(Localization.T("game.best", "Best score: {0}"), progress.bestGuardScore)
                            : Localization.T("game.ready", "Ready to play");
                    continue;
                }
                bool done = progress.IsLevelComplete(level);
                card.title.text = level + ". " + Localization.T(card.info.titleKey);
                card.summary.text = Localization.T(card.info.summaryKey);
                card.status.text = done ? Localization.T("status.done")
                    : unlocked ? Localization.T("status.ready")
                    : string.Format(Localization.T("status.locked"), level - 1);
            }
        }
    }
}
