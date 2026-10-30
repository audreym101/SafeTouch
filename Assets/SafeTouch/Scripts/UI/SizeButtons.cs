using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI
{
    /// <summary>
    /// Two round buttons on the right of the 3D view: + makes Keza bigger, - makes her smaller (pinching does
    /// the same). They show only on the 3D playground, while Keza is on screen.
    /// </summary>
    public class SizeButtons : MonoBehaviour
    {
        const float Step = 1.2f;

        GameManager game;
        UIManager ui;
        GameObject panel;
        Text caption;

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

        void Zoom(float times)
        {
            if (game.AR.Stage != null) game.AR.Stage.Zoom(times);
        }

        void LateUpdate()
        {
            var stage = game.AR != null ? game.AR.Stage : null;
            bool show = stage != null && game.AR.HasCharacter && ui.CameraShowing;
            if (panel.transform.childCount > 0 && panel.transform.GetChild(0).gameObject.activeSelf != show)
                foreach (Transform child in panel.transform) child.gameObject.SetActive(show);
            if (show) caption.text = Localization.Language == "rw" ? "Ingano" : "Size";
        }
    }
}
