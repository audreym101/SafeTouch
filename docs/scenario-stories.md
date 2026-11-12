# Scenario stories: watch it happen, then act it out

In lesson 2, **Safe or Unsafe?**, every card starts as a short 3D story with Keza instead of only words. The child watches the situation happen, sorts the card, and then, for the unsafe and uncomfortable cards, chooses what Keza does next and watches the safe choice play out.

## How a card goes

1. **The story plays.** People walk in and talk in speech bubbles (read aloud when read-aloud is on). The card shows **Skip**, which jumps straight to the question.
2. **The question.** The three colours appear on the floor as before (Safe, Uncomfortable, Unsafe), and the child moves Keza onto one. The card shows **Watch again**, which replays the story. This answer is the one saved for the evaluation, exactly as before.
3. **Feedback**, as before.
4. **How does Keza feel?** The child taps *Happy*, *Uh-oh, uneasy* or *Scared*, and Keza shows the feeling. Safe touch feels good; an "uh-oh" feeling inside is the body's warning, the first sign that something is not right. A happy answer to an unsafe story gets a gentle note: some tricks, like sweets or presents, feel nice at first. This is practice and is not saved.
5. **Act it out** (unsafe and uncomfortable cards only). *What should Keza do now?* Two answers lie on the floor as circles: the safe one and a tempting one.
    - The tempting one gets a gentle "Let's think again" note, and the child tries again.
    - The safe one plays out. For the car story, the child first says NO out loud with the voice coach (or taps NO).
    - **The child gets Keza to safety.** Whenever Keza runs to Mum or goes to tell her, a green ring glows under Mum and the child drags Keza there, or taps Mum. After 10 seconds Keza goes by herself, so no child is ever stuck. In the car story, the stranger drives away as Keza runs.
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
| S14 Late after school *(night)* | It is dark; Keza walks out of the school gate alone. A car pulls up and the driver says "Your mum sent me. Get in!" | Says NO out loud and runs back to the teacher at the gate; the car drives away. The feedback teaches a family password: no password, no ride |
| S15 Lost after dark *(night)* | Keza can't find Mum at the market as night falls; a fruit seller with her own child phones Mum, who comes | (safe card: asking a mum with children or a shopkeeper, and staying where people can see you) |
| S16 Someone following you *(night, 9-11)* | Walking home in the dark, a stranger keeps following Keza | Goes into the bright shop and tells the shopkeeper; the stranger walks away |
| S17 The coach asks first *(9-11)* | At sports practice the coach asks "May I move your hands to show the throw?", with the whole team there | (safe card: asking first, for a reason, in the open) |
| S18 A video call at night *(night, 9-11)* | Late at night, a phone shows messages from someone met in a game: "Turn on your camera. Don't tell your parents!" | Doesn't reply, tells Mum |
| S19 A friend asks first *(9-11)* | After a race, a friend asks "Can I give you a hug?" and Keza says yes | (safe card: consent between friends, both ways) |

**Two decks.** S01 to S07, S14 and S15 are the ages 6 to 8 deck (`level` 1). S08 to S13 and S16 to S19 are the ages 9 to 11 deck (`level` 2). No card is in both, so a child who moves up meets new situations that teach the same ideas in more detail.

## Ages 9 to 11: Spot the Red Flag

The older deck is played differently:
- **While the story plays**, the card shows **Red flag!** The child taps it the moment something feels wrong. Too early, and they're told to keep watching. Right after the warning sign (the uh-oh moment), a **red flag** goes up over the person, the phone or the car.
- **After sorting**, instead of "how does Keza feel?", the child answers **What's the trick?** The choices are a secret, a gift, a stranger, pressure, or no trick (each card's `trick` in `scenarios.json`). A short explanation follows, then the act-out.
- Both are practice and are not saved. Only the sorting answer is saved for the evaluation, as before.

**Day and night.** Each card plays at its own time of day: day stories in daylight, stories after dark at night, whatever the child picked with the moon button. The button is hidden while a card plays and comes back after the lesson.

Nothing in the stories is graphic. The words match the cards, which the content experts review.

## Stories after dark

Cards S14, S15, S16 and S18 have `"night": true` in `scenarios.json`. While their story, question and act-out play, the 3D world turns to night (see [3d-mode.md](3d-mode.md)): a deep blue sky with the moon and stars, street lamps with warm pools of light, a lit shop, a market stall with its bulb and car headlights. A soft light always stays on the play area, so the people are easy to see. The day cards always play in daylight, even if the child chose night with the moon button; their choice comes back after the lesson.

## Code and content

- [ScenarioStories.cs](../Assets/SafeTouch/Scripts/Lessons/ScenarioStories.cs) holds the stories and the act-outs.
  - The props are built from simple shapes, like the playground: a car (with headlights at night), a road, a bench, a fence, a school board, a health-centre sign, phones, sweets, a gift, and for the night stories street lamps, a school gate, a market stall and a lit shop.
  - The people are the same figures as in the other lessons. Other children are children: the friend uses the lifelike classmate model (the older girl for 9 to 11) in her own colours, so she never looks like Keza, and the cousins and the classmate are boys like the boy helper.
  - Everything is placed in metres from Keza, in front of the camera's view of the 3D world.
- [ScenarioLesson.cs](../Assets/SafeTouch/Scripts/Lessons/ScenarioLesson.cs) runs a card: story, question, feedback, act-out.
- The words are `story.*` keys.
    - The English is in the code.
    - The Kinyarwanda is in `Resources/Content/strings_rw.json`. It is a **first draft** that a native speaker must check.
- The car's engine and horn are generated in [AudioManager.cs](../Assets/SafeTouch/Scripts/Managers/AudioManager.cs) (`car`, `horn`), like the other sounds.
