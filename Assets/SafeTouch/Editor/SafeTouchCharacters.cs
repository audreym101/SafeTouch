using System.IO;
using System.Linq;
using SafeTouch.AR;
using SafeTouch.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// SafeTouch > 4. Build Character Prefabs From Models. Turns the models
    /// in Assets/SafeTouch/Models into AR-ready prefabs:
    ///   Keza    - the Mixamo child, with a skin mask so her skin tone can
    ///             change while her clothes stay the same, and her greeting
    ///             wave for "Bounce".
    ///   Adults  - the two people in the Eve model, split into two visitors
    ///             and helpers whose "RealismBody" skin takes the palette.
    ///   Props   - the playground and the school desk, at table-top size,
    ///             in Resources/Props for the Memory Quest chase.
    /// Run it before SafeTouch > 2. Build SafeTouch Scene, which then uses
    /// these prefabs instead of the simple shapes. Everything it writes can
    /// be regenerated, so only this script is kept in git.
    /// The "character (1)" model (a man with a cigar) is not used: it is not
    /// suitable for a children's safety app.
    /// </summary>
    public static class SafeTouchCharacters
    {
        const string Models = SafeTouchModels.ModelsFolder;
        const string KezaModel = Models + "/Ch46_nonPBR@Standing Greeting.fbx";
        const string KezaDiffuse = Models + "/Textures/ch46-nonpbr-standing-greeting/Ch46_1001_Diffuse.png";
        const string AdultsModel = Models + "/haveto-the-parent-of-the-glasses/source/Haveto1TheGlassesPublish1.fbx";
        const string PlaygroundModel = Models + "/playground/source/child.fbx";
        const string DeskModel = Models + "/school-chair/source/Silla.fbx";

        const string PrefabFolder = "Assets/SafeTouch/Prefabs";
        const string GeneratedFolder = "Assets/SafeTouch/Generated";
        const string PropsFolder = "Assets/SafeTouch/Resources/Props";
        const string SparkleMaterial = "Assets/SafeTouch/Materials/SafeTouch Opaque.mat";

        public const string KezaPrefabPath = PrefabFolder + "/Keza Model.prefab";
        public static readonly string[] AdultPrefabPaths = { PrefabFolder + "/Adult 1.prefab", PrefabFolder + "/Adult 2.prefab" };

        // Heights in metres for the table-top AR scale, matching the simple figures.
        const float KezaHeight = 0.55f;
        const float AdultHeight = 0.78f;

        [MenuItem("SafeTouch/4. Build Character Prefabs From Models", priority = 4)]
        public static void BuildAll()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(GeneratedFolder);
            EnsureFolder(PropsFolder);
            var sparkle = AssetDatabase.LoadAssetAtPath<Material>(SparkleMaterial);

            BuildKeza(sparkle);
            BuildAdults(sparkle);
            BuildProp(PlaygroundModel, "playground", 1.1f, true);
            BuildProp(DeskModel, "school", 0.32f, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[SafeTouch] Character prefabs built. Now run SafeTouch > 2. Build SafeTouch Scene to use them.");
        }

        // ---------------------------------------------------------------
        // Keza
        // ---------------------------------------------------------------

        static void BuildKeza(Material sparkle)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(KezaModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] Keza's model is missing: " + KezaModel); return; }
            if (!File.Exists(KezaDiffuse)) SafeTouchModels.ExtractTextures(KezaModel);

            Color painted;
            var mask = BuildSkinMask(KezaDiffuse, GeneratedFolder + "/Keza Skin Mask.png", out painted);
            var material = new Material(Shader.Find("SafeTouch/Skin Tone"));
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(KezaDiffuse));
            material.SetTexture("_SkinMask", mask);
            material.SetColor("_SkinReference", painted);
            material.SetColor("_SkinColor", SkinTones.Get(SkinTones.DefaultId).Color);
            material = SaveAsset(material, GeneratedFolder + "/Keza Skin Tone.mat");

            var root = new GameObject("Keza");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            body.name = "Model";
            body.transform.SetParent(root.transform, false);
            foreach (var r in body.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();

            var animator = body.GetComponent<Animator>();
            if (animator == null) animator = body.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = GreetingController(KezaModel);

            var size = FitHeight(root, body, KezaHeight);
            AddPersonParts(root, size, "Keza", true, sparkle, new Renderer[0]);
            Save(root, KezaPrefabPath);
        }

        /// <summary>
        /// Marks the skin in a character texture: pixels that are warm (red
        /// above green and blue) with green and blue close together. That
        /// takes in pink or tan skin, lips and blush, and leaves out the
        /// lilac top, magenta shorts, orange boots, cream socks and teal hair.
        /// painted returns the average skin colour, which the shader uses to
        /// keep the texture's light and shade.
        /// </summary>
        public static Texture2D BuildSkinMask(string diffusePath, string maskPath, out Color painted)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(diffusePath));
            const int size = 1024;
            int stepX = Mathf.Max(1, source.width / size), stepY = Mathf.Max(1, source.height / size);
            var pixels = source.GetPixels32();
            var mask = new Color32[size * size];
            double rs = 0, gs = 0, bs = 0;
            int skinCount = 0;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = pixels[Mathf.Min(source.height - 1, y * stepY) * source.width + Mathf.Min(source.width - 1, x * stepX)];
                float w = SkinWeight(p);
                byte v = (byte)(w * 255f);
                mask[y * size + x] = new Color32(v, v, v, 255);
                if (w > 0.9f) { rs += p.r; gs += p.g; bs += p.b; skinCount++; }
            }
            painted = skinCount > 0
                ? new Color((float)(rs / skinCount / 255.0), (float)(gs / skinCount / 255.0), (float)(bs / skinCount / 255.0))
                : new Color(0.95f, 0.69f, 0.69f);
            Object.DestroyImmediate(source);

            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            texture.SetPixels32(mask);
            texture.Apply();
            File.WriteAllBytes(maskPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(maskPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(maskPath);
            importer.sRGBTexture = false;   // a mask is data, not colour
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            Debug.Log(string.Format("[SafeTouch] Skin mask {0}: {1:0.0}% of the texture is skin", maskPath, 100f * skinCount / (size * size)));
            return AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        }

        static float SkinWeight(Color32 p)
        {
            if (p.a < 128) return 0f;
            float warm = (p.r - Mathf.Max(p.g, p.b) - 15f) / 20f;   // redder than it is green or blue
            float even = 1f - (Mathf.Abs(p.g - p.b) - 15f) / 15f;    // green and blue close: not purple, orange or magenta
            float light = (p.r - 110f) / 30f;                       // not a dark outline
            return Mathf.Clamp01(Mathf.Min(warm, Mathf.Min(even, light)));
        }

        /// <summary>
        /// Idle holds the first frame of the greeting (a relaxed standing
        /// pose, not the T-pose); the Greet trigger plays the wave once.
        /// </summary>
        static RuntimeAnimatorController GreetingController(string modelPath)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) return null;

            string path = GeneratedFolder + "/Keza Greeting.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(ARFigure.GreetTrigger, AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle");
            idle.motion = clip;
            idle.speed = 0f;
            var greet = machine.AddState("Greet");
            greet.motion = clip;
            machine.defaultState = idle;

            var toGreet = idle.AddTransition(greet);
            toGreet.AddCondition(AnimatorConditionMode.If, 0f, ARFigure.GreetTrigger);
            toGreet.hasExitTime = false;
            toGreet.duration = 0.1f;
            var back = greet.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.2f;
            return controller;
        }

        // ---------------------------------------------------------------
        // Adults
        // ---------------------------------------------------------------

        /// <summary>The Eve model holds two people side by side; each becomes its own prefab.</summary>
        static void BuildAdults(Material sparkle)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AdultsModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] Adults model is missing: " + AdultsModel); return; }

            for (int side = 0; side < AdultPrefabPaths.Length; side++)
            {
                var root = new GameObject("Adult " + (side + 1));
                var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                body.name = "Model";
                body.transform.SetParent(root.transform, false);

                var all = body.GetComponentsInChildren<Renderer>(true);
                var whole = SafeTouchModels.Bounds(all);
                float split = whole.center.x;
                foreach (var r in all)
                {
                    var b = r.bounds;
                    bool ground = b.size.y < whole.size.y * 0.02f && b.size.x > whole.size.x * 0.3f;   // shadow or floor plane
                    bool mine = (b.center.x < split) == (side == 0);
                    r.enabled = mine && !ground;
                }

                var size = FitHeight(root, body, AdultHeight);
                var clothes = body.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.enabled && r.sharedMaterials.Any(m => m != null && m.name.ToLowerInvariant().Contains("fabric")))
                    .ToArray();
                AddPersonParts(root, size, "Visitor", false, sparkle, clothes);
                Save(root, AdultPrefabPaths[side]);
            }
        }

        // ---------------------------------------------------------------
        // Props
        // ---------------------------------------------------------------

        /// <summary>Scales a scenery model to fit the room: by width for the playground, by height for the desk.</summary>
        static void BuildProp(string modelPath, string propName, float targetSize, bool byWidth)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) { Debug.LogWarning("[SafeTouch] Prop model is missing: " + modelPath); return; }
            var root = new GameObject(propName);
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            body.transform.SetParent(root.transform, false);
            foreach (var c in body.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            var b = SafeTouchModels.Bounds(body.GetComponentsInChildren<Renderer>());
            float current = byWidth ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
            float scale = targetSize / Mathf.Max(current, 0.0001f);
            body.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            root.transform.localScale = Vector3.one * scale;
            Save(root, PropsFolder + "/" + propName + ".prefab");
        }

        // ---------------------------------------------------------------
        // Preview
        // ---------------------------------------------------------------

        /// <summary>
        /// Renders Keza in every palette tone side by side to
        /// docs/designs/skin-tones.png, to check the skin mask (clothes, hair
        /// and eyes must not change) and for the report.
        /// </summary>
        [MenuItem("SafeTouch/5. Render Skin Tone Preview", priority = 5)]
        public static void RenderSkinTonePreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KezaPrefabPath);
            if (prefab == null) { Debug.LogWarning("[SafeTouch] Build the character prefabs first."); return; }

            const float spacing = 0.42f;
            var people = new GameObject[SkinTones.All.Length];
            for (int i = 0; i < people.Length; i++)
            {
                people[i] = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                people[i].transform.position = new Vector3((i - (people.Length - 1) * 0.5f) * spacing, 0f, 0f);
                people[i].transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // face the camera
                people[i].GetComponent<SkinToneApplier>().Apply(SkinTones.All[i].Color);
                var label = people[i].GetComponentInChildren<TextMesh>();
                if (label != null)
                {
                    label.text = SkinTones.All[i].english;
                    label.transform.rotation = Quaternion.identity;   // Billboard only runs in play mode
                }
            }

            var light = new GameObject("Preview light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(35f, 200f, 0f);
            var cam = new GameObject("Preview camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.918f, 0.957f, 0.984f);
            cam.orthographic = true;
            cam.orthographicSize = 0.44f;
            cam.transform.position = new Vector3(0f, 0.32f, -3f);
            cam.transform.LookAt(new Vector3(0f, 0.32f, 0f));

            const int width = 1800, height = 600;
            var target = new RenderTexture(width, height, 24);
            cam.targetTexture = target;
            cam.aspect = (float)width / height;
            cam.Render();
            RenderTexture.active = target;
            var png = new Texture2D(width, height, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            png.Apply();
            Directory.CreateDirectory("docs/designs");
            File.WriteAllBytes("docs/designs/skin-tones.png", png.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(png);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(light.gameObject);
            foreach (var p in people) Object.DestroyImmediate(p);
            Debug.Log("[SafeTouch] Wrote docs/designs/skin-tones.png");
        }

        /// <summary>Batch-mode entry: build the prefabs, then render the tone preview.</summary>
        public static void BuildAndPreview()
        {
            BuildAll();
            RenderSkinTonePreview();
        }

        // ---------------------------------------------------------------
        // Shared
        // ---------------------------------------------------------------

        /// <summary>Scales root so the visible model is height tall, feet on the floor, centred. Returns the unscaled size.</summary>
        static Vector3 FitHeight(GameObject root, GameObject body, float height)
        {
            var b = SafeTouchModels.Bounds(body.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray());
            body.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            root.transform.localScale = Vector3.one * (height / Mathf.Max(b.size.y, 0.0001f));
            return b.size;
        }

        /// <summary>The same parts the simple figures have: behaviour, tap collider, name label and skin tone.</summary>
        static void AddPersonParts(GameObject root, Vector3 size, string label, bool isGuide, Material sparkle, Renderer[] tinted)
        {
            ARFigure figure = isGuide ? root.AddComponent<ARCharacter>() : root.AddComponent<ARFigure>();
            float scale = root.transform.localScale.y;

            var hitBox = root.AddComponent<CapsuleCollider>();
            hitBox.center = new Vector3(0f, size.y * 0.5f, 0f);
            hitBox.height = size.y;
            hitBox.radius = Mathf.Max(size.x, size.z) * 0.3f;

            var labelGo = new GameObject("Name label", typeof(TextMesh), typeof(Billboard));
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, size.y + 0.08f / scale, 0f);
            var text = labelGo.GetComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.0075f / scale;   // same printed size as on the simple figures
            text.color = new Color(0.12f, 0.16f, 0.27f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;

            var skin = root.AddComponent<SkinToneApplier>();
            SetField(skin, "sparkleMaterial", sparkle);
            SetArray(figure, "tintedParts", tinted);
            SetField(figure, "nameLabel", text);
        }

        static T SaveAsset<T>(T asset, string path) where T : Object
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void Save(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[SafeTouch] Saved " + path);
        }

        static void SetField(Object target, string field, Object value)
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

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
