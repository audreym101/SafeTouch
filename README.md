# SafeTouch

**An Android app with a 3D guide character that teaches children aged 6 to 11 in Rwanda how to recognise personal boundaries, spot unsafe situations and get help from a trusted adult.**

Keza, a friendly 3D guide, appears straight away in the phone's camera picture, standing in the child's room; no floor scanning is needed. If the phone cannot run AR, or the camera is refused, she appears on a 3D playground instead. With Keza the child works through four short lessons:
- the personal space bubble
- safe-or-unsafe scenario cards
- the STOP response (Say no, Tell, Openly explain, Protect yourself)
- building a "helping hand" of trusted adults

Two games then bring the lessons back. The **Memory Quest** has four rounds: match pairs, build STOP, find the helpers, and a timed chase. In **Bubble Guard**, the child says NO to unsafe requests. Children 9 to 11 get deeper content throughout.

A parent consents first, confirming by an emailed code. A grown-ups area, behind a check, gives parents and teachers conversation guides, warning signs and Rwandan helplines. Everything runs in English or Kinyarwanda. The lessons work offline, and the child's progress stays on the phone.

| | |
|---|---|
| **Walkthrough video** | <https://drive.google.com/file/d/1JzQpt8upAtpz01TeV0gTMEao6Yh_i4RG/view?usp=sharing> |
| **Technical report (PDF)** | <https://drive.google.com/file/d/1EdFF53G_4J6cSFVvZaDhoYj14elDZ1CL/view?usp=sharing> |
| **GitHub repository** | <https://github.com/audreym101/SafeTouch> (branch `audrey`) |
| **Earlier demo (initial build)** | <[https://youtu.be/FnD_9e-jiUc](https://drive.google.com/file/d/1JzQpt8upAtpz01TeV0gTMEao6Yh_i4RG/view?usp=sharing)> |
| **Figma prototype** | [Open in Figma](https://www.figma.com/proto/LGF1ZxkHqCZH1Lswr6zrMm/Untitled?page-id=1%3A1521&node-id=1-2914&p=f&viewport=165%2C-166%2C0.17&t=y4xb4JWYI30hmsgq-1&scaling=min-zoom&content-scaling=fixed&starting-point-node-id=1%3A2890&show-proto-sidebar=1) |
| **Platform** | Android 8.0+ (API 26), ARM64. Camera mode needs an ARCore phone; other phones get the 3D playground |
| **Built with** | Unity 6 LTS, AR Foundation, Google ARCore, C#; Node.js + Express API on Render with MongoDB Atlas |

---

## Contents

1. [Scope alignment: planned and built](#1-scope-alignment-planned-and-built)
2. [Technical requirements](#2-technical-requirements)
3. [Install and run](#3-install-and-run)
4. [How it works](#4-how-it-works)
5. [Testing and results](#5-testing-and-results)
6. [Performance and compatibility](#6-performance-and-compatibility)
7. [Code quality, OOP and algorithms](#7-code-quality-oop-and-algorithms)
8. [Repository structure](#8-repository-structure)
9. [Designs](#9-designs)
10. [Assets, hardware and deployment](#10-assets-hardware-and-deployment)
11. [Privacy and child safety](#11-privacy-and-child-safety)

---

## 1. Scope alignment: planned and built

FR and NFR numbers are the requirements in the approved proposal (§3.3).

| Planned feature | Intended purpose | Current status | Evidence |
|---|---|---|---|
| **FR1** Detect a horizontal surface through the camera | Put Keza on the real floor so the lesson happens in the child's own space | **Completed, then changed** (see *Scope changes*) | `AR/ARManager.cs` (`ViewMode`, `PlaceInCamera`, `SettleOnFloor`), `PlaneVisual.cs`, [docs/3d-mode.md](docs/3d-mode.md) |
| **FR2** Place the character and move it | Let the child act out each situation | **Completed.** Keza appears in front of the phone. She and the visitors can be dragged, tapped or walked with arrow buttons. **+ / −** and pinch make her bigger or smaller, and **Bring Keza back** returns her to the picture | `ARManager.Nudge()`, `ARManager.ChangeSize()`, `AR/ARTouch.cs`, `UI/SizeButtons.cs` |
| **FR3** Personal space bubble with 3 zones | Show "too close" as distance: red 0.3 m, yellow 0.6 m, green 0.9 m | **Completed.** Visitors walk in and the child decides what Keza does; a body map shows private parts on a clothed cartoon | `Lessons/PersonalSpaceBubbleLesson.cs`, `AR/BodyMap.cs` |
| **FR4** Scenario cards with answer selection | Practise telling safe, uncomfortable and unsafe apart | **Completed.** 13 cards (7 for ages 6 to 8, all 13 for 9 to 11). Answers are glowing circles on the floor | `Lessons/ScenarioLesson.cs`, `Lessons/ChoiceCircles.cs`, `Resources/Content/scenarios.json` |
| **FR5** Feedback and explanations | Correct gently, never shame | **Completed.** Every answer is explained; ages 9 to 11 also get *Think deeper* | `ScenarioLesson.cs`, `UIManager.ShowFeedback()` |
| **FR6** Interactive STOP framework | Rehearse the response in the body, not only read it | **Completed.** The child collects the S-T-O-P letters around the room, then acts out each step | `Lessons/StopFrameworkLesson.cs`, `Lessons/LetterHunt.cs` |
| **FR7** Trusted adults placed in AR | Help the child name who to tell | **Completed.** Pick 2 to 8 roles, which stand in an arc around Keza | `Lessons/TrustedAdultsLesson.cs`, `UI/Screens/AdultsScreen.cs` |
| **FR8** Progress saved locally | Keep progress without an account | **Completed.** Unlocks, stars, badges, best scores and scenario answers are saved as JSON in `PlayerPrefs` | `Managers/ProgressManager.cs`, `Data/Progress.cs` |
| **FR9** Parent and teacher resources | Support the conversation at home and school | **Completed.** Grown-up check, talk guides, warning signs, helplines, the child's answers | `UI/Screens/ParentGateScreen.cs`, `ParentScreen.cs` |
| **FR10** English and Kinyarwanda | Reach children in their first language | **Partial.** The whole app switches, but the Kinyarwanda is a first draft that a native speaker and the content experts must check | `Core/Localization.cs`, `Resources/Content/*_rw.json`, `Tests/EditMode/LocalisationTests.cs` |
| **NFR1** No identifying data about the child | Child protection, Rwanda's Law N° 058/2021 | **Completed.** Only an age band, an optional first name or nickname, and roles are kept, all on the phone. No photo, location or voice is kept | `Data/Progress.cs`, `backend/src/validate.js` |
| **NFR2** Android 8.0+ | Run on phones families already own | **Completed.** Phones without ARCore fall back to the 3D playground | `Editor/SafeTouchSetup.cs`, `AR/Stage3D.cs` |
| **NFR4 / NFR5** Child-friendly, not frightening | Keep the experience safe and fun | **Completed.** Large targets, calm colours, no buzzers; being "caught" only explains and replays | `UI/Theme.cs`, `UI/Decor.cs` |
| **NFR6** Easy exit from any screen | The child can always stop | **Completed.** A Back button on every screen returns to where the child came from; the Android back key works too | `UIScreen.StandardColumn()`, `UIManager.CameFrom()` |
| Parental consent and verification *(added)* | A parent agrees before first use and keeps a copy of what they agreed | **Completed.** Terms, four agreements and contact details. The API emails a 6-digit code and a copy of the full terms. Withdrawing deletes everything | `UI/Screens/ConsentScreen.cs`, `Services/ConsentService.cs`, `backend/src/consent.js`, `mailer.js`, [docs/consent.md](docs/consent.md) |
| Parent login and email change *(added)* | Keep the parent area for parents; recover a lost email | **Completed.** Login code by email (and SMS if switched on). The email can be changed from the login screen, and the code goes to the new address | `UI/Screens/LoginScreen.cs`, `ChangeEmailScreen.cs`, [docs/login.md](docs/login.md) |
| Voice coach: saying NO out loud *(added)* | Practise a loud, firm NO with instant feedback | **Completed.** Loud, steady and short, 0 to 3 stars and one tip. No recording, no speech recognition | `Scripts/Voice/`, `UI/VoicePanel.cs`, [docs/voice-coach.md](docs/voice-coach.md) |
| Memory Quest and Bubble Guard games *(added)* | Recall instead of re-reading, and keep children coming back | **Completed** | `Lessons/ReviewLesson.cs`, `Lessons/BubbleGuardGame.cs`, [docs/bubble-guard.md](docs/bubble-guard.md) |
| Kids' menu, cartoons, music and letter size *(added)* | Make the app easy to find your way in, and inviting | **Completed.** Menu hub, round fonts, cartoon animals, generated music and pop sounds, three letter sizes, read-aloud | `UI/Screens/MenuScreen.cs`, `UI/Decor.cs`, `Managers/AudioManager.cs`, `UI/TextSize.cs`, `Voice/ReadAloud.cs`, [docs/menu-and-sound.md](docs/menu-and-sound.md) |
| Keza's look (six skin tones) | Children see someone like themselves | **Completed** | `AR/SkinToneApplier.cs`, `Shaders/SkinTone.shader`, [docs/skin-tones.md](docs/skin-tones.md) |
| Optional anonymous progress sync | Totals for the evaluation | **Completed, off by default.** Only a grown-up can switch it on | `Services/CloudSyncService.cs`, `backend/src/app.js` |
| Recorded Kinyarwanda narration | Children who cannot read yet | **Partial.** The phone's own text-to-speech reads English. Kinyarwanda voices are missing on most phones | `Voice/ReadAloud.cs`, [docs/assets.md](docs/assets.md) |
| Expert content review and pilot with children | Validate the content and measure learning | **Not yet done.** Planned after the ethics approval | [docs/deployment-plan.md](docs/deployment-plan.md) |

Paths in the Evidence column are under `Assets/SafeTouch/Scripts/` unless they start with `backend/` or `docs/`.

**Scope changes and why**

- **Scanning the floor is now optional.**
  - Problem: finding a floor first was the slowest and least reliable step for young children. ARCore needs a textured, well-lit surface, and children waited on a grid before anything happened.
  - Now: in **Camera** mode, the default, Keza appears in the camera picture at once. She settles onto the real floor once ARCore finds it in the background.
  - The original scan-and-place flow is still there as **ScanFloor** mode, so FR1 is kept.
  - **Playground** mode (3D, no camera) lets every lesson run on phones without ARCore, which also covers NFR2.
- **Parental consent and login were added.** The ethics review needs informed consent from a parent, and a record of what the parent agreed to. That required the backend.
- **Voice, games and the kids' menu were added** to give young children more reasons to come back, and a simpler way to find their way around without reading much.

## 2. Technical requirements

| Part | Version used | Notes |
|---|---|---|
| Unity Editor | **6000.0.58f1** (Unity 6 LTS) | Android Build Support with OpenJDK, Android SDK and NDK |
| AR Foundation | 6.0.3 | Plane detection, raycasts, anchors, XR Simulation |
| Google ARCore XR Plugin | 6.0.3 | Camera mode and ScanFloor mode |
| Input System | 1.11.2 | Touch, pinch, Android back key |
| uGUI | 2.0.0 | Every screen is built in code (`UIFactory`) |
| Render pipeline | Built-in | Lightest option for low-end phones |
| Node.js | 20 or newer (22 LTS in CI) | The API |
| Express / MongoDB driver | 4.21 / 6.21 | API framework and database |
| MongoDB Atlas | M0 free tier | Consents and optional progress |
| Hosting | Render (free plan) | Deploys automatically from branch `audrey` |
| Email | Brevo or Resend (`MAIL_PROVIDER`; `console` prints emails in development) | Consent and login codes |

**Hardware**

- **Laptop:** Windows 10/11 or macOS 13+, 8 GB RAM (16 GB recommended), and about 25 GB free disk.
- **Phone:** Android 8.0+ on ARM64.
  - For camera mode, the phone must be on the [ARCore list](https://developers.google.com/ar/devices), with *Google Play Services for AR* installed.
  - The phone needs a microphone for the voice coach. It is optional: a NO button always works.

## 3. Install and run

### 3.1 Just try the app (no Unity needed)

1. Copy `SafeTouch.apk` (in the submission ZIP) to an Android 8.0+ phone.
2. Open it and allow installing from this source when Android asks.
3. On an ARCore phone, install **Google Play Services for AR** from the Play Store, so Keza can appear in the camera picture. Without it, the app uses the 3D playground.

### 3.2 Set up the development environment

1. **Install Unity Hub** from <https://unity.com/download> and sign in with a free Unity Personal licence.
2. In Unity Hub > **Installs** > **Install Editor**, choose **6000.0.58f1** (Unity 6 LTS) and tick these modules:
   * **Android Build Support**, with **OpenJDK** and **Android SDK & NDK Tools** underneath
   * **Visual Studio Code** or **Visual Studio Community**
3. **Install Git** (<https://git-scm.com/downloads>) and, for the API, **Node.js 22 LTS** (<https://nodejs.org>).
4. **Prepare the phone:**
   * Turn on Developer options (Settings > About phone > tap *Build number* 7 times).
   * Then turn on **USB debugging**.

### 3.3 Open, build and run the Unity project

```bash
git clone https://github.com/audreym101/SafeTouch.git
cd SafeTouch
git checkout audrey
```

1. Unity Hub > **Projects** > **Add** > **Add project from disk** > choose the `SafeTouch` folder.
   * The first open imports the packages in `Packages/manifest.json`, which takes a few minutes.
   * If Unity asks to enable the new Input System backends and restart, click **Yes**.
2. Run **SafeTouch > 1. Configure Android AR Settings**. It sets:
   * the app id `com.alu.safetouch`
   * minimum API 26, IL2CPP ARM64, OpenGL ES 3, portrait
   * the **Google ARCore** loader
3. **File > Build Profiles** > **Android** > **Switch Platform**.
4. *(Recommended)* Run **SafeTouch > 4. Build Character Prefabs From Models**. It turns the FBX files in `Assets/SafeTouch/Models` into Keza, the adults and the scenery.
5. Run **SafeTouch > 2. Build SafeTouch Scene**. It creates `Assets/SafeTouch/Scenes/SafeTouch.unity` with the AR Session, the XR Origin and every manager, and adds the scene to the build.
6. **Run it:**
   * **In the Editor:** press **Play**. Without a phone, Keza appears on the 3D playground. For phone screen sizes, use **Window > General > Device Simulator** (a Samsung Galaxy A70 profile is in `Assets/SafeTouch/Devices`).
   * **On the phone:** connect it by USB, then **File > Build Profiles > Android > Build And Run**.
7. **Choose how Keza is shown:** select **ARManager** and set **View Mode** to Camera (the default), Playground or ScanFloor. See [docs/3d-mode.md](docs/3d-mode.md).

### 3.4 Run the API

```bash
cd backend
npm install
npm test          # 71 tests
npm start         # http://localhost:3000; in-memory store, emails printed to the console
```

**To use MongoDB and real email:** copy `.env.example` to `.env.local` and set these values:
- `MONGODB_URI`
- `MAIL_PROVIDER`, `MAIL_API_KEY` and `MAIL_FROM`

The live API runs on Render from branch `audrey`; [docs/mongodb.md](docs/mongodb.md) explains the setup. In Unity, set **Base Url** on the **Consent Service** and **Cloud Sync Service** components to the Render address.

## 4. How it works

![Architecture](docs/designs/system/architecture.png)

- **Unity app (C#):**
  - `GameManager` runs the flow (consent, age, menu, lessons).
  - `UIManager` shows one `UIScreen` at a time.
  - `ARManager` places Keza in the camera picture, on the playground or on a scanned floor.
  - Each lesson is a `LessonBase`.
  - Content (scenarios, terms, strings, games) is JSON in `Resources/Content`, so experts can correct the wording without touching code.
- **API (Node.js + Express):**
  - `consent.js` handles the consent and login flows.
  - `validate.js` checks every request.
  - `mailer.js` sends the emails.
  - `store.js` keeps the data in MongoDB Atlas, or in memory for tests.
- **How the two talk (frontend ↔ backend):**
  1. The parent fills in the consent form.
  2. `ConsentService` posts it as JSON, with the full terms the parent read, to `POST /api/consent`.
  3. The API validates it and creates a 6-digit code. It stores only a SHA-256 hash of the code.
  4. The API emails the code and a copy of the consent.
  5. The app sends the code to `POST /api/consent/verify`.
  - Offline, the consent waits on the phone and is sent when the API can be reached.
  - Login (`/api/consent/login`) and the email change (`/api/consent/email`) work the same way.

| Feature | Technical logic |
|---|---|
| Keza in the camera picture | A ray from the viewport point (0.5, 0.3) finds the floor ARCore already knows. If there is none, the floor is assumed 1.1 m below the phone. Keza settles onto the real floor once, and moves again only if the height changes by more than 4 cm, so she never drifts |
| Bigger / smaller | In camera mode Keza steps closer or further (0.6 to 5 m) instead of scaling, so the 0.3 / 0.6 / 0.9 m bubble zones stay true distances |
| Bubble zones | The distance from Keza to each visitor on the floor plane is compared with the three zone radii (`ARManager.ZoneOf`) |
| Voice coach | 1. Microphone frames are measured for loudness. 2. A pitch is found for each frame with YIN. 3. A segmenter finds the word against the room's noise level. 4. `NoCoach` gives 0 to 3 stars. Audio is never saved |
| Progress | One `Progress` object is serialised to JSON in `PlayerPrefs`; lessons unlock in order |
| Language | `Localization` loads `strings_en.json` or `strings_rw.json` and falls back to English for a missing key |

## 5. Testing and results

| Strategy | Why it was chosen | How it was done | Results | What changed afterwards |
|---|---|---|---|---|
| **Unit testing** (Unity Test Framework, EditMode) | The voice analysis is maths a person cannot check by watching the screen | 46 NUnit test cases feed **synthetic voices** with a known answer (tones, harmonics, noise, silence, at 16 and 44.1 kHz) to `PitchDetector`, `UtteranceSegmenter` and `NoCoach`; localisation tests check every Kinyarwanda string | **46 / 46 pass** in 0.8 s (Unity 6000.0.58f1, batch mode) | A rising "no?" now gets its own tip, and every voice rule has a test that keeps it working |
| **Integration / API testing** (`node:test`) | The consent flow touches validation, storage, email and security together | 71 tests start the real Express app on a random port with an in-memory store and call it over HTTP. They cover the happy path and the error cases: wrong code, too many tries, expired code, rate limits, bad input, HTML in names, withdrawal. GitHub Actions runs them on every push (`.github/workflows/backend.yml`) | **71 / 71 pass** | Unity's `JsonUtility` sends `[]` and `""` for empty values, which the API refused; validation now accepts them, with a test. The email change sent the code to the old address; fixed, with tests |
| **Compatibility testing** (screen sizes) | Children use whatever phone or tablet the family has | **SafeTouch > 7. Render Screens On Phones And Tablets** renders every screen on 5 devices: Samsung A70 (20:9), a 16:9 phone, a 720p phone, a 4:3 tablet and a 16:10 tablet. It reports text that overflows or overlaps | **0 layout problems** on all 5 ([docs/designs/devices](docs/designs/devices)) | The renders showed the size buttons on every screen and scenery hidden in portrait; both were fixed |
| **Functional and device testing** (on a phone) | AR, the camera and the microphone only behave truly on real hardware | The APK was installed on the developer's Android phone and every lesson played in camera mode; the Editor runs the playground fallback | Shown in the walkthrough video | Three bugs found on the phone were fixed. Keza drifted out of view as ARCore refined the floor: she now settles once, and **Bring Keza back** appears. The email-change code went to the old address. A parent had to log in again after a quick trip to the mail app: there is now a 5-minute grace |

**Different inputs and how the system responds** (each case is covered by an automated test or shown in the video)

| Input | Response |
|---|---|
| Wrong consent or login code | "That code is not right." and the number of tries left; after 5 tries a new code is needed (HTTP 429) |
| Code older than 24 h (consent) or 10 min (login) | "This code has expired. Ask for a new code." (HTTP 410) |
| More than one login code a minute, or 10 a day | Refused until the wait is over |
| Invalid email, phone without a country code, a missing agreement | The form shows which field to fix (HTTP 400) |
| Unknown email at login | The same reply as a known one and no email, so nobody can test which emails are registered |
| No internet when consenting | The consent is kept on the phone and sent later; the child can start at once |
| Camera refused, or no ARCore | The app explains, then switches to the 3D playground, so every lesson still works |
| Background noise only, or silence | No pitch is found and no NO is counted |
| A quiet or rising "no?" | One or two stars, and a tip such as "Nearly there! Just a little louder." |
| Android back key, or Back on any screen | Returns to the screen the child came from |

## 6. Performance and compatibility

**How the app is kept light for low-end phones**
- Built-in render pipeline and unlit shaders.
- Cartoons, icons, floor zones and effects are drawn in code, with no large textures.
- Music and sound effects are made in code on a background thread at start-up, so there are no audio files.
- One scene, and every screen is built once at start and then reused.
- The APK is about 40 MB.

**Software environments checked**

| Environment | Version | What was checked |
|---|---|---|
| Unity Editor on Windows 11 | 6000.0.58f1 | Compiles with no errors; EditMode tests; screen renders |
| Android | 8.0 (API 26) minimum, ARM64 | Build settings and the installed APK |
| Node.js, Windows | 24.11 | 71 API tests |
| Node.js, Ubuntu (GitHub Actions) | 22 LTS | 71 API tests on every push |
| Render and MongoDB Atlas | Free plan, M0 | Live consent, login and email-change flows |

**Measuring on phones:** for FPS, use the Unity Profiler over USB (**Development Build** + **Autoconnect Profiler**); for memory, `adb shell dumpsys meminfo com.alu.safetouch`. For each phone, record the FPS, memory and battery use in a lesson. The results for a low-end and a higher-end phone are in the technical report.

## 7. Code quality, OOP and algorithms

**Object-oriented design**

| Principle | Where |
|---|---|
| **Abstraction** | `LessonBase` (abstract `Level`, `OnBegin`; virtual `OnEnd`, `OnTap`) and `UIScreen` (abstract `Name`, `BuildContent`; virtual `OnShow`, `ShowsCamera`) state what a lesson or a screen is, not how |
| **Inheritance** | 6 lessons and games extend `LessonBase`; 24 screens extend `UIScreen` |
| **Polymorphism** | `GameManager` starts any `LessonBase` the same way, and `UIManager` keeps a `List<UIScreen>` and shows any of them by name. On the backend, `MemoryStore` and `MongoStore` have the same methods and are swapped by one setting; email providers are swapped the same way |
| **Encapsulation** | `ProgressManager` is the only class that reads or writes saved progress; `ConsentService` is the only one that talks to the consent API; screen roots have `private set` |
| **Separation of concerns and modularity** | Folders by role (`AR`, `Core`, `Data`, `Lessons`, `Managers`, `Services`, `UI`, `Voice`). The voice analysis is its own assembly (`SafeTouch.Voice.Analysis.asmdef`) with no dependency on the game, so it is tested alone. Content is in JSON, not in code. Editor tools are in `Editor/` and never ship in the app |
| **Reusable components** | `UIFactory` (buttons, labels, rows, cards), `Theme` (colours, sizes, the style guide in code), `Decor` (cartoons), `Motion` (squish, bob), `ChoiceCircles` and `LetterHunt` (shared by several lessons) |

**Algorithms and custom solutions**

| Problem | Solution | Code |
|---|---|---|
| Find the pitch of a child's voice | YIN pitch detection (de Cheveigné and Kawahara, 2002): difference function, cumulative mean normalisation, absolute threshold, parabolic interpolation; 75 to 1000 Hz for children's voices | `Voice/Analysis/PitchDetector.cs` |
| Find where a spoken word starts and ends in any room | A noise floor that follows the room, with separate start and stop thresholds (hysteresis), so one phone works in a quiet bedroom and a busy classroom | `Voice/Analysis/UtteranceSegmenter.cs` |
| Judge *how* NO was said, not which word | Three checks (loud against the room, steady or falling pitch, 1 to 2 syllables), a star each, and one tip for the weakest | `Voice/Analysis/NoCoach.cs` |
| Show Keza without scanning, and stop her drifting | Viewport raycast with a fallback floor height, a single settle onto the detected floor, and a 4 cm threshold | `AR/ARManager.cs` |
| Make Keza bigger without breaking the lesson distances | Move the whole scene along the camera ray instead of scaling it | `ARManager.ChangeSize` |
| Music and sounds with no audio files | Additive synthesis of marimba, chords, beat and shaker into one seamless loop, on a background thread | `Managers/AudioManager.cs` |
| An app icon with no image files | Signed distance fields for the shield, hand and heart, with 4 × 4 supersampling for smooth edges | `Editor/SafeTouchAppIcon.cs` |
| One layout for every screen shape | Scale factor chosen from the 1080 × 1920 design, plus safe-area padding | `UI/ResponsiveCanvas.cs` |
| Safe one-time codes | `crypto.randomInt`, only a SHA-256 hash is stored, `timingSafeEqual` comparison, 5 tries per code, expiry and rate limits | `backend/src/consent.js` |

**Conventions**
- C# follows .NET naming: PascalCase for types and members, camelCase for fields, and one class per file named after it. Every class has a `<summary>` saying what it is for.
- JavaScript uses CommonJS modules, one concern per file.
- Feature notes are in [docs/](docs).
- Commits are small, with messages that say what changed for the user.

## 8. Repository structure

```
SafeTouch/
├── Assets/SafeTouch/
│   ├── Scripts/
│   │   ├── Core/        GameManager (app flow), AppSession, Localization, LessonInfo
│   │   ├── AR/          ARManager (view modes, placement, size), Stage3D (playground), ARTouch, ARFx,
│   │   │                ARCharacter, ARFigure, BodyMap, SkinToneApplier, PlaneVisual, PlacementReticle
│   │   ├── Lessons/     LessonBase + 4 lessons + ReviewLesson (Memory Quest) + BubbleGuardGame,
│   │   │                ChoiceCircles, LetterHunt
│   │   ├── Managers/    ProgressManager, ScenarioManager, AudioManager
│   │   ├── Services/    ConsentService, CloudSyncService, ApiAddress
│   │   ├── Data/        Progress, Scenario, TrustedAdult, Badge, SkinTone, ConsentRecord, ContentLibrary
│   │   ├── UI/          UIManager, UIScreen, UIFactory, Theme, Decor, Motion, TextSize, SizeButtons,
│   │   │                ResponsiveCanvas, VoicePanel; Screens/ (24 screens)
│   │   └── Voice/       MicrophoneInput, VoiceCoach, ReadAloud;
│   │                    Analysis/ (own assembly): PitchDetector, FrameAnalyzer, UtteranceSegmenter, NoCoach
│   ├── Editor/          SafeTouch menu: project setup, scene builder, character prefabs, screen renders, app icon
│   ├── Tests/EditMode/  Unity unit tests (voice analysis, localisation)
│   ├── Resources/       Content/ (JSON lessons, terms, strings in EN and RW), Fonts/
│   ├── Models/          FBX models (Keza, adults, scenery)
│   ├── Shaders/         Skin Tone shader
│   └── Icons/           App icon layers
├── backend/
│   ├── src/             app, consent, validate, mailer, sms, store, server
│   └── test/            71 API tests
├── docs/                Feature notes, designs, device renders, prototype, deployment plan
├── Packages/            Unity package list
├── ProjectSettings/     Unity project settings
└── .github/workflows/   API tests on every push
```

## 9. Designs

**Figma:** the Figma prototype is built from [`docs/designs/figma`](docs/designs/figma/README.md): 31 editable screens, 75 wired taps, colour and text styles, the navigation map and the AR placement plan. A one-click plugin builds it in the Figma desktop app.

The browser version ([`docs/prototype/index.html`](docs/prototype/index.html)) shows every screen, the interaction flow, where objects are placed in the room, and the style guide. The screenshots below come from it and show the design stage. **The screens of the current app**, rendered from Unity on five devices, are in [docs/designs/devices](docs/designs/devices) (for example the [Samsung A70](docs/designs/devices/samsung-a70.png)).

### Interaction flow

| | | | |
|---|---|---|---|
| ![Splash](docs/designs/screens/01-splash.png) | ![Age group](docs/designs/screens/02-age-group.png) | ![Lesson map](docs/designs/screens/03-lesson-map.png) | ![Find the floor](docs/designs/screens/04-find-the-floor.png) |
| Splash and language | Age group only | Lesson map (hub) | Find the floor (FR1) |
| ![Bubble zones](docs/designs/screens/05-bubble-zones.png) | ![Stranger](docs/designs/screens/07-bubble-stranger.png) | ![Correct](docs/designs/screens/08-feedback-correct.png) | ![Complete](docs/designs/screens/09-lesson-complete.png) |
| Bubble zones (FR3) | Someone new comes close | Feedback | Lesson complete |
| ![Scenario](docs/designs/screens/10-scenario-card.png) | ![Try again](docs/designs/screens/11-scenario-try-again.png) | ![STOP](docs/designs/screens/12-stop-say-no.png) | ![Protect](docs/designs/screens/13-stop-protect.png) |
| Scenario card (FR4) | Gentle correction (FR5) | STOP: Say no (FR6) | STOP: Protect yourself |
| ![Pick adults](docs/designs/screens/14-pick-trusted-adults.png) | ![Adults AR](docs/designs/screens/15-trusted-adults-ar.png) | ![Badges](docs/designs/screens/16-badges.png) | ![Gate](docs/designs/screens/17-grown-up-check.png) |
| Pick trusted adults (FR7) | Helping hand in AR | Badges (FR8) | Grown-up check |
| ![Talk](docs/designs/screens/18-parent-talk.png) | ![Help](docs/designs/screens/19-parent-get-help.png) | ![Settings](docs/designs/screens/20-parent-settings.png) | ![Zone info](docs/designs/screens/06-bubble-zone-info.png) |
| Parent guides (FR9) | Rwandan helplines | Language, sync, reset | Zone explanation |

**Ages 9 to 11, move buttons and the Memory Quest**

| | | | |
|---|---|---|---|
| ![Move buttons](docs/designs/screens/07-bubble-stranger.png) | ![Online](docs/designs/screens/21-bubble-online-9-11.png) | ![Keep telling](docs/designs/screens/22-stop-keep-telling-9-11.png) | ![Match](docs/designs/screens/23-memory-match.png) |
| Move buttons and zone label | My bubble online (9 to 11) | Keep telling (9 to 11) | Memory Quest: match the pairs |
| ![A match](docs/designs/screens/24-memory-a-match.png) | ![Build STOP](docs/designs/screens/25-build-stop.png) | ![Blocks](docs/designs/screens/26-stop-blocks-flying.png) | ![Helpers](docs/designs/screens/27-helping-hand.png) |
| A match and its reminder | Build STOP in AR | Letters fly above Keza | Helpers come back |
| ![Pick a place](docs/designs/screens/28-pick-a-place.png) | ![Chase](docs/designs/screens/29-safe-places-chase.png) | ![Stars](docs/designs/screens/30-chase-stars.png) | ![Certificate](docs/designs/screens/31-certificate.png) |
| Pick home, school or playground | Timed chase | Time and stars | I remember! certificate |

### Navigation map

![Navigation map](docs/designs/navigation-map.png)

### Virtual object placement

| Top view | Side view |
|---|---|
| ![Top view](docs/designs/ar-placement-top.png) | ![Side view](docs/designs/ar-placement-side.png) |

### Style guide

![Style guide](docs/designs/style-guide.png)

### System design (from the proposal)

| Architecture | Class diagram |
|---|---|
| ![Architecture](docs/designs/system/architecture.png) | ![Class diagram](docs/designs/system/class-diagram.png) |
| **Data model (ERD)** | **Use cases** |
| ![ERD](docs/designs/system/ERD.png) | ![Use cases](docs/designs/system/use-cases.png) |

8-week timeline: [docs/designs/system/timeline.png](docs/designs/system/timeline.png)


## 10. Assets, hardware and deployment

- **Assets** ([docs/assets.md](docs/assets.md)), with the source, licence and reason for each:
  - Keza and the adults: Mixamo / Quaternius CC0 models.
  - Fonts: Andika and Sniglet (SIL Open Font License, in [docs/fonts](docs/fonts)).
  - Generated in code: the music, sound effects, cartoons, floor zones and app icon.
  - Content: JSON, ready for expert review.
- **Hardware** ([docs/hardware.md](docs/hardware.md)):
  - The camera and motion sensors place Keza in the room.
  - The touch screen drives every choice.
  - The microphone is opened only while Keza listens for the child's NO.
  - The laptop runs the whole flow in the Unity Editor.
- **Deployment** ([docs/deployment-plan.md](docs/deployment-plan.md)):
  - **App:** a signed APK is installed by hand for the evaluation sessions in Kigali; Google Play internal testing comes after the capstone, under the Families policy.
  - **API:** deployed automatically from branch `audrey` to Render, with MongoDB Atlas ([docs/mongodb.md](docs/mongodb.md)).

## 11. Privacy and child safety

- **What the app keeps about the child:** an age band, an optional first name or nickname for the guide, and the roles the child picks (never names of people). Everything stays on the phone. No photo, location or voice is kept.
- **What the parent gives:** name, phone, email and country, with their agreement, only to verify the consent (Law N° 058/2021, Article 9). These are kept apart from the child's progress and deleted when the consent is withdrawn ([docs/consent.md](docs/consent.md)).
- **The microphone** opens only while Keza listens for a NO. Loudness, pitch and length are measured and then forgotten; nothing is recorded or sent, and no speech recognition runs ([docs/voice-coach.md](docs/voice-coach.md)).
- **The camera** is used only to show Keza in the room. Its picture is never saved or sent. The app explains why before asking, and if the camera is refused, the app uses the 3D playground.
- **Anonymous sync** is off by default, and only a grown-up can switch it on. Switching it off deletes the server copy.
- **The content:** scenario wording is non-graphic, and every answer ends by pointing the child to a trusted adult. All content is reviewed by child-protection experts before any child uses the app.
- **What it is not:** SafeTouch is an educational tool. It does not diagnose, investigate or replace child-protection services.

---

*Capstone project, African Leadership University. Supervisor: Tunde Isiaq Gbadamosi.*
