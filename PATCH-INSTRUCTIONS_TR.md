# Derby Arena 1.0.5 — mevcut repo için güncelleme paketi

Bu arşiv, güncel Godot projesini `wreck-circuit/` altında ve GitHub Actions iş akışını repo kökünde `.github/workflows/android-build.yml` konumunda içerir. Web uygulamasının kök dosyalarını değiştirmek için tasarlanmamıştır.

## Termux

```bash
cd ~/derbyarena97
# Bu zip'i önce Android İndirilenler klasörüne kaydet.
unzip -o ~/storage/downloads/DerbyArena_GitHub_Patch.zip -d ~/derbyarena97
git diff --check
git status --short
git add wreck-circuit .github/workflows/android-build.yml
git commit -m "Upgrade Derby Arena mobile controls and combat"
git push origin main
```

Push sonrasında GitHub > Actions altında `Derby Arena Android APK (wreck-circuit)` çalışmalıdır. Çıktı `wreck-circuit/build/derby-arena-1.0.5.apk` yolundan artifact olarak yüklenir. Godot testleri burada çalıştırılmadı; ilk Actions run doğrulama kapısıdır.
