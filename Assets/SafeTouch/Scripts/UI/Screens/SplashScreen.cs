using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>First screen: name, promise and language choice.</summary>
    public class SplashScreen : UIScreen
    {
        Text tagline;
        Button start, english, kinyarwanda;

        public override string Name { get { return "splash"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, null, null);
            UIFactory.Spacer(col, 1f);
            Decor.Friends(col, 520f);   // Keza and her dog, waving hello
            UIFactory.Label(col, "SafeTouch", Theme.Display + 24, Theme.Primary, TextAnchor.MiddleCenter, true);
            tagline = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.MiddleCenter, false);
            UIFactory.Spacer(col, 1f);
            start = UIFactory.Button(col, "Start", Theme.Primary, Color.white, () =>
            {
                game.ContinueFromSplash();   // the consent page first, until a parent has agreed
            });
            var langs = UIFactory.Row(col, "Languages", Theme.Gap, Theme.TouchTarget);
            english = UIFactory.Button(langs.transform, "English", Theme.Card, Theme.Ink, () => game.SetLanguage("en"));
            kinyarwanda = UIFactory.Button(langs.transform, "Ikinyarwanda", Theme.Card, Theme.Ink, () => game.SetLanguage("rw"));
        }

        public override void OnShow()
        {
            tagline.text = Localization.T("splash.tagline");
            UIFactory.SetButtonLabel(start, Localization.T("start"));
            english.image.color = Localization.Language == "en" ? Theme.Sun : Theme.Card;
            kinyarwanda.image.color = Localization.Language == "rw" ? Theme.Sun : Theme.Card;
        }
    }
}
