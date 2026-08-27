# ADR 0039 — İncili Köşk: konumun kıyıdan türetilmesi ve tartışmalı örtü

- **Tarih**: 2026-08-25
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/incili_kosk.md`)
- **Bağlam**: Faz 3, S-kademe. **IV. Murad uçuşu buradan izledi** (Evliya).

## Karar 1 — Konum kıyı çizgisinden **ölçülerek** türetildi (156 m düzeltme)

Katalog değeri (28,9866 / 41,0135) elle girilmişti ve yapıyı **denizden
125 m içeride, 14,7 m yukarıda** bırakıyordu. Oysa kaynak köşkü Bizans
**deniz suru** üzerinde, denize **taşan** bir yapı olarak tarif eder.

Yapı 1872'de yıkıldığı için ölçülü bir koordinat yok — ama kaynak
**ölçülebilir bir tarif** veriyor: *"Sarayburnu'ndan kıyı boyunca yaklaşık
300 m, Soter Filantropos kalıntısı ile Ahırkapı arasında."*

Bu tarif **kendi 1632 kıyı çizgimizde** uygulandı: Sarayburnu ucu bulundu
(1110, −930), kıyı çizgisi 25 m aralıklarla tarandı ve uçtan 300 m'deki su
hattı **(1210, −1225), kot 0,1 m** çıktı → **28,988070 D / 41,014361 K**.
Geri dönüşüm sapması 0,000 m.

Yani konum uydurulmadı; **belgeli bir mesafe kendi arazimizde ölçüldü.**

Bu turun **üçüncü** elle-girilmiş-koordinat hatası (Doğancılar 771 m,
Üsküdar Mihrimah 164 m, İncili Köşk 156 m). Ortak ders artık açık: bu
kataloğun `APPROX` etiketli her koordinatı şüphelidir.

### İki yan bulgu

**(a)** Mevcut "kara landmark'ı denizde olamaz" denetimi doğru şekilde
patladı — köşk kot 0,1 m'de. Denetimin zaten bir çıkış kapısı vardı
(`on_water=True`) ve köşk tam onun için: yapı gerçekten suya taşar.

**(b)** O hata mesajı **konsolda çöküyordu**: metin Türkçe harf içeriyor,
konsol cp1252. Gerçek hata görünmedi, yerine bir `UnicodeEncodeError`
okundu. `log()` artık kodlanamayan karakteri değiştirerek yazıyor —
bir denetim, mesajı okunamıyorsa denetim değildir.

## Karar 2 — Örtü tartışması **iki varyantla** taşınır

TDV ortada yükselen kare kütle ve **kubbe** der; bir tasvir **piramidal**
gösterir; **Sedat Hakkı Eldem** örtünün **ahşap** olduğunu savunur.

Karar verilmiş gibi davranmak yerine iki varyant üretildi — Galata
Kulesi külahındaki yolun aynısı (ADR 0033). Üretici ikisinin gerçekten
ayrıştığını da sınıyor: Hüdâyî türbesinde `acik` bayrağı okunmadığı hâlde
"açık" diye kataloglanmıştı (ADR 0037), aynı sessizlik burada da mümkündü.

## Karar 3 — Kıyı yapıları **denize** bakar

`LandmarkPlacer`a üçüncü yönlendirme kuralı eklendi. Köşk için eğim de
kıble de yanlış olurdu: yapı deniz surunun üstündedir, cumbası denize
taşar ve padişah kıyıdaki töreni oradan seyreder. `Waterward` çevredeki
**en alçak** arazi yönünü verir. Ölçüldü: önünde 20-120 m boyunca −12,0 m,
arkasında 0,8 → 14,1 m.

## Karar 4 — Sayılan özellikler geometriyi bağlar

Ölçü yok, ama kaynak sayılabilir şeyler söylüyor ve hepsi teste bağlandı:
Sarayburnu tarafında **bir**, Ahırkapı tarafında **iki** kemer (asimetri
belgeli — simetrik yapmak "daha düzgün" göründüğü için tam olarak sessizce
kayacak şeydir); **dört** köşe bacası; **taşan** cumba.

### Render iki kusur gösterdi

**Cumba düz bir sıva levhasıydı** ve tabela gibi okundu. Oysa kaynak
padişahların töreni "köşkün **pencerelerinden**" seyrettiğini söyler —
camekânı olmayan bir cumba, cumba değil bir çıkıntıdır. Ahşap iskelet +
beş gözlü cam şeridi + yan pencereler eklendi.

**Bacalar köşe payandası gibi** duruyordu (geniş ve alçak). Baca ince ve
yüksek olur; 0,62 × 4,10 m'ye çekildi, külah ve ağız eklendi.

## Sonuç

- `IncliKosk` (kubbe) LOD0 5 120; `IncliKosk_Ahsap` LOD0 4 864; fark 0,46 m.
- Yerleşim (1210, 0, −1225); taban **−1,60 m** (su altında), tepe 16,30 m.
- Galata Kulesi'ne **1 722 m** — uçuş hattının solunda, seyir mesafesinde.
- Sahnede 9 landmark; boş/gömülü malzeme yok.

## Açık kalanlar

- Örtü seçimi Caner'e soruldu (kubbe mi ahşap mı sahnede dursun).
- Köşkün oturduğu **Bizans deniz suru** henüz modellenmedi; şimdilik alt
  yapı tek başına duruyor.
