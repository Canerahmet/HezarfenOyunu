# ADR 0048 — Fâtih Camii: bugünkü yapı 1632'de yok

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/fatih_camii.md`)
- **Bağlam**: Faz 3, A-kademe. Faz 3'ün **en büyük** tarihsel farkı.

## Bulgu

Galata Kulesi'nde külah, Adalet Kulesi'nde bir kat, Kız Kulesi'nde
malzeme, Yeni Cami'de bütün yapı değişmişti. Fâtih Camii'nde değişen
**şemanın kendisidir**.

**1766 depremi** camiyi yıktı ve kaynak açık: *"caminin geri kalan kısmı
zemine kadar yıktırıldı"*. Bugünkü barok yapı **1767-71**, mimar Mehmed
Tahir Ağa'dır.

| | 1632 | bugün |
|---|---|---|
| yarım kubbe | **1**, mihrap yönünde | **4** |
| kubbeyi taşıyan ayak | **2** (+ duvarlar) | **4** fil ayağı |
| yan bölümler | **daha alçak**, üçer küçük kubbe | yarım kubbelerle çözülmüş |
| minare şerefe | **birer** | ikişer |

TDV özgün hâli şöyle verir: *"İlk Fâtih Camii'nin ortada bir büyük
kubbesiyle mihrap tarafında **bir yarım kubbesi** ve yanlarda daha alçak
**üçer küçük kubbeli** bölümleri bulunduğu eski resimlerinden
anlaşılmaktadır."*

Yani plan bugünkü gibi **merkezî değil, uzunlamasınadır**.

## Karar 1 — Yarım kubbe sayısı bu projede planı **tanımlar**

Üsküdar Mihrimah **üç**, Süleymaniye ve Ayasofya **iki**, Sultanahmet
**dört**, Fâtih **bir**. Beş yapı, beş plan. Hepsi `kind="selatin"` ve bir
kişi "selâtin camisi böyle yapılır" deyip dördünü aynı sayıya çekse
katalog tutarlı görünürdü.

Teste bağlandı (`FatihCamiiHasTheOriginalSingleHalfDomeScheme`,
`HalfDomeCountDistinguishesThePlans`).

## Karar 2 — Yan bölümler **daha alçak** olmalı

İlk kurulumda yanlar da orta kütle kadar yüksekti ve üçer kubbe çatıda
1,3 m'lik kabarcıklara dönüyordu: **sayı mesh'te vardı ama siluette
yoktu**. Kaynağın sözü "daha alçak"tır ve özgün şemayı bugünkünden ayıran
basamak tam olarak budur — barok yapı yanları yarım kubbelerle çözüp
kütleyi tek parça yapar.

Yan nefler 14 m, orta kütle 21,98 m. Teste bağlandı.

## Karar 3 — Avlunun sayıları **ayakta duran** ölçülerdir

İlk yapıdan bugüne kalanlar: **şadırvan avlusunun üç duvarı**, ortadaki
**şadırvan**, **taçkapı**, **mihrap**, ve minarelerin **şerefe altına
kadar** kaide, pabuç ve gövdeleri.

Bu liste modelin omurgasıdır. Avlunun **on sekiz sütunu**, **yirmi iki
kubbesi** ve **üç kapısı** türetilmiş değil, hâlâ **ayakta duran**
ölçülerdir — harimin aksine. Kataloğun bu ikisini ayırması gerekiyordu ve
ayırıyor.

## Karar 4 — Kilit kotu türetildi ve **D3** işaretlendi

Özgün yapının kilit kotu hiçbir kaynakta yok. Sayı uydurulmadı, **sayılan
bir değerden** türedi: yan neflerin üçer küçük kubbesi (~8,7 m açıklık)
saçağın altında kalmak zorunda → saçak ~22 m → Osmanlı zinciri
**50,5 m** veriyor. `validate` bu ilişkiyi ters yönde de denetliyor.

Harim ölçüleri de aynı yoldan: kubbe 26 + iki yan nef 8,7 = **43,4 m**
genişlik; kaynak "kareye yakın plânlı" der ve 43,4 × 39,0 kareye
yakındır — **türetme kendi kendini denetliyor**.

## Karar 5 — Kubbe çapı bir tarihi de doğruluyor

**26 m** ve *"bir yüzyıl boyunca en büyük kubbe niteliğini
korumuştur"*. 1470'ten Süleymaniye'ye (1557, 26,5 m) **87 yıl** —
"bir yüzyıl" tarifine oturuyor. Test iki yapının sırasını da tutuyor:
Süleymaniye rekoru kırmış olmalı, ama **az farkla**.

1767-71 bu çapı korudu, gerisini korumadı.

## Telif

SALT Research'te 1766 öncesi **plan ve kesit fotoğrafları** var; lisansı
**CC BY-NC-ND** — yalnızca bakılır, kopyalanmaz. Bu model onlardan değil,
**metin** kaynaklarından kuruldu ve şema tarifi TDV'nin kendi cümlesidir.

## Sonuç

- `FatihCamii` LOD0 4 424; ayak izi 50,4 × 86,5 m, yükseklik 57,13 m.
- Doğruluk: kubbe **D2**, şema ve sayılar **D3** (TDV "eski
  resimlerinden anlaşılmaktadır" der — ölçülü çizim değil, tasvir).
- Sahnede **18 landmark**. EditMode **213/213**.

## Açık kalanlar

- **Sahn-ı Semân** medreseleri (caminin iki yanında), tetimme medreseleri,
  darüşşifa, imaret, tabhâne, kervansaray, hamam, kütüphane — hepsi
  1632'de ayakta, hiçbiri üretilmedi (Faz 4). Külliye şehrin en büyüğüydü.
- **Fâtih Sultan Mehmed türbesi** (1481) ve **Gülbahar Hatun türbesi** —
  ikincisi depremi **atlattı** (1767-68'de onarıldı), yani bugün ayakta
  duran gövde 1632'de de oradaydı. Üretilmedi.
- Yapının kendi ekseni ölçülmedi (OSM izi kaba, üstelik 18. yüzyıl
  yapısının izi); şehir kıblesini kullanıyor.
