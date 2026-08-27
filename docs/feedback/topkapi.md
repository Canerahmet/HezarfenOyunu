# Topkapı silueti — inceleme notu

İnceleme paketleri: `renders/review/TopkapiAdaletKulesi_v1/contact_sheet.png`,
`renders/review/TopkapiBabusselam_v1/contact_sheet.png`
Karar kaydı: **ADR 0040**. Araştırma: RESEARCH.md §5.7.

## Bakarken bilmen gereken

**Adalet Kulesi 1632'de bugünkünden alçaktır.** Fotoğraftaki yüksek, sivri
külahlı kule 19. yüzyıldır: **II. Mahmud (1819-20)** dördüncü taş katı,
ahşap seyir bölümünü ve yükseltilmiş külahı ekledi; **Abdülaziz** sivriltti.
1632'de kule **üç taş kat + ahşap üst kat + kurşun piramidal külah**tır —
bodur ve ağırbaşlı.

Galata Kulesi'ndeki hatanın aynısı: tanınan siluet sonraki yüzyılların eseri.

**Bâbüsselâm**'ın çifte konik külahı 1632'de var; tartışma yalnızca kimin
eklediği (Necipoğlu Fatih, yaygın görüş Kanûnî) ve iki ihtimal de 1632'den
önce — yani model etkilenmiyor.

## Ölçüler

| | | |
|---|---|---|
| Adalet Kulesi | **3** taş kat (belgeli sayı) | toplam 24,86 m, LOD0 286 |
| Bâbüsselâm | **2** kule (belgeli sayı) | toplam 21,60 m, cephe 22,9 m, LOD0 550 |
| aralarındaki mesafe | 121 m | = ikinci avlunun boyu |
| konumlar | **ölçülü** (Kültür Envanteri) | |

Ölçülü çizim yok → kütleler **D3**. Ama sayılar var ve geometriyi bağlıyor:
üç taş kat, iki kule. Siluet kuralı da teste bağlı — kule kapıdan yüksek
(79,4 m / 75,1 m).

## Bu turda eklenen bir yetenek

Yerleştiricinin üç yön kuralı vardı (kıble, denize, eğime) ve üçü de bu iki
yapı için yanlıştı: Bâbüsselâm birinci avludan ikinciye açılır, **güneye**
bakar; eğim onu batıya döndürüyordu.

Yerleştiriciye yapıya özel istisna yazmak yerine, varlığın **kendi belgeli
yönünü bildirmesini** sağladım (`face_deg`). Şimdi dört kural öncelik
sırasıyla çalışıyor. Bâbüsselâm'ın açısı uydurma değil: kapı→kule yönü
ölçüldü, ön cephe onun tersi.

## Sana sorduğum

1. **Kule yeterince bodur mu duruyor?** Bugünkü siluete alışkın gözle
   "eksik" görünebilir — kasıtlı.
2. Bâbüsselâm'ın kuleleri **oranlı mı**? Uçuş hattından bakınca sarayı
   tanıtacak tek işaret onlar.

## Bilerek eksik

- **Alay Köşkü**: 1632'de **ahşap** (bugünkü mermer 1810/1819-20). Kayıtlı,
  üretilmedi.
- Sur-ı Sultanî, Kubbealtı ve sarayın kurşun çatı denizi → Faz 4 (prosedürel).

---

**Onay**: _(bekliyor — "OK vN" yaz)_
