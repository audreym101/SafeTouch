using UnityEngine;
using UnityEngine.EventSystems;

namespace SafeTouch.UI
{
    /// <summary>
    /// A button that squashes a little under the finger and springs back with a small wobble when let go,
    /// so every tap feels alive. Scale only: the layout and the touch area do not move.
    /// </summary>
    public class Squish : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Pressed = 0.93f;
        float target = 1f, scale = 1f, speed;

        public void OnPointerDown(PointerEventData e) { target = Pressed; }
        public void OnPointerUp(PointerEventData e) { target = 1f; }
        public void OnPointerExit(PointerEventData e) { target = 1f; }

        void OnDisable()
        {
            target = scale = 1f;
            speed = 0f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (Mathf.Abs(scale - target) < 0.0005f && Mathf.Abs(speed) < 0.0005f) return;
            // A spring: quick to press, a bouncy release.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            speed += (target - scale) * 900f * dt;
            speed *= Mathf.Exp(-22f * dt);
            scale += speed * dt;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>
    /// Gentle life for the cartoons: a character hops (or sways) in a loop. Each one starts at a different
    /// moment so a group never moves in lock-step. Moves the picture only, never the layout.
    /// </summary>
    public class Bob : MonoBehaviour
    {
        public float height = 18f;    // canvas units
        public float period = 0.9f;   // seconds per hop
        public float sway = 3f;       // degrees
        public float phase;

        RectTransform rt;
        Transform inner;

        void Awake()
        {
            rt = (RectTransform)transform;
            if (phase == 0f) phase = Random.value;
        }

        void Start()
        {
            // The layout owns this object's position, so the drawing is moved inside a child that holds the parts.
            if (inner != null) return;
            var holder = new GameObject("Moves", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(rt, false);
            holder.anchorMin = Vector2.zero;
            holder.anchorMax = Vector2.one;
            holder.offsetMin = holder.offsetMax = Vector2.zero;
            holder.pivot = new Vector2(0.5f, 0f);
            var parts = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in rt) if (child != holder) parts.Add(child);
            foreach (var part in parts) part.SetParent(holder, false);   // in order, so the drawing order is kept
            inner = holder;
        }

        void Update()
        {
            if (inner == null) return;
            float t = (Time.unscaledTime / period + phase) % 1f;
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI));   // up and down, a soft landing
            inner.localPosition = new Vector3(0f, hop * height, 0f);
            inner.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * sway);
        }
    }
}
