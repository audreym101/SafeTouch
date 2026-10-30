using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The menu after Start (and the login): big, bright buttons a child can find without reading much.
    /// Each has its own colour and a round picture-letter. Play, My badges, Keza's look, Settings, Help and
    /// About us are open to the child. Contact us and Grown-ups go through the grown-up check first,
    /// because they open the parent's area or another app (email, phone).
    /// </summary>
    public class MenuScreen : UIScreen
    {
        class Tile
        {
            public Button button;
            public Text label;
            public string en, rw;
        }

        Text hello;
        Tile play, badges, look, settings, help, about, contact, grownUps;

        public override string Name { get { return "menu"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            Decor.Gang(col, 230f);   // five friends hopping
            hello = UIFactory.Label(col, "", Theme.Title, Theme.Ink, TextAnchor.MiddleCenter, true);
            var list = UIFactory.ScrollList(col, Theme.Gap);

            // The big one: the lessons.
            play = MakeTile(list, "Play the lessons", "Kina amasomo", ">", Theme.Primary, 230f, () => ui.ShowMenu("home"));

            var row1 = Row(list);
            badges = MakeTile(row1, "My badges", "Imidari yanjye", "B", Theme.Sun, 0f, () => ui.ShowMenu("badges"));
            look = MakeTile(row1, "Keza's look", "Isura ya Keza", "K", Theme.Hex("#E86FA8"), 0f, () => ui.ShowMenu("look"));
            var row2 = Row(list);
            settings = MakeTile(row2, "Settings", "Igenamiterere", "S", Theme.Hex("#8E6FD8"), 0f, () => ui.ShowMenu("settings"));
            help = MakeTile(row2, "How to play", "Uko bikinwa", "?", Theme.Safe, 0f, () => ui.ShowMenu("help"));
            var row3 = Row(list);
            about = MakeTile(row3, "About us", "Abo turi bo", "i", Theme.Hex("#2BB3C0"), 0f, () => ui.ShowMenu("about"));
            contact = MakeTile(row3, "Contact us", "Twandikire", "C", Theme.Caution, 0f, () => ParentGateScreen.Open(ui, "contact", "menu"));
            grownUps = MakeTile(list, "Grown-ups", "Abantu bakuru", "G", Theme.Ink, 0f, () => ParentGateScreen.Open(ui, "parent", "menu"));
        }

        static Transform Row(RectTransform list)
        {
            var row = UIFactory.Row(list, "Tiles", Theme.Gap, 210f);
            return row.transform;
        }

        /// <summary>A coloured tile with a white circle holding a big symbol, and the name under it.</summary>
        Tile MakeTile(Transform parent, string en, string rw, string symbol, Color colour, float height, System.Action open)
        {
            var tile = new Tile { en = en, rw = rw };
            tile.button = UIFactory.Button(parent, "", colour, Color.white, open);
            var le = tile.button.GetComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height > 0f ? height : 210f;
            le.minWidth = le.preferredWidth = 0f;   // tiles side by side share the row equally, whatever their words
            var text = tile.button.GetComponentInChildren<Text>();
            UIFactory.Remove(text.gameObject);

            var h = tile.button.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(28, 20, 16, 16);
            h.spacing = 22f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            float circle = Mathf.Min(le.minHeight - 40f, 150f);
            var dot = UIFactory.Dot(tile.button.transform, "Icon", new Color(1f, 1f, 1f, 0.92f), circle);
            dot.raycastTarget = false;
            var mark = UIFactory.Label(dot.transform, symbol, (int)(circle * 0.6f), colour, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(mark.rectTransform);
            mark.GetComponent<LayoutElement>().ignoreLayout = true;

            tile.label = UIFactory.Label(tile.button.transform, en, Theme.Body, Color.white, TextAnchor.MiddleLeft, true);
            tile.label.GetComponent<LayoutElement>().flexibleWidth = 1f;
            tile.label.resizeTextForBestFit = true;   // long Kinyarwanda words shrink a little instead of spilling out
            tile.label.resizeTextMinSize = Theme.Small;
            tile.label.resizeTextMaxSize = height > 0f ? Theme.Title : Theme.Body;
            return tile;
        }

        public override void OnShow()
        {
            TitleText.text = L("Menu", "Ibikubiyemo");
            hello.text = L("Hello, friend! What shall we do?", "Muraho, nshuti! Dukore iki?");
            foreach (var t in new[] { play, badges, look, settings, help, about, contact, grownUps })
                t.label.text = L(t.en, t.rw);
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
