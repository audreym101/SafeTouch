# Voice coach: practising a strong "NO"

SafeTouch teaches children to say NO. The STOP lesson, the Personal Space Bubble lesson, the Memory Quest chase and the Bubble Guard game now ask the child to **say** it out loud rather than only tap a button, and the app coaches **how** it was said.

This follows Behavioural Skills Training (instruction, modelling, **rehearsal, feedback**). The child rehearses the response with their voice and gets feedback straight after each try.

## What the child sees

1. **Warm-up (once per session):** "Say *Hello Keza!* in your everyday voice." This measures the child's normal talking pitch.
2. **Get ready… / Say NO now!** The app listens to the room for about 0.3 s, then invites the NO.
3. A **loudness meter** fills while the child speaks. It turns green past the dark line that marks a strong voice for this room.
4. A **pitch line** draws one dot per 32 ms. The muted line is the child's talking voice. Dots above the orange line (more than 6 semitones higher) turn red.
5. A **result**: 0–3 stars, three checks (**Loud**, **Steady**, **Short**: *yes* / *not yet*), and one tip.
6. **Try again** (up to 3 tries), **Continue**, or **Tap NO instead**.

A child who can't or won't speak always has the tap option. A try that was heard never gets zero stars.

## What is measured

| Check | Measure | Strong NO | Why |
|---|---|---|---|
| **Loud** | Mean level of the word compared with the room's noise floor | ≥ 13 dB above the room (heard at all: ≥ 7 dB) | Measured against the room, not a fixed number, so it works in a quiet bedroom, a noisy classroom and on any phone. |
| **Steady** | Pitch contour: end pitch vs start pitch, in semitones | Flat or falling (rise ≤ 1.5 st) | A rising "No?" sounds like a question or a plea; a falling "NO." sounds certain. |
| | Median pitch vs the child's talking pitch | ≤ 6 st above it | Fear and screaming push pitch up. A low, firm voice is the goal. |
| **Short** | Duration and syllable count | 0.15–1.3 s, 1–2 syllables | "NO!" or "OYA!" is short and sharp; "Nooooo…" or a sentence is not. |

The child also gets tips, without losing a star, for:
- **Whisper:** less than 30% of frames have a clear pitch.
- **Shout:** more than 10% of frames clip the microphone. The child is told a strong voice is enough and they don't need to scream.

Until the warm-up, the talking pitch defaults to the typical value for the age group: about **290 Hz at 6–8** and **260 Hz at 9–11** (Lee, Potamianos & Narayanan, 1999).

Thresholds live in `Scripts/Voice/Analysis/NoCoachSettings.cs` so the Week 7 expert review can tune them.

## How it works

```
Microphone (16 kHz, 1 s loop) ─► FrameAnalyzer ─► UtteranceSegmenter ─► NoCoach ─► NoRating
   MicrophoneInput.cs             64 ms frames,      noise floor,            stars, checks, tip
                                  32 ms hop:         word start/end,
                                  level, pitch       Utterance summary
                                  (YIN), peak
```

- **Pitch** uses the YIN algorithm (de Cheveigné & Kawahara, 2002): difference function → cumulative-mean normalisation → first dip under 0.15 → parabolic interpolation. It covers 75–1000 Hz, which spans children's speech and shouting.
- **Word detection:** a word starts two frames 8 dB above the noise floor and ends after 0.25 s within 5 dB of it. Clicks under 0.12 s are ignored, and answers are cut at 2.5 s.
- **Syllables** are counted from dips of 6 dB or more in the loudness envelope.

The analysis code (`Scripts/Voice/Analysis`) is plain C# in its own assembly with **33 EditMode tests** (`Tests/EditMode`). The tests use synthetic voices (glides with harmonics, room noise, whispers, clipped shouts), so no recordings of children are needed.

Run them with **Window → General → Test Runner → EditMode → Run All**, or:

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Privacy (NFR1, Law N° 058/2021)

- The microphone is **open only while Keza is listening** and closes as soon as a result is shown, a lesson ends or the app is paused.
- Audio sits in a **one-second loop that is constantly overwritten**. Nothing is recorded, saved to storage or sent anywhere.
- **No speech recognition runs.** SafeTouch judges *how* NO was said, not *which* word, so the analysis needs no cloud service, and "OYA" (Kinyarwanda) works the same as "NO".
- The talking-pitch value from the warm-up is kept **in memory for the session only**.
- Progress stores only a **count** of three-star NOs (for the *Strong Voice* badge). This count is not sent by cloud sync.
- Parents can switch voice practice off in **Grown-ups → Settings**. The lessons then use the NO button.
- Android asks for microphone permission the first time a lesson listens. If it is refused, the lessons fall back to buttons.

## Limits

- The coach cannot tell "NO" from any other short word. A child could say "yes" loudly and pass the check. The lesson text, the adult supervising (NFR4) and the expert review cover this. Recognising the word on the device would need an offline keyword model, which is noted as future work.
- The thresholds come from the literature and synthetic tests, not yet from children in Kigali. The Week 7–8 sessions should note whether they feel fair, especially in noisy classrooms.
- Very young or shy children may stay quiet. The tap path keeps the lesson moving, and the tips never blame the child.

## A child should not have to shout

The thresholds are low enough for an ordinary firm voice. Measured with synthetic voices in a quiet room (about -59 dBFS), a classroom (-43 dBFS) and a noisy classroom (-35 dBFS):

| Voice | Quiet room | Classroom | Noisy classroom |
|---|---|---|---|
| Soft | 3 stars | heard, 2 stars ("a little louder") | not heard |
| Normal | 3 stars | 3 stars | heard, 2 stars |
| Firm | 3 stars | 3 stars | 3 stars |

Before this change a normal voice in the classroom got "a little louder", and in the noisy classroom it was not heard at all, so children shouted. The lessons now ask for a **clear** NO "like you mean it", not a loud one.

**Noise is not a NO.** A clap (under 0.12 s) is ignored. A scrape or rustle has no pitch, so Bubble Guard does not count it as an answer either: it keeps listening for the real NO. A whisper still gets feedback in the single-try lessons.
