# Firebase backend

The SafeTouch API (`backend/`) runs on Firebase:
- one **Cloud Function** called `safetouch`, which serves the same Express API as `npm start`;
- a **Cloud Firestore** database behind it.

The Unity app talks only to the function. Firestore rules (`backend/firestore.rules`) refuse every read and write that comes straight from a phone or browser, so the parents' contact details can only be reached through the API, which checks every request.

| Collection | Document id | What it holds |
|---|---|---|
| `consents` | the consent id | The parent's name, phone, email, country, the four agreements, terms version, dates, and a SHA-256 hash of the verification code (see [consent.md](consent.md)) |
| `progress` | the random device id | Anonymous progress, only when a parent switches sync on |

Every read uses a document id, so no indexes are needed.

## Cost

Cloud Functions need the **Blaze (pay as you go)** plan, which asks for a payment card. A pilot stays well inside the free allowance:
- Firestore: 50,000 reads and 20,000 writes a day;
- Functions: 2 million calls a month.

`maxInstances: 3` in `src/function.js` caps how far the function can scale. Set a budget alert too (step 2 below).

## One-time setup

### 1. Create the project

1. Go to <https://console.firebase.google.com> and choose **Create a project**.
2. Name it **SafeTouch**. Note the **project ID** under the name (for example `safetouch-4f2a1`); you'll need it below.
3. **Google Analytics:** turn it off. The app is for children and doesn't need it.

### 2. Switch to the Blaze plan and set a budget

1. Click the plan name (**Spark**) at the bottom of the left menu, then **Upgrade** > **Blaze**, and add a card.
2. When asked for a budget, set a small one, for example **5 USD** (or later: Google Cloud console > **Billing** > **Budgets & alerts**). You get an email at 50%, 90% and 100%.

A budget sends warnings; it doesn't stop the service.

### 3. Create the database

1. **Build** > **Firestore Database** > **Create database**.
2. **Edition:** Standard.
3. **Location:** `europe-west1 (Belgium)`, the same region as the function. **The location can't be changed later.**
4. **Start in production mode**. The deploy below replaces the rules with `backend/firestore.rules` anyway.

> **Rwanda's data protection law** (N° 058/2021) needs **NCSA authorisation** to store personal data outside Rwanda, and every Firestore location is outside Rwanda. Mention the Firestore location in the ethics application and the NCSA registration before the pilot collects real parents' details.

### 4. Install the Firebase CLI on the laptop

```bash
npm install -g firebase-tools
firebase login
```

### 5. Point `backend/` at the project

```bash
cd backend
npm install
firebase use --add            # pick the SafeTouch project, alias "default"
cp .env.example .env          # API_REGION, MAIL_PROVIDER, MAIL_FROM
```

`firebase use --add` writes `.firebaserc`. You can instead copy `.firebaserc.example` and type the project ID. `.firebaserc` and `.env` aren't committed.

Two rules for `.env`, because it's uploaded with the function:
- **No secrets.**
- **No names Firebase reserves:** `PORT`, `FUNCTION_*`, `FIREBASE_*`.

Settings for your laptop only go in `.env.local` (see `.env.local.example`).

### 6. Set up the email provider (when ready for real emails)

While `MAIL_PROVIDER=console`, consent emails, including the 6-digit code, are only written to the function logs (step 9). That's fine for testing. To send real emails:

1. Create a free **Brevo** account (<https://www.brevo.com>, 300 emails a day).
2. In Brevo, verify the sender address: **Senders, domains & dedicated IPs**.
3. Create an API key: **SMTP & API** > **API keys**.
4. In `backend/.env`, set `MAIL_PROVIDER=brevo` and `MAIL_FROM=` the verified address.
5. Store the key as a Firebase secret (it is never written in a file):
   ```bash
   firebase functions:secrets:set MAIL_API_KEY
   ```

Resend works the same way, with `MAIL_PROVIDER=resend`.

### 7. Deploy

```bash
npm test          # 25 tests
npm run deploy    # firebase deploy --only functions,firestore:rules
```

The first deploy turns on the Google Cloud services it needs, so it takes a few minutes. It ends with the **Function URL**, for example:

```
https://europe-west1-safetouch-4f2a1.cloudfunctions.net/safetouch
```

Check it in a browser: `<Function URL>/api/health` should answer `{"status":"ok"}`.

### 8. Connect the Unity app

In the scene, select **SafeTouch App** and paste the Function URL, without a slash at the end, into:
- **Consent Service** > **Base Url**;
- **Cloud Sync Service** > **Base Url**. Also tick **Enabled In Build**. Sync still only runs once a parent switches it on.

Build the APK again.

### 9. Watching it

| Where | What |
|---|---|
| **Firestore Database** > **Data** | The `consents` and `progress` collections. Only project members can see them. |
| **Functions** > **Logs** (or `firebase functions:log`) | Requests, errors and, while `MAIL_PROVIDER=console`, the consent emails |
| `<Function URL>/api/summary` | Totals for the evaluation: devices, lessons, accuracy, consents total and verified |

Give the supervisor access with **Project settings** > **Users and permissions** > **Add member**, role **Viewer**.

## Running locally

| How | Data goes to | Setup |
|---|---|---|
| `npm start` | Memory (lost on restart) | Nothing |
| `npm start` with the **emulator** | Local Firestore emulator | `npm run emulate` in another terminal (the emulator needs Java 11+). In `.env.local`: `FIRESTORE_EMULATOR_HOST=127.0.0.1:8080` and `GOOGLE_CLOUD_PROJECT=demo-safetouch` |
| `npm start` against the **real** Firestore | The SafeTouch project | **Project settings** > **Service accounts** > **Generate new private key**. Keep the file outside the repo, and set `GOOGLE_APPLICATION_CREDENTIALS=<path to it>` in `.env.local` |

`npm run emulate` also runs the function itself, at `http://127.0.0.1:5001/<project-id>/europe-west1/safetouch`. The emulator UI is at <http://127.0.0.1:4000>.

A development build on the phone can use `http://<laptop-ip>:3000` (same Wi-Fi). Release builds need the `https://` Function URL.

## Deleting the data after the study

In the console, open **Firestore Database** > **Data**, then the `⋮` menu on each collection, then **Delete collection**. Or delete the whole Firebase project: it is kept for 30 days, then removed.
