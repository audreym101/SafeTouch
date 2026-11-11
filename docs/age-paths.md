# Two lesson paths: Explorer (6-8) and Guardian (9-11)

The home screen has a tab for each path. Each path keeps its **own finished lessons, stars, badges, chase time and Bubble Guard best**, so a 6-8 child's progress and a 9-11 child's progress never mix.

**Two paths, two sets of modules.** Both paths teach the same four ideas: personal space, safe and unsafe, STOP, and trusted adults. Each path has its own module of every lesson, with its own name, its own content and its own way of playing. A child who started at 6 and is now 9 or older does not repeat what they did: they meet new situations and a new kind of challenge.
- **Explorer (6 to 8)** is short and simple, with few words.
- **Guardian (9 to 11)** goes into more detail ("think deeper" notes, proper names, naming the trick).

| Lesson | Explorer, ages 6-8 | Guardian, ages 9-11 |
|---|---|---|
| 1 | **My Space Bubble.** Tap the red, yellow and green zones to learn them. Grandma and a classmate visit, then someone new comes too close (say STOP, send them back). 4 steps | **My Boundaries.** **Who goes where?** Grandma, someone new and a neighbour who asked for a secret wait outside the bubble, and the child **drags** each one to where they belong (knowing someone isn't enough to come close). Then someone new, and **my bubble online**. 4 steps |
| Body map | gentle words: **private parts** (what underwear covers), **bottom**, **chest** | the proper names: **vulva and vagina** (Keza) or **penis and testicles** (Ganza), **breasts** (Keza), **buttocks and anus** |
| 2 | **Safe or Unsafe?** Its own deck of **9 cards** (2 after dark). Watch the story, move Keza onto a colour, then **how does Keza feel?** (Happy, Uh-oh, Scared) | **Spot the Red Flag.** Its own deck of **10 cards** (2 after dark), none shared with 6-8. While the story plays, tap **Red flag!** the moment something feels wrong, and a red flag goes up over the warning sign. After sorting, **what's the trick?** (a secret, a gift, a stranger, pressure, or no trick) |
| 3 | **STOP!** Collect the letters S, T, O and P around the room; each letter starts its step | **My Speak-Up Plan.** No letters: four **moments** ("Someone says it's just a game", "The teacher asks what happened") and the child picks the STOP step each one needs; then the step, and **keep telling** when the first adult doesn't help |
| 4 | **My Trusted Adults.** Fill the helping hand (2 or more), then who to tell first | **My Safety Network.** At least 3 helpers, **one from outside the family** (school or the community), then **what to say**: the words that get help fastest |
| Memory Quest | the basic pairs | the basic pairs and extra ones (online strangers, surprises) |
| Bubble Guard | 8 visitors from the 6-8 set | up to 12 visitors from **its own set** of 14 (5 safe, 9 unsafe), with more tricks: a fake message from Mum, fake praise, game coins for photos, pressure, being taken somewhere alone |
| The 3D world | bright late-morning light, the cartoon visitors | a warm, low evening sun and a deeper film grade; the visitors and strangers are scanned, real-looking people |
| Tab colour | blue | green |

The lessons read the path in play (`Older` in `LessonBase`) and each module's name comes from `LessonInfo.Title` (keys ending in `.older` for 9 to 11). The scenario decks are `level` 1 and 2 in `scenarios.json`, and the Bubble Guard visitors are `level` 1 and 2 in `guard_requests.json`. Neither path sees the other's cards or visitors.

**Private parts are named, never shown.** The body map puts glowing dots on a clothed character. Tapping a dot shows the part's name (gentle words for 6 to 8, the proper names for 9 to 11); a red dot means private. Then comes **Find it!**: the app names five parts, private ones included, and the child taps each one. Child-protection programmes teach the correct words because a child who knows them can say clearly what happened. The parents' guide (*Use the right names for body parts*) tells parents which words the app uses. The Kinyarwanda words are drafts, and a native speaker and the child-protection expert must agree them before any child sees them.

## Unlocking (why it is locked)

The Guardian lessons talk openly about online strangers and private parts of the body, so they are not for younger children.

- The Explorer path is always open.
- The Guardian path opens **only when a grown-up has confirmed the child's age** ([age-check.md](age-check.md)): not by choosing it, and not by finishing the Explorer lessons. Until then its tab says *locked*, and tapping it opens the age check.
- A child on the Guardian path can still open the Explorer tab.

## How it is stored

`Progress` always holds the path in play (`ageGroup`, finished lessons, scores, badges, bests). The other path's copy waits in `otherPaths` and is swapped back in when the tab is tapped (`Progress.SwitchPath`). The lessons, the badges screen and the certificate need no changes: they only ever see the path in play.

- A save from before the paths existed keeps working: the old results become the path they were on.
- **Delete progress** (Grown-ups > Settings) clears both paths.
- **Anonymous sync** (when a parent switches it on) sends the path in play (`ageBand`, finished lessons).

## Checked

The path rules (opening, switching, keeping each path's results apart, an older save) are exercised by 32 checks run against the real `Progress.cs`. The home screen tabs have been compiled but not yet looked at on a phone.
