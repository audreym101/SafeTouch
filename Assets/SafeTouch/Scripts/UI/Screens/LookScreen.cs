using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// "Keza's look": the child picks Keza's skin tone. When Keza is already
    /// in the room the camera shows through and the change plays on her in
    /// AR (a colour blend and a sparkle); otherwise a drawn face previews it.
    /// Seeing a guide who looks like them helps children feel the lessons
    /// are about them.
    /// </summary>
    public class LookScreen : UIScreen
    {
        RectTransform preview;
        Image face, body;
        Text toneName, hint;

        public override string Name { get { return "look"; } }
        public override bool ShowsCamera { get { return true; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu("home"));
            TitleText.color = Theme.Ink;
            BuildPreview(col);
            UIFactory.Spacer(col, 1f);

            var card = UIFactory.Panel(col, "Look card", Theme.WithAlpha(Theme.Card, 0.96f), true);
            var cardRt = (RectTransform)card.transform;
            UIFactory.Column(cardRt, 24f, 44);
            card.gameObject.AddComponent<LayoutElement>();
            toneName = UIFactory.Label(cardRt, "", Theme.Title, Theme.Ink, TextAnchor.MiddleCenter, true);
            BuildSwatches(cardRt);
            hint = UIFactory.Label(cardRt, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            done = UIFactory.Button(cardRt, "", Theme.Primary, Color.white, () => ui.ShowMenu("home"));
        }

        Button done;
        readonly Image[] swatches = new Image[SkinTones.All.Length];

        /// <summary>One big round button per tone; the chosen one gets a dark ring.</summary>
        void BuildSwatches(RectTransform parent)
        {
            var row = UIFactory.Row(parent, "Swatches", 16f, 140f);
            for (int i = 0; i < SkinTones.All.Length; i++)
            {
                var tone = SkinTones.All[i];
                var dot = UIFactory.Dot(row.transform, "Swatch " + tone.id, tone.Color, 120f);
                var button = dot.gameObject.AddComponent<Button>();
                button.targetGraphic = dot;
                button.onClick.AddListener(() => Pick(tone));
                var ring = dot.gameObject.AddComponent<Outline>();
                ring.effectColor = Theme.Ink;
                ring.effectDistance = new Vector2(7f, -7f);
                ring.enabled = false;
                swatches[i] = dot;
            }
        }

        void Pick(SkinTone tone)
        {
            game.Audio.PlaySFX("tap");
            game.SetSkinTone(tone.id);   // saves, and plays the change on Keza if she is in the room
            Show(tone);
            if (game.AR.Character != null) game.AR.Character.Bounce();
        }

        /// <summary>A simple drawn Keza: round face with eyes and a smile, and her orange top.</summary>
        void BuildPreview(RectTransform col)
        {
            preview = UIFactory.Rect(col, "Preview");
            var le = preview.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 620f;

            body = Shape(preview, "Top", Theme.Hex("#F28C38"), new Vector2(0f, -170f), new Vector2(380f, 280f), false);
            face = Shape(preview, "Face", Theme.Hex("#8D5524"), new Vector2(0f, 90f), new Vector2(340f, 340f), true);
            Shape(face.transform, "Eye L", Theme.Ink, new Vector2(-62f, 30f), new Vector2(44f, 44f), true);
            Shape(face.transform, "Eye R", Theme.Ink, new Vector2(62f, 30f), new Vector2(44f, 44f), true);
            Shape(face.transform, "Smile", Theme.Hex("#5A2A1E"), new Vector2(0f, -70f), new Vector2(120f, 26f), false);
        }

        static Image Shape(Transform parent, string shapeName, Color color, Vector2 position, Vector2 size, bool round)
        {
            var img = UIFactory.Panel(parent, shapeName, color, !round);
            if (round) img.sprite = UIFactory.Circle;
            img.raycastTarget = false;
            var rt = (RectTransform)img.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return img;
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("look.title", "Keza's look");
            UIFactory.SetButtonLabel(done, Localization.T("look.done", "Done"));
            bool inRoom = game.AR.HasCharacter;
            preview.gameObject.SetActive(!inRoom);
            hint.text = inRoom
                ? Localization.T("look.hint.room", "Keza can look like you, or like a friend. Watch her change in your room!")
                : Localization.T("look.hint", "Keza can look like you, or like a friend. Pick any colour.");
            Show(SkinTones.Get(game.Progress.GetData().skinTone));
        }

        void Show(SkinTone tone)
        {
            face.color = tone.Color;
            toneName.text = Localization.T(tone.nameKey, tone.english);
            for (int i = 0; i < swatches.Length; i++)
                swatches[i].GetComponent<Outline>().enabled = SkinTones.All[i].id == tone.id;
        }
    }
}
