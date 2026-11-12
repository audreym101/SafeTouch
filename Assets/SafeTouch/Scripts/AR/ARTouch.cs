using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SafeTouch.AR
{
    /// <summary>What a tap in the camera view touched: people, letters or body dots, and the floor point under the finger.</summary>
    public struct ARTap
    {
        public Vector2 screen;
        public RaycastHit[] hits;     // nearest first
        public bool hasFloor;
        public Vector3 floor;         // where the finger meets the floor Keza stands on

        /// <summary>The nearest touched object that has (or is inside) a T, or null.</summary>
        public T Find<T>() where T : Component
        {
            if (hits == null) return null;
            foreach (var h in hits)
            {
                var c = h.collider.GetComponentInParent<T>();
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>True when the tap touched obj or one of its children.</summary>
        public bool Touched(GameObject obj)
        {
            if (hits == null || obj == null) return false;
            foreach (var h in hits) if (h.transform == obj.transform || h.transform.IsChildOf(obj.transform)) return true;
            return false;
        }
    }

    /// <summary>
    /// Direct touch in the 3D view, so children act on the world itself rather
    /// than on buttons:
    ///   tap          - touch a person, a floor zone, a letter or a body dot (Tapped);
    ///                  touching a person also picks them for the move pad
    ///   drag         - walk a person across the floor with a finger (they step and turn as they go)
    ///   swipe        - on the grass of the 3D world, swing the camera around the guide
    ///   twist        - with two fingers, turn the person last touched (or the guide)
    ///   pinch, twist - in "look closer" mode, make Keza bigger and turn her;
    ///                  one-finger drag in that mode turns her too.
    /// Touches that start on the app's buttons or cards are left to the UI.
    /// In the Editor the mouse stands in: click, drag, scroll wheel to
    /// resize and right-drag to turn.
    /// </summary>
    public class ARTouch : MonoBehaviour
    {
        const float TapMaxSeconds = 0.4f;
        const float TapMaxPixels = 22f;

        ARManager ar;
        static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        public event Action<ARTap> Tapped;
        /// <summary>A person is being dragged (every frame while it moves), and when the finger lifts.</summary>
        public event Action<ARFigure> DragMoved, DragEnded;

        /// <summary>Lessons switch dragging off when people must stay where they are.</summary>
        public bool DragEnabled { get; set; } = true;

        // One-finger gesture state
        bool pressing, blocked, moved, dragging;
        Vector2 pressStart, lastPosition;
        float pressTime;
        ARFigure dragFigure;
        Vector3 grabOffset;

        // "Look closer": the object pinch and twist act on
        Transform target;
        float minScale, maxScale, baseScale;
        bool twoFinger, twisting;
        float pinchStartDistance, pinchStartScale, twistStartAngle, twistStartYaw;
        ARFigure twistFigure;
        const float TwistThreshold = 12f;   // degrees the fingers turn before a pinch also turns the person

        public bool Manipulating { get { return target != null; } }

        public void Init(ARManager manager)
        {
            ar = manager;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        /// <summary>Lets pinch and twist resize (between min and max times its size now) and turn an object.</summary>
        public void BeginManipulate(Transform obj, float minTimes, float maxTimes)
        {
            target = obj;
            baseScale = obj.localScale.x;
            minScale = baseScale * minTimes;
            maxScale = baseScale * maxTimes;
        }

        public void EndManipulate()
        {
            target = null;
            twoFinger = false;
        }

        void Update()
        {
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                TwoFingers(touches[0].screenPosition, touches[1].screenPosition);
                return;
            }
            if (twoFinger)
            {
                // Lifting one finger of a pinch must not turn into a tap or a drag.
                twoFinger = false;
                if (touches.Count == 1) { blocked = true; pressing = true; }
            }
            if (touches.Count == 1)
            {
                var t = touches[0];
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began) Press(t.screenPosition);
                else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled) Release(t.screenPosition);
                else Hold(t.screenPosition);
                return;
            }
            if (touches.Count == 0 && pressing && Touchscreen.current != null && Mouse.current == null) Release(lastPosition);
            MouseInput();
        }

        /// <summary>Mouse stand-in for the Editor and Chromebooks.</summary>
        void MouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame) Press(position);
            else if (mouse.leftButton.wasReleasedThisFrame) Release(position);
            else if (mouse.leftButton.isPressed) Hold(position);

            if (OverUI(position)) return;
            if (target == null)
            {
                // Right-drag turns the person last touched, like a two-finger twist.
                var who = ar != null ? ar.Selected : null;
                if (mouse.rightButton.isPressed && who != null && Mathf.Abs(mouse.delta.ReadValue().x) > 0.01f) who.TurnBy(-mouse.delta.ReadValue().x * 0.6f);
                return;
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) SetScale(target.localScale.x * (1f + Mathf.Sign(scroll) * 0.08f));
            if (mouse.rightButton.isPressed) target.Rotate(0f, -mouse.delta.ReadValue().x * 0.4f, 0f, Space.World);
        }

        void Press(Vector2 position)
        {
            pressing = true;
            moved = false;
            dragging = false;
            dragFigure = null;
            pressStart = lastPosition = position;
            pressTime = Time.unscaledTime;
            blocked = OverUI(position);
            if (blocked || target != null || !DragEnabled || ar == null) return;

            // Pressing on a person who may move starts a possible drag.
            var tap = Cast(position);
            var figure = tap.Find<ARFigure>();
            if (figure != null && ar.CanDrag(figure) && tap.hasFloor)
            {
                dragFigure = figure;
                grabOffset = figure.transform.position - tap.floor;
                grabOffset.y = 0f;
            }
        }

        void Hold(Vector2 position)
        {
            if (!pressing || blocked) { lastPosition = position; return; }
            if (!moved && (position - pressStart).magnitude > TapMaxPixels) moved = true;

            if (target != null && moved)
            {
                // Look-closer mode: a sideways drag turns the object.
                target.Rotate(0f, -(position.x - lastPosition.x) * 0.35f, 0f, Space.World);
            }
            else if (dragFigure != null && moved)
            {
                Vector3 floor;
                if (FloorPoint(position, out floor))
                {
                    if (!dragging) { dragging = true; dragFigure.Stop(); ar.Select(dragFigure); }
                    ar.DragFigure(dragFigure, floor + grabOffset);
                    if (DragMoved != null) DragMoved(dragFigure);
                }
            }
            else if (moved && ar != null)
            {
                // A swipe on the grass swings the camera around the guide: half the screen's width is a quarter turn.
                ar.Orbit((position.x - lastPosition.x) / Mathf.Max(Screen.width, 1) * 180f);
            }
            lastPosition = position;
        }

        void Release(Vector2 position)
        {
            if (!pressing) return;
            pressing = false;
            if (blocked) { blocked = false; return; }
            if (dragging)
            {
                dragging = false;
                if (DragEnded != null) DragEnded(dragFigure);
                dragFigure = null;
                return;
            }
            bool quick = Time.unscaledTime - pressTime <= TapMaxSeconds;
            if (!moved && quick)
            {
                var tap = Cast(position);
                // Touching a person picks them for the move pad and the two-finger twist.
                var figure = tap.Find<ARFigure>();
                if (figure != null && ar != null && ar.CanDrag(figure)) ar.Select(figure);
                if (Tapped != null) Tapped(tap);
            }
            dragFigure = null;
        }

        void TwoFingers(Vector2 a, Vector2 b)
        {
            if (pressing && dragging && DragEnded != null) DragEnded(dragFigure);
            pressing = false;
            dragging = false;
            dragFigure = null;
            float distance = Vector2.Distance(a, b);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            if (target == null)
            {
                TwistPerson(angle);
                return;
            }
            if (!twoFinger)
            {
                twoFinger = true;
                pinchStartDistance = Mathf.Max(distance, 1f);
                pinchStartScale = target.localScale.x;
                twistStartAngle = angle;
                twistStartYaw = target.eulerAngles.y;
                return;
            }
            SetScale(pinchStartScale * distance / pinchStartDistance);
            var e = target.eulerAngles;
            target.rotation = Quaternion.Euler(e.x, twistStartYaw - Mathf.DeltaAngle(twistStartAngle, angle), e.z);
        }

        /// <summary>
        /// Two fingers turning like a dial turn the person last touched (or the guide). A pinch without a turn
        /// only zooms (SizeButtons), so the person starts turning once the fingers have turned a little.
        /// </summary>
        void TwistPerson(float angle)
        {
            if (!twoFinger)
            {
                twoFinger = true;
                twisting = false;
                twistStartAngle = angle;
                twistFigure = ar != null ? ar.Selected : null;
                if (twistFigure != null) twistStartYaw = twistFigure.transform.eulerAngles.y;
                return;
            }
            if (twistFigure == null) return;
            float turned = Mathf.DeltaAngle(twistStartAngle, angle);
            if (!twisting && Mathf.Abs(turned) < TwistThreshold) return;
            if (!twisting)
            {
                twisting = true;
                twistStartAngle = angle;   // from here on, the person turns as much as the fingers
                twistStartYaw = twistFigure.transform.eulerAngles.y;
                return;
            }
            twistFigure.TurnTo(twistStartYaw - turned);
        }

        void SetScale(float scale)
        {
            target.localScale = Vector3.one * Mathf.Clamp(scale, minScale, maxScale);
        }

        Camera Cam
        {
            get
            {
                var t = ar != null ? ar.CameraTransform : null;
                var cam = t != null ? t.GetComponent<Camera>() : null;
                return cam != null ? cam : Camera.main;
            }
        }

        ARTap Cast(Vector2 position)
        {
            var tap = new ARTap { screen = position };
            var cam = Cam;
            if (cam == null) return tap;
            var ray = cam.ScreenPointToRay(position);
            tap.hits = Physics.RaycastAll(ray, 20f);
            Array.Sort(tap.hits, (x, y) => x.distance.CompareTo(y.distance));
            tap.hasFloor = FloorPoint(position, out tap.floor);
            return tap;
        }

        /// <summary>Where a screen point meets the floor Keza stands on.</summary>
        bool FloorPoint(Vector2 position, out Vector3 point)
        {
            point = Vector3.zero;
            var cam = Cam;
            if (cam == null || ar == null || ar.Character == null) return false;
            var plane = new Plane(Vector3.up, ar.Character.transform.position);
            var ray = cam.ScreenPointToRay(position);
            float enter;
            if (!plane.Raycast(ray, out enter) || enter > 15f) return false;
            point = ray.GetPoint(enter);
            return true;
        }

        /// <summary>True when the point is on one of the app's buttons or cards, which keep the touch.</summary>
        static bool OverUI(Vector2 position)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            UiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = position }, UiHits);
            return UiHits.Count > 0;
        }
    }
}
