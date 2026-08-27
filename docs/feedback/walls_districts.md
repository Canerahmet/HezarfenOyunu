# Geri bildirim günlüğü — Sur hatları ve semtler

Üretici: `tools/gis/walls_build.py`, `tools/gis/districts_build.py`
Artefakt: `refs/maps/walls_1632.geojson`, `refs/maps/districts.geojson`
Sahne: `Assets/_Project/Scenes/Faz1_Terrain.unity` → `GIS_1632`
Karar kaydı: [ADR 0011](../decisions/0011-walls-districts-streaming.md)

---

## v1 — 2026-08-19 · **Caner'in kararı gerekiyor (2 madde)**

İnceleme paketi: `renders/review/Map1632_v1/overlay.png` + `info.md`

### Ne ölçüldü

Bunlar iddia değil, üretim sırasında geçirilen denetimlerdir — biri düşseydi
dosya yazılmayacaktı.

| Hat | Ölçülen | Yaygın verilen | Kanıt sınıfı |
|---|---|---|---|
| Kara surları | **5,82 km** | ~5,7 km | **bugün ayakta** |
| Marmara deniz surları | **7,55 km** | ~8,5 km | 1632'de ayakta; çizgi kıyımızdan türedi |
| Haliç deniz surları | **5,25 km** | ~5,5 km | aynı |
| Galata surları | çevre **2,54 km / 37 ha** | ~2800 m / **37 ha** | biçim kaba, **büyüklük belgeli** |

> **2026-08-23 güncellemesi (ADR 0029).** Galata için bir ölçü çapası bulundu:
> Ceneviz surları ~2800 m çevre ile ~**37 ha**'lık bir alanı çevreliyordu.
> Elle çizdiğim halka **53 ha** ölçülüyordu — %43 büyük. Halka, ağırlık
> merkezine göre ölçeklenip belgeli alana oturtuldu. **Güzergâh iddiası hâlâ
> yok**; düzeltilen şey büyüklük. Çevremiz 2,54 km çıkıyor çünkü poligon
> altıgen, gerçek sur ise girintili — alan tutuyor, çevre doğal olarak kısa.

Ölçülenler yaygın verilenlerin biraz altında; çizgilerimiz sadeleştirilmiş
(koy girintileri, burç çıkıntıları yok). Bilerek böyle.

Deniz surlarının kıyıya yapıştırma kayması: Marmara ortanca 86 m, Haliç 35 m.

Üç surun **birlikte kapattığı** halka **1334 ha** ölçülür. Bugünkü Fatih
ilçesi 1562 ha; aradaki fark 20. yüzyıl kıyı dolgularıyla tutarlıdır, yani
beklenen yönde. Bu halka artık yeşil doku katmanının "sur içi" sınırı olarak
da kullanılıyor — ayrı bir kutu yok, iki geometri ayrışamaz (ADR 0029).

---

### ❓ Karar 1 — Galata surları sahnede kalsın mı?

**Sorun:** Galata surları 1860'larda yıkıldı ve elimizde **georeferanslı dönem
planı yok**. RESEARCH.md §3 varlığını ve kapı adlarını (Azapkapı, Kule Kapısı)
belgeliyor ama güzergâh vermiyor.

CLAUDE.md diyor ki: *"Kaynak niteliksel olduğunda metrik geometri UYDURMA."*
O yüzden bir sur hattı çizmedim. Onun yerine **kaba bir çevre poligonu** var:
çevre 3,05 km, kapalı alan 53 ha, Galata Kulesi içinde (bu otomatik denetleniyor —
kule kuzey surun tepesindeydi). Bindirmede **pembe** çizilir, diğer surlardan
ayrı renk, çünkü güveni ayrı.

**Seçenekler:**

- **(A) Kalsın, kaba çevre olarak.** Faz 3'te Galata surları zaten S-kademe hero
  varlık; o zaman dönem gravürlerinden (Lorck 1559, Grelot 1680 — ikisi de kamu
  malı ve `refs/` altında) güzergâh çıkarılıp yerine konur. Şimdilik yerleştirici
  için "burası surlu alan" bilgisi işe yarar. **Önerim bu.**
- **(B) Sahneden çıkarılsın**, Faz 3'te gravürlerden çizilene kadar hiç olmasın.
  Daha temiz ama Faz 2'nin Galata sokağı testi sursuz kurulur.

---

### ❓ Karar 2 — Semt sınırları oynanış olarak doğru mu?

**Bunlar tarihsel mahalle sınırı DEĞİLDİR** ve olamaz: 1632 mahalleleri
kadastral değildi; Vakıf Tahrir Defterleri mahalle *adlarını* verir, sınır
çizgisi vermez. Bunlar plan Faz 1 madde 6'nın **yayın hücreleridir**, hepsi
`Graybox` etiketli. Yani soru tarihsel değil, **oynanış**:

| Bölge | Öncelik | Tekil kara | Ne var |
|---|---|---|---|
| D_Galata | 1 | 409 ha | Kule, Galata surları, Arap Camii, Tophane, Tersane |
| D_Okmeydani | 1 | 580 ha | talim alanı, namazgâh (minaresiz), menzil taşları |
| D_Surici_Dogu | 1 | 410 ha | Ayasofya, Sultanahmet, Topkapı, Süleymaniye, Zulmiyye harabesi |
| D_Uskudar | 1 | 388 ha | Mihrimah, Doğancılar (iniş), Kız Kulesi |
| D_Bogaz | 1 | su | uçuşun geçtiği boğaz, kayık ağı |
| D_Surici_Bati | 2 | 1068 ha | Fatih (özgün şema), Şehzade, Yavuz Selim, kara surları, Yedikule |
| D_Halic | 2 | su | Haliç suyu — **1632'de köprü YOK**, tüm geçiş kayıkla |
| D_Eyup | 3 | 433 ha | dikey dilimde siluet |

**Öncelik-1 toplamı 1786 ha.** Dikey dilimin doldurulacak alanı budur; plan
Bölüm 0'ın *"az semt, dolu semt"* direği bu sayıya bakarak tartılmalı.

Uçuş koridoru (Galata Kulesi → Doğancılar, 3709 m) 100 m'de bir örneklendi;
**38 örneğin tamamı** bir öncelik-1 bölgenin içinde. Yani uçuş boyunca yüklü
olmayan hücre yok.

**Bakılması istenen:** öncelik dağılımı doğru mu? Özellikle:
- **Okmeydanı öncelik-1** yapıldı çünkü uçuş talimleri orada geçiyor (plan Bölüm 8
  S-kademe). Sen dikey dilimde Okmeydanı istemiyorsan öncelik-2'ye iner ve
  bütçe 1786 → 1206 ha'ya düşer.
- **Suriçi-Batı 1068 ha ile en büyük bölge** ve öncelik-2. 1632'de burası daha
  seyrek, bostanlı ve mezarlıklıydı; yine de doldurulacak en büyük alan.

---

### Bu turda düzeltilen üç kendi hatam

Denetimlerin ne işe yaradığını göstermek için:

1. **Haliç sur izimi yanlıştı.** Cibali/Fener/Balat'ı kıyıdan içeri, güneye
   yazmışım. Yapıştırma kayması ortanca 241 m / en çok 668 m çıkınca denetim
   üretimi durdurdu. Düzeltince 35 m.
2. **Su bölgeleri kara yutuyordu.** `D_Halic` %62 kara raporluyordu; Faz 4
   bütçesi aynı araziyi iki kez sayacaktı. Poligonlar suya daraltıldı (%25) ve
   alan ölçümü çakışmasız hâle getirildi. Aynı ölçüm `D_Okmeydani`nin
   `D_Eyup`un 268 ha karasını sahiplendiğini de gösterdi.
3. **İnceleme bindirmesi dikeyde tersti** — Marmara kuzeyde görünüyordu.
   **Aynı hata daha önceki kıyı bindirmesinde de vardı**, yani `v1` kıyı
   incelemesinde baktığın `preview_coastline.png` de tersti. İkisi de
   düzeltildi ve yeniden üretildi. Kıyı kararın (v2) bundan etkilenmiyor —
   karar sayılara ve düzeltme alanlarının *yerine* dayanıyordu, ama yine de
   bilmen gerek.

---

### Onay biçimi

Bu dosyanın altına yaz:

```
## Caner notu — v1
OK v1        (ya da: düzeltme istekleri)
Karar 1: A / B
Karar 2: (öncelik değişiklikleri varsa)
```

---
