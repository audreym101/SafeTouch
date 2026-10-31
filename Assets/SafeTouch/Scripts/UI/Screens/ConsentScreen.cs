using System.Collections.Generic;
using System.Text.RegularExpressions;
using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// The parental consent page, shown the first time the app starts and
    /// whenever the terms change. A parent or guardian reads the terms, ticks
    /// the four agreements and gives their name, phone, email and country.
    /// SafeTouch then emails them a 6-digit code and a copy of the consent
    /// (ConsentService). The child can start straight away; the parent
    /// confirms the code within 7 days.
    /// </summary>
    public class ConsentScreen : UIScreen
    {
        RectTransform list;
        Text intro, detailsTitle, error, footnote;
        InputField nameField, phoneField, emailField, countryField;
        Toggle guardian, terms, childUse, dataUse;
        Button agree;
        readonly List<GameObject> termCards = new List<GameObject>();

        static readonly Regex Email = new Regex(@"^[^\s@<>()\[\]\\,;:""]+@[^\s@<>()\[\]\\,;:""]+\.[A-Za-z]{2,}$");
        static readonly Regex Phone = new Regex(@"^\+?[0-9][0-9 ()-]{6,19}$");

        public override string Name { get { return "consent"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("splash"));
            list = UIFactory.ScrollList(col, Theme.Gap);
            intro = UIFactory.Label(list, "", Theme.Body, Theme.Ink, TextAnchor.UpperLeft, false);
            termsAnchor = UIFactory.Rect(list, "Terms");
            UIFactory.Column(termsAnchor, 20f, 0);

            detailsTitle = UIFactory.Label(list, "", Theme.Title, Theme.Ink, TextAnchor.MiddleLeft, true);
            nameField = UIFactory.Input(list, "", InputField.ContentType.Name, TouchScreenKeyboardType.Default, 80);
            phoneField = UIFactory.Input(list, "", InputField.ContentType.Standard, TouchScreenKeyboardType.PhonePad, 20);
            emailField = UIFactory.Input(list, "", InputField.ContentType.EmailAddress, TouchScreenKeyboardType.EmailAddress, 254);
            countryField = UIFactory.Input(list, "", InputField.ContentType.Standard, TouchScreenKeyboardType.Default, 56);

            guardian = UIFactory.Checkbox(list, "", _ => Refresh());
            terms = UIFactory.Checkbox(list, "", _ => Refresh());
            childUse = UIFactory.Checkbox(list, "", _ => Refresh());
            dataUse = UIFactory.Checkbox(list, "", _ => Refresh());

            error = UIFactory.Label(list, "", Theme.Label, Theme.Unsafe, TextAnchor.UpperLeft, true);
            agree = UIFactory.Button(list, "", Theme.Primary, Color.white, Submit);
            footnote = UIFactory.Label(list, "", Theme.Small, Theme.Muted, TextAnchor.UpperLeft, false);

            // After a withdrawal the next person must not see the previous parent's details.
            if (game.Consent != null) game.Consent.Changed += () => { if (game.Consent.Record == null) ClearForm(); };
        }

        void ClearForm()
        {
            foreach (var f in new[] { nameField, phoneField, emailField, countryField }) f.text = "";
            foreach (var t in new[] { guardian, terms, childUse, dataUse }) t.isOn = false;
        }

        RectTransform termsAnchor;

        public override void OnShow()
        {
            TitleText.text = T("consent.title", "For parents and guardians");
            intro.text = T("consent.intro", "Dear parents,\n\nBefore your child uses SafeTouch, please read what it does and what it keeps, tick the boxes, and give your details so we can confirm by email that it was you.");
            detailsTitle.text = T("consent.details", "Your details");
            SetPlaceholder(nameField, T("consent.name", "Your full name"));
            SetPlaceholder(phoneField, T("consent.phone", "Phone number with country code"));
            SetPlaceholder(emailField, T("consent.email", "Email address"));
            SetPlaceholder(countryField, T("consent.country", "Country"));
            if (string.IsNullOrEmpty(phoneField.text)) phoneField.text = "+250 ";
            if (string.IsNullOrEmpty(countryField.text)) countryField.text = T("consent.country.default", "Rwanda");
            SetLabel(guardian, T("consent.agree.guardian", "I am the parent or legal guardian of the child who will use SafeTouch."));
            SetLabel(terms, T("consent.agree.terms", "I have read and agree to the terms and privacy notice above."));
            SetLabel(childUse, T("consent.agree.child", "I give permission for my child to use SafeTouch, with an adult nearby."));
            SetLabel(dataUse, T("consent.agree.data", "I agree that SafeTouch keeps my name, phone number, email and country to verify this consent and contact me about it."));
            UIFactory.SetButtonLabel(agree, T("consent.submit", "I agree and give permission"));
            footnote.text = T("consent.footnote", "We will email you a 6-digit code and a copy of this consent. Your child can start now; please confirm the code within 7 days.");
            BuildTerms();

            // Coming back to correct an email: fill in what was given before.
            var previous = game.Consent != null ? game.Consent.Record : null;
            if (previous != null && string.IsNullOrEmpty(nameField.text))
            {
                nameField.text = previous.parentName;
                phoneField.text = previous.phone;
                emailField.text = previous.email;
                countryField.text = previous.country;
            }
            error.gameObject.SetActive(false);
            Refresh();
        }

        void BuildTerms()
        {
            foreach (var c in termCards) UIFactory.Remove(c);
            termCards.Clear();
            foreach (var section in ContentLibrary.Terms)
            {
                var card = UIFactory.Panel(termsAnchor, section.termId, Theme.Card, true);
                UIFactory.Column((RectTransform)card.transform, 10f, 40);
                UIFactory.Label(card.transform, section.title, Theme.Body, Theme.Ink, TextAnchor.UpperLeft, true);
                UIFactory.Label(card.transform, section.body, Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
                termCards.Add(card.gameObject);
            }
        }

        /// <summary>The button wakes up once all four boxes are ticked.</summary>
        void Refresh()
        {
            agree.interactable = guardian.isOn && terms.isOn && childUse.isOn && dataUse.isOn;
        }

        void Submit()
        {
            string problem = Check();
            if (problem != null)
            {
                error.text = problem;
                error.gameObject.SetActive(true);
                return;
            }
            var record = ConsentRecord.Create(game.Progress.GetData().userId, Localization.Language);
            record.parentName = nameField.text.Trim();
            record.phone = phoneField.text.Trim();
            record.email = emailField.text.Trim();
            record.country = countryField.text.Trim();
            record.agreements = new ConsentAgreements { guardian = true, terms = true, childUse = true, dataUse = true };
            var previous = game.Consent.Record;
            if (previous != null && previous.Agreed && !previous.verified)
            {
                // A correction keeps the same consent, so the server sends the new email a fresh code.
                record.consentId = previous.consentId;
                record.submittedAt = previous.submittedAt;
            }
            // Save first: the verify screen reads the new consent as soon as it opens.
            game.Consent.Submit(record, result => ui.UpdateHUD());
            ui.ShowMenu("verify");
        }

        /// <summary>The same rules the API applies, so most mistakes are caught before sending.</summary>
        string Check()
        {
            if (!(guardian.isOn && terms.isOn && childUse.isOn && dataUse.isOn))
                return T("consent.error.boxes", "Please tick all four boxes.");
            var name = nameField.text.Trim();
            if (name.Length < 2 || name.Length > 80)
                return T("consent.error.name", "Please type your full name.");
            var phone = phoneField.text.Trim();
            int digits = Regex.Replace(phone, @"\D", "").Length;
            if (!Phone.IsMatch(phone) || digits < 8 || digits > 15)
                return T("consent.error.phone", "Please type your phone number with the country code, for example +250 788 123 456.");
            if (!Email.IsMatch(emailField.text.Trim()))
                return T("consent.error.email", "Please check your email address.");
            if (countryField.text.Trim().Length < 2)
                return T("consent.error.country", "Please type your country.");
            return null;
        }

        static void SetPlaceholder(InputField field, string text)
        {
            var p = field.placeholder as Text;
            if (p != null) p.text = text;
        }

        static void SetLabel(Toggle box, string text)
        {
            var label = box.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        static string T(string key, string english)
        {
            return Localization.T(key, english);
        }
    }
}
