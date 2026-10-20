using System.Linq;
using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Parent and teacher section (FR9): conversation guides, warning signs,
    /// Rwandan support services, and settings (language, optional anonymous
    /// sync, voice practice, reset).
    /// </summary>
    public class ParentScreen : UIScreen
    {
        static readonly string[] Tabs = { "talk", "signs", "help", "settings" };

        readonly Button[] tabButtons = new Button[Tabs.Length];
        RectTransform list;
        string tab = "talk";
        bool confirmReset;

        public override string Name { get { return "parent"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("home"));
            var row = UIFactory.Row(col, "Tabs", 16f, 120f);
            for (int i = 0; i < Tabs.Length; i++)
            {
                string t = Tabs[i];
                tabButtons[i] = UIFactory.Button(row.transform, t, Theme.Card, Theme.Ink, () => { tab = t; confirmReset = false; confirmWithdraw = false; OnShow(); });
                UIFactory.FixedHeight(tabButtons[i], 120f);
            }
            list = UIFactory.ScrollList(col, Theme.Gap);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("parent.title");
            for (int i = 0; i < Tabs.Length; i++)
            {
                UIFactory.SetButtonLabel(tabButtons[i], Localization.T("parent.tab." + Tabs[i]));
                tabButtons[i].image.color = Tabs[i] == tab ? Theme.Primary : Theme.Card;
                tabButtons[i].GetComponentInChildren<Text>().color = Tabs[i] == tab ? Color.white : Theme.Ink;
            }

            UIFactory.Clear(list);
            if (tab == "settings") BuildSettings();
            else foreach (var r in ContentLibrary.ParentResources.Where(r => r.section == tab)) ResourceCard(r);
        }

        void ResourceCard(ParentResource r)
        {
            var card = UIFactory.Panel(list, r.resourceId, Theme.Card, true);
            UIFactory.Column((RectTransform)card.transform, 10f, 40);
            UIFactory.Label(card.transform, r.title, Theme.Body, Theme.Ink, TextAnchor.UpperLeft, true);
            UIFactory.Label(card.transform, r.body, Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            if (!string.IsNullOrEmpty(r.phone))
            {
                string number = r.phone;
                UIFactory.Button(card.transform, Localization.T("parent.call") + " " + number, Theme.Safe, Color.white,
                    () => Application.OpenURL("tel:" + number));
            }
        }

        void BuildSettings()
        {
            var progress = game.Progress.GetData();

            UIFactory.Label(list, Localization.T("parent.language"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            var langs = UIFactory.Row(list, "Languages", Theme.Gap, Theme.TouchTarget);
            UIFactory.Button(langs.transform, "English", Localization.Language == "en" ? Theme.Sun : Theme.Card, Theme.Ink, () => game.SetLanguage("en"));
            UIFactory.Button(langs.transform, "Ikinyarwanda", Localization.Language == "rw" ? Theme.Sun : Theme.Card, Theme.Ink, () => game.SetLanguage("rw"));

            UIFactory.Label(list, Localization.T("parent.sync.title"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Label(list, Localization.T("parent.sync.body"), Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            UIFactory.Button(list, Localization.T(progress.cloudSyncConsent ? "parent.sync.on" : "parent.sync.off"),
                progress.cloudSyncConsent ? Theme.Safe : Theme.Card, progress.cloudSyncConsent ? Color.white : Theme.Ink, () =>
                {
                    game.SetCloudSync(!progress.cloudSyncConsent);
                    OnShow();
                });

            UIFactory.Label(list, Localization.T("parent.voice.title", "Saying NO out loud"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Label(list, Localization.T("parent.voice.body",
                "In the lessons your child practises saying NO out loud, and Keza coaches how it sounded: loud, steady and short. " +
                "The microphone opens only while Keza is listening and closes straight after. SafeTouch measures loudness, pitch and length, " +
                "then forgets them. No sound is recorded, saved or sent. Switched off, the lessons use a NO button instead."),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            UIFactory.Button(list, Localization.T(progress.voiceEnabled ? "parent.voice.on" : "parent.voice.off",
                    progress.voiceEnabled ? "Voice practice is on" : "Voice practice is off"),
                progress.voiceEnabled ? Theme.Safe : Theme.Card, progress.voiceEnabled ? Color.white : Theme.Ink, () =>
                {
                    game.SetVoiceEnabled(!progress.voiceEnabled);
                    OnShow();
                });

            BuildConsent();

            UIFactory.Label(list, Localization.T("parent.reset.title"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Button(list, Localization.T(confirmReset ? "parent.reset.confirm" : "parent.reset"),
                confirmReset ? Theme.Unsafe : Theme.Card, confirmReset ? Color.white : Theme.Ink, () =>
                {
                    if (!confirmReset) { confirmReset = true; OnShow(); return; }
                    confirmReset = false;
                    game.ResetProgress();
                });
        }

        bool confirmWithdraw;

        /// <summary>The parent's consent: its status, confirming the emailed code, and withdrawing it.</summary>
        void BuildConsent()
        {
            var record = game.Consent != null ? game.Consent.Record : null;
            UIFactory.Label(list, Localization.T("parent.consent.title", "Your consent"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            string state = record == null ? Localization.T("parent.consent.none", "No consent on this phone yet.")
                : record.verified ? string.Format(Localization.T("parent.consent.verified", "Confirmed by email ({0})."), Short(record.verifiedAt))
                : !record.sentToServer ? Localization.T("parent.consent.unsent", "Saved on this phone; it will be sent and the code emailed when the phone has internet.")
                : string.Format(Localization.T("parent.consent.pending", "Waiting for the code from the email: {0} days left to confirm."), record.DaysLeft);
            if (record != null) state += "\n" + string.Format(Localization.T("parent.consent.who", "Given by {0}, {1}."), record.parentName, record.email);
            UIFactory.Label(list, state, Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            if (record == null) return;

            if (!record.verified)
                UIFactory.Button(list, Localization.T("parent.consent.confirm", "Confirm the code now"), Theme.Primary, Color.white, () => ui.ShowMenu("verify"));
            UIFactory.Button(list, Localization.T(confirmWithdraw ? "parent.consent.withdraw.confirm" : "parent.consent.withdraw",
                    confirmWithdraw ? "Tap again: delete my details and my child's progress" : "Withdraw consent and delete data"),
                confirmWithdraw ? Theme.Unsafe : Theme.Card, confirmWithdraw ? Color.white : Theme.Ink, () =>
                {
                    if (!confirmWithdraw) { confirmWithdraw = true; OnShow(); return; }
                    confirmWithdraw = false;
                    game.WithdrawConsent();
                });
        }

        static string Short(string iso)
        {
            return !string.IsNullOrEmpty(iso) && iso.Length >= 10 ? iso.Substring(0, 10) : iso;
        }
    }
}
