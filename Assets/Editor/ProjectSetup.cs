using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    // Applies the Player Settings called for by the proposal's scope (1.5) and
    // non-functional requirements (NFR2). Uses only stable, documented PlayerSettings
    // APIs so this is safe to run once after the project is first opened.
    public static class ProjectSetup
    {
        private const string AndroidApplicationId = "com.safetouch.rw.app";
        private const string IosApplicationId = "com.safetouch.rw.app";

        [MenuItem("SafeTouch/Setup/1. Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "African Leadership University";
            PlayerSettings.productName = "SafeTouch";

            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Android — NFR2: run on Android 8.0+ (API 26) devices with ARCore support.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidApplicationId);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // iOS — kept ready alongside Android per project decision, ARKit-based.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IosApplicationId);
            PlayerSettings.iOS.targetOSVersionString = "12.0";
            PlayerSettings.iOS.cameraUsageDescription =
                "SafeTouch uses the camera to show the AR lesson characters in your room. No photos or video are saved.";

            Debug.Log("[SafeTouch] Player Settings configured. Next: Edit > Project Settings > XR Plug-in " +
                      "Management -> tick 'ARCore' under the Android tab and 'ARKit' under the iOS tab " +
                      "(not automated here since the exact API differs across AR Foundation versions).");
        }
    }
}
