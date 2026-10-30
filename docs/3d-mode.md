# How Keza is shown: camera, playground or scanned floor

`ARManager` has a **View Mode** setting in the Inspector. **Camera** is the default.

| View Mode | What the child sees | Camera needed? | Scanning? |
|---|---|---|---|
| **Camera** (default) | The phone's camera picture, with Keza standing in the room | yes | **no** |
| **Playground** | A 3D lawn with trees, a house, flowers and clouds | no | no |
| **ScanFloor** | The camera picture; the child finds a floor or table first, then taps Place | yes | yes |

## Camera mode: Keza in the room, no scanning

1. When a lesson opens, the app asks for the camera the first time, with the usual explanation.
2. As soon as the phone knows where it is (usually under a second, never more than 3 seconds), Keza appears **1.5 m in front of the phone**, facing the child. Nobody has to find a floor first.
3. ARCore keeps looking for the real floor in the background, without showing any grid. When it finds the floor under Keza, she settles gently onto it. Until then she stands about 1.1 m below the phone, which is where the floor is when a child holds it.

**What the child can do:**
- **Drag** Keza and the people with a finger.
- **Tap** them, as before.
- Tap **Place again** to bring Keza back in front of the phone.
- Make Keza **bigger or smaller** with the round **+** and **−** buttons on the right, or with a two-finger pinch.

**How + and − work here:** Keza, and everyone with her, steps closer to the phone or further away (between 0.6 m and 5 m). Her real size never changes, so her bubble zones (0.3 / 0.6 / 0.9 m) and every distance in the lessons stay correct.

**If the camera is refused, or the phone cannot run ARCore,** the app switches to the Playground by itself, so the child can still do every lesson. The Unity Editor without AR does the same.

## Playground mode

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
- [SizeButtons.cs](../Assets/SafeTouch/Scripts/UI/SizeButtons.cs): the + / − buttons and the pinch.
