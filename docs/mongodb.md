# MongoDB backend (Atlas + Render, no card)

The API stores progress and consent in MongoDB when `MONGODB_URI` is set. This is the same data as on Firebase ([firebase.md](firebase.md)); the app doesn't change, only the Base Url.

| Collection | Key | What it holds |
|---|---|---|
| `consents` | `consentId` (unique index) | The parent's name, phone, email, country, agreements, dates, and a SHA-256 hash of the verification code (see [consent.md](consent.md)) |
| `progress` | `deviceId` (unique index) | Anonymous progress, only when a parent switches sync on |

The API picks the database like this: `MONGODB_URI` set, so MongoDB; otherwise Firestore if its credentials are set; otherwise memory (lost on restart).

Atlas **M0** and Render's **Free** web service need no card.

## 1. Atlas: the database

1. Sign up at <https://www.mongodb.com/cloud/atlas>.
2. **Create a project** named `safetouch`. Tags are optional; skip them or add one such as `app` = `safetouch`.
3. **Create a cluster** (**Build a Database**):
   - **Tier:** **M0 Free**
   - **Provider and region:** AWS, **Frankfurt (eu-central-1)** or **Ireland (eu-west-1)**
   - **Name:** `safetouch`
4. **Database user** (**Security** > **Database Access** > **Add New Database User**):
   - **Authentication:** Password
   - **Username:** `safetouch-api`, with a long password. Use letters and digits only; a symbol like `@` or `/` has to be percent-encoded in the connection string.
   - **Role:** **Read and write to any database**
5. **Network access** (**Security** > **Network Access** > **Add IP Address**): choose **Allow access from anywhere** (`0.0.0.0/0`). Render's free service has no fixed address, so the database login (user and password) is what protects it.
6. **Connection string:** open the cluster > **Connect** > **Drivers** and copy the string. It looks like:
   ```
   mongodb+srv://safetouch-api:<password>@safetouch.xxxxx.mongodb.net/?retryWrites=true&w=majority
   ```
   Replace `<password>` with the real password, without the `<` `>`.

> **Rwanda's data protection law** (N° 058/2021) needs **NCSA authorisation** to store personal data outside Rwanda. Atlas regions are all outside Rwanda, so mention the region in the ethics application and the NCSA registration.

## 2. Render: the API

At <https://render.com>, choose **New** > **Web Service** and pick the SafeTouch repo.

| Setting | Value |
|---|---|
| Branch | `audrey` |
| Root Directory | `backend` |
| Build Command | `npm ci` |
| Start Command | `npm start` |
| Instance type | Free |
| Health Check Path (Advanced) | `/api/health` |

**Environment variables:**
- `MONGODB_URI`: the connection string from step 6
- `MONGODB_DB`: `safetouch`
- `MAIL_PROVIDER`: `console` for now. The consent emails, with the 6-digit code, then appear in Render's **Logs** tab. For real emails use `brevo` or `resend` with `MAIL_API_KEY` and `MAIL_FROM`.

After the deploy, the log says `SafeTouch API on ... (MongoDB Atlas, consent emails: console)`.

## 3. Check it

- `https://<name>.onrender.com/api/health` should answer `{"status":"ok"}`.
- `https://<name>.onrender.com/api/summary` reads the database. It should answer `{"devices":0,...,"consents":{"total":0,"verified":0}}`. A `{"error":"Server error."}` means the connection failed. Look in Render's **Logs**: a wrong password or a network-access rule is the usual cause.
- After the first consent from the app, Atlas > **Browse Collections** shows `safetouch` > `consents`.

## 4. Connect Unity

On **Consent Service** and **Cloud Sync Service** (both on **SafeTouch App**):
- set **Base Url** to the `https://<name>.onrender.com` address, with no `/` at the end;
- set **Timeout Seconds** to **60**, because the free server sleeps after 15 minutes and takes up to a minute to wake;
- on **Cloud Sync Service**, also tick **Enabled In Build**.

Then rebuild the APK. Open `/api/health` once before a session to wake the server.

## On a laptop

Copy `backend/.env.local.example` to `backend/.env.local`, set `MONGODB_URI`, and run `npm start`. Without it the API keeps data in memory.

## Deleting the data after the study

In Atlas > **Browse Collections**, drop the `consents` and `progress` collections, or delete the cluster.
