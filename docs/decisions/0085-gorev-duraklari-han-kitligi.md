# ADR 0085 — Görev durakları han kıtlığına uyarlanır, dünya göreve uydurulmaz

- Tarih: 2026-09-01
- Durum: kabul (ölçümle)
- Bağlam: 8. yorumcu turu

## Sorun

Görev yöneticisi "uygun arketipleri üret, en yakınını ver" kuralıyla
yürüyüşü 3.724 m'den 882 m'ye indirdi — ve **çeşitliliği sıfırladı**:
20 görevin 20'si `Kayip` çıktı. Bir sıralama kuralının birincisi hiç
değişmiyorsa, o kural bir seçim değil bir sabittir.

Rotasyon + yürüyüş tavanı (1.200 m) getirildi; çeşitlilik 1'den 2'ye
çıktı, 3'e çıkmadı. Sebebi ölçüldü — arketip başına kuş uçuşu yol,
doğum noktasından:

| arketip | yol | durak |
|---|---:|---:|
| Kayip | 875 m | 3 |
| KayikYolcu | 1.093 m | 2 |
| Kacakcilik | 1.188 m | 2 (yalnız 1633 Eylül'ünden sonra) |
| **Tedarik** | **1.985 m** | 3 |
| **Teslimat** | **2.731 m** | 3 |

İki uzun arketipin ortak yanı: ikisi de **Han** istiyor ve Galata'nın
yürüyüş bileşeninde **tek han** var.

## İki seçenek

**(a) Dünyaya han ekle.** Reddedildi. `RESEARCH.md` 1632'de büyük
hanların yokluğunu açıkça kayda geçiyor: Büyük Yeni Han ~1761–64,
Büyük Valide Han "muhtemelen henüz yok / tartışmalı", Mısır Çarşısı
1660–64. Han kıtlığı bir kusur değil, **doğru olan durum**. Onu görev
üretecini rahatlatmak için bozmak, oyunun tarihsel iddiasını bir
oynanış kolaylığına satmak olurdu.

**(b) Görevin durak şartını uyarla.** Seçilen.

- `Teslimat`: `Iskele → Han → Dukkan` yerine `Iskele → Dukkan →
  Dukkan`. Arketipin iddiası zaten *"Haliç'te köprü yok, yük sudan
  gelir çarşıya gider"* — ve o iddia hansız da ayakta. İskele
  korunuyor, çünkü iddiayı taşıyan durak o.
- `Tedarik`: `Han → Firin → Dukkan` yerine `Dukkan → Firin → Dukkan`.
  Burada düzeltilen şey bir tasarım değil bir **tutarsızlık**: kodun
  kendi yorumu *"fırından dükkâna, değirmenden fırına"* diyordu, kodu
  ise han istiyordu. Yorum ile kod ayrı şey söylüyorsa ikisinden biri
  yanlıştır; burada yanlış olan koddu.

Han bir role sahip kalıyor: `Kacakcilik` (han → ev) hâlâ onu istiyor
ve 1.188 m ile tavanın altında.

## Sonuç

Ölçüm bu ADR'nin gövdesi: değişiklikten sonraki çeşitlilik ve ortalama
yol `GorevUretimKalitesiTests` içinde kapı olarak duruyor
(`GorevYonetici.YuruyusTavani` = 1.200 m, en az 3 farklı arketip).

## Kural

Bir görev üreteci dünyanın kıtlığına takıldığında, önce kıtlığın
**doğru** olup olmadığı sorulur. Doğruysa uyarlanacak olan görevdir.
