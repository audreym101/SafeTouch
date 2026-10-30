using System.Collections.Generic;
using SafeTouch.Core;
using SafeTouch.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The lesson map: four lessons and the Memory Quest in order, the Bubble Guard game, stars earned, and the
    /// secondary destinations (badges, Keza's look, grown-ups).
    /// </summary>
    public class HomeScreen : UIScreen
    {
        class Card
        {
            public LessonInfo info;
            public Button button;
            public Text title, summary;
        }

        readonly List<Card> cards = new List<Card>();
        Text heading, stars, pathNote;
        Button badges, look, grownUps, youngTab, olderTab;

        public override string Name { get { return "home"; } }

        protected override void BuildContent(RectTransform root)
        {
            // "Pick a lesson" sits in the top bar beside the back button, leaving more room for the cards.
            var col = StandardColumn(root, "", () => ui.ShowMenu("menu"));
            heading = TitleText;
            stars = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);

            // Two lesson paths, one tab each: Explorer (6-8) and Guardian (9-11), each with its own stars and badges.
            var tabs = UIFactory.Row(col, "Paths", Theme.Gap, Theme.TouchTarget);
            youngTab = UIFactory.Button(tabs.transform, "", Theme.Card, Theme.Ink, () => ShowPath(Data.Progress.PathYoung));
            olderTab = UIFactory.Button(tabs.transform, "", Theme.Card, Theme.Ink, () => ShowPath(Data.Progress.PathOlder));
            pathNote = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, true);

            // The four lessons and the Memory Quest, then the Bubble Guard game. They all fit on
            // 16:9 and taller phones; on smaller screens, or with longer Kinyarwanda text, the list scrolls.
            var list = UIFactory.ScrollList(col, 24f);
            var all = new List<LessonInfo>(LessonInfo.All) { LessonInfo.Game };
            foreach (var info in all)
            {
                var lesson = info;
                var card = new Card { info = info };
                card.button = UIFactory.Button(list, "", Theme.Card, Theme.Ink, () => game.StartLesson(lesson.level));
                UIFactory.Remove(card.button.GetComponentInChildren<Text>().gameObject);

                // The card is as tall as its words need, and at least 176 units.
                var size = card.button.GetComponent<LayoutElement>();
                size.minHeight = 176f;
                size.preferredHeight = -1f;
                var v = card.button.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(44, 140, 14, 14);   // room on the right for the number badge
                v.spacing = 4f;
                v.childAlignment = TextAnchor.MiddleLeft;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;

                var stripe = UIFactory.Panel(card.button.transform, "Colour", info.color, true);
                stripe.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var srt = (RectTransform)stripe.transform;
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(0f, 1f);
                srt.offsetMin = new Vector2(0f, 0f);
                srt.offsetMax = new Vector2(20f, 0f);
                stripe.raycastTarget = false;

                card.title = UIFactory.Label(card.button.transform, "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
                card.summary = UIFactory.Label(card.button.transform, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);
                card.summary.supportRichText = true;   // the status follows the summary in the lesson's colour
                Decor.Badge((RectTransform)card.button.transform, info == LessonInfo.Game ? "G" : info.level.ToString(), info.color, 96f);
                cards.Add(card);
            }

            var row = UIFactory.Row(col, "Secondary", Theme.Gap, Theme.TouchTarget);
            badges = UIFactory.Button(row.transform, "My badges", Theme.Sun, Theme.Ink, () => ui.ShowMenu("badges"));
            look = UIFactory.Button(row.transform, "Keza's look", Theme.Card, Theme.Ink, () => ui.ShowMenu("look"));
            grownUps = UIFactory.Button(row.transform, "Grown-ups", Theme.Card, Theme.Ink, () => ui.ShowMenu("parentGate"));
        }

        void ShowPath(string age)
        {
            var progress = game.Progress.GetData();
            if (!progress.IsPathOpen(age)) { game.ShowAgeGate("home"); return; }   // a grown-up confirms the age first
            game.SwitchPath(age);
            OnShow();
        }

        /// <summary>The path tabs and the note under them: which path is in play, how far it is, and what is different in it.</summary>
        void ShowPathTabs(Data.Progress progress)
        {
            bool onYoung = progress.ageGroup != Data.Progress.PathOlder;
            bool olderOpen = progress.IsPathOpen(Data.Progress.PathOlder);
            UIFactory.SetButtonLabel(youngTab, L("Explorer 6-8", "Umuvumbuzi 6-8"));
            UIFactory.SetButtonLabel(olderTab, olderOpen ? L("Guardian 9-11", "Umurinzi 9-11") : L("Guardian 9-11 (locked)", "Umurinzi 9-11 (urafunze)"));
            youngTab.image.color = onYoung ? Theme.Primary : Theme.Card;
            olderTab.image.color = !onYoung ? Theme.Safe : Theme.Card;
            youngTab.GetComponentInChildren<Text>().color = onYoung ? Color.white : Theme.Ink;
            olderTab.GetComponentInChildren<Text>().color = !onYoung ? Color.white : Theme.Ink;

            int done = progress.PathDone();
            string note;
            if (onYoung)
            {
                note = string.Format(L("Explorer path: {0} of {1} lessons done. Four steps in each lesson, seven scenario cards and a friendly helper.",
                                       "Inzira y'Umuvumbuzi: amasomo {0} kuri {1} arangiye. Intambwe enye muri buri somo, amakarita arindwi n'umufasha w'inshuti."), done, Data.Progress.PathLessons);
                if (!olderOpen) note += " " + L("Guardian: a grown-up confirms the age first.", "Umurinzi: umuntu mukuru abanza kwemeza imyaka.");
            }
            else
            {
                note = string.Format(L("Guardian path: {0} of {1} lessons done. Five steps in lesson 1 (the bubble online), thirteen scenario cards, \"Think deeper\" notes and a faster chase.",
                                       "Inzira y'Umurinzi: amasomo {0} kuri {1} arangiye. Intambwe eshanu mu isomo rya 1 (akabuga kuri interineti), amakarita cumi n'atatu, ibisobanuro byimbitse n'umukino wihuta."), done, Data.Progress.PathLessons);
            }
            pathNote.text = note;
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }

        public override void OnShow()
        {
            var progress = game.Progress.GetData();
            heading.text = Localization.T("home.heading");
            stars.text = string.Format(Localization.T("home.stars"), progress.GetTotalScore(), progress.badgesEarned.Count);
            ShowPathTabs(progress);
            UIFactory.SetButtonLabel(badges, Localization.T("home.badges"));
            UIFactory.SetButtonLabel(look, Localization.T("home.look", "Keza's look"));
            UIFactory.SetButtonLabel(grownUps, Localization.T("home.grownups"));

            foreach (var card in cards)
            {
                int level = card.info.level;
                bool unlocked = game.Progress.IsUnlocked(level);
                card.button.interactable = unlocked;
                string status;
                if (card.info == LessonInfo.Game)
                {
                    card.title.text = Localization.T(card.info.titleKey, "Bubble Guard");
                    status = !unlocked
                        ? string.Format(Localization.T("status.locked"), ProgressManager.GameUnlockedBy)
                        : progress.guardGamesPlayed > 0
                            ? string.Format(Localization.T("game.best", "Best score: {0}"), progress.bestGuardScore)
                            : Localization.T("game.ready", "Ready to play");
                    SetSummary(card, Localization.T(card.info.summaryKey, "Game: say NO to keep Keza's bubble safe"), status);
                    continue;
                }
                bool done = progress.IsLevelComplete(level);
                card.title.text = level + ". " + Localization.T(card.info.titleKey);
                status = done ? Localization.T("status.done")
                    : unlocked ? Localization.T("status.ready")
                    : string.Format(Localization.T("status.locked"), level - 1);
                SetSummary(card, Localization.T(card.info.summaryKey), status);
            }
        }

        /// <summary>"Learn the three zones around your body  -  Ready to play", the status bold in the lesson's colour.</summary>
        static void SetSummary(Card card, string summary, string status)
        {
            string colour = ColorUtility.ToHtmlStringRGB(card.info.color);
            card.summary.text = summary + "  <color=#" + colour + "><b>" + status + "</b></color>";
        }
    }
}
