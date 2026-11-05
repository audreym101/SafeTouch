# Scenario stories: watch it happen, then act it out

In lesson 2, **Safe or Unsafe?**, every card starts as a short 3D story with Keza instead of only words. The child watches the situation happen, sorts the card, and then, for the unsafe and uncomfortable cards, chooses what Keza does next and watches the safe choice play out.

## How a card goes

1. **The story plays.** People walk in and talk in speech bubbles (read aloud when read-aloud is on). The card shows **Skip**, which jumps straight to the question.
2. **The question.** The three colours appear on the floor as before (Safe, Uncomfortable, Unsafe), and the child moves Keza onto one. The card shows **Watch again**, which replays the story. This answer is the one saved for the evaluation, exactly as before.
3. **Feedback**, as before.
4. **Act it out** (unsafe and uncomfortable cards only). *What should Keza do now?* Two answers lie on the floor as circles: the safe one and a tempting one.
    - The tempting one gets a gentle "Let's think again" note, and the child tries again.
    - The safe one plays out. For the car story, the child first says NO out loud with the voice coach (or taps NO), then Keza runs to Mum and the car drives away.
    - Act-it-out choices are practice: they are not saved.

## The stories

| Card | What happens | What Keza does (the safe choice) |
|---|---|---|
| S01 A hug from Grandma | Grandma comes and asks for a hug; Keza is happy to | (safe card: no act-out) |
| S02 Too much tickling | A cousin keeps tickling after Keza says stop | Says STOP loudly; the cousin steps back |
| S03 A touching secret | A neighbour by the fence asks for a touching secret | Says NO and tells Mum |
| S04 At the health centre | A nurse checks Keza's tummy, with Mum right there | (safe card) |
| S05 Sweets from someone new | A car pulls up, a stranger gets out with sweets: "Come with me" | Says NO out loud, runs to Mum; the car drives away |
| S06 Standing too close | A classmate stands too close at school and won't move | Steps back and asks the teacher, who helps |
| S07 High-five from your teacher | The teacher asks for a high-five | (safe card) |
| S08 A message online (9-11) | A big phone shows messages asking for photos and secrets | Doesn't reply, shows Mum |
| S09 Gifts with a catch (9-11) | A family friend gives a present, then asks for a secret | Gives it back and tells Mum |
| S10 Sitting on a lap (9-11) | A grown-up on a bench says "sit on my lap" | Says no thank you and sits by Mum |
| S11 Pictures on a phone (9-11) | An older cousin wants Keza to look at phone pictures (the screen shows nothing) | Walks away and tells Mum |
| S12 A friend's worry (9-11) | A friend shares a worry and asks Keza not to tell | Tells the teacher, together with the friend |
| S13 A kiss hello (9-11) | An uncle insists on a kiss hello | Offers a wave or a high-five instead |

Nothing in the stories is graphic. The words match the cards, which the content experts review.

## Code and content

- [ScenarioStories.cs](../Assets/SafeTouch/Scripts/Lessons/ScenarioStories.cs) holds the stories and the act-outs.
  - The props are built from simple shapes, like the playground: a car, a road, a bench, a fence, a school board, a health-centre sign, phones, sweets and a gift.
  - The people are the same figures as in the other lessons.
  - Everything is placed in metres from Keza, in front of the phone's view, so it works in the 3D world and in camera mode.
- [ScenarioLesson.cs](../Assets/SafeTouch/Scripts/Lessons/ScenarioLesson.cs) runs a card: story, question, feedback, act-out.
- The words are `story.*` keys.
    - The English is in the code.
    - The Kinyarwanda is in `Resources/Content/strings_rw.json`. It is a **first draft** that a native speaker must check.
- The car's engine and horn are generated in [AudioManager.cs](../Assets/SafeTouch/Scripts/Managers/AudioManager.cs) (`car`, `horn`), like the other sounds.
