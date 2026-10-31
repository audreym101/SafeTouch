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
    /// People picker for lesson 4. The child taps kinds of people, never
    /// names. Trusted mode (FR7): the grown-ups they trust; at least two are
    /// needed, five fill the "helping hand". Unsafe mode: anyone who makes
    /// them feel unsafe or worried, which their parent can see.
    /// </summary>
    public class AdultsScreen : UIScreen
    {
        Text instructions, counter;
        RectTransform grid;
        Button confirm;
        readonly Dictionary<TrustedAdult, Button> tiles = new Dictionary<TrustedAdult, Button>();
        bool unsafeMode, tilesForUnsafe;
        Action<List<TrustedAdult>> unsafeDone;

        public const int Minimum = 2;
        public const int HelpingHand = 5;

        public Action<List<TrustedAdult>> Confirmed;

        public override string Name { get { return "adults"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", Back);
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
                if (game.Reader != null) game.Reader.Stop();
                if (unsafeMode) { Finish(Selected()); return; }
                if (Confirmed != null) Confirmed(Selected());
            });
        }

        /// <summary>Opens the picker for "who makes you feel unsafe?". done gets the choices, or null if the child went back.</summary>
        public void AskUnsafe(Action<List<TrustedAdult>> done)
        {
            unsafeMode = true;
            unsafeDone = done;
            foreach (var p in ContentLibrary.UnsafePeople) p.isSelected = false;   // a fresh answer each time
            ui.ShowMenu(Name);
        }

        void Finish(List<TrustedAdult> chosen)
        {
            var done = unsafeDone;
            unsafeDone = null;
            unsafeMode = false;
            if (done != null) done(chosen);
        }

        /// <summary>Back from the unsafe question skips it; back from the trusted adults leaves the lesson.</summary>
        void Back()
        {
            if (unsafeMode) Finish(null);
            else game.ReturnToMenu();
        }

        public override void OnShow()
        {
            if (unsafeMode)
            {
                TitleText.text = Localization.T("adults.unsafe.screen", "Feeling unsafe");
                instructions.text = Localization.T("adults.unsafe.instructions", "Tap anyone who makes you feel unsafe or worried.");
                UIFactory.SetButtonLabel(confirm, Localization.T("adults.unsafe.done", "Done"));
            }
            else
            {
                TitleText.text = Localization.T("lesson4.title");
                instructions.text = Localization.T("adults.instructions");
                UIFactory.SetButtonLabel(confirm, Localization.T("adults.place"));
            }
            if (tiles.Count == 0 || tilesForUnsafe != unsafeMode) BuildTiles();
            Refresh();
            if (game.Reader != null) game.Reader.SpeakIfAuto(instructions.text);
        }

        public List<TrustedAdult> Selected()
        {
            return tiles.Keys.Where(a => a.isSelected).ToList();
        }

        void BuildTiles()
        {
            UIFactory.Clear(grid);
            tiles.Clear();
            tilesForUnsafe = unsafeMode;
            var people = unsafeMode ? ContentLibrary.UnsafePeople : ContentLibrary.TrustedAdults;
            var chosen = game.Progress.GetData().trustedAdults;
            foreach (var person in people)
            {
                var a = person;
                if (!unsafeMode) a.isSelected = chosen.Contains(a.adultId);
                var tile = UIFactory.Button(grid, a.name, Theme.Card, Theme.Ink, () =>
                {
                    a.Select();
                    if (a.isSelected && game.Reader != null) game.Reader.SpeakIfAuto(a.name);
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
            counter.gameObject.SetActive(!unsafeMode);
            counter.text = string.Format(Localization.T("adults.counter"), count, HelpingHand);
            confirm.interactable = count >= (unsafeMode ? 1 : Minimum);
        }
    }
}
