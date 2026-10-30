# Confirming the child's age (Guardian lessons, 9-11)

The Guardian lessons talk openly about online strangers and private parts of the body, so they are not shown to younger children. A grown-up proves the child's age first, and the proof goes to the parent's email.

## The steps

| Step | What happens |
|---|---|
| 1 | The parent picks **Guardian 9-11** on the login screen (or taps the Guardian tab on the home screen). The first time, the **age check** opens. |
| 2 | They type the child's **day, month and year of birth**. The app works out the age on the phone. |
| 3 | If the child is **9 to 11**, the app tells the API the age in years and the API **emails a 6-digit code** to the parent's email on the consent (and texts it, if SMS is on). |
| 4 | The parent types the code. The Guardian lessons open, and stay open on this phone. |

Younger children (6-8) are pointed to the Explorer lessons; other ages are told that SafeTouch is for ages 6 to 11. A parent must have confirmed their email first, so the code goes to an address that is known to be theirs.

## Privacy: the date of birth is not kept

The date is only used to work out an age. It is **not saved on the phone and never sent**: the app clears the three boxes straight away and sends the number of years only. The API refuses any other field (a birth date included), and it stores only that the age was confirmed (`ageBand: "9-11"` and when). Withdrawing the consent deletes it.

## The API

| Endpoint | What it does |
|---|---|
| `POST /api/consent/:id/age` | `{ "ageYears": 10 }`. Emails the code. Only 9 to 11; anything else, or any extra field, is refused. The consent must be confirmed. |
| `POST /api/consent/:id/age/verify` | `{ "code": "123456" }`. Answers `{ "ageConfirmed": true, "ageBand": "9-11" }`. |

Limits are the same as for the login: a new code once a minute and 10 a day; 5 tries per code; codes last 24 hours. If neither the email nor the text can be sent, the app is told. The age code is separate from the consent and login codes.

## What it does not prove

This is a confirmation by a grown-up, not an identity check: a parent who wants to open the lessons for a younger child can. It stops a child opening them alone, and it puts the decision with the parent.
