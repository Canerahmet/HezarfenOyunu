# İnceleme — Galata surları ve kapıları (1632)

**Üretim:** 2026-08-24 · **ADR:** 0034 · **Kaynak:** RESEARCH.md §5.2
**Kademe:** T1 · **Doğruluk basamağı:** **D2** · **Durum:** `measured`

Faz 3'ün ikinci S-kademe landmark'ı. Kule bunların üstünde durur.

## Bakılacak

| Paket / kare | Ne |
|---|---|
| `renders/review/SurKapisi_v3/contact_sheet.png` | kapı — Harup Kapı ölçüsünde |
| `renders/review/SurBurcu_v2/contact_sheet.png` | **U planlı** büyük burç (16 no'lu) |
| `renders/review/SurBurcu_Kucuk_v1/contact_sheet.png` | küçük U planlı burç (9 no'lu) |
| `renders/review/SurBurcu_Dortgen_v1/contact_sheet.png` | **dörtgen** burç (D3) |
| `Captures/sur_hava.png` | surlu Galata, havadan |
| `Captures/sur_kapi.png` | kapı ve perde duvar, yaya gözünden |

Sahnede: **33 burç (10 büyük U + 11 küçük U + 12 dörtgen), 2 kapı**, 2 510 m hat.

## ~~Karar 15~~ — KAPANDI: tez bulundu ve ölçüleri verdi

*"Tezden bulmaya çalışalım"* dedin. Bulundu:

> **Erdoğan, Batuhan Burhan (2013)**, *Galata Kent Surları ve Koruma
> Önerileri*, YL tezi, İTÜ, dan. **Zeynep Ahunbay** — 442 sayfa, açık erişim,
> ayakta kalan sur/burç/kapıların **2010 arazi ölçümleri**.

Taslak sayıların hepsi ölçüyle değişti; **D3 → D2**:

| Ne | Önce (taslak) | Şimdi (ölçülü) |
|---|---|---|
| Duvar yüksekliği | 9,0 m | **7,0 m** (rölövede 6,50–17 m; fark eğimden) |
| Burç planı | kare | **U planlı, ön yüzü dairesel** |
| Burç ölçüsü | 6,4 × 6,4, h 13,5 | **9,80 × 7,70 / 16,16 m** ve **7,02 × 5,84 / ~10 m** |
| Kapı açıklığı | 3,60 m | **2,70 m** (Harup Kapı) |
| Kemer üzengisi | türetim | **3,60 m** ölçülü |

Sahnede artık **20 büyük + 21 küçük burç** dönüşümlü — tez burçların tek
boyda olmadığını gösteriyor.

**Bir aşırı düzeltmemi de geri aldım:** geçen tur "burçlar kare değil, U
planlı" demiştim. Aynı tez *"dörtgen **ve** U planlı burçlar"* diyor —
hayatta kalan örnek, örneklem değildir. Üçüncü varyant eklendi.

**İki bağımsız doğrulama çıktı:** tez çevreyi 2 800 m / 37 ha veriyor, yani
ADR 0029'da Galata halkasını ölçeklerken kullandığımız çapaların ta kendisi;
ve Galata Kulesi'nin dış çapını "~16 m" veriyor, yani ADR 0033'te TDV'den
aldığım 16,45 m'yi doğruluyor.

Bu turun dersi: *"kaynak nitel"* bir durum tespiti değil, bir **arama emri**.

## 1632'de sur EKSİKSİZ ayakta

Yıkım **1864**'tür — oyunun tarihinden 232 yıl sonra. Bugünkü kalıntılara
bakıp modellemek en büyük hata olurdu. Kaynak notu bunu yazıyor ve test
1864'ün notta geçtiğini arıyor.

## Aynı hatayı tekrar yaptım

Kaldırımda 698 üçgenin 697'si ters sarılmıştı, dersi yazmıştım, SETUP.md'ye
uyarı koymuştum. Sur perdesini yazarken **aynı hatayı yaptım** — üstelik
sarımın doğru olduğunu söyleyen kendi yorumumun altında:

```
perde: YUKARI=1   ASAGI=4198     (düzeltmeden sonra: 4198 / 1)
```

Ters sarımda ışın sorguları arka yüzü görmez, yani **çarpıcı fiilen yoktur** —
oyuncu duvardan geçerdi. Yorum kanıt değil; sayı kanıt. Artık her elle
üretilen mesh için normal sayan bir test var.

## Kapı v1'de kemersizdi

Açıklık kare bir delikti ve 2,9 m yüksekliğindeydi. Bir sur kapısını kapı
yapan şey kemeridir. Mahallenin bütün kemerlerini üreten mevcut aleti
kullandım — çeşme nişi, avlu kapısı ve kilise penceresiyle aynı kemer
karakteri; yoksa şehirde iki ayrı mimarî dil olurdu.

## Bildiğim eksikler

- **Kapılar hattın üstünde değil.** Kapı noktaları ile sur halkası ayrı
  taslak kaynaklardan geliyor. Yerleştirici kapıyı hatta taşıyor **ve taşıma
  mesafesini yazıyor** — düzeltilmesi gereken bir sayı olarak.
- ~~Hat 7 noktalı kaba taslak~~ — **DÜZELTİLDİ (2026-08-25).** Tez hattın
  biçimini de veriyormuş: **yelpaze, tepesi Galata Kulesi**, batıda Azapkapı,
  kuzeydoğuda Tophane, deniz tarafında Haliç. Üç şey değişti:
  **(1)** kule artık surun **üstünde** (önce 80 m içindeydi; ölçülen uzaklık
  **0,0 m**), **(2)** deniz kenarı ayrı bir çizgi değil **kıyının kendisi** —
  su altında kalan oran **%35 → %20**, **(3)** Azapkapı **Haliç'in
  ortasındaydı** (−12 m), düzeltildi ve artık iki kapı da yerleşiyor.
  Ölçekleme kaldırıldı: alan artık bir **sonuç** (30 ha; belgeli 37 ha) ve
  fark **kendi kıyı çizgimiz** hakkında bir şey söylüyor.
- **Burçlar arası mesafe** hâlâ taslak (60 m); tez burç *aralığı* vermiyor.
- **Hendek yok.** Genişliği (15 m) belgeli ama derinliği değil, ve hendek
  arazi kazısı ister — ayrı tur.
- **Deniz surları yok**; halka şu an karada kapanıyor.
- **Kule surla bağlanmadı.** Galata Kulesi surun üstünde durur; şu an ikisi
  ayrı ve kule halkanın içinde kalıyor.

## Onay

```
OK v1        (ya da: düzeltme istekleri)
```

*(Karar 15 kapandı — ölçüler rölöveden.)*
