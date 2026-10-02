# Hardware integration

SafeTouch is handheld AR on an Android phone. The child holds the phone (or an adult holds it with them), looks through the camera at the room, and taps large buttons on the screen. No headset or controller is used.

## Why a phone and not a headset

| | Android phone with ARCore | AR/VR headset |
|---|---|---|
| Cost and access in Rwanda | Families and schools already own them; no purchase in the 60,000 RWF budget | Several hundred dollars each |
| Fit for ages 6 to 11 | Safe for all ages | Most headsets are rated 10+ or 13+ |
| Supervision | The adult sees the same screen and can talk about each card | The adult can't see what the child sees |
| Real-world awareness | The child stays aware of the room (the proposal's reason for choosing AR over VR) | VR hides the room |

## Target devices

* Android 8.0 (API 26) or newer, **on Google's ARCore supported devices list**: <https://developers.google.com/ar/devices>. Check the exact model before a school visit, because many budget phones are not on it.
* **Google Play Services for AR** installed from the Play Store (it installs automatically on supported phones; install it before a session to avoid a download on school Wi-Fi).
* Rear camera, gyroscope and accelerometer (all ARCore phones have them).
* Speaker or headphones for narration.
* 2 GB RAM or more. The app works fully offline.

The development test device is the researcher's own ARCore phone (0 RWF in the budget).

## How the child interacts

| What the child does | Hardware used | What happens in SafeTouch | Code |
|---|---|---|---|
| Moves the phone slowly over the floor | Camera and motion sensors (ARCore motion tracking and plane detection) | White dots show the floor being found; a reticle appears | `ARManager.DetectPlane()` |
| Taps **Place Keza here** | Touch screen | Keza appears on the floor, facing the child, anchored in place | `ARManager.PlaceCharacter()`, `AnchorCharacter()` |
| Walks around Keza or crouches down | 6-degrees-of-freedom tracking | Keza and the zones stay fixed on the floor, so the child can look from any side | `ARAnchor`, `TrackedPoseDriver` |
| Taps a zone, an answer or a helper | Touch screen | Feedback card, sound, Keza reacts | `LessonScreen.ShowPrompt()` |
| Taps **NO!** and says "No!" out loud | Touch screen (the microphone is **not** used, to avoid recording a child's voice) | Keza raises a hand, the visitor steps back | `StopFrameworkLesson.SayNo()` |
| Taps **Listen again** | Speaker | Narration replays in English or Kinyarwanda | `LessonBase.Narrate()` |
| Taps **Place again** | Touch screen | Placement restarts somewhere new | `GameManager.MoveCharacter()` |
| Taps **Move:** then the arrow buttons | Touch screen | Switches between Keza and the other people, then walks the chosen one 15 cm per tap. Up walks away from the phone and left/right follow the screen, because the direction is worked out from the camera. A label says which of Keza's zones a visitor is in | `ARManager.SelectNext()`, `ARManager.Nudge()`, `ZoneOf()` |
| Taps the **person on screen** in the Memory Quest chase | Touch screen + AR camera ray | A ray from the camera through the tap hits the person's collider and counts as saying "Stop!"; the timer stops and stars are given | `ReviewLesson.Update()` (Input System `Pointer`, `Physics.Raycast`) |
| Walks Keza to a helper during the chase | Touch screen (arrows) | Reaching a helper within 0.5 m wins the chase | `ReviewLesson.Update()` |
| Presses the Android back button | Hardware or gesture back | Leaves the lesson and returns to the lesson map | `GameManager.Update()` |

Touch targets are at least 56 dp high, larger than Android's 48 dp minimum, because children's taps are less precise.

## Space and safety during sessions

* A clear floor area of about 2 × 2 m. If space is tight, place Keza on a table: the zones scale with the plane.
* An adult stays beside the child. Sessions last 15 to 20 minutes.
* Children can play sitting down; nothing requires walking.
* The screen stays on during lessons (`Screen.sleepTimeout`), and progress saves when the app is paused.

## Developer hardware and editor workflow

| Item | Minimum | Used for |
|---|---|---|
| Laptop | Windows 10/11 64-bit or macOS 13+, 16 GB RAM recommended, 25 GB free disk | Unity 6 editor, Android build tools |
| USB cable (data, not charge-only) | | Build And Run, logcat |
| ARCore phone with Developer options and USB debugging on | | Testing on a real device |

1. **In the editor, without a phone:** AR Foundation's **XR Simulation** runs a virtual room in Play mode. Move with the right mouse button and WASD. Planes are detected in the simulated room, so the full lesson flow can be tested on the laptop. If XR Simulation is off, `ARManager` falls back to a virtual floor so the UI can still be clicked through.
2. **On the phone:** File > Build Profiles > Android > **Build And Run**. The APK installs over USB and starts.
3. **Debugging on the phone:** Window > Analysis > **Android Logcat** (package) filtered by `SafeTouch`, or `adb logcat -s Unity`.
4. **Recording the demo:** mirror the phone to the laptop with **scrcpy** (free) and record with OBS, so the video shows the phone screen clearly.
