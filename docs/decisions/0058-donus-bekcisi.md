# ADR 0058 — Dönüş bekçisi: "yerine koy, sonra döndür" tuzağı

- **Durum:** kabul (bekçi yazıldı, üç mevcut hata buldu)
- **Tarih:** 2026-08-27
- **Bağlam:** Faz 3 ayrıntı geçişi

## Tuzak

`hz_blender.make_box` (ve kardeşleri) köşe koordinatlarını doğrudan **mesh
verisine** yazar; nesne dönüşümü kimliktir. Bu bilinçli ve doğru bir karar —
docstring'i sebebini söylüyor: *"Böylece 'uygulanmamış ölçek' diye bir durum
hiç doğmaz — FBX'e sızan en yaygın ölçek hatası budur."*

Sinsi sonucu şudur: kutuyu yerine koyup **sonra** `rotation_euler` vermek,
onu kendi merkezi etrafında değil **dünya orijini** etrafında döndürür.
Yedikule'nin kule dibindeki bir konsol, orijinden 80 m uzaktaysa, 22,5°'lik
bir dönüşle otuz metre öteye savrulur.

Doğru sıra: **orijinde kur → döndür → yerine taşı.**

## Neden bir bekçi gerekti

Çünkü tuzak **göze görünür ama yanlış okunur**. Türbe duvarında beliren
beyaz benekleri "mukarnas hücreleri bu ölçekte fazla küçük" diye yorumladım
ve yan nişleri kapattım; belirti azaldı, sebep kaldı. Aynı hatanın ikinci
örneği (`konsol_dizisi`) Galata Kulesi'nde hiç fark edilmedi.

Sonunda onu **bir sayı** ele verdi: Yedikule'nin ayak izi 165,9×161,2 m'den
**173,0×174,6** m'ye çıktı — eklenen 0,4 m'lik konsolların açıklayamayacağı
bir büyüme. Ölçü, gözün göremediğini söyledi.

## Karar

`ottoman_kit._donus_denetimi`, `join_parts`ın içinde her parçayı denetler:

```
dönük bir parçanın mesh merkezi c için   |R·c − c| ≤ 0,35 m
```

Ölçtüğü şey **dönüşün merkezi ne kadar kaçırdığı**dır, merkezin orijine
uzaklığı değil. Fark önemli: dönüş **ekseni üzerinde** duran bir merkez hiç
kıpırdamaz ve o meşrudur. İlk yazdığım bekçi uzaklığa bakıyordu ve iki masum
parçayı suçladı (Yedikule beden duvarı z=7,5'te ve yalnız Z'de dönüyor;
Mihrimah sundurması benzeri). *Bir bekçinin kendisi de ölçülmeli.*

Güvenli deyim `detay_kit.donuk_kutu(name, size, center, rot, col)` olarak
paketlendi.

## Bekçinin bulduğu üç mevcut hata

Otuz üretecin tamamı tarandı:

| yer | ne oluyordu |
|---|---|
| `detay_kit.konsol_dizisi`, `mukarnas_kavsara` | bu geçişte yazıldı; parçalar dünya orijini etrafında savruluyordu |
| `sinan_kit._revak_side` sundurması | Mihrimah'ın **ikinci revağının yan kanat örtüleri** yanlış yerdeydi; 90°'lik dönüş örtüyü de taşıyordu |
| `works_kit` su terazisi künkleri | dönüş kaymasi **iki künke de aynı yönde** uygulanıyordu: gelen ve giden künk simetrik değildi, biri 0,95 m daha dışarı taşıyordu. İki künkün anlamı simetrilerinde |
| `works_kit` değirmen çarkı göbeği | geometri **doğruydu** ama `location`daki elle yazılmış `−0,39` telafisiyle; göbek boyu değişse sessizce bozulurdu |

Son satır ilginç: bekçi yalnız kırık şeyi değil, **kırılgan** şeyi de
yakaladı. Elle telafi edilmiş doğru bir sonuç, doğru olduğu için değil,
tesadüfen doğru.

## Sonuç

Bu, "atlanan test geçen test gibi görünür" (ADR 0041/0043/0044) ve "yeşil
ama bayat" (ADR 0052) ailesinin bir üyesi: **gözün doğruladığı şey
doğrulanmış değildir.** CLAUDE.md'nin kuralı — *"render bir gözlemdir, kanıt
değil: gördüğün kusuru düzeltmeden önce ölç"* — burada bir adım daha ileri
gitti: kural **düzeltmenin kendisi için de** geçerli. Yanlış teşhisle
yapılan düzeltme belirtiyi azaltıp sebebi gizler.

İlgili: [ADR 0057](0057-ayrinti-gecisi.md), [ADR 0052](0052-yesil-ama-bayat.md)
