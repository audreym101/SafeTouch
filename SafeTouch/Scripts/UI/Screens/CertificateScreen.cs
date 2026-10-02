using System;
using System.Collections.Generic;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The end of the Memory Quest: an "I remember!" certificate listing the
    /// four big ideas in the child's own choices (their zones, STOP, their
    /// helpers), to show a parent or teacher.
    /// </summary>
    public class CertificateScreen : UIScreen
    {
        Text heading, sub, stars;
        RectTransform list;
        Button done;
        Action onDone;

        public override string Name { get { return "certificate"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, null, null);
            heading = UIFactory.Label(col, "", Theme.Display, Theme.Primary, TextAnchor.MiddleCenter, true);
            sub = UIFactory.Label(col, "", Theme.Body, Theme.Muted, TextAnchor.MiddleCenter, false);
            list = UIFactory.Rect(col, "Ideas");
            UIFactory.Column(list, Theme.Gap, 0);
            list.gameObject.AddComponent<LayoutElement>();
            stars = UIFactory.Label(col, "", Theme.Title, Theme.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Spacer(col, 1f);
            done = UIFactory.Button(col, "", Theme.Sun, Theme.Ink, () => { if (onDone != null) onDone(); });
        }

        /// <summary>Shows the certificate. Each idea is (colour, text).</summary>
        public void Show(List<KeyValuePair<Color, string>> ideas, int score, int max, Action collect)
        {
            onDone = collect;
            heading.text = Localization.T("review.cert.title", "I remember!");
            sub.text = Localization.T("review.cert.sub", "Keza and I finished the SafeTouch Memory Quest. Show a grown-up what you learned.");
            stars.text = string.Format(Localization.T("review.cert.stars", "{0} of {1} stars"), score, max);
            UIFactory.SetButtonLabel(done, Localization.T("review.cert.collect", "Collect my badge"));
            UIFactory.Clear(list);
            foreach (var idea in ideas)
            {
                var card = UIFactory.Panel(list, "Idea", Theme.Card, true);
                var rt = (RectTransform)card.transform;
                var row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(36, 36, 28, 28);
                row.spacing = 28f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = true;
                row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
                card.gameObject.AddComponent<LayoutElement>().minHeight = 150f;
                var dot = UIFactory.Panel(rt, "Dot", idea.Key, true);
                var dle = dot.gameObject.AddComponent<LayoutElement>();
                dle.minWidth = dle.preferredWidth = 56f;
                dle.minHeight = dle.preferredHeight = 56f;
                var text = UIFactory.Label(rt, idea.Value, Theme.Label, Theme.Ink, TextAnchor.MiddleLeft, false);
                text.GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }
    }
}
