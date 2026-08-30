# ADR 0077 — Kalabalık geçici olarak kapatıldı

- **Durum:** KABUL EDİLDİ (Caner, 2026-08-30)
- **Tarih:** 2026-08-30
- **Bağlam:** Faz 8, oynanış turu
- **İlişki:** Faz 6 (yaşayan şehir), ADR 0070 (rutin), ADR 0071 (takvim)

## İstek

Caner (2026-08-30, oynadıktan sonra):

> "simdilik npcleri kaldir haritaya odaklanalim. npcleri daha guzel bir
> sekilde uretip sonra ekleriz."

## Karar

`OyunSahnesiKur.KalabalikVar = false`. Oyun sahnesinde:

- `SEHIR_NPC` bileşeni **kapalı**, `sakinSayisi = 0`
- `BARK` (replik göstericisi) **kapalı**

Geri açmak tek satır: sabiti `true` yapıp
**Hezarfen → Boru Hatti → Oyun sahnesini kur**.

## Neden silmedim

"Kaldır" denildi ve silmek daha temiz görünürdü. Silmedim çünkü
kalabalığın arkasında duran şey gövdeler değil:

| kalan | ne işe yarar |
|---|---|
| `SokakGrafi` (1.543 düğüm, 5.343 kenar) | yol bulma, iskele, görev hedefleri |
| 15 meslek çizelgesi + `Rutin` | kimin nerede olacağı |
| `BarkKorpusu` — 5.088 replik | şehrin sesi |
| `AranmaSistemi` | asesler, kovalamaca |
| `SehirGunu` | nüfus dağıtımı, tohumdan türeyen sakinler |
| 12 PlayMode + 40 EditMode testi | hepsini koruyan ölçüler |

Bunların hiçbiri gövde çizmiyor; hepsi gövde çizildiğinde **doğru
çizilmesini** sağlıyor. Sildiğim anda geri getirirken yeniden bağlanacak
yedi ayrı bağlantı olurdu ve her biri sessizce yanlış bağlanabilirdi —
bu oturumda tam olarak bu cinsten üç kusur çıktı ("iki sahip, iki
değer").

Bir bayrak, geri dönüşü tek satıra indiriyor ve testler bu arada
sistemi **canlı tutuyor**: kalabalık kapalıyken bile
`NPCYoneticiTests` ve `Faz6DolasimTests` kendi sahnelerini kurup
koşuyor, yani sistem çürümüyor.

## Faz kapısına etkisi

Faz 6'nın kabul ölçütü "şehir yaşıyor"du ve o ölçüm **karşılandı**
(40.000 sakin, vakitlere göre rutin, 60 görünür gövde, replikler).
Kapatma o ölçümü geçersiz kılmaz; sahnedeki bir anahtardır.

Ama dürüst olmak gerekirse: **kalabalık geri gelene kadar Faz 6'nın
çıktısı oyuncunun gördüğü şeyde yok.** Bunu bir "bitti" gibi saymıyorum;
Caner "daha güzel bir şekilde üretip sonra ekleriz" dedi, yani iş
yeniden açılacak.

## Geri geldiğinde ölçülecek (bu turda öğrenildi)

1. **Gövde yüzeye oturuyor mu** — saçılan gövde yatayda kaydırılıp
   yüksekliği düzeltilmezse ortalama 0,63 m havada kalıyor.
2. **Görünür bütçe en yakınlara gidiyor mu** — liste sırasına göre
   seçmek, burnunun dibindeki adamı yok ediyor.
3. **Vakit değişimi kareyi düşürüyor mu** — 40.000 A* tek karede
   birkaç saniyelik donma demek; yenileme kareye yayılmalı.
4. **Yükleme gövdeleri havuza döndürüyor mu** — döndürmezse her F9'da
   60 donmuş heykel birikiyor.

Dördü de bu turda düzeltildi ve kodda duruyor; kalabalık açıldığında
yeniden kazanılması gerekmeyecek.
