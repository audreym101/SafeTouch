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

        const float CodeTimeout = 30f * 60f;
        static float codeSentAt = -1f;

        /// <summary>
        /// True while a screen is waiting for a 6-digit code the parent must read in their email (changing the
        /// email, the age check). Reading it means leaving SafeTouch for the mail app, so coming back must not
        /// log the parent out or leave that screen. It stops counting after 30 minutes, when the codes expire.
        /// </summary>
        public static bool WaitingForCode
        {
            get { return codeSentAt >= 0f && UnityEngine.Time.realtimeSinceStartup - codeSentAt < CodeTimeout; }
        }

        public static void CodeSent()
        {
            codeSentAt = UnityEngine.Time.realtimeSinceStartup;
        }

        public static void CodeDone()
        {
            codeSentAt = -1f;
        }
    }
}
