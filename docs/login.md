# Parent login

Once a parent has confirmed their consent ([consent.md](consent.md)), the app asks them to **log in every time it opens**: they type the email they gave when they agreed, and a fresh 6-digit code arrives by email (and by SMS when [SMS is on](sms.md)). The code opens the app.

| Step | What happens |
|---|---|
| 1 | The app opens on the **Log in** screen when the consent is confirmed (and **Start** leads there too, if someone goes back to the welcome screen). |
| 2 | The parent types their email and taps **Send me a code**. |
| 3 | The API checks the email against the one on record. If it matches, it emails (and texts) a new code. |
| 4 | The parent types the code. Correct: the app opens. The login lasts until the app is closed. |

- **Not saved:** the login is kept only while the app runs. Closing it, or the phone restarting it, means logging in again.
- **Parents who haven't confirmed yet** don't log in. When the app opens, they see a **reminder** to confirm the emailed code, with the days left of the 7. Within the 7 days they can tap "Verify later" and let the child start; after that, the code is needed before the child can continue. Typing the code there also counts as a login. (On the day they agree, the screen is the normal "one last step" instead of a reminder.)
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
