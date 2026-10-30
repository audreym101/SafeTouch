using System.Collections.Generic;
using SafeTouch.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace SafeTouch.UI
{
    /// <summary>
    /// Two round buttons on the right of the 3D or camera view: + makes Keza bigger, - makes her smaller.
    /// Pinching with two fingers (the mouse wheel in the Editor) does the same. On the playground the camera
    /// moves closer; in the camera picture Keza steps closer to the phone (ARManager.ChangeSize).
    /// They show while Keza is on screen.
    /// </summary>
    public class SizeButtons : MonoBehaviour
    {
        const float Step = 1.2f;

        GameManager game;
        UIManager ui;
        GameObject panel;
        Text caption;
        bool showing;
        float lastPinch, outOfViewSince = -1f;
        Button bringBack;

        public static SizeButtons Build(RectTransform parent, GameManager game, UIManager ui)
        {
            var root = UIFactory.Rect(parent, "Size buttons");
            root.anchorMin = root.anchorMax = new Vector2(1f, 0.56f);
            root.pivot = new Vector2(1f, 0.5f);
            root.sizeDelta = new Vector2(150f, 420f);
            root.anchoredPosition = new Vector2(-24f, 0f);
            UIFactory.Column(root, 16f, 0);

            var buttons = root.gameObject.AddComponent<SizeButtons>();
            buttons.game = game;
            buttons.ui = ui;
            buttons.panel = root.gameObject;
            Round(root, "+", Theme.Primary, () => buttons.Zoom(Step));
            buttons.caption = UIFactory.Label(root, "", Theme.Small, Theme.Ink, TextAnchor.MiddleCenter, true);
            var shadow = buttons.caption.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(1f, 1f, 1f, 0.8f);
            Round(root, "-", Theme.Primary, () => buttons.Zoom(1f / Step));
            foreach (Transform child in root) child.gameObject.SetActive(false);   // hidden until Keza is on screen

            // Camera mode: when the child points the phone away and Keza is out of the picture, one tap brings her back.
            buttons.bringBack = UIFactory.Button(parent, "", Theme.Sun, Theme.Ink, () => game.AR.BringIntoView());
            var brt = (RectTransform)buttons.bringBack.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.8f);
            brt.sizeDelta = new Vector2(660f, 150f);
            buttons.bringBack.gameObject.SetActive(false);
            return buttons;
        }

        static void Round(RectTransform parent, string symbol, Color colour, System.Action click)
        {
            var b = UIFactory.Button(parent, symbol, colour, Color.white, click);
            b.image.sprite = UIFactory.Circle;
            b.image.type = Image.Type.Simple;
            var le = b.GetComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 140f;
            le.minWidth = le.preferredWidth = 140f;
            le.flexibleWidth = 0f;
            var label = b.GetComponentInChildren<Text>();
            label.fontSize = 96;
            var rt = label.rectTransform;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void Zoom(float times)
        {
            if (game.AR != null) game.AR.ChangeSize(times);
        }

        void LateUpdate()
        {
            showing = game.AR != null && game.AR.HasCharacter && ui.CameraShowing;
            if (panel.transform.childCount > 0 && panel.transform.GetChild(0).gameObject.activeSelf != showing)
                foreach (Transform child in panel.transform) child.gameObject.SetActive(showing);
            // Shown after Keza has been out of the picture for a moment, so a quick look around does not flash it.
            bool away = showing && game.AR.CanBringBack && !game.AR.CharacterInView;
            if (!away) outOfViewSince = -1f;
            else if (outOfViewSince < 0f) outOfViewSince = Time.unscaledTime;
            bool offer = away && Time.unscaledTime - outOfViewSince > 0.7f;
            if (bringBack.gameObject.activeSelf != offer) bringBack.gameObject.SetActive(offer);
            if (offer) UIFactory.SetButtonLabel(bringBack, string.Format(Localization.Language == "rw" ? "Garura {0} hano" : "Bring {0} back", string.IsNullOrEmpty(game.AR.Character.Label) ? "Keza" : game.AR.Character.Label));
            if (!showing) { lastPinch = 0f; return; }
            caption.text = Localization.Language == "rw" ? "Ingano" : "Size";
            Pinch();
        }

        /// <summary>Two fingers apart or together, anywhere but on a button: bigger or smaller.</summary>
        void Pinch()
        {
            if (game.AR.PinchTaken) { lastPinch = 0f; return; }   // a lesson is using the pinch (turning Keza in "look closer")
            var touches = ETouch.activeTouches;
            if (touches.Count >= 2)
            {
                var a = touches[0].screenPosition;
                var b = touches[1].screenPosition;
                if (OverUI(a) || OverUI(b)) return;
                float distance = Mathf.Max(Vector2.Distance(a, b), 1f);
                if (lastPinch > 0f) Zoom(distance / lastPinch);
                lastPinch = distance;
                return;
            }
            lastPinch = 0f;
            var mouse = Mouse.current;
            if (mouse == null || OverUI(mouse.position.ReadValue())) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll > 0f ? 1.1f : 1f / 1.1f);
        }

        static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        static bool OverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            UiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, UiHits);
            foreach (var hit in UiHits) if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
            return false;
        }
    }
}
