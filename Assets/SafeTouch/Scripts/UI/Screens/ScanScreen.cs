using SafeTouch.AR;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Camera view while ARCore looks for a surface (FR1). Before the camera
    /// opens, it explains why SafeTouch needs it and asks Android for
    /// permission; if the child says no, or the phone cannot run AR, it says
    /// so instead of showing a black screen. The Place button only wakes up
    /// once the reticle sits on a detected plane (FR2).
    /// </summary>
    public class ScanScreen : UIScreen, ITickable
    {
        Text hint, status;
        Image card;
        Button place, action;
        ARManager.ARStatus shown;
        bool first = true;

        public override string Name { get { return "scan"; } }
        public override bool ShowsCamera { get { return true; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => game.ReturnToMenu());
            card = UIFactory.Panel(col, "Hint", Theme.WithAlpha(Theme.Card, 0.92f), true);
            UIFactory.Column((RectTransform)card.transform, 8f, 40);
            card.gameObject.AddComponent<LayoutElement>();
            hint = UIFactory.Label(card.transform, "", Theme.Body, Theme.Ink, TextAnchor.MiddleCenter, true);
            status = UIFactory.Label(card.transform, "", Theme.Label, Theme.Muted, TextAnchor.MiddleCenter, false);
            action = UIFactory.Button(card.transform, "", Theme.Primary, Color.white, OnAction);
            UIFactory.Spacer(col, 1f);
            place = UIFactory.Button(col, "", Theme.Primary, Color.white, () =>
            {
                if (game.AR.PlaceAtReticle()) game.Audio.PlaySFX("place");
            });
        }

        public override void OnShow()
        {
            UIFactory.SetButtonLabel(place, Localization.T("scan.place"));
            first = true;
            Tick();
        }

        public void Tick()
        {
            var now = game.AR.Status;
            if (!first && now == shown)
            {
                // The surface grows while the child scans: keep its size up to date.
                if (now == ARManager.ARStatus.TooSmall && Time.frameCount % 10 == 0) status.text = SizeLine();
                return;
            }
            first = false;
            shown = now;
            Show(now);
        }

        void Show(ARManager.ARStatus s)
        {
            // Before the camera is on, the card is solid so the screen isn't a confusing black.
            bool cameraOn = s == ARManager.ARStatus.Searching || s == ARManager.ARStatus.TooSmall || s == ARManager.ARStatus.Ready;
            Root.GetComponent<Image>().color = cameraOn ? Theme.Clear : Theme.Sky;
            place.gameObject.SetActive(cameraOn);
            place.interactable = s == ARManager.ARStatus.Ready;

            switch (s)
            {
                case ARManager.ARStatus.NeedsCamera:
                    Set(T("scan.camera.title", "Keza needs your camera"),
                        T("scan.camera.body", "SafeTouch uses the camera to show Keza in your room. The picture stays on this phone: it is never saved or sent."),
                        T("scan.camera.allow", "Allow the camera"));
                    break;
                case ARManager.ARStatus.CameraDenied:
                    if (game.AR.CanAskForCamera)
                        Set(T("scan.denied.title", "The camera is switched off"),
                            T("scan.denied.body", "Without the camera, Keza can't appear in your room. Tap below and choose Allow."),
                            T("scan.denied.again", "Try again"));
                    else
                        Set(T("scan.denied.title", "The camera is switched off"),
                            T("scan.settings.body", "Ask a grown-up to open Settings and allow the camera for SafeTouch, then come back."),
                            T("scan.settings.open", "Open Settings"));
                    break;
                case ARManager.ARStatus.Unsupported:
                    Set(T("scan.unsupported.title", "This phone can't show AR"),
                        T("scan.unsupported.body", "SafeTouch needs a phone that works with Google Play Services for AR (ARCore). Ask a grown-up to try another phone."),
                        T("scan.unsupported.back", "Back to the lessons"));
                    break;
                case ARManager.ARStatus.Preparing:
                    Set(T("scan.preparing", "Getting the camera ready..."), "", null);
                    break;
                case ARManager.ARStatus.Searching:
                    Set(T("scan.search.title", "Find the floor or a table"),
                        T("scan.search.body", "Point your phone at the floor or a big table and move it slowly from side to side. Squares appear where Keza can stand."), null);
                    break;
                case ARManager.ARStatus.TooSmall:
                    Set(T("scan.small.title", "Found it! Keep going..."), SizeLine(), null);
                    break;
                default:
                    Set(T("scan.ready.title", "Big enough!"), T("scan.found"), null);
                    break;
            }
        }

        /// <summary>How big the surface is now and how big it needs to be, in metres.</summary>
        string SizeLine()
        {
            var size = game.AR.SurfaceSize;
            return string.Format(T("scan.small.body", "This surface is {0:0.0} x {1:0.0} m. Keza needs about {2:0.0} x {2:0.0} m: move your phone slowly over more of it until the squares turn green."),
                size.x, size.y, ARManager.MinSurfaceSide);
        }

        void Set(string title, string body, string button)
        {
            hint.text = title;
            status.text = body;
            status.gameObject.SetActive(!string.IsNullOrEmpty(body));
            action.gameObject.SetActive(button != null);
            if (button != null) UIFactory.SetButtonLabel(action, button);
        }

        void OnAction()
        {
            switch (game.AR.Status)
            {
                case ARManager.ARStatus.NeedsCamera:
                case ARManager.ARStatus.CameraDenied:
                    if (game.AR.CanAskForCamera) game.AR.RequestCamera(() => { first = true; Tick(); });
                    else CameraPermission.OpenAppSettings();
                    break;
                case ARManager.ARStatus.Unsupported:
                    game.ReturnToMenu();
                    break;
            }
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }
    }
}
