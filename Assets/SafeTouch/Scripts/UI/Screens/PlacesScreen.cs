using System;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Memory Quest, round 4: the child picks where Keza is playing (home,
    /// school or playground). The choice sets which helpers stand in the
    /// room and who comes too close in the chase.
    /// </summary>
    public class PlacesScreen : UIScreen
    {
        static readonly string[] Ids = { "home", "school", "playground" };
        static readonly string[] Names = { "Home", "School", "Playground" };
        static readonly string[] Helpers = { "Mum or Dad, Grandparent", "Teacher, Head teacher", "IZU volunteer, Friend's mum" };

        Text heading, intro, footer;
        readonly Button[] buttons = new Button[3];
        Action<string> picked;

        public override string Name { get { return "places"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            heading = UIFactory.Label(col, "", Theme.Display, Theme.Ink, TextAnchor.MiddleLeft, true);
            intro = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            Color[] colors = { Theme.Hex("#F28C38"), Theme.Primary, Theme.Safe };
            for (int i = 0; i < Ids.Length; i++)
            {
                string id = Ids[i];
                buttons[i] = UIFactory.Button(col, "", Theme.Card, Theme.Ink, () => { if (picked != null) picked(id); });
                UIFactory.FixedHeight(buttons[i], 220f);
                var stripe = UIFactory.Panel(buttons[i].transform, "Colour", colors[i], true);
                var srt = (RectTransform)stripe.transform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(0f, 1f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = new Vector2(20f, 0f);
                stripe.raycastTarget = false;
            }
            UIFactory.Spacer(col, 1f);
            footer = UIFactory.Label(col, "", Theme.Small, Theme.Muted, TextAnchor.MiddleCenter, false);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("review.round4", "Round 4 of 4: safe places chase");
            heading.text = Localization.T("places.title", "Where is Keza playing?");
            intro.text = Localization.T("places.intro", "Pick a place. Someone will come too close three times. Say NO, tap them, or drag Keza to a helper as fast as you can.");
            footer.text = Localization.T("places.footer", "Faster is better: 3 seconds or less earns 3 stars.");
            for (int i = 0; i < Ids.Length; i++)
                UIFactory.SetButtonLabel(buttons[i], Localization.T("places." + Ids[i], Names[i]) + "\n"
                    + Localization.T("places.helpers", "Helpers here:") + " " + Localization.T("places." + Ids[i] + ".helpers", Helpers[i]));
        }

        /// <summary>Calls back with "home", "school" or "playground".</summary>
        public void Ask(Action<string> onPicked)
        {
            picked = onPicked;
        }
    }
}
