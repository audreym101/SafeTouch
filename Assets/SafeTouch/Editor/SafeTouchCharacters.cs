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
    ///   Mum/Dad - from the parents-and-dog model, with skin masks so they
    ///             can match Keza's tone (Keza's family).
    ///   Classmate - Keza's model again, child-sized, in a school uniform
    ///             (dark hair, blue shirt, navy shorts), so the children in
    ///             the lessons are children and not adults or simple shapes.
    ///   Props   - the playground, school desk, chalkboard, family dog and a
    ///             merged grass patch, at table-top size,
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
        const string ChalkboardModel = Models + "/chalk-board/source/MenuBoard.fbx";

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
            BuildClassmate(sparkle);
            BuildAdults(sparkle);
            BuildProp(PlaygroundModel, "playground", 1.1f, true);
            BuildProp(DeskModel, "school", 0.32f, false);
            BuildProp(ChalkboardModel, "chalkboard", 0.45f, false);
            BuildFamily(sparkle);
            BuildGrass();
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
        // Classmate
        // ---------------------------------------------------------------

        public const string ClassmatePrefabPath = PrefabFolder + "/Classmate.prefab";

        /// <summary>
        /// A second child for the classmate and other children in the lessons:
        /// Keza's model and greeting wave with her clothes and hair recoloured,
        /// so the child can tell them apart. The skin is left as painted, so
        /// Keza's skin mask still applies and the classmate gets their own tone.
        /// </summary>
        static void BuildClassmate(Material sparkle)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(KezaModel);
            var kezaMaterial = AssetDatabase.LoadAssetAtPath<Material>(GeneratedFolder + "/Keza Skin Tone.mat");
            var greeting = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(GeneratedFolder + "/Keza Greeting.controller");
            if (model == null || kezaMaterial == null || !File.Exists(KezaDiffuse))
            {
                Debug.LogWarning("[SafeTouch] Build Keza first: the classmate is made from her model.");
                return;
            }

            var material = new Material(kezaMaterial);
            material.SetTexture("_MainTex", SchoolUniform(KezaDiffuse, GeneratedFolder + "/Classmate Diffuse.png"));
            material = SaveAsset(material, GeneratedFolder + "/Classmate Skin Tone.mat");

            var root = new GameObject("Classmate");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            body.name = "Model";
            body.transform.SetParent(root.transform, false);
            foreach (var r in body.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();

            var animator = body.GetComponent<Animator>();
            if (animator == null) animator = body.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = greeting;

            var size = FitHeight(root, body, KezaHeight * 0.97f);   // about Keza's height: a child of her age
            AddPersonParts(root, size, "Classmate", false, sparkle, new Renderer[0]);
            var keep = new SerializedObject(root.GetComponent<ARFigure>());
            keep.FindProperty("keepOwnColours").boolValue = true;   // the uniform stays; only the skin takes a tone
            keep.ApplyModifiedPropertiesWithoutUndo();
            Save(root, ClassmatePrefabPath);
        }

        /// <summary>
        /// Recolours Keza's texture into a school uniform: teal hair becomes
        /// dark brown, the lilac top a light blue shirt, the magenta shorts
        /// navy and the orange boots dark shoes. Skin (SkinWeight) is kept.
        /// </summary>
        static Texture2D SchoolUniform(string diffusePath, string outPath)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(diffusePath));
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (SkinWeight(p) > 0.5f) continue;
                float h, s, v;
                Color.RGBToHSV(p, out h, out s, out v);
                Color c;
                if (h > 0.42f && h < 0.58f && s > 0.2f) c = Color.HSVToRGB(0.07f, 0.5f, v * 0.22f + 0.03f);        // hair
                else if (h > 0.78f && h < 0.97f && s >= 0.4f) c = Color.HSVToRGB(0.61f, 0.6f, v * 0.5f);            // shorts
                else if (h > 0.7f && h < 0.99f && s < 0.4f && s > 0.04f) c = Color.HSVToRGB(0.57f, 0.38f, Mathf.Min(1f, v * 0.97f));   // top
                else if (h > 0.03f && h < 0.14f && s > 0.45f) c = Color.HSVToRGB(0.08f, 0.35f, v * 0.3f);           // boots
                else continue;
                c.a = p.a / 255f;
                pixels[i] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(outPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
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
        // Mum, Dad and the dog
        // ---------------------------------------------------------------

        const string FamilyModel = Models + "/js-parents-and-dog/source/d.fbx";
        public const string MumPrefabPath = PrefabFolder + "/Mum.prefab";
        public const string DadPrefabPath = PrefabFolder + "/Dad.prefab";

        /// <summary>
        /// The family model holds a father, a mother and a dog, each made of
        /// several part meshes named "...DadJ:...", "...MomJ:..." and
        /// "...Perro...". Mum and Dad become helpers whose skin can match
        /// Keza's tone; the dog becomes a prop for the home scene.
        /// </summary>
        static void BuildFamily(Material sparkle)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FamilyModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] Family model is missing: " + FamilyModel); return; }

            // Parts that show skin, and the parts that are only skin (used to sample the painted skin colour).
            BuildParent(model, "MomJ", "Mum", MumPrefabPath, new[] { "Cabeza", "Mano" }, new[] { "Mano" }, sparkle);
            BuildParent(model, "DadJ", "Dad", DadPrefabPath, new[] { "Cabeza", "Brazo", "Pierna" }, new[] { "Brazo" }, sparkle);

            var dog = new GameObject("dog");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            body.transform.SetParent(dog.transform, false);
            foreach (var r in body.GetComponentsInChildren<Renderer>(true)) r.enabled = r.name.Contains("Perro");
            FitHeight(dog, body, 0.2f);
            Save(dog, PropsFolder + "/dog.prefab");
        }

        static void BuildParent(GameObject model, string key, string label, string path, string[] skinParts, string[] pureSkinParts, Material sparkle)
        {
            var root = new GameObject(label);
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            body.name = "Model";
            body.transform.SetParent(root.transform, false);
            var mine = body.GetComponentsInChildren<Renderer>(true).Where(r => r.name.Contains(key)).ToArray();
            foreach (var r in body.GetComponentsInChildren<Renderer>(true)) r.enabled = mine.Contains(r);

            var texture = mine.Select(r => r.sharedMaterial != null ? r.sharedMaterial.mainTexture as Texture2D : null).FirstOrDefault(t => t != null);
            if (texture != null)
            {
                Color painted;
                var mask = BuildIslandSkinMask(AssetDatabase.GetAssetPath(texture),
                    MeshesOf(mine, skinParts), MeshesOf(mine, pureSkinParts),
                    GeneratedFolder + "/" + label + " Skin Mask.png", out painted);
                var material = new Material(Shader.Find("SafeTouch/Skin Tone"));
                material.SetTexture("_MainTex", texture);
                material.SetTexture("_SkinMask", mask);
                material.SetColor("_SkinReference", painted);
                material.SetColor("_SkinColor", painted);
                material = SaveAsset(material, GeneratedFolder + "/" + label + " Skin Tone.mat");
                foreach (var r in mine)
                {
                    // Dad's lens meshes import as opaque dark discs (sunglasses); hide them and keep the frames.
                    bool lens = r.sharedMaterials.Any(m => m != null && m.name.Contains("Trans"));
                    if (lens) { r.enabled = false; continue; }
                    // Only the skin parts get the tone. Clothes keep their own material even
                    // where they share texture pixels with the neck (Mum's shirt does).
                    if (skinParts.Any(p => r.name.Contains(p)))
                        r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                }
            }

            var size = FitHeight(root, body, AdultHeight);
            AddPersonParts(root, size, label, false, sparkle, new Renderer[0]);
            var keep = new SerializedObject(root.GetComponent<ARFigure>());
            keep.FindProperty("keepOwnColours").boolValue = true;
            keep.ApplyModifiedPropertiesWithoutUndo();
            Save(root, path);
        }

        static Mesh[] MeshesOf(Renderer[] renderers, string[] partNames)
        {
            return renderers.Where(r => partNames.Any(p => r.name.Contains(p)))
                .Select(r => r is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)r).sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null)
                .Where(m => m != null).ToArray();
        }

        /// <summary>
        /// A skin mask for a texture that also holds hair, a beard and clothes.
        /// Only texture areas used by the skin parts can be skin, and within
        /// them a pixel counts when its hue, saturation and brightness are
        /// close to the skin painted on the arms or hands. That keeps the
        /// beard (too dark), light hair (too saturated), teeth and eyes (too
        /// white) and lips (too red) as they are.
        /// </summary>
        public static Texture2D BuildIslandSkinMask(string texturePath, Mesh[] skinMeshes, Mesh[] pureSkinMeshes, string maskPath, out Color painted)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(texturePath));
            int w = source.width, h = source.height;
            var pixels = source.GetPixels32();
            Object.DestroyImmediate(source);

            var skinArea = Islands(skinMeshes, w, h);
            var pureArea = Islands(pureSkinMeshes, w, h);

            // The painted skin colour: the average of the arm or hand pixels.
            double rs = 0, gs = 0, bs = 0; int n = 0;
            for (int i = 0; i < pixels.Length; i++)
                if (pureArea[i] && pixels[i].a > 128) { rs += pixels[i].r; gs += pixels[i].g; bs += pixels[i].b; n++; }
            painted = n > 0 ? new Color((float)(rs / n / 255), (float)(gs / n / 255), (float)(bs / n / 255)) : new Color(0.85f, 0.65f, 0.57f);
            float refH, refS, refV;
            Color.RGBToHSV(painted, out refH, out refS, out refV);

            var weights = new float[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                if (!skinArea[i]) continue;
                float hue, sat, val;
                Color.RGBToHSV(pixels[i], out hue, out sat, out val);
                float dh = Mathf.Abs(hue - refH); dh = Mathf.Min(dh, 1f - dh);
                float byHue = 1f - Mathf.Clamp01((dh - 0.03f) / 0.04f);
                float bySat = 1f - Mathf.Clamp01((Mathf.Abs(sat - refS) - 0.08f) / 0.08f);
                float byVal = Mathf.Clamp01((val / Mathf.Max(refV, 0.01f) - 0.5f) / 0.15f);
                weights[i] = Mathf.Min(byHue, Mathf.Min(bySat, byVal));
            }

            // Smooth, then sharpen: scattered specks (strands of hair, a beard)
            // fade away, small holes in a face (shadowed eyelids) fill in, and
            // edges become soft instead of pixel-sharp.
            weights = BoxBlur(BoxBlur(weights, w, h, 4), w, h, 4);
            var mask = new Color32[w * h];
            int skin = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                float weight = skinArea[i] ? Mathf.Clamp01((weights[i] - 0.4f) / 0.3f) : 0f;
                byte v = (byte)(weight * 255f);
                mask[i] = new Color32(v, v, v, 255);
                if (weight > 0.9f) skin++;
            }

            var texture = new Texture2D(w, h, TextureFormat.RGB24, false);
            texture.SetPixels32(mask);
            texture.Apply();
            File.WriteAllBytes(maskPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(maskPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(maskPath);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            Debug.Log(string.Format("[SafeTouch] Skin mask {0}: {1:0.0}% of the texture is skin", maskPath, 100f * skin / (w * h)));
            return AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        }

        /// <summary>A separable box blur of the given radius (in pixels) over a w x h grid.</summary>
        static float[] BoxBlur(float[] src, int w, int h, int radius)
        {
            var tmp = new float[src.Length];
            var dst = new float[src.Length];
            float norm = 1f / (2 * radius + 1);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++) sum += src[y * w + Mathf.Clamp(x + k, 0, w - 1)];
                tmp[y * w + x] = sum * norm;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++) sum += tmp[Mathf.Clamp(y + k, 0, h - 1) * w + x];
                dst[y * w + x] = sum * norm;
            }
            return dst;
        }

        /// <summary>Marks every texture pixel covered by the meshes' UV triangles.</summary>
        static bool[] Islands(Mesh[] meshes, int w, int h)
        {
            var covered = new bool[w * h];
            foreach (var mesh in meshes)
            {
                var uv = mesh.uv;
                if (uv == null || uv.Length == 0) continue;
                var tris = mesh.triangles;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    Vector2 a = Wrap(uv[tris[t]]) * new Vector2(w, h), b = Wrap(uv[tris[t + 1]]) * new Vector2(w, h), c = Wrap(uv[tris[t + 2]]) * new Vector2(w, h);
                    int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                    int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)))), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                    float area = Edge(a, b, c);
                    if (Mathf.Abs(area) < 1e-6f) continue;
                    for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float w0 = Edge(b, c, p) / area, w1 = Edge(c, a, p) / area, w2 = Edge(a, b, p) / area;
                        if (w0 >= -0.01f && w1 >= -0.01f && w2 >= -0.01f) covered[y * w + x] = true;
                    }
                }
            }
            return covered;
        }

        static Vector2 Wrap(Vector2 uv)
        {
            return new Vector2(uv.x - Mathf.Floor(uv.x), uv.y - Mathf.Floor(uv.y));
        }

        static float Edge(Vector2 a, Vector2 b, Vector2 p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        // ---------------------------------------------------------------
        // Grass
        // ---------------------------------------------------------------

        const string GrassModel = Models + "/patch-of-grass/source/Grass.fbx";

        /// <summary>
        /// The grass model is 9,437 separate blades on a 2 m patch, far too many
        /// draw calls for a phone. This keeps a round patch from the middle,
        /// every second blade, and merges them into one mesh (about 7,000
        /// vertices) about 0.6 m across. The soil plane is dropped: the real
        /// floor shows through in AR.
        /// </summary>
        static void BuildGrass()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(GrassModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] Grass model is missing: " + GrassModel); return; }
            var instance = Object.Instantiate(model);
            var whole = SafeTouchModels.Bounds(instance.GetComponentsInChildren<Renderer>());
            float keepRadius = whole.extents.x * 0.45f;

            var parts = new System.Collections.Generic.List<CombineInstance>();
            Material grassMaterial = null;
            int index = 0, vertices = 0;
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                var r = filter.GetComponent<Renderer>();
                if (filter.sharedMesh == null || r == null || r.sharedMaterial == null || r.sharedMaterial.name.Contains("Soil")) continue;
                var offset = r.bounds.center - whole.center;
                if (new Vector2(offset.x, offset.z).magnitude > keepRadius || index++ % 2 == 1) continue;
                grassMaterial = r.sharedMaterial;
                parts.Add(new CombineInstance { mesh = filter.sharedMesh, transform = filter.transform.localToWorldMatrix });
                vertices += filter.sharedMesh.vertexCount;
            }

            var mesh = new Mesh { name = "Grass Patch" };
            if (vertices > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(parts.ToArray(), true, true);
            var b = mesh.bounds;
            var shift = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++) verts[i] += shift;
            mesh.vertices = verts;
            mesh.RecalculateBounds();
            Object.DestroyImmediate(instance);
            mesh = SaveAsset(mesh, GeneratedFolder + "/Grass Patch.asset");

            var root = new GameObject("grass");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = grassMaterial;
            root.transform.localScale = Vector3.one * (0.6f / Mathf.Max(mesh.bounds.size.x, 0.0001f));
            Save(root, PropsFolder + "/grass.prefab");
            Debug.Log("[SafeTouch] Grass patch: " + parts.Count + " blades, " + mesh.vertexCount + " vertices");
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

        /// <summary>
        /// Renders Mum, Keza and Dad together in three tones to
        /// docs/designs/family-tones.png: the family takes the tone the child
        /// picks for Keza. Checks that beards, hair, clothes and glasses stay put.
        /// </summary>
        [MenuItem("SafeTouch/6. Render Family Preview", priority = 6)]
        public static void RenderFamilyPreview()
        {
            var family = new[] { MumPrefabPath, KezaPrefabPath, DadPrefabPath }.Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).ToArray();
            if (family.Any(p => p == null)) { Debug.LogWarning("[SafeTouch] Build the character prefabs first."); return; }
            var tones = new[] { "honey", "cocoa", "ebony" };

            var people = new System.Collections.Generic.List<GameObject>();
            for (int g = 0; g < tones.Length; g++)
            for (int i = 0; i < family.Length; i++)
            {
                var person = (GameObject)PrefabUtility.InstantiatePrefab(family[i]);
                person.transform.position = new Vector3((g - 1) * 1.15f + (i - 1) * 0.34f, 0f, 0f);
                person.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                person.GetComponent<SkinToneApplier>().Apply(SkinTones.Get(tones[g]).Color);
                foreach (var label in person.GetComponentsInChildren<TextMesh>())
                {
                    label.text = i == 1 ? SkinTones.Get(tones[g]).english : "";
                    label.transform.rotation = Quaternion.identity;
                }
                people.Add(person);
            }
            RenderStrip(people, "docs/designs/family-tones.png", 0.62f, 0.4f);
            foreach (var p in people) Object.DestroyImmediate(p);
        }

        /// <summary>Renders the given objects, front on, into a wide PNG.</summary>
        static void RenderStrip(System.Collections.Generic.List<GameObject> objects, string file, float halfHeight, float lookHeight)
        {
            var light = new GameObject("Preview light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(35f, 200f, 0f);
            var cam = new GameObject("Preview camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.918f, 0.957f, 0.984f);
            cam.orthographic = true;
            cam.orthographicSize = halfHeight;
            cam.transform.position = new Vector3(0f, lookHeight, -3f);
            cam.transform.LookAt(new Vector3(0f, lookHeight, 0f));

            const int width = 1800, height = 600;
            var target = new RenderTexture(width, height, 24);
            cam.targetTexture = target;
            cam.aspect = (float)width / height;
            cam.Render();
            RenderTexture.active = target;
            var png = new Texture2D(width, height, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            png.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, png.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(png);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(light.gameObject);
            Debug.Log("[SafeTouch] Wrote " + file);
        }

        /// <summary>Batch-mode entry: build the prefabs, then render the previews.</summary>
        public static void BuildAndPreview()
        {
            BuildAll();
            RenderSkinTonePreview();
            RenderFamilyPreview();
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
