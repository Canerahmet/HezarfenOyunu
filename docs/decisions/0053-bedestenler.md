# ADR 0053 — Bedestenler: üç bağımsız sayı bir geometriyi kapatıyor

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/bedestenler.md`)
- **Bağlam**: Faz 3, A-kademe. Fatih vakfı (~1461); 1632'de ayakta.

## Bulgu — sayılar birbirini kapatıyor

Bedesten bir **ızgaradır**: kubbeler sıra sıra dizilir, kubbeleri taşıyan
ayaklar da ızgaranın **iç düğümlerinde** durur. Yani

```
kubbe = sütun × satır
ayak  = (sütun − 1) × (satır − 1)
```

Kaynaklar üçünü de **ayrı ayrı** verir ve üçü de tutar:

| | ölçü | kubbe | ayak | ızgara | göz |
|---|---|---|---|---|---|
| **Cevahir (İç)** | 45,30 × 29,50 m | **15** | **8** | 5 × 3 | 9,06 × 9,83 m |
| **Sandal** | 40 × 32 m | **20** | **12** | 5 × 4 | 8,00 × 8,00 m |

5×3 = 15 ve 4×2 = 8. 5×4 = 20 ve 4×3 = 12. Dahası ızgara **ölçüyle de**
tutuyor: iki gözün ikisi de kareye yakın, ki kubbeli bir göz zaten kare
ister.

Projede ilk kez **üç bağımsız sayı bir geometriyi kapatıyor**. Şimdiye
kadar sayılar tek tek doğrulanıyordu (96 burç, 10 şerefe, 40 kaburga);
burada birbirlerini doğruluyorlar.

`validate` ilişkiyi denetliyor ve test onu kataloğun kendisinden yeniden
kuruyor: biri değişirse öteki ikisi de değişmek zorunda.

## Karar 1 — Izgaranın **kareliği** bağımsız bir denetimdir

15 kubbeyi 15×1 diye dizmek sayıyı korur, **yapıyı** siler. Göz oranı
0,80–1,25 dışına çıkarsa `validate` reddediyor — ızgaranın yanlış
seçildiğinin işareti.

## Karar 2 — Sandal'ın kilidi Cevahir'den **türedi**

Cevahir'in kubbe kilidi **ölçülü**: 14,89 m. Sandal'ınki kaynakta yok.
Cevahir'in kilit/göz oranından (14,89 / 9,44 = 1,58) türetildi ve
**D3**'tür.

Test ilişkinin **yönünü** de tutuyor: Sandal'ın gözü küçük, dolayısıyla
kubbesi alçak olmalı. Tersine dönerse türetme yanlıştır.

## Karar 3 — 1632'de Kapalıçarşı **bu değildir**

Bugün "Kapalıçarşı" denince akla gelen kâgir tonozlu sokaklar ağı
**sonradır**: bugünkü kâgir örtü büyük yangınlardan (1701) ve **1894
depreminden** sonraki onarımların eseridir. 17. yüzyılda bedestenlerin
arasındaki sokaklar **ahşap** örtülüydü.

Üstelik **1618 yangını** 1632'den yalnızca **on dört yıl** öncedir:
oyunun geçtiği yılda çarşı **yakın zamanda yeniden kurulmuş** bir yerdir.

Bu yüzden yalnızca **iki bedesten** üretildi — onlar kâgirdir, ölçülüdür
ve 1632'de ayaktadır. Çevredeki çarşı dokusu Faz 4'ün işi. Tonozlu
sokakları koymak, Fatih Camii'ne dört yarım kubbe koymakla aynı hata
olurdu (ADR 0048).

## Sonuç

- `CevahirBedesteni` LOD0 2 418 (47,70 × 31,90 × 14,89 m);
  `SandalBedesteni` LOD0 3 168.
- Konumlar **ölçülü** (haritadaki iz merkezleri); ikisi arası ~95 m.
- Sahnede **22 landmark**. EditMode **233/233**.

## Açık kalanlar

- İç ayaklar üretilmedi: dışarıdan görünmezler (Ayasofya'nın
  eksedralarında verilen kararın aynısı, ADR 0045) — ama katalogda
  sayıyla duruyorlar ve ızgarayı onlar kapatıyor.
- Çevredeki **çarşı dokusu** yok: hanlar, sokaklar, ahşap örtüler.
- Bedestenlerin **yönü** ölçülmedi; eğimden türüyor.
