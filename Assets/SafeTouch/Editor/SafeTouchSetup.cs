using System.Linq;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.Lessons;
using SafeTouch.Managers;
using SafeTouch.Services;
using SafeTouch.UI;
using SafeTouch.Voice;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// One-click project setup, so the repo carries code rather than
    /// hand-edited scene files:
    ///   SafeTouch > 1. Configure Android AR Settings
    ///   SafeTouch > 2. Build SafeTouch Scene (uses the model prefabs from step 4 when built)
    /// </summary>
    public static class SafeTouchSetup
    {
        const string Root = "Assets/SafeTouch";
        const string ScenePath = Root + "/Scenes/SafeTouch.unity";

        // ---------------------------------------------------------------
        // 1. Player and XR settings
        // ---------------------------------------------------------------

        [MenuItem("SafeTouch/1. Configure Android AR Settings", priority = 1)]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = "ALU Capstone";
            PlayerSettings.productName = "SafeTouch";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.alu.safetouch");
            PlayerSettings.bundleVersion = "0.1.0";
            ApplyAndroidBuildSettings();
            AssetDatabase.SaveAssets();

            bool arcore = EnsureXRLoaders();
            string xr = arcore
                ? "Google ARCore (Android) and XR Simulation (Editor) are enabled."
                : "Google ARCore could not be enabled automatically. Tick it on the Android tab of Project Settings > XR Plug-in Management, or the phone will show a black screen.";
            EditorUtility.DisplayDialog("SafeTouch",
                "Android AR player settings applied: API 26+, IL2CPP ARM64, OpenGL ES 3, portrait.\n\n" + xr +
                "\n\nNext: File > Build Profiles > Android > Switch Platform, then SafeTouch > 2. Build SafeTouch Scene.", "OK");
        }

        /// <summary>Settings that can be fixed during a build: the build simply carries on with them.</summary>
        public static System.Collections.Generic.List<string> ApplySafeAndroidSettings()
        {
            var changed = new System.Collections.Generic.List<string>();
            // ARCore needs Android 8.0 (API 26) as a minimum (NFR2); ARCore "Required" refuses anything below 24.
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                changed.Add("minimum Android version -> 8.0 (API 26)");
            }
            if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevelAuto
                && PlayerSettings.Android.targetSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
            {
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                changed.Add("target Android version -> highest installed");
            }
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;   // the UI is laid out for portrait
                changed.Add("orientation -> portrait");
            }
            return changed;
        }

        /// <summary>
        /// Settings a running build has already used: if one is wrong it is
        /// fixed here, but the build must be started again to pick it up.
        /// </summary>
        public static System.Collections.Generic.List<string> ApplyAndroidBuildSettings()
        {
            var changed = ApplySafeAndroidSettings();
            // 64-bit IL2CPP: Google Play requires 64-bit, and ARCore ships ARM64 libraries.
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                changed.Add("scripting backend -> IL2CPP");
            }
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                changed.Add("CPU architecture -> ARM64");
            }
            // OpenGL ES 3: the graphics API ARCore's camera background is built for.
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) || apis.Length != 1 || apis[0] != GraphicsDeviceType.OpenGLES3)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
                changed.Add("graphics API -> OpenGL ES 3");
            }
            return changed;
        }

        public const string ARCoreLoaderType = "UnityEngine.XR.ARCore.ARCoreLoader";
        public const string SimulationLoaderType = "UnityEngine.XR.Simulation.SimulationLoader";

        /// <summary>
        /// Turns on Google ARCore for Android and XR Simulation for the Editor
        /// in XR Plug-in Management, and makes ARCore required. Without the
        /// ARCore loader the app never starts AR: it never asks for the camera
        /// and every AR screen stays black. Safe to call repeatedly; returns
        /// true when ARCore is active for Android.
        /// </summary>
        public static bool EnsureXRLoaders()
        {
            bool arcore = TryAssignLoader(BuildTargetGroup.Android, ARCoreLoaderType);
            bool simulation = TryAssignLoader(BuildTargetGroup.Standalone, SimulationLoaderType);
            RequireARCore();
            AssetDatabase.SaveAssets();
            if (!arcore) Debug.LogError("[SafeTouch] Google ARCore is NOT enabled for Android. Tick it in Project Settings > XR Plug-in Management (Android tab).");
            if (!simulation) Debug.LogWarning("[SafeTouch] XR Simulation is not enabled for the Editor; Play mode will use the virtual floor instead.");
            return arcore;
        }

        /// <summary>True when the ARCore loader is switched on for Android builds.</summary>
        public static bool ARCoreEnabled()
        {
            XRGeneralSettingsPerBuildTarget perTarget;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
            var settings = perTarget != null ? perTarget.SettingsForBuildTarget(BuildTargetGroup.Android) : null;
            return settings != null && settings.Manager != null && settings.InitManagerOnStart
                && settings.Manager.activeLoaders.Any(l => l != null && l.GetType().FullName == ARCoreLoaderType);
        }

        /// <summary>ARCore "Required": the Play Store only offers the app to ARCore phones, and ARCore is installed with it.</summary>
        static void RequireARCore()
        {
            UnityEngine.Object arcoreSettings;
            if (!EditorBuildSettings.TryGetConfigObject("UnityEditor.XR.ARCore.ARCoreSettings", out arcoreSettings) || arcoreSettings == null) return;
            var so = new SerializedObject(arcoreSettings);
            var requirement = so.FindProperty("m_Requirement");
            if (requirement == null) return;
            requirement.enumValueIndex = 0;   // Required
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(arcoreSettings);
        }

        static bool TryAssignLoader(BuildTargetGroup group, string loaderType)
        {
            try
            {
                XRGeneralSettingsPerBuildTarget perTarget;
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget);
                if (perTarget == null)
                {
                    EnsureFolder("Assets/XR");
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
                }
                string path = AssetDatabase.GetAssetPath(perTarget);

                var settings = perTarget.SettingsForBuildTarget(group);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    settings.name = group + " Settings";
                    perTarget.SetSettingsForBuildTarget(group, settings);
                    AssetDatabase.AddObjectToAsset(settings, path);
                }
                if (settings.Manager == null)
                {
                    var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                    manager.name = group + " Providers";
                    settings.Manager = manager;
                    AssetDatabase.AddObjectToAsset(manager, path);
                }
                settings.InitManagerOnStart = true;
                // AssignLoader returns false when the loader is already on, so check the result instead.
                if (!settings.Manager.activeLoaders.Any(l => l != null && l.GetType().FullName == loaderType))
                    XRPackageMetadataStore.AssignLoader(settings.Manager, loaderType, group);
                EditorUtility.SetDirty(settings.Manager);
                EditorUtility.SetDirty(settings);
                EditorUtility.SetDirty(perTarget);
                return settings.Manager.activeLoaders.Any(l => l != null && l.GetType().FullName == loaderType);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SafeTouch] Could not enable " + loaderType + " automatically: " + e.Message);
                return false;
            }
        }

        // ---------------------------------------------------------------
        // 2. Scene, materials and prefabs
        // ---------------------------------------------------------------

        [MenuItem("SafeTouch/2. Build SafeTouch Scene", priority = 2)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureXRLoaders();   // without the ARCore loader the phone shows a black screen
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Prefabs");

            var opaque = MaterialAsset("SafeTouch Opaque", "Standard", Color.white);
            var transparent = MaterialAsset("SafeTouch Transparent", "Legacy Shaders/Transparent/Diffuse", new Color(1f, 1f, 1f, 0.4f));
            // Unlit see-through material for the surface rectangles and the reticle; they tint it themselves.
            var planeMat = MaterialAsset("AR Surface", "Sprites/Default", Color.white);

            var keza = PersonPrefab("Keza", opaque, Theme.Hex("#F28C38"), 0.75f, true);
            var figure = PersonPrefab("Figure", opaque, Theme.Hex("#8C99AD"), 1f, false);

            // Prefabs from the repo's 3D models (SafeTouch > 4. Build Character Prefabs From Models), when built.
            var kezaModel = AssetDatabase.LoadAssetAtPath<GameObject>(SafeTouchCharacters.KezaPrefabPath);
            if (kezaModel != null) keza = kezaModel;
            var people = SafeTouchCharacters.AdultPrefabPaths
                .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(p => p != null)
                .Concat(new[] { figure }).ToArray();
            var reticle = ReticlePrefab(planeMat);
            var planePrefab = PlanePrefab(planeMat);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // AR Session and XR Origin (camera, plane detection, raycasts, anchors)
            var session = Object.FindFirstObjectByType<ARSession>();
            if (session == null) session = new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager)).GetComponent<ARSession>();
            // AR starts only after the child has seen why SafeTouch needs the camera and said yes (ARManager.BeginPlacement).
            session.enabled = false;
            var origin = CreateOrigin();
            var planes = GetOrAdd<ARPlaneManager>(origin.gameObject);
            planes.planePrefab = planePrefab;
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            var raycasts = GetOrAdd<ARRaycastManager>(origin.gameObject);
            GetOrAdd<ARAnchorManager>(origin.gameObject);

            // App root with the managers from the class diagram
            var app = new GameObject("SafeTouch App");
            var game = app.AddComponent<GameManager>();
            var ar = app.AddComponent<ARManager>();
            app.AddComponent<AudioSource>();
            var audio = app.AddComponent<AudioManager>();
            var scenarios = app.AddComponent<ScenarioManager>();
            var progress = app.AddComponent<ProgressManager>();
            var cloud = app.AddComponent<CloudSyncService>();
            // Unity adds the RECORD_AUDIO permission to the Android manifest because of the Microphone API.
            var mic = app.AddComponent<MicrophoneInput>();
            var voice = app.AddComponent<VoiceCoach>();
            Set(voice, "microphone", mic);

            var lessonRoot = new GameObject("Lessons");
            lessonRoot.transform.SetParent(app.transform);
            var lessons = new LessonBase[]
            {
                lessonRoot.AddComponent<PersonalSpaceBubbleLesson>(),
                lessonRoot.AddComponent<ScenarioLesson>(),
                lessonRoot.AddComponent<StopFrameworkLesson>(),
                lessonRoot.AddComponent<TrustedAdultsLesson>(),
                lessonRoot.AddComponent<ReviewLesson>(),
                lessonRoot.AddComponent<BubbleGuardGame>(),
            };

            var reticleInstance = (GameObject)PrefabUtility.InstantiatePrefab(reticle);
            reticleInstance.SetActive(false);

            Set(ar, "characterPrefab", keza);
            Set(ar, "figurePrefab", figure);
            SetArray(ar, "figureVariants", people);
            SetRoles(ar, new[] { "mum", "dad" }, new[] { SafeTouchCharacters.MumPrefabPath, SafeTouchCharacters.DadPrefabPath });
            Set(ar, "reticle", reticleInstance.transform);
            Set(ar, "raycastManager", raycasts);
            Set(ar, "planeManager", planes);
            Set(ar, "session", session);
            Set(ar, "arCamera", origin.Camera);
            Set(ar, "opaqueMaterial", opaque);
            Set(ar, "transparentMaterial", transparent);

            // UI canvas (screens are built by UIManager at runtime)
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(app.transform);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;   // ResponsiveCanvas switches to height on tablets
            canvasGo.AddComponent<ResponsiveCanvas>();
            var ui = canvasGo.AddComponent<UIManager>();
            Set(ui, "canvas", canvas);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            Set(game, "arManager", ar);
            Set(game, "uiManager", ui);
            Set(game, "audioManager", audio);
            Set(game, "scenarioManager", scenarios);
            Set(game, "progressManager", progress);
            Set(game, "cloudSync", cloud);
            Set(game, "voiceCoach", voice);
            SetArray(game, "lessons", lessons);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = app;
            Debug.Log("[SafeTouch] Scene built at " + ScenePath + ". Press Play (XR Simulation) or File > Build And Run on an ARCore phone.");
        }

        static XROrigin CreateOrigin()
        {
            // Prefer AR Foundation's own menu item; fall back to building it by hand.
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");
            var origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin != null) return origin;

            var root = new GameObject("XR Origin");
            origin = root.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(root.transform, false);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ARCameraManager), typeof(ARCameraBackground));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;

            var pose = camGo.AddComponent<TrackedPoseDriver>();
            pose.positionInput = new InputActionProperty(new InputAction("Position", binding: "<HandheldARInputDevice>/devicePosition"));
            pose.rotationInput = new InputActionProperty(new InputAction("Rotation", binding: "<HandheldARInputDevice>/deviceRotation"));

            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;
            return origin;
        }

        static GameObject PersonPrefab(string name, Material mat, Color outfit, float scale, bool isGuide)
        {
            var root = new GameObject(name);
            ARFigure component = isGuide ? root.AddComponent<ARCharacter>() : root.AddComponent<ARFigure>();

            var skin = Theme.Hex("#8D5524");
            var body = Part(root, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.25f, 0f), new Vector3(0.2f, 0.25f, 0.2f), mat, outfit);
            Part(root, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.6f, 0f), Vector3.one * 0.2f, mat, skin);
            Part(root, PrimitiveType.Sphere, "Eye L", new Vector3(-0.04f, 0.62f, 0.09f), Vector3.one * 0.03f, mat, Theme.Ink);
            Part(root, PrimitiveType.Sphere, "Eye R", new Vector3(0.04f, 0.62f, 0.09f), Vector3.one * 0.03f, mat, Theme.Ink);

            // One collider for the whole person, so a tap in the camera view can hit them (Memory Quest chase).
            var hitBox = root.AddComponent<CapsuleCollider>();
            hitBox.center = new Vector3(0f, 0.38f, 0f);
            hitBox.height = 0.8f;
            hitBox.radius = 0.15f;

            // Skin tone: finds the "Head" part; Keza takes the child's pick, visitors a mix of tones.
            var skinTone = root.AddComponent<SkinToneApplier>();
            Set(skinTone, "sparkleMaterial", mat);

            var labelGo = new GameObject("Name label", typeof(TextMesh), typeof(Billboard));
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            var label = labelGo.GetComponent<TextMesh>();
            label.text = name;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = 0.01f;
            label.color = Theme.Ink;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;

            root.transform.localScale = Vector3.one * scale;
            SetArray(component, "tintedParts", isGuide ? new Renderer[0] : new[] { body.GetComponent<Renderer>() });
            Set(component, "nameLabel", label);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var m = MaterialAsset("Person " + name + " " + ColorUtility.ToHtmlStringRGB(color), mat.shader.name, color);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>The square that shows where Keza will stand (PlacementReticle draws it).</summary>
        static GameObject ReticlePrefab(Material surface)
        {
            var go = new GameObject("Placement Reticle");
            var square = go.AddComponent<PlacementReticle>();
            Set(square, "material", surface);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/Placement Reticle.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Each detected floor or table, drawn as a smooth rectangle with a grid (PlaneVisual).</summary>
        static GameObject PlanePrefab(Material surface)
        {
            var go = new GameObject("AR Plane", typeof(ARPlane), typeof(PlaneVisual));
            Set(go.GetComponent<PlaneVisual>(), "material", surface);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/AR Plane.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Material MaterialAsset(string name, string shaderName, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find(shaderName) ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError("[SafeTouch] No field " + field + " on " + target); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError("[SafeTouch] No field " + field + " on " + target); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills ARManager's role list with the prefabs that have been built (missing ones are skipped).</summary>
        static void SetRoles(ARManager ar, string[] roles, string[] prefabPaths)
        {
            var so = new SerializedObject(ar);
            var prop = so.FindProperty("rolePrefabs");
            prop.arraySize = 0;
            for (int i = 0; i < roles.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null) continue;
                prop.arraySize++;
                var element = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                element.FindPropertyRelative("role").stringValue = roles[i];
                element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
