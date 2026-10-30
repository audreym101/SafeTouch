using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SafeTouch.AR
{
    /// <summary>
    /// Finds a floor or table with ARCore plane detection (FR1), horizontal
    /// surfaces facing up only, shows each one as a rectangle and a square
    /// reticle where Keza will stand, and places her (FR2) once the surface
    /// is at least MinSurfaceSide wide and deep. She is anchored so she stays
    /// put while the child walks around her.
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        [Tooltip("Off (the default): a 3D playground, where Keza appears at once and the child makes her bigger or smaller; no camera or scanning. On: augmented reality on a real floor or table.")]
        [SerializeField] bool useAR = false;

        [Header("Placement")]
        [SerializeField] GameObject characterPrefab;
        [SerializeField] GameObject figurePrefab;
        [Tooltip("Optional people built from the models; each name always gets the same one. Empty: figurePrefab for everyone.")]
        [SerializeField] GameObject[] figureVariants = new GameObject[0];
        [SerializeField] Transform reticle;

        [Header("AR Foundation")]
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ARSession session;
        [SerializeField] Camera arCamera;

        [Header("Materials")]
        [SerializeField] Material opaqueMaterial;
        [SerializeField] Material transparentMaterial;

        [Tooltip("In the Editor without XR Simulation, place on a virtual floor so lessons can be tested on a laptop.")]
        [SerializeField] bool editorFloorFallback = true;

        static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        ARCharacter character;
        Transform anchorRoot;
        Pose currentPose;
        bool hasPose;
        bool placing;
        readonly List<ARFigure> movable = new List<ARFigure>();
        int selectedIndex;

        /// <summary>One tap on a move button walks the selected character this far, in metres.</summary>
        public const float MoveStep = 0.15f;
        const float MaxDistanceFromAnchor = 2f;

        public event Action<ARCharacter> CharacterPlaced;

        /// <summary>True for augmented reality (the phone's camera, a real floor); false for the 3D playground.</summary>
        public bool UsesAR { get { return useAR; } }

        /// <summary>True while a lesson uses two fingers itself, so the playground does not zoom too (none does yet).</summary>
        public bool PinchTaken { get { return false; } }

        /// <summary>The 3D playground, when the app is not in AR mode (zoom with Stage.Zoom).</summary>
        public Stage3D Stage { get; private set; }

        public ARCharacter Character { get { return character; } }
        public bool HasCharacter { get { return character != null; } }
        public bool HasPlacementPose { get { return hasPose; } }
        public Transform CameraTransform { get { return arCamera != null ? arCamera.transform : Camera.main.transform; } }
        public Material OpaqueMaterial { get { return opaqueMaterial; } }
        public Material TransparentMaterial { get { return transparentMaterial; } }
        public Transform AnchorRoot { get { return anchorRoot; } }

        /// <summary>The character the on-screen move buttons control: Keza, or a figure the lesson placed.</summary>
        public ARFigure Selected
        {
            get
            {
                movable.RemoveAll(f => f == null);
                if (character == null && movable.Count == 0) return null;
                if (selectedIndex <= 0 || selectedIndex > movable.Count) return character;
                return movable[selectedIndex - 1];
            }
        }

        /// <summary>
        /// Keza needs a surface at least this wide and deep (metres) before she
        /// can be placed: room for her and most of her bubble. A table works;
        /// the floor gives the whole 0.9 m green zone.
        /// </summary>
        public const float MinSurfaceSide = 0.75f;

        ARPlane targetPlane;

        /// <summary>Width and depth (metres) of the surface under the reticle; zero when there is none.</summary>
        public Vector2 SurfaceSize { get; private set; }

        /// <summary>True when the surface under the reticle is big enough for Keza.</summary>
        public bool SurfaceBigEnough
        {
            get { return hasPose && SurfaceSize.x >= MinSurfaceSide && SurfaceSize.y >= MinSurfaceSide; }
        }

        void Awake()
        {
            // Floors and tables only: walls and ceilings are never detected or used.
            if (planeManager != null) planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            if (!useAR)
            {
                // The 3D playground: no AR session, so the phone's camera never opens.
                if (session != null) session.enabled = false;
                Stage = gameObject.AddComponent<Stage3D>();
                Stage.Init(this, arCamera != null ? arCamera : Camera.main, opaqueMaterial);
            }
        }

        void Update()
        {
            if (!placing) return;
            StartSession();   // also picks up a camera allowed in Android Settings while the app was away
            hasPose = CameraPermission.Granted && DetectPlane();
            UpdateSurfaceHighlight();
            if (reticle == null) return;
            var square = reticle.GetComponent<PlacementReticle>();
            if (square != null)
            {
                if (hasPose) square.Show(currentPose, CameraTransform.position, SurfaceBigEnough);
                else square.Hide();
                return;
            }
            reticle.gameObject.SetActive(hasPose);
            if (hasPose) reticle.SetPositionAndRotation(currentPose.position, currentPose.rotation);
        }

        /// <summary>Brightens the rectangle of the surface the reticle is on.</summary>
        void UpdateSurfaceHighlight()
        {
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables)
            {
                var visual = plane.GetComponent<PlaneVisual>();
                if (visual != null) visual.Highlighted = plane == targetPlane;
            }
        }

        /// <summary>Starts looking for a surface and shows plane outlines.</summary>
        public void BeginPlacement()
        {
            if (!useAR) { PlaceCharacter(Stage.KezaPose); return; }   // no scanning: Keza is on the playground at once
            placing = true;
            hasPose = false;
            SetPlanesVisible(true);
            StartSession();
        }

        // ---- Camera and AR start-up ----

        /// <summary>What the "find the floor" screen should tell the child.</summary>
        public enum ARStatus
        {
            NeedsCamera,    // not asked yet: explain, then ask
            CameraDenied,   // the child said no
            Preparing,      // checking or installing ARCore, starting the camera
            Unsupported,    // this phone cannot run ARCore
            Searching,      // camera on, looking for a floor or table
            TooSmall,       // a floor or table is found but is still smaller than MinSurfaceSide
            Ready           // a surface is under the reticle
        }

        bool cameraDenied;

        /// <summary>False once Android will no longer show the camera question (only Settings can change it).</summary>
        public bool CanAskForCamera { get; private set; } = true;

        public ARStatus Status
        {
            get
            {
                if (!useAR) return ARStatus.Ready;
                if (!CameraPermission.Granted) return cameraDenied ? ARStatus.CameraDenied : ARStatus.NeedsCamera;
                if (editorFloorFallback && Application.isEditor && !ARSupported())
                    return Found();
                switch (ARSession.state)
                {
                    case ARSessionState.Unsupported: return ARStatus.Unsupported;
                    case ARSessionState.SessionInitializing:
                    case ARSessionState.SessionTracking: return Found();
                    default: return ARStatus.Preparing;
                }
            }
        }

        ARStatus Found()
        {
            if (!hasPose) return ARStatus.Searching;
            return SurfaceBigEnough ? ARStatus.Ready : ARStatus.TooSmall;
        }

        void HideReticle()
        {
            if (reticle == null) return;
            var square = reticle.GetComponent<PlacementReticle>();
            if (square != null) square.Hide();
            else reticle.gameObject.SetActive(false);
        }

        /// <summary>Shows Android's camera question; once allowed, AR starts.</summary>
        public void RequestCamera(Action answered)
        {
            CameraPermission.Request((granted, canAskAgain) =>
            {
                cameraDenied = !granted;
                CanAskForCamera = canAskAgain;
                if (granted) StartSession();
                if (answered != null) answered();
            });
        }

        /// <summary>
        /// Starts ARCore. The scene keeps the session off until now, so the
        /// camera never opens before the child has agreed to it.
        /// </summary>
        public void StartSession()
        {
            if (session == null || session.enabled || !CameraPermission.Granted || !useAR) return;
            session.enabled = true;
        }

        public void CancelPlacement()
        {
            placing = false;
            hasPose = false;
            HideReticle();
            SetPlanesVisible(false);
        }

        /// <summary>
        /// Raycasts from the centre of the screen against detected planes.
        /// Returns true when a surface is under the reticle.
        /// </summary>
        public bool DetectPlane()
        {
            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            targetPlane = null;
            SurfaceSize = Vector2.zero;
            // Within bounds: the hit area is the rectangle the child sees, not the ragged polygon.
            if (raycastManager != null && raycastManager.Raycast(centre, Hits, TrackableType.PlaneWithinBounds))
            {
                foreach (var hit in Hits)   // nearest first
                {
                    var plane = planeManager != null ? planeManager.GetPlane(hit.trackableId) : null;
                    if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp || plane.subsumedBy != null) continue;
                    currentPose = hit.pose;
                    targetPlane = plane;
                    SurfaceSize = plane.size;
                    return true;
                }
            }
            if (editorFloorFallback && Application.isEditor && !ARSupported())
            {
                // Virtual floor 1.2 m below the camera, 1.5 m ahead, big enough to place on.
                var cam = CameraTransform;
                var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                if (forward == Vector3.zero) forward = Vector3.forward;
                currentPose = new Pose(cam.position + forward * 1.5f + Vector3.down * 1.2f, Quaternion.LookRotation(forward));
                SurfaceSize = new Vector2(3f, 3f);
                return true;
            }
            return false;
        }

        /// <summary>Called by the Place button on the scan screen.</summary>
        public bool PlaceAtReticle()
        {
            if (!hasPose || !SurfaceBigEnough) return false;   // keep scanning until the surface is big enough
            PlaceCharacter(currentPose);
            return true;
        }

        public void PlaceCharacter(Pose pose)
        {
            if (character == null)
            {
                var go = Instantiate(characterPrefab, pose.position, Quaternion.identity);
                go.name = "Keza (guide character)";
                character = GetOrAdd<ARCharacter>(go);
            }
            else
            {
                character.transform.position = pose.position;
            }

            character.FaceTowards(CameraTransform.position);
            AnchorCharacter();

            placing = false;
            hasPose = false;
            HideReticle();
            SetPlanesVisible(false);
            if (CharacterPlaced != null) CharacterPlaced(character);
        }

        /// <summary>
        /// Anchors Keza to the real-world surface. The ARAnchor sits on a
        /// parent object because ARCore moves anchored transforms every frame;
        /// Keza herself stays free to walk around inside it.
        /// </summary>
        public void AnchorCharacter()
        {
            if (character == null) return;
            if (anchorRoot != null)
            {
                character.transform.SetParent(null, true);
                Destroy(anchorRoot.gameObject);
            }
            var go = new GameObject("Keza anchor");
            go.transform.SetPositionAndRotation(character.transform.position, Quaternion.identity);
            if (ARSupported()) go.AddComponent<ARAnchor>();
            anchorRoot = go.transform;
            character.transform.SetParent(anchorRoot, true);
        }

        /// <summary>
        /// Shows the tone the child picked on Keza. With animate the colour
        /// blends and sparkles; used live from the "Keza's look" screen.
        /// </summary>
        public void ApplySkinTone(Data.SkinTone tone, bool animate)
        {
            if (character == null) return;
            GetOrAdd<SkinToneApplier>(character.gameObject).SetTone(tone, animate);
        }

        /// <summary>Which person model plays a named visitor or helper; the same name always gets the same one.</summary>
        GameObject FigurePrefabFor(string label)
        {
            var choices = new List<GameObject>();
            foreach (var v in figureVariants) if (v != null) choices.Add(v);
            if (choices.Count == 0) return figurePrefab != null ? figurePrefab : characterPrefab;
            uint hash = 2166136261;   // FNV-1a, stable between runs (like SkinTones.ForName)
            foreach (char c in label ?? "") { hash ^= c; hash *= 16777619; }
            return choices[(int)((hash >> 8) % (uint)choices.Count)];
        }

        /// <summary>A person model kept for one role, e.g. "mum" or "dad" (set up by SafeTouch > Build Character Prefabs).</summary>
        [System.Serializable]
        public class RolePrefab
        {
            public string role;
            public GameObject prefab;
        }

        [Tooltip("Models for particular roles. \"parent\" uses mum or dad; any role not listed uses the figure variants.")]
        [SerializeField] RolePrefab[] rolePrefabs = new RolePrefab[0];

        /// <summary>Roles that are Keza's family: they take the skin tone the child picked for Keza.</summary>
        static bool IsFamily(string role)
        {
            return role == "parent" || role == "mum" || role == "dad";
        }

        GameObject RolePrefabFor(string role, string label)
        {
            if (string.IsNullOrEmpty(role)) return null;
            if (role == "parent")
            {
                // "Mum or Dad": the same label always gets the same parent.
                uint hash = 2166136261;
                foreach (char c in label ?? "") { hash ^= c; hash *= 16777619; }
                return RolePrefabFor((hash >> 4) % 2 == 0 ? "mum" : "dad", label) ?? RolePrefabFor("mum", label) ?? RolePrefabFor("dad", label);
            }
            foreach (var r in rolePrefabs) if (r != null && r.role == role && r.prefab != null) return r.prefab;
            return null;
        }

        /// <summary>Spawns a figure (visitor or trusted adult) at a point around Keza. role picks a model, e.g. "parent".</summary>
        public ARFigure SpawnFigure(string label, Color color, float radius, float angleDegrees, string role = null)
        {
            var go = Instantiate(RolePrefabFor(role, label) ?? FigurePrefabFor(label));
            var figure = GetOrAdd<ARFigure>(go);
            figure.Setup(label, color, opaqueMaterial);
            // A mix of skin tones in every lesson; the same name always gets the same tone.
            // Mum and Dad match Keza, so they look like her family.
            var kezaTone = character != null ? character.GetComponent<SkinToneApplier>() : null;
            var tone = IsFamily(role) && kezaTone != null && !string.IsNullOrEmpty(kezaTone.ToneId)
                ? Data.SkinTones.Get(kezaTone.ToneId)
                : Data.SkinTones.ForName(label);
            GetOrAdd<SkinToneApplier>(go).SetTone(tone, false);
            go.transform.position = PointAroundCharacter(radius, angleDegrees);
            if (anchorRoot != null) go.transform.SetParent(anchorRoot, true);
            figure.FaceTowards(character.transform.position);
            movable.Add(figure);
            return figure;
        }

        // ---- Move buttons (LessonScreen): the child walks the characters around ----

        /// <summary>Switches the move buttons to the next character in the room (Keza first).</summary>
        public ARFigure SelectNext()
        {
            movable.RemoveAll(f => f == null);
            selectedIndex = (selectedIndex + 1) % (movable.Count + 1);
            var selected = Selected;
            if (selected != null) selected.Bounce();
            return selected;
        }

        /// <summary>Lets a lesson take a figure off (or put it back on) the move buttons.</summary>
        public void SetMovable(ARFigure figure, bool canMove)
        {
            movable.Remove(figure);
            if (canMove) movable.Add(figure);
            selectedIndex = 0;
        }

        /// <summary>Forgets the lesson's figures; the move buttons go back to Keza.</summary>
        public void ClearMovable()
        {
            movable.Clear();
            selectedIndex = 0;
        }

        /// <summary>
        /// Steps the selected character across the floor. The direction is
        /// relative to the phone: up walks away from the child, right walks
        /// to the right of the screen, so the buttons always match the view.
        /// </summary>
        public void Nudge(Vector2 direction)
        {
            var figure = Selected;
            if (figure == null || figure.IsMoving) return;
            var cam = CameraTransform;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var target = figure.transform.position + (right * direction.x + forward * direction.y) * MoveStep;
            if (anchorRoot != null)
            {
                var offset = Vector3.ProjectOnPlane(target - anchorRoot.position, Vector3.up);
                if (offset.magnitude > MaxDistanceFromAnchor) return;   // stay near the placed spot
            }
            figure.WalkTo(target, null);
        }

        /// <summary>Which of Keza's bubble zones a figure is standing in: "red", "yellow", "green" or "outside".</summary>
        public string ZoneOf(ARFigure figure)
        {
            if (figure == null || character == null) return "outside";
            float d = Vector3.ProjectOnPlane(figure.transform.position - character.transform.position, Vector3.up).magnitude;
            if (d <= 0.3f) return "red";
            if (d <= 0.6f) return "yellow";
            if (d <= 0.9f) return "green";
            return "outside";
        }

        /// <summary>
        /// Spawns a floating block with a word or letter on it, used by the
        /// Memory Quest to build S-T-O-P above Keza.
        /// </summary>
        public GameObject SpawnSign(string text, Color color, Vector3 position)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Sign " + text;
            Destroy(block.GetComponent<Collider>());
            block.transform.position = position;
            block.transform.localScale = new Vector3(0.2f, 0.2f, 0.05f);
            var r = block.GetComponent<Renderer>();
            if (opaqueMaterial != null) r.material = new Material(opaqueMaterial);
            r.material.color = color;

            var label = new GameObject("Text");
            label.transform.SetParent(block.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 96;
            mesh.characterSize = 0.06f;
            mesh.color = Color.white;
            block.AddComponent<Billboard>();

            if (anchorRoot != null) block.transform.SetParent(anchorRoot, true);
            return block;
        }

        /// <summary>
        /// A floor point around Keza. Angle 0 points from Keza towards the
        /// phone and 180 is behind her, so lessons bring visitors in from the
        /// sides and the far side where the child can see them.
        /// </summary>
        public Vector3 PointAroundCharacter(float radius, float angleDegrees)
        {
            var centre = character.transform.position;
            var toCamera = Vector3.ProjectOnPlane(CameraTransform.position - centre, Vector3.up).normalized;
            if (toCamera == Vector3.zero) toCamera = Vector3.back;
            var dir = Quaternion.AngleAxis(angleDegrees, Vector3.up) * toCamera;
            return centre + dir * radius;
        }

        public void SetPlanesVisible(bool visible)
        {
            // The rectangles read this flag every frame, so surfaces ARCore finds
            // after Keza is placed stay hidden too.
            PlaneVisual.ShowSurfaces = visible;
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables)
                if (plane.GetComponent<PlaneVisual>() == null) plane.gameObject.SetActive(visible);
        }

        /// <summary>Removes Keza so the child can place her somewhere new.</summary>
        public void ResetPlacement()
        {
            if (anchorRoot != null) Destroy(anchorRoot.gameObject);
            else if (character != null) Destroy(character.gameObject);
            character = null;
            anchorRoot = null;
            ClearMovable();
        }

        // Unity's missing-component "null" is not C# null, so no ?? here.
        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        bool ARSupported()
        {
            return ARSession.state != ARSessionState.Unsupported && ARSession.state != ARSessionState.None;
        }
    }
}
