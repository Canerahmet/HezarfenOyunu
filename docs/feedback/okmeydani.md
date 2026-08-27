# İnceleme — Okmeydanı: menziller, namazgâh, tekke (Faz 2b)

**Üretim:** 2026-08-22, **düzeltildi** 2026-08-23 · **ADR:** 0027 + **0028**
**Kaynak:** RESEARCH.md §4.6

Burası oyunun hikâyesinin başladığı yer: Hezarfen'in talim yaptığı meydan.

## Karar 10'un cevabı: yönler kaynakta VAR

Sen "bilgim yok, ara" dedin. Aradım ve **ADR 0027'de yazdığım şey yanlıştı.**
Menzillerin yönü kaynakta var — sadece derece olarak değil, **rüzgâr adıyla**:

> "Rüzgâr, menzil okçuluğunda atış vaktinin ve **atış yönünün**
> belirlenmesindeki temel unsurdu. **Her menzil için belirlenen bir rüzgâr
> vardı**… Böylece hem **rüzgâr arkaya alınıp** atıcının rüzgârdan
> faydalanması sağlanır…"

Menziller zaten rüzgârla anılır: *Lodos Menzili*, *Poyraz Menzili*, *Yıldız
Menzili*. Rüzgâr adı rüzgârın **geldiği** yönü söyler, rüzgâr da arkaya
alınır — yani **ok ters yöne gider**. Dört taşı kuzeybatı yelpazesine
dağıtmıştım; hepsi gitti.

## Sahnedeki beş menzil — hepsi belgeli

| Menzil | Hava | Ok gider | Taş | Gez |
|---|---|---|---|---|
| Havandelen Solak Bali | yıldız | güneye (180°) | Bursalı Şüca | 1251,5 |
| Tozkoparan (Delikli Kaya) | yıldız | güneye (180°) | Tozkoparan İskender | 1279,5 |
| Yıldız | yıldız | güneye (180°) | Mîrî Âlem Ahmed Ağa | 1146 |
| **Arkurı** | gündoğusu | batıya (270°) | Tozkoparan İskender | **1281,5** |
| Lodos | lodos | kuzeydoğuya (45°) | Mîrî Âlem Ahmed Ağa | 1271 |

Adı, havası ve mesafesi belgeli; hepsi 1632'den önce açılmış. **Arkurı
meydanın rekoru** — TDV'nin 845,66 m'si tam olarak 1281,5 × 0,66'dır.

### Bir taşın 80 gezi

Tozkoparan, Şüca'nın Havandelen'deki taşını 28 gez geçti ama oku ana taşın
**80 gez şastına** düştü — koridor her yandan 40 gezdir, yani "aşırı salkı".
Tartışma II. Bayezid'e gitti; Şeyhülmeydan Hamdullah Efendi taşı **ayrı bir
menzil** saydı. Sahnede iki menzil bu yüzden aynı ayak taşını paylaşır ve
Tozkoparan'ın taşı eksenden tam 80 gez yanda durur.

## Sahneden ÇIKARDIĞIM şey

**IV. Murad'ın ~706 m'lik taşı gitti** — geçen turun başlık sayısıydı. Sayı
akademik olmayan bir kaynaktan geliyordu, havası bilinmiyor, ve asıl mesele
tarih: IV. Murad 1623–1640 hüküm sürdü, taşın 1632'den sonra dikilmiş olma
ihtimali yarı yarıya. Tarihlendiremediğim bir taşı koymak, tekkeye minare
koymakla aynı hata.

Ayrıca 588 m'lik "menzil" **kaideye göre menzil bile değilmiş**: menzil
açmanın alt sınırı 900 gez (≈594 m).

## Bakılacak

| Kare | Ne |
|---|---|
| `Captures/okmeydani_rekortasi.png` | Arkurı rekor taşı, 4,5 m'den — kitabe ayak taşına bakar |
| `Captures/okmeydani_tas_arka.png` | aynı taşın **arkası**: boş. Yazı tek yüzdedir |
| `Captures/okmeydani_menzil.png` | ayak taşından koridor boyunca — görünen şey MESAFE |
| `Captures/okmeydani_ayaktasi.png` | ayak taşı, göz hizasından |
| `Captures/okmeydani_tekke.png` | tekke + namazgâh, meydanın çeperinde |
| `Captures/okmeydani_meydan.png` | meydan, 520 m'den |
| `renders/review/MenzilTasi_Buyuk_v3/contact_sheet.png` | rekor taşı, ölçü figürüyle |
| `renders/review/MenzilTasi_Bas_v1/contact_sheet.png` | baş taşı |
| `renders/review/Tekke_Okcular_v3/contact_sheet.png` | tekke — avlulu, **minaresiz** |

Kareler artık **menü öğesi**: `Hezarfen → GIS → Okmeydani inceleme paketi`.
Geçen tur tek seferlik komutlarla alınmışlardı, yani tekrar üretilemiyorlardı.

## Taş artık mermer

İki kusur ölçüldü:

* Sütunun parlaklığı **36,7/255**, yanındaki çayır **162,5** — taş zeminden
  4,4 kat koyuydu. Mermer ışıkta duran en açık şeydir.
* Sütunda **0,95 m periyotlu** dikey bantlanma — yani duvar dokusunun taş
  sırası. Kaynak "tek parça mermer sütun" diyor.

Poly Haven'da lisanslı mermer yok, lisanssız görsel indirmek yasak. Kurşun ve
yaprakta olduğu gibi ürettim: damar, döşenebilir bir gürültünün eş seviye
eğrisi.

## Sana iki soru

**Karar 10 — kapandı.** Yönler artık uydurma değil. Onay ya da itirazın?

**Karar 11 — meydanın sınırı dar olabilir.** 17. yüzyılda Okmeydanı'nı ölçen
Abdullah el-Kâtip alanı kabaca 8150 gez verir; makalenin hesabına göre bu
≈ **4,9 km²**. Bizim taslak poligonumuz **2,74 km²** — yani muhtemelen yarı
yarıya küçük. Bu Karar 8'in (yeşil alan sınırları) parçası; poligonu
büyüteyim mi, yoksa sınırları toptan mı ele alalım?

## Bildiğim eksikler

- Taşlarda **yazı yok**: kitabe bir alan, harf değil.
- Koridorların **gerçek yerleri** bilinmiyor — hava (yön) belgeli, ayak
  yerinin meydandaki konumu değil. Koridorlar araziye göre yerleştirildi.
- Meydan boş ve bu doğru; ama okçu, hedef, havacı gibi donatı yok.
- Tekkede **meydan şeyhinin odası** ayrı bir mekân değil (Faz 6 için not).
- Faz 2b'de kalan altı madde: imaret, arasta, bozahane, değirmen, su terazisi,
  muvakkithane.

## Onay

```
OK v2        (ya da: düzeltme istekleri)
Karar 11: poligonu büyüt / sınırları toptan ele alalım / şimdilik kalsın
```
