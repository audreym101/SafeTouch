using System.Collections.Generic;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// The 3D playground: Keza appears at once in a park under a real sky, on a grass lawn with pine trees, a
    /// little house in its garden and people chatting in the distance, and the phone shows it through a virtual
    /// camera. No camera permission and no scanning for a floor or table. The lessons are unchanged: they still
    /// place people around Keza, in metres, through ARManager.
    ///
    /// The look is cinematic: soft sun shadows, sky light, haze in the distance, a film colour grade with a
    /// vignette (CinematicLook), a camera that breathes a little like a hand-held one, shakes when something
    /// startling happens (Shake) and moves in on what matters (Focus). Ages 9 to 11 get a warmer, more dramatic
    /// late-afternoon light (SetOlderLook). Stories at school play in a classroom (ShowClassroom).
    /// The scenery comes from Resources/Props (SafeTouch > 9. Build 3D World From Downloaded Models); any piece
    /// that is missing is built from simple shapes, as before.
    ///
    /// The park is alive: a child and grown-ups walk its paths far behind the lesson, stop for a moment and
    /// walk on, while others stand chatting. The child can swing the camera around the guide with a swipe
    /// (Orbit); it drifts back to the front after a while. Day or night: the child picks night with the moon
    /// button (ChooseNight). A story card sets its own time while it plays (SetStoryNight): day stories in
    /// daylight, stories after dark at night, whatever the child picked, until the lesson clears it
    /// (ClearStoryTime). Night is a dark blue sky with the moon and stars, street lamps with pools of warm
    /// light, and a soft light on the play area so the people in the lesson are always easy to see.
    ///
    /// The child makes Keza bigger or smaller with the + and - buttons or by pinching (SizeButtons, through
    /// ARManager.ChangeSize). The camera moves closer or further away rather than changing
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
        CinematicLook look;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly List<Object> made = new List<Object>();   // textures and materials made here, destroyed on Teardown
        float size = 1f, shownSize = 1f, lookHeight = 0.2f;
        bool older;

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
            SetOlderLook(ar.Older);
            ARFigure.SpeechScale = 2f;   // the view is further away than in the camera picture
            ARFx.LabelScale = 1.5f;      // and so are the floating labels
            ar.CharacterPlaced += Aim;
            if (ar.HasCharacter) Aim(ar.Character);
            LateUpdate();   // the camera in place from the first frame, so Keza turns to face it
        }

        void Aim(ARCharacter character)
        {
            // Aim above her middle: Keza stands a little below the middle of the screen, in the space between
            // the question at the top and the answers at the bottom, with room above her for speech bubbles.
            lookHeight = HeightOf(character.gameObject) * 0.8f;
        }

        /// <summary>Takes the playground away and gives the camera back its own settings (switching to the camera picture).</summary>
        public void Teardown()
        {
            ar.CharacterPlaced -= Aim;
            ARFigure.SpeechScale = 0f;
            ARFx.LabelScale = 1f;
            ShowClassroom(false);
            if (world != null) Destroy(world.gameObject);
            if (ground != null) Destroy(ground);
            foreach (var m in materials.Values) if (m != null) Destroy(m);
            materials.Clear();
            foreach (var o in made) if (o != null) Destroy(o);
            made.Clear();
            if (look != null) Destroy(look);
            if (cam != null)
            {
                foreach (var b in switchedOff) if (b != null) b.enabled = true;
                cam.clearFlags = saved.clearFlags;
                cam.backgroundColor = saved.background;
                cam.fieldOfView = saved.fieldOfView;
                cam.nearClipPlane = saved.near;
                cam.farClipPlane = saved.far;
            }
            switchedOff.Clear();
            RenderSettings.fog = saved.fog;
            RenderSettings.ambientMode = saved.ambientMode;
            RenderSettings.ambientLight = saved.ambientLight;
            RenderSettings.skybox = saved.skybox;
            RenderSettings.fogStartDistance = saved.fogStart;
            RenderSettings.fogEndDistance = saved.fogEnd;
            QualitySettings.pixelLightCount = saved.pixelLights;
            QualitySettings.shadows = saved.shadows;
            QualitySettings.shadowResolution = saved.shadowResolution;
            QualitySettings.shadowDistance = saved.shadowDistance;
            QualitySettings.antiAliasing = saved.antiAliasing;
            if (madeSun && sun != null) Destroy(sun.gameObject);
            else if (sun != null)
            {
                sun.color = saved.sunColor;
                sun.intensity = saved.sunIntensity;
                sun.shadows = saved.sunShadows;
                sun.transform.rotation = saved.sunRotation;
            }
        }

        // What the camera, the light, the sky and the shadows were before the playground changed them.
        struct Saved
        {
            public CameraClearFlags clearFlags;
            public Color background, ambientLight, sunColor;
            public float fieldOfView, near, far, sunIntensity, shadowDistance, fogStart, fogEnd;
            public int pixelLights;
            public bool fog;
            public UnityEngine.Rendering.AmbientMode ambientMode;
            public LightShadows sunShadows;
            public Quaternion sunRotation;
            public Material skybox;
            public ShadowQuality shadows;
            public ShadowResolution shadowResolution;
            public int antiAliasing;
        }

        Saved saved;
        readonly List<Behaviour> switchedOff = new List<Behaviour>();
        Light sun;
        bool madeSun;

        /// <summary>Makes Keza look bigger (more than 1) or smaller (less than 1).</summary>
        public void Zoom(float times)
        {
            size = Mathf.Clamp(size * times, MinSize, MaxSize);
        }

        // ---- The camera: breathing, shaking and moving in ----

        float shakeUntil, shakeStrength;
        Vector3 focusPoint;
        float focusStart = -10f, focusSeconds;
        float orbit, orbitShown, orbitTouched = -100f;
        const float MaxOrbit = 75f, OrbitRest = 8f;   // degrees either side; seconds before it drifts back to the front

        /// <summary>
        /// Swings the camera around the guide, the way the finger swiped (more than 0: to the right), up to a
        /// three-quarter view either side. After a few seconds without a swipe it drifts back to the front.
        /// </summary>
        public void Orbit(float degrees)
        {
            orbit = Mathf.Clamp(orbit - degrees, -MaxOrbit, MaxOrbit);
            orbitTouched = Time.time;
        }

        /// <summary>A short camera shake, as when a car horn blares or someone grabs: strength 1 is a strong jolt.</summary>
        public void Shake(float strength = 0.5f, float seconds = 0.4f)
        {
            shakeStrength = strength;
            shakeUntil = Time.time + seconds;
        }

        /// <summary>The camera moves in on a point (the stranger, the phone) for a few seconds, then back.</summary>
        public void Focus(Vector3 point, float seconds = 2.2f)
        {
            focusPoint = point;
            focusStart = Time.time;
            focusSeconds = seconds;
        }

        void Update()
        {
            // Keza is always on the lawn, ready for a lesson or for "Keza's look".
            if (!ar.HasCharacter && Time.frameCount > 2) ar.PlaceCharacter(KezaPose);
            MoveWalkers();
        }

        void LateUpdate()
        {
            if (cam == null) return;
            shownSize = Mathf.Lerp(shownSize, size, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var centre = ar.HasCharacter ? ar.Character.transform.position : Vector3.zero;
            var target = centre + Vector3.up * lookHeight;
            float distance = BaseDistance / shownSize;

            // Moving in: eased in and out, a third closer, the point a little nearer the middle of the view.
            float f = focusSeconds > 0f ? (Time.time - focusStart) / focusSeconds : 1f;
            if (f >= 0f && f < 1f)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Min(f / 0.25f, (1f - f) / 0.3f));
                target = Vector3.Lerp(target, Vector3.Lerp(target, focusPoint + Vector3.up * lookHeight, 0.45f), k);
                distance *= 1f - 0.3f * k;
            }

            // The child's swipe, eased; it drifts back to the front when they leave it.
            if (Time.time - orbitTouched > OrbitRest) orbit = Mathf.MoveTowards(orbit, 0f, 25f * Time.unscaledDeltaTime);
            orbitShown = Mathf.Lerp(orbitShown, orbit, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            // Hand-held: a slow, tiny drift, never enough to make anyone dizzy.
            float t = Time.time;
            float yaw = orbitShown + (Mathf.PerlinNoise(t * 0.13f, 0.3f) - 0.5f) * 1.6f;
            float pitch = Pitch + (Mathf.PerlinNoise(0.7f, t * 0.11f) - 0.5f) * 1.0f;
            var direction = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
            var rotation = Quaternion.LookRotation(direction);
            if (Time.time < shakeUntil)
            {
                float s = shakeStrength * (shakeUntil - Time.time) * 4f;
                rotation *= Quaternion.Euler((Mathf.PerlinNoise(t * 31f, 1f) - 0.5f) * s * 3f, (Mathf.PerlinNoise(2f, t * 29f) - 0.5f) * s * 3f, 0f);
            }
            cam.transform.SetPositionAndRotation(target - direction * distance, rotation);
            if (playLight != null) playLight.transform.position = centre + Vector3.up * 1.4f - direction * 0.9f;
        }

        static float HeightOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0.8f;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return Mathf.Clamp(bounds.max.y - go.transform.position.y, 0.3f, 2f);
        }

        // ---- The camera, the sky and the light ----

        void SetUpCamera()
        {
            if (cam == null) return;
            saved.clearFlags = cam.clearFlags;
            saved.background = cam.backgroundColor;
            saved.fieldOfView = cam.fieldOfView;
            saved.near = cam.nearClipPlane;
            saved.far = cam.farClipPlane;
            saved.fog = RenderSettings.fog;
            saved.ambientMode = RenderSettings.ambientMode;
            saved.ambientLight = RenderSettings.ambientLight;
            saved.skybox = RenderSettings.skybox;
            saved.shadows = QualitySettings.shadows;
            saved.shadowResolution = QualitySettings.shadowResolution;
            saved.shadowDistance = QualitySettings.shadowDistance;
            saved.antiAliasing = QualitySettings.antiAliasing;
            saved.fogStart = RenderSettings.fogStartDistance;
            saved.fogEnd = RenderSettings.fogEndDistance;
            saved.pixelLights = QualitySettings.pixelLightCount;
            // The AR parts (camera picture, phone tracking) would show black or move the view: switch them off.
            foreach (var b in cam.GetComponents<Behaviour>())
            {
                var type = b.GetType().Name;
                if ((type == "ARCameraBackground" || type == "ARCameraManager" || type == "TrackedPoseDriver") && b.enabled)
                {
                    b.enabled = false;
                    switchedOff.Add(b);
                }
            }
            var horizon = new Color(0.74f, 0.84f, 0.92f);
            var sky = Resources.Load<Material>("Looks/Sky");
            cam.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.6f, 0.84f, 0.97f);   // sky blue, if there is no sky
            cam.fieldOfView = 62f;   // wide enough for the scenery on a tall, narrow screen
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            if (sky != null)
            {
                sky = new Material(sky);
                made.Add(sky);
                RenderSettings.skybox = sky;
                daySky = new Material(sky);   // the day sky's settings, to come back to after the night
                made.Add(daySky);
            }

            // Light from the sky above, the haze at the horizon and the grass below, instead of one flat colour.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = horizon;
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 75f;

            // Soft shadows and smooth edges in the 3D world, even on the phone's "Medium" quality.
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 22f;
            QualitySettings.antiAliasing = 2;

            sun = FindFirstObjectByType<Light>();
            madeSun = sun == null || sun.type != LightType.Directional;
            if (madeSun)
            {
                sun = new GameObject("Sun light").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            else
            {
                saved.sunColor = sun.color;
                saved.sunIntensity = sun.intensity;
                saved.sunShadows = sun.shadows;
                saved.sunRotation = sun.transform.rotation;
            }
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.3f;
            look = cam.GetComponent<CinematicLook>();
            if (look == null) look = cam.gameObject.AddComponent<CinematicLook>();
        }

        /// <summary>
        /// Ages 6 to 8: a bright, gentle late morning. Ages 9 to 11: a warm, low late-afternoon sun, longer
        /// shadows, deeper colours and a stronger vignette, like a film.
        /// </summary>
        public void SetOlderLook(bool isOlder)
        {
            older = isOlder;
            ApplyLight();
        }

        // ---- Day and night ----

        bool chosenNight;
        bool? storyNight;   // set while a story card plays: its own time of day, over the child's choice
        Material daySky;
        Light playLight;
        GameObject nightSky;
        readonly List<GameObject> lamps = new List<GameObject>();

        /// <summary>True while the park is at night.</summary>
        public bool Night { get { return storyNight ?? chosenNight; } }

        /// <summary>True while a story card decides the time of day (the moon button is hidden then).</summary>
        public bool StoryTime { get { return storyNight.HasValue; } }

        /// <summary>The moon or sun button: the child picks night or day.</summary>
        public void ChooseNight(bool night)
        {
            chosenNight = night;
            ApplyLight();
        }

        /// <summary>A story card plays at its own time: night for the stories after dark, day for the others.</summary>
        public void SetStoryNight(bool night)
        {
            if (storyNight == night) return;
            storyNight = night;
            ApplyLight();
        }

        /// <summary>The story cards are over: back to the day or night the child picked.</summary>
        public void ClearStoryTime()
        {
            if (!storyNight.HasValue) return;
            storyNight = null;
            ApplyLight();
        }

        void ApplyLight()
        {
            if (sun == null) return;
            bool night = Night;
            ShowNight(night);
            if (look != null) { if (night) look.NightPreset(); else look.Preset(older); }
            if (classroom != null && !night)
            {
                // At school the sun comes in through the windows on the left, so their light falls on the floor.
                sun.color = new Color(1f, 0.93f, 0.82f);
                sun.intensity = 1.15f;
                sun.shadowStrength = 0.8f;
                sun.transform.rotation = Quaternion.Euler(32f, 75f, 0f);
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.64f, 0.68f);
                RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
                RenderSettings.ambientGroundColor = new Color(0.4f, 0.32f, 0.26f);
            }
            else if (older)
            {
                sun.color = new Color(1f, 0.84f, 0.66f);
                sun.intensity = 1.25f;
                sun.shadowStrength = 0.75f;
                sun.transform.rotation = Quaternion.Euler(24f, -42f, 0f);
                RenderSettings.ambientSkyColor = new Color(0.5f, 0.58f, 0.72f);
                RenderSettings.ambientEquatorColor = new Color(0.62f, 0.56f, 0.5f);
                RenderSettings.ambientGroundColor = new Color(0.24f, 0.27f, 0.18f);
            }
            else
            {
                sun.color = new Color(1f, 0.96f, 0.88f);
                sun.intensity = 1.15f;
                sun.shadowStrength = 0.65f;
                sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                RenderSettings.ambientSkyColor = new Color(0.56f, 0.67f, 0.82f);
                RenderSettings.ambientEquatorColor = new Color(0.6f, 0.66f, 0.66f);
                RenderSettings.ambientGroundColor = new Color(0.27f, 0.33f, 0.2f);
            }
            if (night)
            {
                // Moonlight: low, cool and soft; the lamps and the light on the play area do the rest.
                sun.color = new Color(0.62f, 0.7f, 1f);
                sun.intensity = 0.42f;
                sun.shadowStrength = 0.55f;
                sun.transform.rotation = Quaternion.Euler(36f, 150f, 0f);
                RenderSettings.ambientSkyColor = new Color(0.16f, 0.2f, 0.36f);
                RenderSettings.ambientEquatorColor = new Color(0.11f, 0.13f, 0.22f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.08f);
            }
            var skybox = RenderSettings.skybox;
            if (skybox != null && daySky != null)
            {
                skybox.CopyPropertiesFromMaterial(daySky);
                if (night)
                {
                    // The same sky at a fraction of the light: deep blue overhead, the moon where its light comes from.
                    if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", 0.22f);
                    if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", new Color(0.32f, 0.38f, 0.62f));
                    if (skybox.HasProperty("_GroundColor")) skybox.SetColor("_GroundColor", new Color(0.05f, 0.06f, 0.09f));
                    if (skybox.HasProperty("_SunSize")) skybox.SetFloat("_SunSize", 0.07f);
                }
                else if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", older ? 1.05f : 1.25f);
            }
            cam.backgroundColor = night ? new Color(0.05f, 0.07f, 0.14f) : new Color(0.6f, 0.84f, 0.97f);
            RenderSettings.fogColor = night ? new Color(0.07f, 0.09f, 0.17f) : older ? new Color(0.84f, 0.8f, 0.76f) : new Color(0.74f, 0.84f, 0.92f);
            RenderSettings.fogStartDistance = night ? 9f : 16f;
            RenderSettings.fogEndDistance = night ? 48f : 75f;
            // At night the play-area light and the nearest lamps light pixel by pixel; the rest are cheaper, for mid-range phones.
            QualitySettings.pixelLightCount = night ? Mathf.Max(saved.pixelLights, 3) : saved.pixelLights;
        }

        /// <summary>The moon and stars, the street lamps and the soft light on the play area: on at night only.</summary>
        void ShowNight(bool night)
        {
            if (night && nightSky == null) BuildNight();
            if (nightSky != null) nightSky.SetActive(night);
            foreach (var l in lamps) if (l != null) l.SetActive(night);
            if (playLight != null) playLight.enabled = night;
        }

        void BuildNight()
        {
            // Stars: tiny dots on a far dome. The sprite shader ignores the fog, so they shine through the haze.
            nightSky = new GameObject("Night sky");
            nightSky.transform.SetParent(world, false);
            var starMaterial = ARFx.Unlit(new Color(1f, 1f, 0.95f, 0.9f), ARFx.SoftDot);
            made.Add(starMaterial);
            var random = new System.Random(11);
            for (int i = 0; i < 140; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2f, h = 0.08f + (float)random.NextDouble() * 0.9f;
                var dir = new Vector3(Mathf.Cos(a) * Mathf.Sqrt(1f - h * h), h, Mathf.Sin(a) * Mathf.Sqrt(1f - h * h));
                if (dir.z < -0.2f) continue;   // only where the camera looks
                var star = GameObject.CreatePrimitive(PrimitiveType.Quad);
                star.name = "Star";
                Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(nightSky.transform, false);
                star.transform.localPosition = dir * 95f;
                star.transform.localRotation = Quaternion.LookRotation(dir);
                star.transform.localScale = Vector3.one * (0.35f + (float)random.NextDouble() * 0.5f);
                var r = star.GetComponent<Renderer>();
                r.sharedMaterial = starMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            // The moon, big and pale, where the moonlight comes from, with a soft glow around it.
            // Low over the trees to the right: seen when the child zooms out or swings the view round.
            var toMoon = Quaternion.Euler(-11f, 14f, 0f) * Vector3.forward;
            Glow(nightSky.transform, "Moon glow", toMoon * 90f, 16f, new Color(0.75f, 0.82f, 1f, 0.3f));
            Glow(nightSky.transform, "Moon", toMoon * 89f, 4.5f, new Color(1f, 0.98f, 0.9f, 1f), UI.UIFactory.Circle.texture);

            // Street lamps along the paths and by the house, each with a warm pool of light on the grass.
            Lamp(new Vector3(-1.9f, 0f, 1.6f));
            Lamp(new Vector3(2.1f, 0f, 2.4f));
            Lamp(new Vector3(-3.6f, 0f, 6.6f));
            Lamp(new Vector3(3.8f, 0f, 7.2f));

            // A soft light that follows the play area, so the people in the lesson are always easy to see.
            playLight = new GameObject("Play area light").AddComponent<Light>();
            playLight.transform.SetParent(world.parent, false);
            playLight.type = LightType.Point;
            playLight.color = new Color(1f, 0.9f, 0.76f);
            playLight.range = 3.2f;
            playLight.intensity = 1.1f;
            playLight.shadows = LightShadows.None;
            playLight.renderMode = LightRenderMode.ForcePixel;
            made.Add(playLight.gameObject);
        }

        /// <summary>A flat glowing disc that always faces the camera's side of the world (the moon and its halo).</summary>
        void Glow(Transform parent, string name, Vector3 at, float size, Color colour, Texture texture = null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = at;
            quad.transform.localRotation = Quaternion.LookRotation(at);
            quad.transform.localScale = Vector3.one * size;
            var m = ARFx.Unlit(colour, texture != null ? texture : ARFx.SoftDot);
            made.Add(m);
            var r = quad.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>A street lamp: a dark post with a glowing lamp, its light, and a warm pool of light below.</summary>
        void Lamp(Vector3 at)
        {
            var lamp = new GameObject("Street lamp");
            lamp.transform.SetParent(world, false);
            lamp.transform.localPosition = at;
            var post = Paint(new Color(0.16f, 0.17f, 0.2f));
            Part(lamp.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.75f, 0f), new Vector3(0.05f, 0.75f, 0.05f), post);
            Part(lamp.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.13f, 0.02f, 0.13f), post);
            Part(lamp.transform, PrimitiveType.Cube, new Vector3(0f, 1.5f, 0f), new Vector3(0.16f, 0.05f, 0.16f), post);
            var bulb = Part(lamp.transform, PrimitiveType.Sphere, new Vector3(0f, 1.44f, 0f), new Vector3(0.12f, 0.1f, 0.12f), ARFx.Unlit(new Color(1f, 0.88f, 0.6f)));
            bulb.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Glow(lamp.transform, "Glow", new Vector3(0f, 1.44f, 0f), 0.7f, new Color(1f, 0.82f, 0.5f, 0.45f));
            lamp.transform.Find("Glow").gameObject.AddComponent<Billboard>();
            var pool = ARFx.FloorQuad("Light pool", ARFx.SoftDot, new Color(1f, 0.78f, 0.45f, 0.32f), 2.2f);
            pool.transform.SetParent(lamp.transform, false);
            pool.transform.localPosition = new Vector3(0f, 0.004f, 0f);

            var light = new GameObject("Lamp light").AddComponent<Light>();
            light.transform.SetParent(lamp.transform, false);
            light.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.52f);
            light.range = 2.8f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
            lamps.Add(lamp);
        }

        GameObject Part(Transform parent, PrimitiveType type, Vector3 at, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // ---- The classroom, for stories at school ----

        GameObject classroom;

        /// <summary>Shows the classroom around Keza instead of the park (stories at school), or the park again.</summary>
        public void ShowClassroom(bool show)
        {
            if (show == (classroom != null)) return;
            if (show)
            {
                var prefab = Resources.Load<GameObject>("Props/classroom");
                if (prefab == null) return;   // not built: the story brings its own blackboard
                classroom = Instantiate(prefab);
                classroom.transform.SetPositionAndRotation(KezaPose.position, Quaternion.identity);
            }
            else
            {
                Destroy(classroom);
                classroom = null;
            }
            if (world != null) world.gameObject.SetActive(!show);
            ApplyLight();
        }

        /// <summary>True while a story plays in the classroom (it then needs no blackboard of its own).</summary>
        public bool InClassroom { get { return classroom != null; } }

        // ---- The park ----

        Material Paint(Color c)
        {
            Material m;
            if (materials.TryGetValue(c, out m)) return m;
            m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Standard"));
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            materials[c] = m;
            return m;
        }

        Transform world;
        GameObject ground;

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

        /// <summary>A built scenery piece from Resources/Props, or null when it has not been built.</summary>
        GameObject Prop(string name, Vector3 position, float yaw, float scale = 1f)
        {
            var prefab = Resources.Load<GameObject>("Props/" + name);
            if (prefab == null) return null;
            var go = Instantiate(prefab, world, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = prefab.transform.localScale * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        void BuildWorld()
        {
            world = new GameObject("Playground").transform;
            // The lawn stays when the classroom replaces the park, so the floor never ends in the sky.
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Lawn";
            Destroy(ground.GetComponent<Collider>());
            ground.transform.position = new Vector3(0f, -0.03f, 20f);
            ground.transform.localScale = new Vector3(18f, 1f, 18f);   // 180 m: the haze hides its edge
            var lawn = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Standard"));
            lawn.color = Color.white;
            lawn.mainTexture = GrassTexture();
            lawn.mainTextureScale = new Vector2(70f, 70f);
            if (lawn.HasProperty("_Glossiness")) lawn.SetFloat("_Glossiness", 0.05f);
            made.Add(lawn);
            ground.GetComponent<Renderer>().sharedMaterial = lawn;

            // A sandy path of stepping stones from the camera to Keza.
            for (int i = 0; i < 6; i++)
                Shape(PrimitiveType.Cylinder, "Stone", new Vector3(Mathf.Sin(i * 1.3f) * 0.12f, -0.027f, -1.3f - i * 0.55f), new Vector3(0.26f, 0.008f, 0.2f), new Color(0.47f, 0.44f, 0.4f), i * 40f);

            // The phone shows a tall, narrow view, so the scenery sits behind Keza: a row of trees and
            // a house in its garden in the distance, bushes and flowers on both sides of her.
            float[] treeX = { -5.5f, -3.2f, -1.2f, 1.4f, 3.4f, 5.6f, -7.5f, 7.8f, 0.2f, -9.5f, 10f };
            float[] treeZ = { 7f, 9.5f, 12f, 10f, 7.5f, 9f, 13f, 12.5f, 15f, 9f, 8f };
            for (int i = 0; i < treeX.Length; i++) Tree(new Vector3(treeX[i], 0f, treeZ[i]), 0.7f + (i % 3) * 0.2f, i);
            bool realGrass = Resources.Load<GameObject>("Props/grass") != null;
            for (int side = -1; side <= 1; side += 2)
            {
                Tree(new Vector3(side * 3.1f, 0f, 4.2f), 0.6f, side + 3);
                if (realGrass) continue;   // tufts of real grass instead of round bushes
                Shape(PrimitiveType.Sphere, "Bush", new Vector3(side * 1.7f, 0.2f, 1.6f), new Vector3(0.7f, 0.5f, 0.7f), new Color(0.22f, 0.45f, 0.22f));
                Shape(PrimitiveType.Sphere, "Bush", new Vector3(side * 2.1f, 0.16f, 0.4f), new Vector3(0.5f, 0.38f, 0.5f), new Color(0.26f, 0.5f, 0.24f));
            }
            // Tufts of real grass around the play area (never on the path or where the lessons happen).
            for (int i = 0; i < 22; i++)
            {
                float a = i * 2.39996f + 0.5f, r = 1.4f + (i * 0.61f) % 3.6f;
                var p = new Vector3(Mathf.Cos(a) * r, -0.03f, Mathf.Sin(a) * r + 0.6f);
                if (p.z < -0.6f && Mathf.Abs(p.x) < 1.1f) continue;
                Prop("grass", p, i * 47f, 0.8f + (i % 3) * 0.2f);
            }
            Color[] petals = { new Color(1f, 0.45f, 0.55f), new Color(1f, 0.85f, 0.25f), new Color(0.65f, 0.5f, 1f), Color.white };
            for (int i = 0; i < 18; i++)
            {
                float a = i * 2.39996f, r = 1.2f + (i * 0.37f) % 2.4f;   // a sunflower spiral, so they never bunch up
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (p.z < -0.8f && Mathf.Abs(p.x) < 0.8f) continue;   // keep the path clear
                Shape(PrimitiveType.Sphere, "Flower", p, new Vector3(0.045f, 0.03f, 0.045f), petals[i % petals.Length]);
            }

            if (Prop("house", new Vector3(-3.4f, -0.03f, 9f), 200f) == null) House(new Vector3(-2.4f, 0f, 6.5f), 18f);

            // People in the distance: a pair chatting, and others walking the paths, so the park feels alive.
            Person(1, new Vector3(2.3f, 0f, 9.5f), new Vector3(2.75f, 0f, 9.8f));
            Person(2, new Vector3(2.75f, 0f, 9.8f), new Vector3(2.3f, 0f, 9.5f));
            AddWalkers();

            if (RenderSettings.skybox == null)
            {
                // No sky: the sun and some clouds, far away, from simple shapes.
                var sunBall = Shape(PrimitiveType.Sphere, "Sun", new Vector3(9f, 11f, 30f), Vector3.one * 4f, new Color(1f, 0.86f, 0.3f));
                sunBall.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                for (int c = 0; c < 4; c++) Cloud(new Vector3(-14f + c * 9f, 7f + (c % 2) * 2f, 26f + (c % 3) * 4f));
            }
        }

        GameObject Person(int number, Vector3 at, Vector3 facing)
        {
            var p = Prop("person " + number, at, 0f);
            if (p == null) return null;
            var flat = Vector3.ProjectOnPlane(facing - at, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f) p.transform.rotation = Quaternion.LookRotation(flat);
            return p;
        }

        // ---- People walking about ----

        /// <summary>Someone walking round a path in the park, stopping now and then.</summary>
        class Walker
        {
            public Transform body, sway;   // sway: the scanned body that rocks from foot to foot (people with a skeleton step instead)
            public Vector3[] path;
            public int next;
            public float speed, wait, stride;
            public Quaternion swayRest;
        }

        readonly List<Walker> walkers = new List<Walker>();

        /// <summary>
        /// A child and grown-ups (the classmate, Mum and Dad models, when built, and scanned people) walk loops
        /// far behind the lesson: across the lawn, past the trees and the house. Never near the play area.
        /// </summary>
        void AddWalkers()
        {
            Walk(Extra("child"), 0.32f, 1.5f, new Vector3(-6f, 0f, 6.2f), new Vector3(-1.5f, 0f, 5.4f), new Vector3(2.5f, 0f, 6f), new Vector3(6.5f, 0f, 7f), new Vector3(1f, 0f, 8.2f));
            Walk(Extra("mum"), 0.26f, 4f, new Vector3(5.5f, 0f, 11f), new Vector3(1.5f, 0f, 10.5f), new Vector3(-2.2f, 0f, 11.5f), new Vector3(-6.5f, 0f, 10.4f), new Vector3(-1f, 0f, 12.6f));
            Walk(Extra("dad"), 0.3f, 2.5f, new Vector3(-7f, 0f, 8.4f), new Vector3(-4.6f, 0f, 7.6f), new Vector3(-1.2f, 0f, 8.8f), new Vector3(-5f, 0f, 9.6f));
            Walk(Prop("person 4", Vector3.zero, 0f), 0.24f, 0f, new Vector3(7f, 0f, 13.5f), new Vector3(3f, 0f, 12.8f), new Vector3(-3f, 0f, 14f), new Vector3(-7.5f, 0f, 13f), new Vector3(0f, 0f, 15f));
            Walk(Prop("person 6", Vector3.zero, 0f), 0.28f, 6f, new Vector3(-4f, 0f, 15.5f), new Vector3(1f, 0f, 14.8f), new Vector3(6f, 0f, 15.6f));
        }

        /// <summary>A built person model as an extra: no name over the head, nothing to tap, never part of a lesson.</summary>
        GameObject Extra(string role)
        {
            var prefab = ar.ExtraModel(role);
            if (prefab == null) return null;
            var go = Instantiate(prefab, world, false);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            var figure = go.GetComponent<ARFigure>();
            if (figure != null) figure.ShowName(false);
            return go;
        }

        void Walk(GameObject person, float speed, float startDelay, params Vector3[] path)
        {
            if (person == null) return;
            person.name = "Walker " + person.name;
            person.transform.localPosition = path[0];
            var w = new Walker { body = person.transform, path = path, next = 1, speed = speed, wait = startDelay };
            // A scanned body cannot step: it rocks from foot to foot. People with an ARFigure do that, or step, themselves.
            if (person.GetComponent<ARFigure>() == null && person.transform.childCount > 0)
            {
                w.sway = person.transform.GetChild(0);
                w.swayRest = w.sway.localRotation;
            }
            walkers.Add(w);
        }

        void MoveWalkers()
        {
            float dt = Time.deltaTime;
            foreach (var w in walkers)
            {
                if (w.body == null) continue;
                if (w.wait > 0f)
                {
                    w.wait -= dt;
                    if (w.sway != null) w.sway.localRotation = w.swayRest;
                    continue;
                }
                var to = w.path[w.next] - w.body.localPosition;
                to.y = 0f;
                if (to.magnitude < 0.05f)
                {
                    // At a corner: sometimes a pause, as if looking around, then on to the next point.
                    w.next = (w.next + 1) % w.path.Length;
                    w.wait = (w.next * 7 + w.path.Length) % 3 == 0 ? 2.5f : 0.3f;
                    continue;
                }
                var step = Vector3.ClampMagnitude(to, w.speed * dt);
                w.body.localPosition += step;
                w.body.localRotation = Quaternion.RotateTowards(w.body.localRotation, Quaternion.LookRotation(to), 140f * dt);
                if (w.sway == null) continue;
                w.stride += step.magnitude / 0.12f;
                w.sway.localRotation = w.swayRest * Quaternion.Euler(0f, 0f, Mathf.Sin(w.stride) * 3.5f);
            }
        }

        void Tree(Vector3 at, float scale, int seed)
        {
            // A pine from the house model, if built: four shapes, turned and sized so no two look alike.
            if (Prop("pine " + (1 + Mathf.Abs(seed) % 4), at + Vector3.down * 0.03f, seed * 73f, scale * 0.9f) != null) return;
            Shape(PrimitiveType.Cylinder, "Trunk", at + Vector3.up * 0.6f * scale, new Vector3(0.22f, 0.6f, 0.22f) * scale, new Color(0.55f, 0.36f, 0.22f));
            Color[] leaves = { new Color(0.27f, 0.6f, 0.3f), new Color(0.35f, 0.7f, 0.33f), new Color(0.22f, 0.52f, 0.33f) };
            var leaf = leaves[Mathf.Abs(seed) % leaves.Length];
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

        /// <summary>
        /// A tile of lawn made in code: soft patches of darker and lighter green, fine blades and a little soil,
        /// so the ground looks like grass close up and never like one flat colour.
        /// </summary>
        Texture2D GrassTexture()
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true) { name = "Lawn", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            var pixels = new Color32[n * n];
            Color dark = new Color(0.22f, 0.36f, 0.15f), light = new Color(0.38f, 0.52f, 0.24f), soil = new Color(0.38f, 0.32f, 0.22f);
            var random = new System.Random(7);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // Tiling noise: sampled on a circle in each direction so the edges of the tile meet.
                float u = x / (float)n * Mathf.PI * 2f, v = y / (float)n * Mathf.PI * 2f;
                float patches = Mathf.PerlinNoise(3f + Mathf.Cos(u) * 1.2f + Mathf.Sin(v) * 0.3f, 5f + Mathf.Sin(u) * 1.2f + Mathf.Cos(v) * 1.2f);
                float blades = (float)random.NextDouble();
                var c = Color.Lerp(dark, light, patches * 0.7f + blades * 0.45f - 0.1f);
                if (patches < 0.3f && blades > 0.93f) c = Color.Lerp(c, soil, 0.6f);
                pixels[y * n + x] = c;
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, true);
            made.Add(tex);
            return tex;
        }
    }
}
