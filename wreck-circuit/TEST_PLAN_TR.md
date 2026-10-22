# Derby Arena 1.0.5 — Test plan and verification status

## Automated GitHub Actions smoke checks

`tests/smoke.gd` is configured to check:

- Main menu and Play/Garage buttons load.
- Arena loads with one player, four AI opponents, an initialized player collider, and at least five GLB pickups.
- Colosseum and crowd GLB assets import.
- Mobile control zones exist (steering, fire, nitro, brake/reverse, drift).
- Weapon and projectile pool initialize, a projectile can be spawned, and repair pickup restores health.
- On-screen mouse controls route into the same control state rather than silently using the keyboard path.
- Player vehicle accelerates past the minimum smoke-test speed threshold across 80 physics frames.

## Manual Android checks still required

- [ ] Install and launch on a real arm64 Android device in landscape full-screen.
- [ ] Verify the safe-area margins on a notched phone and a 16:9 screen.
- [ ] Hold fire while steering with a second finger; confirm auto-aim chooses the nearest live enemy.
- [ ] Tap nitro once; confirm one short burst and that holding the button does not auto-retrigger after cooldown.
- [ ] Hold Brake / Reverse to slow down, stop, then back up.
- [ ] Test Drift at medium speed and verify the car does not spin uncontrollably.
- [ ] Check vehicle collisions, projectile impacts, pickup effects, health, death, win/lose results, pause/resume and restart.
- [ ] Background/resume the app; confirm controls do not remain stuck and audio state recovers.
- [ ] Test Low/Medium/High on a lower-end device and verify stable frame pacing.

## Honest build status

The current execution environment does not have the Godot executable and cannot fetch it from GitHub, so these latest source edits have **not** been compiled or run here. The workflow is configured to import assets, run the smoke tests, export the debug APK and upload it as an artifact. Treat the first GitHub Actions run as the authoritative compile/test result; any error log should be fixed before calling the build successful.

The generated APK will be a debug build for testing. A production Play Store AAB requires a separately managed release keystore, signed release export, and device/store validation.
