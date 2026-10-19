using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// Runs before every Android build, ahead of ARCore's own checks, so a
    /// project that never ran SafeTouch > 1. Configure Android AR Settings
    /// still builds a working AR app:
    ///   - ARCore is switched on (without it: no camera prompt, black screen);
    ///   - the minimum Android version, target version and portrait
    ///     orientation are fixed, and the build carries on;
    ///   - IL2CPP, ARM64 and OpenGL ES 3 are fixed too, but the build has
    ///     already used them, so it stops and asks for Build to be pressed again.
    /// </summary>
    public class SafeTouchBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return -100; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            if (!SafeTouchSetup.ARCoreEnabled())
            {
                Debug.LogWarning("[SafeTouch] ARCore was off for Android; switching it on before the build.");
                SafeTouchSetup.EnsureXRLoaders();
            }
            if (!SafeTouchSetup.ARCoreEnabled())
                throw new BuildFailedException(
                    "SafeTouch needs Google ARCore for the camera and AR. Open Project Settings > XR Plug-in Management, " +
                    "Android tab, and tick Google ARCore (or run SafeTouch > 1. Configure Android AR Settings), then build again.");

            var fixedNow = SafeTouchSetup.ApplySafeAndroidSettings();
            if (fixedNow.Count > 0)
                Debug.LogWarning("[SafeTouch] Fixed Android settings for AR: " + string.Join(", ", fixedNow.ToArray()) + ".");

            var needRestart = SafeTouchSetup.ApplyAndroidBuildSettings();
            if (needRestart.Count > 0)
            {
                AssetDatabase.SaveAssets();
                throw new BuildFailedException(
                    "SafeTouch fixed Android settings that AR needs (" + string.Join(", ", needRestart.ToArray()) +
                    "). This build had already started with the old ones: press Build again.");
            }
        }
    }
}
