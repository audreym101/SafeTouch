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
    ///
    /// The NO power meter is the centre of it: a big bar that rises as the
    /// child says NO, with a PASS line (a firm, strong voice), a white mark
    /// that stays at the loudest point of this try, and a gold mark at the
    /// child's record, their loudest NO ever. Reaching the line passes; a
    /// NO louder than the record is cheered as a new record. Only the
    /// number is kept (Progress.loudestNo), never the sound.
    ///
    /// The child always knows whether the app heard them: a microphone badge
    /// glows red while the app listens and swells with their voice ("I hear
    /// you!"); when a NO is heard it turns into a big green tick, the card
    /// says "I heard your NO!", and Keza answers in the room (Heard).
    /// </summary>
    public class VoicePanel
    {
        public const int MaxTries = 3;

        /// <summary>The child's record so far (0 to 100), shown as the gold mark. Set by the lesson screen.</summary>
        public Func<int> Record;
        /// <summary>Keeps a try's NO power if it beats the record; true for a new record. Set by the lesson screen.</summary>
        public Func<int, bool> KeepRecord;
        /// <summary>Plays a sound effect, e.g. the cheer for a new record. Set by the lesson screen.</summary>
        public Action<string> PlaySound;
        /// <summary>A spoken NO was heard (each try): the guide answers in the room. Set by the lesson screen.</summary>
        public Action<NoRating> Heard;

        Image micBadge, micIcon;
        float heardPopStart = -10f;

        RectTransform root;
        Text heading, body, status;
        Button primary, secondary;
        Action primaryAction, secondaryAction;
        Image meterFill;
        RectTransform meterTarget, peakMark, recordMark;
        Text powerNumber, recordLabel, newRecord;
        float meterShown, peak, recordPopStart = -10f;
        int record;
        RectTransform pitchArea;
        Image[] pitchDots;
        readonly System.Collections.Generic.List<float> pitchHistory = new System.Collections.Generic.List<float>();
        GameObject resultRow;
        Image[] stars, checks;
        Text[] checkLabels;
        Text tip;

        VoiceCoach coach;
        Action<NoRating> done;
        NoRating best;
        int tries;
        bool waiting;

        public bool Visible { get { return root != null && root.gameObject.activeSelf; } }

        /// <summary>The card's words, for reading aloud.</summary>
        public string[] Words { get { return new[] { heading.text, body.text }; } }

        public void Build(RectTransform parent)
        {
            var card = UIFactory.Panel(parent, "Voice card", Theme.WithAlpha(Theme.Card, 0.96f), true);
            root = (RectTransform)card.transform;
            UIFactory.Column(root, 20f, 44);
            card.gameObject.AddComponent<LayoutElement>();
            heading = UIFactory.Label(root, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(root, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            // The microphone badge and the words beside it: listening, hearing, heard.
            var listenRow = UIFactory.Row(root, "Listening", 20f, 110f);
            listenRow.childForceExpandWidth = false;
            micBadge = UIFactory.Panel(listenRow.transform, "Mic badge", Theme.Muted, false);
            micBadge.sprite = UIFactory.Circle;
            var mle = micBadge.gameObject.AddComponent<LayoutElement>();
            mle.minWidth = mle.preferredWidth = 104f;
            mle.flexibleWidth = 0f;
            micIcon = UIFactory.Panel(micBadge.transform, "Icon", Color.white, false);
            micIcon.sprite = Icons.Mic;
            micIcon.raycastTarget = false;
            var irt = (RectTransform)micIcon.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(64f, 64f);
            status = UIFactory.Label(listenRow.transform, "", Theme.Title, Theme.Primary, TextAnchor.MiddleLeft, true);
            status.GetComponent<LayoutElement>().flexibleWidth = 1f;
            UIFactory.FitInside(status);
            BuildMeter();
            BuildPitchLine();
            BuildResult();

            var row = UIFactory.Row(root, "Voice buttons", 20f, Theme.TouchTarget);
            primary = UIFactory.Button(row.transform, "", Theme.Unsafe, Color.white, () => { if (primaryAction != null) primaryAction(); });
            secondary = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, () => { if (secondaryAction != null) secondaryAction(); });
            root.gameObject.SetActive(false);
        }

        static readonly Color Gold = Theme.Hex("#E9A912");

        /// <summary>
        /// The NO power meter: "NO power" and a big number above a tall bar. The bar fills as the child speaks:
        /// blue, green past the PASS line (a strong voice for this room), gold past their record.
        /// </summary>
        void BuildMeter()
        {
            var top = UIFactory.Row(root, "NO power", 16f, 96f);
            top.childForceExpandWidth = false;
            var label = UIFactory.Label(top.transform, T("voice.power", "NO power"), Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            label.GetComponent<LayoutElement>().flexibleWidth = 1f;
            UIFactory.FitInside(label);
            powerNumber = UIFactory.Label(top.transform, "0", Theme.Display, Theme.Primary, TextAnchor.MiddleRight, true);
            var nle = powerNumber.GetComponent<LayoutElement>();
            nle.minWidth = nle.preferredWidth = 200f;

            var meter = UIFactory.Panel(root, "Power meter", Theme.Sky, true);
            UIFactory.FixedHeight(meter, 104f);
            meterFill = UIFactory.Panel(meter.transform, "Fill", Theme.Primary, true);
            var fill = (RectTransform)meterFill.transform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            // The PASS line: a dark bar with its word beside it.
            meterTarget = Mark(meter.transform, "Pass line", Theme.Ink, 8f);
            var pass = UIFactory.Label(meterTarget, T("voice.pass", "PASS"), Theme.Small, Theme.Ink, TextAnchor.UpperLeft, true);
            var prt = (RectTransform)pass.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(0f, 1f);
            prt.sizeDelta = new Vector2(160f, 44f);
            prt.anchoredPosition = new Vector2(8f, -4f);

            // The record: a gold bar with a gold dot on top. The loudest point of this try: a white bar.
            recordMark = Mark(meter.transform, "Record", Gold, 12f);
            var dot = UIFactory.Panel(recordMark, "Record dot", Gold, false);
            dot.sprite = UIFactory.Circle;
            var drt = (RectTransform)dot.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 1f);
            drt.sizeDelta = new Vector2(34f, 34f);
            peakMark = Mark(meter.transform, "Loudest this try", Color.white, 10f);
            var edge = peakMark.gameObject.AddComponent<Outline>();
            edge.effectColor = Theme.WithAlpha(Theme.Ink, 0.6f);

            var below = UIFactory.Row(root, "Record row", 16f, 56f);
            below.childForceExpandWidth = false;
            recordLabel = UIFactory.Label(below.transform, "", Theme.Label, Gold, TextAnchor.MiddleLeft, true);
            recordLabel.GetComponent<LayoutElement>().flexibleWidth = 1f;
            newRecord = UIFactory.Label(below.transform, T("voice.record.new", "New record!"), Theme.Body, Gold, TextAnchor.MiddleRight, true);
            var lift = newRecord.gameObject.AddComponent<Outline>();
            lift.effectColor = Theme.WithAlpha(Theme.Ink, 0.35f);
            newRecord.gameObject.SetActive(false);
        }

        /// <summary>A thin upright bar across the meter, placed with SetMark.</summary>
        static RectTransform Mark(Transform meter, string markName, Color color, float width)
        {
            var mark = UIFactory.Panel(meter, markName, color, false);
            mark.raycastTarget = false;
            var rt = (RectTransform)mark.transform;
            rt.sizeDelta = new Vector2(width, 0f);
            SetMark(rt, 0f);
            return rt;
        }

        static void SetMark(RectTransform mark, float at)
        {
            mark.anchorMin = new Vector2(at, 0f);
            mark.anchorMax = new Vector2(at, 1f);
        }

        void UpdateMeter(float level, float strong)
        {
            meterShown = Mathf.Lerp(meterShown, level, 0.5f);
            peak = Mathf.Max(peak, meterShown);
            ((RectTransform)meterFill.transform).anchorMax = new Vector2(meterShown, 1f);
            float best = record / 100f;
            meterFill.color = record > 0 && meterShown > best && meterShown >= strong ? Gold : meterShown >= strong ? Theme.Safe : Theme.Primary;
            SetMark(meterTarget, strong);
            SetMark(peakMark, peak);
            peakMark.gameObject.SetActive(peak > 0.01f);
            powerNumber.text = Mathf.RoundToInt(peak * 100f).ToString();
            powerNumber.color = peak >= strong ? Theme.Safe : Theme.Primary;
        }

        void ShowRecord()
        {
            recordMark.gameObject.SetActive(record > 0);
            SetMark(recordMark, record / 100f);
            recordLabel.text = record > 0 ? string.Format(T("voice.record", "Your record: {0}"), record) : T("voice.record.none", "Say NO loudly to set your record!");
        }

        // ---- Result: stars, the three checks, one tip ----

        void BuildResult()
        {
            var row = UIFactory.Row(root, "Result", 16f, 90f);
            row.childForceExpandWidth = false;
            stars = new Image[3];
            for (int i = 0; i < stars.Length; i++) stars[i] = UIFactory.Dot(row.transform, "Star", Theme.Sun, 64f);
            checks = new Image[3];
            checkLabels = new Text[3];
            for (int i = 0; i < checks.Length; i++)
            {
                checks[i] = UIFactory.Panel(row.transform, "Check", Theme.Safe, true);
                var le = checks[i].gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.minHeight = 72f;
                checkLabels[i] = UIFactory.Label(checks[i].transform, "", Theme.Small, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch((RectTransform)checkLabels[i].transform);
            }
            resultRow = row.gameObject;
            tip = UIFactory.Label(root, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            ShowResultRow(false);
        }

        void ShowResultRow(bool show)
        {
            resultRow.SetActive(show);
            tip.gameObject.SetActive(show);
        }

        void FillResult(NoRating rating)
        {
            for (int i = 0; i < stars.Length; i++) stars[i].color = i < rating.stars ? Theme.Sun : Theme.WithAlpha(Theme.Muted, 0.25f);
            SetCheck(0, T("voice.check.loud", "Loud"), rating.loud);
            SetCheck(1, T("voice.check.steady", "Steady"), rating.steady);
            SetCheck(2, T("voice.check.short", "Short"), rating.brief);
            tip.text = VoiceTips.For(rating.tip);
            resultRow.SetActive(rating.Heard);
            tip.gameObject.SetActive(true);
        }

        void SetCheck(int index, string label, bool passed)
        {
            // Colour plus a word, not a tick glyph: the built-in font has no tick,
            // and colour alone fails for colour-blind children.
            checks[index].color = passed ? Theme.Safe : Theme.Caution;
            checkLabels[index].text = label + (passed ? ": " + T("voice.check.yes", "yes") : ": " + T("voice.check.notyet", "not yet"));
        }

        // ---- Pitch line ----

        const int PitchDots = 48;
        const float LowestSemitone = -8f, HighestSemitone = 12f;   // relative to the talking voice

        /// <summary>
        /// The pitch line: one dot per frame while the child speaks, scrolling
        /// left. The muted line is the child's talking voice; dots above the
        /// orange line are squeaky. A firm NO stays level or slopes down.
        /// </summary>
        void BuildPitchLine()
        {
            var area = UIFactory.Panel(root, "Pitch line", Theme.Sky, true);
            UIFactory.FixedHeight(area, 110f);
            pitchArea = (RectTransform)area.transform;
            GuideLine(0f, Theme.WithAlpha(Theme.Muted, 0.5f), "Talking voice");
            GuideLine(6f, Theme.WithAlpha(Theme.Caution, 0.7f), "Too high");

            pitchDots = new Image[PitchDots];
            for (int i = 0; i < PitchDots; i++)
            {
                var dot = UIFactory.Panel(pitchArea, "Pitch dot", Theme.Primary, false);
                dot.sprite = UIFactory.Circle;
                var rt = (RectTransform)dot.transform;
                rt.sizeDelta = new Vector2(16f, 16f);
                float x = (i + 0.5f) / PitchDots;
                rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
                dot.raycastTarget = false;
                pitchDots[i] = dot;
            }
        }

        void GuideLine(float semitones, Color color, string lineName)
        {
            var line = (RectTransform)UIFactory.Panel(pitchArea, lineName, color, false).transform;
            float y = PitchY(semitones);
            line.anchorMin = new Vector2(0f, y);
            line.anchorMax = new Vector2(1f, y);
            line.sizeDelta = new Vector2(0f, 4f);
        }

        static float PitchY(float semitones)
        {
            return Mathf.InverseLerp(LowestSemitone, HighestSemitone, semitones);
        }

        void OnFrameHeard(VoiceFrame frame)
        {
            if (!waiting || !coach.Hearing) return;
            pitchHistory.Add(frame.Voiced ? VoiceMath.Semitones(frame.pitchHz, coach.Settings.talkingPitchHz) : float.NaN);
            if (pitchHistory.Count > PitchDots) pitchHistory.RemoveAt(0);
            DrawPitch();
        }

        void DrawPitch()
        {
            int start = PitchDots - pitchHistory.Count;
            for (int i = 0; i < PitchDots; i++)
            {
                var dot = pitchDots[i];
                float st = i >= start ? pitchHistory[i - start] : float.NaN;
                dot.enabled = !float.IsNaN(st);
                if (!dot.enabled) continue;
                var rt = (RectTransform)dot.transform;
                float y = Mathf.Clamp01(PitchY(st));
                rt.anchorMin = rt.anchorMax = new Vector2((i + 0.5f) / PitchDots, y);
                dot.color = st > coach.Settings.maxAboveTalkingSemitones ? Theme.Unsafe : Theme.Primary;
            }
        }

        /// <summary>Opens the card and starts the first try. onDone gets the best try, or null if the child tapped.</summary>
        public void Show(string title, string text, VoiceCoach voiceCoach, Action<NoRating> onDone)
        {
            if (coach != null) coach.FrameHeard -= OnFrameHeard;
            coach = voiceCoach;
            coach.FrameHeard += OnFrameHeard;
            done = onDone;
            best = null;
            tries = 0;
            record = Record != null ? Record() : 0;
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
            if (coach != null)
            {
                coach.FrameHeard -= OnFrameHeard;
                coach.Stop();
            }
            if (root != null) root.gameObject.SetActive(false);
        }

        /// <summary>Called every frame while the lesson screen is showing.</summary>
        public void Tick()
        {
            if (!Visible) return;
            // A new record pops in: big, then settling to its size.
            float pop = Time.unscaledTime - recordPopStart;
            if (newRecord.gameObject.activeSelf) newRecord.transform.localScale = Vector3.one * (pop < 0.6f ? 1f + 0.5f * (1f - pop / 0.6f) * Mathf.Abs(Mathf.Cos(pop * 12f)) : 1f);
            // "Heard!": the green tick pops in the same way.
            float heard = Time.unscaledTime - heardPopStart;
            if (!waiting) micBadge.transform.localScale = Vector3.one * (heard < 0.5f ? 1f + 0.45f * (1f - heard / 0.5f) : 1f);
            if (!waiting || coach == null) return;
            UpdateMeter(coach.Level01, coach.StrongLevel01);
            // Listening: the microphone glows red, breathing gently, and swells with the child's voice.
            bool on = coach.Ready || coach.Hearing;
            micBadge.color = on ? Theme.Unsafe : Theme.Muted;
            float breathe = on ? 0.04f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
            micBadge.transform.localScale = Vector3.one * (1f + breathe + 0.35f * coach.Level01);
            if (coach.Hearing) SetStatus(T("voice.hearing.you", "I hear you!"), Theme.Primary);
            else if (coach.Ready) SetStatus(T("voice.now", "Say NO now!"), Theme.Unsafe);
        }

        void StartTry()
        {
            tries++;
            waiting = true;
            meterShown = 0f;
            peak = 0f;
            UpdateMeter(0f, coach.StrongLevel01);
            ShowRecord();
            micIcon.sprite = Icons.Mic;
            micBadge.color = Theme.Muted;
            micBadge.transform.localScale = Vector3.one;
            newRecord.gameObject.SetActive(false);
            pitchHistory.Clear();
            DrawPitch();
            ShowResultRow(false);
            SetStatus(T("voice.ready", "Get ready..."), Theme.Muted);
            SetButtons(null, null, T("voice.tap", "Tap NO instead"), () => Finish(null));
            coach.ListenForNo(OnResult);
        }

        void OnResult(NoRating rating)
        {
            if (!waiting) return;
            waiting = false;
            // The microphone can't be used (permission refused, or no microphone): say so, and offer the NO button.
            if (!rating.Heard && !coach.Available)
            {
                ShowResultRow(false);
                micIcon.sprite = Icons.Mic;
                micBadge.color = Theme.Muted;
                SetStatus(T("voice.nomic", "The microphone is off. Tap NO instead."), Theme.Caution);
                SetButtons(T("voice.tap", "Tap NO instead"), () => Finish(null), null, null);
                return;
            }
            if (best == null || rating.stars > best.stars || (rating.stars == best.stars && rating.Power > best.Power)) best = rating;
            ShowResult(rating);

            // Loud enough to reach the PASS line, with two stars or more: on to the lesson. Quieter: try again, louder
            // (Continue is still there, and after three tries it is the only button: no child is ever stuck).
            bool canRetry = tries < MaxTries;
            string again = T("voice.again", "Try again");
            string next = T("continue", "Continue");
            if (!canRetry) SetButtons(next, () => Finish(best), null, null);
            else if (rating.Good && rating.loud) SetButtons(next, () => Finish(best), again, StartTry);
            else if (rating.Heard) SetButtons(again, StartTry, next, () => Finish(best));
            else SetButtons(again, StartTry, T("voice.tap", "Tap NO instead"), () => Finish(null));
        }

        void ShowResult(NoRating rating)
        {
            // The meter shows this NO's measured power, and the record moves up if it was beaten.
            int power = rating.Power;
            peak = meterShown = power / 100f;
            UpdateMeter(meterShown, coach.StrongLevel01);
            bool newBest = rating.Heard && KeepRecord != null && KeepRecord(power);
            if (newBest) record = power;
            ShowRecord();
            newRecord.gameObject.SetActive(newBest);
            if (newBest)
            {
                recordPopStart = Time.unscaledTime;
                if (PlaySound != null) PlaySound("correct");
            }

            // Heard or not, said plainly: a green tick and "I heard your NO!", or the microphone and "I didn't hear you".
            bool spoken = rating.Heard && rating.tip != NoTip.Whisper;
            micIcon.sprite = spoken ? Icons.Check : Icons.Mic;
            micBadge.color = spoken ? Theme.Safe : Theme.Caution;
            if (spoken)
            {
                heardPopStart = Time.unscaledTime;
                if (PlaySound != null && !newBest) PlaySound("pop");
                if (Heard != null) Heard(rating);
            }
            if (!rating.Heard) SetStatus(T("voice.nothing", "I didn't hear you"), Theme.Caution);
            else if (!spoken) SetStatus(T("voice.heard.whisper", "I heard a whisper. Say it out loud!"), Theme.Caution);
            else if (rating.loud) SetStatus(T("voice.heard.pass", "I heard your NO! You pass!"), Theme.Safe);
            else SetStatus(T("voice.heard.louder", "I heard your NO! Now louder!"), Theme.Caution);
            FillResult(rating);
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
