using SafeTouch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.UI.Screens
{
    /// <summary>
    /// "What's your name?" The child types a first name or a nickname, and
    /// the guide in the room takes it: the lessons then talk about "Amani's
    /// bubble" instead of "Keza's", so they are about the child. Skipping
    /// keeps "Keza". The name stays on this phone, is never sent, and is
    /// deleted with the progress. Shown once after consent, and again from
    /// "Keza's look" to change it.
    /// </summary>
    public class NameScreen : UIScreen
    {
        Text question, note, preview;
        InputField field;
        Button ok, skip;
        string returnTo;

        public override string Name { get { return "name"; } }

        protected override void BuildContent(RectTransform root)
        {
            var col = StandardColumn(root, "", Back);
            UIFactory.Spacer(col, 0.4f);
            question = UIFactory.Label(col, "", Theme.Display, Theme.Ink, TextAnchor.MiddleCenter, true);

            // A name tag: the guide's name in big letters as the child types it.
            var tag = UIFactory.Panel(col, "Name tag", Theme.Card, true);
            var tagLayout = UIFactory.Column((RectTransform)tag.transform, 12f, 40);
            tagLayout.childAlignment = TextAnchor.MiddleCenter;
            var stripe = UIFactory.Panel(tag.transform, "Stripe", Theme.Sun, true);
            UIFactory.FixedHeight(stripe, 18f);
            preview = UIFactory.Label(tag.transform, "", Theme.Display, Theme.Primary, TextAnchor.MiddleCenter, true);
            field = UIFactory.Input(tag.transform, "", InputField.ContentType.Name, TouchScreenKeyboardType.Default, 20);
            field.onValueChanged.AddListener(_ => UpdatePreview());

            note = UIFactory.Label(col, "", Theme.Label, Theme.Muted, TextAnchor.MiddleCenter, false);
            UIFactory.Spacer(col, 1f);
            ok = UIFactory.Button(col, "", Theme.Primary, Color.white, () => Done(field.text));
            skip = UIFactory.Button(col, "", Theme.Card, Theme.Ink, () => Done(""));
        }

        /// <summary>Opens the screen to change the name, then goes back to the given screen.</summary>
        public void Change(string backTo)
        {
            returnTo = backTo;
            ui.ShowMenu(Name);
        }

        public override void OnShow()
        {
            TitleText.text = Localization.T("name.title", "Your name");
            question.text = Localization.T("name.question", "What's your name?");
            note.text = Localization.T("name.note", "Your helper in the room will have your name. Use your first name or a nickname. It stays on this phone.");
            var placeholder = field.placeholder as Text;
            if (placeholder != null) placeholder.text = Localization.T("name.placeholder", "Type your name");
            var saved = game.Progress.GetData().childName;
            if (string.IsNullOrEmpty(field.text) && !string.IsNullOrEmpty(saved)) field.text = saved;
            UIFactory.SetButtonLabel(ok, Localization.T("name.ok", "That's me!"));
            UIFactory.SetButtonLabel(skip, string.Format(Localization.T("name.skip", "Skip: call my helper {0}"), Localization.DefaultName));
            UpdatePreview();
        }

        void UpdatePreview()
        {
            var clean = Localization.CleanName(field.text);
            preview.text = string.IsNullOrEmpty(clean) ? Localization.DefaultName : clean;
            ok.interactable = clean.Length > 0;
        }

        void Done(string name)
        {
            game.SetChildName(name);
            if (game.Reader != null) game.Reader.SpeakIfAuto(string.Format(Localization.T("keza.hello", "Hi! I'm {0}!"), Localization.CharacterName));
            Leave();
        }

        void Back()
        {
            if (!string.IsNullOrEmpty(returnTo)) { Leave(); return; }
            ui.ShowMenu("splash");
        }

        void Leave()
        {
            var back = returnTo;
            returnTo = null;
            if (!string.IsNullOrEmpty(back)) ui.ShowMenu(back);
            else game.ContinueToLessons();
        }
    }
}
