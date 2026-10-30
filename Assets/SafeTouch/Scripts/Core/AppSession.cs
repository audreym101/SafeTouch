namespace SafeTouch.Core
{
    /// <summary>Facts about the app's current run that several parts need to agree on.</summary>
    public static class AppSession
    {
        const float AskingTimeout = 90f;
        static bool asking;
        static float askedAt;

        /// <summary>
        /// True while Android's camera or microphone question is on screen. That question pauses the app,
        /// but the parent has not left it, so coming back from it must not log the parent out. It stops
        /// counting after 90 seconds, in case an answer never arrives.
        /// </summary>
        public static bool AskingPermission
        {
            get { return asking && UnityEngine.Time.realtimeSinceStartup - askedAt < AskingTimeout; }
        }

        public static void PermissionAsked()
        {
            asking = true;
            askedAt = UnityEngine.Time.realtimeSinceStartup;
        }

        public static void PermissionAnswered()
        {
            asking = false;
        }
    }
}
