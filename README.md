# DERBY ARENA

Unity 6 (6000.3.27f1, Built-in RP) araç savaşı oyunu · paket `com.lenstedreal.derbyarena` · sürüm 4.0.1 (401)

| Klasör | İçerik |
|---|---|
| `unity-game/` | Unity projesi (Build Automation proje yolu). Belgeler: `BUILD_TR.md`, `CHANGES_TR.md`, `KNOWN_ISSUES_TR.md`, `TECHNICAL_REPORT_4.0_TR.md`, `ASSET_INVENTORY_TR.md` |
| `scripts/` | Varlık üreteçleri (Python): `carforge.py`, `tankforge.py`, `texforge.py`, `glbtex.py`, `crowdforge.py`, `uiforge.py`, `make_icons.py` |
| `docs/` | GitHub Pages: gizlilik, kullanım koşulları, lisanslar (oyun içinden bağlanır), reklam planı |
| `validate_derby.py` | Unity gerektirmeyen statik doğrulama (`python3 validate_derby.py`) |
| `.github/workflows/unity-android.yml` | Push'ta statik doğrulama; APK işi elle |

Tek dal: `main`. Derleme: Unity Build Automation, hedef dalı `main`.
