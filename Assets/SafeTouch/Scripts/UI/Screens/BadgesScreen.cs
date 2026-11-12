using System.Collections.Generic;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// My badges: the child's saved progress, made to feel like a game's trophy room rather than a list.
    ///
    /// - My rank: Keza cheering, the child's rank and a bar to the next one. Each finished lesson earns a new
    ///   rank named after what it taught (Bubble Buddy, Smart Spotter, Brave Voice...); the Guardian path
    ///   (9-11) has its own ranks (Boundary Keeper, Red-Flag Spotter, STOP Pro...).
    /// - My journey: the five lessons as stops on a path. Finished stops are ticked with up to three stars,
    ///   the next one glows and pulses, the others are locked.
    /// - My badge shelf: round medals, gold and shining when won, grey with a padlock when not. Tapping one
    ///   says what it is for.
    /// - My records: the fastest NO in the chase, the best Bubble Guard score and the loudest NO (which opens
    ///   My NO).
    /// Everything pops in one after the other when the screen opens.
    /// </summary>
    public class BadgesScreen : UIScreen, ITickable
    {
        static readonly Color Gold = Theme.Hex("#E9A912");
        static readonly Color GuardianNavy = Theme.Hex("#2E3A78");

        static readonly string[] RanksYoung = { "Safety Starter", "Bubble Buddy", "Smart Spotter", "Brave Voice", "Helper Finder", "Safety Champion" };
        static readonly string[] RanksOlder = { "Guardian Recruit", "Boundary Keeper", "Red-Flag Spotter", "STOP Pro", "Network Builder", "Guardian Champion" };

        RectTransform list;
        readonly List<RectTransform> pops = new List<RectTransform>();
        readonly List<RectTransform> pulses = new List<RectTransform>();
        float shownAt;

        public override string Name { get { return "badges"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu(ui.CameFrom("menu", "home")));
            list = UIFactory.ScrollList(col, Theme.Gap);
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("home.badges");
            UIFactory.Clear(list);
            pops.Clear();
            pulses.Clear();
            var progress = game.Progress.GetData();
            bool older = progress.ageGroup == Data.Progress.PathOlder;

            BuildRank(progress, older);
            BuildJourney(progress, older);
            BuildShelf();
            BuildRecords(progress);
            shownAt = Time.unscaledTime;
            Tick();
        }

        /// <summary>Things pop in one after another, then the next lesson's stop keeps pulsing.</summary>
        public void Tick()
        {
            float t = Time.unscaledTime - shownAt;
            for (int i = 0; i < pops.Count; i++)
            {
                if (pops[i] == null) continue;
                float local = Mathf.Clamp01((t - i * 0.07f) / 0.35f);
                // Ease out with a small overshoot: 0 -> 1.12 -> 1.
                float s = local < 1f ? Mathf.Sin(local * Mathf.PI * 0.62f) * 1.12f / Mathf.Sin(Mathf.PI * 0.62f) : 1f;
                pops[i].localScale = Vector3.one * Mathf.Max(0.001f, local >= 1f ? 1f : s);
            }
            foreach (var p in pulses)
                if (p != null) p.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f));
        }

        // ---- My rank ----

        void BuildRank(Data.Progress progress, bool older)
        {
            int done = Mathf.Clamp(progress.PathDone(), 0, Data.Progress.PathLessons);
            var ranks = older ? RanksOlder : RanksYoung;
            string prefix = older ? "rank.g." : "rank.";

            var card = UIFactory.Panel(list, "Rank", older ? GuardianNavy : Theme.Primary, true);
            var row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(28, 36, 24, 24);
            row.spacing = 24f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var keza = Decor.Hop(Decor.Keza(card.transform, 230f), 9f, 0.9f, 0f);
            pops.Add(keza);

            var words = UIFactory.Rect(card.transform, "Words");
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UIFactory.Column(words, 8f, 0).childAlignment = TextAnchor.MiddleLeft;
            UIFactory.Label(words, T("rank.you", "You are a"), Theme.Label, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleLeft, false);
            var rank = UIFactory.Label(words, T(prefix + done, ranks[done]), Theme.Title, Color.white, TextAnchor.MiddleLeft, true);
            UIFactory.FitInside(rank);
            var lift = rank.gameObject.AddComponent<Shadow>();
            lift.effectColor = new Color(0f, 0f, 0f, 0.3f);
            lift.effectDistance = new Vector2(0f, -3f);

            // A bar that fills one fifth for each lesson.
            var bar = UIFactory.Panel(words, "Bar", new Color(1f, 1f, 1f, 0.25f), true);
            UIFactory.FixedHeight(bar, 40f);
            var fill = UIFactory.Panel(bar.transform, "Fill", Theme.Sun, true);
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(Mathf.Max(0.04f, done / (float)Data.Progress.PathLessons), 1f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;

            string next = done >= Data.Progress.PathLessons
                ? T("rank.top", "You reached the top rank! Play again to win every star.")
                : string.Format(T("rank.next", "Finish lesson {0} to become {1}!"), done + 1, T(prefix + (done + 1), ranks[done + 1]));
            UIFactory.Label(words, next, Theme.Small, Color.white, TextAnchor.UpperLeft, false);

            // Stars and badges, big and gold.
            var totals = UIFactory.Row(words, "Totals", 14f, 64f);
            totals.childForceExpandWidth = false;
            totals.childAlignment = TextAnchor.MiddleLeft;
            Icon(totals.transform, Icons.Star, Theme.Sun, 60f);
            UIFactory.Label(totals.transform, progress.GetTotalScore().ToString(), Theme.Title, Theme.Sun, TextAnchor.MiddleLeft, true);
            Icon(totals.transform, Icons.Shield, Theme.Sun, 56f);
            UIFactory.Label(totals.transform, progress.badgesEarned.Count.ToString(), Theme.Title, Theme.Sun, TextAnchor.MiddleLeft, true);
        }

        // ---- My journey ----

        void BuildJourney(Data.Progress progress, bool older)
        {
            UIFactory.Label(list, T("journey.title", "My journey"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            var card = UIFactory.Panel(list, "Journey", Theme.Card, true);
            UIFactory.FixedHeight(card, 300f);

            // The path the stops sit on, behind them.
            var road = UIFactory.Panel(card.transform, "Path", Theme.WithAlpha(Theme.Muted, 0.25f), true);
            var rrt = road.rectTransform;
            rrt.anchorMin = new Vector2(0.1f, 1f);
            rrt.anchorMax = new Vector2(0.9f, 1f);
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.sizeDelta = new Vector2(0f, 16f);
            rrt.anchoredPosition = new Vector2(0f, -100f);
            road.raycastTarget = false;

            var stops = UIFactory.Rect(card.transform, "Stops");
            UIFactory.Stretch(stops);
            var row = stops.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 12, 36, 20);
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;

            bool nextFound = false;
            foreach (var info in LessonInfo.All)
            {
                int level = info.level;
                bool done = progress.IsLevelComplete(level);
                bool next = !done && !nextFound && game.Progress.IsUnlocked(level);
                if (next) nextFound = true;

                var stop = UIFactory.Rect(stops, "Stop " + level);
                var v = UIFactory.Column(stop, 10f, 0);
                v.childAlignment = TextAnchor.UpperCenter;
                v.childForceExpandWidth = false;

                var dot = UIFactory.Dot(stop, "Circle", done ? info.color : next ? Theme.Sun : Theme.WithAlpha(Theme.Muted, 0.3f), 128f);
                dot.raycastTarget = false;
                if (done) Icon(dot.transform, Icons.Check, Color.white, 76f, true);
                else if (next)
                {
                    var number = UIFactory.Label(dot.transform, level.ToString(), Theme.Title, Theme.Ink, TextAnchor.MiddleCenter, true);
                    UIFactory.Stretch(number.rectTransform);
                    number.GetComponent<LayoutElement>().ignoreLayout = true;
                    pulses.Add(dot.rectTransform);
                }
                else Icon(dot.transform, Icons.Lock, Color.white, 58f, true);
                pops.Add(dot.rectTransform);

                // Up to three little stars for how well the lesson went; "Next!" on the stop to play.
                if (done) Stars(stop, progress.BestScore(level), progress.MaxScore(level));
                else Narrow(UIFactory.Label(stop, next ? T("journey.next", "Next!") : "", Theme.Small, Theme.Ink, TextAnchor.MiddleCenter, true));
                Narrow(UIFactory.Label(stop, older ? T("journey.short.g." + level, ShortOlder[level - 1]) : T("journey.short." + level, ShortYoung[level - 1]),
                    Theme.Small, Theme.Muted, TextAnchor.UpperCenter, false));
            }
        }

        /// <summary>Keeps a word under a stop no wider than the stop, shrinking it if it must.</summary>
        static void Narrow(Text label)
        {
            var le = label.GetComponent<LayoutElement>();
            le.preferredWidth = 170f;
            UIFactory.FitInside(label);
        }

        static readonly string[] ShortYoung = { "Bubble", "Safe?", "STOP", "Helpers", "Quest" };
        static readonly string[] ShortOlder = { "Bubble", "Red flags", "STOP", "Network", "Quest" };

        static void Stars(Transform parent, int score, int max)
        {
            int filled = max > 0 ? Mathf.Clamp(Mathf.RoundToInt(3f * score / max), 1, 3) : 3;
            var row = UIFactory.Row(parent, "Stars", 2f, 44f);
            row.childForceExpandWidth = false;
            for (int i = 0; i < 3; i++) Icon(row.transform, Icons.Star, i < filled ? Theme.Sun : Theme.WithAlpha(Theme.Muted, 0.3f), 42f);
        }

        // ---- My badge shelf ----

        void BuildShelf()
        {
            var badges = game.Progress.Badges;
            int won = 0;
            foreach (var b in badges) if (b.unlocked) won++;
            UIFactory.Label(list, T("shelf.title", "My badge shelf"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Label(list, won == badges.Count
                    ? Localization.T("badges.all", "You won every badge! Well done.")
                    : string.Format(T("shelf.count", "{0} of {1} won. Tap a badge to see how to win it!"), won, badges.Count),
                Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);

            var grid = UIFactory.Rect(list, "Shelf");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(300f, 310f);
            g.spacing = new Vector2(24f, 24f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.UpperCenter;

            foreach (var badge in badges)
            {
                var b = badge;
                var cell = UIFactory.Button(grid, "", b.unlocked ? Theme.WithAlpha(Theme.Sun, 0.25f) : Theme.Card, Theme.Ink, () =>
                    ui.ShowToast(b.unlocked ? b.title + ": " + b.description : string.Format(T("shelf.howto", "To win {0}: {1}"), b.title, b.description)));
                UIFactory.Remove(cell.GetComponentInChildren<Text>().gameObject);
                var v = cell.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(12, 12, 20, 12);
                v.spacing = 10f;
                v.childAlignment = TextAnchor.UpperCenter;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = v.childForceExpandHeight = false;

                var medal = Medal(cell.transform, b.unlocked, 170f);
                pops.Add(medal);
                var name = UIFactory.Label(cell.transform, b.title, Theme.Small, b.unlocked ? Theme.Ink : Theme.Muted, TextAnchor.UpperCenter, true);
                UIFactory.FitInside(name);
            }
        }

        /// <summary>A round medal: gold rings and a white star when won, grey with a padlock when not.</summary>
        static RectTransform Medal(Transform parent, bool won, float size)
        {
            var frame = UIFactory.Rect(parent, "Medal");
            var le = frame.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            Disc(frame, won ? Gold : Theme.WithAlpha(Theme.Muted, 0.3f), 1f);
            Disc(frame, won ? Theme.Sun : Theme.WithAlpha(Theme.Muted, 0.18f), 0.8f);
            if (won) Disc(frame, new Color(1f, 1f, 1f, 0.35f), 0.62f);
            Icon(frame, won ? Icons.Star : Icons.Lock, won ? Color.white : Theme.WithAlpha(Theme.Muted, 0.8f), size * (won ? 0.46f : 0.36f), true);
            if (won) Decor.Hop(frame, 6f, 1.3f, Random.value);
            return frame;
        }

        static void Disc(RectTransform parent, Color color, float fraction)
        {
            var d = UIFactory.Panel(parent, "Disc", color, false);
            d.sprite = UIFactory.Circle;
            d.raycastTarget = false;
            var rt = d.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = parent.GetComponent<LayoutElement>().preferredWidth * fraction * Vector2.one;
        }

        // ---- My records ----

        void BuildRecords(Data.Progress progress)
        {
            UIFactory.Label(list, T("records.title", "My records"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            var row = UIFactory.Row(list, "Records", Theme.Gap, 250f);
            Record(row.transform, progress.bestChaseSeconds > 0f ? progress.bestChaseSeconds.ToString("0.0") + " s" : "-",
                T("records.chase", "Fastest NO in the chase"), Theme.Primary, null);
            Record(row.transform, progress.guardGamesPlayed > 0 ? progress.bestGuardScore.ToString() : "-",
                T("records.guard", "Bubble Guard best"), Theme.Hex("#7A5CC7"), null);
            Record(row.transform, progress.loudestNo > 0 ? progress.loudestNo.ToString() : "-",
                T("records.no", "My loudest NO"), Theme.Unsafe, () => ui.ShowMenu("myno"));
        }

        void Record(Transform parent, string number, string caption, Color colour, System.Action open)
        {
            var tile = UIFactory.Button(parent, "", Theme.Card, Theme.Ink, open);
            tile.interactable = open != null;
            var colours = tile.colors;
            colours.disabledColor = Color.white;   // a tile without a link still looks like the others
            tile.colors = colours;
            UIFactory.Remove(tile.GetComponentInChildren<Text>().gameObject);
            var le = tile.GetComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 250f;
            le.minWidth = le.preferredWidth = 0f;   // three tiles share the row equally
            var v = tile.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 16, 16);
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            var big = UIFactory.Label(tile.transform, number, Theme.Display, colour, TextAnchor.MiddleCenter, true);
            UIFactory.FitInside(big);
            var small = UIFactory.Label(tile.transform, caption, Theme.Small, Theme.Ink, TextAnchor.MiddleCenter, false);
            UIFactory.FitInside(small);
            pops.Add((RectTransform)tile.transform);
        }

        // ---- Helpers ----

        /// <summary>A white icon sprite tinted colour. centred: placed over its parent's middle instead of in its layout.</summary>
        static Image Icon(Transform parent, Sprite sprite, Color colour, float size, bool centred = false)
        {
            var img = UIFactory.Panel(parent, "Icon", colour, false);
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            if (centred)
            {
                le.ignoreLayout = true;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
            }
            return img;
        }
    }
}
