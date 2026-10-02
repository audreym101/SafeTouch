# Video demo script (about 9 to 10 minutes)

The brief asks for 5 to 10 minutes that **demonstrate the functions**, with little research background. This script spends under a minute on context and the rest on the running app.

**Setup before recording**

* Phone charged, brightness up, Do Not Disturb on, Google Play Services for AR installed.
* Progress reset (Grown-ups > Settings > Delete progress on this phone) so the demo starts fresh.
* A clear, well-lit floor with some texture (a rug or tiles; plain white floors are hard for ARCore).
* Phone mirrored to the laptop with **scrcpy**, recorded in **OBS** with two sources: the laptop screen (Unity or the mirrored phone) and your webcam or a second phone filming you holding the device, so viewers see the AR happening in the room.
* Microphone check. Speak slowly; you can re-record any section.

---

| Time | What is on screen | What to say (say it in your own words) |
|---|---|---|
| **0:00 – 0:30** | Title slide or the app's splash screen | "Hi, I'm Audrey Munezero. This is the initial demo of SafeTouch, an Android AR app that teaches children aged 6 to 11 in Rwanda how to recognise unsafe situations and get help. I'll show the development setup, then every function of the current build." |
| **0:30 – 1:40** | Unity Hub, then the Unity editor with the project open | Show the Unity version (Unity 6 LTS). Open **Window > Package Manager** and point at AR Foundation and Google ARCore XR Plugin. Open **Project Settings > XR Plug-in Management**: ARCore on the Android tab, XR Simulation on the desktop tab. Open **Build Profiles**: Android is the active platform. Say: "I chose Unity with AR Foundation because it gives plane detection and anchors on ARCore phones, which most families here can access, instead of a headset." |
| **1:40 – 2:30** | The **SafeTouch** menu, then the Project window and the Hierarchy | Click **SafeTouch > 2. Build SafeTouch Scene** (or show it already built). In the Hierarchy, show AR Session, XR Origin with the plane, raycast and anchor managers, and **SafeTouch App** with GameManager, ARManager, AudioManager, ScenarioManager, ProgressManager. Say: "These are the classes from my class diagram. The lesson content is JSON in Resources, so experts can correct wording without changing code." Open `scenarios.json` for two seconds. |
| **2:30 – 3:10** | Mirrored phone: splash, language, age, lesson map | Launch SafeTouch. Switch English and Kinyarwanda to show the Start button change, then back to English. Pick an age group and point out: "It only asks the age group, never the child's name." On the lesson map, show that lessons 2 to 4 are locked until the one before is finished. |
| **3:10 – 4:30** | **Lesson 1, My Space Bubble** (webcam view of you moving the phone, then the mirrored phone) | Move the phone slowly until dots and the reticle appear, tap **Place Keza here**. Walk a step around Keza to show she stays anchored. Tap each zone. Let Grandma, the classmate and "someone new" walk in; choose the right answer for the first two and, for the stranger, first choose **Let them come closer** to show the gentle "Let's think again" feedback, then **Say "Stop!"**. Show the lesson complete card and the new badge. |
| **4:30 – 5:30** | **Lesson 2, Safe or Unsafe?** | Answer three or four cards: one correct, one wrong, to show that each answer gets an explanation and the right answer. Say: "Every answer is saved on the phone for my pre and post evaluation." Skip ahead through the rest if time is short. |
| **5:30 – 6:30** | **Lesson 3, STOP!** | Go through S, T, O, P. On **S**, tap **NO!** and say "No!" out loud. On **T**, a teacher appears. On **P**, Keza walks to the safe place. |
| **6:30 – 7:10** | **Lesson 4, My Trusted Adults** | Pick four or five roles (Mum or Dad, Teacher, IZU volunteer, Nurse or doctor). Tap **Put them around Keza**; they appear around her in the room. Choose who Keza tells first. |
| **7:10 – 8:10** | **Lesson 5, Memory Quest** (switch the age group to 9 to 11 first if you want the six-pair deck) | Match two or three pairs and read one reminder aloud. Build STOP: pick the steps in order and show the letters landing above Keza. Pick a helper. Choose **School**, then show all three ways to win a chase: tap **NO!**, tap the person on screen, and walk Keza to the teacher with the arrow buttons. Let one chase "catch" Keza to show the gentle explanation. Finish on the certificate. Say: "The end game makes the child recall each idea instead of re-reading it, and the timer turns saying NO quickly into a skill they practise." |
| **8:10 – 8:30** | Show the **move buttons** in lesson 1 | Select a visitor with **Move:** and walk them into Keza's zones; the label changes from green to yellow to red. |
| **8:30 – 8:50** | **My badges**, then close and reopen the app | Show stars and badges. Swipe the app away, reopen it: progress is still there (local storage). |
| **8:50 – 9:20** | **Grown-ups** | Show the grown-up check (a sum in words). Open **Talk**, **Signs** and **Get help** (Child Helpline 116, Isange One Stop Centre, RIB, Police). In **Settings**, show that anonymous sync is off by default. Optional: in a terminal, run `npm test` in `backend/` to show the API tests passing. |
| **9:20 – 9:50** | The prototype page and the README | "The prototype, asset list, hardware notes and deployment plan are in the repo. Next: replace the placeholder models with the Blender and Mixamo versions, record Kinyarwanda narration, and run the expert review in week 7. Thank you." |

**If something fails while recording**, stop and re-record that section only; you can join the clips in OBS or any editor. If ARCore can't find the floor, add texture (a patterned cloth) and more light.

**Upload:** YouTube (Unlisted) or Google Drive (anyone with the link can view). Paste the link into the README under **Video demo**.
