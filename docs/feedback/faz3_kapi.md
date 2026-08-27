# Faz 3 — Kapı Paketi

**Durum:** onay bekliyor
**Tarih:** 2026-08-27
**Ne için:** PLAN'ın kuralı — *"her faz kapısında Claude Code, kabul
kriterleri karşılandığına dair kanıt (test çıktısı + inceleme paketi) sunar;
Caner onaylarsa sonraki faza geçilir."* Bu, o kanıt.

---

## 1. Söz verilen ne yapıldı

**S-kademe** (uçuş ekseni, dikey dilim için zorunlu) ve **A-kademe**
(suriçi genişlemesi) — PLAN Bölüm 8'in tablosunda **tek satır eksiksiz**:
Galata Kulesi ve surları, Okmeydanı, İncili Köşk, Kız Kulesi, Üsküdar
Mihrimah + külliye + iskele, Doğancılar, Topkapı belirleyicileri + Alay
Köşkü, Süleymaniye, Ayasofya, Yeni Cami harabesi, Sultanahmet, Fâtih,
Beyazıt + iki bedesten, kara surları + 7 kapı + Yedikule, dört padişah
türbesi.

**36 anıt varlığı, 475 901 üçgen.** Hepsi `tier: T1` (varlığı belgeli).
Doğruluk: **14 D2** (dönem görselinden oran), **22 D3** (tipolojik).
D1 (ölçülü çizim) yok ve olmayacak — o çizimlerin çoğu telifli.

---

## 2. Kanıt

| ne | değer |
|---|---|
| EditMode testi | **241 / 241 yeşil** |
| `Assets/_Import` | **boş** (test bunu zorluyor) |
| Sahnedeki anıt | 28 yerleştirildi, koordinat denetimi **0 şikâyet** |
| İnceleme paketi | 36 varlığın **hepsinde** var (`renders/review/`) |
| ADR | 59 karar kaydı |
| Sürüm kontrolü | git + LFS, 24 commit, `faz3-ayrinti` etiketli |

Testlerin bir kısmı fonksiyon değil **olgu** koruyor: 1 birim = 1 metre,
Blender→Unity eksen dönüşümü, Sultanahmet'in altı minaresi ve on altı
şerefesi, Fâtih'in on sekiz avlu sütunu ve yirmi iki kubbesi, kapalı revak
halkasında göz − sütun = 4.

Üç bekçi de kendi hatalarımızdan doğdu ve onları bir daha sessiz bırakmıyor:
derlenmemiş test assembly'si (ADR 0052), yok sayılan varlığın geçmiş sayılması
(0041/0043/0044), dünya orijini etrafında dönen parça (0058).

---

## 3. Bu fazın gerçek kazancı: bulunan yanlışlar

Kapıda asıl bakılması gereken şey üçgen sayısı değil, **kaç yanlışın
yakalandığı**. Hiçbiri gözle bulunmadı; hepsi ölçümle:

- **Kıble 16,7° yanlıştı.** Her cami büyük daire formülüyle 150,40°'ye
  döndürülmüştü — doğru bir sayı, ama 1632'nin camileri oraya bakmıyor. On
  tarihî cami ölçüldü, medyan sapma −16,6°. Şehirdeki yedi cami döndü.
- **Beş konum hatası** (771 m, 700 m, 164 m, 156 m, 148 m) — arazi
  koordinatın tanığı yapıldı.
- **Fâtih 1766 öncesi şemayla** kuruldu: bugünkü barok yapı 1767-71.
- **Yeni Cami harabe**, Galata Kulesi bugünkünden alçak, Kız Kulesi ahşap,
  Alay Köşkü ise bugünkünden **yüksek** — farkın yönü de soruldu.
- Ayrıntı geçişinde altı ölçüm hatası daha (revak gözü ters yönde, örtü
  duvarı 1,4 m aşıyor, kemer saçağı deliyor, köşe sütunçesi duvarın içinde,
  Yedikule'nin ayak izi 7×13 m büyümüş, `data/` kuralı 28 dosyayı yutmuş).

---

## 4. Faz 4'e bilinçli bırakılanlar

Bunlar eksik değil, **kapsam dışı**:

- Sur-ı Sultanî ve Topkapı'nın kütle denizi
- Menzil taşları (132 âbide üretildi, dağıtılmadı)
- Galata surlarının hendeği
- Doğancılar meydanının zemini ve çınarları
- Kapalıçarşı'nın sokak örtüsü (1632'de **ahşap**, kâgir tonoz 1701 sonrası)
- İmaret-tabhâne ve Kurşunlu Han — 1632'de ayaktalar ama **yerleri bilinmiyor**

---

## 5. Senden gereken

### 5a. Dört açık karar (üretimi bekletiyor)

| ADR | soru | benim önerim |
|---|---|---|
| **0037** | Uçuş fizik ayarı — Doğancılar'a 3336 m için gereken süzülme 64,6:1, gerçek 11,56:1. Rüzgâr çözmüyor (205 km/h gerekir); gereken ortalama yükselen hava ~0,9 m/s | Yükselen havayı **mekanik** yap (termik değil) |
| **0039** | İncili Köşk örtüsü — TDV kubbe der, Eldem ahşap. İki varyant da üretildi | Kararı **sen** ver; ikisi de hazır |
| **0046** | 1632 kıblesi 133,70° — şehirdeki bütün camileri döndürdü | **Onayla**; on caminin ölçümü ve 2009 yapısındaki +0,04° kontrolü kayıtlı |
| **0051** | Beyazıt şadırvan kubbesi — IV. Murad ekletti (1623-40), oyunun yılı tam ortası | **Konmasın**; Murad gerçek iktidarı 1632'de aldı |

### 5b. Kapı onayı — ve bir süreç sorusu

**36 inceleme notunun hiçbiri imzalanmadı.** Bu bir sitem değil, bir ölçüm:
varlık başına tek tek onay isteyen protokol haftalardır sıfır imza üretti.
Notlarımdan biri bunu zaten yazmış: *"13 inceleme notu imzasız."* Bugün 36.

Protokol senin kararın, o yüzden iki seçenek ve bir öneri yazdım:
→ **[ADR 0060](../decisions/0060-faz-kapisi-onayi.md)**

Kısaca: ya 36 notu tek tek imzalarsın, ya da **kapıyı tek imzayla** geçip
tek tek onayı yalnızca *senin işaretlediğin* varlıklara saklarız. Önerim
ikincisi — çünkü birincisi denendi ve çalışmadı.

---

## 6. Onay

Bu kapı için beklediğim tek satır:

```
OK Faz 3
```

…ya da düzeltilmesini istediğin varlıkların listesi. Hangi varlığa
bakacağını seçmek istersen, en çok değişen altısı şunlar:

| varlık | paket |
|---|---|
| Sultanahmet | `renders/review/Sultanahmet_v4/` |
| Süleymaniye | `renders/review/Suleymaniye_v8/` |
| Ayasofya | `renders/review/Ayasofya_v4/` |
| Fâtih Camii | `renders/review/FatihCamii_v4/` |
| Sultan Ahmed türbesi | `renders/review/TurbeSultanAhmed_v4/` |
| Bâbüsselâm | `renders/review/TopkapiBabusselam_v3/` |

Her karede 1,70 m'lik ölçek figürü var.
