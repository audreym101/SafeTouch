# Parent login

Once a parent has confirmed their consent ([consent.md](consent.md)), the app asks them to **log in every time it opens**: they type the email they gave when they agreed, and a fresh 6-digit code arrives by email (and by SMS when [SMS is on](sms.md)). The code opens the app.

| Step | What happens |
|---|---|
| 1 | The app opens on the **Log in** screen when the consent is confirmed (and **Start** leads there too, if someone goes back to the welcome screen). |
| 2 | The parent types their email and taps **Send me a code**. |
| 3 | The API checks the email against the one on record. If it matches, it emails (and texts) a new code. |
| 4 | The parent types the code. Correct: the app opens. The login lasts until the app is closed. |

- **Not saved, and ended when the app is left:** the login lasts only while the app is in use. When the parent leaves the app (the home button, another app) and comes back, the Log in screen shows again. Android's camera and microphone questions also pause the app, and those do not log the parent out.
- **The consent panel stays until the email is confirmed.** A parent who agreed to the terms but has not typed the emailed code sees the code screen every time the app opens, with a reminder once a day or more has passed. There is no "verify later" and no grace period: SafeTouch stays closed until the code is typed. Typing the code there also counts as a login.
- **Needs internet,** because the code comes from the API. Without it, the screen says so and waits. There is no bypass, so check the phone has internet before a session.
- **After withdrawing** consent, the next start goes to the consent page.

## The API

| Endpoint | What it does |
|---|---|
| `POST /api/consent/:id/login` | `{ "email": "..." }`. Sends a login code if the email matches the consent on record. |
| `POST /api/consent/:id/login/verify` | `{ "code": "123456" }`. Answers `{ "loggedIn": true }`. |

- **The same answer for any email.** The first endpoint says `{ "sent": true }` whether or not the email matches, so nobody can use it to find out which emails are registered. The code only ever goes to the email on record.
- **Limits.** A login code lasts 10 minutes and works once. Wrong codes: 5 tries, then a new code is needed. New codes: once a minute and 10 a day. If neither the email nor the text can be sent, the app is told.
- **Separate from consent codes.** A login code never confirms the consent, and the other way round.

## Costs and limits to know

- Each login sends one email (free on Brevo up to 300 a day) and, with SMS on, one text. The SMS limit of 3 texts per phone per day ([sms.md](sms.md)) is used up quickly by logins: after that, only the email goes out. Raise `SMS_PER_PHONE_DAILY` if the parent should always get a text.
- The free Render server sleeps after 15 minutes without use, so the first code request after a pause can take up to a minute.

## Choosing the group on the login screen

The Log in screen has the two group buttons, **Explorer 6-8** and **Guardian 9-11**, above the email box. The parent picks the group for their child, logs in, and the app opens that group's lessons. Picking Guardian when the age has not been confirmed first sends the parent to the age check ([age-check.md](age-check.md)).
