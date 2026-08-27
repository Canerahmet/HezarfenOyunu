# Sultanahmet Camii — inceleme notu

İnceleme paketi: `renders/review/Sultanahmet_v2/contact_sheet.png`
Karar kaydı: **ADR 0047**. Araştırma: RESEARCH.md §5.13.

## 1632'de bu yapı **yeni**

Süleymaniye için "1557'den beri değişmedi" demiştim. Burada daha keskin:
Sultanahmet **1616**'da ibadete açıldı, yani IV. Murad'ın İstanbul'unda
**on altı yaşında**. Şehrin en tanınan silueti, oyunun geçtiği yıl daha
bir kuşak eskimemiştir.

Külliye de tamam: arasta ve hamam 1617, **Sultan Ahmed türbesi 1619**
(II. Osman tamamlattı), medrese-darüşşifa-imaret 1620. Sonradan eklenen
tek şey III. Selim'in su haznesi (1802 sonrası) — bu yapıda "1632'de
yok" listesi bir satır.

## Kubbe çapını kaynaktan almadım

Yayımlanan sayı 23,5 m. Yazıp geçebilirdim; onun yerine planı ölçtüm:
ayak duvarlarının ekseni **30,75 m** aralıklı, duvar **3,65 m** kalın →
iç yüzler arası **23,45 m**. Yayımlanan sayı tam olarak bu.

Bu, doğrulamaktan fazlasını yaptı: **hangi ölçü** olduğunu söyledi.
23,5 kubbenin kabuğu değil, **ayaklar arası açıklık**.

Ve şunu ortaya çıkardı: bir Osmanlı kubbesinin **üç** sayısı var.

| | |
|---|---|
| **22,40 m** | TDV, "içten" |
| **23,50 m** | açıklık |
| **27,7 m** | kurşun izi = kasnak + saçak |

Dört turdur "iç/dış ikiliği" diye not ettiğim şey aslında ikilik değil,
**üçlük** — çünkü Osmanlı kubbesi kasnağa oturur, Bizans kubbesi
oturmaz. Ayasofya'da iki sayı vardı, burada üç. Muhasebe sıkıntısı değil,
mimari fark.

## Sayılar

| | |
|---|---|
| Yarım kubbe | **4** (dört yönde birer) |
| Eksedra | **12** (her yarım kubbede üç) |
| Fil ayağı | **4**, çapı **5 m** |
| Minare / şerefe | **6** / **16** (4×3 harim + 2×2 avlu) |
| Avlu revakı | **26** sütun, **30** kubbeli birim |
| LOD0 | 8 282 üçgen |

Yarım kubbe sayısı planı tanımlar ve dördü birbirine benzemez: Üsküdar
Mihrimah **üç**, Süleymaniye ve Ayasofya **iki**, Sultanahmet **dört**.
Teste bağladım ki biri hepsini aynı sayıya çekmesin.

## Bu turda düzelttiğim

**Yarım kubbeleri basık yapmıştım.** Ana kubbenin 0,78 oranını onlara da
uygulamıştım; kilitleri kemer katının 3,3 m altında kalıyor ve dördü de
bloğun arkasına gömülüyordu — render'da kubbenin dibinde kabarcık gibi
duruyorlardı.

Plan ikisinin aynı kotta olduğunu söylüyor, sebebi de açık: **her yarım
kubbe dört büyük kemerden birinin üstüne oturur**, yani kilidi o kemerin
kilididir. Yarım küre yapınca yerine oturdu. Basıklık bir üslup değil,
taşıyıcının sonucu.

**Eksedraları da çok alçak başlatmıştım** ve on ikisi de silüete hiç
katılmıyordu. Şimdi saçak kotundan doğuyorlar ve kaskadı onlar yapıyor.

## Bu yapı bir kuralı doğurdu

Sultanahmet'in eksenini ölçerken kıble sabitimin yanlış olduğunu
anladım — ayrıntısı `docs/feedback/kible.md`'de. Kısacası: şehrin bütün
camileri 16,7° döndü ve bu, tek yapıyı değil bütün şehri düzelten ilk
bulgu oldu.

## Sana sorduğum

1. **Kaskad okunuyor mu?** Sultanahmet'i Sultanahmet yapan şey ana kubbe
   değil, ondan aşağı inen yarım kubbe → eksedra basamakları.
2. **Altı minarenin dizilişi** doğru mu duruyor? Dördü harimin, ikisi
   avlunun köşesinde ve o ikisi kısa.

## Bilerek eksik

- **Sultan Ahmed türbesi** (1619) üretilmedi — ayrı varlık olacak.
- Arasta, hamam, medrese, darüşşifa, imaret: hepsi 1632'de ayakta,
  hiçbiri üretilmedi (Faz 4).
- Minare **boyları** (64 / 54 m) ölçülü kaynağa dayanmıyor — **D3**.
  Plandaki yükseklikler burada güvenilmez çıktı: aynı iz kubbeye 62 m
  diyor, yayımlanan kilit 43 m. Plan **geometrisi** güvenilir, plan
  **yükseklikleri** değil.

---

**Onay**: _(bekliyor — "OK vN" yaz)_
