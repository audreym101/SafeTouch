using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace SafeTouch.AR
{
    /// <summary>
    /// Asks Android for the camera, which AR needs to show Keza in the room.
    /// SafeTouch asks only when the child reaches the "find the floor" screen,
    /// after explaining why, rather than as soon as the app opens. The
    /// camera picture is never saved or sent.
    /// </summary>
    public static class CameraPermission
    {
        /// <summary>True when the app may use the camera (always true in the Editor).</summary>
        public static bool Granted
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Shows Android's camera question. answered gets (granted, canAskAgain):
        /// canAskAgain is false once the child chose "Don't allow" and "Don't ask
        /// again", after which only the phone's Settings can switch it on.
        /// </summary>
        public static void Request(Action<bool, bool> answered)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Granted) { answered(true, true); return; }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => answered(true, true);
            callbacks.PermissionDenied += _ => answered(false, true);
            callbacks.PermissionDeniedAndDontAskAgain += _ => answered(false, false);
            Permission.RequestUserPermission(Permission.Camera, callbacks);
#else
            answered(true, true);
#endif
        }

        /// <summary>Opens SafeTouch's page in Android Settings so a grown-up can allow the camera.</summary>
        public static void OpenAppSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", Application.identifier, null))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uri))
            {
                intent.Call<AndroidJavaObject>("addFlags", 0x10000000);   // FLAG_ACTIVITY_NEW_TASK
                activity.Call("startActivity", intent);
            }
#endif
        }
    }
}
