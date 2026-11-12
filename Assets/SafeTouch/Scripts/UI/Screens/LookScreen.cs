using SafeTouch.Core;
using SafeTouch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// "Keza's look": the child picks a girl (Keza) or a boy (Ganza) helper, and their skin tone. When Keza is already
    /// in her 3D world it shows through and the change plays on her (a colour blend and a sparkle); otherwise a
    /// drawn face previews it.
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
        public override bool CameraVisibleNow { get { return game.AR.HasCharacter; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", () => ui.ShowMenu(ui.CameFrom("menu", "home")));
            TitleText.color = Theme.Ink;
            BuildPreview(col);
            UIFactory.Spacer(col, 1f);

            var card = UIFactory.Panel(col, "Look card", Theme.WithAlpha(Theme.Card, 0.96f), true);
            var cardRt = (RectTransform)card.transform;
            UIFactory.Column(cardRt, 24f, 44);
            card.gameObject.AddComponent<LayoutElement>();
            // A girl (Keza) or a boy (Ganza) helper; the other one appears in the same spot.
            var helpers = UIFactory.Row(cardRt, "Helpers", Theme.Gap, Theme.TouchTarget);
            girl = UIFactory.Button(helpers.transform, "", Theme.Card, Theme.Ink, () => { game.SetGuide(false); OnShow(); });
            boy = UIFactory.Button(helpers.transform, "", Theme.Card, Theme.Ink, () => { game.SetGuide(true); OnShow(); });
            toneName = UIFactory.Label(cardRt, "", Theme.Title, Theme.Ink, TextAnchor.MiddleCenter, true);
            BuildSwatches(cardRt);
            hint = UIFactory.Label(cardRt, "", Theme.Label, Theme.Muted, TextAnchor.UpperLeft, false);
            var buttons = UIFactory.Row(cardRt, "Buttons", Theme.Gap, Theme.TouchTarget);
            rename = UIFactory.Button(buttons.transform, "", Theme.Sky, Theme.Ink, () => ui.Names.Change(Name));
            done = UIFactory.Button(buttons.transform, "", Theme.Primary, Color.white, () => ui.ShowMenu(ui.CameFrom("menu", "home")));
        }

        Button done, rename, girl, boy;
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
            UIFactory.SetButtonLabel(rename, Localization.T("look.rename", "Change my name"));
            UIFactory.SetButtonLabel(girl, string.Format(Localization.T("name.helper.girl", "A girl: {0}"), Localization.DefaultName));
            UIFactory.SetButtonLabel(boy, string.Format(Localization.T("name.helper.boy", "A boy: {0}"), Localization.BoyName));
            girl.image.color = Localization.BoyGuide ? Theme.Card : Theme.Sun;
            boy.image.color = Localization.BoyGuide ? Theme.Sun : Theme.Card;
            // Skin tones and clothes are chosen in daylight, where their colours are true.
            if (game.AR.Stage != null) game.AR.Stage.ChooseNight(false);
            bool inRoom = game.AR.HasCharacter;
            preview.gameObject.SetActive(!inRoom);
            Root.GetComponent<Image>().color = inRoom ? Theme.Clear : Theme.Sky;   // no camera picture yet: not a black screen
            hint.text = inRoom
                ? Localization.T("look.hint.room", "Keza can look like you, or like a friend. Watch the change in the 3D world!")
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
