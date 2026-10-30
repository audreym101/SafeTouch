# MongoDB backend (Atlas + Render, no card)

The API stores progress and consent in MongoDB when `MONGODB_URI` is set; the Unity app only needs the address of the API (its Base Url).

| Collection | Key | What it holds |
|---|---|---|
| `consents` | `consentId` (unique index) | The parent's name, phone, email, country, agreements, dates, and a SHA-256 hash of the verification code (see [consent.md](consent.md)) |
| `progress` | `deviceId` (unique index) | Anonymous progress, only when a parent switches sync on |

Without `MONGODB_URI` the API keeps the data in memory, which is lost when it restarts: fine for testing, not for the pilot.

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

The app calls the API at the address in `ApiAddress.cs` (`https://safetouch-api-xc6t.onrender.com`), with a 60-second wait for the free server to wake up. **On a phone, a scene that still says `http://localhost:3000` is ignored**: "localhost" is the phone itself, so the consent could never reach the server and no email would be sent. In the Editor, the **Base Url** on **Consent Service** and **Cloud Sync Service** is still used, so you can test against `localhost` on your laptop.

To use another server, set **Base Url** on both components to its `https://` address, or change `ApiAddress.Default`. Tick **Enabled In Build** on **Cloud Sync Service** if the ethics approval covers anonymous sync (a parent still has to switch it on).

Then rebuild the APK. Open `/api/health` once before a session to wake the server.

## On a laptop

Copy `backend/.env.local.example` to `backend/.env.local`, set `MONGODB_URI`, and run `npm start`. Without it the API keeps data in memory.

## Deleting the data after the study

In Atlas > **Browse Collections**, drop the `consents` and `progress` collections, or delete the cluster.
