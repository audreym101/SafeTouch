using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>How to play, in five short steps with big numbers, for a child (or a grown-up reading with them).</summary>
    public class HelpScreen : UIScreen
    {
        static readonly string[][] Steps =
        {
            new[] { "Tap \"Play the lessons\" and choose a lesson.", "Kanda \"Kina amasomo\" maze uhitemo isomo." },
            new[] { "Keza appears straight away. Tap + or - to make her bigger or smaller.", "Keza ahita agaragara. Kanda + cyangwa - ngo abe munini cyangwa muto." },
            new[] { "Listen to Keza, then tap the answer you think is right.", "Umva Keza, hanyuma ukande igisubizo utekereza ko ari cyo." },
            new[] { "When Keza asks, say \"NO!\" clearly, like you mean it.", "Keza nabikubaza, vuga \"OYA!\" neza, nk'ubivuga ukomeje." },
            new[] { "Win stars and badges. Try every lesson!", "Tsindira inyenyeri n'imidari. Gerageza amasomo yose!" },
        };

        static readonly Color[] Colours = { Theme.Primary, Theme.Hex("#E86FA8"), Theme.Safe, Theme.Caution, Theme.Sun };

        Text intro, tip;
        readonly Text[] lines = new Text[Steps.Length];

        public override string Name { get { return "help"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("menu"));
            var list = UIFactory.ScrollList(col, 24f);
            intro = UIFactory.Label(list, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            for (int i = 0; i < Steps.Length; i++)
            {
                var card = UIFactory.Panel(list, "Step", Theme.Card, true);
                var h = card.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.padding = new RectOffset(24, 24, 20, 20);
                h.spacing = 24f;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                var dot = UIFactory.Dot(card.transform, "Number", Colours[i], 110f);
                var number = UIFactory.Label(dot.transform, (i + 1).ToString(), 64, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(number.rectTransform);
                number.GetComponent<LayoutElement>().ignoreLayout = true;
                lines[i] = UIFactory.Label(card.transform, "", Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, false);
                lines[i].GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
            tip = UIFactory.Label(list, "", Theme.Body, Theme.Primary, TextAnchor.UpperLeft, true);
            Decor.Friends(list, 300f);
        }

        public override void OnShow()
        {
            TitleText.text = L("How to play", "Uko bikinwa");
            intro.text = L("Learning with Keza is easy:", "Kwiga hamwe na Keza biroroshye:");
            for (int i = 0; i < Steps.Length; i++) lines[i].text = L(Steps[i][0], Steps[i][1]);
            tip.text = L("If something ever worries you, tell a grown-up you trust.", "Niba hari ikintu kiguhangayikishije, bwira umuntu mukuru wizera.");
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
