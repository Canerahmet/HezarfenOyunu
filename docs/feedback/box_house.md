# Geri bildirim günlüğü — Kutu Ev (`BoxHouse`)

Varlık türü: boru hattı doğrulama varlığı (plan Görev 7).
Jeneratör: `tools/blender/gen_box_house.py` · Kanonik kaynak: `art/blend/SM_BoxHouse.blend`
Unity: `Assets/_Project/Art/Prefabs/PF_BoxHouse.prefab`

> Bu dosya geri bildirim protokolünün (plan Bölüm 4) örneğidir. Caner serbest metinle
> not yazar, Claude notu tarih + sürümle buraya işler, uygular ve vN+1 paketini üretir.
> Onay formatı: **"OK vN"**.

---

## v1 — 2026-08-17 · Claude'un kendi incelemesi

Paket: `renders/review/BoxHouse_v1/` (silinebilir; sonraki sürüm geçerli)

**Bulgular (kendi kendine inceleme):**

1. **Model tek renk çıktı.** Palet (taş / kireç badana / aşı kırmızısı ahşap / alaturka
   kiremit) tanımlıydı ama render'da hepsi gri görünüyordu.
   → **Kök neden:** `hz_blender.join()` mesh'leri birleştirirken yüzeylerin
   `material_index` değerlerini yeniden eşlemiyordu; her nesnenin slot listesi 0'dan
   başladığı için bütün yüzeyler ilk malzemeye düştü. **Düzeltildi** (`join()` artık
   malzemeleri yeniden eşliyor).
2. **Tepe görünümü yalpalıyordu** — yüksek yükseliş + azimut birleşince kamera kendi
   ekseninde dönüyor, ayak izi eğri okunuyordu. Tepe görünümü azimut 0'a sabitlendi.
3. **Yakın planlar düz cepheye dik bakıyordu**, çıkma derinliği okunmuyordu. Köşeden
   bakacak şekilde değiştirildi (±42°).

## v2 — 2026-08-17 · Claude'un kendi incelemesi

Paket: `renders/review/BoxHouse_v2/`

**Bulgu:** Ön görünümde üst kat ile alt kat arasında bir **boşluk** görünüyordu — sanki
üst kütle havada duruyordu.

→ **Kök neden geometride DEĞİLDİ.** Mesh kontrolü kütlelerin bitişik olduğunu gösterdi
(taş 0,00–0,60 / sıva 0,60–3,30 / ahşap 3,30–6,00 / çatı 6,00–8,20 m). Sorun
aydınlatmadaydı: cumbanın alt kat duvarına düşürdüğü gölge, beyaz sıvayı tam olarak
arka plan grisinin değerine indiriyordu; göz bunu boşluk olarak okuyordu.

→ **Düzeltme:** dolgu ışığı güçlendirildi, ön dolgu eklendi, arka plan koyulaştırıldı
(açık palet ancak koyu fon üzerinde güvenle okunur), zemin düzlemi 60× büyütüldü
(yan açılarda kadrajı kesen kenar çizgisi "maket" hissi veriyordu).

**Ders:** Bir inceleme render'ında görülen kusurun kaynağı geometri sanılmamalı.
Ölçüyle doğrulanmadan yapılan "modeli düzelt" hamlesi, doğru modeli bozardı.

## v3 — 2026-08-17 · İnceleme bekliyor

Paket: **`renders/review/BoxHouse_v3/contact_sheet.png`**

| Ölçü | Değer |
|---|---|
| Ayak izi | 7,00 × 6,50 m |
| Toplam yükseklik | 8,20 m |
| Taş subasman | 0,60 m |
| Kat yüksekliği | 2,70 m (×2) |
| Cumba çıkması | 0,80 m (sokak cephesi) |
| Yan çıkma | 0,25 m |
| Saçak | 0,70 m |
| Çatı yüksekliği | 2,20 m (kırma) |
| Üçgen | LOD0 44 / LOD1 20 |

**Not:** Bu bir **graybox**tır — doku, pencere, kafes, kapı, baca yok ve olmayacak.
Amacı boru hattını kanıtlamaktı. Gerçek Osmanlı evi Görev 11'de `ottoman_kit.py` ile
gelecek; oranlar hakkında verilecek not (kat yüksekliği, cumba derinliği, saçak,
çatı eğimi) oraya taşınacağı için **şimdi verilse de değerlidir**.

### Caner'in notu

<!-- Serbest metin. Örnek: "cumba %20 daha derin", "saçak yetersiz", "çatı fazla dik".
     Onay: "OK v3" -->

_(bekliyor)_
