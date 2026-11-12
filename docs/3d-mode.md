# Keza's 3D world

SafeTouch is a 3D character app. Keza (or Ganza) appears at once in her own 3D world, and every lesson happens there. The phone's camera is never used, nothing has to be scanned, and any Android 8.0+ phone can run it.

## Why a 3D world and not AR

The proposal planned handheld AR (FR1: find a floor through the camera, then place Keza). Testing showed that finding a floor was the slowest and least reliable step for young children: ARCore needs a textured, well-lit surface and an ARCore phone, and children waited on a grid before anything happened. The AR code was therefore removed:
- no camera mode, floor scanning, placement reticle or ARCore; no AR Foundation package;
- no camera permission, and no need for Google Play Services for AR;
- every lesson keeps its **true distances** (the 0.3 / 0.6 / 0.9 m bubble zones), because the 3D world is laid out in metres just as the room was.

## The playground

- Keza stands in a park.
- **+**, **−** and pinch move the 3D camera closer or further away (0.6× to 3.2×). In lessons, + and − are round magnifier buttons among the lesson's tools, so nothing covers the people.
- **A swipe on the grass** swings the camera around Keza, up to a three-quarter view either side. After 8 seconds without a swipe it drifts back to the front.
- [Stage3D.cs](../Assets/SafeTouch/Scripts/AR/Stage3D.cs) builds the park:
  - a real sky with a sun;
  - a grass lawn whose texture is made in code;
  - pine trees and the forest house in its garden;
  - tufts of real grass;
  - people chatting in the distance, and a child, Mum, Dad and two scanned people **walking the paths** behind the lesson, stopping now and then. Those with a skeleton step with their legs; the scanned ones sway from foot to foot. They never come near the play area and can't be tapped.
- Every piece comes from `Resources/Props`, made by **SafeTouch > 9. Build 3D World From Downloaded Models** ([SafeTouchScenery.cs](../Assets/SafeTouch/Editor/SafeTouchScenery.cs)). Any missing piece is drawn with simple shapes, as before.


## The cinematic look

- Soft sun shadows, sky light and haze in the distance.
- A film colour grade with a vignette ([CinematicLook.cs](../Assets/SafeTouch/Scripts/AR/CinematicLook.cs)).
- A camera that breathes a little like a hand-held one, jolts when something startling happens, and moves in on what matters.
- Ages 9 to 11 get a warmer, more dramatic late-afternoon light.
- Stories at school play in a classroom.
- After changing a model in `Models/forest-house`, `Models/school-classroom` or `Models/lowpoly-people`, run **SafeTouch > 9. Build 3D World From Downloaded Models** to rebuild the pieces.

## Day and night

- The **moon button** among the lesson's tools turns the park to night. At night it becomes a **sun button**, which brings the day back.
- At night:
  - the sky turns deep blue, with the moon and stars;
  - the sun becomes a soft, cool moonlight;
  - four street lamps light warm pools on the grass;
  - a soft light always follows the play area, so the people in the lesson are easy to see;
  - the film look lifts the dark a little, with blue shadows and warm lamplight.
- Each story card plays at its own time of day: cards marked `"night"` at night, the others in daylight, whatever the child picked (see [scenario-stories.md](scenario-stories.md)). The moon button hides while a card plays.
- The child's choice lasts until they leave the 3D world. **Keza's look** always opens in daylight, so skin tones and clothes show their true colours.
- Nothing is saved: day or night is not stored anywhere.
- For mid-range phones, at most three lights light pixel by pixel at night; the others are cheaper.

## Moving people

Every person moves like a person, however they are moved: walking to a spot, dragged by a finger, or walked by a lesson.
- **Lifelike children** (Keza, Ganza, the classmate) step with their legs and swing their arms ([WalkCycle.cs](../Assets/SafeTouch/Scripts/AR/WalkCycle.cs)). Their models have a skeleton but no walking clip, so the steps are made in code from how far they really moved, and the feet keep pace with the ground.
- **Scanned people**, who cannot move their legs, sway from foot to foot.
- Everyone turns smoothly towards where they go, and eases in and out of a walk.
- A person follows a dragging finger at a walking or running pace instead of jumping to it.

The child moves people in three ways ([ARTouch.cs](../Assets/SafeTouch/Scripts/AR/ARTouch.cs)):
- **Drag a person** to walk them.
- **Twist two fingers**, like turning a dial, to turn the person last touched (or Keza).
- **Use the move pad.** The move button (four arrows) among the lesson's tools opens round buttons: arrows that walk and turn buttons. Holding an arrow keeps the person walking.
  - The pad acts on the person the child last touched. A ring glows at that person's feet for a moment. If no one is picked, it acts on Keza.
  - People in a story stay where the story needs them.

## Code

- [ARManager.cs](../Assets/SafeTouch/Scripts/AR/ARManager.cs) (the name from the class diagram; it holds no AR code now) puts the guide and each lesson's people in the 3D world.
  - `PlaceGuide`: Keza appears on the playground when the first lesson opens.
  - `SpawnFigure`, `PointAroundCharacter`, `ZoneOf`: people around Keza, in metres, and which bubble zone they stand in.
  - `ChangeSize` and `Orbit`: the camera moves closer, further or around her.
- [Stage3D.cs](../Assets/SafeTouch/Scripts/AR/Stage3D.cs) builds the park, the sky, day and night, and moves the camera.
- [SizeButtons.cs](../Assets/SafeTouch/Scripts/UI/SizeButtons.cs): the + / − buttons and the pinch.
