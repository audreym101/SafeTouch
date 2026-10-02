using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>Age band only. SafeTouch never asks for a name (NFR1).</summary>
    public class AgeScreen : UIScreen
    {
        Text question, note;

        public override string Name { get { return "age"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            question = UIFactory.Label(col, "", Theme.Display, Theme.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Spacer(col, 0.5f);
            var young = UIFactory.Button(col, "6 - 8", Theme.Primary, Color.white, () => game.SelectAgeGroup("6-8"));
            UIFactory.FixedHeight(young, Theme.TouchTarget * 2f);
            var older = UIFactory.Button(col, "9 - 11", Theme.Safe, Color.white, () => game.SelectAgeGroup("9-11"));
            UIFactory.FixedHeight(older, Theme.TouchTarget * 2f);
            UIFactory.Spacer(col, 1f);
            note = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.MiddleCenter, false);
        }

        public override void OnShow()
        {
            question.text = Localization.T("age.question");
            note.text = Localization.T("age.note");
        }
    }
}
