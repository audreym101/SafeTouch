# Menu, fonts, cartoons and sound

## The menu

After **Start**, and the login if one is needed, the app opens the **menu** ([MenuScreen.cs](../Assets/SafeTouch/Scripts/UI/Screens/MenuScreen.cs)). It has big, coloured buttons, each with a round picture-letter, so a child can find their way without reading much.

| Button | Opens | Grown-up check first? |
|---|---|---|
| Play the lessons | the lesson map | no |
| My badges | badges | no |
| Keza's look | Keza's skin tone | no |
| Settings | music on/off, tap sounds on/off, English / Kinyarwanda | no |
| How to play | five steps with big numbers | no |
| About us | what SafeTouch is for, privacy, version | no (its Contact button: yes) |
| Contact us | email the team, open the parent area, call a free helpline | **yes** |
| Grown-ups | the parent area | **yes** |

Contact us and Grown-ups sit behind the grown-up question because they open the parent's area or another app (email, phone). Google Play's Families policy asks for this.

**Back buttons:**
- The lesson map and the parent area go back to the menu.
- The menu goes back to the Start screen.
- On Android, the back key on the lesson map also goes to the menu.

**Contact us details:**
- The email address is `ContactScreen.TeamEmail`, the same address the consent emails come from. Change it there.
- The helplines are 116 (child helpline), 3029 (Isange One Stop Centre) and 112 (police). Check these numbers before the pilot.

## Fonts

The fonts are in `Assets/SafeTouch/Resources/Fonts`. Both are free under the SIL Open Font License; the licences are in [fonts/](fonts/).

- **Sniglet ExtraBold**: round, chunky letters for titles, buttons and any bold text of body size or larger.
- **Andika** (regular and bold, by SIL, made for early readers): everything else, so longer text stays easy to read.

## Cartoons

All cartoons are drawn in code from circles ([Decor.cs](../Assets/SafeTouch/Scripts/UI/Decor.cs)), so there are no image files and nothing to license.

- `Decor.Gang` shows five friends with different skin tones, hair and clothes. They hop one after the other at the top of the menu.
- Keza and her dog hop gently wherever they appear (`Decor.Friends`).
- Every button squashes a little under the finger and springs back (`Squish`, in [Motion.cs](../Assets/SafeTouch/Scripts/UI/Motion.cs)).

## Sound

[AudioManager.cs](../Assets/SafeTouch/Scripts/Managers/AudioManager.cs) makes all the sounds in code:

- **Music:** a bouncy 17-second loop (marimba tune, ukulele-like chords, soft beat and shaker) that repeats with no gap. It is made on a background thread when the app starts, which takes well under a second.
- **Tap sound:** every button makes a soft "pop".
- **Other effects:** correct, try again, badge and place are short marimba notes.

**When the music changes volume:**
- It fades to silent while the microphone is listening, so the voice coach hears the child's "NO!" and not the tune.
- It plays softer while a voice clip plays.
- It keeps playing between lessons.

Music and tap sounds can each be switched off in **Settings**. The choice is kept on the phone.

**Using a recorded song:** drag it onto the AudioManager's **Music Clip** field and it is used instead of the generated tune. Use only music you have the right to use.

## Letter size

**Settings > Letter size** has three buttons, a small, a medium and a big **A**:

| Button | Letters |
|---|---|
| small A | as the screens were designed |
| medium A (**the default**) | 15 % bigger |
| big A | 30 % bigger |

The choice is kept on the phone. [TextSize.cs](../Assets/SafeTouch/Scripts/UI/TextSize.cs) applies it to every text in the app, including lists and messages a screen makes later.

## App icon

The icon is a white shield on a sky-blue square, holding a raised yellow hand with a red heart in its palm. It has no words, so it reads at any size.

- [SafeTouchAppIcon.cs](../Assets/SafeTouch/Editor/SafeTouchAppIcon.cs) draws it into `Assets/SafeTouch/Icons/`: the whole icon, plus the background and foreground layers that Android's adaptive icons need.
- It sets the icon by itself when the project opens in Unity, if no icon is set yet.
- **SafeTouch > Make App Icon** draws and sets it again.
