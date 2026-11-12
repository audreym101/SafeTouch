using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// Runs before every Android build, so a project that never ran
    /// SafeTouch > 1. Configure Android Settings still builds correctly:
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

            var fixedNow = SafeTouchSetup.ApplySafeAndroidSettings();
            if (fixedNow.Count > 0)
                Debug.LogWarning("[SafeTouch] Fixed Android settings: " + string.Join(", ", fixedNow.ToArray()) + ".");

            var needRestart = SafeTouchSetup.ApplyAndroidBuildSettings();
            if (needRestart.Count > 0)
            {
                AssetDatabase.SaveAssets();
                throw new BuildFailedException(
                    "SafeTouch fixed Android settings (" + string.Join(", ", needRestart.ToArray()) +
                    "). This build had already started with the old ones: press Build again.");
            }
        }
    }
}
