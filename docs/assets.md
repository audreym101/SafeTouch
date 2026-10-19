# SafeTouch asset list

Every asset below exists to serve one of the four learning goals: recognising personal boundaries, classifying situations as safe, uncomfortable or unsafe, rehearsing the STOP response, and naming trusted adults. Assets that don't serve a goal are left out; this keeps the app small (target APK under 150 MB) and the scenes calm.

**Status key:** *In build* = already in this repository and working. *Placeholder* = a simple stand-in is in the build and the final asset replaces it in week 6. *To source* = not in the build yet.

## 3D assets

| Asset | Status | Source and licence | Why SafeTouch needs it |
|---|---|---|---|
| **Keza, the guide character** | In build: the Mixamo child in `Models/` (Ch46, "Standing Greeting"), made into a prefab by `SafeTouch > 4. Build Character Prefabs From Models`; the capsule figure is the fallback | Final: low-poly child modelled in Blender from a CC0 base mesh (Quaternius "Ultimate Modular Characters"), with Rwandan skin tone, hair and school uniform | Children learn body-safety skills better by watching and guiding a peer than by being told (Bandura's social learning theory, used in the proposal). A child-sized character lets the lesson show a body without ever showing a real child. Low-poly keeps it friendly and cartoon-like (NFR5). |
| **Keza's animations**: idle, wave, walk, "stop" hand, happy jump | Wave in build (the model's greeting clip plays on `Bounce`); walk is still procedural in `ARFigure.cs` | Mixamo (free, royalty-free with an Adobe account), retargeted in Unity | The STOP lesson is rehearsal. Seeing Keza raise a hand and step away models the physical response the child is learning, which text alone can't do. |
| **Visitor and adult figures** (Grandma, classmate, someone new, teacher, nurse, IZU volunteer, police officer, parent) | In build: **Mum and Dad** from `Models/js-parents-and-dog` play every parent role and share the skin tone the child picks for Keza; two adults split from the Eve model and the simple figure play the other visitors; each name always gets the same person and skin tone | Quaternius CC0 characters with simple role cues: a book for the teacher, a cap for the police officer, a vest for the IZU volunteer | Lesson 1 needs people at different social distances; lesson 4 needs recognisable helpers from the child's real community, including Rwanda-specific ones (Inshuti z'Umuryango volunteers, Isange staff). Roles, not faces, so no real person is depicted. |
| **Personal space zones** (red, yellow, green floor discs) | In build (procedural discs, transparent material) | Generated in code | The core visual of FR3. Placing the zones on the real floor shows distance at true scale (0.3, 0.6, 0.9 m), which a flat picture cannot. Upgrade in week 6: a soft-edged unlit shader with a gentle pulse when a visitor enters a zone. |
| **Placement reticle and plane visual** | In build | AR Foundation default plane components, custom material | Children need to see where Keza will land before they tap. The reticle makes plane detection (FR1) visible and prevents "nothing happened" confusion. |
| **Safe place marker** | Placeholder (green disc) | Final: small house from Kenney "City Kit" (CC0) | The P step of STOP ("Protect yourself, go to a safe place") needs a destination the child can watch Keza walk to. |
| **STOP letter blocks** (S, T, O, P) | In build (coloured cubes with a 3D text label, `ARManager.SpawnSign()`) | Final: rounded blocks modelled in Blender, Andika letters | Memory Quest round 2: the child rebuilds the word STOP above Keza, so recall becomes something they can see in the room. |
| **Place scenery**: sofa (home), chalkboard (school), tree (playground) | In build: school desk and A-frame chalkboard (school), playground and a merged grass patch (playground), and the family dog at home, all scaled into `Resources/Props`; the sofa is still a primitive | Final: Kenney "Furniture Kit" and "Nature Kit" (CC0) | Memory Quest round 4: shows which place the child chose, with the right helpers nearby, so the chase practice matches real places in their life. |
| **The chaser figure** | Placeholder (grey figure prefab with a tap collider) | Final: the same neutral adult model, never scary | Memory Quest chase: a calm, non-threatening person to practise saying NO to, which keeps the game within NFR5 (not frightening). |

| **Skin tone shader and skin mask** | In build (`Shaders/SkinTone.shader`; mask generated from Keza's texture) | Written for SafeTouch | Lets the child give Keza any of six skin tones while her clothes, hair and eyes stay the same (see `docs/skin-tones.md`). |
| **Models not used** | Kept in `Models/` but left out of the app | `character (1).fbx` (a cigar-smoking caricature), `character.fbx` (the same girl as Keza without animation) and `the-mill` | Not suitable for a children's safety app, or not needed by any lesson. |

## 2D and interface assets

| Asset | Status | Source and licence | Why SafeTouch needs it |
|---|---|---|---|
| **Andika typeface** | Supported in code; drop `Andika-Regular.ttf` into `Resources/Fonts` | SIL International, SIL Open Font Licence | Designed for beginning readers, with the letter shapes children are taught (single-storey a and g), and full support for Kinyarwanda. Ages 6 to 8 are still learning to read. |
| **Rounded button and card shapes** | In build (generated sprite in `UIFactory.Rounded`) | Generated in code | Large, soft shapes read as safe and tappable. Generating them keeps the build free of texture assets. |
| **Icons**: back, listen again, move arrows, badge, phone, home, school, playground | To source | Google Material Symbols (Apache 2.0) | Icons help children who can't yet read every word. |
| **Badge artwork** (6 badges, including Quick and Safe) | To source | Designed in Figma, exported as PNG sprites | Rewards keep attention across a 20-minute session (engagement is one of the evaluation metrics). |
| **Scenario card illustrations** (13) | To source | Drawn in Figma: simple, non-graphic scenes reviewed by the child-protection expert | Younger children rely on pictures as much as text. Illustrations must show situations, never bodies or harm. |
| **Trusted adult icons** (8) | To source | Figma, matching the 3D role cues | Used on the picker screen so non-readers can choose. |

## Audio assets

| Asset | Status | Source and licence | Why SafeTouch needs it |
|---|---|---|---|
| **Narration, English and Kinyarwanda** (about 60 short clips) | Hooks in code (`LessonBase.Narrate`, `Scenario.audioClipName`) | Recorded with a voice actor (20,000 RWF in the research budget) | Many 6 to 8 year olds can't read the cards alone. Narration in Kinyarwanda makes the app usable in homes and schools where English is weak (FR10). |
| **Sound effects**: tap, correct, try again, badge, place | In build (soft generated tones in `AudioManager.cs`) | Final: Kenney "Interface Sounds" (CC0) | Immediate audio feedback confirms a tap and marks a right answer. "Try again" is a gentle falling tone, never a buzzer (NFR5). |

## Content assets

| Asset | Status | File | Why SafeTouch needs it |
|---|---|---|---|
| **13 scenario cards** (7 for ages 6 to 8, plus 6 deeper cards and "Think deeper" notes for 9 to 11) | In build, draft for expert review | `Resources/Content/scenarios.json` | FR4 and FR5. Kept as data so the week 7 expert review changes text, not code. |
| **8 trusted adult roles** | In build | `Resources/Content/trusted_adults.json` | FR7. Roles instead of names keep the app free of personal data (NFR1). |
| **Memory Quest pairs** (6 word and meaning pairs) | In build, draft for expert review | `Resources/Content/review.json` | The end game's match round: recalling the ideas, rather than re-reading them, helps children remember them. |
| **8 badges** (including Strong Voice and Bubble Guard) | In build | `Resources/Content/badges.json` | Progress and motivation (FR8). |
| **Parent guides, warning signs, Rwandan helplines** | In build, numbers to re-confirm with NCDA and RIB before the pilot | `Resources/Content/parent_resources.json` | FR9. Connects the app to the existing child-protection system (Child Helpline 116, Isange One Stop Centre 3029, RIB 166, Police 112). |
| **Bubble Guard requests** (14 safe and unsafe things visitors ask) | In build, draft for expert review | `Resources/Content/guard_requests.json` | The game's content, kept as data so experts can change it without code. |
| **Interface text, English and Kinyarwanda** | English complete; Kinyarwanda started | `Resources/Content/strings_en.json`, `strings_rw.json` | FR10. Missing Kinyarwanda keys fall back to English. |

## Tools used to make assets

Blender 4 (models), Mixamo (animation), Figma (UI, badges, card art), Audacity (narration editing), Unity 6 (import, materials, prefabs).
