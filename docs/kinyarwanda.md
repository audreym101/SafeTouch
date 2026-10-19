# Kinyarwanda version

Children (or a parent) pick **English** or **Ikinyarwanda** on the first screen, and can change it later in **Grown-ups → Settings**. The choice is saved on the phone, and every screen, lesson and game switches at once.

## Where the text lives

| What | English | Kinyarwanda |
|---|---|---|
| Interface and lesson lines (306 strings) | `Resources/Content/strings_en.json` and the English defaults in code | `strings_rw.json` |
| Scenario cards | `scenarios.json` | `scenarios_rw.json` |
| Trusted adult roles | `trusted_adults.json` | `trusted_adults_rw.json` |
| Badges | `badges.json` | `badges_rw.json` |
| Parent guides, warning signs, helplines | `parent_resources.json` | `parent_resources_rw.json` |
| Memory Quest pairs | `review.json` | `review_rw.json` |
| Bubble Guard requests | `guard_requests.json` | `guard_requests_rw.json` |

`ContentLibrary` loads the `_rw` file when Kinyarwanda is chosen and falls back to English for anything missing. The `_rw` files keep the same ids, answers, age levels, badge rules and phone numbers. `Tests/EditMode/LocalisationTests.cs` fails if they drift apart, or if a translated line loses a `{0}` placeholder.

The voice coach measures *how* NO is said, not the word, so **OYA** is coached the same way as NO. STOP keeps its English letters (S-T-O-P), with each step explained in Kinyarwanda:
- **S**: Vuga oya
- **T**: Bwira umuntu mukuru wizeye
- **O**: Sobanura ibyabaye
- **P**: Irinde, jya ahantu hatekanye

## Review before use with children

This is a **first draft by the development team, not a professional translation**. A native Kinyarwanda speaker and the child-protection experts must check all of it in week 7. The children should understand it at ages 6–8, and it should match the words used by teachers, IZU volunteers and Isange staff.

Please check these choices first:

| English | Draft | Question for the reviewer |
|---|---|---|
| Safe / Uncomfortable / Unsafe | Bitekanye / Bimbangamiye / Biteje akaga | Are these the clearest words for young children? |
| Personal space bubble | Uruziga (rwanjye) | Does "uruziga" read as the space around the body? |
| Private parts | Imyanya y'ibanga | Is this the term schools use with children? |
| Trusted adult | Umuntu mukuru wizeye | |
| Tickling (scenario S02) | Gutera ibikoni | Is this the everyday word? |
| Aunt or uncle | Masenge cyangwa Marume | Should it also cover the mother's sister (nyina wacu)? |
| High five | Gukoranaho ibiganza | Do children know a better word? |
| Bubble Guard (game) | Murinzi w'Uruziga | Is the name fun and clear? |
| Grooming (scenario S09) | Kureshya umwana | Is this the term child-protection staff use? |
| Skin tone names | Umusenyi, Ubuki, Karameli, Kakawo, Ikawa, Umukara | Are these natural, and is "Umukara" acceptable for the darkest tone? |

Narration audio is still to be recorded. The code already plays `<clip>_rw` when Kinyarwanda is chosen.
