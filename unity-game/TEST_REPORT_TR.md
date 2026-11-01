# Test raporu — v1.2.0

## Bu ortamda yapılan (gerçek sonuçlar)
| Test | Sonuç |
|---|---|
| `validate_derby.py` (118 kontrol: paketler, parantez dengesi, tip adları, kaldırılan API referansları, sahne GUID, editör ayarları, 10 GLB'nin MiniGlb uyumu, OBJ/PNG) | 118 PASS / 0 FAIL |
| Yeni C# kodundaki her üye/tip adı, derlenmiş v1.1.0 kodu ile karşılaştırıldı; yeni olanlar tek tek Unity API'sine göre elle doğrulandı | yapıldı |
| `System`/`UnityEngine` `Random`/`Object` belirsizliği taraması | 0 bulgu |
| GLB ölçüleri (tekerlek, gövde, taret) koddaki fizik değerleriyle eşleştirildi | yapıldı |

## Yapılamayan (bu ortamda Unity / Android SDK / cihaz yok)
1. Paket çözümleme — **bekliyor** (Build Automation)
2. C# derleme — **bekliyor** (derleyici yok; hata çıkarsa log satırını gönder)
3. Oynanış, kamera, görev, dokunmatik — **bekliyor** (telefon)
4. APK/AAB/IPA imza — **bekliyor**

## Telefonda kabul listesi
- [ ] Menü açılıyor, arkada dönen araç ve arena görünüyor
- [ ] Bölüm 1: geri sayım, araç görünüyor, direksiyon simidi dönüyor ve araç dönüyor
- [ ] Hücre toplanınca sayaç artıyor (+8 sn), 3/3 → yeşil tahliye, 3.5 sn bekle → GÖREV TAMAM
- [ ] ATEŞ yakındaki rakibe kilitleniyor (kırmızı halka), silah ısınınca duruyor
- [ ] ROKET sayısı azalıyor, patlama hasarı veriyor
- [ ] Rakip imha → İMHA mesajı, enkaz yanıyor, 9 sn sonra batıyor
- [ ] Duraklat → Devam / Yeniden / Kontrol / Ses / Ana menü çalışıyor
- [ ] Sonuç → yıldızlar, hurda, Sonraki / Tekrar / Menü
- [ ] Garajda yükseltme alınıyor, bir sonraki bölümde etkisi hissediliyor
- [ ] Bölüm 3 (Kolezyum, Yıkım) ve Bölüm 10 (Patron) açılıyor
- [ ] Ekranda kırmızı/HATA paneli çıkmıyor (çıkarsa ekran görüntüsü at)
