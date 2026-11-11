using System.Collections;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// A person in the room: a visitor in the bubble lesson, a trusted adult,
    /// someone in a story. Movement is procedural, so no walking clips are
    /// needed: models with a skeleton step with their legs and swing their
    /// arms (WalkCycle), scanned models sway from foot to foot, and everyone
    /// turns smoothly and eases in and out of a walk.
    /// </summary>
    public class ARFigure : MonoBehaviour
    {
        [SerializeField] Renderer[] tintedParts = new Renderer[0];
        [SerializeField] TextMesh nameLabel;
        [SerializeField] float walkSpeed = 0.35f;   // metres per second, a calm walk at table scale

        Coroutine moving;

        public string Label { get; private set; }
        public bool IsMoving { get { return moving != null; } }

        /// <summary>Walking speed in metres per second (the chase game speeds visitors up).</summary>
        public float WalkSpeed
        {
            get { return walkSpeed; }
            set { walkSpeed = value; }
        }

        [Tooltip("Keep the model's own textured clothes instead of tinting them with the role colour (e.g. Mum and Dad).")]
        [SerializeField] bool keepOwnColours;

        /// <summary>For people built in code (the boy): keep their own clothes colours; Setup only names them.</summary>
        public bool KeepOwnColours { set { keepOwnColours = value; } }

        void Start()
        {
            // A soft shadow under the feet makes the person stand on the real floor.
            ARFx.AddShadow(transform, 0.3f);
            // A model with a skeleton swings its legs and arms as it moves (the boy built in code has his own).
            if (GetComponent<BoyMotion>() == null) WalkCycle.AddTo(gameObject);
            lastPosition = transform.position;
        }

        /// <summary>Shows or hides the name over the head (hidden while looking closely at the body).</summary>
        public void ShowName(bool show)
        {
            if (nameLabel != null) nameLabel.gameObject.SetActive(show);
        }

        float nameSize;   // the name tag's letter size as designed, before the Settings letter size

        /// <summary>Gives a figure built in code (the boy guide) its name tag.</summary>
        public void UseNameLabel(TextMesh label)
        {
            nameLabel = label;
            nameSize = 0f;
        }

        /// <summary>Changes the name over the person's head (Keza takes the child's chosen name).</summary>
        public void SetName(string label)
        {
            Label = label;
            name = label;
            if (nameLabel == null) return;
            nameLabel.text = label;
            // The name tag follows the letter size chosen in Settings.
            if (nameSize <= 0f) nameSize = nameLabel.characterSize;
            // Bigger in the 3D world, which is seen from further away than the camera picture.
            nameLabel.characterSize = nameSize * UI.TextSize.WorldScale * (SpeechScale > 0f ? 1.6f : 1f);
        }

        // Materials that keep their own look: skin (the skin tone sets it), eyes, hair, glasses.
        static readonly string[] KeepLook = { "skin", "realismbody", "head", "face", "hand", "eye", "iris", "sclera", "brow", "lash", "glass", "teeth", "hair" };

        /// <summary>
        /// Names the person and dresses them in the role's colour. Only the
        /// clothes take the colour: skin keeps the skin tone, and eyes, hair
        /// and glasses keep their own look. Mum and Dad keep their own
        /// textured clothes (keepOwnColours).
        /// </summary>
        public void Setup(string label, Color color, Material baseMaterial)
        {
            SetName(label);
            if (keepOwnColours) return;
            var parts = tintedParts.Length > 0 ? tintedParts : GetComponentsInChildren<Renderer>();
            foreach (var r in parts)
            {
                if (r == null || r.GetComponent<TextMesh>() != null) continue;
                var materials = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var m = materials[i];
                    string lower = m != null ? m.name.ToLowerInvariant() : "";
                    if (m != null && System.Array.Exists(KeepLook, k => lower.Contains(k))) continue;
                    var dressed = m != null ? new Material(m) : (baseMaterial != null ? new Material(baseMaterial) : null);
                    if (dressed == null) continue;
                    dressed.name = m != null ? m.name : "Clothes";   // keep the name: the skin tone looks for it
                    dressed.color = color;
                    materials[i] = dressed;
                    dressedMaterials.Add(dressed);
                    changed = true;
                }
                if (changed) r.materials = materials;
            }
        }

        readonly System.Collections.Generic.List<Material> dressedMaterials = new System.Collections.Generic.List<Material>();

        void OnDestroy()
        {
            // The clothes made for this person go with them (a lesson spawns many people over a session).
            foreach (var m in dressedMaterials) if (m != null) Destroy(m);
            dressedMaterials.Clear();
        }

        GameObject speech;
        static Material speechCard;   // one white card material for every speech bubble, made once

        /// <summary>
        /// How big speech bubbles are. 0 (the camera picture): they follow the person's own size, as always.
        /// More than 0 (the 3D world, seen from further away): this many times the normal bubble, for everyone alike.
        /// </summary>
        public static float SpeechScale = 0f;

        /// <summary>
        /// Shows a speech bubble above the figure (what a visitor asks Keza in
        /// Bubble Guard). Lines wrap at about 18 characters to stay readable at
        /// arm's length; an empty text removes the bubble.
        /// </summary>
        public void Say(string text, Material background)
        {
            var old = speech;
            if (speech != null) Destroy(speech);
            speech = null;
            if (string.IsNullOrEmpty(text)) return;

            // The top of the head; the old bubble is still there until the end of the frame, so skip it.
            float top = 0f;
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (r.GetComponent<TextMesh>() == null && (old == null || !r.transform.IsChildOf(old.transform))) top = Mathf.Max(top, r.bounds.max.y);
            if (top <= 0f) top = transform.position.y + 0.7f;

            speech = new GameObject("Speech");
            speech.transform.SetParent(transform, true);
            // Bigger letters in Settings make the bubble bigger too.
            float size = UI.TextSize.WorldScale * (SpeechScale > 0f ? SpeechScale / Mathf.Max(transform.lossyScale.y, 0.0001f) : 1f);
            speech.transform.localScale = Vector3.one * size;
            speech.AddComponent<Billboard>();

            string wrapped = Wrap(text, 18);
            int lines = wrapped.Split('\n').Length;
            // Above the name label (which sits about 0.08 m over the head), not on top of it; a bigger bubble sits higher.
            float lift = SpeechScale > 0f ? 0.13f + (0.05f + 0.045f * lines) * speech.transform.lossyScale.y * 0.5f : 0.25f * UI.TextSize.WorldScale;
            speech.transform.position = new Vector3(transform.position.x, top + lift, transform.position.z);
            var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(card.GetComponent<Collider>());
            card.transform.SetParent(speech.transform, false);
            card.transform.localScale = new Vector3(0.36f, 0.05f + 0.045f * lines, 1f);
            var cardRenderer = card.GetComponent<Renderer>();
            if (speechCard == null)
            {
                speechCard = background != null ? new Material(background) : new Material(cardRenderer.sharedMaterial);
                speechCard.color = Color.white;
            }
            cardRenderer.sharedMaterial = speechCard;

            var label = new GameObject("Text");
            label.transform.SetParent(speech.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.005f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.font = ARFx.Font;
            label.GetComponent<MeshRenderer>().sharedMaterial = ARFx.Font.material;
            mesh.text = wrapped;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = 0.0065f;
            mesh.color = new Color(0.12f, 0.16f, 0.27f);   // Theme.Ink
        }

        static string Wrap(string text, int width)
        {
            return ARFx.Wrap(text, width);
        }

        Coroutine sayTimer;

        /// <summary>A speech bubble that goes away by itself after a few seconds.</summary>
        public void SayFor(string text, Material background, float seconds)
        {
            Say(text, background);
            if (sayTimer != null) StopCoroutine(sayTimer);
            sayTimer = StartCoroutine(ClearSpeechLater(seconds));
        }

        IEnumerator ClearSpeechLater(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            sayTimer = null;
            Say(null, null);
        }

        /// <summary>Turns to face a point at once.</summary>
        public void FaceTowards(Vector3 worldPoint)
        {
            var flat = Vector3.ProjectOnPlane(worldPoint - transform.position, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
            turning = false;
        }

        const float TurnSpeed = 320f;   // degrees a second: a quick but natural turn on the spot
        bool turning;
        Quaternion turnGoal;

        /// <summary>Turns smoothly to face a point, as a person would, instead of snapping round.</summary>
        public void TurnTowards(Vector3 worldPoint)
        {
            var flat = Vector3.ProjectOnPlane(worldPoint - transform.position, Vector3.up);
            if (flat.sqrMagnitude < 0.000001f) return;
            turnGoal = Quaternion.LookRotation(flat);
            turning = true;
        }

        /// <summary>Turns smoothly by this many degrees (more than 0 turns right, as seen from above); turns add up.</summary>
        public void TurnBy(float degrees)
        {
            turnGoal = Quaternion.Euler(0f, degrees, 0f) * (turning ? turnGoal : transform.rotation);
            turning = true;
        }

        /// <summary>Turns smoothly to face this way (degrees around the up axis, as Unity counts them).</summary>
        public void TurnTo(float yaw)
        {
            turnGoal = Quaternion.Euler(0f, yaw, 0f);
            turning = true;
        }

        Vector3 lastPosition;
        float stride, swing;

        void Update()
        {
            if (!turning) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, turnGoal, TurnSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, turnGoal) < 0.5f) turning = false;
        }

        /// <summary>
        /// A person who cannot move their legs (a scanned or still model) sways from foot to foot whenever they
        /// move, however they are moved (walking, dragged, walked by a lesson), and stands still when they stop.
        /// </summary>
        void LateUpdate()
        {
            var moved = Vector3.ProjectOnPlane(transform.position - lastPosition, Vector3.up).magnitude;
            lastPosition = transform.position;
            var body = StiffBody();
            if (body == null) return;
            float speed = moved > 0.5f ? 0f : moved / Mathf.Max(Time.deltaTime, 0.0001f);
            swing = Mathf.MoveTowards(swing, Mathf.Clamp01(speed / 0.2f), Time.deltaTime * 5f);
            if (swing <= 0f) { StandStill(); return; }
            stride += moved / 0.09f;   // one sway to each side every 0.18 m or so
            float s = Mathf.Max(transform.lossyScale.y, 0.0001f);
            body.localRotation = stiffRest * Quaternion.Euler(3f * swing, 0f, Mathf.Sin(stride) * 4f * swing);
            body.localPosition = stiffRestPosition + Vector3.up * Mathf.Abs(Mathf.Sin(stride)) * 0.012f * swing / s;
        }

        float pace;   // 0 standing to 1 full walking speed

        public void WalkTo(Vector3 target, System.Action arrived)
        {
            if (moving != null) StopCoroutine(moving);
            else pace = 0f;
            moving = StartCoroutine(Walk(target, arrived));
        }

        public void Stop()
        {
            if (moving != null) StopCoroutine(moving);
            moving = null;
            StandStill();
        }

        /// <summary>A small hop, used for waving, saying "No!" and celebrating.</summary>
        public void Bounce()
        {
            var motion = GetComponent<BoyMotion>();
            if (motion != null) motion.Wave();   // the boy built in code waves as he hops
            // Model characters with a greeting clip wave; the simple figures hop.
            var animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null && HasTrigger(animator, GreetTrigger))
            {
                animator.SetTrigger(GreetTrigger);
                return;
            }
            StartCoroutine(Hop());
        }

        /// <summary>Animator trigger that plays the greeting wave (set up by SafeTouch > Build Character Prefabs).</summary>
        public const string GreetTrigger = "Greet";

        static bool HasTrigger(Animator animator, string trigger)
        {
            foreach (var p in animator.parameters)
                if (p.name == trigger && p.type == AnimatorControllerParameterType.Trigger) return true;
            return false;
        }

        IEnumerator Walk(Vector3 target, System.Action arrived)
        {
            // Turning towards where they go as they set off, and easing in and out of the walk.
            TurnTowards(target);
            float start = Vector3.Distance(transform.position, target);
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                float left = Vector3.Distance(transform.position, target);
                pace = Mathf.MoveTowards(pace, 1f, Time.deltaTime * 4f);   // kept between walks, so held arrows walk on smoothly
                float slow = start > 0.15f ? Mathf.Clamp(left / 0.08f, 0.35f, 1f) : 1f;   // slow down for the last steps
                transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * pace * slow * Time.deltaTime);
                if (left > 0.05f) TurnTowards(target);
                yield return null;
            }
            moving = null;
            if (arrived != null) arrived();
        }

        Transform stiffBody;
        Quaternion stiffRest;
        Vector3 stiffRestPosition;
        bool stiffChecked;

        /// <summary>Moves the person this much this frame (Bubble Guard walks its visitors itself); their steps follow.</summary>
        public void MoveBy(Vector3 delta)
        {
            transform.position += delta;
        }

        /// <summary>The body of a model with no animation and no moving limbs (the scanned people), or null.</summary>
        Transform StiffBody()
        {
            if (stiffChecked) return stiffBody;
            stiffChecked = true;
            var animator = GetComponentInChildren<Animator>();
            if (GetComponent<BoyMotion>() != null || (animator != null && animator.runtimeAnimatorController != null)) return null;
            stiffBody = transform.Find("Model");
            if (stiffBody != null)
            {
                stiffRest = stiffBody.localRotation;
                stiffRestPosition = stiffBody.localPosition;
            }
            return stiffBody;
        }

        void StandStill()
        {
            if (stiffBody == null) return;
            stiffBody.localRotation = stiffRest;
            stiffBody.localPosition = stiffRestPosition;
        }

        IEnumerator Hop()
        {
            var start = transform.localPosition;
            for (float t = 0; t < 1f; t += Time.deltaTime * 3f)
            {
                transform.localPosition = start + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.08f;
                yield return null;
            }
            transform.localPosition = start;
        }
    }
}
