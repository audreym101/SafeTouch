using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Camera view while ARCore looks for a surface (FR1). The Place button
    /// only wakes up once the reticle sits on a detected plane (FR2).
    /// </summary>
    public class ScanScreen : UIScreen, ITickable
    {
        Text hint, status;
        Button place;
        bool lastReady = true;

        public override string Name { get { return "scan"; } }
        public override bool ShowsCamera { get { return true; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            var card = UIFactory.Panel(col, "Hint", Theme.WithAlpha(Theme.Card, 0.92f), true);
            UIFactory.Column((RectTransform)card.transform, 8f, 40);
            card.gameObject.AddComponent<LayoutElement>();
            hint = UIFactory.Label(card.transform, "", Theme.Body, Theme.Ink, TextAnchor.MiddleCenter, true);
            status = UIFactory.Label(card.transform, "", Theme.Label, Theme.Muted, TextAnchor.MiddleCenter, false);
            UIFactory.Spacer(col, 1f);
            place = UIFactory.Button(col, "", Theme.Primary, Color.white, () =>
            {
                if (game.AR.PlaceAtReticle()) game.Audio.PlaySFX("place");
            });
        }

        public override void OnShow()
        {
            hint.text = Localization.T("scan.hint");
            UIFactory.SetButtonLabel(place, Localization.T("scan.place"));
            lastReady = !game.AR.HasPlacementPose;   // force a refresh on the next Tick
            Tick();
        }

        public void Tick()
        {
            bool ready = game.AR.HasPlacementPose;
            if (ready == lastReady) return;
            lastReady = ready;
            place.interactable = ready;
            status.text = Localization.T(ready ? "scan.found" : "scan.searching");
        }
    }
}
