using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeTouch.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public LessonType? ActiveLesson { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneRoutes.Bootstrap)
            {
                GoToMainMenu();
            }
        }

        public void GoToMainMenu()
        {
            ActiveLesson = null;
            SceneManager.LoadScene(SceneRoutes.MainMenu);
        }

        public void GoToLesson(LessonType lesson)
        {
            ActiveLesson = lesson;
            SceneManager.LoadScene(SceneNameFor(lesson));
        }

        public void GoToParentSection()
        {
            SceneManager.LoadScene(SceneRoutes.ParentSection);
        }

        // NFR6: app shall include an easy exit from any screen.
        public void ExitToMainMenu()
        {
            GoToMainMenu();
        }

        public void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static string SceneNameFor(LessonType lesson)
        {
            switch (lesson)
            {
                case LessonType.SpaceBubble: return SceneRoutes.SpaceBubble;
                case LessonType.ScenarioCards: return SceneRoutes.ScenarioCards;
                case LessonType.StopFramework: return SceneRoutes.StopFramework;
                case LessonType.TrustedAdults: return SceneRoutes.TrustedAdults;
                default: return SceneRoutes.MainMenu;
            }
        }
    }
}
