using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Contact us, for parents (reached through the grown-up check, because the buttons open other apps):
    /// email the SafeTouch team, open the parent area, or call a free Rwandan helpline about a child's safety.
    /// </summary>
    public class ContactScreen : UIScreen
    {
        /// <summary>Where parents write to: the same address the consent emails come from. Change it here.</summary>
        public const string TeamEmail = "m.audrey1@alustudent.com";

        // Free helplines in Rwanda, for a child who may be in danger.
        const string ChildHelpline = "116";
        const string Isange = "3029";
        const string Police = "112";

        Text intro, emailNote, helpTitle, helpNote;
        Button email, parentArea, callChild, callIsange, callPolice;

        public override string Name { get { return "contact"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("menu"));
            var list = UIFactory.ScrollList(col, Theme.Gap);
            intro = UIFactory.Label(list, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            email = UIFactory.Button(list, "", Theme.Primary, Color.white, () =>
                Application.OpenURL("mailto:" + TeamEmail + "?subject=" + System.Uri.EscapeDataString("SafeTouch")));
            emailNote = UIFactory.Label(list, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            parentArea = UIFactory.Button(list, "", Theme.Card, Theme.Ink, () => ui.ShowMenu("parent"));

            helpTitle = UIFactory.Label(list, "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            helpNote = UIFactory.Label(list, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            callChild = UIFactory.Button(list, "", Theme.Safe, Color.white, () => Application.OpenURL("tel:" + ChildHelpline));
            callIsange = UIFactory.Button(list, "", Theme.Safe, Color.white, () => Application.OpenURL("tel:" + Isange));
            callPolice = UIFactory.Button(list, "", Theme.Unsafe, Color.white, () => Application.OpenURL("tel:" + Police));
        }

        public override void OnShow()
        {
            TitleText.text = L("Contact us", "Twandikire");
            intro.text = L("Questions, ideas or a problem with the app? Write to the SafeTouch team. We read every message.",
                           "Mufite ibibazo, ibitekerezo cyangwa ikibazo kuri porogaramu? Mwandikire itsinda rya SafeTouch. Dusoma ubutumwa bwose.");
            UIFactory.SetButtonLabel(email, L("Email us", "Twoherereze imeyili"));
            emailNote.text = TeamEmail;
            UIFactory.SetButtonLabel(parentArea, L("Open the parent area", "Fungura ahagenewe ababyeyi"));
            helpTitle.text = L("Worried about a child?", "Muhangayikishijwe n'umwana?");
            helpNote.text = L("These calls are free in Rwanda. If a child is in danger now, call the police.",
                              "Izi nimero ni ubuntu mu Rwanda. Niba umwana ari mu kaga ubu, hamagara polisi.");
            UIFactory.SetButtonLabel(callChild, L("Child helpline: 116", "Umurongo w'abana: 116"));
            UIFactory.SetButtonLabel(callIsange, L("Isange One Stop Centre: 3029", "Isange One Stop Centre: 3029"));
            UIFactory.SetButtonLabel(callPolice, L("Police: 112", "Polisi: 112"));
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
