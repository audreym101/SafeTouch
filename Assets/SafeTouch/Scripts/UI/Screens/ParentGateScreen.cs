using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// A simple grown-up check before the parent section: a sum written in
    /// words that most 6-8 year olds cannot read yet. No PIN or account, so
    /// nothing personal is stored.
    /// </summary>
    public class ParentGateScreen : UIScreen
    {
        static readonly string[] Words =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
        };

        Text question;
        readonly Button[] answers = new Button[3];
        int correct;

        public override string Name { get { return "parentGate"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("home"));
            question = UIFactory.Label(col, "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            for (int i = 0; i < answers.Length; i++)
            {
                int index = i;
                answers[i] = UIFactory.Button(col, "", Theme.Card, Theme.Ink, () => Answer(index));
            }
            UIFactory.Spacer(col, 1f);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("gate.title");
            int a = Random.Range(11, 20), b = Random.Range(3, 10);
            question.text = string.Format(Localization.T("gate.question"), Words[a], Words[b]);
            correct = Random.Range(0, answers.Length);
            int[] offsets = { -2, 3, 1 };
            for (int i = 0; i < answers.Length; i++)
            {
                int value = i == correct ? a + b : a + b + offsets[i];
                UIFactory.SetButtonLabel(answers[i], value.ToString());
            }
        }

        void Answer(int index)
        {
            ui.ShowMenu(index == correct ? "parent" : "home");
        }
    }
}
