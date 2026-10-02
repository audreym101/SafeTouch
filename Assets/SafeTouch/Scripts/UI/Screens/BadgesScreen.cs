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
            var col = StandardColumn(root, "", () => ui.ShowMenu("home"));
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
                int score = progress.quizScores.Count >= info.level ? progress.quizScores[info.level - 1] : 0;
                string state = progress.IsLevelComplete(info.level)
                    ? string.Format(Localization.T("badges.score"), score)
                    : Localization.T("badges.notyet");
                UIFactory.Label(list, Localization.T(info.titleKey) + "  -  " + state, Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, false);
            }

            if (progress.bestChaseSeconds > 0f)
                UIFactory.Label(list, string.Format(Localization.T("badges.chase", "Fastest NO in the chase: {0:0.0} seconds"), progress.bestChaseSeconds),
                    Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);

            UIFactory.Label(list, Localization.T("home.badges"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
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
