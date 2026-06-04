# Engineering Audit — EmpowerXR Safe Space

**Audit date:** 2026-06-03
**Scope:** Full repository (`main` plus all remote branches), monorepo across Unity,
Wear OS (Kotlin), Node, and React/TypeScript.
**Audience:** Hackathon organizers and prospective adopters evaluating whether to
build on this project.

This is an honest engineering review of a **hackathon prototype**. It is not a pass/
fail grade — the project achieves an impressive end-to-end vertical slice. The goal
here is to give anyone adopting it a clear, prioritized map of what works, what is
incomplete, and exactly what to fix first.

---

## Executive summary

| Area | Status |
|---|---|
| End-to-end concept (watch → headset → dashboard) | ✅ Demonstrated and working |
| Web dashboard + relay | ✅ Clean, documented, runnable as-is |
| Wear OS broadcaster | ✅ Complete — but on an unmerged branch |
| Sensory sandbox VR content | ✅ Complete — but on an unmerged branch |
| Distributable SDK package | ❌ Empty scaffold |
| Cross-component integration | ⚠️ Field-name mismatch breaks part of the headset path |
| Branch hygiene | ⚠️ Core work scattered across unmerged branches |
| Secrets / security hygiene | ✅ Good (`.gitignore` covers keystores, `.env`, `google-services.json`) |
| Privacy posture (biometric data) | ⚠️ Cleartext LAN broadcast; needs a documented stance |
| Tests / CI | ❌ None |

**Top three things to do before adoption:**
1. **Merge the feature branches** (`feature/wear-os-udp`, `SensorySandbox`) into `main`.
2. **Fix the UDP field-name mismatch** so the Unity headset reads `stress`/`sweat`.
3. **Decide what the SDK package is** — either populate `com.empowerxr.safespace`
   with the real logic, or stop advertising a package and document the in-project
   scripts as the SDK.

---

## Findings

Severity legend: 🔴 high · 🟠 medium · 🟡 low · 🔵 informational.

### 1. Biometric field-name mismatch across the wire

🔴 **High severity.** The Wear OS bridge emits `stress` and `sweat_ml`, but the Unity `BiometricData`
struct (`BiometricReceiver.cs`) deserializes `stress_level` and `sweat_loss`.
`JsonUtility` silently leaves unmatched fields at their default `0`.

**Impact:** On the headset, `stressLevel` and `sweatLoss` stay `0` regardless of the
watch. The `SafeSpaceStateMachine` overwhelm condition `stressLevel > 70` can
therefore never fire from real watch data — only the HR branch (`HR > 77`) works.
The web dashboard is unaffected because the relay's `normalise()` accepts aliases.

**Fix (pick one):**
- Rename the Wear OS JSON keys to `stress_level` / `sweat_loss`, **or**
- Add `[SerializeField]`-friendly aliases / a small remap in `ParseMessage`, **or**
- Route the headset through the relay's canonical schema instead of raw UDP.

Whichever is chosen, **make the relay's canonical schema the single source of truth**
and document it (done in [ARCHITECTURE.md §3.2](ARCHITECTURE.md#32-field-names-and-the-known-mismatch)).

### 2. The SDK package is an empty scaffold

🔴 **High severity.** `packages/com.empowerxr.safespace/Runtime/` and `Editor/` contain only `.gitkeep`.
The README and package `displayName` advertise a "self-contained Unity SDK," but the
actual runtime logic lives in `EpowerXR/Assets/Scripts/` and is **not** part of the
distributable package. An adopter who installs the UPM package gets nothing.

**Impact:** Directly undercuts the "adopt and extend" goal — the headline
deliverable isn't packaged.

**Fix:** Move `BiometricReceiver`, `SafeSpaceStateMachine`, `VignetteController`,
etc. into `packages/com.empowerxr.safespace/Runtime/`, wire up the existing
`Runtime.asmdef`, and have `EpowerXR` and `SensorySandbox` consume them via the
package (the `file:` reference already exists in the sandbox manifest). Add a
package `README.md` and a samples folder.

### 3. Core work scattered across unmerged branches

🟠 **Medium severity.** `main` is missing two of the project's most important pieces:

- The **entire Wear OS app** (`feature/wear-os-udp`) — `main` has only an empty
  `build.gradle` and a `.gitkeep`.
- The **entire sensory sandbox experience** (`SensorySandbox` /
  `SensorySandboxSequel`) — the overstimulation chambers that make the demo
  meaningful. `main`'s `apps/SensorySandbox` is a scaffold.

`feature/intervention-hooks` and `feature/unity-telemetry` show **no diff** against
`main` (stale/empty). `Imad` carries extra effects, partially merged via PR #1.

**Impact:** A reviewer cloning `main` cannot reproduce the full demo and may
conclude the project is far less complete than it actually is.

**Fix:** Consolidate `feature/wear-os-udp` and `SensorySandbox` onto `main` (resolve
the large Unity asset diffs carefully), delete the empty branches, and tag a
release. The [branch map](../README.md#branch-map--where-the-rest-of-the-project-lives)
documents the current state in the meantime.

### 4. Unsafe thread teardown in `BiometricReceiver`

🟠 **Medium severity.**

```csharp
void OnDestroy() {
    receiveThread?.Abort();   // Thread.Abort is obsolete/unsupported on .NET Core/Mono AOT
    udpClient?.Close();
}
```

The receive loop is `while (true) { udpClient.Receive(...) }` with no cancellation
token, and shutdown relies on `Thread.Abort()`, which is deprecated and throws
`PlatformNotSupportedException` on modern runtimes (and on IL2CPP/Android builds).

**Impact:** Potential hang or exception on scene unload / app exit on device.

**Fix:** Close the `UdpClient` first (which unblocks `Receive` with a
`SocketException`), use a `volatile bool running` flag to exit the loop, and
`Join()` the thread — or switch to `UdpClient.ReceiveAsync` with a
`CancellationTokenSource`.

### 5. State-machine thresholds are hardcoded & not data-driven

🟡 **Low severity.** `SafeSpaceStateMachine` hardcodes `heartRateThreshold = 77` and literals
`stressLevel > 70`, `stressLevel < 50`. The richer four-state ladder documented in
the dashboard (`baseline/elevated/alert/intervention`) is **not** implemented on the
headset, where only a two-state toggle exists.

**Impact:** Behavior diverges from the documented model; thresholds can't be tuned
per-user without recompiling. Personal resting HR varies widely, so a fixed 77 bpm
is a poor universal trigger.

**Fix:** Surface thresholds as serialized config (or a ScriptableObject profile),
ideally calibrated to a per-session resting baseline, and implement the four-state
ladder to match the dashboard/SDK contract.

### 6. Smart Proxy divergence between implementations

🟡 **Low severity.** The stress **decay** rule differs:
- Wear OS (`MainActivity.kt`): decays by `5% of (stress − 30)` per tick.
- Dashboard (`smartProxy.ts`) & methodology copy: decays by a flat `0.5` per tick.

Both are reasonable, but the two "sources of truth" disagree, and the on-screen
methodology text describes only one of them.

**Fix:** Pick one decay model, implement it identically in both places, and update
the `MethodologyDrawer` copy to match.

### 7. Privacy & transport posture for biometric data

🟠 **Medium severity (by domain, not by bug).** Biometric telemetry is broadcast **in cleartext over UDP to the whole subnet**, with
no authentication, encryption, or access control. For a hackathon LAN demo this is
fine; for anything resembling real users it is not.

**Impact:** Any device on the network can read a person's heart-rate / stress stream.

**Fix / documentation:** State explicitly that this is a demo and not for real PII;
for production, scope to unicast, add a shared-secret or TLS/WSS, and add a data
retention/consent note. The relay writes every packet to `logs/relay-*.log` on disk
— call that out as well.

### 8. `PassthroughController` is illustrative, not wired to biometrics

🟡 **Low severity.** `PassthoughController.cs` toggles passthrough on a 5-second timer regardless of
state — clearly a spike/demo. The real trigger lives in `SafeSpaceStateMachine`.
Note the filename typo (`Passthough` vs `Passthrough`); the class is correctly named
`PassthroughController`.

**Fix:** Remove or clearly mark as a demo; rename the file to match the class.

### 9. No tests, CI, or linting

🟡 **Low severity.** No unit tests, no CI workflow, no formatter/linter config. The Smart Proxy formulas
and the state ladder are pure functions and would be cheap, high-value units to test
(and would have caught finding #1 and #6).

**Fix:** Add a minimal CI (build the dashboard, lint, run a few Smart Proxy unit
tests). Unity EditMode tests for `SafeSpaceStateMachine` transitions would be valuable.

### 10. Documentation gap (now addressed)

🔵 **Informational.** Before this audit the repo had a single-line `README.md` and no architecture or
run instructions. This pass adds a full [README](../README.md),
[ARCHITECTURE.md](ARCHITECTURE.md), and this audit. Remaining: per-app READMEs
(`udp-relay`, `web-dashboard`, `WearOS_Bridge`) and a populated package README.

---

## What's genuinely good

Worth highlighting for adopters — these are strengths to build on, not just gaps:

- **Clean, idiomatic web code.** The relay and dashboard are well-structured,
  commented, and reconnect/normalize defensively. The µPlot integration (cursor
  sync, sliding window, resize observers) is well done.
- **Thoughtful UX.** Bilingual EN/DE, explicit `LIVE` vs `SMART PROXY` labeling, and
  an in-app methodology drawer that honestly documents every derived value. The
  product is careful not to overclaim — it never pretends derived values are
  measured.
- **Sensible secret hygiene.** `.gitignore` covers keystores, `.env`,
  `google-services.json`, and Unity build artifacts. No secrets found in history.
- **Real, complete components.** The Wear OS app and the sandbox chambers are
  fully implemented (on their branches) — this is more than a mockup.
- **Coherent architecture.** The UDP-broadcast/decoupled-consumers design is a good
  fit: the headset and dashboard are independent, and adding a new consumer is trivial.

---

## Packaging & distribution

- **No packaged build (APK/AAB) is committed**, and none should be — Android
  binaries and Unity build output are intentionally git-ignored. There is no
  pre-built artifact to ship.
- To produce a showcase build:
  - **Headset:** open `EpowerXR` in Unity 6, switch to Android XR, and `Build` to an
    `.apk`/`.aab`.
  - **Watch:** on `feature/wear-os-udp`, `./gradlew assembleRelease` (configure
    `BROADCAST_ADDRESS` first; you'll need a signing config for a non-debug build).
  - **Dashboard:** `npm run build` in `apps/web-dashboard` produces a static `dist/`
    you can host anywhere.

**Recommended submission package for the hackathon:**
1. **Publish the Git repo** (it's already structured for this) and share the link —
   best option, preserves branch history and lets adopters clone/extend.
2. Before sharing, **merge the feature branches** (finding #3) so `main` is the
   complete project, and **tag a release** (e.g. `v0.1.0`).
3. Optionally attach **pre-built artifacts** (dashboard `dist/`, watch APK, headset
   APK) to the GitHub Release, or drop them in the provided Google Drive folder
   alongside the repo link.
4. A short **demo video / GIF** of the watch→headset→dashboard loop will carry the
   showcase far more than any binary.

---

## Suggested adoption roadmap

| Priority | Action | Findings addressed |
|---|---|---|
| P0 | Merge `feature/wear-os-udp` + `SensorySandbox` into `main`; delete empty branches; tag `v0.1.0` | #3 |
| P0 | Fix the UDP field-name mismatch; make the relay schema canonical | #1 |
| P1 | Populate the SDK package with the runtime scripts; add package README + sample | #2, #10 |
| P1 | Harden `BiometricReceiver` shutdown; remove `Thread.Abort` | #4 |
| P1 | Add a privacy/security note; scope the broadcast for non-demo use | #7 |
| P2 | Make thresholds data-driven; implement the four-state ladder on the headset | #5 |
| P2 | Unify the Smart Proxy across watch/dashboard; align methodology copy | #6 |
| P3 | Add CI, Smart Proxy unit tests, per-app READMEs; tidy the demo controller | #8, #9, #10 |

---

*Prepared as part of a documentation pass for hackathon submission. The findings
above reflect the repository state on the audit date; line references point at
`main` unless a branch is named.*
