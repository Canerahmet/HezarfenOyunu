# ADR 0022 — Medrese, sebil, fırın: taçkapı damı aşar

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — üretildi, Galata ve Balat'a yerleşti; Caner onayı bekliyor
**Tetikleyen:** ADR 0021 §7'deki Faz 2b listesinin devamı.
**İlgili:** ADR 0020 (han), 0021 (kurşun/vakıf); RESEARCH.md §4.3(d)(e)(f); PLAN.md §7.1

---

## 1. Üç yapı, üç farklı ölçek

| | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|
| `Medrese_A` (dershaneli) | 28,32 × 28,82 | 7,83 | 7 734 |
| `Medrese_B` (dershanesiz) | 22,32 × 19,21 | 5,64 | 6 322 |
| `Sebil_A` | 5,91 × 5,91 | 5,89 | 2 124 |
| `Firin_A` | 8,00 × 12,92 | 8,39 | 486 |
| `Firin_B` | 7,00 × 11,20 | 7,94 | 486 |

Fırının 486 üçgeni yanlış değil: fırın kâgir bir kutu, bir kubbe ve bir
bacadır. Pahalı olan **kemerli açıklık**tır ve onda bir tane var.

## 2. Taçkapı tek kat değildir — doğrulama bunu öğretti

`MedreseParams.validate()` ilk üretimde reddetti:

```
portal_w=2.6 icin en az 5.65 m yukseklik gerekir, medrese tek katli ve 3.90 m
```

Kural ADR 0020'de handan gelmişti ve doğruydu: sivri kemerli kapı yüksek yer
ister (`h ≥ (0,652·w + 0,45)/0,38`). Medrese tek katlı olduğu için kapı sığmadı.

**Ama çözüm kapıyı daraltmak değildi.** Osmanlı medresesinin ve hanının kapısı
zaten ayrı bir kütledir: cepheden **öne taşar** ve **damı aşar**. Adının "tâc"
kapı olmasının sebebi budur. Kısıt yapının katına değil, kapının **kendi
bloğuna** uygulanır:

```
portal_block = (portal_w + 2,2 ,  spring + rise + 0,55 ,  spring)
spring = max(2,60 ; floor_h · 0,62)
```

`validate` artık şunu ölçüyor: blok cepheden en az 0,60 m yüksek olmalı (yoksa
tâc okunmaz) ve iki yanında duvar kalmalı. `Medrese_A`: cephe 3,90 m, taçkapı
bloğu **4,90 m** — 1,00 m taç.

Ders: bir kısıt reddettiğinde, kısıtı gevşetmeden önce **kısıtın hangi nesneye
ait olduğunu** sor. Burada yanlış olan sayı değil, sayının uygulandığı yerdi.

## 3. Medrese: han ile aynı gramer, farklı cümle

İkisi de avlu + revak + kubbeli dam. Kod da ortak: `_arcade`, `_column`,
`_ring`, `_kuyu` artık `civic_kit`te tek nüsha (han'dan çıkarıldı).

Ayıran üç şey ve hepsi siluetten okunur — RESEARCH.md §4.3(f)'deki tablo.
Kısaca: **tek kat**, **hücre başına eşit kubbe + her kubbede baca**, ve o eşit
ritmi kıran tek büyük kubbe: **dershane**.

### Ölçülen: baca ÇİT gibi çıktı

İlk üretimde baca hep `x + r·1,30`'a, yani hep +X'e kaydırılıyordu. Sonuç iki
kusur: sağ sıradaki bacalar **duvarın dışına taştı**, ve dam üstten bir çit
gibi okundu. Ocak dış duvara yaslanır ve baca o duvarın içinden çıkar — yön
hücrenin **kendisinden** gelmeli. Her hücre artık kendi dış duvarının yönünü
taşıyor; bacalara külah da eklendi.

## 4. Sebil çeşme değildir

Aynı şeyi verirler, farklı biçimde: **çeşmeden kendin alırsın, sebilden sana
verilir.** İçeride bir görevli durduğu için sebil bir niş değil küçük bir
odadır, ve her şebekeli pencerenin önünde bardağın uzatıldığı bir **mermer
tezgâh** vardır.

`validate` iki şeyi ölçer:
- tezgâh kotu **0,85–1,20 m** — dışı bardak uzatılacak yükseklik değildir;
- saçak çıkması **≥ 0,60 m** — kısa saçaklı bir sebil, sekizgen bir kuledir.

### Ölçülen: saçağın üstü kurşun olmalı

İlk denemede saçağın üst yüzü `trim` (aşı boyalı ahşap) idi ve render'da yapının
tepesine **kırmızı bir tabak** konmuş gibi duruyordu; külah ile saçak iki ayrı
yapıya aitmiş gibi okunuyordu. Saçak yağmur alan bir yüzeydir ve kubbeyle aynı
malzemeden örülür: **üstü kurşun, altı ahşap**. Ayrıca 0,95 m'lik çıkmayı taşıyan
**ahşap konsollar** eklendi — onlarsız saçak havada duran bir diskti.

## 5. Fırın: yapıyı fırın yapan şey arkasıdır

Cepheden dükkândır. Onu fırın yapan şey arkadaki kâgir **kubbeli ocak** ve
**kalın, yüksek baca**dır. Ocak gövdenin içinde kalsaydı dışarıdan hiçbir
işareti olmazdı.

İki ölçü kodda **sınanıyor**:
- `baca_h ≥ 2,0 m` (damdan yukarısı) — ahşap yoğunluklu dokuda kıvılcım komşu
  çatıya düşmemeli; 1633 Cibali yangınının hatırlattığı risk budur.
- Kemer basma kotu **açıklık genişliğinden türetilir**. İlk yazımda 2,20 m
  sabitti; 2,20 m'lik açıklıkta kemer tepesi 3,63 m çıkıp 3,40 m'lik duvarı
  aştı ve panel reddetti. Fırın kapısı bir geçit değil **tezgâh açıklığıdır**:
  alçak basar, geniş açılır. Bu, §2'nin küçük ölçekteki tekrarı — ve bu sefer
  cevap gerçekten ölçüyü türetmekti, çünkü fırının taçkapısı yok.

## 6. Yerleşim

| Yapı | Kural | Ölçülen (Galata) |
|---|---|---|
| Medrese | vakıf, çekirdeğe yakın; büyükten küçüğe | mescide 45,9 m, en yakın ev 21,8 m |
| Sebil | çekirdeğin **köşesinde** (±12 m) | mescide 18,7 m |
| Fırın | her mahallede, çekirdek çevresinde | mescide 28,6 m, en yakın ev 14,1 m |

**Sebil neden `PlaceBig` ile konmadı:** o yol "çekirdeğe yakın"ı 60 m sayıyor
ve sebil için bu çok geniş. Sebil kalabalığın geçtiği yere, avlu kapısının
yanına konur — mahallenin bir yerine değil. Aday noktalar ±12 m ile sınırlı ve
yerleşme **loglanıyor**; sessiz başarı da sessiz başarısızlık kadar kötüdür.

**Balat'ta medrese ve sebil yok**, fırın var. İkisi de müslüman vakıf
kurumudur (`QuarterSpec.HasVakif`); ekmek ise cemaate göre değişmez.

## 7. Kalan boşluklar

- ~~**Sokak seviyesinde dolaylı aydınlatma yok**~~ → **ADR 0023'te kapandı**
  (geçici takım; ayrıntı enerjisi 0,53 → 2,28). Kalıcı çözüm Faz 7.
  Yerine geçen boşluk: **arazi dokusu**, ışık gelince en zayıf halka oldu.
- İç mekân yok — medresenin dershanesi, fırının ocağı, sebilin içi.
- Sebilin şebekesi düz demir çubuk; gerçekte dökme tunç kafes olurdu.
- Medresenin avlusunda şadırvan yerine kuyu var; büyük medreselerde şadırvan
  olurdu (`street_kit.build_sadirvan` mevcut, yerleştirilmedi).
- Faz 2b listesinde kalanlar: tekke, imaret, bozahane, değirmen, arasta,
  su terazisi, muvakkithane, namazgâh.
