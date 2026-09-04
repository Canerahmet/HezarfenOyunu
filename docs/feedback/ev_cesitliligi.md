# Ev çeşitliliği — kapının üç kriteri, ölçüldü

**Tarih:** 2026-09-04
**Kriterler (plan):** varyant ≥ 150 · yan yana iki özdeş ev **0** ·
kare ≤ 16,7 ms

| kriter | ölçü | durum |
|---|---|---|
| varyant sayısı | **200** (`art/blend/variants/catalog.json`) | ✅ |
| örnek sayısı | **10.900** ev, varyant başına ~55 | — |
| dağılım | en çok kullanılan 560, en az kullanılanlar ~470 — dengeli | ✅ |
| örnek başına ton | `EvTonu` **10.900 evin 10.900'ünde** takılı (parlaklık ±0,10, doygunluk ±0,14, ton ±3,5°) | ✅ |
| **yan yana özdeş** | **628 çift / 10.351 komşulu ev = %6,07** | ❌ |
| kare süresi | 6,2-10,0 ms (on durak) | ✅ |

## Kalan tek kırmızı: yan yana özdeş ev

`tools/olcum/ev_tekrari.py` (bu turda yazıldı):

```
D_Eyup           722 ev,  671 komsulu,  56 ozdes cift  %8,35
D_Galata        2651 ev, 2490 komsulu,   0 ozdes cift  %0,00
D_Surici_Bati   2026 ev, 1939 komsulu, 131 ozdes cift  %6,76
D_Surici_Dogu   3173 ev, 3033 komsulu, 261 ozdes cift  %8,61
D_Uskudar       2328 ev, 2218 komsulu, 180 ozdes cift  %8,12
```

200 varyantla rastgele seçimde beklenen oran **%0,5**; ölçülen %6-8,6
bunun on iki katı. Yani seçim rastgele bile değil, kümelenmiş.

**Ama D_Galata tam sıfır.** Sıfır, `OttomanStreetBuilder`'daki tekrar
engelinin imzasıdır (`KomsudaAyniVar`, yarıçap 15 m; 15 m'de yoksa
12 m'de de yoktur). Kural çalışıyor — öteki dört semt **onunla
kurulmamış**. Kural 2026-08-31'de eklendi, o semtlerin sahneleri sonra
commit'lendi ama yeniden üretilmedi; commit tarihi üretim tarihi değil.

**Yapılacak:** dört semt yeniden kurulacak, araç tekrar koşulacak,
beklenti sıfır. İş Unity tarafında ve fırın koşarken yapılamaz.

## Kayda geçen: ton değişimi bilerek kısık

`EvTonu` parlaklığı ±%10, doygunluğu ±%14, tonu ±3,5° oynatıyor.
Karede evler hâlâ birbirine benziyor ve bu bir **kusur değil, bir
seçim**: 1632 mahallesi aynı kireç, aynı aşı boyası, aynı ocaktan
çıkan kiremitle kuruluyor. Daha güçlü bir değişim mahalleyi
kartpostal yapar. Yine de sayılar burada yazılı; Caner daha fazlasını
isterse üç sayı da tek yerde.

## Onay

_(Caner: "OK vN" ya da düzeltme.)_
