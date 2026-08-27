# ADR 0006 — İnceleme Paketi Üreticisi

**Tarih:** 2026-08-17
**Durum:** Kabul edildi
**Karar veren:** Claude (Caner projeyi tamamen devretti)
**İlgili:** plan Görev 8 ve Bölüm 4 (geri bildirim protokolü), ADR 0005

## Bağlam

Planın tüm üretim modeli tek bir varsayıma dayanıyor: **Claude kendi çıktısını görerek
yineleyebilir.** Bu varsayım bir alete ihtiyaç duyar — aksi halde "görme" işi Caner'e
düşer ve rol dağılımı çöker.

Aletin iki ayrı işi var:
1. **Kendi kendini düzeltme.** Ben render'a bakar, referansla kıyaslar, düzeltirim.
2. **Caner'e sunum.** Onun notu ("cumba %20 daha derin") bir görüntüye dayanmalı.

Bu ikisi aynı görüntü setiyle karşılanır — yeter ki set **standart** olsun. Sürümler
arası kıyas ancak açı, ışık ve çerçeveleme sabitse anlamlıdır.

## Karar

`tools/blender/render_preview.py`, tek komutla `renders/review/<varlık>_vN/` üretir:

| Dosya | İçerik |
|---|---|
| `01_front` … `04_left` | Dört dik açı — siluet ve oran |
| `05_hero` | 3/4 kahraman açı |
| `06_detail_upper`, `07_detail_base` | Köşeden yakın planlar |
| `08_top` | Ayak izi |
| `contact_sheet.png` | Hepsi tek PNG — **inceleme bunun üstünden yürür** |
| `info.md` | Ölçüler, üçgen sayısı, yeniden üretim komutu |

Sürüm numarası otomatik artar; geri bildirim döngüsü vN → vN+1 üzerine kuruludur.

### Neden bu seçimler

**1,70 m insan figürü her karede durur.** Mimari bir oyunda tek başına en yararlı
inceleme öğesi budur. "Cumba çok derin" ya da "kat alçak" yargısı ancak bir insana göre
verilebilir; metre cinsinden sayı bunu sağlamaz.

**LOD1+ ve `UCX_` gizlenir.** LOD0 ve LOD1 aynı konumda durur; ikisi birden render
edilirse yüzeyler birbirine girer. Kalibre edilmemiş bir görüntü üzerinde alınan her
biçim kararı yanlıştır — bu yüzden ayıklama isteğe bağlı değil, varsayılan.

**Yakın planlar köşeden bakar (±42°).** Düz cepheye dik bakınca çıkma derinliği
okunmaz, siluet düzleşir. Cumba ve saçak ancak köşeden göze çarpar.

**Tepe görünümü azimut 0'da sabittir.** Yüksek yükseliş açısı azimutla birleşince kamera
kendi ekseninde yalpalar ve ayak izi eğri okunur.

**Arka plan koyu, dolgu ışığı güçlü.** Varlık paleti açıktır (kireç badana, kiremit);
siluet ancak koyu fon üzerinde güvenle ayrışır. Dolgu zayıf kalırsa gölgedeki açık
yüzeyler arka plan değerine düşer — bkz. aşağıdaki ders.

**Işık nihai ışık değildir.** Amaç güzellik değil **biçimi okutmak**. Faz 7'nin atmosfer
çalışması bambaşka bir iştir ve Unity tarafında yürüyecek.

**Renk uzayı `Non-Color`.** Kontak sayfası birleştirmesi bir *kompozisyon* işidir, renk
dönüşümü değil. Aksi halde yükle-kaydet turunda gama iki kez uygulanır ve kontak sayfası
kaynak karelerinden farklı görünür.

## Ders: gördüğün kusurun kaynağını varsayma

v2 paketinde ön görünümde üst kat ile alt kat arasında bir **boşluk** göründü; üst kütle
havada duruyor gibiydi. Refleks "geometriyi düzelt" olurdu.

Mesh ölçümü kütlelerin bitişik olduğunu gösterdi (0,00–0,60 / 0,60–3,30 / 3,30–6,00 /
6,00–8,20 m). Kusur aydınlatmadaydı: cumbanın duvara düşürdüğü gölge, beyaz sıvayı tam
olarak arka plan grisinin değerine indiriyordu.

**Ölçmeden yapılacak "düzeltme", doğru modeli bozardı.** Render bir kanıt değil bir
gözlemdir; kanıt ölçümden gelir. Bu, `render_preview.py`'nin `info.md`'ye ölçü tablosu
yazmasının ve damganın boyutları görüntünün üzerine basmasının sebebidir.

Aynı turda bulunan ikinci hata gerçekten üretimdeydi: `hz_blender.join()` mesh'leri
birleştirirken `material_index` değerlerini yeniden eşlemiyor, tüm yüzeyler ilk
malzemeye düşüyordu (model tek renk). **Her iki hata da yalnızca render'a bakıldığı için
bulundu** — planın "görerek yinele" döngüsü ilk kullanımında karşılığını verdi.

## Kapsam dışı (bilinçli)

- **Turntable/GIF.** Plan anıyor; statik açılar şimdilik yetiyor. Animasyon ve sistem
  incelemeleri için Faz 3'te eklenecek.
- **Referans kolajı.** `--ref` parametresi hazır ve kontak sayfasına ek satır olarak
  ekliyor; ancak `refs/` şu an boş — CLAUDE.md gereği lisansı `LICENSES.md`'de
  belgelenmemiş hiçbir görsel indirilmez. İlk referanslar Faz 2 öncesi toplanacak.
- **Otomatik test.** Bu bir Blender aleti; Unity test çerçevesi kapsamaz. Doğrulaması
  çıktının okunmasıdır — nitekim üç kusur böyle bulundu.

## Komut

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\render_preview.py -- `
    --in art\blend\SM_BoxHouse.blend --asset BoxHouse
```

Seçenekler: `--out` (klasörü elle ver), `--lod N`, `--res`, `--samples`,
`--ref <görsel>` (tekrarlanabilir), `--no-human`, `--note "<metin>"`.
