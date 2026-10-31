using System.Collections.Generic;
using System.Linq;
using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Parent and teacher section (FR9): what the child said in lesson 4
    /// (with a guessing game: who does my child trust?), conversation guides,
    /// warning signs, Rwandan support services, and settings (language,
    /// read aloud, optional anonymous sync, voice practice, reset).
    /// </summary>
    public class ParentScreen : UIScreen
    {
        static readonly string[] Tabs = { "child", "talk", "signs", "help", "settings" };

        readonly Button[] tabButtons = new Button[Tabs.Length];
        RectTransform list;
        string tab = "child";
        bool confirmReset;
        readonly HashSet<string> guesses = new HashSet<string>();
        bool guessAgain;

        public override string Name { get { return "parent"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("menu"));
            var row = UIFactory.Row(col, "Tabs", 16f, 120f);
            for (int i = 0; i < Tabs.Length; i++)
            {
                string t = Tabs[i];
                tabButtons[i] = UIFactory.Button(row.transform, t, Theme.Card, Theme.Ink, () => { tab = t; confirmReset = false; confirmWithdraw = false; OnShow(); });
                UIFactory.FixedHeight(tabButtons[i], 120f);
                // Five tabs share the row: smaller words, less padding.
                var label = tabButtons[i].GetComponentInChildren<Text>();
                label.fontSize = Theme.Small;
                ((RectTransform)label.transform).offsetMin = new Vector2(6f, 0f);
                ((RectTransform)label.transform).offsetMax = new Vector2(-6f, 0f);
            }
            list = UIFactory.ScrollList(col, Theme.Gap);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("parent.title");
            for (int i = 0; i < Tabs.Length; i++)
            {
                UIFactory.SetButtonLabel(tabButtons[i], Localization.T("parent.tab." + Tabs[i], Tabs[i] == "child" ? "My child" : Tabs[i]));
                tabButtons[i].image.color = Tabs[i] == tab ? Theme.Primary : Theme.Card;
                tabButtons[i].GetComponentInChildren<Text>().color = Tabs[i] == tab ? Color.white : Theme.Ink;
            }

            UIFactory.Clear(list);
            if (tab == "settings") BuildSettings();
            else if (tab == "child") BuildChild();
            else foreach (var r in ContentLibrary.ParentResources.Where(r => r.section == tab)) ResourceCard(r);
        }

        // ---- My child: what the child said in lesson 4 ----

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }

        void BuildChild()
        {
            var progress = game.Progress.GetData();
            BuildGuess(progress);
            BuildUnsafe(progress);
        }

        /// <summary>
        /// A guessing game: the parent picks the grown-ups they think their
        /// child trusts, then sees what the child chose in lesson 4. A way to
        /// start a conversation, not a test.
        /// </summary>
        void BuildGuess(Progress progress)
        {
            UIFactory.Label(list, T("parent.guess.title", "Who does your child trust?"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            var picked = progress.trustedAdults;
            if (picked.Count == 0)
            {
                UIFactory.Label(list, T("parent.guess.wait", "Your child picks the grown-ups they trust in lesson 4, My Trusted Adults. Come back after that lesson."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                return;
            }

            // A parent who has guessed before sees the result straight away.
            if (guessAgain || progress.parentGuesses.Count == 0)
            {
                UIFactory.Label(list, T("parent.guess.body", "Guess which grown-ups your child picked. Tap them, then check your guess."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                var grid = UIFactory.Rect(list, "Guess grid");
                var g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(460f, 150f);
                g.spacing = new Vector2(Theme.Gap, Theme.Gap);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 2;
                g.childAlignment = TextAnchor.UpperCenter;
                foreach (var adult in ContentLibrary.TrustedAdults)
                {
                    var a = adult;
                    bool on = guesses.Contains(a.adultId);
                    UIFactory.Button(grid, a.name, on ? Theme.WithAlpha(a.GetColor(), 0.35f) : Theme.Card, Theme.Ink, () =>
                    {
                        if (!guesses.Remove(a.adultId)) guesses.Add(a.adultId);
                        OnShow();
                    });
                }
                var check = UIFactory.Button(list, T("parent.guess.check", "Check my guess"), Theme.Primary, Color.white, () =>
                {
                    game.Progress.SetParentGuesses(guesses);
                    guessAgain = false;
                    OnShow();
                });
                check.interactable = guesses.Count > 0;
                return;
            }

            var guessed = progress.parentGuesses;
            var adults = ContentLibrary.TrustedAdults;
            var both = adults.Where(a => picked.Contains(a.adultId) && guessed.Contains(a.adultId)).Select(a => a.name).ToArray();
            var missed = adults.Where(a => picked.Contains(a.adultId) && !guessed.Contains(a.adultId)).Select(a => a.name).ToArray();
            var extra = adults.Where(a => !picked.Contains(a.adultId) && guessed.Contains(a.adultId)).Select(a => a.name).ToArray();

            var card = UIFactory.Panel(list, "Guess result", Theme.Card, true);
            UIFactory.Column((RectTransform)card.transform, 10f, 40);
            UIFactory.Label(card.transform, string.Format(T("parent.guess.score", "You guessed {0} of the {1} grown-ups your child picked."), both.Length, picked.Count),
                Theme.Body, Theme.Ink, TextAnchor.UpperLeft, true);
            if (both.Length > 0) ResultLine(card.transform, T("parent.guess.both", "You both picked:"), both, Theme.Safe);
            if (missed.Length > 0) ResultLine(card.transform, T("parent.guess.missed", "Your child also picked:"), missed, Theme.Primary);
            if (extra.Length > 0) ResultLine(card.transform, T("parent.guess.extra", "You guessed, but your child didn't pick:"), extra, Theme.Caution);
            UIFactory.Label(card.transform, extra.Length > 0
                    ? T("parent.guess.tip.extra", "Ask your child about the grown-ups they didn't pick, gently and without blame. They may simply not have thought of them, or there may be a reason worth hearing.")
                    : T("parent.guess.tip", "Talk with your child about the grown-ups they chose, and thank them for knowing who to go to."),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            UIFactory.Button(list, T("parent.guess.again", "Guess again"), Theme.Card, Theme.Ink, () =>
            {
                guesses.Clear();
                guessAgain = true;
                OnShow();
            });
        }

        static void ResultLine(Transform parent, string heading, string[] names, Color color)
        {
            var t = UIFactory.Label(parent, "<color=#" + ColorUtility.ToHtmlStringRGB(color) + "><b>" + heading + "</b></color> " + string.Join(", ", names),
                Theme.Label, Theme.Ink, TextAnchor.UpperLeft, false);
            t.supportRichText = true;
        }

        /// <summary>
        /// What the child answered to "is there anyone who makes you feel
        /// unsafe or worried?", with what to do next. Kinds of people only,
        /// never names; kept on this phone and never synced.
        /// </summary>
        void BuildUnsafe(Progress progress)
        {
            UIFactory.Label(list, T("parent.unsafe.title", "Who makes your child feel unsafe?"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            if (!progress.unsafeAsked)
            {
                UIFactory.Label(list, T("parent.unsafe.wait", "Your child answers this question at the end of lesson 4."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                return;
            }
            if (progress.unsafePeople.Count == 0)
            {
                UIFactory.Label(list, T("parent.unsafe.none", "Your child said nobody makes them feel unsafe or worried. Keep talking: let them know they can always tell you."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                return;
            }

            var names = ContentLibrary.UnsafePeople.Where(p => progress.unsafePeople.Contains(p.adultId)).Select(p => p.name).ToArray();
            var card = UIFactory.Panel(list, "Unsafe answer", Theme.WithAlpha(Theme.Unsafe, 0.12f), true);
            UIFactory.Column((RectTransform)card.transform, 10f, 40);
            UIFactory.Label(card.transform, T("parent.unsafe.said", "Your child said these people make them feel unsafe or worried:"),
                Theme.Label, Theme.Ink, TextAnchor.UpperLeft, false);
            UIFactory.Label(card.transform, string.Join(", ", names), Theme.Body, Theme.Ink, TextAnchor.UpperLeft, true);
            UIFactory.Label(card.transform, T("parent.unsafe.do",
                    "Stay calm. Thank your child for telling. Ask gently what happened and listen. Believe them, and tell them it is not their fault. Do not blame or punish them, and do not question them in detail. If you are worried, the services below can help."),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            foreach (var r in ContentLibrary.ParentResources.Where(r => r.section == "help" && !string.IsNullOrEmpty(r.phone)).Take(2))
            {
                string number = r.phone;
                UIFactory.Button(card.transform, r.title + ": " + number, Theme.Safe, Color.white, () => Application.OpenURL("tel:" + number));
            }
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

            UIFactory.Label(list, T("parent.read.title", "Read aloud"), Theme.Body, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Label(list, T("parent.read.body",
                "For children who are still learning to read. Every card has a \"Read to me\" button. Switched on, the phone also reads each new card by itself. " +
                "It uses the phone's own voice: nothing is recorded or sent. Kinyarwanda needs a Kinyarwanda voice on the phone, which many phones don't have yet."),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            UIFactory.Button(list, progress.readAloud ? T("parent.read.on", "Read every card: ON - tap to switch off") : T("parent.read.off", "Read every card: OFF - tap to switch on"),
                progress.readAloud ? Theme.Safe : Theme.Card, progress.readAloud ? Color.white : Theme.Ink, () =>
                {
                    game.SetReadAloud(!progress.readAloud);
                    OnShow();
                });

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
            else
                UIFactory.Button(list, Localization.Language == "rw" ? "Hindura imeyili" : "Change my email", Theme.Card, Theme.Ink, () => ui.ShowMenu("email"));
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
