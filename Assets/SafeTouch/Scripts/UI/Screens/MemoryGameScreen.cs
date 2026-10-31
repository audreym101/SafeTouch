using System;
using System.Collections;
using System.Collections.Generic;
using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Memory Quest, round 1: match the pairs. Cards lie face down; the child
    /// turns two at a time to match a word ("Red zone") with its meaning
    /// ("My body: only with my OK"). Each match shows a one-line reminder.
    /// Recalling an idea, rather than re-reading it, is what makes it stick.
    /// </summary>
    public class MemoryGameScreen : UIScreen
    {
        class Card
        {
            public ReviewPair pair;
            public bool isWord;
            public Button button;
            public Image image;
            public Text text;
            public bool faceUp, matched;
        }

        readonly List<Card> cards = new List<Card>();
        RectTransform grid;
        GridLayoutGroup layout;
        Text hint, counter;
        Button read;
        Card first;
        bool busy;
        int flips, matchedPairs, pairCount;
        Action<int, int> finished;

        public override string Name { get { return "memory"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            hint = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            UIFactory.FixedHeight(hint, 170f);
            var row = UIFactory.Row(col, "Counter row", Theme.Gap, 110f);
            counter = UIFactory.Label(row.transform, "", Theme.Label, Theme.Muted, TextAnchor.MiddleLeft, true);
            counter.GetComponent<LayoutElement>().flexibleWidth = 2f;
            read = UIFactory.Button(row.transform, "Read to me", Theme.Card, Theme.Ink, () => game.ReadToMe(hint.text));
            read.GetComponentInChildren<Text>().fontSize = Theme.Label;
            grid = UIFactory.Rect(col, "Cards");
            grid.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.spacing = new Vector2(24f, 24f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.childAlignment = TextAnchor.UpperCenter;
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("review.round1", "Round 1 of 4: match the pairs");
            UIFactory.SetButtonLabel(read, Localization.T("lesson.replay", "Read to me"));
        }

        /// <summary>Deals two cards per pair, face down and shuffled.</summary>
        public void StartGame(List<ReviewPair> pairs, Action<int, int> onFinished)
        {
            finished = onFinished;
            first = null;
            busy = false;
            flips = 0;
            matchedPairs = 0;
            pairCount = pairs.Count;
            UIFactory.Clear(grid);
            cards.Clear();
            foreach (var p in pairs)
            {
                cards.Add(new Card { pair = p, isWord = true });
                cards.Add(new Card { pair = p, isWord = false });
            }
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var tmp = cards[i]; cards[i] = cards[j]; cards[j] = tmp;
            }

            int columns = cards.Count > 8 ? 3 : 2;
            int rows = Mathf.CeilToInt(cards.Count / (float)columns);
            layout.constraintCount = columns;
            FitCards(columns, rows);

            foreach (var card in cards)
            {
                var c = card;
                c.button = UIFactory.Button(grid, "?", Theme.Primary, Color.white, () => Flip(c));
                c.image = c.button.GetComponent<Image>();
                c.text = c.button.GetComponentInChildren<Text>();
                c.text.fontSize = Theme.Display;
            }
            hint.text = Localization.T("review.memory.hint", "Turn over two cards. Find the ones that go together.");
            UpdateCounter();
            if (game.Reader != null) game.Reader.SpeakIfAuto(hint.text);
        }

        /// <summary>
        /// Sizes the cards to the space the grid really has on this phone or
        /// tablet (taller phones get taller cards, up to 300 units), instead of
        /// assuming a 1080 x 1920 screen.
        /// </summary>
        void FitCards(int columns, int rows)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid.parent);
            var area = grid.rect.size;
            if (area.x <= 0f || area.y <= 0f) area = new Vector2(1080f - 2f * Theme.Gutter, 1180f);   // not laid out yet
            float width = (area.x - (columns - 1) * layout.spacing.x) / columns;
            float height = Mathf.Min(300f, (area.y - (rows - 1) * layout.spacing.y) / rows);
            layout.cellSize = new Vector2(width, height);
        }

        void Flip(Card card)
        {
            // A card that is already turned over is read out when tapped, for children still learning to read.
            if (card.matched || card.faceUp) { game.ReadToMe(card.text.text); return; }
            if (busy) return;
            Show(card, true);
            if (game.Reader != null) game.Reader.SpeakIfAuto(card.text.text);
            if (first == null) { first = card; return; }

            flips++;
            var a = first;
            first = null;
            if (a.pair == card.pair)
            {
                a.matched = card.matched = true;
                Paint(a, Theme.WithAlpha(Theme.Safe, 0.35f));
                Paint(card, Theme.WithAlpha(Theme.Safe, 0.35f));
                matchedPairs++;
                hint.text = Localization.T("review.memory.match", "A match!") + " " + card.pair.remember;
                if (game.Reader != null) game.Reader.SpeakIfAuto(hint.text);
                game.Audio.PlaySFX("correct");
                if (game.AR.Character != null) game.AR.Character.Bounce();
                if (matchedPairs == pairCount) game.UI.StartCoroutine(FinishSoon());
            }
            else
            {
                busy = true;
                game.UI.StartCoroutine(HideAgain(a, card));
            }
            UpdateCounter();
        }

        void Show(Card card, bool faceUp)
        {
            card.faceUp = faceUp;
            card.text.text = faceUp ? (card.isWord ? card.pair.word : card.pair.meaning) : "?";
            card.text.fontSize = faceUp ? Theme.Label : Theme.Display;
            card.text.color = faceUp ? Theme.Ink : Color.white;
            card.image.color = faceUp ? (card.isWord ? Theme.Card : Theme.WithAlpha(Theme.Sun, 0.35f)) : Theme.Primary;
        }

        static void Paint(Card card, Color color)
        {
            card.image.color = color;
        }

        void UpdateCounter()
        {
            counter.text = string.Format(Localization.T("review.memory.count", "{0} of {1} pairs found  |  {2} turns"), matchedPairs, pairCount, flips);
        }

        IEnumerator HideAgain(Card a, Card b)
        {
            yield return new WaitForSeconds(1.2f);
            Show(a, false);
            Show(b, false);
            busy = false;
        }

        IEnumerator FinishSoon()
        {
            yield return new WaitForSeconds(1.5f);
            if (finished != null) finished(matchedPairs, flips);
        }
    }
}
