using System.Collections;
using UnityEngine;

namespace SafeTouch.AR
{
    /// <summary>
    /// A friendly low-poly person in AR: a visitor in the bubble lesson or a
    /// trusted adult. Movement is procedural so the prototype needs no
    /// animation assets yet (Mixamo clips replace it in week 6).
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

        public void Setup(string label, Color color, Material baseMaterial)
        {
            Label = label;
            name = label;
            if (nameLabel != null) nameLabel.text = label;
            if (keepOwnColours) return;
            var parts = tintedParts.Length > 0 ? tintedParts : GetComponentsInChildren<Renderer>();
            foreach (var r in parts)
            {
                if (r == null || r.GetComponent<TextMesh>() != null) continue;
                r.material = baseMaterial != null ? new Material(baseMaterial) : r.material;
                r.material.color = color;
            }
            if (nameLabel != null) nameLabel.text = label;
        }

        GameObject speech;

        /// <summary>
        /// Shows a speech bubble above the figure (what a visitor asks Keza in
        /// Bubble Guard). Lines wrap at about 18 characters to stay readable at
        /// arm's length; an empty text removes the bubble.
        /// </summary>
        public void Say(string text, Material background)
        {
            if (speech != null) Destroy(speech);
            if (string.IsNullOrEmpty(text)) return;

            float top = 0f;
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (r.GetComponent<TextMesh>() == null) top = Mathf.Max(top, r.bounds.max.y);
            if (top <= 0f) top = transform.position.y + 0.7f;

            speech = new GameObject("Speech");
            speech.transform.SetParent(transform, true);
            speech.transform.position = new Vector3(transform.position.x, top + 0.16f, transform.position.z);
            speech.AddComponent<Billboard>();

            string wrapped = Wrap(text, 18);
            int lines = wrapped.Split('\n').Length;
            var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(card.GetComponent<Collider>());
            card.transform.SetParent(speech.transform, false);
            card.transform.localScale = new Vector3(0.36f, 0.05f + 0.045f * lines, 1f);
            var cardRenderer = card.GetComponent<Renderer>();
            if (background != null) cardRenderer.material = new Material(background);
            cardRenderer.material.color = Color.white;

            var label = new GameObject("Text");
            label.transform.SetParent(speech.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.005f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = wrapped;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = 0.0065f;
            mesh.color = new Color(0.12f, 0.16f, 0.27f);   // Theme.Ink
        }

        static string Wrap(string text, int width)
        {
            var words = text.Split(' ');
            var result = new System.Text.StringBuilder();
            int lineLength = 0;
            foreach (var word in words)
            {
                if (lineLength > 0 && lineLength + 1 + word.Length > width)
                {
                    result.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    result.Append(' ');
                    lineLength++;
                }
                result.Append(word);
                lineLength += word.Length;
            }
            return result.ToString();
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            var flat = Vector3.ProjectOnPlane(worldPoint - transform.position, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
        }

        public void WalkTo(Vector3 target, System.Action arrived)
        {
            if (moving != null) StopCoroutine(moving);
            moving = StartCoroutine(Walk(target, arrived));
        }

        public void Stop()
        {
            if (moving != null) StopCoroutine(moving);
            moving = null;
        }

        /// <summary>A small hop, used for waving, saying "No!" and celebrating.</summary>
        public void Bounce()
        {
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
            FaceTowards(target);
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
                yield return null;
            }
            moving = null;
            if (arrived != null) arrived();
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
