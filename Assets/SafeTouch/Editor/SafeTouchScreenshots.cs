using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SafeTouch.Core;
using SafeTouch.Data;
using SafeTouch.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SafeTouch.EditorTools
{
    /// <summary>
    /// SafeTouch > 7. Render Screens On Phones And Tablets. Builds the real UI
    /// in the Editor and renders every screen at several device sizes, as the
    /// app's ResponsiveCanvas and SafeArea would lay it out:
    ///   Samsung Galaxy A70 (1080 x 2400, 20:9, notch), a 16:9 phone, a small
    ///   720p phone, a 4:3 tablet and a 16:10 tablet.
    /// Writes one PNG per screen to Screenshots/&lt;device&gt;/ and an overview
    /// sheet per device to docs/designs/devices/, and logs any text or button
    /// that falls outside the safe area, so layout problems are caught
    /// without a phone. Run SafeTouch > 2. Build SafeTouch Scene first.
    /// </summary>
    public static class SafeTouchScreenshots
    {
        struct Device
        {
            public string id, label;
            public int width, height;
            public int topInset;   // status bar or notch, pixels
        }

        static readonly Device[] Devices =
        {
            new Device { id = "samsung-a70",  label = "Samsung Galaxy A70 (1080 x 2400)", width = 1080, height = 2400, topInset = 80 },
            new Device { id = "phone-16x9",   label = "Phone 16:9 (1080 x 1920)",         width = 1080, height = 1920, topInset = 63 },
            new Device { id = "small-phone",  label = "Small phone (720 x 1280)",         width = 720,  height = 1280, topInset = 48 },
            new Device { id = "tablet-4x3",   label = "Tablet 4:3 (1536 x 2048)",         width = 1536, height = 2048, topInset = 48 },
            new Device { id = "tablet-16x10", label = "Tablet 16:10 (1600 x 2560)",       width = 1600, height = 2560, topInset = 50 },
        };

        const string ScenePath = "Assets/SafeTouch/Scenes/SafeTouch.unity";
        const string ShotsFolder = "Screenshots";
        const string SheetFolder = "docs/designs/devices";

        [MenuItem("SafeTouch/7. Render Screens On Phones And Tablets", priority = 7)]
        public static void RenderAll()
        {
            if (!File.Exists(ScenePath)) { Debug.LogError("[Screens] Run SafeTouch > 2. Build SafeTouch Scene first."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var ui = game.UI;
            // Wake the parts the screens read from, as Play mode would.
            Call(game, "Awake");
            Call(game.Progress, "Awake");
            game.Progress.Load();
            Localization.SetLanguage("en");
            ui.Initialise(game);

            var canvas = ui.GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            var safe = canvas.GetComponentInChildren<SafeArea>(true);
            var camera = new GameObject("Screenshot camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.22f, 0.24f, 0.26f);   // stands in for the camera picture on AR screens
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            int problems = 0;
            Directory.CreateDirectory(SheetFolder);
            foreach (var device in Devices)
            {
                var target = new RenderTexture(device.width, device.height, 24);
                camera.targetTexture = target;
                // The same rule as ResponsiveCanvas, as a fixed scale (edit mode has no screen to measure).
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = ResponsiveCanvas.ScaleFactor(device.width, device.height);
                Canvas.ForceUpdateCanvases();
                safe.Apply(new Rect(0, 0, device.width, device.height - device.topInset), new Vector2(device.width, device.height));

                string folder = Path.Combine(ShotsFolder, device.id);
                Directory.CreateDirectory(folder);
                var shots = new List<KeyValuePair<string, Texture2D>>();
                foreach (var shot in Shots(game))
                {
                    try
                    {
                        shot.Value();
                        Settle(canvas);
                        problems += Check(device, shot.Key, safe);
                        var png = Capture(camera, target);
                        File.WriteAllBytes(Path.Combine(folder, shot.Key + ".png"), png.EncodeToPNG());
                        shots.Add(new KeyValuePair<string, Texture2D>(shot.Key, png));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[Screens] " + device.id + "/" + shot.Key + " could not be shown: " + e.Message);
                    }
                    ui.HideFeedback();
                }
                SaveSheet(device, shots, Path.Combine(SheetFolder, device.id + ".png"));
                foreach (var s in shots) UnityEngine.Object.DestroyImmediate(s.Value);
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
            }
            if (game.Voice != null) game.Voice.Stop();
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            Debug.Log("[Screens] Done: " + Devices.Length + " devices, " + problems + " layout problem(s). PNGs in " + ShotsFolder + "/, overview sheets in " + SheetFolder + "/.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);   // discard the in-memory changes
        }

        /// <summary>Every screen, set up with typical content, in the order a child meets them.</summary>
        static IEnumerable<KeyValuePair<string, Action>> Shots(GameManager game)
        {
            var ui = game.UI;
            Func<string, Action, KeyValuePair<string, Action>> shot = (n, a) => new KeyValuePair<string, Action>(n, a);
            yield return shot("01-splash", () => ui.ShowMenu("splash"));
            yield return shot("01a-consent", () => ui.ShowMenu("consent"));
            yield return shot("01a-consent-form", () =>
            {
                ui.ShowMenu("consent");
                var scroll = ui.GetComponentsInChildren<ScrollRect>(false).FirstOrDefault();
                if (scroll != null) { Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 0f; }   // the details and tick boxes
            });
            yield return shot("01b-verify", () =>
            {
                // A sample consent waiting for its code (in memory only; nothing is saved or sent).
                var sample = ConsentRecord.Create(Guid.NewGuid().ToString(), "en");
                sample.parentName = "Uwase Marie"; sample.email = "uwase.marie@example.com"; sample.emailMasked = "u***e@example.com";
                sample.phone = "+250 788 123 456"; sample.country = "Rwanda"; sample.sentToServer = true;
                sample.agreements = new ConsentAgreements { guardian = true, terms = true, childUse = true, dataUse = true };
                typeof(SafeTouch.Services.ConsentService).GetProperty("Record").SetValue(game.Consent, sample);
                ui.ShowMenu("verify");
            });
            yield return shot("01c-login", () =>
            {
                // A parent whose consent is confirmed, not yet logged in (in memory only).
                var sample = ConsentRecord.Create(Guid.NewGuid().ToString(), "en");
                sample.parentName = "Uwase Marie"; sample.email = "uwase.marie@example.com"; sample.emailMasked = "u***e@example.com";
                sample.phone = "+250 788 123 456"; sample.country = "Rwanda"; sample.sentToServer = true; sample.verified = true;
                sample.agreements = new ConsentAgreements { guardian = true, terms = true, childUse = true, dataUse = true };
                typeof(SafeTouch.Services.ConsentService).GetProperty("Record").SetValue(game.Consent, sample);
                ui.ShowMenu("login");
            });
            yield return shot("01d-age-check", () => ui.ShowMenu("agegate"));
            yield return shot("02-age", () => ui.ShowMenu("age"));
            yield return shot("03-home", () => ui.ShowMenu("home"));
            yield return shot("04-scan", () => ui.ShowMenu("scan"));
            yield return shot("05-lesson", () =>
            {
                ui.ShowMenu("lesson");
                ui.Lesson.SetHeader(Localization.T("lesson1.title"), Localization.T("bubble.step1", "Step 1 of 4: explore the zones"));
                ui.Lesson.ShowPrompt(Localization.T("bubble.intro.title", "Meet Keza's space bubble"),
                    Localization.T("bubble.intro.body", "Everyone has a bubble of space around their body. Tap each colour to learn about it."),
                    new PromptOption(Localization.T("bubble.red", "Red zone"), Theme.Unsafe, null),
                    new PromptOption(Localization.T("bubble.yellow", "Yellow zone"), Theme.Caution, null),
                    new PromptOption(Localization.T("bubble.green", "Green zone"), Theme.Safe, null),
                    new PromptOption(Localization.T("bubble.ready", "I'm ready!"), Theme.Primary, null));
            });
            yield return shot("06-feedback", () =>
            {
                ui.ShowMenu("lesson");
                ui.ShowFeedback(Localization.T("feedback.correct", "That's right!"),
                    Localization.T("stop.o.fb", "Clear words help adults help you. It is never the child's fault."), FeedbackTone.Correct, null);
            });
            yield return shot("07-say-no", () =>
            {
                ui.ShowMenu("lesson");
                Call(game.Voice, "Awake");
                game.Voice.Enabled = false;   // no microphone in the Editor capture: shows the "didn't hear you" result
                ui.Lesson.ShowVoice(Localization.T("stop.s.title", "S: Say NO"),
                    Localization.T("stop.s.voice", "Say \"NO!\" out loud, clearly, so the person knows you mean it. Keza is listening!"),
                    game.Voice, null);
            });
            yield return shot("08-memory", () =>
            {
                ui.ShowMenu("memory");
                ui.Memory.StartGame(ContentLibrary.ReviewPairs.ToList(), null);
            });
            yield return shot("09-places", () => { ui.ShowMenu("places"); ui.Places.Ask(_ => { }); });
            yield return shot("10-adults", () => ui.ShowMenu("adults"));
            yield return shot("11-certificate", () =>
            {
                ui.ShowMenu("certificate");
                var ideas = new List<KeyValuePair<Color, string>>
                {
                    new KeyValuePair<Color, string>(Theme.Unsafe, Localization.T("review.cert.bubble", "My bubble: red is my body, yellow is people I trust, green is everyone else.")),
                    new KeyValuePair<Color, string>(Theme.Caution, Localization.T("review.cert.sort", "Touch can be safe, uncomfortable or unsafe. Touching secrets are always told.")),
                    new KeyValuePair<Color, string>(Theme.Primary, Localization.T("review.cert.stop", "S-T-O-P: Say no, Tell, Open up, Protect.")),
                    new KeyValuePair<Color, string>(Theme.Safe, string.Format(Localization.T("review.cert.hand", "My helpers: {0}."), "Teacher, Grandparent")),
                    new KeyValuePair<Color, string>(Theme.Sun, string.Format(Localization.T("review.cert.chase", "Safe places chase: my fastest NO took {0:0.0} seconds."), 2.4f)),
                };
                ui.Certificate.Show(ideas, 18, 21, null);
            });
            yield return shot("12-badges", () => ui.ShowMenu("badges"));
            yield return shot("13-look", () => ui.ShowMenu("look"));
            yield return shot("14-grown-ups", () => ui.ShowMenu("parentGate"));
            yield return shot("15-parent", () => ui.ShowMenu("parent"));
        }

        /// <summary>Lays everything out (layout groups need a few passes in edit mode).</summary>
        static void Settle(Canvas canvas)
        {
            for (int i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var group in canvas.GetComponentsInChildren<LayoutGroup>(false))
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Logs text and buttons that end up outside the safe area (scroll lists may scroll, so they are skipped).</summary>
        static int Check(Device device, string shot, SafeArea safe)
        {
            var area = WorldRect((RectTransform)safe.transform);
            int found = 0;
            foreach (var graphic in safe.GetComponentsInChildren<Graphic>(false))
            {
                if (!graphic.enabled || !(graphic is Text || graphic.GetComponent<Button>() != null)) continue;
                if (graphic.GetComponentInParent<ScrollRect>() != null) continue;
                var r = WorldRect(graphic.rectTransform);
                const float slack = 0.002f;
                if (r.xMin < area.xMin - slack || r.xMax > area.xMax + slack || r.yMin < area.yMin - slack || r.yMax > area.yMax + slack)
                {
                    var text = graphic as Text ?? graphic.GetComponentInChildren<Text>();
                    Debug.LogWarning("[Screens] " + device.id + "/" + shot + ": \"" + (text != null ? text.text : graphic.name) + "\" goes outside the screen");
                    found++;
                }
            }
            return found;
        }

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        static Texture2D Capture(Camera camera, RenderTexture target)
        {
            camera.Render();
            RenderTexture.active = target;
            var png = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            png.Apply();
            RenderTexture.active = null;
            return png;
        }

        /// <summary>All screens of one device side by side, small, in one image for the report.</summary>
        static void SaveSheet(Device device, List<KeyValuePair<string, Texture2D>> shots, string path)
        {
            if (shots.Count == 0) return;
            const int thumbHeight = 640, gap = 24, perRow = 8;
            int thumbWidth = Mathf.RoundToInt(thumbHeight * (float)device.width / device.height);
            int rows = Mathf.CeilToInt(shots.Count / (float)perRow);
            int width = perRow * thumbWidth + (perRow + 1) * gap, height = rows * thumbHeight + (rows + 1) * gap;
            var sheet = new Texture2D(width, height, TextureFormat.RGB24, false);
            var fill = Enumerable.Repeat((Color32)new Color(0.95f, 0.96f, 0.97f), width * height).ToArray();
            sheet.SetPixels32(fill);
            for (int i = 0; i < shots.Count; i++)
            {
                var src = shots[i].Value;
                int ox = gap + (i % perRow) * (thumbWidth + gap);
                int oy = height - (gap + (i / perRow + 1) * (thumbHeight + gap)) + gap;
                for (int y = 0; y < thumbHeight; y++)
                for (int x = 0; x < thumbWidth; x++)
                    sheet.SetPixel(ox + x, oy + y, src.GetPixelBilinear((x + 0.5f) / thumbWidth, (y + 0.5f) / thumbHeight));
            }
            sheet.Apply();
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            Debug.Log("[Screens] " + device.label + ": " + shots.Count + " screens -> " + path);
        }

        static void Call(object target, string method)
        {
            if (target == null) return;
            var m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m != null) m.Invoke(target, null);
        }
    }
}
