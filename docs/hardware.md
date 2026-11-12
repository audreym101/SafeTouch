# Hardware integration

SafeTouch is a 3D character app on an Android phone. The child holds the phone (or an adult holds it with them), watches Keza in her 3D world, and uses the touch screen: tapping, dragging, twisting and pinching. The microphone hears the child's spoken NO. No headset, controller or camera is used.

## Why a phone and not a headset

| | Android phone | AR/VR headset |
|---|---|---|
| Cost and access in Rwanda | Families and schools already own them; no purchase in the 60,000 RWF budget | Several hundred dollars each |
| Fit for ages 6 to 11 | Safe for all ages | Most headsets are rated 10+ or 13+ |
| Supervision | The adult sees the same screen and can talk about each card | The adult can't see what the child sees |
| Real-world awareness | The child sits with the adult and stays aware of the room | VR hides the room |

The proposal planned handheld AR through the phone's camera. It was replaced by a 3D world: finding a floor through the camera was slow and unreliable for young children and needed an ARCore phone ([3d-mode.md](3d-mode.md#why-a-3d-world-and-not-ar)).

## Target devices

* Android 8.0 (API 26) or newer, 64-bit (ARM64), with OpenGL ES 3. No ARCore phone or Google Play Services for AR needed.
* A microphone for the voice coach (optional: a NO button always works).
* Speaker or headphones for narration and the child's recorded NOs.
* 2 GB RAM or more. The app works fully offline.

The development test device is the researcher's own Android phone (0 RWF in the budget).

## How the child interacts

| What the child does | Hardware used | What happens in SafeTouch | Code |
|---|---|---|---|
| Opens a lesson | Touch screen | Keza appears at once in her 3D park, facing the child | `ARManager.PlaceGuide()`, `Stage3D` |
| Taps a zone, an answer or a helper | Touch screen | Feedback card, sound, Keza reacts | `LessonScreen.ShowPrompt()` |
| Drags a person with a finger | Touch screen | They walk where the finger goes, at a person's pace; a label says which of Keza's zones they are in | `ARTouch`, `ARManager.DragFigure()`, `ZoneOf()` |
| Twists two fingers | Touch screen | Turns the person last touched (or Keza) | `ARTouch` |
| Pinches, or taps + and − | Touch screen | The camera moves closer or further; Keza's real size and the zone distances never change | `ARManager.ChangeSize()`, `SizeButtons` |
| Swipes on the grass | Touch screen | The camera swings around Keza, and drifts back after 8 seconds | `ARManager.Orbit()` |
| Opens the move pad, holds an arrow | Touch screen | The picked person walks and turns | `ARManager.Step()`, `Turn()` |
| Says "NO!" out loud | Microphone (after Keza's card and Android's question) | The NO power meter rises; stars, the loud / steady / short checks and a tip | `VoiceCoach`, `VoicePanel` |
| Taps a NO on My NO | Speaker | The child hears their own NO played back | `NoRecorder.Play()` |
| Taps **Read to me** | Speaker | The card is read aloud in English (Kinyarwanda where the phone has a voice) | `ReadAloud` |
| Taps the **person on screen** in the Memory Quest chase | Touch screen + a ray from the 3D camera | The ray hits the person's collider and counts as saying "Stop!"; the timer stops and stars are given | `ReviewLesson.OnTap()` |
| Walks Keza to a helper during the chase | Touch screen | Reaching a helper within 0.5 m wins the chase | `ReviewLesson.Update()` |
| Presses the Android back button | Hardware or gesture back | Leaves the lesson and returns to the lesson map | `GameManager.Update()` |

Touch targets are at least 56 dp high, larger than Android's 48 dp minimum, because children's taps are less precise.

## Space and safety during sessions

* A seat next to an adult; nothing requires walking or space around the child.
* An adult stays beside the child. Sessions last 15 to 20 minutes.
* The screen stays on during lessons (`Screen.sleepTimeout`), and progress saves when the app is paused.

## Developer hardware and editor workflow

| Item | Minimum | Used for |
|---|---|---|
| Laptop | Windows 10/11 64-bit or macOS 13+, 16 GB RAM recommended, 25 GB free disk | Unity 6 editor, Android build tools |
| USB cable (data, not charge-only) | | Build And Run, logcat |
| Android 8.0+ phone with Developer options and USB debugging on | | Testing on a real device |

1. **In the editor, without a phone:** press Play. The 3D world and every lesson run on the laptop exactly as on a phone; the mouse stands in for a finger and the wheel for a pinch.
2. **On the phone:** File > Build Profiles > Android > **Build And Run**. The APK installs over USB and starts.
3. **Debugging on the phone:** Window > Analysis > **Android Logcat** (package) filtered by `SafeTouch`, or `adb logcat -s Unity`.
4. **Recording the demo:** mirror the phone to the laptop with **scrcpy** (free) and record with OBS, so the video shows the phone screen clearly.
