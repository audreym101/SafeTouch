using System.Collections.Generic;
using SafeTouch.Core;
using SafeTouch.Voice;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// My NO: the voice coach outside the lessons, and what the child has
    /// achieved with their voice.
    ///
    /// - Practise: the same "Say NO!" card as the lessons (NO power meter,
    ///   pitch line, stars, the loud / steady / short checks and a tip), as
    ///   often as the child likes.
    /// - Numbers: their record, strong NOs and how many NOs they have said.
    /// - How my NO has grown: a bar for each recent NO, with the PASS line.
    /// - Achievements: goals on the way to a strong NO, done or how close.
    /// - Hear my NOs: the recordings kept on this phone (the first, the best
    ///   and the newest), to play back.
    ///
    /// Before the first spoken NO, Keza asks to use the microphone in her own
    /// words (GameManager.ExplainMicrophone), then Android asks.
    /// </summary>
    public class MyNoScreen : UIScreen, ITickable
    {
        static readonly Color Gold = Theme.Hex("#E9A912");
        const int ChartBars = 12;

        readonly VoicePanel coach = new VoicePanel();
        RectTransform list;
        GameObject scroll;
        bool practising;

        public override string Name { get { return "myno"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", Back);
            coach.Build(col);
            coach.Record = () => game.Progress.LoudestNo;
            coach.KeepRecord = power => game.Progress.RecordNoPower(power);
            coach.PlaySound = sound => game.Audio.PlaySFX(sound);
            list = UIFactory.ScrollList(col, Theme.Gap);
            scroll = list.parent.parent.gameObject;   // content > viewport > scroll
        }

        void Back()
        {
            if (practising) { StopPractice(); return; }
            ui.ShowMenu(ui.CameFrom("menu", "home", "badges"));
        }

        public void Tick()
        {
            coach.Tick();
        }

        public override void OnHide()
        {
            if (practising) StopPractice();
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }

        public override void OnShow()
        {
            TitleText.text = T("myno.title", "My NO");
            if (practising) return;   // the coach card is showing; the list is rebuilt when it closes
            UIFactory.Clear(list);
            var progress = game.Progress.GetData();
            var journal = game.NoVoices.Journal ?? new NoJournal();

            BuildPractice(progress);
            BuildNumbers(journal, progress);
            BuildGrowth(journal);
            BuildAchievements(journal, progress);
            BuildRecordings(journal, progress);
        }

        // ---- Practise ----

        void BuildPractice(Data.Progress progress)
        {
            var card = UIFactory.Panel(list, "Practise", Theme.WithAlpha(Theme.Unsafe, 0.12f), true);
            UIFactory.Column((RectTransform)card.transform, 12f, 36);
            UIFactory.Label(card.transform, T("myno.practise.title", "Practise your NO"), Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            UIFactory.Label(card.transform, T("myno.practise.body", "Stand tall, look at the person and say NO like you mean it. Keza tells you how strong it was."),
                Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);

            var voice = game.Voice;
            if (voice == null || !progress.voiceEnabled)
            {
                UIFactory.Label(card.transform, T("myno.off", "A grown-up has switched voice practice off. They can switch it on in Grown-ups > Settings."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                return;
            }
            if (!MicrophoneInput.HasDevice)
            {
                UIFactory.Label(card.transform, T("myno.nomic", "This phone has no microphone, so Keza can't hear you here. You can still say NO in the lessons with the NO button."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                return;
            }
            if (voice.Declined)
            {
                UIFactory.Label(card.transform, T("myno.declined", "The microphone is off for now."), Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                UIFactory.Button(card.transform, T("myno.allow", "Use my voice"), Theme.Primary, Color.white, () =>
                {
                    voice.AllowAskingAgain();
                    StartPractice();
                });
                return;
            }
            var go = UIFactory.Button(card.transform, T("myno.practise.go", "Say NO now!"), Theme.Unsafe, Color.white, StartPractice);
            UIFactory.FixedHeight(go, Theme.TouchTarget * 1.3f);
        }

        void StartPractice()
        {
            practising = true;
            scroll.SetActive(false);
            coach.Show(T("myno.say.title", "Say NO like you mean it!"),
                T("myno.say.body", "Pretend someone wants to break your body rules. Stand tall and say NO, clear and firm!"),
                game.Voice, rating =>
                {
                    practising = false;
                    scroll.SetActive(true);
                    if (rating != null) game.Progress.RecordNo(rating);
                    OnShow();
                });
        }

        void StopPractice()
        {
            practising = false;
            coach.Hide();
            scroll.SetActive(true);
            OnShow();
        }

        // ---- Numbers ----

        void BuildNumbers(NoJournal journal, Data.Progress progress)
        {
            var row = UIFactory.Row(list, "Numbers", Theme.Gap, 230f);
            NumberTile(row.transform, progress.loudestNo.ToString(), T("myno.record", "My record"), Gold);
            NumberTile(row.transform, System.Math.Max(progress.strongNoCount, journal.threeStars).ToString(), T("myno.strong", "3-star NOs"), Theme.Safe);
            NumberTile(row.transform, journal.total.ToString(), T("myno.said", "NOs said"), Theme.Primary);
        }

        static void NumberTile(Transform parent, string number, string caption, Color colour)
        {
            var tile = UIFactory.Panel(parent, caption, Theme.Card, true);
            var le = tile.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 0f;   // the three tiles share the row equally, whatever their words
            le.flexibleWidth = 1f;
            var column = UIFactory.Column((RectTransform)tile.transform, 0f, 16);
            column.childAlignment = TextAnchor.MiddleCenter;
            var big = UIFactory.Label(tile.transform, number, Theme.Display, colour, TextAnchor.MiddleCenter, true);
            UIFactory.FitInside(big);
            var small = UIFactory.Label(tile.transform, caption, Theme.Small, Theme.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.FitInside(small);
        }

        // ---- How my NO has grown ----

        /// <summary>
        /// One bar per NO, oldest on the left, as tall as its NO power, with the PASS line across. The record
        /// is gold, NOs past the line green, the others blue. Under it: the first NO against the best.
        /// </summary>
        void BuildGrowth(NoJournal journal)
        {
            if (journal.entries.Count == 0) return;
            UIFactory.Label(list, T("myno.growth", "How my NO has grown"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);

            var chart = UIFactory.Panel(list, "Chart", Theme.Card, true);
            UIFactory.FixedHeight(chart, 300f);
            var area = UIFactory.Rect(chart.transform, "Bars");
            UIFactory.Stretch(area);
            area.offsetMin = new Vector2(28f, 24f);
            area.offsetMax = new Vector2(-28f, -24f);

            var best = journal.Best;
            int start = Mathf.Max(0, journal.entries.Count - ChartBars);
            int count = journal.entries.Count - start;
            float slot = 1f / ChartBars;
            for (int i = 0; i < count; i++)
            {
                var e = journal.entries[start + i];
                var bar = UIFactory.Panel(area, "Bar", e == best ? Gold : e.loud ? Theme.Safe : Theme.Primary, true);
                bar.raycastTarget = false;
                var rt = (RectTransform)bar.transform;
                float x = i * slot;
                rt.anchorMin = new Vector2(x + slot * 0.15f, 0f);
                rt.anchorMax = new Vector2(x + slot * 0.85f, Mathf.Max(0.03f, e.power / 100f));
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }

            // The PASS line, the same as on the coach's meter.
            float pass = game.Voice != null ? Mathf.Clamp01(game.Voice.StrongLevel01) : 0.43f;
            var line = UIFactory.Panel(area, "Pass line", Theme.Ink, false);
            line.raycastTarget = false;
            var lrt = (RectTransform)line.transform;
            lrt.anchorMin = new Vector2(0f, pass);
            lrt.anchorMax = new Vector2(1f, pass);
            lrt.sizeDelta = new Vector2(0f, 5f);
            var word = UIFactory.Label(lrt, T("voice.pass", "PASS"), Theme.Small, Theme.Ink, TextAnchor.LowerLeft, true);
            var wrt = (RectTransform)word.transform;
            wrt.anchorMin = wrt.anchorMax = new Vector2(0f, 1f);
            wrt.pivot = new Vector2(0f, 0f);
            wrt.sizeDelta = new Vector2(160f, 44f);

            var first = journal.First;
            string summary = string.Format(T("myno.growth.body", "My first NO: {0}. My best NO: {1}."), first.power, best.power);
            int grew = best.power - first.power;
            if (grew > 0) summary += " " + string.Format(T("myno.growth.more", "That's {0} stronger!"), grew);
            UIFactory.Label(list, summary, Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
        }

        // ---- Achievements ----

        static readonly Dictionary<string, string[]> MilestoneWords = new Dictionary<string, string[]>
        {
            { "first",   new[] { "First NO", "I said NO out loud for the first time." } },
            { "pass",    new[] { "Past the line", "My NO reached the PASS line." } },
            { "stars",   new[] { "Three stars", "A NO that was loud, steady and short." } },
            { "power50", new[] { "NO power 50", "My NO reached 50 on the meter." } },
            { "strong3", new[] { "Strong Voice", "Three NOs with three stars." } },
            { "ten",     new[] { "Ten NOs", "I practised my NO ten times." } },
            { "grew",    new[] { "Growing stronger", "My best NO is 15 more than my first." } },
            { "power80", new[] { "Mighty NO", "My NO reached 80 on the meter." } },
        };

        void BuildAchievements(NoJournal journal, Data.Progress progress)
        {
            var milestones = NoMilestones.For(journal, progress.loudestNo, progress.strongNoCount);
            int done = 0;
            foreach (var m in milestones) if (m.Done) done++;
            UIFactory.Label(list, T("myno.achieved", "What I have achieved"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Label(list, string.Format(T("myno.achieved.count", "{0} of {1} done"), done, milestones.Count),
                Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);

            foreach (var m in milestones)
            {
                string[] words = MilestoneWords[m.id];
                var card = UIFactory.Panel(list, m.id, m.Done ? Theme.WithAlpha(Theme.Sun, 0.35f) : Theme.Card, true);
                var row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(24, 24, 16, 16);
                row.spacing = 24f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = row.childControlHeight = true;
                row.childForceExpandWidth = row.childForceExpandHeight = false;

                var dot = UIFactory.Dot(card.transform, "Done", m.Done ? Theme.Safe : Theme.WithAlpha(Theme.Muted, 0.25f), 88f);
                dot.raycastTarget = false;
                if (m.Done)
                {
                    var tick = UIFactory.Panel(dot.transform, "Tick", Color.white, false);
                    tick.sprite = Icons.Check;
                    tick.raycastTarget = false;
                    var trt = (RectTransform)tick.transform;
                    trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
                    trt.sizeDelta = new Vector2(60f, 60f);
                }

                var text = UIFactory.Rect(card.transform, "Words");
                text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                UIFactory.Column(text, 2f, 0).childAlignment = TextAnchor.MiddleLeft;
                UIFactory.Label(text, T("myno.goal." + m.id, words[0]), Theme.Body, m.Done ? Theme.Ink : Theme.Muted, TextAnchor.MiddleLeft, true);
                string detail = T("myno.goal." + m.id + ".body", words[1]);
                if (!m.Done) detail += "  " + string.Format(T("myno.goal.progress", "({0} of {1})"), Mathf.Min(m.value, m.goal), m.goal);
                UIFactory.Label(text, detail, Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, false);
            }
        }

        // ---- Hear my NOs ----

        void BuildRecordings(NoJournal journal, Data.Progress progress)
        {
            UIFactory.Label(list, T("myno.hear", "Hear my NOs"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            var kept = journal.WithAudio();
            if (kept.Count == 0)
            {
                UIFactory.Label(list, progress.keepNoRecordings
                        ? T("myno.hear.none", "Say NO out loud and it is saved here, so you can hear how strong you are getting.")
                        : T("myno.hear.off", "A grown-up has switched off keeping NO recordings. Your numbers and achievements are still kept."),
                    Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                return;
            }
            foreach (var entry in kept) NoClipRow.Build(list, entry, journal, game);
            UIFactory.Label(list, T("myno.private", "Your NOs stay on this phone. Only you and your grown-up can hear them."),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
        }
    }
}
