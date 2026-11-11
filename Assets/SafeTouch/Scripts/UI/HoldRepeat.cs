using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SafeTouch.UI
{
    /// <summary>
    /// A button that keeps acting while the finger stays on it (the arrows that walk a person): once at the
    /// touch, then every interval until the finger lifts or slides off. Small children hold buttons down.
    /// </summary>
    public class HoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action action;
        public float interval = 0.2f;

        bool held;
        float next;

        public void OnPointerDown(PointerEventData eventData)
        {
            held = true;
            next = Time.unscaledTime + interval * 1.5f;   // a short pause first, so one tap is one step
            if (action != null) action();
        }

        public void OnPointerUp(PointerEventData eventData) { held = false; }

        public void OnPointerExit(PointerEventData eventData) { held = false; }

        void OnDisable() { held = false; }

        void Update()
        {
            if (!held || Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;
            if (action != null) action();
        }
    }
}
