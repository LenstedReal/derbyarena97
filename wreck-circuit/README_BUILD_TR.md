# Derby Arena 1.0.5 — Android build

- Application name: Derby Arena
- Version name: 1.0.5
- Version code: 105
- Android application ID: `com.lenstedreal.derbyarena`
- Engine: Godot 4.7.2 Stable, GDScript, Mobile renderer
- Target: Android arm64-v8a debug APK

## Build through GitHub Actions

The standalone source archive keeps the workflow at `.github/workflows/android-build.yml` at repository root. Commit and push it to `main`, then open Actions and run **Derby Arena Android APK**. The workflow imports GLB resources, writes Godot Android SDK/JDK paths into editor settings, runs the smoke test, exports the debug APK, and uploads `derby-arena-1.0.5-debug-apk`. A patch archive for the `derbyarena97` web repo instead runs Godot with `--path wreck-circuit` and stores the artifact from `wreck-circuit/build/`.

The Android APK is a debug build for testing, not a signed production Play Store release. Do not overwrite a production install using a different signing key.

## Assets

Vehicle GLBs, arena layout, crowd, pickups and VFX are copied from the user-provided companion `Derby-arena` repository. Check the original repository's asset licenses before redistributing publicly. Godot currently uses the detailed vehicle models and crowd tiers; remaining GLBs are included for future integration.

## Honest build status

Bu kaynak paketindeki değişiklikler bu çalışma ortamında Godot editörüyle derlenip çalıştırılamadı; burada Godot ikilisi mevcut değildi ve ağdan indirme yapılamadı. GitHub Actions workflow’u import, smoke test ve APK export aşamalarını yapacak şekilde yapılandırılmıştır. İlk çalıştırmanın sonucu Actions günlükleriyle doğrulanmalıdır; test edilmemiş bir derlemenin başarılı olduğu iddia edilmez.
