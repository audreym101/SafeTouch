# Voice coach: practising a strong "NO"

SafeTouch teaches children to say NO. The STOP lesson, the Personal Space Bubble lesson, the Memory Quest chase and the Bubble Guard game now ask the child to **say** it out loud rather than only tap a button, and the app coaches **how** it was said.

This follows Behavioural Skills Training (instruction, modelling, **rehearsal, feedback**). The child rehearses the response with their voice and gets feedback straight after each try.

The child can also practise **any time** on the **My NO** screen (the red microphone tile on the menu), hear their own NOs played back, and see what they have achieved.

## Asking to use the microphone

The first time the app listens (the warm-up, a lesson's "Say NO!", Bubble Guard or My NO), a card appears **before** Android's own question:

- A big microphone picture, so a child who can't read yet knows what it is about, and a **Read to me** button.
- **"Can Keza hear your NO?"** Keza wants to listen so she can say how strong the NO is. The NO is saved on this phone so the child and their grown-up can hear how strong they are getting, and it is never sent anywhere. The child is asked to check with their grown-up, then tap **Yes**. The phone asks too.
- **Yes, use the microphone** opens Android's question. **Not now** keeps the NO buttons for the rest of this visit. On My NO, **Use my voice** asks again.
- If Android was told **Don't ask again**, the card explains how a grown-up can allow it, and its button opens SafeTouch's page in the phone's settings.
- If the parent has switched off keeping recordings, the card says nothing is saved.

Code: `GameManager.ExplainMicrophone`, `UIManager.Ask`, `VoiceCoach.Begin`, `MicrophoneInput.Blocked` / `OpenAppSettings`.

## My NO: practice, achievements and recordings

`UI/Screens/MyNoScreen.cs`

1. **Practise your NO:** **Say NO now!** opens the same coach card as the lessons (NO power meter, pitch line, stars, checks, tip, up to three tries), as often as the child likes.
2. **My record, 3-star NOs, NOs said:** three big numbers.
3. **How my NO has grown:** a bar for each of the last 12 NOs, as tall as its NO power, with the PASS line across. The record is gold, NOs past the line are green. Under it: *"My first NO: 32. My best NO: 78. That's 46 stronger!"*
4. **What I have achieved:** eight goals, each ticked when reached, or showing how close the child is ("3 of 10"):

    | Goal | Reached when |
    |---|---|
    | First NO | the first spoken NO |
    | Past the line | a NO reaches the PASS line |
    | Three stars | a NO that is loud, steady and short |
    | NO power 50 | the record reaches 50 |
    | Strong Voice | three 3-star NOs (also the *Strong Voice* badge) |
    | Ten NOs | ten spoken NOs |
    | Growing stronger | the best NO is 15 more than the first |
    | Mighty NO | the record reaches 80 |

5. **Hear my NOs:** the kept recordings, newest first, each with a big play button, its NO power, stars, when and where it was said (Practice, or the lesson's name). The best is gold.

Parents see the same numbers, the goals reached, and the **first, best and latest** NO to play in **Grown-ups → My child**, with **Delete the NO recordings**.

## What the child sees in a lesson

1. **Warm-up (once per session):** "Say *Hello Keza!* in your everyday voice." This measures the child's normal talking pitch.
2. **Get ready… / Say NO now!** The app listens to the room for about 0.3 s, then invites the NO.
3. The **NO power meter** rises while the child speaks, with a big number from 0 to 100 above it.
    - The **PASS** line marks a strong, firm voice for this room. The bar turns green past it, and gold past the child's record.
    - A **white mark** stays at the loudest point of this try.
    - A **gold mark** shows the child's **record**, their loudest NO ever ("Your record: 85"). A NO louder than the record is cheered: **New record!**, with a sound.
    - NO power is the loudness above the room's own noise: 30 dB above the room fills the bar, and the PASS line sits at the coach's strong voice (13 dB, about 43).
4. A **pitch line** draws one dot per 32 ms. The muted line is the child's talking voice. Dots above the orange line (more than 6 semitones higher) turn red.
5. A **result**: 0–3 stars, three checks (**Loud**, **Steady**, **Short**: *yes* / *not yet*), and one tip.
6. **Try again** (up to 3 tries), **Continue**, or **Tap NO instead**. A NO that reaches the PASS line with two stars or more offers **Continue** first ("Strong NO! You pass!"). A quieter one offers **Try again** first ("Louder, to reach PASS!"), with Continue beside it. After three tries Continue is the only button, so no child is ever stuck.

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

The analysis code (`Scripts/Voice/Analysis`) is plain C# in its own assembly with EditMode tests (`Tests/EditMode`). The tests use synthetic voices (glides with harmonics, room noise, whispers, clipped shouts), so no recordings of children are needed. `NoRecordingTests` covers the ring, the WAV files, which recordings the journal keeps, and the achievements.

Run them with **Window → General → Test Runner → EditMode → Run All**, or:

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Privacy (NFR1, Law N° 058/2021)

- The microphone is **open only while Keza is listening** and closes as soon as a result is shown, a lesson ends, the child leaves My NO or the app is paused.
- Audio sits in a **one-second loop that is constantly overwritten**, and the coach keeps the last **4 seconds** in a ring (`SampleRing`), also overwritten as it goes.
- **Only the NO itself is kept.** When a spoken NO is heard, just that word (with 0.3 s either side) is cut from the ring and saved as a small WAV in the app's private storage on the phone (`NoRecorder`, about 32 KB a second). Talking before or after it, whispers, noises and the "Hello Keza!" warm-up are never kept.
- **Only a few recordings are kept:** the child's first NO, their best NO and the 8 newest. Older ones are deleted as new ones come (`NoJournal`). The numbers of the latest 60 NOs are kept for the growth chart, plus totals.
- **Nothing is ever sent.** The recordings and the NO journal live outside `Progress`, so cloud sync can't send them.
- Parents can **switch recording off** in **Grown-ups → Settings → Keeping NO recordings** (the recordings are deleted and only the numbers are kept), **delete the recordings** in **Grown-ups → My child**, or switch voice practice off altogether. A reset or a withdrawn consent deletes everything.
- These are in the consent terms, **version 1.6**, so every parent agrees again before recordings are kept.
- **No speech recognition runs.** SafeTouch judges *how* NO was said, not *which* word, so the analysis needs no cloud service, and "OYA" (Kinyarwanda) works the same as "NO".
- The talking-pitch value from the warm-up is kept **in memory for the session only**.
- Progress also stores a **count** of three-star NOs (for the *Strong Voice* badge) and the child's **record** (a number from 0 to 100, terms version 1.5). Cloud sync never sends either.
- Before Android's microphone question, Keza's card explains why the app wants the microphone and what it keeps (above). If either is refused, the lessons fall back to buttons.

Before the pilot, the ethics committee should confirm it is content with recordings of the child's NO being kept on the family's phone. Recording can be switched off for the whole study by setting `Progress.keepNoRecordings` to `false` by default.

## Limits

- The coach cannot tell "NO" from any other short word. A child could say "yes" loudly and pass the check. The lesson text, the adult supervising (NFR4) and the expert review cover this. Recognising the word on the device would need an offline keyword model, which is noted as future work.
- The thresholds come from the literature and synthetic tests, not yet from children in Kigali. The Week 7–8 sessions should note whether they feel fair, especially in noisy classrooms.
- Very young or shy children may stay quiet. The tap path keeps the lesson moving, and the tips never blame the child.

## A child should not have to shout

Passing still needs only a firm voice: the PASS line is the coach's strong voice, well below a shout. The record above it is a game for children who want to go further, and a NO screamed into the phone still gets the tip "You don't need to scream".

The thresholds are low enough for an ordinary firm voice. Measured with synthetic voices in a quiet room (about -59 dBFS), a classroom (-43 dBFS) and a noisy classroom (-35 dBFS):

| Voice | Quiet room | Classroom | Noisy classroom |
|---|---|---|---|
| Soft | 3 stars | heard, 2 stars ("a little louder") | not heard |
| Normal | 3 stars | 3 stars | heard, 2 stars |
| Firm | 3 stars | 3 stars | 3 stars |

Before this change a normal voice in the classroom got "a little louder", and in the noisy classroom it was not heard at all, so children shouted. The lessons now ask for a **clear** NO "like you mean it", not a loud one.

**Noise is not a NO.** A clap (under 0.12 s) is ignored. A scrape or rustle has no pitch, so Bubble Guard does not count it as an answer either: it keeps listening for the real NO. A whisper still gets feedback in the single-try lessons.
