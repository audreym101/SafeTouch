namespace SafeTouch.Core
{
    // Central place for the privacy-by-design switches described in the proposal
    // (NFR1: no personal data collected; section 1.5: cloud sync is optional and anonymous-only).
    public static class AppConfig
    {
        public const string ProgressPrefsKey = "safetouch.progress.v1";

        // Off by default. The MVP scope (1.5) only requires local storage; cloud sync
        // of anonymous progress is an optional stretch goal, never enabled silently.
        public const bool CloudSyncEnabled = false;

        public const int MinScenariosInDeck = 10;
        public const int TrustedAdultsTargetCount = 2; // evaluation metric: >=2 adults named post-intervention
    }
}
