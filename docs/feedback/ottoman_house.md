# Geri bildirim günlüğü — Osmanlı konutu (Faz 2 kiti)

Üretici: `tools/blender/lib/ottoman_kit.py` + `gen_ottoman_house.py`
Kanonik kaynak: `art/blend/SM_House_A.blend`
Karar kaydı: [ADR 0012](../decisions/0012-ottoman-kit-materials.md)

İlişki: `box_house` (Görev 7) boru hattının çalıştığını kanıtlayan varlıktı ve
oranları hâlâ **onayını bekliyor** ([box_house.md](box_house.md)). Bu kit o
oranları **parametreye** çevirdi — yani oran kararın geldiğinde tek satır değişir,
yeniden modelleme gerekmez.

---

## v1 (dokulu) — 2026-08-20 · **Caner'in bakması isteniyor**

İnceleme paketleri:

| Paket | Aydınlatma | Ne için |
|---|---|---|
| `renders/review/House_A_v5/` | **nötr stüdyo** (ADR 0006) | oran ve kütle yargısı |
| `renders/review/House_A_HDRI_v2/` | **HDRI, gündüz** | malzeme/gerçekçilik yargısı |

İki kip bilerek ayrı: nötr ışık oranları dürüst gösterir, HDRI malzemeyi.
Nötr kip değiştirilmedi.

### Ölçülen

| | Değer |
|---|---|
| Ayak izi × yükseklik | 8,90 × 8,70 × 8,51 m |
| Üçgen LOD0 / LOD1 / LOD2 | 908 / 56 / 20 |
| Çatı eğimi | 30° → 2,51 m |
| Pivot | taban merkez ✅ (otomatik denetim) |

Dokular Poly Haven **CC0**; hepsi `refs/LICENSES.md`'de üreticileriyle kayıtlı.
UV **gerçek dünya ölçüsünden** hesaplanır — taş 2 m'lik dokusuyla duvarda
gerçekten 2 m kaplar, göz kararı katsayı yok.

### Bu turda ölçümün yakaladığı dört hatam

1. **Kapı, pencerelerin üstüne biniyordu** — 4 pencereden 2'si. Cephe artık tek
   sayıda bölmeye ayrılıyor, ortası kapıya ayrılıyor.
2. **Pencere panelleri ve söveler duvarın içine gömülüyordu** (hiç görünmüyorlardı) —
   cephe yönü işaretini üç yerde ters yazmışım.
3. **Aşı boyası 2,5 kat fazla koyuydu.** Sebep model hatasıydı: boya, altındaki
   tahtanın koyuluğunu taşımaz, örter. Karışım `COLOR`→`MIX` oldu.
4. **Çatı kâğıt gibi inceydi.** 12 üçgenlik alınlık tahtası eklendi.

---

## v2 (yakın plan) — 2026-08-20 · **Caner'in bakması isteniyor**

**Karar 2 cevaplandı: (B) — yakın plana in.** Gerekçe (Caner): *"karakter
İstanbul içerisinde de gezecek sokaklarda, sadece uçma olmayacak; atmosfer o
yüzden gerçekçi olmalı."* Karar kaydı: [ADR 0013](../decisions/0013-near-detail-construction.md).

### İnceleme paketleri

| Paket | Kadraj | Ne için |
|---|---|---|
| `renders/review/House_A_Eye_v3/` | **yaya, 1,65 m** | yakın plan yargısı |
| `renders/review/House_B_Corner_v1/` | yaya | **Karar 1** için köşe evi örneği |
| `renders/review/House_A_v5/` | nötr stüdyo | oran yargısı (değişmedi) |

Yeni kadraj kipi `--eye`: yörünge kameraları evi müze nesnesi gibi gösteriyordu;
yakın plan detayının işe yarayıp yaramadığı ancak yaya kadrajında anlaşılır.

### Ne eklendi

Gerçek delikli duvar (söve derinliği görünür), denizlik, eşik, kapı kanadı,
**saçak altı mertekleri**, subasman silmesi, ahşap karkas dikme + hatıl, mahya,
baca külahı, cumba döşemesi, kapı önü taş basamak.

### Ölçülen

| | House_A (sokak) | House_B (köşe) | House_M (kalabalık) |
|---|---|---|---|
| Üçgen LOD0 | **1 980** | **4 540** | **944** |
| LOD1 / LOD2 | 56 / 20 | 56 / 20 | 56 / 20 |
| Pivot | ✅ | ✅ | ✅ |

**LOD1/LOD2 hiç değişmedi** — yakın plan detayı uzak siluete hiçbir şey ödetmiyor.
Öz-test bunu kilitliyor.

Köşe evi 4 540 üçgen; v1'de verdiğim "(B) ~2 500-3 000" tahminini **aşıyor**.
Sebep üç cephede kafesli pencere. Sokak evi tahmin içinde.

### Bu turda ölçümün yakaladığı üç hatam

Üçü de **uzaktan görünmüyordu**; kademenin gerekçesi tam olarak bu.

1. **Kapı havada asılıydı** — eşik sokaktan 0,74 m yukarıda, önünde hiçbir şey
   yok. Taş basamak eklendi; sayısı yükseltiden türetiliyor, sabit değil.
2. **Ahşap üst kata taş denizlik** konuyordu. Denizlik artık duvarın malzemesini
   izliyor.
3. **Cumbanın altı açıktı** — çıkmanın altından geçen oyuncu evin içini görebilirdi.

Ayrıca sıvanın "düz" göründüğü izlenimim **ölçümle çürüdü** (sapma 24,4 → doku
var). Yeni araç: `tools/blender/measure_render.py`.

### Yeni: Blender öz-testi

`tools/blender/selftest.py` — 5 test, hepsi geçiyor. Duvar panelinin su
geçirmezliği **açık kenar sayısıyla** (tam sayı) ölçülüyor; ilk yazımda hacim
karşılaştırmasıydı ve float32 birikme hatası yüzünden kalıyordu.

---

## ❓ Karar 1 — Yan ve arka cepheler penceresiz kalsın mı? *(hâlâ açık)*

**Artık bir bayrak:** `--facades street | sides | all`. Varsayılan `street`.
Köşe evi örneği: `renders/review/House_B_Corner_v1/`.
Sokakta yürüyen oyuncu için bu karar v1'dekinden daha önemli hâle geldi.

Şu an yalnızca **sokak cephesinde** (−Y) pencere var. Gerekçe: sıkışık mahalle
dokusunda evler bitişik nizamdır ve yan duvarlar komşuyla paylaşılır
(RESEARCH.md: "dar, çıkmaz ve merdivenli sokaklar"). Bu T2 bir çıkarım.

Ama **serbest duran** bir evde üç cephe boş kalıyor ve render'da bu göze çarpıyor.

- **(A) Sokak evi penceresiz kalsın, köşe evi ayrı varyant olsun** — yerleştirme
  hangi evin köşede olduğunu bilir, `--facades sides` ile üretir. Kalabalık
  ucuz kalır (944 üçgen), köşe evi pahalıdır ama azdır. **Önerim hâlâ bu.**
- **(B) Her eve üç cephe** — serbest ev her yerde doğru görünür ama sokak evi de
  4 540 üçgene çıkar; 8 000 ev ölçeğinde bütçeyi zorlar.

## ❓ Karar 3 — Alaturka kiremidin saçak ucu *(yeni)*

Gerçek alaturka çatı saçakta **yuvarlak kiremit uçlarıyla** biter; şu an düz bir
alınlık tahtası var. Yaya gözü yukarı baktığında saçak ucu siluetin en belirgin
çizgisidir.

- **(A) Doku/alfa ile çöz** — trim sheet pasında halledilir, üçgen bedeli yok.
  **Önerim bu.**
- **(B) Geometriyle** — çevre boyunca ~160 parça; tek başına LOD0'ı ikiye katlar.

## ✅ Unity tarafı bitti — 2026-08-20

Karar kaydı: [ADR 0014](../decisions/0014-unity-hdrp-materials.md).
Sahne yakalamaları: `unity/HezarfenGame/Captures/faz2_house_*.png` (gerçek arazi,
Galata kotu 51,96 m).

Ev artık Unity'de HDRP malzemeleriyle, 3 LOD'lu, collider'lı, prefab'lı ve
HistoricalTag'li. Unity'den okunan ölçüler Blender'ınkiyle **birebir**:
8,900 × 8,700 m, pivot tabanda, cumba +Z'ye 0,800 m.
**Testler: EditMode 95/95, PlayMode 9/9.**

Yol boyunca bulunan gerçek hata: **malzeme adı çakışması**. `M_Timber_Dark` üç
farklı boya parametresini, `M_Roof_Alaturka` iki farklı kiremidi gösteriyordu.
Blender bunu sessizce `.001` ekleyerek geçiştiriyordu. Adlar ayrıştırıldı ve
öz-test kilitledi.

## ❓ Karar 4 — Atlas: ölçtüm, gerekmiyor gibi *(plandan sapma)*

Tam kayıt: [ADR 0015](../decisions/0015-atlas-olculdu.md).

Plan "2–3 trim sheet + 1 atlas" istiyor. Gerekçesi ev başına 6 malzemenin
8 000 evde patlaması. **Ölçtüm — patlamıyor:**

| Sokak seviyesi, 1080p | boş | 8 000 + 400 ev |
|---|---|---|
| medyan | 4,83 ms | **5,39 ms** |
| p95 | 7,48 ms | **6,85 ms** (bütçenin %41'i) |
| **setPass** | 31 | **43** |

8 400 ev × 6 malzeme, naif beklentiyle on binlerce bağlama; ölçülen fark
**+12 setPass**. Sebep: 10 malzemenin hepsi aynı shader'ı kullanıyor, SRP
Batcher malzeme değişimini neredeyse bedava yapıyor. Atlas'ın çözdüğü problem
bu mimaride büyük ölçüde zaten çözülmüş.

Bunun yerine **doku tekilleştirmesi** yaptım — aynı derdin ölçülebilir kısmı:
maske ve normal kaynak dokuya ait, role değil. `weathered_planks` dört rolde
kullanıldığı için aynı 2K maske dört kez yazılıyormuş.
**27 dosya / 271,5 MiB → 21 dosya / 188,2 MiB (−%31).**

- **(A) Atlas'ı yapma; ölçümü tetikleyici listesiyle kabul et.** Kazanılan zaman
  "20 kombinasyon + Galata sokağı" sahnesine gider. **Önerim bu.**
- **(B) Yine de yap.** Bedeli dünya ölçekli UV'den vazgeçmek ya da özel shader;
  ikisi de texel yoğunluğu garantisini zayıflatır. Kazancı ölçülemedi.

> Karar bugünün yapılandırmasına ait. Farklı bir shader gerekirse (cam, bitki,
> saydam kafes), malzeme sayısı artarsa, VRAM sıkışırsa ya da hedef donanım
> düşerse **yeniden ölçülür**. ADR 0015 §5 bu tetikleyicileri listeliyor.

## ✅ 20 varyant + Galata sokağı — 2026-08-20

Karar kaydı: [ADR 0016](../decisions/0016-mahalle-dokusu.md).
Araştırma: **RESEARCH.md §4.1** (yeni, kaynaklı).
Bak: `Captures/faz2_sokak_1.png`, `faz2_sokak_2.png`, `faz2_mahalle_ust.png`.

**Caner'in itirazı doğruydu.** Şüphelendiğin ızgara bir ölçüm kurgusuydu, ama
şehir yerleştiricisi yazılsaydı muhtemelen ondan türeyecekti. Araştırma dokunun
**organik** olduğunu doğruladı; ızgara tarihi yarımadaya 19. yy yangın sonrası
düzenlemeleriyle girer. Yerleştirici artık arazinin **eş yükselti eğrisini**
izliyor, cepheler yerel olarak dik duruyor, çıkmazlar dallanıyor.

Ve ölçüm yeni bir kırık buldu: eş yükselti izleyen sokak, evleri yamacın **en
dik yönüne** oturtuyor. Ayak izi altında medyan **3,22 m** kot farkı; 108 evin
**89'u** hem havada hem gömülüydü. Çözüm tarihsel — ev en yüksek köşeye oturur,
altı **taş kaideyle** dolar. Gömülen ev **89 → 0**.

| | |
|---|---|
| Varyant | 20, LOD0 ortalama 2 424 üçgen |
| Mahalle | 108 ev, 4 çıkmaz, 102 taş kaide (1 020 üçgen, tek mesh) |
| Testler | EditMode **101/101**, PlayMode 9/9 |

## ⚠️ Yapılmadı

- **Bu bir şerittir, doku değildir** — tek ana sokak + 4 çıkmaz. Gerçek mahalle
  bir ağdır; Faz 4'ün işi.
- **Mescit çekirdeği yok** — mahalle mescitten dallanır (RESEARCH.md §4.1(g)),
  yerleştirici henüz mescit bilmiyor.
- **Sokak yüzeyi yok** — kaldırım, merdiven basamağı, bahçe duvarı, çeşme yok;
  sokak şu an çıplak arazi.
- **Gayrimüslim palet Unity'de denenmedi** (malzemeler üretildi, o palette ev
  import edilmedi).
- **Basamak collider'a girmiyor** — `UCX_` uçuş için siluetten dar tutuluyor.
  Karakter yürümeye başladığında ayrıca çözülmeli.

---

### Onay biçimi

```
## Caner notu — v2
OK v2        (ya da: düzeltme istekleri)
Karar 3: A / B
Karar 4: A / B        (atlas — plandan sapma)
```

---

## Caner notu — 2026-08-20

> *"Evler bitişik olacaksa yan pencerelere gerek yok. Fakat evler arasında yol,
> sokak ve köşe gibi evlerde pencere olursa iyi olur çünkü oralara da girebiliriz.
> Ama A planı yeterli gibi her iki karar için."*

**Karar 1: A** — sokak evi tek cepheli (944 üçgen), köşe/ara sokak evleri
`--facades sides` ile ayrı üretilir (4 540 üçgen).
**Karar 3: A** — saçak ucu kiremit ağızları doku/alfa ile çözülür, geometriyle değil.

Bu, yerleştirmeye bir **görev** yükler: hangi evin köşede ya da ara sokakta
olduğunu yerleştirici bilmek zorunda. Aksi hâlde karar kâğıt üstünde kalır ve
şehir baştan sona tek cepheli evlerle dolar. Faz 4'ün sokak yerleştiricisi bu
sınıflandırmayı üretmeli — burada not edilmezse sessizce düşer.

*(Karar 2 — yakın plana geç — 2026-08-20'de cevaplandı: B. Bkz. ADR 0013.)*

---
