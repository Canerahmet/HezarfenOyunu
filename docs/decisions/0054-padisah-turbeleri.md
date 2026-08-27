# ADR 0054 — Padişah türbeleri: üç yapı, üç ayrı plan

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/turbeler.md`)
- **Bağlam**: Faz 3, A-kademe'nin **son** kalemi.

## Bulgu — benzeyen yapılar tek şablona inmemeli

Üç padişah türbesi yan yana durur, üçü de kâgir ve kubbelidir, üçü de
Ayasofya haziresindedir. Tek şablonla üretmek kolay olurdu ve katalogda
tutarlı görünürdü. **Kaynaklar üçünün planını ayrı ayrı verir:**

| türbe | tarih | mimar | plan |
|---|---|---|---|
| **II. Selim** | 1577 | **Sinan** | **kare, köşeleri pahlı** (içi sekizgen galerili) |
| **III. Murad** | 1599 | **Dâvud Ağa** + Dalgıç Ahmed | **altıgen**, revaklı, mermer |
| **III. Mehmed** | 1604-1608 | Dalgıç Ahmed → **Sedefkâr Mehmed** | **sekizgen** |
| **Sultan Ahmed** | 1619 | — | kare, revaklı |

Faz 3 boyunca kovaladığım hatanın aynısı: yarım kubbe sayıları
(ADR 0048), minare tipleri (ADR 0045), burç planları (ADR 0049). Her
seferinde "benzer" olan şeyler **ölçüldüğünde ayrışıyor**.

## Karar 1 — Kare-pahlı plan **düzgün değildir** ve bu ölçülür

"Kare, köşeleri pahlı" bir plan, sekiz yüzlüdür ama **düzgün sekizgen
değildir**: dört uzun, dört kısa yüz. Onu düzgün sekizgen yapmak
II. Selim'in planını III. Mehmed'inkiyle **aynı** yapardı.

Katalog `face_spread` (yüz uzunluklarının bağıl yayılımı) kaydediyor:
kare-pahlıda **0,70**, düzgün sekizgende **0,000**. Üretici üç planın
gerçekten ayrıştığını mesh'ten ölçerek denetliyor, ve test de öyle.

Bu, "sayılan değer geometride yaşamalı" kuralının bir adım ötesi: burada
**şeklin kendisi** ölçülebilir bir sayıya bağlandı.

## Karar 2 — Ayrı kit

`mahalle_kit.TurbeParams` mahalle türbesi içindir (3 m yarıçap, 4,6 m
duvar) ve `validate`ı kapalı türbe için `sides in (6, 8)` der. O kural
**mahalle türbesinin olgusudur**; kare-pahlı planı oraya zorlamak,
kuralı taşıdığı olgudan koparmak olurdu — bu hatayı bir kez
`karasur_kit` içinde yaptım ve orada da geri aldım (ADR 0049).

## Karar 3 — Çift kabuk kaydedilir, üretilmez

Üç Ayasofya türbesinin üçü de **çift kubbelidir** (Sinan'ın Kanûnî
türbesinde kullandığı örtü). İç kabuk dışarıdan görünmez ve **mesh'e
girmez** — Ayasofya'nın eksedralarında verilen kararın aynısı
(ADR 0045). Katalog `double_shell: true` yazar.

## Karar 4 — 1632'de hazirede **dört** türbe var, beş değil

**I. Mustafa ve İbrahim türbesi 1639**'dur ve o tarihte Ayasofya'nın
vaftizhânesi hâlâ **yağhânedir** (ADR 0045). Test kataloğun beşinciyi
taşımadığını sınıyor: bir gün biri "Ayasofya'nın türbeleri" diye toptan
eklerse patlar.

## Bir 1632 bağı

**Sultan Ahmed türbesinde yatanların arasında II. Osman da vardır**:
1622'de Yedikule'de öldürülüp buraya gömüldü (ADR 0050). Oyunun yılından
**on yıl** önce, ve tahttaki IV. Murad onun kardeşidir. Yedikule'nin
"Genç Osman Kulesi" ile bu türbe **aynı olayın iki ucudur** ve ikisi de
sahnede duruyor.

## Bu turda düzeltilen

Altıgen planda **−Y'ye bir köşe** düşüyordu ve revak o köşeye
dayanıyordu; render'da yapıya yapıştırılmış gibi duruyordu. Bir revak bir
**yüze** dayanır. Köşe açıları kaydırıldı ve revağın genişliği artık
**ön yüzden türüyor**, elle girilmiyor.

## Sonuç

- `TurbeSelimII` (kare-pahlı), `TurbeMuradIII` (altıgen, revaklı,
  mermer), `TurbeMehmedIII` (sekizgen), `TurbeSultanAhmed` (kare-pahlı,
  revaklı); LOD0 554-938.
- Konumlar **ölçülü** (harita izlerinin merkezleri).
- Sahnede **26 landmark**. EditMode **237/237**.

## Bununla A-kademe **tamamlandı**

## Açık kalanlar

- **Şehzâdeler türbesi** üretilmedi (III. Murad ile II. Selim arasında).
- Ölçüler **D3**: haritadaki ayak izleri (24-30 m) revağı ve hazire
  duvarını da içeriyor, ayrıştırılmadı; gövde ölçüleri tipolojik.
- Hazire duvarı, servi ağaçları ve mezar taşları yok.
- Türbelerin **yönü** ölçülmedi; eğimden türüyor. Gerçekte kapılar
  hazirenin düzenine göre açılır.
