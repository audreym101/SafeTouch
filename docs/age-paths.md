# Two lesson paths: Explorer (6-8) and Guardian (9-11)

The home screen has a tab for each path. Each path keeps its **own finished lessons, stars, badges, chase time and Bubble Guard best**, so a 6-8 child's progress and a 9-11 child's progress never mix.

| | Explorer, ages 6-8 | Guardian, ages 9-11 |
|---|---|---|
| Lesson 1 (AR bubble) | 4 steps | 5 steps: the 5th is **my bubble online** |
| "Think deeper" notes | none | after the zone, STOP and trusted-adult steps |
| Lesson 2 (STOP) | ends after the letter hunt | adds **keep telling**: what to do when the first adult doesn't help |
| Lesson 3 (scenario baskets) | 7 cards | 13 cards |
| Memory Quest | the basic pairs | extra pairs (online strangers, surprises) |
| Tab colour | blue | green |

The AR scenes are the same room, Keza and the zones; the Guardian path adds steps on top, and the lessons already read the path in play (`Older` in `LessonBase`).

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
