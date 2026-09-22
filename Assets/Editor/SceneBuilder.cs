using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using SafeTouch.AR;
using SafeTouch.Core;

namespace SafeTouch.EditorTools
{
    // Builds the Week 1 scenes described in the timeline (Fig. 1.8) deliverable:
    // "AR scene, plane detection". Uses Unity's own GameObject menu commands for the
    // parts that are fragile to hand-author (XR Origin + tracked pose driver wiring,
    // UI Canvas/EventSystem), then wires SafeTouch scripts on top with SerializedObject
    // so this keeps working even if AR Foundation's internal defaults shift slightly.
    public static class SceneBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string ScenesFolder = "Assets/Scenes";

        [MenuItem("SafeTouch/Setup/2. Build Week 1 Scenes (Bootstrap + AR Test)")]
        public static void BuildWeek1Scenes()
        {
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(ScenesFolder);

            string bootstrapPath = BuildBootstrapScene();
            string arTestPath = BuildARFoundationTestScene();

            var buildScenes = new[]
            {
                new EditorBuildSettingsScene(bootstrapPath, true),
                new EditorBuildSettingsScene(arTestPath, true),
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            Debug.Log("[SafeTouch] Week 1 scenes built: " + bootstrapPath + ", " + arTestPath +
                      ". Open the AR test scene and press Play (or build to an ARCore device) to try plane detection + tap-to-place.");
        }

        private static string BuildBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();

            var progressGO = new GameObject("ProgressTracker");
            progressGO.AddComponent<ProgressTracker>();

            var audioGO = new GameObject("AudioManager");
            audioGO.AddComponent<AudioSource>();
            audioGO.AddComponent<AudioManager>();

            string path = $"{ScenesFolder}/{SceneRoutes.Bootstrap}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildARFoundationTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var arSessionGO = CreateViaMenuOrFallback("GameObject/XR/AR Session", "AR Session",
                () =>
                {
                    var go = new GameObject("AR Session");
                    go.AddComponent<ARSession>();
                    return go;
                });
            if (arSessionGO.GetComponent<ARSession>() == null)
            {
                arSessionGO.AddComponent<ARSession>();
            }

            var xrOriginGO = CreateViaMenuOrFallback("GameObject/XR/XR Origin (AR)", "XR Origin",
                () =>
                {
                    Debug.LogWarning("[SafeTouch] Could not find the 'XR Origin (AR)' menu item. Creating a plain " +
                                      "'XR Origin' GameObject instead - you will need to add an AR Camera child and a " +
                                      "TrackedPoseDriver by hand (GameObject > XR > XR Origin (AR) in your Editor's menu).");
                    var go = new GameObject("XR Origin");
                    go.AddComponent<XROrigin>();
                    return go;
                });

            var planeManager = xrOriginGO.GetComponent<ARPlaneManager>();
            if (planeManager == null) planeManager = xrOriginGO.AddComponent<ARPlaneManager>();

            var raycastManager = xrOriginGO.GetComponent<ARRaycastManager>();
            if (raycastManager == null) raycastManager = xrOriginGO.AddComponent<ARRaycastManager>();

            GameObject planePrefab = CreateOrLoadPlanePrefab();
            planeManager.planePrefab = planePrefab;

            GameObject characterPrefab = CreateOrLoadCharacterPrefab();

            var tapToPlace = xrOriginGO.AddComponent<ARPlaneTapToPlace>();
            var tapToPlaceSO = new SerializedObject(tapToPlace);
            tapToPlaceSO.FindProperty("placedPrefab").objectReferenceValue = characterPrefab;
            tapToPlaceSO.FindProperty("planeManager").objectReferenceValue = planeManager;
            tapToPlaceSO.FindProperty("hidePlanesAfterFirstPlacement").boolValue = true;
            tapToPlaceSO.ApplyModifiedProperties();

            BuildStatusCanvas(arSessionGO, planeManager, tapToPlace);

            string path = $"{ScenesFolder}/{SceneRoutes.ARFoundationTest}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void BuildStatusCanvas(GameObject arSessionGO, ARPlaneManager planeManager, ARPlaneTapToPlace tapToPlace)
        {
            var canvasGO = CreateViaMenuOrFallback("GameObject/UI/Canvas", "Canvas", () =>
            {
                var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                if (Object.FindFirstObjectByType<EventSystem>() == null)
                {
                    var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
                    eventSystemGO.AddComponent<StandaloneInputModule>();
#else
                    eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
                }
                return go;
            });

            Selection.activeGameObject = canvasGO;
            var statusTextGO = CreateViaMenuOrFallback("GameObject/UI/Text - TextMeshPro", "StatusText", () =>
            {
                var go = new GameObject("StatusText", typeof(RectTransform));
                go.transform.SetParent(canvasGO.transform, false);
                go.AddComponent<TextMeshProUGUI>();
                return go;
            });
            statusTextGO.name = "StatusText";
            var statusRect = statusTextGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 80f);
            statusRect.sizeDelta = new Vector2(900f, 150f);

            var statusText = statusTextGO.GetComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 42;
            statusText.text = "Getting ready...";

            var statusUI = canvasGO.AddComponent<ARSessionStatusUI>();
            var statusSO = new SerializedObject(statusUI);
            statusSO.FindProperty("arSession").objectReferenceValue = arSessionGO.GetComponent<ARSession>();
            statusSO.FindProperty("planeManager").objectReferenceValue = planeManager;
            statusSO.FindProperty("tapToPlace").objectReferenceValue = tapToPlace;
            statusSO.FindProperty("statusText").objectReferenceValue = statusText;
            statusSO.ApplyModifiedProperties();
        }

        private static GameObject CreateOrLoadPlanePrefab()
        {
            string path = $"{PrefabFolder}/ARDetectedPlane.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var planeGO = new GameObject("ARDetectedPlane");
            planeGO.AddComponent<MeshFilter>();
            var renderer = planeGO.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateUrpMaterial("ARPlaneMaterial", new Color(0.2f, 0.8f, 1f, 0.35f));
            planeGO.AddComponent<ARPlane>();
            planeGO.AddComponent<ARPlaneMeshVisualizer>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(planeGO, path);
            Object.DestroyImmediate(planeGO);
            return prefab;
        }

        private static GameObject CreateOrLoadCharacterPrefab()
        {
            string path = $"{PrefabFolder}/PlaceholderCharacter.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var charGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            charGO.name = "PlaceholderCharacter";
            charGO.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            var collider = charGO.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            charGO.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpMaterial("PlaceholderCharacterMaterial", new Color(1f, 0.75f, 0.3f, 1f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(charGO, path);
            Object.DestroyImmediate(charGO);
            return prefab;
        }

        private static Material CreateUrpMaterial(string name, Color color)
        {
            string path = $"{PrefabFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Prefers Unity's own menu command (handles version-specific wiring like
        // TrackedPoseDriver input actions correctly); falls back to a manual build
        // only if the menu item can't be found by that exact path.
        private static GameObject CreateViaMenuOrFallback(string menuPath, string expectedNameContains, System.Func<GameObject> fallback)
        {
            var previousSelection = Selection.activeGameObject;
            EditorApplication.ExecuteMenuItem(menuPath);

            var created = Selection.activeGameObject;
            if (created != null && created != previousSelection)
            {
                return created;
            }

            Debug.LogWarning($"[SafeTouch] Menu item '{menuPath}' did not produce the expected object; using fallback for '{expectedNameContains}'.");
            return fallback();
        }
    }
}
