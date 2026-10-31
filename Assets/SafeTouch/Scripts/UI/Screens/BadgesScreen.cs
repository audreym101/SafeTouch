using SafeTouch.Core;
using UnityEngine;

namespace SafeTouch.UI.Screens
{
    /// <summary>Progress per lesson and the badge shelf.</summary>
    public class BadgesScreen : UIScreen
    {
        RectTransform list;

        public override string Name { get { return "badges"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu(ui.CameFrom("menu", "home")));
            list = UIFactory.ScrollList(col, Theme.Gap);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("home.badges");
            UIFactory.Clear(list);
            var progress = game.Progress.GetData();

            UIFactory.Label(list, Localization.T("badges.lessons"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            foreach (var info in LessonInfo.All)
            {
                int score = progress.BestScore(info.level), max = progress.MaxScore(info.level);
                string state = !progress.IsLevelComplete(info.level) ? Localization.T("badges.notyet")
                    : max > 0 ? string.Format(Localization.T("badges.score.of", "{0} of {1} stars"), score, max)
                    : string.Format(Localization.T("badges.score"), score);
                UIFactory.Label(list, Localization.T(info.titleKey) + "  -  " + state, Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, false);
            }

            if (progress.bestChaseSeconds > 0f)
                UIFactory.Label(list, string.Format(Localization.T("badges.chase", "Fastest NO in the chase: {0:0.0} seconds"), progress.bestChaseSeconds),
                    Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);

            UIFactory.Label(list, Localization.T("home.badges"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            // Why the lessons and the game stay open: say what is left to win.
            int left = game.Progress.BadgesToWin().Count;
            UIFactory.Label(list, left == 0
                    ? Localization.T("badges.all", "You won every badge! Well done.")
                    : string.Format(Localization.T("badges.left", "{0} badges left to win. Play the lessons and games again to win them!"), left),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            foreach (var badge in game.Progress.Badges)
            {
                var card = UIFactory.Panel(list, badge.badgeId, badge.unlocked ? Theme.WithAlpha(Theme.Sun, 0.35f) : Theme.Card, true);
                UIFactory.Column((RectTransform)card.transform, 6f, 36);
                UIFactory.Label(card.transform, badge.unlocked ? badge.title : badge.title + " (" + Localization.T("badges.locked") + ")",
                    Theme.Body, badge.unlocked ? Theme.Ink : Theme.Muted, TextAnchor.MiddleLeft, true);
                UIFactory.Label(card.transform, badge.description, Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);
            }
        }
    }
}
