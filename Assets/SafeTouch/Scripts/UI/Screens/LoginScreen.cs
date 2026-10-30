using SafeTouch.Core;
using SafeTouch.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The parent logs in each time the app opens: they type the email they gave
    /// when they agreed, and the API sends a fresh 6-digit code (by email, and by
    /// SMS when that is switched on). The code opens the app. The reply is the same
    /// whether or not the email matches, so the screen says "if it matches".
    /// Needs internet: offline, it says so and waits.
    /// </summary>
    public class LoginScreen : UIScreen
    {
        Text heading, body, status;
        InputField email, code;
        Button send, login, again, other;
        bool codeStep, waiting;

        public override string Name { get { return "login"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            heading = UIFactory.Label(col, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            email = UIFactory.Input(col, "", InputField.ContentType.EmailAddress, TouchScreenKeyboardType.EmailAddress, 254);
            code = UIFactory.Input(col, "000000", InputField.ContentType.IntegerNumber, TouchScreenKeyboardType.NumberPad, 6);
            code.textComponent.fontSize = Theme.Display;
            code.textComponent.alignment = TextAnchor.MiddleCenter;
            ((Text)code.placeholder).alignment = TextAnchor.MiddleCenter;
            UIFactory.FixedHeight(code, Theme.TouchTarget * 1.3f);
            status = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, true);
            send = UIFactory.Button(col, "", Theme.Primary, Color.white, SendCode);
            login = UIFactory.Button(col, "", Theme.Primary, Color.white, LogIn);
            var row = UIFactory.Row(col, "More", Theme.Gap, Theme.TouchTarget);
            again = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, SendCode);
            other = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, () => { codeStep = false; status.text = ""; OnShow(); });
            UIFactory.Spacer(col, 1f);
        }

        public override void OnShow()
        {
            var service = game.Consent;
            if (service == null || !service.NeedsLogin) { game.ContinueFromSplash(); return; }

            TitleText.text = L("Log in", "Kwinjira");
            heading.text = L("Welcome back", "Murakaza neza");
            UIFactory.SetButtonLabel(send, L("Send me a code", "Mpa kode"));
            UIFactory.SetButtonLabel(login, L("Log in", "Injira"));
            UIFactory.SetButtonLabel(again, L("Send the code again", "Ohereza kode nanone"));
            UIFactory.SetButtonLabel(other, L("Use another email", "Indi meyili"));
            ((Text)email.placeholder).text = L("Your email", "Imeyili yanyu");

            email.gameObject.SetActive(!codeStep);
            send.gameObject.SetActive(!codeStep);
            code.gameObject.SetActive(codeStep);
            login.gameObject.SetActive(codeStep);
            again.transform.parent.gameObject.SetActive(codeStep);

            if (!codeStep)
            {
                body.text = L("Type the email you gave when you agreed to the terms. We will send you a code to open SafeTouch.",
                              "Andika imeyili mwatanze igihe mwemeraga amabwiriza. Tuzabohereza kode yo gufungura SafeTouch.");
            }
            else
            {
                string shown = string.IsNullOrEmpty(service.Record.emailMasked) ? "" : service.Record.emailMasked;
                body.text = string.Format(L("If that email matches, we sent a 6-digit code to {0}, and by text message if it is switched on. Type it here. It works for 10 minutes.",
                                            "Niba iyo meyili ihuye, twohereje kode y'imibare 6 kuri {0}, no kuri telefoni niba byemewe. Yandike hano. Imara iminota 10."), shown);
            }
        }

        void SendCode()
        {
            var service = game.Consent;
            var typed = email.text.Trim();
            if (typed.Length == 0 || !typed.Contains("@")) { Say(L("Please type your email.", "Andika imeyili yanyu."), Theme.Caution); return; }
            if (waiting) return;
            waiting = true;
            Say(L("Sending...", "Birohereza..."), Theme.Muted);
            service.RequestLoginCode(typed, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Sent:
                        codeStep = true; code.text = ""; status.text = ""; OnShow();
                        Say(L("Please check your email, including the spam folder.", "Murebe imeyili yanyu, harimo n'aho spam iba."), Theme.Safe);
                        break;
                    case ConsentService.Result.Invalid: Say(L("Please type a real email.", "Andika imeyili nyayo."), Theme.Caution); break;
                    case ConsentService.Result.Wait:
                    case ConsentService.Result.TooManyTries: Say(L("Please wait a minute before asking again.", "Mutegereze umunota umwe mbere yo gusaba indi."), Theme.Caution); break;
                    case ConsentService.Result.Offline: Say(NoInternet(), Theme.Caution); break;
                    default: Say(L("The code could not be sent. Please try again later.", "Kode ntishoboye koherezwa. Mwongere mugerageze nyuma."), Theme.Caution); break;
                }
            });
        }

        void LogIn()
        {
            var digits = code.text.Trim();
            if (digits.Length != 6) { Say(L("The code has 6 digits.", "Kode ifite imibare 6."), Theme.Caution); return; }
            if (waiting) return;
            waiting = true;
            Say(L("Checking...", "Biraragenzura..."), Theme.Muted);
            game.Consent.VerifyLogin(digits, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Verified:
                        codeStep = false; code.text = ""; email.text = ""; status.text = "";
                        game.ContinueFromSplash();
                        break;
                    case ConsentService.Result.WrongCode: Say(L("That code is not right. Please check and try again.", "Iyo kode si yo. Mwongere mugerageze."), Theme.Unsafe); break;
                    case ConsentService.Result.Expired: Say(L("This code has expired. Tap \"Send the code again\".", "Iyi kode yararangiye. Mukande \"Ohereza kode nanone\"."), Theme.Unsafe); break;
                    case ConsentService.Result.TooManyTries: Say(L("Too many tries. Tap \"Send the code again\" for a new one.", "Mwagerageje inshuro nyinshi. Mukande \"Ohereza kode nanone\"."), Theme.Unsafe); break;
                    case ConsentService.Result.Offline: Say(NoInternet(), Theme.Caution); break;
                    default: Say(L("Something went wrong. Please try again in a moment.", "Hari ikitagenze neza. Mwongere mugerageze mukanya."), Theme.Caution); break;
                }
            });
        }

        void Say(string text, Color colour)
        {
            status.text = text;
            status.color = colour;
        }

        static string NoInternet()
        {
            return L("No internet. Please connect and try again.", "Nta murandasi. Mwongere mugerageze mumaze kuyihuza.");
        }

        static string L(string english, string kinyarwanda)
        {
            return Localization.Language == "rw" ? kinyarwanda : english;
        }
    }
}
