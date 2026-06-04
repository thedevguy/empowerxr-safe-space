# How to install / setup / use this application

A complete, end-to-end guide for running the **EmpowerXR Safe Space** demo on your
own hardware: a **Samsung Galaxy XR** headset and a **Samsung Galaxy Watch Ultra**
(or any Wear OS 3+ watch with a heart-rate sensor).

By the end you will have: the watch streaming your live heart rate, the headset
running the *hallway → sensory overload → puppy + breathing-balloon intervention*
experience, and (optionally) a laptop dashboard showing the biometrics in real time.

> **Read first — two short safety notes.**
> 1. The experience deliberately induces sensory stress: **strobing lights, rapid
>    flashing, harsh audio, and walls closing in.** Do not use it if you are
>    photosensitive/epileptic, and stop immediately if you feel unwell.
> 2. This is a **demonstration, not a medical device.** Only heart rate is a real
>    measurement; all other biometrics are physiological *estimates*. Do not use it
>    for diagnosis or treatment.

---

## Table of contents

1. [How it fits together](#1-how-it-fits-together)
2. [What you need](#2-what-you-need)
3. [Step 0 — Network setup (do this first)](#3-step-0--network-setup-do-this-first)
4. [Step 1 — Get the code](#4-step-1--get-the-code)
5. [Step 2 — Install the Watch app (Galaxy Watch Ultra)](#5-step-2--install-the-watch-app-galaxy-watch-ultra)
6. [Step 3 — Build & deploy the Headset app (Galaxy XR)](#6-step-3--build--deploy-the-headset-app-galaxy-xr)
7. [Step 4 — (Optional) Facilitator dashboard](#7-step-4--optional-facilitator-dashboard)
8. [Step 5 — Run the demo](#8-step-5--run-the-demo)
9. [Calibration & tuning](#9-calibration--tuning)
10. [Troubleshooting](#10-troubleshooting)
11. [Configuration reference](#11-configuration-reference)

---

## 1. How it fits together

```
⌚ Galaxy Watch Ultra            🥽 Galaxy XR headset            🖥️ Laptop (optional)
   reads live heart rate            runs the VR experience          facilitator view
   derives 5 more metrics           + intervention logic
          │                                 ▲                              ▲
          │   UDP :5000 (JSON, ~2/sec)      │                              │
          └──── broadcast on your Wi-Fi ────┼──────────────────────────────┘
                                            │            (relay → WebSocket :8080)
```

The watch **broadcasts** a small JSON packet over Wi-Fi twice a second. The headset
listens for it and drives the experience; an optional laptop relay+dashboard listens
to the same broadcast to visualize it. **Everything depends on all devices sharing
one Wi-Fi subnet** — that's why Step 0 matters most.

For the deeper architecture, see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## 2. What you need

### Hardware
- [ ] **Samsung Galaxy XR** headset (Android XR).
- [ ] **Samsung Galaxy Watch Ultra** — or any **Wear OS 3+** watch with a heart-rate sensor.
- [ ] A **Samsung phone** paired to the watch via the **Galaxy Wearable** app (used to set the watch up; the demo app itself runs standalone).
- [ ] A **Wi-Fi network you control** — a home router, a **travel router**, or a **phone hotspot**. (Public/venue/corporate Wi-Fi usually blocks device-to-device and broadcast traffic — see Step 0.)
- [ ] A **development PC/Mac** and a USB cable.

### Software on your dev machine
- [ ] **Unity 6 — version `6000.4.1f1`** with **Android Build Support** (includes Android SDK/NDK & OpenJDK). Install via Unity Hub.
- [ ] **Android Studio** (or at least the Android SDK + `adb`) to build and install the watch app.
- [ ] **Git**.
- [ ] **Node.js 18+** — only if you want the optional laptop dashboard.

---

## 3. Step 0 — Network setup (do this first)

All three devices (watch, headset, laptop) **must be on the same Wi-Fi subnet**, and
that network **must allow broadcast / client-to-client traffic.**

### 3.1 Pick the right network
- ✅ **Best:** a personal travel router or a phone **hotspot**. Predictable subnet, broadcast allowed.
- ⚠️ **Home Wi-Fi:** usually fine.
- ❌ **Avoid:** hotel/office/conference Wi-Fi — most enable *AP/client isolation*, which silently drops the broadcast and **nothing will reach the headset.**

### 3.2 Find your subnet's broadcast address
Connect your laptop (or phone) to the chosen Wi-Fi and check its IP:

- **Windows:** open PowerShell → `ipconfig` → read **IPv4 Address** and **Subnet Mask**.
- **macOS/Linux:** `ifconfig` or `ip addr`.

For a typical home/hotspot network with mask `255.255.255.0` (a "/24"), the broadcast
address is the first three octets followed by **`.255`**:

| Your device IP | Broadcast address to use |
|---|---|
| `192.168.1.37` | `192.168.1.255` |
| `192.168.8.101` | `192.168.8.255` |
| `10.0.0.42` | `10.0.0.255` |

**Write this broadcast address down** — you'll put it in the watch app in Step 2.

---

## 4. Step 1 — Get the code

```bash
git clone <your-repo-url> empowerxr-safe-space
cd empowerxr-safe-space
```

The pieces you'll use live here:
- `apps/WearOS_Bridge/` — the watch app
- `EpowerXR/` — the Unity headset app
- `apps/udp-relay/` + `apps/web-dashboard/` — the optional laptop dashboard

---

## 5. Step 2 — Install the Watch app (Galaxy Watch Ultra)

The watch app reads your heart rate and broadcasts the biometric packet. It's a
standalone Wear OS app — once installed it runs on the watch by itself.

### 5.1 Set your broadcast address
Open `apps/WearOS_Bridge/src/main/java/com/empowerxr/wearosbridge/MainActivity.kt`
and set the constant to the broadcast address you found in Step 0:

```kotlin
private const val BROADCAST_ADDRESS = "192.168.1.255"   // ← your subnet broadcast
private const val BROADCAST_PORT     = 5000              // leave as-is (matches headset/relay)
private const val BROADCAST_INTERVAL_MS = 500L           // 2 packets/sec; leave as-is
```

### 5.2 Put the watch in developer mode
On the watch (menu names vary slightly by One UI Watch version):
1. **Settings → About watch → Software** → tap **Software version** ~7 times until "Developer mode enabled".
2. **Settings → Developer options** → enable **ADB debugging**, then enable **Debug over Wi-Fi** (a.k.a. *Wireless debugging*). Note the **IP address : port** it shows.

### 5.3 Connect and install over Wi-Fi
From your dev machine, with the watch on the same Wi-Fi:

```bash
# Pair once (if your adb prompts for it), then connect:
adb pair <watch-ip>:<pairing-port>        # only on first connect, if shown
adb connect <watch-ip>:<debug-port>

# Build and install the app onto the watch:
cd apps/WearOS_Bridge
./gradlew installDebug      # Windows: .\gradlew.bat installDebug
```

> The first Gradle run downloads the Android toolchain and can take several minutes.

### 5.4 Launch & grant permission
1. On the watch, open **"WearOS Bridge"** from the app list.
2. When prompted, **Allow** the **Body sensors** permission (required for heart rate).
3. Wear the watch **snugly** on your wrist. Within a few seconds the screen should
   show a live **`HR: ## bpm`** and **"Streaming…"**, plus the derived values
   (Resp, Stress, Skin, Sweat, SpO₂).

If it says `HR: waiting…` for more than ~30 s, see [Troubleshooting](#10-troubleshooting).

---

## 6. Step 3 — Build & deploy the Headset app (Galaxy XR)

### 6.1 Open the project
1. In **Unity Hub → Add → Add project from disk**, select the **`EpowerXR/`** folder.
2. Open it with **Unity `6000.4.1f1`** (Unity Hub will warn if you don't have that exact version — install it).
3. Let Unity import (first import takes a while). Open **`Assets/Scenes/SampleScene.unity`**.

### 6.2 Validate the scene (important)
This experience was assembled from multiple contributors' work. **Confirm the scene
is intact before building:**
- The scene should contain the **liminal hallway**, the stress effects
  (`ArcFlashController`, `EchoChamberController`, **Light chaos**), the
  **SafeSpaceStateMachine**, a **Puppy** environment, and an **"Inhale Exhale"**
  breathing object.
- Check the Console for **missing references / pink (magenta) materials**. If you see
  any, re-assign the missing asset or material (all assets are under
  `Assets/3D model/`, `Assets/Audio/`, `Assets/Materials/`, `Assets/SandboxEffects/`).

### 6.3 Configure the intervention
Select the **SafeSpaceStateMachine** object in the Hierarchy and review its Inspector:

| Field | What to set |
|---|---|
| **Biometric Receiver** | the scene's `BiometricReceiver` object (listens on UDP **5000**) |
| **Heart Rate Threshold** | `77` by default — the bpm that triggers intervention. **Calibrate this** (see Step 9). |
| **Intervention Mode** | `ARPassthrough` (enables passthrough + shows the puppy environment) **or** `SwapObjects` (hides the hallway, shows the calm scene) |
| **Puppies Environment** | the Puppy/calming GameObject (used by `ARPassthrough` mode) |
| **Objects To Activate / Deactivate** | the calm-vs-stress GameObjects (used by `SwapObjects` mode) |

> **How triggering works:** the SDK enters intervention when `heartRate > threshold`
> **or** `stressLevel > 70`, and recovers when `heartRate < threshold` **and**
> `stressLevel < 50`. (On the headset, `stressLevel` currently reads 0 due to a known
> field-name mismatch — so triggering is effectively **heart-rate-driven**. See
> [§10](#known-headset-only-reads-heart-rate-stresssweat-show-0) for the one-line fix.)

### 6.4 Build to the headset
1. **File → Build Settings** (or **Build Profiles**) → select **Android** → **Switch Platform**.
2. Confirm `Assets/Scenes/SampleScene` is the scene in the build list.
3. Make sure **OpenXR / Android XR** is enabled: **Project Settings → XR Plug-in Management → Android tab → OpenXR** checked (the `Android XR` / `androidxr-openxr` feature group should be enabled).
4. Enable **Developer mode** on the Galaxy XR headset and connect it via `adb` (USB or Wi-Fi), the same way as any Android device.
5. Click **Build And Run** (or **Build** an `.apk` and `adb install -r your.apk`).

The headset must be on the **same Wi-Fi subnet** as the watch (Step 0).

---

## 7. Step 4 — (Optional) Facilitator dashboard

A laptop on the same Wi-Fi can show the live biometrics — useful for an audience or
a facilitator. It does **not** affect the headset.

```bash
# Terminal 1 — relay (listens UDP :5000, serves WebSocket :8080)
cd apps/udp-relay
npm install
npm start

# Terminal 2 — dashboard
cd apps/web-dashboard
npm install
cp .env.example .env        # contains VITE_WS_URL=ws://localhost:8080
npm run dev                 # open the printed http://localhost:5173
```

Allow **inbound UDP 5000** through the laptop's firewall (Windows will usually prompt
the first time `node` runs — click **Allow**). When the watch is streaming, the
watch faces and charts come alive.

---

## 8. Step 5 — Run the demo

1. **Wear the watch** (snug) and confirm it shows **`HR … Streaming…`**.
2. **Put on the Galaxy XR** and launch the **EmpowerXR** app.
3. (Optional) open the **dashboard** on the laptop.
4. **Walk the experience:** you start in a calm space, then move down the **liminal
   hallway** where the environment escalates — light chaos/flicker, arc-flash strobes,
   an echo chamber with closing walls and harsh audio.
5. As stress builds, **your heart rate rises.** When it crosses the threshold, the
   SDK **intervenes**: passthrough/calm scene engages, the **puppy** appears, and the
   **breathing balloon ("Inhale Exhale")** guides you to slow your breathing.
6. As your heart rate settles back below the threshold, the experience **resumes**.

To rehearse without raising your real heart rate, temporarily lower the threshold
(Step 9) so the intervention triggers at your resting rate.

---

## 9. Calibration & tuning

- **Heart-rate threshold (most important):** resting HR varies a lot between people.
  Note your resting bpm on the watch screen, then set **Heart Rate Threshold** on the
  `SafeSpaceStateMachine` a bit above it (e.g., resting + 15–20 bpm) so intervention
  triggers on genuine elevation. For a quick stage demo, set it *just above* resting
  so a few seconds of stress trips it.
- **Intervention style:** use `ARPassthrough` if you want the real world + puppy to
  fade in (grounding via passthrough); use `SwapObjects` if you want to swap the VR
  hallway for a fully calm VR scene.
- **Broadcast rate:** `BROADCAST_INTERVAL_MS` (watch) defaults to 500 ms. Lower =
  snappier reaction, more packets.
- **Smart Proxy tuning** (stress/sweat/etc. thresholds) lives in the watch's
  `MainActivity.kt`; the formulas are documented in
  [ARCHITECTURE.md §4](ARCHITECTURE.md#4-smart-proxy-methodology).

---

## 10. Troubleshooting

### Watch shows `HR: waiting…` and never updates
- Make sure you **granted the Body sensors permission** (reinstall or re-open the app to be re-prompted).
- Wear the watch **snugly** — a loose watch can't read HR.
- Keep the app **in the foreground** while demoing.

### Headset (and/or dashboard) shows no data
This is almost always **the network**. Check in order:
1. Watch, headset, and laptop are all on the **same Wi-Fi** (Step 0).
2. The watch's `BROADCAST_ADDRESS` **matches your subnet** (e.g., `192.168.1.255`). Rebuild the watch app after changing it.
3. You're **not on isolating Wi-Fi** (hotel/office). Switch to a hotspot/travel router.
4. **Laptop only:** allow **inbound UDP 5000** in the firewall.
5. Quick isolation test: run the **dashboard relay** (Step 4) on the laptop — if the
   relay log prints `[udp] received …`, the watch is broadcasting correctly and the
   problem is on the headset side; if it prints nothing, it's the watch/network.

### Known: headset only reads heart rate (Stress/Sweat show 0)
The watch sends the keys `stress` and `sweat_ml`, but the Unity receiver
(`EpowerXR/Assets/Scripts/BiometricReceiver.cs`) reads `stress_level` and
`sweat_loss`, so those two fields stay `0` on the headset. The demo still works
(intervention is heart-rate-driven). To also use stress on the headset, make the keys
match — easiest is to edit the **watch** payload in `MainActivity.kt`:

```kotlin
put("stress",   stressLevel)   →   put("stress_level", stressLevel)
put("sweat_ml", round1(sweatLoss)) →   put("sweat_loss",  round1(sweatLoss))
```

(The relay/dashboard accept both spellings, so this won't break them.) See
[AUDIT.md §1](AUDIT.md#1-biometric-field-name-mismatch-across-the-wire).

### Scene has pink/magenta materials or missing references in Unity
Re-import assets (right-click `Assets` → **Reimport**), and confirm you opened the
project in **Unity `6000.4.1f1`**. Reassign any missing material from `Assets/Materials/`.

### `./gradlew` fails to install on the watch
- Confirm `adb devices` lists the watch.
- Re-run `adb connect <watch-ip>:<debug-port>` (Wi-Fi debugging can drop).
- Ensure JDK 17 is used (the project targets JVM 17).

### Passthrough / puppy doesn't appear on intervention
- Verify the `SafeSpaceStateMachine` Inspector has **Puppies Environment** (and/or the
  **Objects To Activate/Deactivate**) assigned, and an `ARCameraManager` linked for
  `ARPassthrough` mode.
- Confirm your heart rate actually crossed the **threshold** (watch it on the dashboard or watch face).

---

## 11. Configuration reference

| Setting | Default | Where |
|---|---|---|
| Broadcast address | `192.168.8.255` | `apps/WearOS_Bridge/.../MainActivity.kt` → `BROADCAST_ADDRESS` |
| Biometric port (UDP) | `5000` | watch `BROADCAST_PORT`, headset `BiometricReceiver.port`, relay `UDP_PORT` |
| Broadcast interval | `500 ms` | watch `BROADCAST_INTERVAL_MS` |
| HR intervention threshold | `77 bpm` | `SafeSpaceStateMachine` Inspector → **Heart Rate Threshold** |
| Intervention mode | `ARPassthrough` | `SafeSpaceStateMachine` Inspector → **Intervention Mode** |
| Dashboard WebSocket port | `8080` | `apps/udp-relay` → `WS_PORT` |
| Dashboard WS URL | `ws://localhost:8080` | `apps/web-dashboard/.env` → `VITE_WS_URL` |

### Versions
Unity `6000.4.1f1` · Wear OS app: minSdk 30 (Wear OS 3+), Kotlin 1.9.24, AGP 8.13.2,
JVM 17 · Node 18+ for the dashboard.

---

*Questions or issues running the demo? See the [README](../README.md),
[ARCHITECTURE.md](ARCHITECTURE.md), and [AUDIT.md](AUDIT.md).*
