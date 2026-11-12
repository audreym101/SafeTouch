using System;
using System.Collections.Generic;
using SafeTouch.UI;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// Keza's 3D world and the people in it. Keza (or Ganza) appears at once
    /// on the 3D playground (Stage3D), with no camera and no floor to find
    /// (FR2), and each lesson brings its visitors, helpers and props around
    /// her, in metres, so the bubble zones (0.3, 0.6 and 0.9 m) are true
    /// distances. The child moves people with a finger or the move pad.
    ///
    /// The class keeps the name ARManager from the class diagram in the
    /// approved proposal; since the scope change it holds no AR code.
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        [Header("Characters")]
        [SerializeField] GameObject characterPrefab;
        [Tooltip("Optional: a boy model for the boy guide. Empty: a boy built from simple shapes (BoyModel).")]
        [SerializeField] GameObject boyPrefab;
        [SerializeField] GameObject figurePrefab;
        [Tooltip("Optional people built from the models; each name always gets the same one. Empty: figurePrefab for everyone.")]
        [SerializeField] GameObject[] figureVariants = new GameObject[0];

        [Header("3D world")]
        [Tooltip("The camera that looks at the 3D world. Empty: the main camera.")]
        [SerializeField] Camera arCamera;

        [Header("Materials")]
        [SerializeField] Material opaqueMaterial;
        [SerializeField] Material transparentMaterial;

        ARCharacter character;
        Transform anchorRoot;
        readonly List<ARFigure> movable = new List<ARFigure>();
        ARTouch touch;

        /// <summary>How far (metres) people may walk from where Keza stands.</summary>
        const float MaxDistanceFromAnchor = 2f;

        public event Action<ARCharacter> CharacterPlaced;

        /// <summary>Keza's 3D world: the park, the camera that looks at it, zoom and orbit.</summary>
        public Stage3D Stage { get; private set; }

        public ARCharacter Character { get { return character; } }
        public bool HasCharacter { get { return character != null; } }
        public Transform CameraTransform { get { return arCamera != null ? arCamera.transform : Camera.main.transform; } }
        public Material OpaqueMaterial { get { return opaqueMaterial; } }
        public Material TransparentMaterial { get { return transparentMaterial; } }

        /// <summary>The group that holds Keza and everyone in the lesson with her.</summary>
        public Transform AnchorRoot { get { return anchorRoot; } }

        /// <summary>Taps, drags, pinches and twists in the 3D view.</summary>
        public ARTouch Touch { get { return touch; } }

        /// <summary>True while a lesson uses two fingers to resize and turn something, so the playground does not zoom too.</summary>
        public bool PinchTaken { get { return touch != null && touch.Manipulating; } }

        void Awake()
        {
            Stage = gameObject.AddComponent<Stage3D>();
            Stage.Init(this, arCamera != null ? arCamera : Camera.main, opaqueMaterial);
            touch = GetComponent<ARTouch>();
            if (touch == null) touch = gameObject.AddComponent<ARTouch>();
            touch.Init(this);
        }

        /// <summary>Puts the guide on the playground, if they are not there yet. A lesson starts straight after.</summary>
        public void PlaceGuide()
        {
            PlaceCharacter(Stage.KezaPose);
        }

        /// <summary>
        /// Makes Keza look bigger (more than 1) or smaller: the camera moves closer or further away. Her real
        /// size and the distances in the lessons never change.
        /// </summary>
        public void ChangeSize(float times)
        {
            Stage.Zoom(times);
        }

        bool boyGuide;

        /// <summary>The helper the child picked: a boy (true) or Keza, a girl (false).</summary>
        public bool BoyGuide { get { return boyGuide; } }

        /// <summary>
        /// Picks the girl or the boy helper. If one is already in the room, the other takes their place
        /// at the same spot (only outside lessons: the look and name screens).
        /// </summary>
        public void SetGuide(bool boy)
        {
            if (boy == boyGuide) return;
            boyGuide = boy;
            ReplaceGuide();
        }

        [Header("Ages 9 to 11 (Guardian path)")]
        [Tooltip("The girl helper for ages 9 to 11 (SafeTouch > 8 makes it from a downloaded model). Empty: the same as 6 to 8.")]
        [SerializeField] GameObject olderGirlPrefab;
        [Tooltip("The boy helper for ages 9 to 11. Empty: the same as 6 to 8.")]
        [SerializeField] GameObject olderBoyPrefab;
        [Tooltip("People for ages 9 to 11: visitors, helpers and the people in the stories. Empty: the same as 6 to 8.")]
        [SerializeField] GameObject[] olderFigureVariants = new GameObject[0];

        bool older;

        /// <summary>True on the 9 to 11 path, which can have its own, more lifelike, characters.</summary>
        public bool Older { get { return older; } }

        /// <summary>
        /// The age path: "9-11" uses the 9 to 11 characters where they have been set, "6-8" the others.
        /// A helper already in the room is swapped at the same spot (only outside lessons).
        /// </summary>
        public void SetAgeGroup(string ageGroup)
        {
            bool isOlder = ageGroup == "9-11";
            if (isOlder == older) return;
            older = isOlder;
            if (Stage != null) Stage.SetOlderLook(older);   // 9 to 11: the warmer, more dramatic light
            ReplaceGuide();
        }

        void ReplaceGuide()
        {
            if (character == null) return;
            var pose = new Pose(character.transform.position, character.transform.rotation);
            ClearMovable();
            if (anchorRoot != null) Destroy(anchorRoot.gameObject);
            else Destroy(character.gameObject);
            character = null;
            anchorRoot = null;
            PlaceCharacter(pose);
        }

        GameObject GirlPrefab { get { return older && olderGirlPrefab != null ? olderGirlPrefab : characterPrefab; } }

        GameObject BoyPrefab { get { return older && olderBoyPrefab != null ? olderBoyPrefab : boyPrefab; } }

        /// <summary>A boy: the boy model for this age if there is one, otherwise one built from shapes.</summary>
        GameObject NewBoy()
        {
            if (BoyPrefab != null) return Instantiate(BoyPrefab);
            float scale = characterPrefab != null ? characterPrefab.transform.localScale.y : 1f;
            return BoyModel.Build(opaqueMaterial, scale);
        }

        GameObject NewGuide()
        {
            return boyGuide ? NewBoy() : Instantiate(GirlPrefab);
        }

        public void PlaceCharacter(Pose pose)
        {
            if (character == null)
            {
                var go = NewGuide();
                go.transform.SetPositionAndRotation(pose.position, Quaternion.identity);
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

            if (CharacterPlaced != null) CharacterPlaced(character);
        }

        /// <summary>
        /// Puts Keza in a group of her own at the spot where she stands; each lesson's people and props join
        /// it. Keza herself stays free to walk around inside it.
        /// </summary>
        public void AnchorCharacter()
        {
            if (character == null) return;
            if (anchorRoot != null)
            {
                character.transform.SetParent(null, true);
                Destroy(anchorRoot.gameObject);
            }
            var go = new GameObject("Keza group");
            go.transform.SetPositionAndRotation(character.transform.position, Quaternion.identity);
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
            var people = older && olderFigureVariants.Length > 0 ? olderFigureVariants : figureVariants;   // 9 to 11: their own people, if set
            foreach (var v in people) if (v != null && v != figurePrefab) choices.Add(v);
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
            // Other children (a friend, a classmate, a cousin): the classmate model for 6 to 8 (Keza's age, but
            // their own face, hair and clothes), the older girl for 9 to 11; Keza's model if neither is built.
            if (role == "child") return (!older ? RoleModel("child") : null) ?? GirlPrefab;
            if (role == "parent")
            {
                // "Mum or Dad": the same label always gets the same parent.
                uint hash = 2166136261;
                foreach (char c in label ?? "") { hash ^= c; hash *= 16777619; }
                return RolePrefabFor((hash >> 4) % 2 == 0 ? "mum" : "dad", label) ?? RolePrefabFor("mum", label) ?? RolePrefabFor("dad", label);
            }
            return RoleModel(role);
        }

        GameObject RoleModel(string role)
        {
            foreach (var r in rolePrefabs) if (r != null && r.role == role && r.prefab != null) return r.prefab;
            return null;
        }

        /// <summary>
        /// A model for the people who walk about in the 3D park ("mum", "dad" or "child"), or null when it
        /// has not been built. They are extras: never Keza, never a person in a lesson.
        /// </summary>
        public GameObject ExtraModel(string role)
        {
            return RoleModel(role);
        }

        /// <summary>Spawns a figure (visitor or trusted adult) at a point around Keza. role picks a model, e.g. "parent".</summary>
        public ARFigure SpawnFigure(string label, Color color, float radius, float angleDegrees, string role = null)
        {
            // "boy": another boy in a story, built like the boy guide.
            var go = role == "boy"
                ? NewBoy()
                : Instantiate(RolePrefabFor(role, label) ?? FigurePrefabFor(label));
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
            // Other children wear the role's colour: on the lifelike child model through the skin-tone shader's
            // tint, on the boy built from shapes through his T-shirt.
            if (role == "child" || (role == "boy" && go.GetComponent<BoyMotion>() == null)) DressChild(go, color);
            else if (role == "boy") BoyModel.Dress(go, color);
            go.transform.position = PointAroundCharacter(radius, angleDegrees);
            if (anchorRoot != null) go.transform.SetParent(anchorRoot, true);
            figure.FaceTowards(character.transform.position);
            movable.Add(figure);
            return figure;
        }

        /// <summary>
        /// Another child on the child model, so they never look like Keza's twin: the Skin Tone shader's
        /// tint colours their clothes and hair in the role's colour, and its mask keeps their skin.
        /// </summary>
        static void DressChild(GameObject go, Color color)
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                var materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty("_SkinMask")) continue;
                    r.GetPropertyBlock(block, i);
                    block.SetColor("_Color", Color.Lerp(color, Color.white, 0.3f));
                    r.SetPropertyBlock(block, i);
                }
            }
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
            Select(null);
        }

        ARFigure selected;
        GameObject selectedRing;

        /// <summary>
        /// The person the move pad walks and turns, and a two-finger twist turns: the one the child last
        /// touched or dragged, if they may still be moved; otherwise the guide.
        /// </summary>
        public ARFigure Selected
        {
            get { return selected != null && CanDrag(selected) ? selected : character; }
        }

        /// <summary>Picks the person the move pad acts on (null: the guide); a ring glows at their feet for a moment.</summary>
        public void Select(ARFigure figure)
        {
            if (selectedRing != null) Destroy(selectedRing);
            selectedRing = null;
            selected = figure;
            if (figure == null || !CanDrag(figure)) return;
            selectedRing = ARFx.FloorQuad("Picked", ARFx.Ring, Theme.WithAlpha(Color.white, 0.9f), 0.32f);
            selectedRing.transform.SetParent(figure.transform, false);
            float s = Mathf.Max(figure.transform.lossyScale.y, 0.0001f);
            selectedRing.transform.localPosition = Vector3.up * 0.006f / s;
            selectedRing.transform.localScale /= s;
            FxAnimator.Pulse(selectedRing);
            Destroy(selectedRing, 1.6f);
        }

        /// <summary>
        /// One step for the arrow buttons, for the guide or whoever the child picked: up walks away from the
        /// phone, right walks to the right of the screen, so the buttons always match the view. A help for
        /// children who find dragging hard; holding an arrow keeps them walking.
        /// </summary>
        public void Step(Vector2 direction)
        {
            var who = Selected;
            if (who == null) return;
            var cam = CameraTransform;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var target = who.transform.position + (right * direction.x + forward * direction.y) * 0.18f;
            if (anchorRoot != null)
            {
                var offset = Vector3.ProjectOnPlane(target - anchorRoot.position, Vector3.up);
                if (offset.magnitude > MaxDistanceFromAnchor) return;   // stay near the placed spot
            }
            who.WalkTo(target, null);
        }

        /// <summary>Turns the guide, or whoever the child picked, on the spot (more than 0: to the right).</summary>
        public void Turn(float degrees)
        {
            var who = Selected;
            if (who != null) who.TurnBy(degrees);
        }

        /// <summary>Swings the camera around the guide (a swipe on the grass).</summary>
        public void Orbit(float degrees)
        {
            Stage.Orbit(degrees);
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
            // Following the finger at a person's pace (they step, not slide), and turning the way they go.
            var from = figure.transform.position;
            var to = Vector3.Lerp(from, point, 1f - Mathf.Exp(-9f * Time.deltaTime));
            to = from + Vector3.ClampMagnitude(to - from, 1.2f * Time.deltaTime);
            if ((to - from).sqrMagnitude > 0.0000004f) figure.TurnTowards(to + (to - from) * 10f);
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

        /// <summary>Removes Keza (a reset); she appears again on the playground when the next lesson opens.</summary>
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
    }
}
