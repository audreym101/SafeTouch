using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>Settings a child can change: music, sound effects and the language. The choices stay on the phone.</summary>
    public class SettingsScreen : UIScreen
    {
        Text musicTitle, soundsTitle, lettersTitle, languageTitle;
        Button music, sounds, english, kinyarwanda;
        readonly Button[] letterButtons = new Button[TextSize.Scales.Length];

        public override string Name { get { return "settings"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("menu"));
            musicTitle = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            music = UIFactory.Button(col, "", Theme.Safe, Color.white, () => { game.Audio.MusicOn = !game.Audio.MusicOn; OnShow(); });
            soundsTitle = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            sounds = UIFactory.Button(col, "", Theme.Safe, Color.white, () => { game.Audio.SoundsOn = !game.Audio.SoundsOn; OnShow(); });
            // Letter size: three buttons with a small, a medium and a big "A".
            lettersTitle = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            var sizes = UIFactory.Row(col, "Letter sizes", Theme.Gap, Theme.TouchTarget);
            for (int i = 0; i < letterButtons.Length; i++)
            {
                int step = i;
                letterButtons[i] = UIFactory.Button(sizes.transform, "A", Theme.Card, Theme.Ink, () => { TextSize.Step = step; OnShow(); });
                letterButtons[i].GetComponentInChildren<Text>().fontSize = 40 + i * 16;
            }
            languageTitle = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            var langs = UIFactory.Row(col, "Languages", Theme.Gap, Theme.TouchTarget);
            english = UIFactory.Button(langs.transform, "English", Theme.Card, Theme.Ink, () => game.SetLanguage("en"));
            kinyarwanda = UIFactory.Button(langs.transform, "Ikinyarwanda", Theme.Card, Theme.Ink, () => game.SetLanguage("rw"));
            UIFactory.Spacer(col, 1f);
            Decor.Friends(col, 300f);
        }

        public override void OnShow()
        {
            TitleText.text = L("Settings", "Igenamiterere");
            musicTitle.text = L("Music", "Umuziki");
            soundsTitle.text = L("Sounds when you tap", "Amajwi iyo ukanze");
            lettersTitle.text = L("Letter size", "Ingano y'inyuguti");
            for (int i = 0; i < letterButtons.Length; i++)
            {
                bool chosen = i == TextSize.Step;
                letterButtons[i].image.color = chosen ? Theme.Sun : Theme.Card;
            }
            languageTitle.text = L("Language", "Ururimi");
            Switch(music, game.Audio.MusicOn);
            Switch(sounds, game.Audio.SoundsOn);
            english.image.color = Localization.Language == "en" ? Theme.Sun : Theme.Card;
            kinyarwanda.image.color = Localization.Language == "rw" ? Theme.Sun : Theme.Card;
        }

        /// <summary>An on/off button: green and "On", or white and "Off".</summary>
        static void Switch(Button button, bool on)
        {
            button.image.color = on ? Theme.Safe : Theme.Card;
            button.GetComponentInChildren<Text>().color = on ? Color.white : Theme.Ink;
            UIFactory.SetButtonLabel(button, on ? L("On  (tap to turn off)", "Birafunguye  (kanda ubifunge)") : L("Off  (tap to turn on)", "Birafunze  (kanda ubifungure)"));
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
