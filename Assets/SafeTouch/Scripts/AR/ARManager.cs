using System;
using System.Collections.Generic;
using SafeTouch.UI;
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
        /// <summary>How Keza is shown: in the camera picture (no scanning), on a 3D playground, or on a scanned floor.</summary>
        public enum ViewMode { Camera, Playground, ScanFloor }

        [Tooltip("Camera (the default): the phone's camera picture with Keza standing in it at once, no scanning. Playground: a 3D lawn, no camera. ScanFloor: find a real floor or table first, then place Keza.")]
        [SerializeField] ViewMode viewMode = ViewMode.Camera;

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
        ARTouch touch;

        const float MaxDistanceFromAnchor = 2f;

        public event Action<ARCharacter> CharacterPlaced;

        /// <summary>True while the phone's camera picture is used; false on the 3D playground.</summary>
        public bool UsesAR { get { return Stage == null; } }

        /// <summary>The 3D playground, when the app is not in AR mode (zoom with Stage.Zoom).</summary>
        public Stage3D Stage { get; private set; }

        public ARCharacter Character { get { return character; } }
        public bool HasCharacter { get { return character != null; } }
        public bool HasPlacementPose { get { return hasPose; } }
        public Transform CameraTransform { get { return arCamera != null ? arCamera.transform : Camera.main.transform; } }
        public Material OpaqueMaterial { get { return opaqueMaterial; } }
        public Material TransparentMaterial { get { return transparentMaterial; } }
        public Transform AnchorRoot { get { return anchorRoot; } }

        /// <summary>Taps, drags, pinches and twists in the camera view.</summary>
        public ARTouch Touch { get { return touch; } }

        /// <summary>True while a lesson uses two fingers to resize and turn something, so the playground does not zoom too.</summary>
        public bool PinchTaken { get { return touch != null && touch.Manipulating; } }

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
            if (viewMode == ViewMode.Playground) StartPlayground();
            touch = GetComponent<ARTouch>();
            if (touch == null) touch = gameObject.AddComponent<ARTouch>();
            touch.Init(this);
        }

        void Update()
        {
            if (!placing) { SettleOnFloor(); return; }
            StartSession();   // also picks up a camera allowed in Android Settings while the app was away
            if (viewMode == ViewMode.Camera) { PlaceInCamera(); return; }
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
            if (Stage != null) { PlaceCharacter(Stage.KezaPose); return; }   // no scanning: Keza is on the playground at once
            placing = true;
            hasPose = false;
            SetPlanesVisible(viewMode == ViewMode.ScanFloor);   // camera mode finds floors quietly, without showing them
            StartSession();
        }

        // ---- Camera mode: the phone's camera picture, Keza in it at once, no scanning ----

        /// <summary>The 3D playground instead of the camera picture (also when the camera is refused or ARCore is missing).</summary>
        void StartPlayground()
        {
            if (Stage != null) return;
            if (session != null) session.enabled = false;
            SetPlanesVisible(false);
            Stage = gameObject.AddComponent<Stage3D>();
            Stage.Init(this, arCamera != null ? arCamera : Camera.main, opaqueMaterial);
        }

        float cameraWaitStart = -1f;

        /// <summary>Puts Keza in the camera picture as soon as the phone knows where it is: no floor to find first.</summary>
        void PlaceInCamera()
        {
            // No camera allowed, a phone without ARCore, or the Editor without AR: the 3D playground instead.
            if (cameraDenied || ARSession.state == ARSessionState.Unsupported || (Application.isEditor && !ARSupported()))
            {
                StartPlayground();
                PlaceCharacter(Stage.KezaPose);
                return;
            }
            if (!CameraPermission.Granted) return;   // the scan screen explains and asks for the camera
            if (cameraWaitStart < 0f) cameraWaitStart = Time.unscaledTime;
            // Wait until the phone tracks the room (usually under a second), but never more than 3 seconds.
            bool tracking = ARSession.state == ARSessionState.SessionTracking;
            if (!tracking && (ARSession.state != ARSessionState.SessionInitializing || Time.unscaledTime - cameraWaitStart < 3f)) return;
            cameraWaitStart = -1f;
            PlaceCharacter(InstantPose());
        }

        /// <summary>
        /// Where Keza appears in the camera picture: on the floor where the phone is looking, a little below the middle
        /// of the screen (so she is in view whether the phone points ahead or down), 0.9 to 3 m away, facing the phone.
        /// The floor is the one ARCore has found there, or else a guess about 1.1 m below the phone.
        /// </summary>
        public Pose InstantPose()
        {
            var cam = CameraTransform;
            var view = arCamera != null ? arCamera : Camera.main;
            var ray = view != null ? view.ViewportPointToRay(new Vector3(0.5f, 0.3f, 0f)) : new Ray(cam.position, cam.forward);
            var flat = Vector3.ProjectOnPlane(ray.direction, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            flat.Normalize();

            Vector3 spot = Vector3.zero;
            bool found = false;
            if (raycastManager != null && raycastManager.Raycast(ray, Hits, TrackableType.PlaneWithinInfinity))
            {
                foreach (var hit in Hits)   // nearest first
                {
                    var plane = planeManager != null ? planeManager.GetPlane(hit.trackableId) : null;
                    if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp || hit.pose.position.y > cam.position.y - 0.3f) continue;
                    float d = Vector3.ProjectOnPlane(hit.pose.position - cam.position, Vector3.up).magnitude;
                    if (d < 0.6f || d > 3.5f) continue;
                    spot = hit.pose.position;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                // No floor found there yet: guess it 1.1 m below the phone and stand where the aiming ray meets it.
                float floorY = cam.position.y - 1.1f;
                float down = -ray.direction.y;
                float distance = down > 0.01f ? (cam.position.y - floorY) / down * Vector3.ProjectOnPlane(ray.direction, Vector3.up).magnitude : 3f;
                spot = cam.position + flat * Mathf.Clamp(distance, 0.9f, 3f);
                spot.y = floorY;
            }
            var face = Vector3.ProjectOnPlane(cam.position - spot, Vector3.up);
            return new Pose(spot, Quaternion.LookRotation(face.sqrMagnitude > 0.0001f ? face.normalized : Vector3.back));
        }

        /// <summary>The height of the lowest floor (or table) ARCore has found under a point, below the phone.</summary>
        bool FloorUnder(Vector3 point, out float height)
        {
            height = 0f;
            if (planeManager == null) return false;
            bool found = false;
            float phone = CameraTransform.position.y;
            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp || plane.subsumedBy != null) continue;
                float y = plane.center.y;
                if (y > phone - 0.3f) continue;
                var local = plane.transform.InverseTransformPoint(point);
                var half = plane.size * 0.5f + Vector2.one * 0.5f;   // a little past its edges still counts
                if (Mathf.Abs(local.x - plane.centerInPlaneSpace.x) > half.x || Mathf.Abs(local.z - plane.centerInPlaneSpace.y) > half.y) continue;
                if (!found || y < height) { height = y; found = true; }
            }
            return found;
        }

        float nextSettle, settleY = float.NaN;

        /// <summary>
        /// Camera mode: once ARCore finds the real floor under Keza, she settles down onto it, gently, and then stays
        /// put. Small corrections ARCore makes to the floor afterwards are ignored, so she never keeps drifting.
        /// </summary>
        void SettleOnFloor()
        {
            if (viewMode != ViewMode.Camera || Stage != null || anchorRoot == null) return;
            if (float.IsNaN(settleY) && Time.unscaledTime >= nextSettle)
            {
                nextSettle = Time.unscaledTime + 0.5f;
                float floor;
                if (FloorUnder(anchorRoot.position, out floor) && Mathf.Abs(floor - anchorRoot.position.y) > 0.04f) settleY = floor;
            }
            if (float.IsNaN(settleY)) return;
            var p = anchorRoot.position;
            p.y = Mathf.MoveTowards(p.y, settleY, Time.deltaTime * 0.5f);
            anchorRoot.position = p;
            if (Mathf.Abs(p.y - settleY) < 0.001f) settleY = float.NaN;   // landed
        }

        /// <summary>True when Keza is somewhere in the picture (her middle, give or take a little).</summary>
        public bool CharacterInView
        {
            get
            {
                var view = arCamera != null ? arCamera : Camera.main;
                if (character == null || view == null) return false;
                var v = view.WorldToViewportPoint(character.transform.position + Vector3.up * 0.4f);
                return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
            }
        }

        /// <summary>Camera mode: Keza can be brought back into the picture ("Bring Keza back").</summary>
        public bool CanBringBack { get { return viewMode == ViewMode.Camera && Stage == null && character != null && anchorRoot != null; } }

        /// <summary>
        /// Brings Keza back in front of the phone after the child looked away. Everyone with her comes too, turned
        /// so they stand around her just as the child saw them before; the lesson carries on.
        /// </summary>
        public void BringIntoView()
        {
            if (!CanBringBack) return;
            var pose = InstantPose();
            var cam = CameraTransform.position;
            var from = character.transform.position;
            var before = Vector3.ProjectOnPlane(cam - from, Vector3.up);
            var after = Vector3.ProjectOnPlane(cam - pose.position, Vector3.up);
            if (before.sqrMagnitude > 0.0001f && after.sqrMagnitude > 0.0001f)
                anchorRoot.RotateAround(from, Vector3.up, Vector3.SignedAngle(before, after, Vector3.up));
            anchorRoot.position += pose.position - character.transform.position;
            settleY = float.NaN;
            nextSettle = 0f;
            ARFx.Ripple(pose.position, Color.white);
        }

        /// <summary>
        /// Makes Keza look bigger (more than 1) or smaller. On the playground the camera moves closer; in the camera
        /// picture Keza, and everyone with her, steps closer to the phone or further away. Her real size and the
        /// distances in the lessons never change.
        /// </summary>
        public void ChangeSize(float times)
        {
            if (Stage != null) { Stage.Zoom(times); return; }
            if (anchorRoot == null) return;
            var flat = Vector3.ProjectOnPlane(anchorRoot.position - CameraTransform.position, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) return;
            float distance = Mathf.Clamp(flat.magnitude / times, 0.6f, 5f);
            anchorRoot.position += flat.normalized * distance - flat;
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
                if (Stage != null) return ARStatus.Ready;
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
            if (viewMode == ViewMode.Camera) return ARStatus.Ready;   // no floor needed
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
            if (session == null || session.enabled || !CameraPermission.Granted || Stage != null) return;
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
            if (viewMode == ViewMode.Camera) { PlaceCharacter(InstantPose()); return true; }
            if (!hasPose || !SurfaceBigEnough) return false;   // keep scanning until the surface is big enough
            PlaceCharacter(currentPose);
            return true;
        }

        public void PlaceCharacter(Pose pose)
        {
            if (character == null)
            {
                var go = Instantiate(characterPrefab, pose.position, Quaternion.identity);
                character = GetOrAdd<ARCharacter>(go);
            }
            else
            {
                character.transform.position = pose.position;
            }
            // The guide carries the name the child chose (or "Keza").
            character.SetName(Core.Localization.CharacterName);
            ARFx.Ripple(pose.position, Color.white);

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
            if (ARSupported() && viewMode == ViewMode.ScanFloor) go.AddComponent<ARAnchor>();   // camera mode settles Keza on the floor itself
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
            // The modelled people only: the simple figure (capsule and spheres) stands in
            // just when no model has been built, even if an older scene still lists it.
            var choices = new List<GameObject>();
            foreach (var v in figureVariants) if (v != null && v != figurePrefab) choices.Add(v);
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
            var prefab = RolePrefabFor(role, label) ?? FigurePrefabFor(label);
            var go = Instantiate(prefab);
            var figure = GetOrAdd<ARFigure>(go);
            // Clothes in the role's colour, so the roles can be told apart; skin, eyes and hair keep their look.
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

        // ---- Touch: the child moves people with a finger (ARTouch) ----

        /// <summary>Lets a lesson stop a figure being dragged (or allow it again). Keza can always be dragged.</summary>
        public void SetMovable(ARFigure figure, bool canMove)
        {
            movable.Remove(figure);
            if (canMove) movable.Add(figure);
        }

        /// <summary>Forgets the lesson's figures; only Keza can be dragged.</summary>
        public void ClearMovable()
        {
            movable.Clear();
        }

        /// <summary>
        /// One step of the guide for the arrow buttons: up walks away from the
        /// phone, right walks to the right of the screen, so the buttons always
        /// match the view. A help for children who find dragging hard.
        /// </summary>
        public void Step(Vector2 direction)
        {
            if (character == null) return;
            var cam = CameraTransform;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var target = character.transform.position + (right * direction.x + forward * direction.y) * 0.18f;
            if (anchorRoot != null)
            {
                var offset = Vector3.ProjectOnPlane(target - anchorRoot.position, Vector3.up);
                if (offset.magnitude > MaxDistanceFromAnchor) return;   // stay near the placed spot
            }
            character.WalkTo(target, null);
        }

        public bool CanDrag(ARFigure figure)
        {
            return figure != null && (figure == character || movable.Contains(figure));
        }

        /// <summary>
        /// Moves a figure towards a floor point under the child's finger,
        /// smoothly, facing the way it walks, and never far from where Keza
        /// was placed.
        /// </summary>
        public void DragFigure(ARFigure figure, Vector3 point)
        {
            if (anchorRoot != null)
            {
                var offset = Vector3.ProjectOnPlane(point - anchorRoot.position, Vector3.up);
                if (offset.magnitude > MaxDistanceFromAnchor) point = anchorRoot.position + offset.normalized * MaxDistanceFromAnchor;
            }
            point.y = figure.transform.position.y;
            var from = figure.transform.position;
            var to = Vector3.Lerp(from, point, 1f - Mathf.Exp(-18f * Time.deltaTime));
            if ((to - from).sqrMagnitude > 0.000001f) figure.FaceTowards(to + (to - from) * 10f);
            figure.transform.position = to;
        }

        /// <summary>Which of Keza's bubble zones a floor point is in: "red", "yellow", "green" or "outside".</summary>
        public string ZoneAt(Vector3 point)
        {
            if (character == null) return "outside";
            float d = Vector3.ProjectOnPlane(point - character.transform.position, Vector3.up).magnitude;
            if (d <= 0.3f) return "red";
            if (d <= 0.6f) return "yellow";
            if (d <= 0.9f) return "green";
            return "outside";
        }

        /// <summary>Which of Keza's bubble zones a figure is standing in.</summary>
        public string ZoneOf(ARFigure figure)
        {
            return figure == null ? "outside" : ZoneAt(figure.transform.position);
        }

        /// <summary>
        /// True when the camera really tracks the room (a phone, or XR
        /// Simulation), so the child can walk up to things. False on the
        /// Editor's virtual floor, where "walk closer" can't be done.
        /// </summary>
        public bool CanWalkAround
        {
            get { return Stage == null && ARSupported() && ARSession.state == ARSessionState.SessionTracking; }
        }

        /// <summary>
        /// What happens when the child taps something no lesson is waiting
        /// for: Keza waves and says hello with the child's name, other people
        /// wave back, and the floor ripples.
        /// </summary>
        public void DefaultTap(ARTap tap)
        {
            var figure = tap.Find<ARFigure>();
            if (figure != null && figure == character)
            {
                character.Bounce();
                character.SayFor(string.Format(Core.Localization.T("keza.hello", "Hi! I'm {0}!"), Core.Localization.CharacterName), opaqueMaterial, 2.5f);
                return;
            }
            if (figure != null) { figure.Bounce(); return; }
            if (tap.hasFloor) ARFx.Ripple(tap.floor, Color.white);
        }

        /// <summary>
        /// Spawns a floating letter block that the child can grab with a tap
        /// (the Memory Quest's S-T-O-P). A soft glow on the floor below shows
        /// where it is.
        /// </summary>
        public GameObject SpawnSign(string text, Color color, Vector3 position)
        {
            // An unscaled root holds the block (which turns to the phone) and its glow
            // on the floor (which must stay flat), so both move together.
            var root = new GameObject("Sign " + text);
            root.transform.position = position;

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Block";
            block.GetComponent<BoxCollider>().size = new Vector3(1.6f, 1.6f, 4f);   // bigger than it looks: easy to tap
            block.transform.SetParent(root.transform, false);
            block.transform.localScale = new Vector3(0.16f, 0.16f, 0.04f);
            // Unlit, so the letter is the same bright colour from every side of the room.
            // Drawn after everything solid (this shader writes no depth, so anything drawn
            // later would cover it) and before the white letter on it (3000).
            var solid = ARFx.Unlit(color);
            solid.renderQueue = 2990;
            block.GetComponent<Renderer>().sharedMaterial = solid;

            var label = new GameObject("Text");
            label.transform.SetParent(block.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.font = ARFx.Font;
            label.GetComponent<MeshRenderer>().sharedMaterial = ARFx.Font.material;
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 96;
            mesh.characterSize = 0.06f;
            mesh.color = Color.white;
            block.AddComponent<Billboard>();

            var glow = ARFx.FloorQuad("Glow", ARFx.SoftDot, Theme.WithAlpha(color, 0.55f), 0.3f);
            glow.transform.position = new Vector3(position.x, character != null ? character.transform.position.y + 0.005f : position.y - 0.3f, position.z);
            glow.transform.SetParent(root.transform, true);
            FxAnimator.Pulse(glow);

            if (anchorRoot != null) root.transform.SetParent(anchorRoot, true);
            return root;
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
