using System;
using SafeTouch.Core;
using SafeTouch.Voice;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// The "Say NO!" card in the lesson HUD. It listens for the child's
    /// spoken NO, shows how it sounded and lets them try again (up to three
    /// times) or tap NO instead. Tapping is always allowed: a child who
    /// cannot or does not want to speak still finishes the lesson.
    /// </summary>
    public class VoicePanel
    {
        public const int MaxTries = 3;

        RectTransform root;
        Text heading, body, status;
        Button primary, secondary;
        Action primaryAction, secondaryAction;
        Image meterFill;
        RectTransform meterTarget;
        float meterShown;

        VoiceCoach coach;
        Action<NoRating> done;
        NoRating best;
        int tries;
        bool waiting;

        public bool Visible { get { return root != null && root.gameObject.activeSelf; } }

        public void Build(RectTransform parent)
        {
            var card = UIFactory.Panel(parent, "Voice card", Theme.WithAlpha(Theme.Card, 0.96f), true);
            root = (RectTransform)card.transform;
            UIFactory.Column(root, 20f, 44);
            card.gameObject.AddComponent<LayoutElement>();
            heading = UIFactory.Label(root, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(root, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            status = UIFactory.Label(root, "", Theme.Title, Theme.Primary, TextAnchor.MiddleCenter, true);
            BuildMeter();

            var row = UIFactory.Row(root, "Voice buttons", 20f, Theme.TouchTarget);
            primary = UIFactory.Button(row.transform, "", Theme.Unsafe, Color.white, () => { if (primaryAction != null) primaryAction(); });
            secondary = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, () => { if (secondaryAction != null) secondaryAction(); });
            root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Loudness meter: it fills as the child speaks and turns green past
        /// the dark line, which marks a strong voice for this room.
        /// </summary>
        void BuildMeter()
        {
            var meter = UIFactory.Panel(root, "Loudness meter", Theme.Sky, true);
            UIFactory.FixedHeight(meter, 56f);
            meterFill = UIFactory.Panel(meter.transform, "Fill", Theme.Primary, true);
            var fill = (RectTransform)meterFill.transform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            meterTarget = (RectTransform)UIFactory.Panel(meter.transform, "Strong voice line", Theme.Ink, false).transform;
            meterTarget.sizeDelta = new Vector2(8f, 0f);
            meterTarget.anchorMin = new Vector2(0.66f, 0f);
            meterTarget.anchorMax = new Vector2(0.66f, 1f);
        }

        void UpdateMeter(float level, float strong)
        {
            meterShown = Mathf.Lerp(meterShown, level, 0.5f);
            ((RectTransform)meterFill.transform).anchorMax = new Vector2(meterShown, 1f);
            meterFill.color = meterShown >= strong ? Theme.Safe : Theme.Primary;
            meterTarget.anchorMin = new Vector2(strong, 0f);
            meterTarget.anchorMax = new Vector2(strong, 1f);
        }

        /// <summary>Opens the card and starts the first try. onDone gets the best try, or null if the child tapped.</summary>
        public void Show(string title, string text, VoiceCoach voiceCoach, Action<NoRating> onDone)
        {
            coach = voiceCoach;
            done = onDone;
            best = null;
            tries = 0;
            heading.text = title;
            heading.gameObject.SetActive(!string.IsNullOrEmpty(title));
            body.text = text;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            StartTry();
        }

        /// <summary>Closes the card and the microphone without reporting a result.</summary>
        public void Hide()
        {
            waiting = false;
            if (coach != null) coach.Stop();
            if (root != null) root.gameObject.SetActive(false);
        }

        /// <summary>Called every frame while the lesson screen is showing.</summary>
        public void Tick()
        {
            if (!Visible || !waiting || coach == null) return;
            UpdateMeter(coach.Level01, coach.StrongLevel01);
            if (coach.Hearing) SetStatus(T("voice.hearing", "Listening..."), Theme.Primary);
            else if (coach.Ready) SetStatus(T("voice.now", "Say NO now!"), Theme.Unsafe);
        }

        void StartTry()
        {
            tries++;
            waiting = true;
            meterShown = 0f;
            UpdateMeter(0f, coach.StrongLevel01);
            SetStatus(T("voice.ready", "Get ready..."), Theme.Muted);
            SetButtons(null, null, T("voice.tap", "Tap NO instead"), () => Finish(null));
            coach.ListenForNo(OnResult);
        }

        void OnResult(NoRating rating)
        {
            if (!waiting) return;
            waiting = false;
            if (best == null || rating.stars > best.stars) best = rating;
            ShowResult(rating);

            bool canRetry = tries < MaxTries;
            string again = T("voice.again", "Try again");
            string next = T("continue", "Continue");
            if (!canRetry) SetButtons(next, () => Finish(best), null, null);
            else if (rating.Good) SetButtons(next, () => Finish(best), again, StartTry);
            else if (rating.Heard) SetButtons(again, StartTry, next, () => Finish(best));
            else SetButtons(again, StartTry, T("voice.tap", "Tap NO instead"), () => Finish(null));
        }

        void ShowResult(NoRating rating)
        {
            SetStatus(rating.Heard ? string.Format(T("voice.stars", "{0} of 3 stars"), rating.stars) : T("voice.nothing", "I didn't hear you"),
                rating.Good ? Theme.Safe : Theme.Caution);
        }

        void Finish(NoRating rating)
        {
            var callback = done;
            done = null;
            Hide();
            if (callback != null) callback(rating);
        }

        void SetStatus(string text, Color color)
        {
            status.text = text;
            status.color = color;
        }

        void SetButtons(string first, Action firstAction, string second, Action secondAction)
        {
            primaryAction = firstAction;
            secondaryAction = secondAction;
            primary.gameObject.SetActive(first != null);
            secondary.gameObject.SetActive(second != null);
            if (first != null) UIFactory.SetButtonLabel(primary, first);
            if (second != null) UIFactory.SetButtonLabel(secondary, second);
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }
    }
}
