using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// Tools for the 3D models in Assets/SafeTouch/Models:
    ///   SafeTouch > 3. Render Model Previews - a PNG of every model in
    ///   docs/designs/models, with its size and materials in the console,
    ///   so the team can see what each FBX is without opening it.
    /// </summary>
    public static class SafeTouchModels
    {
        public const string ModelsFolder = "Assets/SafeTouch/Models";
        const string PreviewFolder = "docs/designs/models";
        const int PreviewSize = 512;

        /// <summary>
        /// The Mixamo and Sketchfab FBX files carry their textures inside the
        /// file, but Unity shows them white until they are extracted. This
        /// writes them to Models/Textures/&lt;model&gt; and reimports, so the
        /// materials pick them up by name.
        /// </summary>
        public static void ExtractTextures(string modelPath)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) return;
            string folder = ModelsFolder + "/Textures/" + Slug(Path.GetFileNameWithoutExtension(modelPath));
            if (AssetDatabase.IsValidFolder(folder)) return;   // already done
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            if (importer.ExtractTextures(folder))
            {
                AssetDatabase.Refresh();
                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
                importer.SaveAndReimport();
                Debug.Log("[SafeTouch] Extracted textures of " + modelPath + " to " + folder);
            }
        }

        [MenuItem("SafeTouch/3. Render Model Previews", priority = 3)]
        public static void RenderPreviews()
        {
            Directory.CreateDirectory(PreviewFolder);
            // The source models, plus the prefabs built from them when they exist.
            var builtFolders = new[] { "Assets/SafeTouch/Prefabs", "Assets/SafeTouch/Resources/Props" }.Where(AssetDatabase.IsValidFolder).ToArray();
            var paths = AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder })
                .Concat(builtFolders.Length > 0 ? AssetDatabase.FindAssets("t:Prefab", builtFolders) : new string[0])
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p).ToArray();
            foreach (var path in paths)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;
                string file = Path.Combine(PreviewFolder, Slug(Path.GetFileNameWithoutExtension(path)) + ".png");
                Render(model, file);
                Debug.Log("[SafeTouch] Preview " + file + "  " + Describe(model));
            }
            Debug.Log("[SafeTouch] Rendered " + paths.Length + " model previews into " + PreviewFolder);
        }

        /// <summary>Size in metres and material names, e.g. for choosing which model can take a skin tone.</summary>
        public static string Describe(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var bounds = Bounds(renderers);
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct();
            return string.Format("size {0:0.00} x {1:0.00} x {2:0.00} m, {3} renderers, materials: {4}",
                bounds.size.x, bounds.size.y, bounds.size.z, renderers.Length, string.Join(", ", materials.ToArray()));
        }

        static void Render(GameObject model, string file)
        {
            var instance = Object.Instantiate(model);
            instance.hideFlags = HideFlags.HideAndDontSave;
            foreach (var label in instance.GetComponentsInChildren<TextMesh>())
                label.transform.rotation = Quaternion.LookRotation(-new Vector3(0.35f, 0.25f, 1f));   // face the preview camera
            var bounds = Bounds(instance.GetComponentsInChildren<Renderer>().Where(r => r.GetComponent<TextMesh>() == null).ToArray());

            var light = new GameObject("Preview light") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, 150f, 0f);

            var camGo = new GameObject("Preview camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.918f, 0.957f, 0.984f);   // Theme.Sky
            cam.fieldOfView = 30f;
            float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var direction = new Vector3(0.35f, 0.25f, 1f).normalized;   // front, a little to the side and above
            cam.transform.position = bounds.center + direction * distance;
            cam.transform.LookAt(bounds.center);
            cam.nearClipPlane = distance * 0.01f;
            cam.farClipPlane = distance * 4f;

            var target = new RenderTexture(PreviewSize, PreviewSize, 24);
            cam.targetTexture = target;
            cam.Render();
            RenderTexture.active = target;
            var png = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, PreviewSize, PreviewSize), 0, 0);
            png.Apply();
            File.WriteAllBytes(file, png.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(png);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(light.gameObject);
            Object.DestroyImmediate(instance);
        }

        /// <summary>World bounds of the renderers that draw (switched-off parts, like the other adult, are skipped).</summary>
        public static Bounds Bounds(Renderer[] renderers)
        {
            var shown = renderers.Where(r => r.enabled).ToArray();
            if (shown.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = shown[0].bounds;
            foreach (var r in shown) b.Encapsulate(r.bounds);
            return b;
        }

        static string Slug(string name)
        {
            var chars = name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
            return new string(chars).Trim('-').Replace("--", "-");
        }
    }
}
