# Verification code by SMS

The 6-digit consent code is emailed to the parent ([consent.md](consent.md)). With an SMS provider set, the **same code is also texted** to the phone number the parent gave. They can type whichever arrives first.

- **Email stays the fallback.** If the text fails (no credit, a number the provider refuses), the email still goes out, and the other way round. The reply from `POST /api/consent` and `/resend` says what was sent: `emailSent` and `smsSent`.
- **Same safety rules** as the email: 5 tries per code, codes expire after 24 hours, a new code at most once a minute and 5 times a day. A new code replaces the old one in both messages.
- **A corrected phone number** gets a new code by SMS. The same details sent again (an offline retry) send nothing.
- **No app change.** The consent form already collects the phone number with its country code.

## Setting it up with Brevo

1. In Brevo, open **SMS** and buy credits (**Pricing** / **Buy credits**). SMS is paid per message, and Rwanda's price differs from other countries'. Check it before buying.
2. Set these in `backend/.env.local`, and in Render > **Environment**:

   | Key | Value |
   |---|---|
   | `SMS_PROVIDER` | `brevo` |
   | `SMS_SENDER` | `SafeTouch` (the name the parent sees: up to 11 letters and digits) |
   | `SMS_API_KEY` | optional: an API key with SMS permission. Without it, `MAIL_API_KEY` is used |

3. Restart the API. With `SMS_PROVIDER=console`, the texts are printed instead of sent; without `SMS_PROVIDER`, no SMS is sent.

**Sender name:** some countries and mobile networks require a sender name to be registered, or replace it with a number. If parents don't receive the text, ask Brevo support whether `SafeTouch` must be registered for Rwanda (MTN and Airtel).

## Limits, so a fake form can't run up the bill

Anyone can call the API, so the API caps the texts, in memory (they reset when the server restarts, which happens when Render's free server sleeps):

| Setting | Default | Meaning |
|---|---|---|
| `SMS_PER_PHONE_DAILY` | 3 | Texts to one number in 24 hours |
| `SMS_DAILY_LIMIT` | 100 | Texts in total in 24 hours |

Past the limit, the email still goes out.

## Privacy

The phone number is now also passed to the SMS provider (Brevo), as the email address is. The terms already say that SafeTouch keeps the number to verify the consent and contact the parent. Mention Brevo as the provider in the ethics application and the NCSA registration.
