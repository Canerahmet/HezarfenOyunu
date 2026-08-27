# ADR 0060 — Faz kapısı onayı: tek tek mi, kapıda mı?

**Tarih:** 2026-08-27
**Durum:** **B seçildi** (Caner, 2026-08-27 — fiilen)
**Konu:** Geri bildirim protokolünün ölçülen sonucu

## Bağlam

PLAN Bölüm 4'ün geri bildirim protokolü şöyle: Claude her varlık için bir
inceleme paketi üretir, `docs/feedback/<varlık>.md` notunu yazar, Caner
**"OK vN"** yazar ve varlık o sürümde kilitlenir.

Protokol iyi tasarlanmış ve gerekçesi sağlam: insan pasının yerini alan
mekanizma bu, ve onay sohbette değil dosyada durmalı.

## Ölçüm

**36 inceleme notu var. Hiçbiri imzalanmadı.**

Bu bir tahmin değil: `docs/feedback/` altındaki bütün notlar tarandı,
"OK vN" geçen 15 satırın tamamı **şablon metni** çıktı ("Onay formatı:
`OK v1`" gibi). Gerçek imza sıfır.

Üstelik bu ilk kez fark edilmiyor. `docs/feedback/denetim_turu.md` haftalar
önce yazmış: *"13 inceleme notu imzasız. Hiçbiri 'OK vN' almadı."* O gün 13'tü,
bugün 36. Yani eğilim düzelmiyor, hızlanıyor.

**Sonuç:** protokol çalışmıyor. Ve çalışmadığı için Faz 3 kapısı — üretim
tarafı bittiği hâlde — açılamıyor.

## Neden çalışmadığını sanıyorum

Onay birimi yanlış seçilmiş. Caner'in rolü "yalnızca kurulum/onay ve yazılı
geri bildirim" olarak tanımlı; ama 36 ayrı varlığı ayrı ayrı incelemek,
üretimden bağımsız **kendi başına bir iş yükü**. Protokol Caner'e üretim
görevi atamıyor ama *üretim ölçeğinde* bir inceleme yükü atıyor.

Bir de şu var: varlıkların çoğu **birbirinin aynı kararı** taşıyor. Dört
padişah türbesi aynı kit, 192 sur burcu aynı üreteç, beş selâtin camisi aynı
dağarcık. Onları tek tek onaylamak, aynı kararı beş kez sormak demek.

## İki seçenek

### A — Protokol aynı kalsın, borç kapatılsın
36 notu tek tek imzalarsın. Değişmeyen şey: her varlık kendi sürümünde
kilitlenir, izlenebilirlik en yüksek.
**Bedeli:** bugüne kadar üretmediği imzayı bundan sonra üreteceğini
varsaymak. Ve Faz 4 aynı noktada yeniden tıkanır.

### B — Onay birimi **faz kapısı** olsun *(önerim)*
- Kapıda **tek imza**: `OK Faz N`. Kanıtı ben toplarım (test çıktısı,
  üçgen bütçesi, inceleme paketlerinin listesi, bulunan yanlışlar,
  Faz N+1'e bilinçle bırakılanlar) — bkz. `docs/feedback/faz3_kapi.md`.
- Varlık başına tek tek onay **kalkmaz**, ama artık **istisna** olur:
  yalnızca senin işaretlediğin varlıklar için. Kapı paketinde "en çok
  değişen altısı" gibi bir kısa liste sunarım, sen hangisine bakacağını
  seçersin.
- **Tartışmalı kararlar bunun dışındadır** ve tek tek sorulmaya devam eder
  (ADR 0037 uçuş fiziği, 0039 İncili Köşk örtüsü, 0046 kıble, 0051 şadırvan
  kubbesi). Bunlar zaten varlık onayı değil, **tasarım kararı**.
- Notlar yazılmaya devam eder. Değişen tek şey **imzanın nerede
  toplandığı**.

## Önerim

**B.** Gerekçe A'nın kötü olması değil — A denendi ve ölçülen sonucu sıfır.
Bir süreç iki kez ölçülüp iki kez sıfır veriyorsa, sorun uygulayanda değil
tasarımdadır.

B'nin kabul ettiği bedel açık: bir varlık, kimse ayrıca bakmadan kapıdan
geçebilir. Bunu üç şey azaltıyor — inceleme paketi yine de üretiliyor ve
depoda duruyor; sayılan değerler zaten teste bağlı (kapı, gözün değil
testin tuttuğu şeyleri taşıyor); ve tartışmalı olan her şey kapının dışında
tek tek soruluyor.

## Karar

**B.** Caner, Faz 3 kapı paketini okuduktan sonra 36 notu tek tek imzalamadan
*"faz 4'e devam edelim"* dedi — yani onay birimi fiilen **kapı** oldu.

Bu, açıkça yazılmış bir "B'yi seçiyorum" değil; bir davranıştan okunmuş bir
karar. Öyle kaydediyorum ki yanlışsa düzeltilebilsin. Bundan sonra:

- Her faz kapısında `docs/feedback/fazN_kapi.md` üretilir ve **tek imza**
  beklenir.
- Varlık notları yazılmaya **devam eder** (üretim kaydıdır), ama imza
  istisnadır: yalnızca Caner'in işaretlediği varlıklar için.
- Tartışmalı **tasarım** kararları kapının dışındadır ve tek tek sorulmaya
  devam eder. Faz 3'ten devredenler: ADR 0037, 0039, 0046, 0051 — **dördü de
  hâlâ açık** ve kapı onayı bunları kapatmaz.

İlgili: `docs/feedback/faz3_kapi.md`, `docs/feedback/denetim_turu.md`,
PLAN Bölüm 4 (Geri Bildirim Protokolü)
