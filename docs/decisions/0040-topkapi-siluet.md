# ADR 0040 — Topkapı silueti: 1632'nin iki sessiz farkı ve bildirilen yön

- **Tarih**: 2026-08-25
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/topkapi.md`)
- **Bağlam**: Faz 3, S-kademe. PLAN sarayı "**siluet (uzaktan)**" olarak
  tarif eder.

## Karar 1 — Saray tek varlık değil, **siluetin belirleyicileri**

Saray bir yapı değil bir kenttir (~700×400 m). Tek bir dev varlık ne
yönetilebilir ne de dürüst olurdu. Bu tur siluetin iki belirleyicisi
üretildi: **Adalet Kulesi** ve **Bâbüsselâm**. Geri kalan kütle (Sur-ı
Sultanî, Kubbealtı, kurşun çatı denizi, Harem bacaları) Faz 4'ün
prosedürel doldurmasına aittir.

## Karar 2 — Adalet Kulesi **üç** taş katlı kurulur

Kanûnî 1527-29'da taş bölümü ekletti; 1632'deki biçim **üç taş kat + ahşap
üst kat + kurşun piramidal külah**tır. **II. Mahmud (1819-20)** dördüncü
taş katı, ahşap seyir bölümünü ve yükseltilmiş külahı ekledi; **Abdülaziz**
bugünkü sivri külahı verdi.

Bu **Galata Kulesi'ndekiyle aynı hata ailesidir** (ADR 0033): tanınan
siluet sonraki yüzyılların eseridir. Üretici üç katı zorluyor, test de.

Kule bir kule değil bir **pencere taşıyıcısıdır**: hünkâr penceresi
Kubbealtı'na bakar ve padişah divanı oradan izler. Kafes, mahalle kitinin
demir şebekesiyle kuruldu — aynı demir işçiliğinin ikinci bir nüshasını
yazmak, zamanla iki farklı şebeke demektir.

## Karar 3 — Bâbüsselâm'ın kule tartışması modeli etkilemez

Kuleler 1632'de vardır; tartışma yalnızca **kimin** eklediğidir (Necipoğlu
Fatih der, yaygın görüş Kanûnî). **İki ihtimal de 1632'den öncedir**, yani
bu bir varyant gerektirmez — yalnızca kayda geçer. (Karşılaştır: İncili
Köşk'ün örtüsü *gerçekten* iki farklı 1632 sonucu verir, o yüzden orada
iki varyant var.)

Siluet kuralı teste bağlandı: **Adalet Kulesi kapıdan yüksek** olmalı.
Ölçüm: kule tepesi 79,4 m, kapı 75,1 m.

## Karar 4 — `face_deg`: varlık **kendi belgeli yönünü** bildirebilir

Yerleştiricinin üç yön kuralı vardı (kıble, denize, eğime) ve üçü de bu
iki yapı için yanlıştı. Bâbüsselâm birinci avludan ikinciye açılır, yani
**güneye** bakar; eğim onu batıya (278°) döndürüyordu.

Çözüm yerleştiriciye yapıya özel bir istisna yazmak **değil**, varlığın
kendi yönünü bildirebilmesiydi. Katalog `face_deg` taşıyor ve yerleştirici
onu **en yüksek öncelikle** okuyor. Ölçüldü: Adalet Kulesi 270,0°,
Bâbüsselâm 189,0°, Mihrimah 330,4° (kıble), İncili Köşk 90,0° (denize),
Galata Kulesi 204,9° (eğim) — dört kural da öncelik sırasıyla çalışıyor.

Bâbüsselâm'ın 189°'si uydurulmadı: kapı→kule yönü ölçüldü (8,9°), ön cephe
onun tersidir.

Bir test iki yön kaynağının **çakışmamasını** da sınıyor: kıble kuralını
kullanan bir yapı ayrıca `face_deg` bildiremez.

### Yolda kırılan şey

`face_deg` ayrıştırıcısını yazarken C# `char` literaline **gerçek bir satır
sonu** yazdım ve derleme kırıldı. Daha kötüsü: iki tur boyunca konsolu
okumadan "neden çalışmıyor" diye aradım — oysa hata oradaydı. Şimdi
karakter denetimi yalnızca `,` ve `}` arıyor; JSON'da sayıyı zaten onlar
bitirir ve satır sonu karakterine hiç gerek yok.

## Sonuç

- `TopkapiAdaletKulesi` LOD0 286; `TopkapiBabusselam` LOD0 550.
- Kule (811, 54,5, −1454) tepe 79,4 m; kapı (792, 53,5, −1574) tepe 75,1 m;
  aralarında 121 m (ikinci avlunun boyu).
- Sahnede **11 landmark**; boş/gömülü malzeme yok. EditMode 185/185.

## Açık kalanlar

- **Alay Köşkü**: 1632'de **ahşap** (bugünkü mermer köşk 1810/1819-20).
  Kayıt RESEARCH.md §5.7'de; henüz üretilmedi.
- Sur-ı Sultanî ve sarayın kütle denizi Faz 4'e ait.
- Adalet Kulesi'nin hünkâr penceresi silüet mesafesinde düz bir dikdörtgen
  gibi okunuyor; yakın plan gerekirse ayrıca çalışılmalı.
