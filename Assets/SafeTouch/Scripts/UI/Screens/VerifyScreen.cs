using SafeTouch.Core;
using SafeTouch.Data;
using SafeTouch.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Confirming the parent's consent with the 6-digit code from the email.
    /// While the 7-day grace period lasts the parent can choose "Verify later"
    /// and let the child start; after that the code is needed to continue.
    /// Offline, it explains that the email goes out once the phone has internet.
    /// </summary>
    public class VerifyScreen : UIScreen
    {
        Text heading, body, status;
        InputField code;
        Button confirm, resend, change, later;
        bool waiting;

        public override string Name { get { return "verify"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            heading = UIFactory.Label(col, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            code = UIFactory.Input(col, "000000", InputField.ContentType.IntegerNumber, TouchScreenKeyboardType.NumberPad, 6);
            code.textComponent.fontSize = Theme.Display;
            code.textComponent.alignment = TextAnchor.MiddleCenter;
            ((Text)code.placeholder).alignment = TextAnchor.MiddleCenter;
            UIFactory.FixedHeight(code, Theme.TouchTarget * 1.3f);
            status = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, true);
            confirm = UIFactory.Button(col, "", Theme.Primary, Color.white, Confirm);
            var row = UIFactory.Row(col, "More", Theme.Gap, Theme.TouchTarget);
            resend = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, Resend);
            change = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, () => ui.ShowMenu("consent"));
            UIFactory.Spacer(col, 1f);
            later = UIFactory.Button(col, "", Theme.Sun, Theme.Ink, () => game.ContinueToLessons());
            if (game.Consent != null) game.Consent.Changed += () => { if (Root.gameObject.activeInHierarchy) OnShow(); };
        }

        public override void OnShow()
        {
            var service = game.Consent;
            var record = service != null ? service.Record : null;
            if (record == null) { ui.ShowMenu("consent"); return; }

            TitleText.text = T("verify.title", "Confirm by email");
            UIFactory.SetButtonLabel(confirm, T("verify.confirm", "Confirm"));
            UIFactory.SetButtonLabel(resend, T("verify.resend", "Send the email again"));
            UIFactory.SetButtonLabel(change, T("verify.change", "Change my details"));

            if (record.verified)
            {
                heading.text = T("verify.done.title", "Thank you, consent confirmed");
                body.text = T("verify.done.body", "Your consent is confirmed. You can withdraw it at any time in Grown-ups > Settings.");
                SetMode(false);
                UIFactory.SetButtonLabel(later, T("verify.continue", "Continue"));
                later.gameObject.SetActive(true);
                return;
            }

            SetMode(true);
            // A day or more after agreeing, this is a reminder rather than the "one last step" thank-you.
            bool reminder = record.sentToServer && (record.MustVerifyNow || record.DaysLeft < ConsentRecord.GraceDays);
            string where = string.IsNullOrEmpty(record.emailMasked) ? record.email : record.emailMasked;
            heading.text = reminder ? L("Reminder: please confirm your consent", "Kwibutsa: mwemeze uruhushya rwanyu")
                                    : T("verify.heading", "Thank you! One last step");
            if (reminder)
            {
                string left = record.MustVerifyNow
                    ? L("The 7 days are over, so SafeTouch needs the code before your child can continue.",
                        "Iminsi 7 irashize, bityo SafeTouch ikeneye kode mbere y'uko umwana wanyu akomeza.")
                    : string.Format(L("You have {0} day(s) left to confirm it.", "Mufite iminsi {0} yo kuyemeza."), record.DaysLeft);
                body.text = string.Format(L("You agreed to the terms, but have not confirmed the 6-digit code yet. Type the code we emailed to {0}. {1} Tap \"Send the email again\" if you can't find it.",
                                            "Mwemeye amabwiriza, ariko ntimwemeza kode y'imibare 6. Yandike kode twohereje kuri {0}. {1} Mukande \"Ohereza imeyili nanone\" niba mutayibonye."), where, left);
            }
            else
            {
                body.text = record.sentToServer
                    ? string.Format(T("verify.sent", "We emailed a 6-digit code and a copy of your consent to {0}. Type the code here."), where)
                    : T("verify.offline", "Your consent is saved on this phone. As soon as the phone has internet, we will email you a 6-digit code and a copy of your consent.");
            }
            if (!waiting) status.text = "";

            bool canWait = !record.MustVerifyNow;
            later.gameObject.SetActive(canWait);
            UIFactory.SetButtonLabel(later, string.Format(T("verify.later", "Verify later ({0} days left) and start"), record.DaysLeft));
            if (!canWait) status.text = T("verify.required", "7 days have passed: please confirm the code so your child can keep using SafeTouch.");
        }

        void SetMode(bool asking)
        {
            code.gameObject.SetActive(asking);
            confirm.gameObject.SetActive(asking);
            resend.transform.parent.gameObject.SetActive(asking);
        }

        void Confirm()
        {
            var digits = code.text.Trim();
            if (digits.Length != 6) { Say(T("verify.six", "The code has 6 digits."), Theme.Caution); return; }
            waiting = true;
            Say(T("verify.checking", "Checking..."), Theme.Muted);
            game.Consent.Verify(digits, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Verified: code.text = ""; OnShow(); Say(T("verify.ok", "Confirmed. Thank you!"), Theme.Safe); break;
                    case ConsentService.Result.WrongCode: Say(T("verify.wrong", "That code is not right. Please check the email and try again."), Theme.Unsafe); break;
                    case ConsentService.Result.Expired: Say(T("verify.expired", "This code has expired. Tap \"Send the email again\" for a new one."), Theme.Unsafe); break;
                    case ConsentService.Result.TooManyTries: Say(T("verify.toomany", "Too many tries. Tap \"Send the email again\" for a new code."), Theme.Unsafe); break;
                    case ConsentService.Result.Offline: Say(T("verify.nointernet", "No internet. Please connect and try again."), Theme.Caution); break;
                    default: Say(T("verify.error", "Something went wrong. Please try again in a moment."), Theme.Caution); break;
                }
            });
        }

        void Resend()
        {
            waiting = true;
            Say(T("verify.sending", "Sending..."), Theme.Muted);
            game.Consent.Resend(result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Sent: Say(T("verify.resent", "Sent. Please check your email, including the spam folder."), Theme.Safe); break;
                    case ConsentService.Result.Wait:
                    case ConsentService.Result.TooManyTries: Say(T("verify.wait", "Please wait a minute before asking again."), Theme.Caution); break;
                    case ConsentService.Result.Offline: Say(T("verify.nointernet", "No internet. Please connect and try again."), Theme.Caution); break;
                    default: Say(T("verify.emailfailed", "The email could not be sent. Please try again later."), Theme.Caution); break;
                }
            });
        }

        void Say(string text, Color colour)
        {
            status.text = text;
            status.color = colour;
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }

        /// <summary>English or Kinyarwanda text written in place, for the reminder.</summary>
        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
