# ADR 0041 — Okmeydanı: konumu araziden ölçmek, varlıkları dünyaya almak

- **Tarih**: 2026-08-26
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/okmeydani.md`)
- **Bağlam**: Faz 3'ün son S-kademe kalemi. Hezarfen'in **talim yeri**.

## Karar 1 — Konum **araziden ölçüldü** (700 m)

Kataloğumuzdaki nokta, yeşil doku poligonunun **ağırlık merkeziydi** ve
yamaca düşüyordu: 400×400 m içinde **94,1 m** kot yayılımı. Belgelenmiş
**845,66 m**'lik meydan rekoru böyle bir yerde atılamaz.

2×2 km tarandı; en düz nokta **700 m doğuda**: kot 94,5 m, 300×300 m'de
**10,1 m** yayılım, 30° yönünde 900 m'lik koridorda **5,6 m**.
Yeni konum **28,961319 D / 41,055858 K**.

**Bu, önceki üç koordinat hatasından farklı bir türdür.** Doğancılar
(771 m), Üsküdar Mihrimah (164 m) ve İncili Köşk (156 m) ölçülü koordinatı
olan yapılardı ve elle girilen değer yanlıştı. Burada **poligon doğru** —
yeni nokta onun içindedir. Yanlış olan tek şey, bir **alanın** ağırlık
merkezini o alanın temsilcisi saymaktı. Bir meydan bir nokta değildir;
noktası, işlevinin gerçekleşebildiği yerdir.

## Karar 2 — Gerekçe teste bağlandı

`OkmeydaniHasGroundFlatEnoughForTheRecordShot`: meydan çevresinde en az bir
845,66 m'lik koridorda kot farkı 15 m'yi aşmamalı. Konum sessizce kayarsa
test patlar.

### Atlanan test, geçen test gibi görünür

İlk yazımda test araziyi `GameObject.Find("TR_Istanbul")` ile arıyordu ve
**her koşumda atlandı** — test koşucusunun açık sahnesinde arazi yok.
Sonuç yeşil görünüyordu ve hiçbir şeyi korumuyordu. `TerrainData` bir
**varlıktır**; sahneden bağımsız okunur. Test artık gerçekten koşuyor
(187/187, atlanan yok).

## Karar 3 — Yerleştirici **bütün** katalogları tarar

Okmeydanı'nın namazgâhı ve tekkesi Faz 2'de üretilmişti ama kendi
kataloglarındaydı (`art/blend/okmeydani/`). Yerleştirici yalnızca landmark
kataloğunu okuduğu için o varlıklar **yerleştirilebilir değildi** —
türleri ve bildirdikleri yön görünmüyordu. Artık `art/blend/*/catalog.json`
hepsi taranıyor.

## Karar 4 — Namazgâh ve tekke **kıbleye** döner

İkisi de eğime göre dönüyordu. Namazgâh bir **ibadet yeridir**: mihrabı ve
(Okmeydanı'nda) minberi vardır; eğime göre döndürmek onu kıblesiz bırakır,
yani namazgâh olmaktan çıkarır. Tekke de kendi mescidiyle hizalıdır.
İkisi de kıble kuralına eklendi; ölçüldü: 330,4°.

## 1632 kayıtları

* **Okçular (Kemankeş) Tekkesi** — **1624-25**'te **Gürcü Mehmed Paşa**
  mescidi onartıp **minber** ekletti: 1632'de mescit yeni onarılmış ve
  minberlidir. **MİNARESİZDİR** — minare ancak **1770-71**'de eklendi.
* **Minberli namazgâh** — ayakta.
* Meydan bir **yokluk kaydıdır**: II. Bayezid vakfiyesi yapı, mezar, su
  yolu, bağ ve bahçe yapılmasını yasaklar.

## Sonuç

- Sahnede **13 landmark**; boş/gömülü malzeme yok. EditMode **187/187**.
- Tekke (−1143, 96,7, 3331), namazgâh (−1083, 98,5, 3395), aralarında 88 m.

## Açık kalanlar

- **Menzil taşları**: 132 âbide tespit edilmiş, rekor 845,66 m. Varlıklar
  üretilmiş (`MenzilTasi_Bas/_Ayak/_Buyuk`) ama dağıtılmadı — ölçülen 30°
  koridoru boyunca dizilmeleri **Faz 4**'ün prosedürel yerleşimine ait.
- Tekke ile namazgâhın arasındaki 88 m **belgeli değildir**; ikisinin
  gerçek konumu için ölçülü koordinat bulunamadı.
