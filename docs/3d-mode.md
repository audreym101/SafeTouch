# How Keza is shown: 3D world, camera or scanned floor

SafeTouch is a 3D character app: Keza appears in her own 3D world (**Playground**) by default, with no camera. In **Settings > See my helper in my room (camera)**, the child can switch to the camera picture instead, and back. The choice is kept on the phone and takes effect at once.

Developers can also set **View Mode** on `ARManager` in the Inspector. **ScanFloor** set there is always used; otherwise the Settings choice decides between Playground and Camera.

| View Mode | What the child sees | Camera needed? | Scanning? |
|---|---|---|---|
| **Playground** (default) | A 3D lawn with trees, a house, flowers and clouds | no | no |
| **Camera** (Settings switch) | The phone's camera picture, with Keza standing in the room | yes | **no** |
| **ScanFloor** | The camera picture; the child finds a floor or table first, then taps Place | yes | yes |

## Camera mode: Keza in the room, no scanning

1. When a lesson opens, the app asks for the camera the first time, with the usual explanation.
2. As soon as the phone knows where it is (usually under a second, never more than 3 seconds), Keza appears **where the phone is looking**, facing the child. Nobody has to find a floor first.
   - She stands on the floor just below the middle of the screen, 0.9–3 m away, so she is in the picture whether the phone points ahead or down.
   - That floor is the one ARCore has already found there. If it hasn't found one yet, the app guesses the floor is about 1.1 m below the phone.
3. ARCore keeps looking for the real floor in the background, without showing any grid. When it finds the floor under Keza, she settles gently onto it **once**, and then stays put. Small corrections ARCore makes afterwards are ignored, so she never keeps drifting. She only moves again if the floor height changes by more than 4 cm.
4. If the child points the phone away and Keza is out of the picture for a moment, a yellow **"Bring Keza back"** button appears near the top. One tap brings her back in front of the phone, with everyone around her, and the lesson carries on.

**What the child can do:**
- **Drag** Keza and the people with a finger.
- **Tap** them, as before.
- Tap **Place again** to bring Keza back in front of the phone.
- Make Keza **bigger or smaller** with the round **+** and **−** buttons on the right, or with a two-finger pinch.

**How + and − work here:** Keza, and everyone with her, steps closer to the phone or further away (between 0.6 m and 5 m). Her real size never changes, so her bubble zones (0.3 / 0.6 / 0.9 m) and every distance in the lessons stay correct.

**If the camera is refused, or the phone cannot run ARCore,** the app switches to the Playground by itself, so the child can still do every lesson. The Unity Editor without AR does the same.

## Playground mode (the default)

- Keza stands on a 3D lawn, and the phone's camera is never opened.
- **+**, **−** and pinch move the 3D camera closer or further away (0.6× to 3.2×).
- [Stage3D.cs](../Assets/SafeTouch/Scripts/AR/Stage3D.cs) builds the lawn from simple shapes.

## ScanFloor mode

The original flow:
1. Find a large floor or table (shown as a grid).
2. Tap **Place**.
3. Keza is anchored to that real surface.

## Code

- [ARManager.cs](../Assets/SafeTouch/Scripts/AR/ARManager.cs) holds the `ViewMode`.
  - `PlaceInCamera` and `InstantPose`: where Keza appears without a floor.
  - `SettleOnFloor`: drops her onto the floor once ARCore finds it.
  - `ChangeSize`: bigger or smaller, in every mode.
  - `StartPlayground`: the 3D lawn, also used as the fallback.
  - `SetCameraMode`: the Settings switch between the 3D world and the camera picture. `Stage3D.Teardown` gives the camera back its own settings.
- [SizeButtons.cs](../Assets/SafeTouch/Scripts/UI/SizeButtons.cs): the + / − buttons and the pinch.
