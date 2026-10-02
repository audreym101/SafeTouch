using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SafeTouch.AR
{
    /// <summary>
    /// Finds a floor or table with ARCore plane detection (FR1), shows a
    /// placement reticle and places the guide character, Keza, on it (FR2).
    /// The character is anchored so it stays put while the child walks
    /// around it.
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] GameObject characterPrefab;
        [SerializeField] GameObject figurePrefab;
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

        void Update()
        {
            if (!placing) return;
            hasPose = DetectPlane();
            if (reticle == null) return;
            reticle.gameObject.SetActive(hasPose);
            if (hasPose) reticle.SetPositionAndRotation(currentPose.position, currentPose.rotation);
        }

        /// <summary>Starts looking for a surface and shows plane outlines.</summary>
        public void BeginPlacement()
        {
            placing = true;
            hasPose = false;
            SetPlanesVisible(true);
        }

        public void CancelPlacement()
        {
            placing = false;
            hasPose = false;
            if (reticle != null) reticle.gameObject.SetActive(false);
            SetPlanesVisible(false);
        }

        /// <summary>
        /// Raycasts from the centre of the screen against detected planes.
        /// Returns true when a surface is under the reticle.
        /// </summary>
        public bool DetectPlane()
        {
            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (raycastManager != null && raycastManager.Raycast(centre, Hits, TrackableType.PlaneWithinPolygon))
            {
                currentPose = Hits[0].pose;
                return true;
            }
            if (editorFloorFallback && Application.isEditor && !ARSupported())
            {
                // Virtual floor 1.2 m below the camera, 1.5 m ahead.
                var cam = CameraTransform;
                var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                if (forward == Vector3.zero) forward = Vector3.forward;
                currentPose = new Pose(cam.position + forward * 1.5f + Vector3.down * 1.2f, Quaternion.LookRotation(forward));
                return true;
            }
            return false;
        }

        /// <summary>Called by the Place button on the scan screen.</summary>
        public bool PlaceAtReticle()
        {
            if (!hasPose) return false;
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
            if (reticle != null) reticle.gameObject.SetActive(false);
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

        /// <summary>Spawns a simple figure (visitor or trusted adult) at a point around Keza.</summary>
        public ARFigure SpawnFigure(string label, Color color, float radius, float angleDegrees)
        {
            var go = Instantiate(figurePrefab != null ? figurePrefab : characterPrefab);
            var figure = GetOrAdd<ARFigure>(go);
            figure.Setup(label, color, opaqueMaterial);
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
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(visible);
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
