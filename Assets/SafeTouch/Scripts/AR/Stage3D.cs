using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SafeTouch.AR
{
    /// <summary>
    /// The 3D playground: Keza appears at once on a sunny lawn, with trees, a little house and clouds, and the
    /// phone shows it through a virtual camera. No camera permission and no scanning for a floor or table.
    /// The lessons are unchanged: they still place people around Keza, in metres, through ARManager.
    ///
    /// The child makes Keza bigger or smaller with the + and - buttons or by pinching with two fingers
    /// (the mouse wheel in the Editor). The camera moves closer or further away rather than changing
    /// Keza's real size, so her bubble zones and the distances in the lessons stay right.
    /// </summary>
    public class Stage3D : MonoBehaviour
    {
        public const float MinSize = 0.6f, MaxSize = 3.2f;
        const float BaseDistance = 4f;     // metres from Keza at size 1: Keza and her 0.9 m bubble fit across a portrait screen
        const float Pitch = 14f;           // degrees the camera looks down

        ARManager ar;
        Camera cam;
        Material baseMaterial;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        float size = 1f, shownSize = 1f, lookHeight = 0.2f;
        float lastPinch;

        /// <summary>How big Keza looks: 1 is the starting size, between MinSize and MaxSize.</summary>
        public float Size { get { return size; } }

        /// <summary>Where Keza stands: the middle of the lawn, facing the camera.</summary>
        public Pose KezaPose { get { return new Pose(Vector3.zero, Quaternion.LookRotation(Vector3.back)); } }

        public void Init(ARManager manager, Camera camera, Material opaque)
        {
            ar = manager;
            cam = camera;
            baseMaterial = opaque;
            SetUpCamera();
            BuildWorld();
            ar.CharacterPlaced += character => lookHeight = HeightOf(character.gameObject) * 0.2f;   // aim low: Keza stands above the middle, clear of the lesson cards
            LateUpdate();   // the camera in place from the first frame, so Keza turns to face it
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        /// <summary>Makes Keza look bigger (more than 1) or smaller (less than 1).</summary>
        public void Zoom(float times)
        {
            size = Mathf.Clamp(size * times, MinSize, MaxSize);
        }

        void Update()
        {
            // Keza is always on the lawn, ready for a lesson or for "Keza's look".
            if (!ar.HasCharacter && Time.frameCount > 2) ar.PlaceCharacter(KezaPose);
            Pinch();
        }

        void LateUpdate()
        {
            if (cam == null) return;
            shownSize = Mathf.Lerp(shownSize, size, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var centre = ar.HasCharacter ? ar.Character.transform.position : Vector3.zero;
            var target = centre + Vector3.up * lookHeight;
            var direction = Quaternion.Euler(Pitch, 0f, 0f) * Vector3.forward;
            cam.transform.SetPositionAndRotation(target - direction * (BaseDistance / shownSize), Quaternion.LookRotation(direction));
        }

        /// <summary>Two fingers apart or together, anywhere but on a button: bigger or smaller.</summary>
        void Pinch()
        {
            if (ar.PinchTaken) { lastPinch = 0f; return; }   // a lesson is using the pinch (turning Keza in "look closer")
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                var a = touches[0].screenPosition;
                var b = touches[1].screenPosition;
                if (OverUI(a) || OverUI(b)) return;
                float distance = Vector2.Distance(a, b);
                if (lastPinch > 0f) Zoom(distance / lastPinch);
                lastPinch = Mathf.Max(distance, 1f);
                return;
            }
            lastPinch = 0f;
            var mouse = Mouse.current;
            if (mouse == null || OverUI(mouse.position.ReadValue())) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll > 0f ? 1.1f : 1f / 1.1f);
        }

        static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        static bool OverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            UiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, UiHits);
            foreach (var hit in UiHits) if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
            return false;
        }

        static float HeightOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0.8f;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return Mathf.Clamp(bounds.max.y - go.transform.position.y, 0.3f, 2f);
        }

        // ---- The camera: a normal 3D camera instead of the phone's camera picture ----

        void SetUpCamera()
        {
            if (cam == null) return;
            // The AR parts (camera picture, phone tracking) would show black or move the view: switch them off.
            foreach (var b in cam.GetComponents<Behaviour>())
            {
                var type = b.GetType().Name;
                if (type == "ARCameraBackground" || type == "ARCameraManager" || type == "TrackedPoseDriver") b.enabled = false;
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.6f, 0.84f, 0.97f);   // sky blue
            cam.fieldOfView = 62f;   // wide enough for the scenery on a tall, narrow screen
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 80f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = cam.backgroundColor;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 60f;

            var sun = FindFirstObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                sun = new GameObject("Sun light").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.45f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // ---- The playground, built from simple shapes ----

        Material Paint(Color c)
        {
            Material m;
            if (materials.TryGetValue(c, out m)) return m;
            m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            materials[c] = m;
            return m;
        }

        Transform world;

        GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color colour, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());   // scenery never catches the child's taps
            go.transform.SetParent(world, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.GetComponent<Renderer>().sharedMaterial = Paint(colour);
            return go;
        }

        void BuildWorld()
        {
            world = new GameObject("Playground").transform;
            Shape(PrimitiveType.Cylinder, "Lawn", new Vector3(0f, -0.06f, 0f), new Vector3(80f, 0.05f, 80f), new Color(0.45f, 0.76f, 0.36f));
            Shape(PrimitiveType.Cylinder, "Play mat", new Vector3(0f, -0.005f, 0f), new Vector3(2f, 0.005f, 2f), new Color(0.56f, 0.83f, 0.46f));
            // A sandy path from the camera to Keza.
            for (int i = 0; i < 6; i++)
                Shape(PrimitiveType.Cylinder, "Stone", new Vector3(Mathf.Sin(i * 1.3f) * 0.12f, -0.01f, -1.3f - i * 0.55f), new Vector3(0.42f, 0.01f, 0.34f), new Color(0.93f, 0.85f, 0.66f), i * 40f);

            // The phone shows a tall, narrow view, so the scenery sits behind Keza: a row of trees and
            // a little house in the distance, bushes and flowers on both sides of her.
            float[] treeX = { -5.5f, -3.2f, -1.2f, 1.4f, 3.4f, 5.6f, -7.5f, 7.8f };
            float[] treeZ = { 7f, 9.5f, 12f, 10f, 7.5f, 9f, 13f, 12.5f };
            for (int i = 0; i < treeX.Length; i++) Tree(new Vector3(treeX[i], 0f, treeZ[i]), 0.9f + (i % 3) * 0.25f, i);
            for (int side = -1; side <= 1; side += 2)
            {
                Tree(new Vector3(side * 2.7f, 0f, 3.2f), 0.75f, side + 3);
                Shape(PrimitiveType.Sphere, "Bush", new Vector3(side * 1.7f, 0.2f, 1.6f), new Vector3(0.7f, 0.5f, 0.7f), new Color(0.27f, 0.58f, 0.3f));
                Shape(PrimitiveType.Sphere, "Bush", new Vector3(side * 2.1f, 0.16f, 0.4f), new Vector3(0.5f, 0.38f, 0.5f), new Color(0.32f, 0.64f, 0.33f));
            }
            Color[] petals = { new Color(1f, 0.45f, 0.55f), new Color(1f, 0.85f, 0.25f), new Color(0.65f, 0.5f, 1f), Color.white };
            for (int i = 0; i < 30; i++)
            {
                float a = i * 2.39996f, r = 1.2f + (i * 0.37f) % 2.4f;   // a sunflower spiral, so they never bunch up
                var p = new Vector3(Mathf.Cos(a) * r, 0.04f, Mathf.Sin(a) * r);
                if (p.z < -0.8f && Mathf.Abs(p.x) < 0.8f) continue;   // keep the path clear
                Shape(PrimitiveType.Sphere, "Flower", p, Vector3.one * 0.08f, petals[i % petals.Length]);
            }

            House(new Vector3(-2.4f, 0f, 6.5f), 18f);

            // The sun and some clouds, far away.
            var sunBall = Shape(PrimitiveType.Sphere, "Sun", new Vector3(9f, 11f, 30f), Vector3.one * 4f, new Color(1f, 0.86f, 0.3f));
            sunBall.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int c = 0; c < 4; c++) Cloud(new Vector3(-14f + c * 9f, 7f + (c % 2) * 2f, 26f + (c % 3) * 4f));
        }

        void Tree(Vector3 at, float scale, int seed)
        {
            Shape(PrimitiveType.Cylinder, "Trunk", at + Vector3.up * 0.6f * scale, new Vector3(0.22f, 0.6f, 0.22f) * scale, new Color(0.55f, 0.36f, 0.22f));
            Color[] leaves = { new Color(0.27f, 0.6f, 0.3f), new Color(0.35f, 0.7f, 0.33f), new Color(0.22f, 0.52f, 0.33f) };
            var leaf = leaves[seed % leaves.Length];
            Shape(PrimitiveType.Sphere, "Leaves", at + Vector3.up * 1.55f * scale, Vector3.one * 1.3f * scale, leaf);
            Shape(PrimitiveType.Sphere, "Leaves", at + new Vector3(0.45f, 1.25f, 0.1f) * scale, Vector3.one * 0.9f * scale, leaf);
            Shape(PrimitiveType.Sphere, "Leaves", at + new Vector3(-0.4f, 1.3f, -0.1f) * scale, Vector3.one * 0.85f * scale, leaf);
        }

        void House(Vector3 at, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Shape(PrimitiveType.Cube, "Walls", at + Vector3.up * 0.8f, new Vector3(2.4f, 1.6f, 2f), new Color(1f, 0.92f, 0.75f), yaw);
            var roof = Shape(PrimitiveType.Cube, "Roof", at + Vector3.up * 1.85f, new Vector3(1.75f, 1.75f, 2.2f), new Color(0.88f, 0.36f, 0.3f), yaw);
            roof.transform.localRotation = rot * Quaternion.Euler(0f, 0f, 45f);
            Shape(PrimitiveType.Cube, "Door", at + rot * new Vector3(0f, 0.5f, -1.01f), new Vector3(0.5f, 1f, 0.04f), new Color(0.2f, 0.45f, 0.78f), yaw);
            Shape(PrimitiveType.Cube, "Window", at + rot * new Vector3(0.75f, 0.95f, -1.01f), new Vector3(0.45f, 0.45f, 0.04f), new Color(0.75f, 0.9f, 1f), yaw);
            Shape(PrimitiveType.Cube, "Window", at + rot * new Vector3(-0.75f, 0.95f, -1.01f), new Vector3(0.45f, 0.45f, 0.04f), new Color(0.75f, 0.9f, 1f), yaw);
        }

        void Cloud(Vector3 at)
        {
            foreach (var puff in new[] { new Vector4(0f, 0f, 0f, 2.6f), new Vector4(1.6f, -0.3f, 0f, 2f), new Vector4(-1.6f, -0.3f, 0f, 2f), new Vector4(0.6f, 0.7f, 0f, 1.8f) })
            {
                var s = Shape(PrimitiveType.Sphere, "Cloud", at + (Vector3)puff, Vector3.one * puff.w, Color.white);
                s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
