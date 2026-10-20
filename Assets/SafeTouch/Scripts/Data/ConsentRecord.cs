using System;
using System.Globalization;
using UnityEngine;

namespace SafeTouch.Data
{
    /// <summary>The four things a parent or guardian agrees to before a child uses SafeTouch.</summary>
    [Serializable]
    public class ConsentAgreements
    {
        public bool guardian;   // I am the child's parent or legal guardian
        public bool terms;      // I have read and agree to the terms and privacy notice
        public bool childUse;   // My child may use SafeTouch, with an adult nearby
        public bool dataUse;    // SafeTouch may keep my name, phone, email and country for this consent

        public bool All { get { return guardian && terms && childUse && dataUse; } }
    }

    /// <summary>
    /// A parent's consent, kept on the phone (PlayerPrefs) apart from the
    /// child's progress. It holds the parent's own details, which they gave
    /// so the consent can be verified by email (Law N° 058/2021, Article 9),
    /// and nothing about the child. The child may use the app straight away;
    /// the parent confirms with the emailed code within GraceDays.
    /// </summary>
    [Serializable]
    public class ConsentRecord
    {
        public const string PrefsKey = "safetouch.consent.v1";

        /// <summary>The version of the terms shown; bump it when the terms change so parents agree again.</summary>
        public const string TermsVersion = "1.0";

        /// <summary>Days a child can use SafeTouch before the parent must confirm the emailed code.</summary>
        public const int GraceDays = 7;

        public string consentId;     // random id for this consent
        public string deviceId;      // Progress.userId, so withdrawal can delete synced progress too
        public string parentName;
        public string phone;
        public string email;
        public string country;
        public string language;      // the language of the consent email: "en" or "rw"
        public string termsVersion;
        public ConsentAgreements agreements = new ConsentAgreements();
        public string submittedAt;   // when the parent agreed (UTC, ISO 8601)
        public bool sentToServer;    // false until the API has it (the phone may be offline)
        public bool verified;
        public string verifiedAt;
        public string emailMasked;   // "u***e@example.com", from the API, for the verify screen

        public bool Agreed { get { return agreements != null && agreements.All && termsVersion == TermsVersion; } }

        DateTime Submitted
        {
            get
            {
                DateTime t;
                return DateTime.TryParse(submittedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out t)
                    ? t : DateTime.UtcNow;
            }
        }

        /// <summary>Whole days left to verify (0 when the grace period is over).</summary>
        public int DaysLeft
        {
            get
            {
                if (verified) return GraceDays;
                double left = (Submitted.AddDays(GraceDays) - DateTime.UtcNow).TotalDays;
                return Math.Max(0, (int)Math.Ceiling(left));
            }
        }

        /// <summary>True once the grace period is over and the consent is still not verified.</summary>
        public bool MustVerifyNow { get { return Agreed && !verified && DateTime.UtcNow >= Submitted.AddDays(GraceDays); } }

        public static ConsentRecord Load()
        {
            if (!PlayerPrefs.HasKey(PrefsKey)) return null;
            try { return JsonUtility.FromJson<ConsentRecord>(PlayerPrefs.GetString(PrefsKey)); }
            catch (ArgumentException) { return null; }
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>A new consent with fresh ids and today's date.</summary>
        public static ConsentRecord Create(string deviceId, string language)
        {
            return new ConsentRecord
            {
                consentId = Guid.NewGuid().ToString(),
                deviceId = deviceId,
                language = language == "rw" ? "rw" : "en",
                termsVersion = TermsVersion,
                submittedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
        }
    }
}
