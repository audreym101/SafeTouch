# SafeTouch — Unity Project

AR-based child protection education app (BSc Capstone, ALU). This repo is being built out
step by step, matching the timeline in the proposal (Fig. 1.8).

## Status: Week 1 — Foundation

Done in this pass:
- Project scaffold: `Assets/`, `Packages/manifest.json` (URP, AR Foundation, ARCore, ARKit,
  XR Management, Input System, TextMeshPro), `ProjectSettings/ProjectVersion.txt` pinned to
  **Unity 6000.6.0f1**.
- Core architecture (`Assets/Scripts/Core`): `GameManager`, `ProgressTracker`, `AudioManager` —
  matches the "Unity app core" box in the architecture diagram (section 3.4.2). Progress is
  stored **locally only** via `PlayerPrefs` as JSON (`Assets/Scripts/Data/ProgressData.cs`),
  consistent with NFR1 (no personal data collected) and section 1.5 (cloud sync is optional,
  anonymous, and off by default — see `AppConfig.CloudSyncEnabled`).
- AR plane detection + character placement (`Assets/Scripts/AR`): `ARPlaneTapToPlace`
  (FR1, FR2), `ARSessionStatusUI` (calm, child-friendly status messages instead of raw
  tracking states).
- Editor automation (`Assets/Editor`): `ProjectSetup.cs` and `SceneBuilder.cs`, run once from
  the Unity menu (see below) to finish configuring the project and generate the Week 1 scenes.

**Important — this sandbox has no working Unity Editor binary**, so none of this has been
compiled or opened in the Editor yet. Everything below is written against stable, documented
Unity/AR Foundation APIs, but the *first time you open this in real Unity 6000.6 is the real
test*. If the Console shows compile errors after opening, tell me the exact error text and
I'll fix it immediately.

## First-time setup (in Unity, on your machine)

1. **Unity Hub → Add → select this folder** (`SafeTouch`). Open with Unity **6000.6.0f1**.
2. Let Package Manager finish resolving packages (first import can take a few minutes).
   If any package fails to resolve at the exact pinned version, Package Manager will offer a
   compatible version — accept it.
3. Run **`SafeTouch > Setup > 1. Configure Player Settings`** from the menu bar.
4. Run **`SafeTouch > Setup > 2. Build Week 1 Scenes (Bootstrap + AR Test)`**.
5. Two manual steps the scripts intentionally don't automate (version-fragile APIs, but
   quick in the GUI):
   - **Edit → Project Settings → XR Plug-in Management** → tick **ARCore** under the
     Android tab, tick **ARKit** under the iOS tab.
   - **Create a URP asset**: `Assets → Create → Rendering → URP Asset (with Universal
     Renderer)`, then in `Edit → Project Settings → Graphics` and `→ Quality`, assign it as
     the render pipeline asset.
6. Open `Assets/Scenes/01_ARFoundation_PlaneDetection.unity`, press Play (Editor AR
   simulation is limited — for a real test, build to an ARCore-capable Android phone via
   `File → Build Settings`). Move the phone to detect a surface, then tap to place the
   placeholder character (an orange capsule stand-in — no 3D model yet). Tapping elsewhere
   moves it, matching FR2.

## What's covered vs. what's next

| Requirement | Status |
|---|---|
| FR1 plane detection | Done (`ARPlaneTapToPlace`, `ARSessionStatusUI`) |
| FR2 place + move character | Done |
| NFR1 no personal data | Enforced by design — see `AppConfig`, `ProgressData` comments |
| NFR2 Android 8.0+/ARCore | Set in `ProjectSetup.cs` (min API 26) |
| NFR6 easy exit from any screen | Not built yet — arrives with `MainMenu` in the next step |
| FR3 Personal Space Bubble | Next (Week 2) |
| FR4/FR5 Scenario cards | Week 3 |
| FR6 STOP framework | Week 4 |
| FR7 Trusted Adults | Week 5 |
| FR9 Parent/teacher section | Weeks 6-7, alongside `MainMenu` |
| FR10 Kinyarwanda support | Deferred (optional per scope) |

Say the word and I'll continue with the Personal Space Bubble lesson (FR3) next, following
the same pattern: runtime scripts + an Editor scene-builder command.

## Known risk areas (things to watch for on first open)

- **TMP Essentials prompt**: the first time anything creates a TextMeshPro object, Unity
  may pop up a one-time "Import TMP Essential Resources" dialog. Accept it, then re-run
  step 4 above if the status text object looks empty.
- **`ARPlaneMeshVisualizer` class name**: assumed stable from AR Foundation 3.x–6.x. If the
  Console reports it missing, tell me and I'll swap in whatever AR Foundation 6.0.5 actually
  calls it (e.g. `ARFeatheredPlaneMeshVisualizer`).
- **Package versions in `manifest.json`** are best-effort pins for Unity 6000.6; Package
  Manager will offer newer compatible versions on first resolve — accept those prompts.
