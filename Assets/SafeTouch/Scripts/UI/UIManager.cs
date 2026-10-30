using System;
using System.Collections;
using System.Collections.Generic;
using SafeTouch.Core;
using SafeTouch.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    public enum FeedbackTone { Neutral, Correct, TryAgain, Badge }

    /// <summary>A choice button on the lesson prompt card.</summary>
    public class PromptOption
    {
        public string label;
        public Color color;
        public Action onClick;

        public PromptOption(string label, Color color, Action onClick)
        {
            this.label = label;
            this.color = color;
            this.onClick = onClick;
        }
    }

    /// <summary>
    /// Builds and switches the app's screens, the lesson HUD and the feedback
    /// panel. Navigation map: docs/designs/navigation-map.png.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] GameObject[] menus = new GameObject[0];
        [SerializeField] Button[] buttons = new Button[0];
        [SerializeField] GameObject feedbackPanel;
        [SerializeField] Canvas canvas;

        readonly List<UIScreen> screens = new List<UIScreen>();
        UIScreen active;
        GameManager game;

        Text feedbackTitle, feedbackBody;
        Button feedbackButton;
        Action feedbackContinue;
        GameObject toast;
        Image backdrop;
        Text toastText;
        Coroutine toastRoutine;

        public string ActiveMenu { get { return active != null ? active.Name : ""; } }
        public LessonScreen Lesson { get; private set; }
        public AdultsScreen Adults { get; private set; }
        public MemoryGameScreen Memory { get; private set; }
        public CertificateScreen Certificate { get; private set; }
        public PlacesScreen Places { get; private set; }

        public void Initialise(GameManager gameManager)
        {
            game = gameManager;
            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
            if (canvas.GetComponent<ResponsiveCanvas>() == null) canvas.gameObject.AddComponent<ResponsiveCanvas>();

            // Fills the whole screen behind the safe area: notches, rounded corners and
            // tablet margins show the app's colour instead of black. Hidden on AR screens.
            backdrop = UIFactory.Panel(canvas.transform, "Backdrop", Theme.Sky, false);
            UIFactory.Stretch((RectTransform)backdrop.transform);
            backdrop.raycastTarget = false;

            var safeArea = UIFactory.Rect(canvas.transform, "Safe area");
            UIFactory.Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeArea>();

            Lesson = new LessonScreen();
            Adults = new AdultsScreen();
            Memory = new MemoryGameScreen();
            Certificate = new CertificateScreen();
            Places = new PlacesScreen();
            screens.Add(new SplashScreen());
            screens.Add(new ConsentScreen());
            screens.Add(new VerifyScreen());
            screens.Add(new LoginScreen());
            screens.Add(new AgeGateScreen());
            screens.Add(new ChangeEmailScreen());
            screens.Add(new AgeScreen());
            screens.Add(new HomeScreen());
            screens.Add(new ScanScreen());
            screens.Add(Lesson);
            screens.Add(Adults);
            screens.Add(Memory);
            screens.Add(Certificate);
            screens.Add(Places);
            screens.Add(new BadgesScreen());
            screens.Add(new LookScreen());
            screens.Add(new ParentGateScreen());
            screens.Add(new ParentScreen());

            menus = new GameObject[screens.Count];
            for (int i = 0; i < screens.Count; i++)
            {
                screens[i].Build(this, game, safeArea);
                menus[i] = screens[i].Root.gameObject;
                menus[i].SetActive(false);
            }

            BuildFeedbackPanel(safeArea);
            BuildToast(safeArea);
            buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons) b.onClick.AddListener(() => game.Audio.PlaySFX("tap"));
            Localization.Changed += RefreshActive;
        }

        void OnDestroy()
        {
            Localization.Changed -= RefreshActive;
        }

        void Update()
        {
            var tickable = active as ITickable;
            if (tickable != null) tickable.Tick();
            UpdateBackdrop();   // the scan screen's camera turns on while it is showing
        }

        void UpdateBackdrop()
        {
            if (backdrop != null) backdrop.enabled = active == null || !active.CameraVisibleNow;
        }

        /// <summary>Shows one screen by name and hides the others.</summary>
        public void ShowMenu(string name)
        {
            foreach (var s in screens)
            {
                bool show = s.Name == name;
                s.Root.gameObject.SetActive(show);
                if (show) active = s;
            }
            UpdateBackdrop();
            HideFeedback();
            UpdateHUD();
        }

        public void ShowFeedback(string msg)
        {
            ShowFeedback("", msg, FeedbackTone.Neutral, null);
        }

        public void ShowFeedback(string title, string msg, FeedbackTone tone, Action onContinue)
        {
            feedbackPanel.SetActive(true);
            feedbackPanel.transform.SetAsLastSibling();
            feedbackTitle.text = title;
            feedbackTitle.color = ToneColor(tone);
            feedbackTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
            feedbackBody.text = msg;
            feedbackContinue = onContinue;
            UIFactory.SetButtonLabel(feedbackButton, Localization.T("continue"));
            game.Audio.PlaySFX(tone == FeedbackTone.Correct ? "correct" : tone == FeedbackTone.TryAgain ? "tryagain" : tone == FeedbackTone.Badge ? "badge" : "tap");
        }

        public void HideFeedback()
        {
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
        }

        /// <summary>Short banner at the top, used when a badge unlocks.</summary>
        public void ShowToast(string msg)
        {
            toastText.text = msg;
            toast.SetActive(true);
            toast.transform.SetAsLastSibling();
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideToastLater());
        }

        /// <summary>Refreshes stars, lesson states and the lesson header.</summary>
        public void UpdateHUD()
        {
            if (active != null) active.OnShow();
        }

        public static Color ToneColor(FeedbackTone tone)
        {
            switch (tone)
            {
                case FeedbackTone.Correct: return Theme.Safe;
                case FeedbackTone.TryAgain: return Theme.Caution;
                case FeedbackTone.Badge: return Theme.Sun;
                default: return Theme.Ink;
            }
        }

        void RefreshActive()
        {
            if (active != null) active.OnShow();
        }

        void BuildFeedbackPanel(RectTransform parent)
        {
            var scrim = UIFactory.Panel(parent, "Feedback panel", Theme.Scrim, false);
            UIFactory.Stretch((RectTransform)scrim.transform);
            feedbackPanel = scrim.gameObject;

            var card = UIFactory.Panel(scrim.transform, "Card", Theme.Card, true);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(Theme.Gutter * 0.5f, Theme.Gutter * 0.5f);
            rt.offsetMax = new Vector2(-Theme.Gutter * 0.5f, 0f);
            UIFactory.Column(rt, Theme.Gap, (int)Theme.Gutter);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            feedbackTitle = UIFactory.Label(card.transform, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            feedbackBody = UIFactory.Label(card.transform, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            feedbackButton = UIFactory.Button(card.transform, "Continue", Theme.Primary, Color.white, () =>
            {
                HideFeedback();
                var next = feedbackContinue;
                feedbackContinue = null;
                if (next != null) next();
            });
            feedbackPanel.SetActive(false);
        }

        void BuildToast(RectTransform parent)
        {
            var img = UIFactory.Panel(parent, "Toast", Theme.Ink, true);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(Theme.Gutter, -Theme.TouchTarget - Theme.Gutter);
            rt.offsetMax = new Vector2(-Theme.Gutter, -Theme.Gutter);
            img.raycastTarget = false;
            toastText = UIFactory.Label(img.transform, "", Theme.Label, Theme.Sun, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch((RectTransform)toastText.transform);
            toast = img.gameObject;
            toast.SetActive(false);
        }

        IEnumerator HideToastLater()
        {
            yield return new WaitForSeconds(3f);
            toast.SetActive(false);
        }
    }

    /// <summary>Screens that need a per-frame update (the scan screen).</summary>
    public interface ITickable
    {
        void Tick();
    }

    /// <summary>
    /// Keeps the UI inside the phone's safe area (notches such as the
    /// Samsung A70's, status bars, rounded corners) and, on tablets, centres
    /// it in a column no wider than ResponsiveCanvas.MaxContentWidth.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2 appliedScreen;

        void Update()
        {
            var area = Screen.safeArea;
            var screen = new Vector2(Screen.width, Screen.height);
            if (area == applied && screen == appliedScreen) return;
            Apply(area, screen);
        }

        /// <summary>Fits the UI to a safe area of a screen (both in pixels). Also used to preview other devices.</summary>
        public void Apply(Rect area, Vector2 screen)
        {
            if (screen.x <= 0f || screen.y <= 0f) return;
            applied = area;
            appliedScreen = screen;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            rt.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Tablets: keep a phone-shaped column in the middle rather than stretching every button.
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            float width = parent.rect.width * (rt.anchorMax.x - rt.anchorMin.x);
            float extra = width - ResponsiveCanvas.MaxContentWidth;
            if (extra <= 0f) return;
            rt.offsetMin = new Vector2(extra * 0.5f, 0f);
            rt.offsetMax = new Vector2(-extra * 0.5f, 0f);
        }
    }
}
