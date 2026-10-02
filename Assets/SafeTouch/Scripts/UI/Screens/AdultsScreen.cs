using System;
using System.Collections.Generic;
using System.Linq;
using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Trusted adults picker (FR7). The child taps roles, not names; at
    /// least two are needed, five fill the "helping hand".
    /// </summary>
    public class AdultsScreen : UIScreen
    {
        Text instructions, counter;
        RectTransform grid;
        Button confirm;
        readonly Dictionary<TrustedAdult, Button> tiles = new Dictionary<TrustedAdult, Button>();

        public const int Minimum = 2;
        public const int HelpingHand = 5;

        public Action<List<TrustedAdult>> Confirmed;

        public override string Name { get { return "adults"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            instructions = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);

            grid = UIFactory.Rect(col, "Adults grid");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(460f, 170f);
            g.spacing = new Vector2(Theme.Gap, Theme.Gap);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            g.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            counter = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.MiddleCenter, true);
            confirm = UIFactory.Button(col, "", Theme.Safe, Color.white, () =>
            {
                if (Confirmed != null) Confirmed(Selected());
            });
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("lesson4.title");
            instructions.text = Localization.T("adults.instructions");
            UIFactory.SetButtonLabel(confirm, Localization.T("adults.place"));
            if (tiles.Count == 0) BuildTiles();
            Refresh();
        }

        public List<TrustedAdult> Selected()
        {
            return tiles.Keys.Where(a => a.isSelected).ToList();
        }

        void BuildTiles()
        {
            var chosen = game.Progress.GetData().trustedAdults;
            foreach (var adult in ContentLibrary.TrustedAdults)
            {
                var a = adult;
                a.isSelected = chosen.Contains(a.adultId);
                var tile = UIFactory.Button(grid, a.name, Theme.Card, Theme.Ink, () =>
                {
                    a.Select();
                    Refresh();
                });
                tiles[a] = tile;
            }
        }

        void Refresh()
        {
            foreach (var pair in tiles)
            {
                pair.Value.image.color = pair.Key.isSelected ? Theme.WithAlpha(pair.Key.GetColor(), 0.35f) : Theme.Card;
            }
            int count = Selected().Count;
            counter.text = string.Format(Localization.T("adults.counter"), count, HelpingHand);
            confirm.interactable = count >= Minimum;
        }
    }
}
