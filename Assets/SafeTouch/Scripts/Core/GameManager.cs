using System.Linq;
using SafeTouch.AR;
using SafeTouch.Data;
using SafeTouch.Lessons;
using SafeTouch.Managers;
using SafeTouch.Services;
using SafeTouch.UI;
using SafeTouch.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SafeTouch.Core
{
    /// <summary>
    /// Entry point and flow controller. Owns the app state (current lesson,
    /// age group) and connects the managers from the class diagram:
    /// ARManager, UIManager, AudioManager, ScenarioManager, ProgressManager.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] int currentLevel;
        [SerializeField] string ageGroup;
        [SerializeField] string selectedMode = "learn";   // "learn" now; "practice" (replay without scoring) is planned

        [Header("Managers")]
        [SerializeField] ARManager arManager;
        [SerializeField] UIManager uiManager;
        [SerializeField] AudioManager audioManager;
        [SerializeField] ScenarioManager scenarioManager;
        [SerializeField] ProgressManager progressManager;
        [SerializeField] CloudSyncService cloudSync;
        [SerializeField] VoiceCoach voiceCoach;
        [SerializeField] ConsentService consent;

        [Header("Lessons (levels 1-4) and the Memory Quest (5)")]
        [SerializeField] LessonBase[] lessons = new LessonBase[0];

        Progress progress;
        LessonBase activeLesson;

        public ARManager AR { get { return arManager; } }
        public UIManager UI { get { return uiManager; } }
        public AudioManager Audio { get { return audioManager; } }
        public ScenarioManager Scenarios { get { return scenarioManager; } }
        public ProgressManager Progress { get { return progressManager; } }
        public VoiceCoach Voice { get { return voiceCoach; } }
        public ConsentService Consent { get { return consent; } }
        public int CurrentLevel { get { return currentLevel; } }
        public string SelectedMode { get { return selectedMode; } }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;   // the screen stays on during AR lessons

            if (uiManager == null) uiManager = GetComponentInChildren<UIManager>();
            if (audioManager == null) audioManager = GetComponentInChildren<AudioManager>();
            if (scenarioManager == null) scenarioManager = GetComponentInChildren<ScenarioManager>();
            if (progressManager == null) progressManager = GetComponentInChildren<ProgressManager>();
            if (cloudSync == null) cloudSync = GetComponentInChildren<CloudSyncService>();
            if (voiceCoach == null) voiceCoach = GetComponentInChildren<VoiceCoach>();
            if (consent == null) consent = GetComponentInChildren<ConsentService>();
            if (consent == null) consent = gameObject.AddComponent<ConsentService>();
            if (lessons.Length == 0) lessons = GetComponentsInChildren<LessonBase>();
        }

        void Start()
        {
            LoadProgress();
            Localization.SetLanguage(string.IsNullOrEmpty(progress.language) ? "en" : progress.language);
            uiManager.Initialise(this);
            arManager.CharacterPlaced += OnCharacterPlaced;
            progressManager.BadgeUnlocked += b => uiManager.ShowToast(Localization.T("badge.unlocked") + " " + b.title);
            foreach (var lesson in lessons) lesson.Completed += OnLessonCompleted;
            uiManager.ShowMenu("splash");
        }

        void Update()
        {
            // Android back button: leave the lesson and go home (NFR6).
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (uiManager.ActiveMenu == "home" || uiManager.ActiveMenu == "splash") uiManager.ShowMenu("splash");
                else ReturnToMenu();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && progressManager != null) SaveProgress();
        }

        // ---- Class diagram API ----

        public void SelectAgeGroup(string age)
        {
            ageGroup = age;
            progressManager.SetAgeGroup(age);
            if (voiceCoach != null) voiceCoach.PrepareForAge(age);
            uiManager.ShowMenu("home");
        }

        /// <summary>Opens a lesson. Places Keza first if she is not in the room yet.</summary>
        public void StartLesson(int level)
        {
            if (!progressManager.IsUnlocked(level)) return;
            EndActiveLesson();
            currentLevel = level;
            if (!arManager.HasCharacter)
            {
                uiManager.ShowMenu("scan");
                arManager.BeginPlacement();
                return;
            }
            BeginCurrentLesson();
        }

        public void SaveProgress()
        {
            progressManager.Save();
        }

        public void LoadProgress()
        {
            progress = progressManager.Load();
            ageGroup = progress.ageGroup;
            if (voiceCoach != null)
            {
                voiceCoach.Enabled = progress.voiceEnabled;
                voiceCoach.PrepareForAge(ageGroup);
            }
        }

        /// <summary>Opens the next lesson after the current one is completed.</summary>
        public void UnlockNextLevel()
        {
            progressManager.RebuildUnlockedLevels();
        }

        /// <summary>Leaves any lesson and returns to the lesson map. Progress is kept.</summary>
        public void ReturnToMenu()
        {
            EndActiveLesson();
            arManager.CancelPlacement();
            audioManager.StopAll();
            if (voiceCoach != null) voiceCoach.Stop();
            currentLevel = 0;
            uiManager.ShowMenu("home");
        }

        // ---- Other actions used by the screens ----

        public void MoveCharacter()
        {
            int level = currentLevel;
            EndActiveLesson();
            currentLevel = level;
            uiManager.ShowMenu("scan");
            arManager.BeginPlacement();
        }

        public void SetLanguage(string language)
        {
            progressManager.SetLanguage(language);
            Localization.SetLanguage(language);
        }

        /// <summary>
        /// Where "Start" leads. A parent must agree first (the consent page);
        /// after 7 days without confirming the emailed code, the code is asked
        /// for again; then the child chooses an age group or goes to the lessons.
        /// </summary>
        public void ContinueFromSplash()
        {
            if (consent != null && !consent.HasConsent) { uiManager.ShowMenu("consent"); return; }
            if (consent != null && consent.MustVerifyNow) { uiManager.ShowMenu("verify"); return; }
            ContinueToLessons();
        }

        /// <summary>After consent (or "verify later"): the age question the first time, then the lesson map.</summary>
        public void ContinueToLessons()
        {
            uiManager.ShowMenu(string.IsNullOrEmpty(progressManager.GetData().ageGroup) ? "age" : "home");
        }

        /// <summary>
        /// A parent withdraws consent: their details are deleted from the server
        /// (now, or when online), the child's progress is deleted from this phone,
        /// and the app returns to the start for a new consent.
        /// </summary>
        public void WithdrawConsent()
        {
            if (consent != null) consent.Withdraw(null);
            ResetProgress();
        }

        /// <summary>Parent switched spoken NO practice on or off. Off closes the microphone straight away.</summary>
        public void SetVoiceEnabled(bool on)
        {
            progressManager.SetVoiceEnabled(on);
            if (voiceCoach == null) return;
            voiceCoach.Enabled = on;
            if (!on) voiceCoach.Stop();
        }

        public void SyncNow()
        {
            if (cloudSync != null && progress.cloudSyncConsent) StartCoroutine(cloudSync.Upload(progress));
        }

        /// <summary>Parent switched sync on or off. Off also deletes the copy on the server.</summary>
        public void SetCloudSync(bool on)
        {
            progressManager.SetCloudSyncConsent(on);
            if (cloudSync == null) return;
            if (on) SyncNow();
            else StartCoroutine(cloudSync.Withdraw(progress.userId));
        }

        public void ResetProgress()
        {
            EndActiveLesson();
            arManager.ResetPlacement();
            progressManager.ResetAll();
            LoadProgress();
            uiManager.ShowMenu("splash");
        }

        /// <summary>The child picked a new look for Keza. If she is in the room, the change plays there too.</summary>
        public void SetSkinTone(string toneId)
        {
            progressManager.SetSkinTone(toneId);
            arManager.ApplySkinTone(SkinTones.Get(toneId), true);
        }

        void OnCharacterPlaced(ARCharacter character)
        {
            arManager.ApplySkinTone(SkinTones.Get(progress.skinTone), false);
            if (currentLevel > 0 && activeLesson == null) BeginCurrentLesson();
        }

        void BeginCurrentLesson()
        {
            activeLesson = lessons.FirstOrDefault(l => l.Level == currentLevel);
            if (activeLesson == null)
            {
                Debug.LogError("[SafeTouch] No lesson component for level " + currentLevel);
                ReturnToMenu();
                return;
            }
            uiManager.ShowMenu("lesson");
            activeLesson.Begin(this);
        }

        void EndActiveLesson()
        {
            if (activeLesson != null) activeLesson.End();
            activeLesson = null;
        }

        void OnLessonCompleted(int score, int max)
        {
            int level = currentLevel;
            activeLesson = null;
            if (level == ProgressManager.GameLevel)
            {
                // Bubble Guard is replayable and outside the lesson order: record the score only.
                int best = progress.bestGuardScore;
                progressManager.RecordGuard(score);
                string summary = string.Format(Localization.T("game.end.body", "You scored {0} out of {1}."), score, max);
                if (score > best && best > 0) summary += " " + Localization.T("game.end.best", "That's your best score yet!");
                uiManager.ShowFeedback(Localization.T("game.end.title", "Bubble Guard complete!"), summary, FeedbackTone.Badge, ReturnToMenu);
                return;
            }
            progressManager.CompleteLevel(level, score);
            UnlockNextLevel();
            SaveProgress();
            SyncNow();

            string next = level < ProgressManager.LessonCount
                ? Localization.T("complete.next")
                : Localization.T("complete.all");
            uiManager.ShowFeedback(Localization.T("complete.title"),
                string.Format(Localization.T("complete.body"), score, max) + " " + next,
                FeedbackTone.Badge, ReturnToMenu);
        }
    }
}
