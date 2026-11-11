# Bubble Guard (game)

A replayable AR game that opens after **Lesson 3, STOP!**. It sits on the lesson map below the Memory Quest. It is outside the lesson order, so it never affects the "finished every lesson" badge.

## How it plays

1. Keza's three bubble zones appear on the floor.
2. Visitors walk towards her one at a time, from the far side of the room. Each one asks something in a speech bubble: *"High five!"*, *"Let's play a secret touching game."*, *"Come inside, nobody will know."*
3. If it's **not okay**, the child **says NO out loud** (or taps NO!) before the visitor reaches the red zone.
4. If it's **okay**, the child taps **OK** (or lets the friend arrive in the yellow zone).
5. After each answer, a one-line explanation shows, then the next visitor comes. Each visitor walks a little faster (0.16 → 0.34 m/s).

| Situation | Result |
|---|---|
| Unsafe + NO | +1, the visitor walks away. A **three-star spoken NO** earns a **+1 strong-voice bonus**. |
| Unsafe + OK, or reaches the red zone | Keza loses a heart (she has 3). |
| Safe + OK | +1, the visitor waves. |
| Safe + NO | No points, **no heart lost**: *"You can always say no."* |

Ages 6–8 get 8 visitors from their own set; ages 9–11 get up to 12 from a separate set of 14 (5 safe, 9 unsafe), including requests about online photos, cars and threats ("You'll get in trouble if you tell"). The two sets share no visitors, so an older child meets new tricks.

## Seeing the trick

Visitors bring what their trick needs, so the child sees it coming, not only reads it:

| Prop | Visitors |
|---|---|
| Sweets | "Sit on my lap for sweets." |
| Money | "Take this money. Tell nobody." |
| A puppy on a lead | "Help me find my lost puppy!" |
| A car that pulls up behind them | "Get in my car, I'll drive you." and "Your mum sent me to fetch you." |
| A glowing phone | Photos for game coins, a secret video chat |
| A box | "Help me carry this inside. Quick!" |
| A football | The safe ones: playing, and the coach's water break |

After a NO, the trick is named in red over the visitor: **Trick spotted: a bribe!** If the child misses it, the app says what the trick was.

| Trick | Example |
|---|---|
| A secret | "Come with me. Don't tell Mum." |
| A bribe | Sweets or money for something |
| A lure | The lost puppy, a ride |
| A touching game | "Let's play a secret touching game." |
| Getting you alone | "Come inside, nobody will know." "Help me carry this inside." |
| A threat | "You'll get in trouble if you tell." |
| A fake message (9 to 11) | "Your mum sent me to fetch you." The answer: ask for the family's code word. |
| Fake praise (9 to 11) | "You're so grown-up. Our secret, okay?" |
| An online trick (9 to 11) | Photos for game coins, a secret video chat |
| Pressure (9 to 11) | "Everyone does it. It's normal." |

The camera jolts on each NO, and harder when a trick gets through. On ages 9 to 11 the visitors are the scanned people. They cannot move their legs, so they sway from foot to foot as they walk.

The best score is saved, and 8 or more unlocks the **Bubble Guard** badge. Every three-star NO also counts towards **Strong Voice**.

Requests live in `Resources/Content/guard_requests.json` (`safe`, `text`, `explain`, `level`, `prop`, `trick`) for the Week 7 expert review.
