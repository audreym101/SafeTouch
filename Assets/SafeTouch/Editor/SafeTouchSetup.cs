using System.Linq;
using SafeTouch.AR;
using SafeTouch.Core;
using SafeTouch.Lessons;
using SafeTouch.Managers;
using SafeTouch.Services;
using SafeTouch.UI;
using SafeTouch.Voice;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// One-click project setup, so the repo carries code rather than
    /// hand-edited scene files:
    ///   SafeTouch > 1. Configure Android Settings
    ///   SafeTouch > 2. Build SafeTouch Scene (uses the model prefabs from step 4 when built)
    /// SafeTouch is a 3D character app: Keza lives in her own 3D world, so no AR
    /// package, camera or XR set-up is needed.
    /// </summary>
    public static class SafeTouchSetup
    {
        const string Root = "Assets/SafeTouch";
        const string ScenePath = Root + "/Scenes/SafeTouch.unity";

        // ---------------------------------------------------------------
        // 1. Player settings
        // ---------------------------------------------------------------

        [MenuItem("SafeTouch/1. Configure Android Settings", priority = 1)]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = "ALU Capstone";
            PlayerSettings.productName = "SafeTouch";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.alu.safetouch");
            PlayerSettings.bundleVersion = "0.1.0";
            // Development builds may reach the API on a laptop over plain http (http://192.168.x.x:3000);
            // release builds need https, as Android requires.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            ApplyAndroidBuildSettings();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("SafeTouch",
                "Android player settings applied: API 26+, IL2CPP ARM64, OpenGL ES 3, portrait." +
                "\n\nNext: File > Build Profiles > Android > Switch Platform, then SafeTouch > 2. Build SafeTouch Scene.", "OK");
        }

        /// <summary>Settings that can be fixed during a build: the build simply carries on with them.</summary>
        public static System.Collections.Generic.List<string> ApplySafeAndroidSettings()
        {
            var changed = new System.Collections.Generic.List<string>();
            // Android 8.0 (API 26) as a minimum (NFR2).
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
            // 64-bit IL2CPP: Google Play requires 64-bit.
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
            // OpenGL ES 3: runs on almost every Android 8 phone, including low-cost ones.
            var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) || apis.Length != 1 || apis[0] != GraphicsDeviceType.OpenGLES3)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
                changed.Add("graphics API -> OpenGL ES 3");
            }
            return changed;
        }

        // ---------------------------------------------------------------
        // 2. Scene, materials and prefabs
        // ---------------------------------------------------------------

        [MenuItem("SafeTouch/2. Build SafeTouch Scene", priority = 2)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Prefabs");

            var opaque = MaterialAsset("SafeTouch Opaque", "Standard", Color.white);
            var transparent = MaterialAsset("SafeTouch Transparent", "Legacy Shaders/Transparent/Diffuse", new Color(1f, 1f, 1f, 0.4f));

            var keza = PersonPrefab("Keza", opaque, Theme.Hex("#F28C38"), 0.75f, true);
            var figure = PersonPrefab("Figure", opaque, Theme.Hex("#8C99AD"), 1f, false);

            // Prefabs from the repo's 3D models (SafeTouch > 4. Build Character Prefabs From Models).
            // Built here when any is missing, so the scene never falls back to the simple shapes by accident.
            bool modelsMissing = new[] { SafeTouchCharacters.KezaPrefabPath, SafeTouchCharacters.ClassmatePrefabPath,
                    SafeTouchCharacters.MumPrefabPath, SafeTouchCharacters.DadPrefabPath }
                .Concat(SafeTouchCharacters.AdultPrefabPaths)
                .Any(p => AssetDatabase.LoadAssetAtPath<GameObject>(p) == null);
            if (modelsMissing) SafeTouchCharacters.BuildAll();
            var kezaModel = AssetDatabase.LoadAssetAtPath<GameObject>(SafeTouchCharacters.KezaPrefabPath);
            if (kezaModel != null) keza = kezaModel;
            // Visitors are the modelled adults when they have been built; the simple figure
            // (capsule and spheres) only stands in when there are none.
            var people = SafeTouchCharacters.AdultPrefabPaths
                .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(p => p != null).ToArray();
            if (people.Length == 0) people = new[] { figure };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // The camera that looks at Keza's 3D world (Stage3D moves it: zoom, orbit, the cinematic moves).
            var camera = CreateCamera();

            // App root with the managers from the class diagram
            var app = new GameObject("SafeTouch App");
            var game = app.AddComponent<GameManager>();
            var ar = app.AddComponent<ARManager>();
            app.AddComponent<AudioSource>();
            var audio = app.AddComponent<AudioManager>();
            var scenarios = app.AddComponent<ScenarioManager>();
            var progress = app.AddComponent<ProgressManager>();
            var cloud = app.AddComponent<CloudSyncService>();
            var consent = app.AddComponent<ConsentService>();
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

            Set(ar, "characterPrefab", keza);
            Set(ar, "figurePrefab", figure);
            SetArray(ar, "figureVariants", people);
            SetRoles(ar, new[] { "mum", "dad", "child" },
                new[] { SafeTouchCharacters.MumPrefabPath, SafeTouchCharacters.DadPrefabPath, SafeTouchCharacters.ClassmatePrefabPath });
            Set(ar, "boyPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SafeTouchCharacters.GanzaPrefabPath));   // empty: Ganza from simple shapes
            SafeTouchCharacters.SetOlder(ar);   // the 9 to 11 characters from downloaded models, if they have been made (SafeTouch > 8)
            Set(ar, "arCamera", camera);
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
            Set(game, "consent", consent);
            SetArray(game, "lessons", lessons);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = app;
            Debug.Log("[SafeTouch] Scene built at " + ScenePath + ". Press Play, or File > Build And Run on an Android phone.");
        }

        /// <summary>A plain camera on a rig; Stage3D places it, sets the sky and moves it during the lessons.</summary>
        static Camera CreateCamera()
        {
            var rig = new GameObject("Camera Rig");
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            return cam;
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
