# SafeTouch in Figma

This folder turns the SafeTouch prototype into a real, clickable Figma prototype. You can do it the fast way with the included plugin, or by hand.

| File | What it is |
|---|---|
| `plugin/manifest.json`, `plugin/code.js` | A one-click Figma plugin that builds the whole prototype |
| `screens-svg/*.svg` | The 31 app screens as editable vectors: every button, card and label is its own layer with real text |
| `navmap.svg` | Navigation map |
| `topdown.svg`, `sideview.svg` | Virtual object placement plan (top and side views, in metres) |

## Option A: build it with the plugin (about 2 minutes)

You need the free **Figma desktop app** (development plugins don't run in the browser): <https://www.figma.com/downloads/>.

1. Open the Figma desktop app and create a new design file. Name it **SafeTouch Prototype**.
2. Menu (Figma logo, top left) > **Plugins** > **Development** > **Import plugin from manifest...**
3. Pick `docs/designs/figma/plugin/manifest.json` from the unzipped repo.
4. Menu > **Plugins** > **Development** > **SafeTouch Prototype Importer**.

The plugin adds a page called **SafeTouch prototype** with:

* the 31 screens as frames, each with a title, laid out in flow order;
* **75 taps wired**: every button that moves to another screen, plus every back button;
* two flow starting points, **Child flow** (from 01) and **Grown-ups flow** (from 17);
* colour styles (`SafeTouch/Primary`, `SafeTouch/Safe`, ...) and text styles (`SafeTouch/Display`, `Title`, `Body`, `Label`) in Andika;
* the navigation map and the AR placement plan.

When it finishes, a message at the bottom says how many taps were wired. If it lists any as "Not wired", add those by hand using the table below.

## Option B: build it by hand (about 30 minutes)

1. Drag all 31 files from `screens-svg/` onto the canvas. Each becomes an editable frame. Rename each frame to its file name.
2. If text shows in a different font, install **Andika** (Google Fonts). Figma has it built in when you are online.
3. Switch to the **Prototype** tab (right panel). For each row in the table below, select the button layer (its name starts with the text in the **Tap** column), drag the blue `+` handle onto the target frame, and choose **On tap > Navigate to**, animation **Dissolve**, 250 ms.
4. Select frame 01 and click **+ Flow starting point**; name it *Child flow*. Do the same for frame 17, *Grown-ups flow*.
5. Drag in `navmap.svg`, `topdown.svg` and `sideview.svg` next to the screens.

## Present and share

1. Prototype tab > **Device**: choose an Android phone.
2. Click **Present** (the play icon, top right) and tap through.
3. **Share** > *Anyone with the link* > *can view* > **Copy link**, and paste it into the main README under **Designs** and into your submission.

## Every wired tap

The plugin wires these, and Option B uses the same list. Some screens stand in for a whole lesson, so a tap may jump ahead (for example, Green zone goes to "Someone new comes close", skipping the Grandma and classmate steps that the app itself plays).

| From screen | Tap | Goes to |
|---|---|---|
| 01 Splash and language | Start | 02 Age group |
| 02 Age group | 6 - 8 | 03 Lesson map (hub) |
| 02 Age group | 9 - 11 | 03 Lesson map (hub) |
| 02 Age group | ‹ back | 01 Splash and language |
| 03 Lesson map (hub) | Lesson card 1. | 04 Find the floor (FR1) |
| 03 Lesson map (hub) | Lesson card 2. | 10 Scenario card (FR4) |
| 03 Lesson map (hub) | Lesson card 3. | 12 STOP: Say no (FR6) |
| 03 Lesson map (hub) | Lesson card 4. | 14 Pick trusted adults (FR7) |
| 03 Lesson map (hub) | Lesson card 5. | 23 Memory Quest: match the pairs |
| 03 Lesson map (hub) | My badges | 16 My badges (FR8) |
| 03 Lesson map (hub) | Grown-ups | 17 Grown-up check |
| 03 Lesson map (hub) | ‹ back | 01 Splash and language |
| 04 Find the floor (FR1) | Place Keza | 05 Bubble zones (FR3) |
| 04 Find the floor (FR1) | ‹ back | 03 Lesson map (hub) |
| 05 Bubble zones (FR3) | Red zone | 06 Zone explanation |
| 05 Bubble zones (FR3) | Yellow zone | 06 Zone explanation |
| 05 Bubble zones (FR3) | Green zone | 07 Someone new comes close |
| 05 Bubble zones (FR3) | ‹ back | 03 Lesson map (hub) |
| 06 Zone explanation | Continue | 05 Bubble zones (FR3) |
| 07 Someone new comes close | Say | 08 Feedback: correct |
| 07 Someone new comes close | Let them | 11 Feedback: try again (FR5) |
| 07 Someone new comes close | ‹ back | 03 Lesson map (hub) |
| 08 Feedback: correct | Continue | 21 Ages 9-11: my bubble online |
| 21 Ages 9-11: my bubble online | Stop chatting | 09 Lesson complete |
| 21 Ages 9-11: my bubble online | ‹ back | 03 Lesson map (hub) |
| 22 Ages 9-11: keep telling | Tell another | 09 Lesson complete |
| 22 Ages 9-11: keep telling | ‹ back | 03 Lesson map (hub) |
| 23 Memory Quest: match the pairs | Any two cards | 24 Memory Quest: a match |
| 23 Memory Quest: match the pairs | ‹ back | 03 Lesson map (hub) |
| 24 Memory Quest: a match | Any two cards | 25 Memory Quest: build STOP (AR) |
| 24 Memory Quest: a match | ‹ back | 03 Lesson map (hub) |
| 25 Memory Quest: build STOP (AR) | Say no | 26 STOP letters fly above Keza |
| 25 Memory Quest: build STOP (AR) | ‹ back | 03 Lesson map (hub) |
| 26 STOP letters fly above Keza | Open up | 27 Memory Quest: helping hand |
| 26 STOP letters fly above Keza | ‹ back | 03 Lesson map (hub) |
| 27 Memory Quest: helping hand | Mum or Dad | 28 Memory Quest: pick a place |
| 27 Memory Quest: helping hand | Teacher | 28 Memory Quest: pick a place |
| 28 Memory Quest: pick a place | Home | 29 Safe places chase (timed) |
| 28 Memory Quest: pick a place | School | 29 Safe places chase (timed) |
| 28 Memory Quest: pick a place | Playground | 29 Safe places chase (timed) |
| 28 Memory Quest: pick a place | ‹ back | 03 Lesson map (hub) |
| 29 Safe places chase (timed) | NO! | 30 Chase result: time and stars |
| 29 Safe places chase (timed) | ‹ back | 03 Lesson map (hub) |
| 30 Chase result: time and stars | Continue | 31 I remember! certificate |
| 27 Memory Quest: helping hand | ‹ back | 03 Lesson map (hub) |
| 31 I remember! certificate | Collect my badge | 09 Lesson complete |
| 09 Lesson complete | Continue | 03 Lesson map (hub) |
| 10 Scenario card (FR4) | Safe | 08 Feedback: correct |
| 10 Scenario card (FR4) | Uncomfortable | 11 Feedback: try again (FR5) |
| 10 Scenario card (FR4) | Unsafe | 11 Feedback: try again (FR5) |
| 10 Scenario card (FR4) | ‹ back | 03 Lesson map (hub) |
| 11 Feedback: try again (FR5) | Continue | 12 STOP: Say no (FR6) |
| 12 STOP: Say no (FR6) | NO! | 13 STOP: Protect yourself |
| 12 STOP: Say no (FR6) | ‹ back | 03 Lesson map (hub) |
| 13 STOP: Protect yourself | To a safe place | 22 Ages 9-11: keep telling |
| 13 STOP: Protect yourself | ‹ back | 03 Lesson map (hub) |
| 14 Pick trusted adults (FR7) | Put them | 15 Helping hand in AR |
| 14 Pick trusted adults (FR7) | ‹ back | 03 Lesson map (hub) |
| 15 Helping hand in AR | Mum or Dad | 09 Lesson complete |
| 15 Helping hand in AR | Teacher | 09 Lesson complete |
| 15 Helping hand in AR | ‹ back | 03 Lesson map (hub) |
| 16 My badges (FR8) | ‹ back | 03 Lesson map (hub) |
| 17 Grown-up check | 14 | 18 Parents: Talk (FR9) |
| 17 Grown-up check | 17 | 03 Lesson map (hub) |
| 17 Grown-up check | 15 | 03 Lesson map (hub) |
| 17 Grown-up check | ‹ back | 03 Lesson map (hub) |
| 18 Parents: Talk (FR9) | Tab Get help | 19 Parents: Get help |
| 18 Parents: Talk (FR9) | Tab Settings | 20 Parents: Settings (FR10) |
| 18 Parents: Talk (FR9) | ‹ back | 03 Lesson map (hub) |
| 19 Parents: Get help | Tab Talk | 18 Parents: Talk (FR9) |
| 19 Parents: Get help | Tab Settings | 20 Parents: Settings (FR10) |
| 19 Parents: Get help | ‹ back | 03 Lesson map (hub) |
| 20 Parents: Settings (FR10) | Tab Talk | 18 Parents: Talk (FR9) |
| 20 Parents: Settings (FR10) | Tab Get help | 19 Parents: Get help |
| 20 Parents: Settings (FR10) | ‹ back | 03 Lesson map (hub) |
