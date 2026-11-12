using System;
using System.Globalization;
using SafeTouch.Core;
using SafeTouch.Voice;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// One kept NO in a list: a big round play button, its NO power and stars,
    /// and when and where it was said. Used on the child's My NO screen and in
    /// Grown-ups > My child.
    /// </summary>
    public static class NoClipRow
    {
        static readonly Color Gold = Theme.Hex("#E9A912");

        public static void Build(Transform parent, NoEntry entry, NoJournal journal, GameManager game)
        {
            bool best = entry == journal.Best, first = entry == journal.First;
            var card = UIFactory.Panel(parent, "NO " + entry.id, best ? Theme.WithAlpha(Theme.Sun, 0.3f) : Theme.Card, true);
            var row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(24, 24, 18, 18);
            row.spacing = 24f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            if (entry.HasAudio)
            {
                var play = UIFactory.Button(card.transform, "", Theme.Primary, Color.white, () =>
                {
                    if (!game.NoVoices.Play(entry, game.Audio))
                        game.UI.ShowToast(Localization.T("myno.play.missing", "This NO's sound is no longer on the phone."));
                });
                UIFactory.Remove(play.GetComponentInChildren<Text>().gameObject);
                play.image.sprite = UIFactory.Circle;
                play.image.type = Image.Type.Simple;
                var le = play.GetComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = 120f;
                le.minHeight = le.preferredHeight = 120f;
                le.flexibleWidth = 0f;
                var icon = UIFactory.Panel(play.transform, "Play", Color.white, false);
                icon.sprite = Icons.Play;
                icon.raycastTarget = false;
                var irt = (RectTransform)icon.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = new Vector2(72f, 72f);
            }

            var words = UIFactory.Rect(card.transform, "Words");
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var column = UIFactory.Column(words, 4f, 0);
            column.childAlignment = TextAnchor.MiddleLeft;

            string name = best ? Localization.T("myno.best", "My best NO")
                : first ? Localization.T("myno.first", "My first NO")
                : Localization.T("myno.one", "A NO");
            UIFactory.Label(words, name + ":  " + string.Format(Localization.T("myno.power", "NO power {0}"), entry.power),
                Theme.Body, best ? Gold : Theme.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Label(words, Stars(entry.stars) + "  ·  " + When(entry.at) + "  ·  " + Source(entry.source, game),
                Theme.Small, Theme.Muted, TextAnchor.MiddleLeft, false);
        }

        public static string Stars(int stars)
        {
            return stars == 1 ? Localization.T("myno.star", "1 star") : string.Format(Localization.T("myno.stars", "{0} stars"), stars);
        }

        /// <summary>The day and time, written the same in both languages (e.g. 11/11/2026 14:05).</summary>
        public static string When(string iso)
        {
            DateTime at;
            if (!DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out at)) return "";
            return at.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>Where the NO was said: on the practice screen, or the lesson's name.</summary>
        public static string Source(string source, GameManager game)
        {
            if (source == "practice") return Localization.T("myno.source.practice", "Practice");
            int level;
            if (source != null && source.StartsWith("lesson:") && int.TryParse(source.Substring(7), out level) && level > 0)
                return LessonInfo.For(level).Title(game.Progress.GetData().ageGroup == Data.Progress.PathOlder);
            return Localization.T("myno.source.lesson", "A lesson");
        }
    }
}
