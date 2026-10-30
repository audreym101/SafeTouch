# 3D mode: Keza on a playground, no scanning

SafeTouch now runs as a 3D character app by default. Keza stands on a sunny playground built in 3D, with a lawn, a path, trees, a little house, bushes, flowers and clouds. She appears as soon as a lesson opens:

- No camera permission is needed.
- The child does not scan a floor or table.
- There is no "Find the floor" screen.

## Making Keza bigger or smaller

| On the phone | In the Editor |
|---|---|
| The round **+** and **−** buttons on the right of the 3D view | the same buttons, or the mouse wheel |
| Pinch with two fingers | |

The size goes from 0.6× to 3.2× the starting size.

The camera moves closer or further away; Keza's real size does not change. This keeps her bubble zones (0.3 / 0.6 / 0.9 m) and the places where visitors and helpers stand correct in every lesson. The lesson code is unchanged.

While the body lesson uses a pinch to turn and resize Keza ("look closer"), the playground does not zoom, so the two never fight.

## How it works

- [Stage3D.cs](../Assets/SafeTouch/Scripts/AR/Stage3D.cs) builds the playground from simple shapes.
  - It switches off the phone-camera parts of the AR camera (camera picture and tracking), turns on a sky-blue background, light and soft shadows, and places Keza in the middle.
  - It moves the camera smoothly when the size changes.
- [ARManager.cs](../Assets/SafeTouch/Scripts/AR/ARManager.cs) has a **Use AR** switch, which is **off** by default.
  - When it is off, `BeginPlacement` puts Keza on the playground at once. The status is always "ready". The AR session and the phone's camera stay off.
- [SizeButtons.cs](../Assets/SafeTouch/Scripts/UI/SizeButtons.cs) shows the + / − buttons. They appear only on the 3D view while Keza is on screen.

## Going back to AR

To use AR again:
1. Select the object with **ARManager** in the scene.
2. Tick **Use AR**.

Scanning, placing on a real floor and walking around Keza work as before.
