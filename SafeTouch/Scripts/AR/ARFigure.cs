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

        public void Setup(string label, Color color, Material baseMaterial)
        {
            Label = label;
            name = label;
            var parts = tintedParts.Length > 0 ? tintedParts : GetComponentsInChildren<Renderer>();
            foreach (var r in parts)
            {
                if (r == null || r.GetComponent<TextMesh>() != null) continue;
                r.material = baseMaterial != null ? new Material(baseMaterial) : r.material;
                r.material.color = color;
            }
            if (nameLabel != null) nameLabel.text = label;
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
            StartCoroutine(Hop());
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
