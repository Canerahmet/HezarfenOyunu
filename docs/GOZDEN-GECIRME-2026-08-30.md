# Baştan sona gözden geçirme — 2026-08-30

Caner'in eklediği üretim hattı planı (**ADR 0078**, `PLAN.md` Bölüm II)
kabul edildikten sonra projenin tamamı bu plana göre ölçüldü. Bu belge
hafızadan değil **ölçümden** yazıldı; her sayı bugün alındı.

---

## 1. Bugün nerede duruyoruz

| alan | ölçü |
|---|---|
| ADR | **79** karar kaydı |
| Test | **368** EditMode + **45** PlayMode — hepsi yeşil |
| Test dosyası | 76 EditMode + 24 PlayMode |
| Runtime C# | 50 dosya, 10 alan (City, Core, Flight, Gis, Player, Streaming, Time, UI, Diagnostics) |
| Semt sahnesi | **8** (Galata, Haliç, Boğaz, Eyüp, Okmeydanı, Suriçi ×2, Üsküdar) + Addressables |
| Ev varyantı | **26** · yerleşen ev **10.868** |
| Landmark | 36 · mahalle 24 · works 15 · sokak 10 · doğa 8 · kilise 7 · Okmeydanı 7 · civic 6 · cami 3 |
| Karakter | 3 durum (gövde, sivil, uçuş) · giyinik **55.168** üçgen |
| Animasyon | **13** klip |
| Işık profili | **5** ayar (GI, sis, poz, tonemap, film grain) |

Bu, "küçük bir prototip" değil. Gerçek DEM üzerine kurulmuş, akışa
alınmış, test edilmiş bir şehir. Planın 51 maddesinin **23'ünün** zaten
karşılığı olması bundandır (ADR 0078, Bölüm 1).

---

## 2. En önemli bulgu: klipler artık karakterin iskeletinden gelmiyor

Ölçülen:

| dosya | zaman |
|---|---|
| `SK_Hezarfen_Ucus@Yurume.fbx` (ve öteki 12 klip) | **11:36** |
| `SK_Hezarfen_Ucus.fbx` (gövde + iskelet) | **22:07** |

Bugün öğleden sonra taban gövde CC0 paket mesh'inden **MPFB2**'ye
geçirildi (ADR 0079). Rig gövdeden ölçüldüğü için iskeletin eklem
konumları da değişti. Klipler ise sabahki iskelete göre çözülmüş
hâllerinde duruyor.

Unity Humanoid ile yeniden hedefleme yaptığı için klipler **oynar** —
kırık bir şey görünmez. Ama:

> `animasyon.json`'daki ayak kayması (yürüme **1,34 cm**, koşma
> **2,75 cm**) Blender'da, **eski** bacak uzunluklarıyla ölçüldü.
> Oyunda oynayan şey yeniden hedeflenmiş harekettir ve onun kayması
> ölçülmemiştir.

Ve testler bunu yakalayamaz: `KarakterTests` `kayma_cm` alanını
**JSON'dan** okuyor. Yani test, oyunun ölçüsünü değil **üreticinin
kendi beyanını** doğruluyor.

Bu, bu projede tekrar eden kusurun bir örneği daha: *bozuk olan
ölçtüğün şey değil, ölçme biçimin.* Sayı doğru, sahibi yanlış yerde.

**Sonuç:** oyun içi ayak kayması ölçen bir PlayMode testi gerekiyor —
karakteri yürüt, ayak yere değdiği karelerde temas noktasının dünyada
ne kadar kaydığını ölç. Bu test yazılmadan hiçbir klip (bizimki de
Mixamo'nunki de) "tamam" sayılamaz.

---

## 3. Açık kusurlar ve riskler — sıralı

### 3.1 ADR 0076 hâlâ açık (yüksek)

13 klibi toptan yeniden üretmek karakteri parçalıyor: kaftan içi boş
bir silindire dönüyor, sarık başın yanında ayrı duruyor, gövde çıplak
kalıyor. **Hiçbir test kırmızı yanmadı** — hepsi sayı okuyordu. Kök
sebep bulunamadı; yürüme ve koşma yeni, kalan on bir klip depodaki
hâlinde bırakıldı.

**Bunun animasyon kararına etkisi doğrudan:** locomotion'ı Mixamo'dan
almak, o on bir klibi yeniden üretme ihtiyacını **ortadan kaldırır**.
Mixamo klibi bizim Blender hattımızdan geçmez; FBX olarak gelir,
Humanoid ile hedeflenir. Yani açık kusurun etrafından dolaşmıyoruz,
onu **gereksiz** kılıyoruz.

Kalan uçuş klipleri (süzülme pozları, kalkış, iniş, çakılma) elle
kalır — süzülüşün mocap'i yok ve olamaz.

### 3.2 APV hiç pişmedi (yüksek, iç mekân gelince kritik)

Sahnede **19 ProbeVolume** referansı var, diskte **veri yok**. ADR
0072'nin temel katmanı hiç çalışmadı. Dışarıda göze batmıyor; kapalı
bir odada sıçrama ışığı olmaması odayı düz ve ölü gösterir. Faz II.D
(iç mekân) başlamadan Faz II.F'nin bu maddesi kapanmalı.

### 3.3 Işık profili beş ayardan ibaret (orta)

Var: GI, sis, poz, tonemap, film grain.
**Yok:** ortam örtme (AO), temas gölgesi, bloom, renk derecelendirme,
vinyet, alan derinliği.

"Sinematik görüntü" isteğinin büyük kısmı tam olarak bu eksik
katmanlardır — model çözünürlüğü değil.

### 3.4 Kayıt dosyası bozuk okunuyor (orta)

Unity konsolunda her oyun oturumunda:

```
[Hezarfen] Kayit okunamadi: JSON parse error: Missing a name for object member.
[Hezarfen] Kayit surumu 0 cok eski (en az 1).
```

Sistem düşüşü zarifçe karşılıyor (varsayılana dönüyor), ama mesaj
diskteki dosyanın **bozuk** olduğunu söylüyor. Faz II.H'de kayıt
kapsamı genişletilirken önce bu doğrulanmalı.

### 3.5 Kalabalık kapalı (planlı)

ADR 0077 ile NPC'ler kapatıldı — kod duruyor. Faz II.G'de İnsan DNA'sı
ile dönecek. Bugün şehir boş; bu bilinen ve kabul edilmiş bir durum.

### 3.6 Küçük artıklar (düşük)

- 20 bahçenin **4'ü** taşma dolgusuna kapalı.
- Kaldırım denetiminde **~%3** kuyruk.
- Mest'in iç kenarında küçük bir deri/ten sızması (v12 render'ında
  görüldü, oynanışta fark edilmez).

---

## 4. Plana göre boşluklar

| Faz | ne eksik | büyüklük |
|---|---|---|
| **II.A** Karakter | Mixamo klipleri + oyun içi kayma ölçümü | küçük |
| **II.B** Arazi öznitelikleri | katman hiç yok; ağaç kusurunun **sebebi** | orta |
| **II.C** Ev çeşitliliği | 26 → ~200; kir/yaşlanma/prop katmanları yok | büyük |
| **II.D** İç mekân | hiç yok; kapı boyalı | **en büyük** |
| **II.E** Su/hava/ses | üçü de yok; deniz düz, oyun sessiz | büyük |
| **II.F** Sinematik | APV + 4 post katmanı | orta |
| **II.G** NPC DNA | DNA yok, Utility AI yok | büyük |
| **II.H** Etkileşim | `IEtkilesim` yok, envanter yok | orta |

---

## 5. Sıradaki üç iş — önerilen sıra

1. **Oyun içi ayak kayması testi** (küçük, hemen). Bölüm 2'deki kör
   noktayı kapatır ve Mixamo kliplerini yargılayacak cetveli önceden
   kurar. Cetvel, ölçeceği şeyden **önce** hazır olmalı.
2. **Faz II.B — arazi öznitelik katmanları.** Ağaçların binaların
   içinden bitmesinin sebebini kapatır; Faz II.C ve II.E'nin de girdisi
   olur (bina kiri yağmura, bitki neme bakacak).
3. **Mixamo klipleri geldiğinde** locomotion'ı devral, ayak kaymasını
   oyunda ölç, ADR 0076'yı "locomotion tarafında konu dışı" diye
   daralt.

Ara verildiğinde oyun her zaman oynanabilir kalır: hiçbir adım şehri
bozmuyor.
