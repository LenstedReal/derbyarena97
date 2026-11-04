# Test raporu — v4.0.1 (401)

| Test | Sonuç |
|---|---|
| `python3 -I validate_derby.py`: 344 kontrol (4.0 + 4.0.1: kontrol sistemi, kayıt v6, LOD1 düğümleri, sinematik açılış, kademeli GLB, ses katmanları, CI/BUILD belgeleri) | 344 PASS / 0 FAIL |
| Roslyn anlamsal derleme, 5 yapılandırma: `UNITY_ANDROID,DERBY_ADS,UNITY_EDITOR` · `UNITY_IOS,DERBY_ADS,UNITY_EDITOR` (Editor betikleri dahil) · `UNITY_ANDROID,DERBY_ADS` · `UNITY_IOS,DERBY_ADS` · `UNITY_ANDROID` | 0 hata (yalnız 3 "kullanılmayan alan" uyarısı) |
| Araç GLB'leri + `Body_LOD1` yazılım render'ı (`tools/glbview.py`, LOD0 / LOD1 karşılaştırması) | silüet, lastik ve taret konumları doğru; LOD1 ≈ 930 üçgen |
| Çalışma zamanı mantığı masa başı incelemesi (RectTransform matematiği: kayan pad ve editör sürükleme; giriş akışı: parmak kimlikleri, düğme üstünden geçiş, editör açılış dokunuşu; eğim ekseni: Input System ekran yönü telafisi; editör kapanış yolları; ses perde/ses aralıkları; kayıt v6 taşıma ve boş `ControlPos`) | incelendi, iki düzeltme yapıldı (eğim ekseni `compensateForScreenOrientation`; yuvarlak düğme iki satırlı yazı alanı). Bağımsız ikinci ajan incelemesi API kotası nedeniyle tamamlanamadı — cihaz testi bu açığı kapatır |
| Unity derlemesi, Play Mode, APK/AAB, cihaz testi (kontroller, eğim, ses, kare hızı, bellek) | **bekliyor** — kullanıcı cihaz testi; `PerfLog` çıktısı (`adb logcat -s Unity \| grep DerbyPerf`) ile ölçülür |

Telefonda kabul listesi (4.0.1): `KNOWN_ISSUES_TR.md` §A (A1-A10).

---

# Test raporu — v1.5.0

| Test | Sonuç |
|---|---|
| `validate_derby.py`: 184 kontrol | 184 PASS / 0 FAIL |
| Roslyn C# 9 sözdizimi (36 dosya) | 0 hata |
| Roslyn anlamsal derleme (Unity DLL'leri olmadan; yalnız Unity türlerinden kaynaklananlar ayıklandı): projenin kendi kodundaki ad, metot, argüman sayısı ve kapsam hataları | 0 hata |
| Çevrilmemiş metin taraması (Loc.T dışında Türkçe görünen metin) | yalnız iki dilli dil seçim ekranı ve çeviri tabloları |
| Unity derlemesi ve cihaz testi | **bekliyor** (Build Automation + telefon) |

---

# Test raporu — v1.4.0

| Test | Sonuç |
|---|---|
| `validate_derby.py`: 175 kontrol. Önceki kontrollere ek olarak 20 ikon dosyası + 2 uyarlanabilir XML, gamma uzayında renk düzeltme, yansıma probu, zorunlu piksel ışığı yok | 175 PASS / 0 FAIL |
| Roslyn (C# 9) sözdizimi denetimi, tüm 35 C# dosyası (Unity ile aynı dil sürümü) | 0 hata |
| Yeni Unity API'leri (ReflectionProbe, CanvasGroup, AspectRatioFitter, Display, Screen.SetResolution, Shader.WarmupAllShaders, IPostGenerateGradleAndroidProject) elle doğrulandı | yapıldı |
| İkon maske önizlemesi (daire / squircle / yuvarlak kare) | başlık ve araç her maskede tam görünür |
| Unity derlemesi ve cihaz testi | **bekliyor** (Build Automation + telefon) |

---

# Test raporu — v1.3.0

## Bu ortamda yapılan (gerçek sonuçlar)
| Test | Sonuç |
|---|---|
| `validate_derby.py`: 147 kontrol. Paketler, parantez dengesi, tip adları, sahne GUID, editör ayarları, sürüm 1.3.0/130, ikon boyutları, shader'lar, crowd.glb dahil 11 GLB | 147 PASS / 0 FAIL |
| API karşılaştırması: yeni koddaki her üye/tip adı derlenmiş 1.2.0 koduyla (commit c7ecd39) kıyaslandı, yeni çıkan 63 ad Unity 6 API'sine göre elle doğrulandı | yapıldı |
| Kapsam taraması (CS0136/CS0128, 1.2.0'daki `vel` hatasının türü) | 0 gerçek bulgu (1 alan/parametre gölgelemesi, 1.2.0'dan kalma, geçerli C#) |
| `System`/`UnityEngine` `Random`/`Object` belirsizliği | 0 bulgu |
| İkon maske simülasyonu (daire / squircle / kare): ön plan kartının en uzak noktası 143,4 px < 144 px güvenli yarıçap | taşmıyor |
| Kontrol noktası rotası, asit havuzu ve enkaz konumları bariyer, kule, boru ve doğma noktalarıyla çakışma için hesaplandı | çakışma yok |

## Yapılamayan (bu ortamda Unity, Android SDK ve cihaz yok)
1. C# derleme: **bekliyor** (Build Automation). Hata çıkarsa log satırını gönder.
2. Shader derleme (DerbyPost, DerbyVertexLit): **bekliyor**. Desteklenmezse oyun post-efektsiz ve seyirciler düz renkli çalışır, çökmez.
3. Oynanış, FPS ve dokunmatik: **bekliyor** (telefon)

## Telefonda kabul listesi (1.3.0)
- [ ] Ana ekranda uygulama ikonu Derby Arena görseli, kenarlardan taşmıyor
- [ ] Menüde sağ altta anahtar görsel, bölüm kartlarında 6 renkli görev türü
- [ ] Bölüm 1 yükleme ekranında anahtar görsel ve görev rozeti
- [ ] Bölüm 2 (Kolezyum): tribünde robot seyirciler, zemin düz, takılma yok, mangallar yanıyor
- [ ] Bölüm 4 (Kontrol): sarı kapılar sırayla yanıyor, her kapı +5 sn
- [ ] Bölüm 10 patron: bitirilebilir zorlukta, şok dalgası öncesi kırmızı halka uyarısı
- [ ] Bölüm 13 (Hayatta Kal): süre bitince kazanılıyor
- [ ] Bölüm 15 (Son Kalan): kırmızı bölge daralıyor, dışarıda can gidiyor, "BÖLGE DIŞINDASIN!"
- [ ] Bölüm 21+: yeşil asit havuzları yakıyor
- [ ] Araçlar renkli (oyuncu turuncu, düşmanlar kırmızı, gri, yeşil), siyah siluet değil
- [ ] Drift'te lastik izi, nitroda mavi alev, isabette beyaz flaş
- [ ] Ekranda kırmızı HATA paneli çıkmıyor

---

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
