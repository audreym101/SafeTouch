using SafeTouch.Core;
using UnityEngine;

namespace SafeTouch.UI
{
    /// <summary>
    /// One screen in the app. UIManager builds every screen once at start-up
    /// and shows one menu at a time (plus the feedback panel on top).
    /// </summary>
    public abstract class UIScreen
    {
        protected UIManager ui;
        protected GameManager game;
        public RectTransform Root { get; private set; }

        /// <summary>The title label in the top bar made by StandardColumn.</summary>
        protected UnityEngine.UI.Text TitleText { get; private set; }

        public abstract string Name { get; }

        /// <summary>AR screens are see-through so the camera feed shows.</summary>
        public virtual bool ShowsCamera { get { return false; } }

        /// <summary>
        /// True while the camera picture is really showing behind this screen. A see-through
        /// screen whose camera is not on yet (Keza not placed, camera not allowed) says false,
        /// so the full-screen background fills the notch and tablet margins instead of black.
        /// </summary>
        public virtual bool CameraVisibleNow { get { return ShowsCamera; } }

        public void Build(UIManager owner, GameManager gameManager, RectTransform parent)
        {
            ui = owner;
            game = gameManager;
            var bg = UIFactory.Panel(parent, "Screen " + Name, ShowsCamera ? Theme.Clear : Theme.Sky, false);
            Root = (RectTransform)bg.transform;
            UIFactory.Stretch(Root);
            bg.raycastTarget = !ShowsCamera;
            if (!ShowsCamera) Decor.Background(Root, Name);   // sun, clouds, hills and stars behind the content
            BuildContent(Root);
        }

        protected abstract void BuildContent(RectTransform root);

        /// <summary>Refresh text and state each time the screen appears.</summary>
        public virtual void OnShow() { }

        /// <summary>
        /// A padded column filling the screen, with an exit / back button at
        /// the top so every screen has an easy way out (NFR6).
        /// </summary>
        protected RectTransform StandardColumn(RectTransform root, string title, System.Action onBack)
        {
            var col = UIFactory.Rect(root, "Column");
            UIFactory.Stretch(col);
            UIFactory.Column(col, Theme.Gap, (int)Theme.Gutter);

            if (onBack != null || title != null)
            {
                var bar = UIFactory.Row(col, "Top bar", Theme.Gap, Theme.TouchTarget);
                bar.childForceExpandWidth = false;
                if (onBack != null)
                {
                    var back = UIFactory.Button(bar.transform, "<", Theme.Card, Theme.Ink, onBack);
                    var le = back.GetComponent<UnityEngine.UI.LayoutElement>();
                    le.flexibleWidth = 0f;
                    le.minWidth = Theme.TouchTarget;
                    le.preferredWidth = Theme.TouchTarget;
                }
                TitleText = UIFactory.Label(bar.transform, title ?? "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
                TitleText.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            }
            return col;
        }
    }
}
