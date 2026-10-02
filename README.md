# SafeTouch

SafeTouch is an Android AR app I'm building for my capstone. It teaches children aged 6 to 11 in Rwanda about personal boundaries, how to tell safe from unsafe situations, and who to go to for help.

The child points the phone at the floor and places Keza, a friendly guide character, in the room. Keza then takes them through short lessons:

1. **My Space Bubble** – three coloured zones around Keza (red, yellow, green). People walk up to her and the child decides what Keza should do.
2. **Safe or Unsafe?** – short scenario cards. Every answer gets an explanation.
3. **STOP!** – practising Say no, Tell, Open up, Protect.
4. **My Trusted Adults** – the child picks the adults they trust and they appear around Keza.
5. **Memory Quest** – a final game to help children remember what they learned: matching cards, building the word STOP, and a timed chase at home, school or the playground where saying NO wins.

Children aged 9 to 11 get extra "think deeper" explanations and a few harder scenarios. There is also a section for parents and teachers with conversation tips, warning signs and Rwandan helplines.

The app works offline. It never asks for a child's name, photo or location.

- **GitHub repo:** https://github.com/audreym101/SafeTouch
- **Video demo:** 
- **Figma prototype:** https://www.figma.com/proto/LGF1ZxkHqCZH1Lswr6zrMm/Untitled?node-id=1-1568&p=f&viewport=165%2C-166%2C0.17&t=z8OjXHODLxORaZaL-1&scaling=min-zoom&content-scaling=fixed&starting-point-node-id=1%3A1524&show-proto-sidebar=1&page-id=1%3A1521

---

## Tools I used

- **Unity 6 LTS (6000.0.58f1)** – the game engine
- **AR Foundation 6 + Google ARCore** – for finding the floor and placing Keza in the real room
- **C#** – all the app code
- **Figma** – screen designs and the clickable prototype
- **Node.js + Express + MongoDB** (optional) – a small API for saving anonymous results during testing. It is switched off by default.

I chose a phone instead of a VR headset because most families and schools here have Android phones, and children can use them without any extra equipment.

---

## Setting up the environment

1. Install **Unity Hub** from https://unity.com/download.
2. In Unity Hub, go to **Installs → Install Editor** and choose **Unity 6000.0.58f1**.
3. When it asks for modules, tick **Android Build Support**, including **OpenJDK** and **Android SDK & NDK Tools**.
4. On your Android phone, install **Google Play Services for AR** from the Play Store. The phone must be on the ARCore supported list: https://developers.google.com/ar/devices

## Running the project

1. Clone or download this repo.
2. In Unity Hub, click **Add → Add project from disk** and choose the `SafeTouch` folder.
3. Open it. The first time takes a few minutes while Unity downloads the AR packages.
4. Go to **File → Build Profiles**, select **Android** and click **Switch Platform**.
5. In the top menu click **SafeTouch → 1. Configure Android AR Settings**.
6. Then click **SafeTouch → 2. Build SafeTouch Scene**. This creates the scene with Keza and all the screens.
7. **To test on the laptop:** go to **Edit → Project Settings → XR Plug-in Management**, tick **XR Simulation** on the desktop tab, and press **Play**.
8. **To run on a phone:** turn on Developer options and USB debugging on the phone, plug it in, then go to **File → Build Profiles → Build And Run**.

**Optional backend:** go into the `backend` folder and run `npm install`, then `npm test`.

---

## Designs

- **Figma prototype:** (link above). The Figma files and an import plugin are in `docs/designs/figma`.
- **Screenshots** of every screen are in `docs/designs/screens`.
- **Navigation map:** `docs/designs/navigation-map.png`
- **Where objects are placed in AR:** `docs/designs/ar-placement-top.png` and `docs/designs/ar-placement-side.png`
- **Style guide:** `docs/designs/style-guide.png`


<img width="2360" height="1734" alt="Screenshot 2026-10-02 111835" src="https://github.com/user-attachments/assets/16ef078d-fc1a-4cbf-9c2e-cb0114b1a981" />



The assets I plan to use, and why I need each one, are listed in `docs/assets.md`. How the user interacts with the app on the phone is explained in `docs/hardware.md`.

---

## Deployment plan

For now, SafeTouch is not going on the Play Store. The plan is:

1. **Development:** test on my laptop with Unity's XR Simulation.
2. **Device testing:** build an APK and install it on my own ARCore phone.
3. **Pilot:** install a signed APK by hand on 2 or 3 phones for testing with children at a partner school, after the content has been checked by child protection experts.
4. **Optional:** host the small API on Render with a free MongoDB Atlas database, only if my supervisor and the ethics review approve it.

More details are in `docs/deployment-plan.md`.

---

## What's still in progress

- Keza and the other characters are simple placeholder shapes for now. They will be replaced with proper 3D models and animations.
- Narration and full Kinyarwanda translations still need to be recorded and written.
- All lesson content will be reviewed by experts before testing with children.
- The helpline numbers need to be confirmed with NCDA and RIB.
