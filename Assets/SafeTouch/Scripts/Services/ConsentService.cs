using System;
using System.Collections;
using System.Text;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace SafeTouch.Services
{
    /// <summary>
    /// Sends the parent's consent to the SafeTouch API (backend/src/consent.js),
    /// which emails a 6-digit code and a copy of the consent, and checks the
    /// code the parent types. Works offline: the consent is kept on the phone
    /// and sent when the internet comes back; a withdrawal made offline is
    /// also sent later. Only the parent's details travel; nothing about the child.
    /// </summary>
    public class ConsentService : MonoBehaviour
    {
        public enum Result { Sent, EmailFailed, Verified, WrongCode, Expired, TooManyTries, Wait, Invalid, Offline, NeedsLogin, Error }

        [Tooltip("The SafeTouch API. Use an https:// address for phones; Android refuses plain http in release builds.")]
        [SerializeField] string baseUrl = "http://localhost:3000";
        [SerializeField] int timeoutSeconds = 15;
        [SerializeField] float retrySeconds = 30f;

        const string PendingWithdrawKey = "safetouch.consent.withdraw";

        float nextRetry;
        bool busy;

        public ConsentRecord Record { get; private set; }

        /// <summary>Raised whenever the record changes (sent, verified, withdrawn).</summary>
        public event Action Changed;

        /// <summary>The parent has agreed to the current terms on this phone.</summary>
        public bool HasConsent { get { return Record != null && Record.Agreed; } }

        /// <summary>The 7 days are over and the code was never confirmed: the app asks for it before a child continues.</summary>
        public bool MustVerifyNow { get { return HasConsent && Record.MustVerifyNow; } }

        /// <summary>The parent has logged in since the app was opened. Not saved: every time the app starts, the parent logs in again.</summary>
        public bool LoggedIn { get; private set; }

        /// <summary>
        /// A parent whose consent is confirmed must log in (email, then a code) each time the app opens.
        /// Parents still in the 7 days before confirming go through the verify screen instead.
        /// </summary>
        public bool NeedsLogin { get { return HasConsent && Record.verified && !LoggedIn; } }

        /// <summary>Ends the login: the app asks the parent for a code again (called when the app goes to the background).</summary>
        public void LogOut()
        {
            LoggedIn = false;
        }

        /// <summary>Message from the last failed request, for the screen.</summary>
        public string LastError { get; private set; }

        [Serializable] class Payload
        {
            public string consentId, deviceId, parentName, phone, email, country, language, termsVersion;
            public ConsentAgreements agreements;
        }
        [Serializable] class StatusReply { public bool verified; public string verifiedAt; public string emailMasked; public bool emailSent; public bool sent; public bool loggedIn; public bool ageConfirmed; public bool emailChanged; public string error; public string[] errors; public int attemptsLeft; }
        [Serializable] class CodeBody { public string code; }
        [Serializable] class EmailBody { public string email; }

        void Awake()
        {
            Record = ConsentRecord.Load();
        }

        void Update()
        {
            // Offline earlier? Try again every half minute.
            if (busy || Time.unscaledTime < nextRetry) return;
            nextRetry = Time.unscaledTime + retrySeconds;
            if (PlayerPrefs.HasKey(PendingWithdrawKey)) StartCoroutine(SendWithdraw(PlayerPrefs.GetString(PendingWithdrawKey), null));
            else if (HasConsent && !Record.sentToServer) StartCoroutine(SendConsent(null));
        }

        /// <summary>Saves the parent's consent on the phone and sends it. done gets Sent, EmailFailed, Offline...</summary>
        public void Submit(ConsentRecord record, Action<Result> done)
        {
            Record = record;
            Record.sentToServer = false;
            Record.verified = false;
            Record.Save();
            Notify();
            StartCoroutine(SendConsent(done));
        }

        public void Verify(string code, Action<Result> done)
        {
            if (!HasConsent) { done(Result.Invalid); return; }
            if (!Record.sentToServer) { done(Result.Offline); return; }
            var body = JsonUtility.ToJson(new CodeBody { code = code });
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/verify", body, (status, reply) =>
            {
                if (status == 200 && reply.verified)
                {
                    Record.verified = true;
                    Record.verifiedAt = reply.verifiedAt;
                    LoggedIn = true;   // typing the emailed code proves the email: no second login right after
                    Record.Save();
                    Notify();
                    done(Result.Verified);
                }
                else done(Classify(status));
            }));
        }

        public void Resend(Action<Result> done)
        {
            if (!HasConsent) { done(Result.Invalid); return; }
            if (!Record.sentToServer) { StartCoroutine(SendConsent(done)); return; }
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/resend", "{}", (status, reply) =>
                done(status == 200 ? (reply.emailSent ? Result.Sent : Result.EmailFailed) : status == 429 ? Result.Wait : Classify(status))));
        }

        /// <summary>
        /// Login step 1: asks the API to email (and text) a fresh code to the parent. The API
        /// answers "sent" even if the typed email is not the parent's, so the screen says
        /// "if it matches, a code is on its way". done gets Sent, Invalid (not an email), Wait, EmailFailed or Offline.
        /// </summary>
        public void RequestLoginCode(string email, Action<Result> done)
        {
            if (!HasConsent || !Record.sentToServer) { done(Result.Offline); return; }
            var body = JsonUtility.ToJson(new EmailBody { email = (email ?? "").Trim() });
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/login", body, (status, reply) =>
                done(status == 200 ? Result.Sent : status == 400 ? Result.Invalid : status == 429 ? Result.Wait : status == 502 ? Result.EmailFailed : Classify(status))));
        }

        /// <summary>Login step 2: checks the code. done gets Verified (logged in), WrongCode, Expired, TooManyTries or Offline.</summary>
        public void VerifyLogin(string code, Action<Result> done)
        {
            if (!HasConsent || !Record.sentToServer) { done(Result.Offline); return; }
            var body = JsonUtility.ToJson(new CodeBody { code = code });
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/login/verify", body, (status, reply) =>
            {
                if (status == 200 && reply.loggedIn)
                {
                    LoggedIn = true;
                    done(Result.Verified);
                }
                else done(Classify(status));
            }));
        }

        /// <summary>
        /// Age check step 1: tells the API the child's age in whole years (worked out on this phone from a
        /// date that is neither kept nor sent) so it emails the parent a code. Only 9 to 11 gets one.
        /// done gets Sent, Invalid (not 9 to 11), Wait, EmailFailed or Offline.
        /// </summary>
        public void RequestAgeCheck(int ageYears, Action<Result> done)
        {
            if (!HasConsent || !Record.sentToServer) { done(Result.Offline); return; }
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/age", "{\"ageYears\":" + ageYears + "}", (status, reply) =>
                done(status == 200 ? Result.Sent : status == 400 ? Result.Invalid : status == 403 ? Result.Error : status == 429 ? Result.Wait : status == 502 ? Result.EmailFailed : Classify(status))));
        }

        /// <summary>Age check step 2: checks the code. done gets Verified (the Guardian lessons may open), WrongCode, Expired, TooManyTries or Offline.</summary>
        public void VerifyAgeCheck(string code, Action<Result> done)
        {
            if (!HasConsent || !Record.sentToServer) { done(Result.Offline); return; }
            var body = JsonUtility.ToJson(new CodeBody { code = code });
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/age/verify", body, (status, reply) =>
                done(status == 200 && reply.ageConfirmed ? Result.Verified : Classify(status))));
        }

        /// <summary>
        /// Change the email, step 1 (consent confirmed, parent logged in): the API sends a code to the NEW
        /// email; the old one stays until the code is typed. done gets Sent, Invalid (not an email, or the
        /// same one), NeedsLogin (log in again), Wait, EmailFailed or Offline.
        /// </summary>
        public void RequestEmailChange(string email, Action<Result> done)
        {
            if (!HasConsent || !Record.sentToServer || !Record.verified) { done(Result.Offline); return; }
            var body = JsonUtility.ToJson(new EmailBody { email = (email ?? "").Trim() });
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/email", body, (status, reply) =>
            {
                if (status == 200) done(Result.Sent);
                else if (status == 401) { LogOut(); done(Result.NeedsLogin); }
                else done(status == 400 ? Result.Invalid : status == 429 ? Result.Wait : status == 502 ? Result.EmailFailed : Classify(status));
            }));
        }

        /// <summary>Change the email, step 2: the code from the new email. Verified means the codes now go to the new email.</summary>
        public void VerifyEmailChange(string code, string newEmail, Action<Result> done)
        {
            if (!HasConsent || !Record.sentToServer) { done(Result.Offline); return; }
            var body = JsonUtility.ToJson(new CodeBody { code = code });
            StartCoroutine(Request("POST", "/api/consent/" + Record.consentId + "/email/verify", body, (status, reply) =>
            {
                if (status == 200 && reply.emailChanged)
                {
                    Record.email = (newEmail ?? "").Trim();
                    Record.emailMasked = reply.emailMasked;
                    Record.Save();
                    Notify();
                    done(Result.Verified);
                }
                else done(Classify(status));
            }));
        }

        /// <summary>
        /// Withdraws consent: forgets it on this phone at once, and asks the API to
        /// delete the parent's details and the synced progress (now, or when online).
        /// </summary>
        public void Withdraw(Action<Result> done)
        {
            if (Record == null) { if (done != null) done(Result.Sent); return; }
            string id = Record.consentId;
            bool wasSent = Record.sentToServer;
            ConsentRecord.Delete();
            Record = null;
            LoggedIn = false;
            Notify();
            if (!wasSent) { if (done != null) done(Result.Sent); return; }
            PlayerPrefs.SetString(PendingWithdrawKey, id);
            PlayerPrefs.Save();
            StartCoroutine(SendWithdraw(id, done));
        }

        IEnumerator SendConsent(Action<Result> done)
        {
            var r = Record;
            // A slow earlier request (the free server waking up) may still be on its way with the old email.
            // Wait for it, so the newest details reach the API last and the code goes to the new email.
            while (busy) yield return null;
            if (r != Record) yield break;   // corrected again meanwhile: the newer details are sent instead
            var payload = new Payload
            {
                consentId = r.consentId, deviceId = r.deviceId, parentName = r.parentName, phone = r.phone,
                email = r.email, country = r.country, language = r.language, termsVersion = r.termsVersion, agreements = r.agreements
            };
            yield return Request("POST", "/api/consent", JsonUtility.ToJson(payload), (status, reply) =>
            {
                if (status == 200 || status == 201)
                {
                    r.sentToServer = true;
                    r.verified = reply.verified;
                    if (!string.IsNullOrEmpty(reply.emailMasked)) r.emailMasked = reply.emailMasked;
                    r.Save();
                    Notify();
                    if (done != null) done(reply.emailSent || status == 200 ? Result.Sent : Result.EmailFailed);
                }
                else if (done != null) done(status == 400 ? Result.Invalid : Classify(status));
            });
        }

        IEnumerator SendWithdraw(string consentId, Action<Result> done)
        {
            yield return Request("DELETE", "/api/consent/" + consentId, null, (status, reply) =>
            {
                if (status == 204 || status == 404)   // deleted, or never there
                {
                    PlayerPrefs.DeleteKey(PendingWithdrawKey);
                    PlayerPrefs.Save();
                }
                if (done != null) done(status == 204 || status == 404 ? Result.Sent : Classify(status));
            });
        }

        IEnumerator Request(string method, string path, string json, Action<long, StatusReply> handle)
        {
            busy = true;
            using (var req = new UnityWebRequest(ApiAddress.Resolve(baseUrl).TrimEnd('/') + path, method))
            {
                if (json != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = ApiAddress.Timeout(timeoutSeconds);
                yield return req.SendWebRequest();
                busy = false;

                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.DataProcessingError)
                {
                    LastError = req.error;
                    handle(0, new StatusReply());
                    yield break;
                }
                var reply = new StatusReply();
                var text = req.downloadHandler.text;
                if (!string.IsNullOrEmpty(text) && text.TrimStart().StartsWith("{"))
                {
                    try { reply = JsonUtility.FromJson<StatusReply>(text); } catch (ArgumentException) { }
                }
                LastError = reply.errors != null && reply.errors.Length > 0 ? reply.errors[0] : reply.error;
                handle(req.responseCode, reply);
            }
        }

        static Result Classify(long status)
        {
            switch (status)
            {
                case 0: return Result.Offline;
                case 400: return Result.WrongCode;
                case 410: return Result.Expired;
                case 429: return Result.TooManyTries;
                default: return Result.Error;
            }
        }

        void Notify()
        {
            if (Changed != null) Changed();
        }
    }
}
