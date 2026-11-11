using System.Collections.Generic;
using System.IO;
using System.Linq;
using SafeTouch.AR;
using UnityEditor;
using UnityEngine;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// SafeTouch > 9. Build 3D World From Downloaded Models. Turns the scenery models in Assets/SafeTouch/Models
    /// into the pieces the 3D world (Stage3D) and the stories load from Resources/Props:
    ///   house      - the forest house with its fence, rocks and grass (forest-house), merged into one mesh so the
    ///                phone draws it in one go; its base block is left out so it stands on the lawn;
    ///   pine 1..n  - the house model's trees, one by one, to plant around the lawn;
    ///   classroom  - the classroom (school-classroom) as a stage set, half size like the rest of the world:
    ///                the blackboard behind the helper, the wall and roof nearest the phone taken off so the
    ///                camera looks in, and the desks moved to the sides so the middle is free for the stories;
    ///   person 1..6 - the six scanned people (lowpoly-people), standing in the distance to make the park lively.
    /// It also makes the six people the 9 to 11 path's visitors and strangers (Prefabs/Ages 9-11/Scan person N),
    /// and the sky (Resources/Looks/Sky). Textures are limited to 1024 pixels for the phone.
    /// Everything it writes can be regenerated.
    /// </summary>
    public static class SafeTouchScenery
    {
        const string Models = SafeTouchModels.ModelsFolder;
        const string HouseModel = Models + "/forest-house/source/forest_house_model.fbx";
        const string HouseTextures = Models + "/forest-house/textures";
        const string ClassroomModel = Models + "/school-classroom/source/Classroom.fbx";
        const string ClassroomTextures = Models + "/school-classroom/textures";
        const string PeopleModel = Models + "/lowpoly-people/source/people.fbx";
        const string PeopleTextures = Models + "/lowpoly-people/textures";

        const string PropsFolder = "Assets/SafeTouch/Resources/Props";
        const string LooksFolder = "Assets/SafeTouch/Resources/Looks";
        const string Generated = "Assets/SafeTouch/Generated/Scenery";
        const string OlderPrefabs = "Assets/SafeTouch/Prefabs/Ages 9-11";
        const string SparkleMaterial = "Assets/SafeTouch/Materials/SafeTouch Opaque.mat";

        const string SceneFile = "Assets/SafeTouch/Scenes/SafeTouch.unity";
        const float HouseWidth = 7f;          // metres across the house and its garden in the world
        const float ClassroomScale = 0.5f;    // the world is half size: Keza (0.55 m) is a child of about 1.1 m
        const float PersonHeight = 0.78f;     // grown-ups, as everywhere else in the app

        [MenuItem("SafeTouch/9. Build 3D World From Downloaded Models", priority = 9)]
        public static void BuildAll()
        {
            SafeTouchCharacters.EnsureFolder(PropsFolder);
            SafeTouchCharacters.EnsureFolder(LooksFolder);
            SafeTouchCharacters.EnsureFolder(Generated);
            BuildHouse();
            BuildClassroom();
            BuildPeople();
            BuildSky();
            SafeTouchCharacters.BuildGrass();
            AssetDatabase.SaveAssets();

            // The scanned people become the 9 to 11 path's visitors on ARManager in the scene.
            if (File.Exists(SceneFile))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneFile);
                var ar = Object.FindFirstObjectByType<ARManager>();
                if (ar != null)
                {
                    SafeTouchCharacters.SetOlder(ar);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                }
            }
            Debug.Log("[SafeTouch] 3D world built into " + PropsFolder + ". The 3D world and the stories use it at once.");
        }

        // ---------------------------------------------------------------
        // The forest house and its trees
        // ---------------------------------------------------------------

        static void BuildHouse()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(HouseModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] House model is missing: " + HouseModel); return; }
            var colours = AlphaTexture(HouseTextures + "/main_diffuse.png", HouseTextures + "/main_alpha.png", Generated + "/House Colours.png", false);
            var material = Cutout(colours, Generated + "/House.mat");

            var instance = Object.Instantiate(model);
            instance.transform.position = Vector3.zero;
            var ground = instance.transform.Find("ground");
            if (ground != null) Object.DestroyImmediate(ground.gameObject);   // the base block: the house stands on the lawn

            // How big the whole garden is, so the trees keep the house's scale.
            var whole = SafeTouchModels.Bounds(instance.GetComponentsInChildren<Renderer>());
            float scale = HouseWidth / Mathf.Max(Mathf.Max(whole.size.x, whole.size.z), 0.0001f);

            // The trees, one by one, for the lawn; then the rest without them.
            int pines = 0;
            var trees = instance.transform.Find("Trees_GRP");
            if (trees != null)
            {
                foreach (Transform tree in trees)
                {
                    var filters = tree.GetComponentsInChildren<MeshFilter>();
                    if (filters.Length == 0) continue;
                    var mesh = Merge(filters, "Pine " + (pines + 1));
                    SaveProp("pine " + (++pines), mesh, material, scale);
                }
            }
            var house = Merge(instance.GetComponentsInChildren<MeshFilter>(), "House");
            Object.DestroyImmediate(instance);
            SaveProp("house", house, material, scale);
            Debug.Log("[SafeTouch] House: " + house.vertexCount + " vertices in one mesh, and " + pines + " pine trees");
        }

        /// <summary>Merges the meshes into one, with its origin on the floor under the middle.</summary>
        static Mesh Merge(MeshFilter[] filters, string name)
        {
            var parts = filters.Where(f => f.sharedMesh != null)
                .Select(f => new CombineInstance { mesh = f.sharedMesh, transform = f.transform.localToWorldMatrix })
                .ToArray();
            var mesh = new Mesh { name = name };
            if (parts.Sum(p => p.mesh.vertexCount) > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(parts, true, true);
            var b = mesh.bounds;
            var shift = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++) verts[i] += shift;
            mesh.vertices = verts;
            mesh.RecalculateBounds();
            return SafeTouchCharacters.SaveAsset(mesh, Generated + "/" + name + ".asset");
        }

        static void SaveProp(string propName, Mesh mesh, Material material, float scale)
        {
            var root = new GameObject(propName);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            root.transform.localScale = Vector3.one * scale;
            SafeTouchCharacters.Save(root, PropsFolder + "/" + propName + ".prefab");
        }

        /// <summary>The colours with the alpha picture as their see-through part, for leaves, grass cards and glass.</summary>
        static Texture2D AlphaTexture(string colourPath, string alphaPath, string outPath, bool pixelArt)
        {
            var colour = Readable(colourPath);
            var alpha = Readable(alphaPath);
            var result = new Texture2D(colour.width, colour.height, TextureFormat.RGBA32, false);
            var pixels = colour.GetPixels();
            for (int y = 0; y < colour.height; y++)
            for (int x = 0; x < colour.width; x++)
            {
                var a = alpha.GetPixelBilinear((x + 0.5f) / colour.width, (y + 0.5f) / colour.height);
                pixels[y * colour.width + x].a = a.grayscale;
            }
            result.SetPixels(pixels);
            File.WriteAllBytes(outPath, result.EncodeToPNG());
            AssetDatabase.ImportAsset(outPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            if (pixelArt) importer.filterMode = FilterMode.Point;   // a small pixel-art palette: keep its colours crisp
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        static Texture2D Readable(string path)
        {
            var t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(path));
            return t;
        }

        static Material Cutout(Texture2D texture, string path)
        {
            var m = new Material(Shader.Find("Standard"));
            m.mainTexture = texture;
            MakeCutout(m, 0.5f);
            m.SetFloat("_Glossiness", 0.1f);
            return SafeTouchCharacters.SaveAsset(m, path);
        }

        static void MakeCutout(Material m, float cutoff)
        {
            m.SetFloat("_Mode", 1f);
            m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }

        // ---------------------------------------------------------------
        // The classroom
        // ---------------------------------------------------------------

        static void BuildClassroom()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ClassroomModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] Classroom model is missing: " + ClassroomModel); return; }

            var root = new GameObject("classroom");
            var room = Object.Instantiate(model);
            room.name = "Room";
            room.transform.SetParent(root.transform, false);
            // The blackboard wall (the model's -x) goes to the back, away from the phone (+z);
            // the model's x then runs towards the phone and its z to the right.
            room.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            room.transform.localPosition = new Vector3(-0.39f, 0f, 0.37f);
            foreach (var gone in new[] { "Roof", "Wall.001" })   // the roof and the wall nearest the phone: the camera looks in
            {
                var t = room.transform.Find(gone);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            classroomMaterials.Clear();
            foreach (var r in room.GetComponentsInChildren<Renderer>())
            {
                string part = r.sharedMaterial != null ? r.sharedMaterial.name : r.name;
                r.sharedMaterial = ClassroomMaterial(part);
            }

            // The desks are one mesh of nine desks (three rows of three): split it, keep four, and put two
            // rows of two either side of the middle, in front of the helper, so the stories have room.
            var deskBlock = room.transform.Find("School desk");
            if (deskBlock != null)
            {
                var desks = SplitDesks(deskBlock.GetComponent<MeshFilter>(), deskBlock.GetComponent<Renderer>().sharedMaterial);
                Object.DestroyImmediate(deskBlock.gameObject);
                // Root-local metres (half size in the world): two columns either side, two rows towards the phone.
                var spots = new[] { new Vector3(-1.9f, 0f, -1.4f), new Vector3(1.9f, 0f, -1.4f), new Vector3(-1.9f, 0f, -2.75f), new Vector3(1.9f, 0f, -2.75f) };
                for (int i = 0; i < desks.Count; i++)
                {
                    if (i < spots.Length)
                    {
                        desks[i].transform.SetParent(root.transform, false);
                        desks[i].transform.localPosition = spots[i];
                    }
                    else Object.DestroyImmediate(desks[i]);
                }
            }
            foreach (var c in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            root.transform.localScale = Vector3.one * ClassroomScale;
            SafeTouchCharacters.Save(root, PropsFolder + "/classroom.prefab");
        }

        /// <summary>One object per desk (and its chair), cut out of the merged mesh by where each triangle lies.</summary>
        static List<GameObject> SplitDesks(MeshFilter filter, Material material)
        {
            var mesh = filter.sharedMesh;
            var toRoom = filter.transform.parent.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var verts = mesh.vertices.Select(v => toRoom.MultiplyPoint3x4(v)).ToArray();
            var normals = mesh.normals.Select(n => toRoom.MultiplyVector(n).normalized).ToArray();
            var uvs = mesh.uv;
            var tris = mesh.triangles;

            // The desks stand in a three by three grid: find the gaps along x and z.
            var xCuts = Gaps(verts.Select(v => v.x).ToArray());
            var zCuts = Gaps(verts.Select(v => v.z).ToArray());
            var cells = new Dictionary<int, List<int>>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                var c = (verts[tris[t]] + verts[tris[t + 1]] + verts[tris[t + 2]]) / 3f;
                int key = Cell(c.x, xCuts) * 10 + Cell(c.z, zCuts);
                if (!cells.ContainsKey(key)) cells[key] = new List<int>();
                cells[key].AddRange(new[] { tris[t], tris[t + 1], tris[t + 2] });
            }

            var result = new List<GameObject>();
            foreach (var pair in cells.OrderBy(p => p.Key))
            {
                var map = new Dictionary<int, int>();
                var v = new List<Vector3>();
                var n = new List<Vector3>();
                var u = new List<Vector2>();
                var idx = new List<int>();
                foreach (int i in pair.Value)
                {
                    int j;
                    if (!map.TryGetValue(i, out j))
                    {
                        j = v.Count;
                        map[i] = j;
                        v.Add(verts[i]);
                        n.Add(normals.Length > i ? normals[i] : Vector3.up);
                        u.Add(uvs.Length > i ? uvs[i] : Vector2.zero);
                    }
                    idx.Add(j);
                }
                // Each desk's origin on the floor under its middle, turned into the classroom's frame
                // (the model's -x, towards the blackboard, is +z; the model's z is +x).
                var b = new Bounds(v[0], Vector3.zero);
                foreach (var p in v) b.Encapsulate(p);
                var shift = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                var desk = new Mesh { name = "Desk " + (result.Count + 1) };
                desk.SetVertices(v.Select(p => { p += shift; return new Vector3(p.z, p.y, -p.x); }).ToList());
                desk.SetNormals(n.Select(p => new Vector3(p.z, p.y, -p.x)).ToList());
                desk.SetUVs(0, u);
                desk.SetTriangles(idx, 0);
                desk.RecalculateBounds();
                desk.RecalculateTangents();
                desk = SafeTouchCharacters.SaveAsset(desk, Generated + "/Desk " + (result.Count + 1) + ".asset");
                var go = new GameObject("Desk " + (result.Count + 1));
                go.AddComponent<MeshFilter>().sharedMesh = desk;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
                result.Add(go);
            }
            return result;
        }

        /// <summary>The middles of the two biggest empty stretches between the values: the gaps between three rows.</summary>
        static float[] Gaps(float[] values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            var gaps = new List<KeyValuePair<float, float>>();
            for (int i = 1; i < sorted.Length; i++) gaps.Add(new KeyValuePair<float, float>(sorted[i] - sorted[i - 1], (sorted[i] + sorted[i - 1]) * 0.5f));
            return gaps.OrderByDescending(g => g.Key).Take(2).Select(g => g.Value).OrderBy(v => v).ToArray();
        }

        static int Cell(float value, float[] cuts)
        {
            int c = 0;
            foreach (var cut in cuts) if (value > cut) c++;
            return c;
        }

        static readonly Dictionary<string, Material> classroomMaterials = new Dictionary<string, Material>();

        /// <summary>A phone-friendly material for a classroom part, from its colour, normal, ambient-occlusion and see-through pictures.</summary>
        static Material ClassroomMaterial(string part)
        {
            string key = part.Replace(" (Instance)", "");
            Material m;
            if (classroomMaterials.TryGetValue(key, out m) && m != null) return m;
            string prefix = ClassroomTextures + "/" + key.Replace(" ", "_");
            if (!File.Exists(prefix + "_Base_Color.png")) prefix = ClassroomTextures + "/" + key;   // "Teacher desk_Base_Color"
            m = new Material(Shader.Find("Standard")) { name = "Classroom " + key };
            Texture2D colour = null;
            if (key == "Windows" && File.Exists(prefix + "_Opacity.png"))
            {
                // Glass lets the sun through: cut out, so the window light falls on the floor.
                colour = AlphaTexture(prefix + "_Base_Color.png", prefix + "_Opacity.png", Generated + "/Windows Colours.png", false);
                MakeCutout(m, 0.4f);
            }
            else if (File.Exists(prefix + "_Base_Color.png")) colour = Texture(prefix + "_Base_Color.png", false);
            m.mainTexture = colour;
            if (File.Exists(prefix + "_Normal.png"))
            {
                m.SetTexture("_BumpMap", Texture(prefix + "_Normal.png", true));
                m.EnableKeyword("_NORMALMAP");
            }
            if (File.Exists(prefix + "_Mixed_AO.png")) m.SetTexture("_OcclusionMap", Texture(prefix + "_Mixed_AO.png", false));
            m.SetFloat("_Glossiness", key == "Floor" ? 0.45f : 0.15f);   // a polished wooden floor
            m.SetFloat("_Metallic", 0f);
            m = SafeTouchCharacters.SaveAsset(m, Generated + "/Classroom " + key + ".mat");
            classroomMaterials[key] = m;
            return m;
        }

        /// <summary>The texture, at most 1024 pixels for the phone; normal maps marked as such.</summary>
        static Texture2D Texture(string path, bool normal)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.maxTextureSize != 1024) { importer.maxTextureSize = 1024; changed = true; }
                var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != type) { importer.textureType = type; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------
        // The scanned people
        // ---------------------------------------------------------------

        /// <summary>
        /// The model holds each of the six people four times (three plain copies and a detailed one): the
        /// detailed one (".002") is kept, with its own photo texture and normal map.
        /// </summary>
        static void BuildPeople()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PeopleModel);
            if (model == null) { Debug.LogWarning("[SafeTouch] People model is missing: " + PeopleModel); return; }
            SafeTouchCharacters.EnsureFolder(OlderPrefabs);
            var sparkle = AssetDatabase.LoadAssetAtPath<Material>(SparkleMaterial);
            for (int i = 1; i <= 6; i++)
            {
                string id = i.ToString("00") + "_person";
                var part = model.transform.Find(id + ".002") ?? model.transform.Find(id);
                if (part == null) continue;
                var m = new Material(Shader.Find("Standard")) { name = "Scan person " + i };
                m.mainTexture = Texture(PeopleTextures + "/" + id + "_albedo.jpeg", false);
                m.SetTexture("_BumpMap", Texture(PeopleTextures + "/" + id + "_normals.jpeg", true));
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Glossiness", 0.12f);
                m = SafeTouchCharacters.SaveAsset(m, Generated + "/Scan person " + i + ".mat");

                // For the park: only the person, standing on the floor.
                SafeTouchCharacters.Save(Person(part, m, "person " + i), PropsFolder + "/person " + i + ".prefab");

                // For the 9 to 11 path: the same person as a visitor or stranger who can walk, talk and be tapped.
                var root = new GameObject("Person");
                var body = Person(part, m, "Model");
                body.transform.localScale = Vector3.one;
                body.transform.SetParent(root.transform, false);
                var size = SafeTouchCharacters.FitHeight(root, body, PersonHeight);
                SafeTouchCharacters.AddPersonParts(root, size, "Person", false, sparkle, new Renderer[0]);
                var so = new SerializedObject(root.GetComponent<ARFigure>());
                so.FindProperty("keepOwnColours").boolValue = true;   // their real clothes, not the role's colour
                so.ApplyModifiedPropertiesWithoutUndo();
                SafeTouchCharacters.Save(root, OlderPrefabs + "/Scan person " + i + ".prefab");
            }
        }

        /// <summary>One scanned person as its own object, PersonHeight tall, feet on the floor, centred.</summary>
        static GameObject Person(Transform part, Material material, string name)
        {
            var root = new GameObject(name);
            var body = new GameObject("Scan");
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = part.localRotation;
            body.transform.localScale = part.localScale;
            body.AddComponent<MeshFilter>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = material;
            var b = body.GetComponent<Renderer>().bounds;
            body.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            root.transform.localScale = Vector3.one * (PersonHeight / Mathf.Max(b.size.y, 0.0001f));
            return root;
        }

        // ---------------------------------------------------------------
        // The sky
        // ---------------------------------------------------------------

        static void BuildSky()
        {
            var m = new Material(Shader.Find("Skybox/Procedural")) { name = "Sky" };
            m.SetFloat("_SunDisk", 2f);   // high quality sun
            m.SetFloat("_SunSize", 0.035f);
            m.SetFloat("_SunSizeConvergence", 6f);
            m.SetFloat("_AtmosphereThickness", 0.85f);
            m.SetColor("_SkyTint", new Color(0.45f, 0.62f, 0.9f));
            m.SetColor("_GroundColor", new Color(0.42f, 0.5f, 0.38f));
            m.SetFloat("_Exposure", 1.25f);
            SafeTouchCharacters.SaveAsset(m, LooksFolder + "/Sky.mat");
        }
    }
}
