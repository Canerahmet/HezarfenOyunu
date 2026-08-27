# ADR 0047 — Sultanahmet: bir kubbenin üç sayısı

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/sultanahmet.md`)
- **Bağlam**: Faz 3, A-kademe. ADR 0046'nın (1632 kıblesi) çıkış noktası
  bu yapıdır.

## Karar 1 — Açıklık kaynaktan kopyalanmadı, plandan **çıkarıldı**

Yayımlanan sayı **23,5 m**. Onu yazıp geçebilirdim; bunun yerine planı
ölçtüm: ayak duvarlarının ekseni **30,75 m** aralıklı, duvar **3,65 m**
kalın → iç yüzler arası **23,45 m**.

Bu, sayıyı doğrulamaktan fazlasını yapıyor: **hangi ölçü olduğunu**
söylüyor. 23,5 m, kubbenin kabuğu değil **ayaklar arası açıklıktır**.

## Karar 2 — Bir Osmanlı kubbesinin **üç** sayısı vardır

| | |
|---|---|
| **22,40 m** | TDV, "içten" — kabuğun eteği |
| **23,50 m** | açıklık — ayakların iç yüzleri arası |
| **27,7 m** | plandan okunan kurşun izi — **kasnak + saçak** |

Ayasofya'da (ADR 0045) yalnızca **iki** sayı vardı ve aradaki 1,1 m
kabuk kalınlığıydı. Burada üçüncü bir sayı var, çünkü **Osmanlı kubbesi
kasnağa oturur**; Bizans kubbesi oturmaz.

Yani bu bir muhasebe sıkıntısı değil, **mimari bir fark** — ve dört
turdur takip ettiğim "iç/dış ikiliği" aslında ikilik değil, **üçlük**.
Mesh açıklığı taşır; öteki ikisi katalogda durur.

## Karar 3 — Eksedralar burada mesh'e **girer**

Ayasofya'da on iki eksedra mesh'ten çıkarılmıştı: gömülü kalıyorlardı
çünkü orada **iç mekân** öğesidirler (ADR 0045). Sultanahmet'te
girerler — yarım kubbelerin eteğinden dışa taşar ve siluetin basamaklı
kaskadını **onlar** yapar.

Aynı sözcük, iki yapıda iki ayrı şey. Katalog ikisini ayrı alanda tutar
(`exedrae` / `exedrae_interior`) ki "Ayasofya'da da vardı, oraya da
koyayım" refleksi bir daha kurulmasın.

## Karar 4 — Yarım kubbe **yarım küredir**, basık değil

İlk kurulumda ana kubbenin 0,78 basıklığı yarım kubbelere de
uygulanmıştı; kilitleri 25,64 m'de kalıyordu, kemer katının tepesi ise
28,97 m. Dördü de bloğun **arkasına gömülmüştü** ve render'da kubbenin
dibinde kabarcık gibi okunuyorlardı.

Plan ikisinin **aynı** kotta olduğunu söylüyor (yarım kubbe izi h=39,
kemer duvarlarının tepesi de h=39). Sebebi de açık ve geometriktir: her
yarım kubbe dört büyük kemerden **birinin** üzerine oturur, yani kilidi
o kemerin kilididir. Yarıçapı 10,8 m'lik bir yarım küre 17,22'den doğup
**28,02**'ye çıkar; kemer kilidi 28,97. Buluşuyorlar.

Basıklık oranı bir **üslup** değil, taşıyıcının sonucudur — ve bunu iki
yapıda iki ayrı yönde öğrendim (Ayasofya'da 0,78 fazla basıktı, burada
yarım kubbeye uygulanınca fazla alçaktı).

## Karar 5 — Zincir **üçüncü** kez doğrulandı

Yalnızca ölçülen kilitten (43,00) ve açıklıktan (23,50) türeyen kemer
kilidi **28,97 m**; plandan bağımsız okunan kemer katı **30 m**.
1,03 m fark.

Zincir Üsküdar Mihrimah'ta kuruldu (ADR 0036), Ayasofya'da **Bizans**
oranıyla beslenip tuttu (0,40 ve 0,90 m), burada **Osmanlı** oranıyla
yine tutuyor. Üç bina, iki oran, bir geometri.

## Karar 6 — Yükseklikler için plana güvenilmez

Ayasofya'da plandaki kubbe yüksekliği (56 m) yayımlanan 55,60 ile
**0,4 m** içinde buluşmuştu. Sultanahmet'te aynı kaynak kubbeye **62 m**
diyor, yayımlanan kilit ise **43 m**.

Kural: **plan geometrisi** (uydu izinden çizilen kontur) güvenilirdir,
**plan yükseklikleri** güvenilir değildir. Sultanahmet'in minare boyları
(64 / 54 m) bu yüzden **D3**; bağlayıcı olan, harim minarelerinin avlu
minarelerinden uzun olduğudur.

## "64 × 72 m" yapının neresi?

Üçüncü kez aynı tuzak (Ayasofya 82 × 73, kubbe çapı, şimdi bu). Ölçülen
**61 × 55** kullanıldı.

## Sonuç

- `Sultanahmet` LOD0 8 282; ayak izi 73,6 × 120,3 m, yükseklik 64,70 m.
- Yerleşim (292, 42,9, −2257), yön **313,7°** (şehrin 1632 kıblesi —
  bu yapı `face_deg` bildirmez, çünkü kural onun ölçüsünden doğdu).
- Tepe 108,9 m. Sahnede **17 landmark**. EditMode **208/208**.

## 1632'de neredeyse hiçbir şey eksik değil

Cami 1616, arasta ve hamam 1617, **Sultan Ahmed türbesi 1619** (II. Osman
tamamlattı), medrese-darüşşifa-imaret 1620. 1632'de külliye bütünüyle
ayakta ve yapı **on altı yaşında** — IV. Murad'ın İstanbul'unda şehrin en
tanınan silueti daha bir kuşak eskimemiştir. Sonradan eklenen tek şey
III. Selim'in su haznesidir (1802 sonrası).

## Açık kalanlar

- **Sultan Ahmed türbesi** üretilmedi; kubbe merkezinden 114 m batı,
  127 m kuzeyde ve ayrı varlık olacak (Ayasofya türbeleriyle birlikte).
- Arasta, hamam, medrese, darüşşifa, imaret — hepsi 1632'de ayakta,
  hiçbiri üretilmedi (Faz 4).
- Harim cephesi kabaca geçildi; siluet ve orta mesafe için yeterli.
