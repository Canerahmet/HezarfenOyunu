# ADR 0079 — MPFB2 tabanına geçişte çıkan altı yanlış cetvel

- **Tarih:** 2026-08-30
- **Durum:** kapandı
- **Bağlam:** Taban gövde CC0 paket mesh'inden **MPFB2 (MakeHuman)**
  parametrik gövdesine geçirildi (ADR 0068'in öngördüğü hat). Kıyafet
  gövdeden türediği ve rig gövdeden ölçüldüğü için taban değişimi
  hattın kırılması değil, tasarlandığı durumdur — ama **hattın bütün
  cetvelleri eski gövdenin duruşuna göre ayarlanmıştı** ve altısı
  sessizce yanlış cevap verdi.

Bu kaydın amacı sayıları saklamak değil, **hangi ölçümün neden yanlış
olduğunu** saklamak. Projedeki tekrar eden ders bir kez daha çıktı:

> Bozuk olan çoğu zaman ölçtüğün şey değil, ölçme biçimin.

---

## 1. Yön: kafadan ölçülemez

`karakter_kit.one_cevir` gövdenin yönünü istatistikle tahmin eder ve
güven 0,50'nin altında **dönmez**. MPFB gövdesinde güven **0,40** çıktı;
yani hem dönüş hem de "burun +Y'de kalmasın" değişmezi atlandı. Bu
proje aynı kusuru bir kez yaşadı: *"Hata ancak oyunda, kamera arkaya
geçtiğinde görüldü."*

Yerine yazdığım ilk cetvel de yanlıştı: kafa bandındaki **en uzak
nokta** burun değil **ense kubbesi** çıktı, çünkü kafa y=0'da merkezli
değil. Ölçüm "+Y" dedi; aynı gövdenin −Y'den alınan render'ı **yüzü**
gösterdi.

**Çözüm:** yön **ayaktan** ölçülüyor (`mpfb_kit._on_yonu`). Ayak
bileğinden parmak ucuna olan mesafe topuğa olanın ~iki katıdır ve bu
her insanda böyledir. Referans gövde merkezi değil **baldırın kendisi**.

## 2. Kol/gövde sınırı: tek eşik yetmiyor

`kiyafet_kit.kol_siniri` tek bir kotta koltuk altı boşluğunu arar.
Kalça kotunda **kol yok** (eller 0,90 m'de bitiyor) ve ölçüm dürüstçe
`None` döndü; sonra elle yazılmış yedek (boyun %11'i) devreye girdi.

Ölçülen |x| profili:

| kot | \|x\| en büyük | ne |
|---|---|---|
| 0,0–0,5 | 0,217–0,240 | **bacak** |
| 0,5–0,9 | 0,186–0,193 | gövde |
| 0,9–1,2 | 0,442–0,538 | **kol** |

Bacak, herhangi bir "kol eşiği"nden daha dışarıda. Üç kusur, tek yanlış
cetvel: entari kolu **baldırı sardı**, şalvar baldırın dışını açıkta
bıraktı, "parmak ucu" diye ölçülen kot (0,074 m) aslında ayak bileğiydi.

**Çözüm:** `kol_ayirici` iki sayı döndürür — eşik ve `z_alt`. Eşik
"en büyük boşluk"tan değil, **kolun ayrı durduğu kotlardaki gövde
genişliğinden** hesaplanır (ilk deneme 0,105 m, ikincisi 0,322 m verdi;
doğrusu 0,171 m).

**Ölçülen kazanç:** giyinik karakter 89.856 → **59.664** üçgen.
Sadeleştirmeyle değil, entarinin artık bacakları sarmamasıyla.

## 3. Uzuv çizgisi: örnekleme boşluğu ≠ yapısal boşluk

`rig_kit.uzuv_cizgisi` "ilk boş dilimde dur" diyordu; gerekçesi
parmak ucu ile ayak arasındaki **55 cm**'lik gerçek boşluktu. Ama
MakeHuman baldırında köşe satırları seyrek: ölçülen boşluklar
2,6 / 4,0 / **4,8** / 3,0 cm, dilim kalınlığı 4,1 cm. Bacak çizgisi
0,238 m'de kesildi, diz kotu 0,297 → 0,346 kaydı, rig denetimi reddetti.

**Çözüm:** tolerans boyun %8'i (≈13,6 cm) — örnekleme boşluğunun üç
katı, yapısal boşluğun dörtte biri. Aradaki açıklık ölçülmüştür.

## 4. Kesit: gövdenin y ekseni ortalanmış değil

`kesit` yarı derinliği `max(|y|)` diye veriyordu. Kalça kotunda kaba et
y = +0,228, karın y ≈ 0 — yerel y ekseni gövdenin **önünden** geçiyor.
`max(|y|)` bu durumda derinliği değil eksenin ne kadar kenarda
olduğunu ölçer.

Bedeli: etek konisi (0,0)'a göre kuruluyordu, kaba eti içine alabilmek
için yarıçapı **0,71 m**'ye çıkıyordu — 1,4 m çapında bir çadır.

**Çözüm:** `kesit_merkezli` merkezi de döndürür; etek ve kuşak o
merkeze göre kurulur, koni **eğik**tir (üst merkez belde, alt merkez
ayaklarda).

## 5. Etek: koninin gövdeyi içerdiği garanti değil

Etek serbest düşen bir konidir (doğru seçim: kumaş bacağı takip etmez)
ama bu, gövdeyi içerdiği anlamına gelmez. Bacaklar açık durduğu için
şalvar eteğin dışına taştı: teal eteğin üstünde **478 yüzlük** kırmızı
mercek lekeleri.

**Çözüm:** `alt_zarf` + `etek_acikligi`. Koni doğrusaldır, her örnek
bir alt sınır verir, gereken en büyüğü alınır. Tarihsel açılma çarpanı
**taban** olarak korunur; hesap yalnız yükseltebilir. Üst uç da
hesaplanır — yoksa belin 5 cm altındaki kalça tabanı patlatır.

## 6. Parça sayısı: eşik neyi saydığını bilmiyordu

Üretim "en az 48 giysi parçası" diye denetleniyordu. Sakal 54 karttan
tek bir kabuğa dönünce denetim, giysinin tamamı yerinde olduğu hâlde
üretimi reddetti.

**Çözüm:** sayı değil **ad** denetleniyor (`zorunlu` kümesi).

---

## Yöntemsel kararlar

- **Sakal kart değil kabuk.** Kartlar düz dikdörtgen oldukları için
  kulaktan kulaga bir **önlük** oluşturuyordu ve arası boşluk olduğu
  için ışık almıyordu. Kabuk çeneyi kendiliğinden izler.
- **Sakala opak malzeme.** Kabuğa kart alfası (tel deseni) uygulanınca
  sakal render'da tamamen kayboldu — maske katı yüzeyi delik deşik
  ediyor. `sac_kit.sakal_material` opaktır.
- **Mest kabuk değil kalıp.** MakeHuman ayağı parmak parmak modellidir;
  kabuk beş parmağı deriye taşıdı, 14 yinelemeli yumuşatma da silmedi.
  Mest ayağın ofseti değil, kendi biçimi olan bir kılıftır. Halkaları
  **o kottaki gerçek ayak kesitinden** ölçülüyor. 8.312 → ~320 üçgen.
- **Makro varsayılan bırakmak bir karardır.** MPFB `gender: 0.5` ile
  gelir; üretilen gövdede göğüs vardı ve entarinin altından okunuyordu.
  `HEZARFEN_MAKRO` yazıldı — **T3**, portre iddiası yok (Hezarfen'in
  çağdaş tasviri yoktur).

## Sonuç

| ölçü | önce | sonra |
|---|---|---|
| Giyinik üçgen (Sivil) | 89.856 | **55.168** |
| Gövde derinliği (giyinik) | 0,77 m | **0,58 m** |
| Şalvarın eteği deldiği yüz | 478 | **0** |
| Sakal kot aralığı | 30 cm | **13 cm** |
| Diz kotu (rig denetimi) | 0,346 ✗ | **0,281 ✓** |

Kapı (Faz II.A): giyinik ≤ 80.000 üçgen — **geçildi**.
