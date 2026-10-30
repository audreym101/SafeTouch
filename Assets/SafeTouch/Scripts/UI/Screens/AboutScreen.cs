using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>About SafeTouch: what it is for, who it is for, how it keeps data, and the app version.</summary>
    public class AboutScreen : UIScreen
    {
        Text heading, what, who, privacy, version;
        Button contact;

        public override string Name { get { return "about"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("menu"));
            var list = UIFactory.ScrollList(col, Theme.Gap);
            Decor.Friends(list, 300f);
            heading = UIFactory.Label(list, "", Theme.Title, Theme.Primary, TextAnchor.MiddleCenter, true);
            what = Card(list);
            who = Card(list);
            privacy = Card(list);
            contact = UIFactory.Button(list, "", Theme.Caution, Color.white, () => ParentGateScreen.Open(ui, "contact", "about"));
            version = UIFactory.Label(list, "", Theme.Small, Theme.Muted, TextAnchor.MiddleCenter, false);
        }

        static Text Card(RectTransform list)
        {
            var card = UIFactory.Panel(list, "Card", Theme.Card, true);
            UIFactory.Column((RectTransform)card.transform, 0f, 36);
            return UIFactory.Label(card.transform, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
        }

        public override void OnShow()
        {
            TitleText.text = L("About us", "Abo turi bo");
            heading.text = "SafeTouch";
            what.text = L("SafeTouch helps children aged 6 to 11 learn to keep their body safe: personal space, safe and unsafe touch, saying \"No\", and who to tell. Keza, our guide, appears in your room with the phone's camera.",
                          "SafeTouch ifasha abana bafite imyaka 6 kugeza 11 kwiga kurinda umubiri wabo: umwanya bwite, gukorwaho bikwiye n'ibidakwiye, kuvuga \"Oya\", n'uwo babwira. Keza, umuyobozi wacu, agaragara mu cyumba cyawe binyuze kuri kamera ya telefoni.");
            who.text = L("SafeTouch was made in Rwanda, in English and Kinyarwanda, to be used by a child with a parent or teacher nearby.",
                         "SafeTouch yakorewe mu Rwanda, mu Cyongereza no mu Kinyarwanda, kugira ngo ikoreshwe n'umwana ari kumwe n'umubyeyi cyangwa umwarimu.");
            privacy.text = L("We keep only what a parent agreed to: the parent's details to confirm consent, and, only if the parent switches it on, the child's stars and badges saved online. A parent can delete it all at any time in Grown-ups > Settings.",
                             "Tubika gusa ibyo umubyeyi yemeye: amakuru y'umubyeyi yo kwemeza uruhushya, n'inyenyeri n'imidari by'umwana bibitswe kuri interineti, gusa iyo umubyeyi abyemeye. Umubyeyi ashobora kubisiba byose igihe icyo ari cyo cyose muri Abantu bakuru > Igenamiterere.");
            UIFactory.SetButtonLabel(contact, L("Contact us", "Twandikire"));
            version.text = L("Version ", "Verisiyo ") + Application.version;
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
