using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// Runs before every Android build. An APK built without the ARCore
    /// loader opens to a black screen and never asks for the camera, so this
    /// switches ARCore on if it is off, and stops the build with a clear
    /// message if that is not possible.
    /// </summary>
    public class SafeTouchBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

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

            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
                Debug.LogWarning("[SafeTouch] ARCore needs Android 8.0 (API 26) or newer. Run SafeTouch > 1. Configure Android AR Settings.");
        }
    }
}
