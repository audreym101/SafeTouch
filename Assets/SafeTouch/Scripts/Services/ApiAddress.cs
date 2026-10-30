using UnityEngine;

namespace SafeTouch.Services
{
    /// <summary>
    /// Where the app finds the SafeTouch API (the consent emails, the login, the age check).
    /// On a phone, "localhost" is the phone itself: it can never reach the API, so a build that still
    /// has the laptop address saved in the scene would silently never send the parent's emails.
    /// This turns that address into the real one on a device, and lets the Editor keep using the laptop.
    /// </summary>
    public static class ApiAddress
    {
        /// <summary>The deployed API (Render + MongoDB Atlas, see docs/mongodb.md). Change it here if the address changes.</summary>
        public const string Default = "https://safetouch-api-xc6t.onrender.com";

        /// <summary>
        /// The free server sleeps after 15 minutes and takes up to a minute to wake: a shorter wait would
        /// give up on the first request of a session, and the parent would never see the email.
        /// </summary>
        public const int MinTimeoutSeconds = 60;

        /// <summary>The address to call: the one set in the Inspector, except an empty or laptop address on a phone.</summary>
        public static string Resolve(string configured)
        {
            return Resolve(configured, Application.isEditor);
        }

        public static string Resolve(string configured, bool inEditor)
        {
            if (string.IsNullOrWhiteSpace(configured)) return Default;
            return !inEditor && IsThisDevice(configured) ? Default : configured.Trim();
        }

        /// <summary>The timeout to use: the one set in the Inspector, but never shorter than the server's wake-up time.</summary>
        public static int Timeout(int configured)
        {
            return Mathf.Max(configured, MinTimeoutSeconds);
        }

        /// <summary>True for "localhost" and "127.0.0.1": addresses that mean the phone itself.</summary>
        public static bool IsThisDevice(string url)
        {
            var u = (url ?? "").ToLowerInvariant();
            return u.Contains("//localhost") || u.Contains("//127.0.0.1") || u.Contains("//0.0.0.0");
        }
    }
}
