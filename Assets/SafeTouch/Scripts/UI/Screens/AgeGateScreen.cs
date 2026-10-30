using System;
using SafeTouch.Core;
using SafeTouch.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The door to the Guardian lessons (ages 9-11). Those lessons talk openly about online strangers and
    /// private parts of the body, so a grown-up proves the child's age first:
    ///   1. they type the child's date of birth (day, month, year). The date is only used to work out the
    ///      age: it is not saved on the phone and never sent anywhere;
    ///   2. if the age is 9 to 11, the API emails the parent (the email on the consent) a 6-digit code;
    ///   3. typing the code opens the Guardian lessons.
    /// Younger children are pointed to the Explorer lessons.
    /// </summary>
    public class AgeGateScreen : UIScreen
    {
        Text heading, body, status;
        InputField day, month, year, code;
        Button check, confirm, resend, explorer;
        bool codeStep, waiting;
        int askedAge;

        public override string Name { get { return "agegate"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu(string.IsNullOrEmpty(game.AgeGateReturn) ? "home" : game.AgeGateReturn));
            var mascot = UIFactory.Row(col, "Mascot", 0f, 250f);
            mascot.childAlignment = TextAnchor.LowerCenter;
            mascot.childForceExpandWidth = false;
            Decor.Keza(mascot.transform, 250f);

            heading = UIFactory.Label(col, "", Theme.Title, Theme.Ink, TextAnchor.UpperLeft, true);
            body = UIFactory.Label(col, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);

            var date = UIFactory.Row(col, "Date", Theme.Gap, Theme.TouchTarget * 1.2f);
            day = DateField(date.transform, 2);
            month = DateField(date.transform, 2);
            year = DateField(date.transform, 4);
            year.GetComponent<LayoutElement>().flexibleWidth = 1.6f;

            code = UIFactory.Input(col, "000000", InputField.ContentType.IntegerNumber, TouchScreenKeyboardType.NumberPad, 6);
            code.textComponent.fontSize = Theme.Display;
            code.textComponent.alignment = TextAnchor.MiddleCenter;
            ((Text)code.placeholder).alignment = TextAnchor.MiddleCenter;
            UIFactory.FixedHeight(code, Theme.TouchTarget * 1.3f);

            status = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, true);
            check = UIFactory.Button(col, "", Theme.Safe, Color.white, CheckAge);
            confirm = UIFactory.Button(col, "", Theme.Safe, Color.white, Confirm);
            explorer = UIFactory.Button(col, "", Theme.Primary, Color.white, OpenExplorer);
            var row = UIFactory.Row(col, "More", Theme.Gap, Theme.TouchTarget);
            resend = UIFactory.Button(row.transform, "", Theme.Card, Theme.Ink, () => SendCode(askedAge));
            UIFactory.Spacer(col, 1f);
        }

        static InputField DateField(Transform parent, int digits)
        {
            var field = UIFactory.Input(parent, "", InputField.ContentType.IntegerNumber, TouchScreenKeyboardType.NumberPad, digits);
            field.textComponent.fontSize = Theme.Title;
            field.textComponent.alignment = TextAnchor.MiddleCenter;
            ((Text)field.placeholder).alignment = TextAnchor.MiddleCenter;
            var le = field.GetComponent<LayoutElement>() ?? field.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            return field;
        }

        public override void OnShow()
        {
            codeStep = false;
            waiting = false;
            day.text = month.text = year.text = code.text = "";
            status.text = "";
            Refresh();
        }

        void Refresh()
        {
            TitleText.text = L("Guardian lessons", "Amasomo y'Umurinzi");
            heading.text = L("Ages 9 to 11", "Imyaka 9 kugeza 11");
            ((Text)day.placeholder).text = L("Day", "Umunsi");
            ((Text)month.placeholder).text = L("Month", "Ukwezi");
            ((Text)year.placeholder).text = L("Year", "Umwaka");
            UIFactory.SetButtonLabel(check, L("Check the age", "Genzura imyaka"));
            UIFactory.SetButtonLabel(confirm, L("Open the Guardian lessons", "Fungura amasomo y'Umurinzi"));
            UIFactory.SetButtonLabel(explorer, L("Go to the Explorer lessons", "Jya ku masomo y'Umuvumbuzi"));
            UIFactory.SetButtonLabel(resend, L("Send the code again", "Ohereza kode nanone"));

            day.transform.parent.gameObject.SetActive(!codeStep);
            check.gameObject.SetActive(!codeStep);
            code.gameObject.SetActive(codeStep);
            confirm.gameObject.SetActive(codeStep);
            resend.transform.parent.gameObject.SetActive(codeStep);
            explorer.gameObject.SetActive(false);

            if (!codeStep)
            {
                body.text = L("These lessons talk openly about online strangers and private parts of the body, so a grown-up must confirm the child's age first. Type the child's date of birth: it is only used to work out the age, and it is not saved or sent. We then email a code to the parent.",
                              "Aya masomo avuga mu buryo bweruye ku bantu batazwi kuri interineti no ku bice by'umubiri by'ibanga, bityo umuntu mukuru agomba kubanza kwemeza imyaka y'umwana. Andika itariki umwana yavukiyeho: ikoreshwa mu kubara imyaka gusa, ntibikwa kandi ntiyoherezwa. Hanyuma twohereza kode ku mubyeyi.");
            }
            else
            {
                var record = game.Consent != null ? game.Consent.Record : null;
                string where = record == null ? "" : (string.IsNullOrEmpty(record.emailMasked) ? record.email : record.emailMasked);
                body.text = string.Format(L("We emailed a 6-digit code to {0}, and by text if it is switched on. Type it here to confirm that the child is {1} years old. The code works for 24 hours.",
                                            "Twohereje kode y'imibare 6 kuri {0}, no kuri telefoni niba byemewe. Yandike hano kwemeza ko umwana afite imyaka {1}. Kode imara amasaha 24."), where, askedAge);
            }
        }

        // ---- step 1: the date ----

        void CheckAge()
        {
            int d, m, y;
            if (!int.TryParse(day.text, out d) || !int.TryParse(month.text, out m) || !int.TryParse(year.text, out y) || y < 1900 || y > DateTime.Today.Year)
            {
                Say(L("Please type the day, the month and the year, for example 14, 3 and 2016.", "Andika umunsi, ukwezi n'umwaka, urugero 14, 3 na 2016."), Theme.Caution);
                return;
            }
            DateTime born;
            try { born = new DateTime(y, m, d); }
            catch (ArgumentOutOfRangeException)
            {
                Say(L("That date does not exist. Please check it.", "Iyo tariki ntibaho. Mwongere murebe."), Theme.Caution);
                return;
            }
            int age = AgeOn(born, DateTime.Today);
            day.text = month.text = year.text = "";   // the date has done its job: it is not kept
            if (age < 0) { Say(L("That date has not happened yet.", "Iyo tariki ntirashira."), Theme.Caution); return; }
            if (age >= 9 && age <= 11) { SendCode(age); return; }

            explorer.gameObject.SetActive(age >= 6 && age <= 8);
            Say(age >= 6 && age <= 8
                ? string.Format(L("This child is {0}. The Guardian lessons are for ages 9 to 11; the Explorer lessons are made for them.", "Uyu mwana afite imyaka {0}. Amasomo y'Umurinzi ni ay'imyaka 9 kugeza 11; amasomo y'Umuvumbuzi ni yo bagenewe."), age)
                : string.Format(L("This child is {0}. SafeTouch lessons are for ages 6 to 11.", "Uyu mwana afite imyaka {0}. Amasomo ya SafeTouch ni ay'imyaka 6 kugeza 11."), age), Theme.Caution);
        }

        /// <summary>Whole years between a birth date and a day (never negative for a past date).</summary>
        public static int AgeOn(DateTime born, DateTime today)
        {
            int age = today.Year - born.Year;
            if (today.Month < born.Month || (today.Month == born.Month && today.Day < born.Day)) age--;
            return age;
        }

        // ---- step 2: the code ----

        void SendCode(int age)
        {
            if (waiting || game.Consent == null) return;
            waiting = true;
            askedAge = age;
            Say(L("Sending the code...", "Birohereza kode..."), Theme.Muted);
            game.Consent.RequestAgeCheck(age, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Sent:
                        codeStep = true;
                        status.text = "";
                        Refresh();
                        Say(L("Please check the parent's email, including the spam folder.", "Murebe imeyili y'umubyeyi, harimo n'aho spam iba."), Theme.Safe);
                        break;
                    case ConsentService.Result.Wait:
                    case ConsentService.Result.TooManyTries: Say(L("Please wait a minute before asking again.", "Mutegereze umunota umwe mbere yo gusaba indi."), Theme.Caution); break;
                    case ConsentService.Result.Offline: Say(NoInternet(), Theme.Caution); break;
                    case ConsentService.Result.Invalid: Say(L("The Guardian lessons are for ages 9 to 11.", "Amasomo y'Umurinzi ni ay'imyaka 9 kugeza 11."), Theme.Caution); break;
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
            game.Consent.VerifyAgeCheck(digits, result =>
            {
                waiting = false;
                switch (result)
                {
                    case ConsentService.Result.Verified:
                        code.text = "";
                        game.UnlockGuardian();
                        game.SwitchPath(Data.Progress.PathOlder);
                        game.ContinueFromSplash();
                        break;
                    case ConsentService.Result.WrongCode: Say(L("That code is not right. Please check the email and try again.", "Iyo kode si yo. Murebe imeyili mwongere mugerageze."), Theme.Unsafe); break;
                    case ConsentService.Result.Expired: Say(L("This code has expired. Tap \"Send the code again\".", "Iyi kode yararangiye. Mukande \"Ohereza kode nanone\"."), Theme.Unsafe); break;
                    case ConsentService.Result.TooManyTries: Say(L("Too many tries. Tap \"Send the code again\" for a new one.", "Mwagerageje inshuro nyinshi. Mukande \"Ohereza kode nanone\"."), Theme.Unsafe); break;
                    case ConsentService.Result.Offline: Say(NoInternet(), Theme.Caution); break;
                    default: Say(L("Something went wrong. Please try again in a moment.", "Hari ikitagenze neza. Mwongere mugerageze mukanya."), Theme.Caution); break;
                }
            });
        }

        void OpenExplorer()
        {
            game.SwitchPath(Data.Progress.PathYoung);
            game.ContinueFromSplash();
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
