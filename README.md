# SafeTouch

**An Android augmented reality app that teaches children aged 6 to 11 in Rwanda how to recognise personal boundaries, spot unsafe situations and get help from a trusted adult.**

The child places Keza, a friendly guide character, on the real floor through the phone camera and works through four short lessons with her: the personal space bubble, safe-or-unsafe scenario cards, the STOP response (Say no, Tell, Openly explain, Protect yourself) and building a "helping hand" of trusted adults. A final **Memory Quest** brings it all back as a game: a card matching round, building the word STOP in the room, finding their helpers, and a timed chase at home, school or the playground where saying NO wins. Ages 9 to 11 get deeper content throughout. A grown-ups section gives parents and teachers conversation guides, warning signs and Rwandan helplines. The app works offline and stores no name, photo, location or voice.

| | |
|---|---|
| **GitHub repository** | <https://github.com/audreym101/SafeTouch> |
| **Video demo** | https://youtu.be/FnD_9e-jiUc  |
| **Figma prototype** | https://www.figma.com/proto/LGF1ZxkHqCZH1Lswr6zrMm/Untitled?page-id=1%3A1521&node-id=1-2914&p=f&viewport=165%2C-166%2C0.17&t=y4xb4JWYI30hmsgq-1&scaling=min-zoom&content-scaling=fixed&starting-point-node-id=1%3A2890&show-proto-sidebar=1  |
| **Clickable prototype (browser)** | https://www.figma.com/proto/LGF1ZxkHqCZH1Lswr6zrMm/Untitled?page-id=1%3A1521&node-id=1-2914&p=f&viewport=165%2C-166%2C0.17&t=y4xb4JWYI30hmsgq-1&scaling=min-zoom&content-scaling=fixed&starting-point-node-id=1%3A2890&show-proto-sidebar=1 |
| **Platform** | Android 8.0+ (API 26) phones on the ARCore supported list |
| **Built with** | Unity 6 LTS, AR Foundation 6, Google ARCore XR Plugin, C#; optional Node.js + Express + MongoDB Atlas API |

---

## Contents

1. [What works in this build](#1-what-works-in-this-build)
2. [Tools and why they were chosen](#2-tools-and-why-they-were-chosen)
3. [Set up the environment](#3-set-up-the-environment)
4. [Set up and run the project](#4-set-up-and-run-the-project)
5. [Designs](#5-designs)
6. [Assets](#6-assets)
7. [Hardware integration](#7-hardware-integration)
8. [Deployment plan](#8-deployment-plan)
9. [Project structure](#9-project-structure)
10. [Privacy and child safety](#10-privacy-and-child-safety)
11. [Next steps](#11-next-steps)

---

## 1. What works in this build

| Requirement (proposal §3.3) | In this build | Where in the code |
|---|---|---|
| **FR1** Detect a horizontal plane through the camera | Plane detection with a reticle at the centre of the screen; an editor fallback floor for laptop testing | `Scripts/AR/ARManager.cs` |
| **FR2** Place the character on the plane and move it | *Place Keza here*, anchored with `ARAnchor`; *Place again* re-places her. **Move buttons** on every lesson (Move: Keza / a visitor, and four arrows) walk the chosen character 15 cm per tap, relative to the phone; a label says which of Keza's zones a visitor is standing in | `ARManager.Nudge()`, `ARManager.ZoneOf()`, `UI/Screens/LessonScreen.cs` |
| **FR3** Personal space bubble with 3 coloured zones | Red 0.3 m, yellow 0.6 m and green 0.9 m zones on the floor; three visitors walk in and the child decides what Keza does | `Scripts/Lessons/PersonalSpaceBubbleLesson.cs` |
| **FR4** Scenario cards with answer selection | 13 cards (7 for ages 6 to 8, all 13 for 9 to 11, including online contact, grooming with gifts, being shown pictures, a friend's secret and forced kisses); Safe / Uncomfortable / Unsafe | `ScenarioLesson.cs`, `Managers/ScenarioManager.cs`, `Resources/Content/scenarios.json` |
| **FR5** Feedback and explanations | Every answer gets an explanation; wrong answers show the right one gently. Ages 9 to 11 also get a **Think deeper** note (consent, grooming, the three parts of good telling) | `ScenarioLesson.cs`, `UIManager.ShowFeedback()` |
| **FR6** Interactive STOP framework | Four rehearsed steps with Keza, a teacher figure and a safe place in the room; ages 9 to 11 add "keep telling until someone helps". My Space Bubble adds "my bubble online" for 9 to 11 | `StopFrameworkLesson.cs` |
| **FR7** Trusted adults and AR placement | Choose 2 to 8 roles; they appear in an arc around Keza | `TrustedAdultsLesson.cs`, `UI/Screens/AdultsScreen.cs` |
| **FR8** Progress saved locally | Lessons unlock in order; stars, badges, the fastest chase time, the Bubble Guard best score, the count of strong spoken NOs and every scenario answer saved in `PlayerPrefs` as JSON | `Managers/ProgressManager.cs`, `Data/Progress.cs` |
| **Review game** (Memory Quest, lesson 5) | Unlocks after lesson 4. Round 1 match the pairs (4 pairs for 6 to 8, 6 for 9 to 11); round 2 S-T-O-P letter blocks fly above Keza in AR; round 3 the child's own helpers return; round 4 pick home, school or playground, then three timed chases where saying NO!, tapping the person or walking Keza to a helper wins (3 s or less = 3 stars). Ends with an "I remember!" certificate. The *Quick and Safe* badge needs a NO in 3 seconds | `Lessons/ReviewLesson.cs`, `UI/Screens/MemoryGameScreen.cs`, `PlacesScreen.cs`, `CertificateScreen.cs`, `Resources/Content/review.json` |
| **Saying NO out loud** (voice coach) | The STOP lesson's *S - Say no*, the bubble lesson's stranger step, the Memory Quest chase and Bubble Guard listen for the child's spoken NO (or OYA). After a "Hello Keza" warm-up, a loudness meter and a pitch line show the try; the coach gives 0-3 stars on **Loud** (vs the room), **Steady** (pitch flat or falling, not far above the child's talking voice) and **Short** (1-2 syllables), plus one tip, with up to 3 tries. Tapping NO always works. 33 EditMode tests with synthetic voices | `Scripts/Voice/`, `UI/VoicePanel.cs`, `Tests/EditMode/`, [docs/voice-coach.md](docs/voice-coach.md) |
| **Keza's look** (skin tones) | Six tones on *Home > Keza's look*; the change blends and sparkles on Keza in the room. Visitors get a mix of tones. On the Mixamo Keza a generated skin mask and the *Skin Tone* shader change only her skin | `AR/SkinToneApplier.cs`, `UI/Screens/LookScreen.cs`, `Shaders/SkinTone.shader`, [docs/skin-tones.md](docs/skin-tones.md) |
| **Bubble Guard** (game) | Opens after lesson 3. Visitors walk in with a request in a speech bubble; say NO to unsafe ones before they reach the red zone, tap OK for safe ones. Faster each time, 3 hearts, a strong-voice bonus, best score saved | `Lessons/BubbleGuardGame.cs`, `Resources/Content/guard_requests.json`, [docs/bubble-guard.md](docs/bubble-guard.md) |
| **FR9** Parent and teacher resources | Grown-up check, talk guides, warning signs, Rwandan helplines, settings | `UI/Screens/ParentGateScreen.cs`, `ParentScreen.cs` |
| **FR10** English and Kinyarwanda | Language switch; Kinyarwanda strings started, English fallback | `Core/Localization.cs`, `strings_*.json` |
| **NFR1** No personal data | Age band only; random device id; trusted adults stored as roles | `Progress.cs`, `backend/src/validate.js` |
| **NFR2** Android 8.0+ with ARCore | Player settings applied by the setup menu | `Editor/SafeTouchSetup.cs` |
| **NFR4 / NFR5** Child-friendly, not frightening | Large targets, calm colours, no buzzers. The only timer is in the final chase game; being "caught" just explains what to do and replays the chase | `UI/Theme.cs` |
| **NFR6** Easy exit from any screen | Back button top-left on every screen, Android back button returns to the lesson map | `UIScreen.StandardColumn()`, `GameManager.Update()` |

**Models:** Keza (the Mixamo child, with her greeting wave), two adult visitors and the school desk and playground come from `Assets/SafeTouch/Models` once **SafeTouch > 4. Build Character Prefabs From Models** has run; until then the simple shapes are used. Previews are in [docs/designs/models](docs/designs/models).

**Placeholders in this build:** sound effects are generated tones. Narration and card illustrations are listed in [docs/assets.md](docs/assets.md) and arrive in week 6. Lesson text is a draft for the expert review in week 7.

## 2. Tools and why they were chosen

| Tool | Version | Why |
|---|---|---|
| **Unity** | 6 LTS (6000.0.x) | Long-term support release, so APIs stay stable through the capstone; best-documented engine for mobile AR; C# |
| **AR Foundation** | 6.0 | One API for plane detection, raycasts and anchors; includes XR Simulation for testing without a phone |
| **Google ARCore XR Plugin** | 6.0 | ARCore runs on the Android phones families and schools already have, unlike headsets |
| **Input System** | 1.11 | Required by AR Foundation's camera tracking; also handles the Android back button |
| **uGUI** (built from code) | 2.0 | Screens are generated by `UIFactory` so the whole interface is readable and reviewable as C# |
| **Built-in render pipeline** | | Simplest AR camera setup and fastest on low-end phones |
| **Visual Studio Code** + C# Dev Kit | | Editing and debugging C# |
| **Git and GitHub** | | Version control; GitHub Actions runs the API tests |
| **Figma** (plus the HTML prototype in `docs/prototype`) | | Screens, flows, style guide |
| **Blender, Mixamo, Audacity** | | Models, animation, narration (week 6) |
| **Node.js, Express, MongoDB Atlas** | 22, 4, M0 free tier | Optional anonymous progress API (shown as optional in the architecture diagram) |

## 3. Set up the environment

You need a Windows 10/11 or macOS 13+ laptop with 16 GB RAM (8 GB works, slowly) and about 25 GB free disk, plus an ARCore phone for device testing.

1. **Install Unity Hub** from <https://unity.com/download> and sign in with a free Unity Personal licence.
2. In Unity Hub > **Installs** > **Install Editor**, choose **Unity 6 LTS (6000.0.x)** and tick these modules:
   * **Android Build Support**, with **OpenJDK** and **Android SDK & NDK Tools** ticked underneath
   * **Visual Studio Code** or **Visual Studio Community** (Windows)
3. **Install Git**: <https://git-scm.com/downloads>.
4. **Install Visual Studio Code** with the **C# Dev Kit** and **Unity** extensions (skip if you chose Visual Studio).
5. **Prepare the phone:**
   * Check the model at <https://developers.google.com/ar/devices>.
   * Install **Google Play Services for AR** from the Play Store.
   * Turn on Developer options (Settings > About phone > tap *Build number* 7 times), then **USB debugging**.
6. *Optional, for the API:* install **Node.js 22 LTS** from <https://nodejs.org>.

## 4. Set up and run the project

### 4.1 Open the Unity project

```bash
git clone https://github.com/audreym101/SafeTouch.git
```

1. Unity Hub > **Projects** > **Add** > **Add project from disk** > choose the `SafeTouch` folder (the one that contains `Assets` and `Packages`). If Hub asks which editor to use, pick your Unity 6 LTS version.
2. The first open imports the packages listed in `Packages/manifest.json` (AR Foundation, ARCore, XR Plug-in Management, Input System). This takes a few minutes.
3. If Unity asks to **enable the new Input System backends and restart**, click **Yes**.

### 4.2 Configure and build the scene

1. Menu **SafeTouch > 1. Configure Android AR Settings**. This sets the app id `com.alu.safetouch`, minimum API 26, IL2CPP ARM64, OpenGL ES 3, portrait, and turns on the **Google ARCore** loader (Android) and **XR Simulation** (desktop).
2. **File > Build Profiles** (Build Settings in older versions) > **Android** > **Switch Platform**.
3. *(Recommended)* Menu **SafeTouch > 4. Build Character Prefabs From Models**. This turns the FBX files in `Assets/SafeTouch/Models` into Keza (with her skin mask and greeting wave), two adult visitors, and the school and playground scenery. Run it before step 4 so the scene uses them. **SafeTouch > 5. Render Skin Tone Preview** then writes `docs/designs/skin-tones.png` to check the result.
4. Menu **SafeTouch > 2. Build SafeTouch Scene**. This creates `Assets/SafeTouch/Scenes/SafeTouch.unity` with the AR Session, XR Origin (plane, raycast and anchor managers), the managers from the class diagram, the four lessons, materials and prefabs, and adds the scene to the build.
5. Check **Edit > Project Settings > XR Plug-in Management**: **Google ARCore** ticked on the Android tab, **XR Simulation** ticked on the desktop tab. Tick them by hand if the dialog in step 1 asked you to. Under **Google ARCore**, set **Requirement** to **Required**.

### 4.3 Run it

* **On the laptop:** press **Play**. XR Simulation shows a virtual room; hold the right mouse button and use WASD to look around until the floor is found. The UI appears at phone size in the Game view (set the Game view to a 1080 × 1920 portrait resolution).
* **On the phone:** connect it by USB, **File > Build Profiles > Android > Build And Run**. Choose a file name such as `Builds/SafeTouch.apk`.

### 4.4 Optional: the anonymous progress API

```bash
cd backend
npm install
npm test          # 5 tests: save, reject a child name, reject a phone-number id, totals only, withdraw
npm start         # http://localhost:3000, in-memory store unless MONGODB_URI is set
```

Copy `.env.example` to `.env` and add a MongoDB Atlas connection string to use a real database. In Unity, select **SafeTouch App**, and on **Cloud Sync Service** set **Base Url** and tick **Enabled In Build**. Sync still only runs after a parent switches it on in the grown-ups section.

### 4.5 Before recording the demo, check

- [ ] The project opens with no errors in the Console
- [ ] Play mode reaches the lesson map and lesson 1 places Keza
- [ ] The APK installs and finds the floor on the phone
- [ ] Progress survives closing and reopening the app

## 5. Designs

**Figma:** the Figma prototype is built from [`docs/designs/figma`](docs/designs/figma/README.md): 31 editable screens, 75 wired taps, colour and text styles, the navigation map and the AR placement plan. A one-click plugin builds it in the Figma desktop app.

The browser version ([`docs/prototype/index.html`](docs/prototype/index.html)) shows every screen, the interaction flow, where objects are placed in the room, and the style guide. It follows the same steps and wording as the Unity code, and all screenshots below come from it.

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

## 6. Assets

Full list with sources, licences and a justification for each: **[docs/assets.md](docs/assets.md)**. In short: a low-poly guide character and community figures (Blender, Quaternius CC0, Mixamo animations) so lessons show situations without showing real children; floor zones and a reticle generated in code; the Andika typeface for early readers; English and Kinyarwanda narration; content as JSON for expert review.

## 7. Hardware integration

Details: **[docs/hardware.md](docs/hardware.md)**. SafeTouch is handheld AR on an ARCore Android phone: the camera and motion sensors find the floor and keep Keza anchored while the child walks around her, and the touch screen drives every choice. The microphone is never used. On the laptop, AR Foundation's XR Simulation runs the whole flow in the Unity editor; on the phone, Build And Run installs over USB.

## 8. Deployment plan

Details: **[docs/deployment-plan.md](docs/deployment-plan.md)**.

1. **Weeks 1 to 6:** development builds with Build And Run on the researcher's ARCore phone.
2. **Week 7:** expert review of all content; a signed release APK (`0.2.0`) is built.
3. **Weeks 7 to 8:** the APK is installed by hand on 2 to 3 ARCore phones for the evaluation sessions in Kigali; the app runs fully offline and progress is reset between children.
4. **Optional:** the anonymous API is deployed to Render with MongoDB Atlas M0, only if the ethics approval covers it; it stores totals for the evaluation and supports withdrawal.
5. **After the capstone:** Google Play internal testing under the Families policy.

## 9. Project structure

```
SafeTouch/
├── Assets/SafeTouch/
│   ├── Scripts/
│   │   ├── Core/          GameManager (flow), Localization, LessonInfo
│   │   ├── AR/            ARManager (planes, placement, anchors), ARCharacter, ARFigure, Billboard
│   │   ├── Managers/      ProgressManager, ScenarioManager, AudioManager
│   │   ├── Lessons/       LessonBase + the four lessons + ReviewLesson (Memory Quest)
│   │   ├── UI/            UIManager, Theme (style guide), UIFactory, Screens/ (12 screens)
│   │   ├── Data/          Scenario, Progress, TrustedAdult, Badge, ContentLibrary
│   │   └── Services/      CloudSyncService (optional, off by default)
│   ├── Editor/            SafeTouchSetup: the SafeTouch menu (player settings, scene builder)
│   └── Resources/Content/ scenarios, review pairs, trusted adults, badges, parent resources, strings (JSON)
├── Packages/manifest.json Unity packages
├── ProjectSettings/       Unity version (other settings are generated on first open)
├── backend/               Optional Node.js + Express API with tests
├── docs/                  prototype, designs, assets, hardware, deployment plan, video script
└── .github/workflows/     API tests on every push
```

The managers match the class diagram in the proposal: `GameManager`, `UIManager`, `ARManager`, `ScenarioManager`, `ProgressManager`, `AudioManager`, and the data classes `Scenario`, `Progress`, `TrustedAdult`, `Badge`.

## 10. Privacy and child safety

* No account, name, photo, location or voice recording. Only an age band and a random device id (Law N° 058/2021 on personal data, Article 9).
* The microphone opens only while Keza listens for a spoken NO, and closes straight after. Sound sits in a one-second loop that is constantly overwritten: SafeTouch measures loudness, pitch and length, then forgets them. Nothing is recorded, saved or sent, and no speech recognition runs. Parents can switch voice practice off; the lessons then use a NO button. See [docs/voice-coach.md](docs/voice-coach.md).
* The camera feed is never saved or sent.
* Keza's skin tone describes the guide character, not the child, and is not synced.
* Anonymous sync is **off** by default, can only be switched on in the grown-ups section, and switching it off deletes the server copy.
* Scenario wording is non-graphic and every answer ends by pointing the child to a trusted adult; all content is reviewed by child-protection experts before any child uses the app.
* SafeTouch is an educational tool. It does not diagnose, investigate or replace child-protection services.

## 11. Next steps

| Week | Work |
|---|---|
| 5 | Progress tracking polish, badge art |
| 6 | Final models and animations, Kinyarwanda narration, card illustrations, optional API deployment |
| 7 | Expert content review and changes, first tests with children |
| 8 | Evaluation sessions, data analysis, final report and demo |

---

*Capstone project, African Leadership University. Supervisor: Tunde Isiaq Gbadamosi.*
