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

Ages 6–8 get 8 visitors; ages 9–11 get 10, including requests about online photos, cars and threats ("You'll get in trouble if you tell").

The best score is saved, and 8 or more unlocks the **Bubble Guard** badge. Every three-star NO also counts towards **Strong Voice**.

Requests live in `Resources/Content/guard_requests.json` (`safe`, `text`, `explain`, `level`) for the Week 7 expert review.
