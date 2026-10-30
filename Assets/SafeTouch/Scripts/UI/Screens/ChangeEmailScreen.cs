using SafeTouch.Core;
using SafeTouch.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// Changing the parent's email after the consent is confirmed (Grown-ups > Settings):
    ///   1. the parent types the new email and the API sends a 6-digit code to that NEW email;
    ///   2. typing the code makes it the parent's email: login and age-check codes go there from then on,
    ///      and the old email gets a short notice. Until then nothing changes.
    /// Before the consent is confirmed, "Change my details" on the verify screen corrects the email instead.
    /// </summary>
    public class ChangeEmailScreen : UIScreen
    {
        Text heading, body, status;
        InputField email, code;
        Button send, confirm, again, other;
        bool codeStep, waiting;
        string newEmail;

        public override string Name { get { return "email"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => { Reset(); ui.ShowMenu("parent"); });
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
            confirm = UIFactory.Button(col, "", Theme.Primary, Color.white, Confirm);
            var row = UIFactory.Row(col, "More", Theme.Gap, Theme.TouchTarget);
            again = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, SendCode);
            other = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, () => { codeStep = false; status.text = ""; OnShow(); });
            UIFactory.Spacer(col, 1f);
        }

        public override void OnShow()
        {
            var service = game.Consent;
            if (service == null || service.Record == null || !service.Record.verified) { ui.ShowMenu("parent"); return; }

            TitleText.text = L("Change my email", "Guhindura imeyili");
            heading.text = L("Your new email", "Imeyili yanyu nshya");
            UIFactory.SetButtonLabel(send, L("Send a code to the new email", "Ohereza kode kuri imeyili nshya"));
            UIFactory.SetButtonLabel(confirm, L("Confirm", "Emeza"));
            UIFactory.SetButtonLabel(again, L("Send the code again", "Ohereza kode nanone"));
            UIFactory.SetButtonLabel(other, L("Use another email", "Indi meyili"));
            ((Text)email.placeholder).text = L("New email", "Imeyili nshya");

            email.gameObject.SetActive(!codeStep);
            send.gameObject.SetActive(!codeStep);
            code.gameObject.SetActive(codeStep);
            confirm.gameObject.SetActive(codeStep);
            again.transform.parent.gameObject.SetActive(codeStep);

            string current = string.IsNullOrEmpty(service.Record.emailMasked) ? service.Record.email : service.Record.emailMasked;
            body.text = !codeStep
                ? string.Format(L("Your codes go to {0} now. Type the email you want to use instead and we will send a 6-digit code to it, to check it is yours.",
                                  "Ubu kode zanyu zijya kuri {0}. Andika imeyili mushaka gukoresha, tuyoherezeho kode y'imibare 6 kugira ngo twemeze ko ari iyanyu."), current)
                : string.Format(L("We sent a 6-digit code to {0}. Type it here. It works for 30 minutes. Until then, your codes still go to the old email.",
                                  "Twohereje kode y'imibare 6 kuri {0}. Yandike hano. Imara iminota 30. Kugeza icyo gihe, kode zanyu zikomeza kujya kuri imeyili ya mbere."),
                                newEmail);
        }

        void SendCode()
        {
            var typed = codeStep ? newEmail : email.text.Trim();
            if (string.IsNullOrEmpty(typed) || !typed.Contains("@") || !typed.Contains(".")) { Say(L("Please type your new email.", "Andika imeyili yanyu nshya."), Theme.Caution); return; }
            if (waiting) return;
            waiting = true;
            Say(L("Sending...", "Birohereza..."), Theme.Muted);
            game.Consent.RequestEmailChange(typed, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Sent:
                        newEmail = typed; codeStep = true; code.text = ""; OnShow();
                        AppSession.CodeSent();   // reading the code means leaving for the mail app: keep this screen when they come back
                        Say(L("Please check the new email, including the spam folder.", "Murebe muri imeyili nshya, harimo n'aho spam iba."), Theme.Safe);
                        break;
                    case ConsentService.Result.Invalid: Say(L("Please type a real email that is different from the one you use now.", "Andika imeyili nyayo itandukanye n'iyo mukoresha ubu."), Theme.Caution); break;
                    case ConsentService.Result.NeedsLogin: Reset(); game.ContinueFromSplash(); break;   // logged in too long ago: log in, then try again
                    case ConsentService.Result.Wait:
                    case ConsentService.Result.TooManyTries: Say(L("Please wait a minute before asking again.", "Mutegereze umunota umwe mbere yo gusaba indi."), Theme.Caution); break;
                    case ConsentService.Result.Offline: Say(NoInternet(), Theme.Caution); break;
                    default: Say(L("The code could not be sent. Please try again later.", "Kode ntishoboye koherezwa. Mwongere mugerageze nyuma."), Theme.Caution); break;
                }
            });
        }

        void Confirm()
        {
            var digits = code.text.Trim();
            if (digits.Length != 6) { Say(L("The code has 6 digits.", "Kode ifite imibare 6."), Theme.Caution); return; }
            if (waiting) return;
            waiting = true;
            Say(L("Checking...", "Biraragenzura..."), Theme.Muted);
            game.Consent.VerifyEmailChange(digits, newEmail, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Verified:
                        Reset();
                        ui.ShowMenu("parent");
                        break;
                    case ConsentService.Result.WrongCode: Say(L("That code is not right. Please check and try again.", "Iyo kode si yo. Mwongere mugerageze."), Theme.Unsafe); break;
                    case ConsentService.Result.Expired: Say(L("This code has expired. Tap \"Send the code again\".", "Iyi kode yararangiye. Mukande \"Ohereza kode nanone\"."), Theme.Unsafe); break;
                    case ConsentService.Result.TooManyTries: Say(L("Too many tries. Tap \"Send the code again\" for a new one.", "Mwagerageje inshuro nyinshi. Mukande \"Ohereza kode nanone\"."), Theme.Unsafe); break;
                    case ConsentService.Result.Offline: Say(NoInternet(), Theme.Caution); break;
                    default: Say(L("Something went wrong. Please try again in a moment.", "Hari ikitagenze neza. Mwongere mugerageze mukanya."), Theme.Caution); break;
                }
            });
        }

        void Reset()
        {
            AppSession.CodeDone();
            codeStep = false; newEmail = null; email.text = ""; code.text = ""; status.text = "";
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
