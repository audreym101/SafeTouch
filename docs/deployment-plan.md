# Deployment plan

SafeTouch is an offline Android app. Deployment means getting a tested APK onto the phones used in the evaluation sessions, with an optional small API for anonymous results. Nothing is published to the public Play Store during the capstone.

## Environments

| Environment | What runs | Where | When |
|---|---|---|---|
| Development | Unity editor with XR Simulation; API on `localhost:3000` with the in-memory store | Researcher's laptop | Weeks 1 to 6 |
| Device testing | Development APK (Build And Run over USB) | Researcher's ARCore phone | Weeks 2 to 7 |
| Pilot | Signed release APK, installed by hand | 2 to 3 ARCore phones used at the partner school or community centre in Kigali | Weeks 7 to 8 |
| Cloud API | `backend/` on Render's free web service with MongoDB Atlas M0 ([mongodb.md](mongodb.md)) | Render and Atlas, HTTPS | Week 6: parental consent; progress sync only if the supervisor and ethics review approve it |

## Building the release APK

1. SafeTouch > **1. Configure Android AR Settings** (API 26+, IL2CPP ARM64, OpenGL ES 3, portrait). Then Project Settings > XR Plug-in Management > Google ARCore > **Requirement: Required**, so the Play Store only offers it to ARCore phones later.
2. Project Settings > Player > Android > Publishing Settings: create a keystore (`safetouch.keystore`), kept outside the repo and backed up.
3. Set **Version** (`0.1.0` for the supervisor demo, `0.2.0` for the pilot) and increase **Bundle Version Code** every build.
4. File > Build Profiles > Android: Development Build **off**, Build.
5. Record the APK's SHA-256 (`certutil -hashfile SafeTouch.apk SHA256` on Windows, `shasum -a 256` on macOS) in the release notes so the pilot build can be identified.

## Installing on pilot phones

1. Check the model on the ARCore supported devices list.
2. Install **Google Play Services for AR** from the Play Store.
3. Copy the APK over USB or with `adb install SafeTouch.apk`, and allow "Install unknown apps" for that one install, then turn it off again.
4. Open SafeTouch once, allow the camera, and run the scan screen to confirm planes are detected in that room.
5. In the parent section, confirm **Anonymous progress sync is OFF** (the default) unless the ethics approval covers it.
6. Between children, use Settings > **Delete progress on this phone** so each session starts clean.

## The API (parental consent and optional anonymous progress)

| Item | Choice | Reason |
|---|---|---|
| Runtime | Node.js 22, Express | Small, well documented, already in the architecture diagram |
| Database | MongoDB Atlas M0 (free), `progress` and `consents` collections | Matches the ERD; consents kept apart from progress; the free tier is enough for a pilot |
| Hosting | Render free web service, deployed from GitHub (branch `audrey`, root directory `backend`) | HTTPS by default, free, no card; it sleeps after 15 minutes without use, so the first request can take up to a minute |
| Access | Atlas: one database user for the API, and network access open to Render (`0.0.0.0/0`) because the free plan has no fixed address | Parents' details are only reachable with that user and password |
| Secrets | `MONGODB_URI`, `MAIL_API_KEY` and the SMS key are set as Render environment variables | Never committed (`.env` and `.env.local` are in `.gitignore`) |
| Endpoints | `POST /api/progress`, `GET /api/progress/:deviceId`, `DELETE /api/progress/:deviceId`, `GET /api/summary`, `GET /api/health`, and `/api/consent` ([consent.md](consent.md)) | Save, read back, withdraw, report totals, parental consent |

Deploy steps ([mongodb.md](mongodb.md)): create the Atlas cluster and database user > Render > New Web Service from the repo (branch `audrey`, root directory `backend`, build `npm ci`, start `npm start`) with the environment variables > set **Base Url** on **ConsentService** and **CloudSyncService** to the Render address and tick **Enabled In Build**. GitHub Actions runs `npm test` on every push to `backend/`.

**Privacy rules the API enforces:** it rejects any field it doesn't know (so a name can't slip in), requires the device id to be a random UUID, accepts only role ids for trusted adults, and `/api/summary` returns totals only. Switching sync off in the app calls `DELETE`, so a parent can withdraw at any time (Law N° 058/2021, Article 9). All records are deleted when the study ends.

## Release checklist

- [ ] All 10 scenarios and parent texts approved by the expert reviewers (week 7)
- [ ] Helpline numbers re-confirmed with NCDA and RIB
- [ ] Kinyarwanda strings reviewed by a native speaker
- [ ] Tested on every pilot phone model in a real classroom light level
- [ ] Sync off by default; no personal data fields anywhere in `Progress`
- [ ] Version and SHA-256 recorded

## Rollback

Keep the previous signed APK. If a pilot build fails on a phone, uninstall and `adb install` the previous version; progress is local, so nothing else needs undoing.

## After the capstone

Google Play **internal testing** track, then closed testing with partner schools. Because the app is for children, it would follow Google Play's Families policy and declare "no data collected" in the Data safety form while sync is off.
