# Kotlin (native-android) vs Unity — karşılaştırma ve birleştirme kararı

Kaynak: yedekteki tüm git geçmişi (19 commit, `main` + `unity/blackout-run`), `native-android/` (9.4k satır Kotlin),
`wreck-circuit/` (Godot, 3.1k satır), web (`src/`, 9k satır TS) ve `unity-game/` (3.8k satır C#), ayrıca 2 oynanış videosu.

## Geçmiş (kısa)
Grok web dışa aktarımı → Godot `wreck-circuit` (1.0.5) → Kotlin + Compose + Filament (`native-android`, 450 bölüm, Kolezyum)
→ Kotlin içinde "Blackout Run" yeniden tasarımı → Unity 6 (`unity-game`, URP + glTFast) → URP/glTFast kırılmaları (siyah ekran,
CS0103) → v1.1.0 Built-in pipeline (açıldı, oynanabilir ama "demo") → **v1.2.0 (bu sürüm)**.

## Kotlin sürümünün güçlü yanları (videoda görünen)
| Özellik | Kotlin | Unity v1.1.0 | v1.2.0 |
|---|---|---|---|
| Menü, bölüm seçimi, ilerleme | 450 bölüm, 5 şampiyonluk, yıldız, hurda | yok | **50 bölüm, 5 bölüm grubu, yıldız, hurda, kayıt** |
| Garaj / yükseltme | 4 yükseltme | yok | **5 yükseltme × 5 seviye** |
| Arena kimliği | Roma Kolezyumu (tribün, sütunlar, hurda araçlar) | sadece Spillway | **Spillway + Kolezyum (Kotlin modeli taşındı)** |
| HUD | radar, segmentli barlar, silah/ısı, KILLS/ALIVE | düz renkli kutular | **radar, can/kalkan/nitro/ısı, nişangâh, patron barı, mesaj akışı** |
| Ganimet | 6 tür | 4 silindir | **4 tür, Kotlin sandık modelleri, yeniden doğma** |
| Görev çeşitliliği | battle royale bölgesi | tek görev | **reaktör + yıkım + patron** |

## Kotlin sürümünün zayıf yanları (neden motor olarak devam etmedi)
* Fizik 2D: dünya `x,y` düzleminde, 3D sadece görüntü. Rampa, köprü, gerçek çarpışma yok.
* Hasar yarışları (`PHASE_A_DEATH_DIAGNOSIS.md`): can değeri iki yerden yazılıyordu, 5 düşman aynı anda ateşle 3-4 sn'de öldürüyordu.
* 450 bölüm tamamen formül; içerik farkı yok. Kalite yerine sayı.
* Filament + Compose ile kendi motorunu yazmak: gölge, parçacık, ses, fizik hep elle. iOS yok.
* Battle royale bölgesi ekranı kırmızıya boğuyor (videoda arenanın yarısı kırmızı).

## Unity'nin güçlü yanları
Gerçek 3D fizik (raycast süspansiyon, çarpışma), gölge/sis/gökyüzü, parçacık, 3D ses, tek kod tabanından Android + iOS,
Unity Build Automation ile telefondan derleme.

## Karar
Motor **Unity**. Kotlin'den alınanlar: menü/bölüm/garaj/ilerleme yapısı, Kolezyum arenası (GLB), ganimet modelleri, HUD
bilgi yoğunluğu, adil düşman ateş temposu ve spawn koruması (Phase A teşhisi). Alınmayanlar: 450 bölüm (50 anlamlı bölüm),
battle royale bölgesi (tam ekran kırmızı, mobilde okunaksız), 2D fizik.

## Battle royale zor mu?
Tek oyunculu "daralan bölge + son hayatta kalan" modu bu altyapıyla 1-2 iterasyonluk iş (bölge halkası + bölge hasarı +
8-12 yapay zekâ). Gerçek çok oyunculu battle royale (sunucu, eşleştirme, ağ senkronu, hile koruması) ayrı ve büyük bir
proje: sunucu maliyeti ve aylar. Önerim: önce bu kampanyayı cihazda oturt, sonra tek oyunculu "Son Hayatta Kalan" modu.
