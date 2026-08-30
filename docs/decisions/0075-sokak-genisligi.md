# ADR 0075 — Sokak genişliği ve evler arası aralık

- **Durum:** KABUL EDİLDİ (Caner, 2026-08-30)
- **Tarih:** 2026-08-30
- **Bağlam:** Faz 8, oynanış geri bildirimi
- **İlişki:** ADR 0016'nın (4,6 m sokak) sayısını **oynanabilirlik lehine
  değiştirir**

## İstek

Caner (2026-08-30, oynarken):

> "bina ve evler birbirine cok yakin. daha genis olabilir yolar."

## Ölçülen mevcut durum

`OttomanStreetBuilder`:

| ölçü | değer |
|---|---|
| ana sokak (`StreetWidth`) | 4,6 m |
| çıkmaz/ara sokak (`AlleyWidth`) | 3,0 m |
| komşu iki ev arası (`gap`) | %83 ihtimalle **0,25–0,95 m**, %17 ihtimalle 2,5–5,0 m |
| cephe düzensizliği (`setback`) | −0,10…+0,40 m |

Ev, eksenden `StreetWidth/2 + duvar_derinliği/2 + setback` kadar konuyor,
yani **karşılıklı iki duvar arası açıklık ≈ 4,4–5,4 m**. Üstelik Osmanlı
evinin **cumbası** üst katta sokağa taşar; göz hizasında algılanan genişlik
bundan da dar.

## Neden bu bir ADR

4,6 m uydurulmuş bir sayı değil: ADR 0016 onu kaynaktan ölçmüştü ve
17. yüzyıl İstanbul'unun sokağı gerçekten bu kadar dardı. Yani Caner'in
gördüğü "çok yakın" **tarihsel olarak doğru**.

Ama oyunun kendisi de bir gerçek: üçüncü şahıs kamerası 3,2 m'lik bir kolla
arkadan bakıyor ve 4,6 m'lik sokakta o kol sürekli duvara çarpıp kısalıyor —
ölçüldü, turda üç durakta birden 1,40 m'ye çöktü. Dar sokak, oyuncunun
karakterini göremediği sokak demek.

Bu, "gerçekçilik" ile "oynanabilirlik" arasında **gerçek** bir çatışma; ADR
0074'teki gibi ikisinin aynı yöne baktığı bir durum değil.

## Karar

| ölçü | eski | yeni |
|---|---|---|
| ana sokak | 4,6 m | **7,2 m** |
| ara sokak | 3,0 m | **4,4 m** |
| komşu ev aralığı | 0,25–0,95 m | **1,4–3,0 m** |
| geniş aralık (bahçe/geçit) | 2,5–5,0 m | **4,5–8,0 m** |

7,2 m keyfî değil: kamera kolu 3,2 m ve çarpışma yarıçapı 0,25 m; kol,
karakterin arkasında tam boyunda kalabilmek için karşı duvara en az
~3,5 m ister. 7,2 m sokakta oyuncu eksende yürürken her iki yana 3,6 m
kalıyor.

## Kayda geçen bedel

**Bu artık T1 değil.** ADR 0016'nın ölçtüğü sayı 4,6 m'dir ve şehir onu
taşımıyor. `HistoricalTag` bakımından mahalle dokusu zaten T2
(*"Sokak dokusu RESEARCH.md 4.1'den çıkarım"*), ama şimdi **ölçülmüş bir
sayıdan bilerek uzaklaşan** bir T2. Bunu gizlemek yerine yazıyorum:

> Sokak genişliği tarihsel ölçüden **%57 geniş**. Sebep oynanabilirlik,
> kaynak değil. Geri almak isteyen tek sayıyı değiştirir.

Şehrin dokusu da bundan etkilenecek: bitişik nizam gevşeyecek, mahalle
biraz daha seyrek görünecek. Bu, isteğin kendisi.

## Uygulama notu

Bütün türev mesafeler `StreetWidth`ten ölçekleniyor (ev geri çekilmesi,
dükkân yerleşimi, avlu, ara sokağın başlangıcı, kaldırım şeridi), yani tek
sabiti değiştirmek yeterli. Ölçüldü: `StreetWidth`e bağlı beş kullanım var
ve hepsi `StreetWidth * 0.5f + …` biçiminde.

## Sonrası: genişletmenin ödenmemiş faturaları (2026-08-30, aynı gün)

Genişletme tek sabiti değiştirdi ve "sokaklar geniş" ölçümü doğruladı
(kaldırım üstünde açık genişlik ortanca **7,67 m**). Ama üç türev sayı
o sabitle birlikte hareket etmedi ve üçü de oyuncunun göreceği kusur
üretti:

| kusur | sebep | ölçülen | düzeltildi |
|---|---|---|---|
| Evler birbirinin duvarından geçiyor | ilerleme `W_bu + aralık`, oysa merkezler arası `(W_bu+W_sonraki)/2 + aralık` ister; ayrıca çakışma denetimi **daireydi** | evlerin **%20,0**'si, en kötü 2,34 m | ilerleme düzeltildi + denetim dikdörtgene (SAT) çevrildi → **%0,0** |
| Çıkmaz ağzı ana sokağın ev sırasının içinde başlıyor | sabit `+ 6f` payı 4,6 m'lik sokağa göreydi | — | pay katalogdaki en derin evden ölçülüyor |
| Kaldırım yamaçta 1–3,5 m'lik taş terasa dönüştü | kesitin **en yüksek** noktası alınıyordu; 4,6 m'de 0,64 m olan fark 7,2 m'de ~1,0 m (p90 ~2,1 m) oldu | — | yol enine eğimli: eksen kesitin ortasını izler, kenarlar kendi kotuna eğilir (`CaprazEgim` 0,45 m) |

Ders, ADR 0074'ünkinin tersi değil aynısı: **bir sabiti değiştirmek, o
sabitten türeyen sayıları değiştirmez.** "Bütün türev mesafeler
`StreetWidth`ten ölçekleniyor" diye yazmıştım; ölçeklenen mesafeler
öyleydi, ama ölçeklenmeyen **paylar** ve **denetim şekilleri** vardı ve
onları saymamıştım.

Ev sayısı 11.451'den 10.868'e düştü (%5) — çakışan evler artık
konulmuyor. Şehir seyrelmedi, üst üste binen kütleler ayrıldı.
