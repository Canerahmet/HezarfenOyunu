# Mihrimah Külliyesi — medrese ve sıbyan mektebi

İnceleme paketleri: `renders/review/MihrimahMedrese_v1/contact_sheet.png`,
`renders/review/MihrimahMektebi_v2/contact_sheet.png`
Karar kaydı: **ADR 0038**. Araştırma: RESEARCH.md §5.4.

## Önce bir hata: caminin yeri 164 m yanlıştı

Medrese ile mektebi **ölçülü** koordinatlarıyla ekleyince belgeli göreli
konumlar tutmadı — medrese "caminin doğusunda" olmalıyken 213 m
kuzeydoğuda, mektep "kıble tarafında" olmalıyken ters yönde çıktı.

Sebep yeni koordinatlar değil, **caminin kendi koordinatıydı**: elle
girilmiş ve ~164 m yanlış. Düzeltince:

| | ölçüm | belge |
|---|---|---|
| mektep — cami | 33 m, **kıble bileşeni 1,00** | "caminin kıble tarafında" |
| medrese — cami | 52 m, doğu bileşeni 0,39 | "caminin doğusunda" |

Mektebin tam 1,00 çıkması bağımsız bir doğrulama: o ilişkiyi koordinatı
düzeltirken kullanmadım. Doğancılar'daki 771 m'lik hatanın (ADR 0037) aynı
ailesi — **elle girilmiş koordinatlar sessizce yanlış oluyor ve ancak başka
bir ölçümle çelişince ortaya çıkıyor.** Artık bir test bunu arıyor.

## Üretilenler

**Medrese** — 1548, Sinan, caminin doğusunda. **On altı hücre** + kubbeli
dershane (TDV/İBB). Ayak izi 24,3 × 28,5 m.

Ölçüsü yok, ama **sayısı var**: avluyu "makul" çizip çıkan hücre sayısını
kabullenmek yerine, avluyu **16 tutana kadar aradım** (ilk denemem 14 verdi,
üretici reddetti). Dürüst olmak gerekirse kısıt gevşek: 16 veren **100
kombinasyon** var. O yüzden D3.

**Sıbyan mektebi** — 1547-48, caminin kıble tarafında. Kışlık kubbeli oda +
**yazlık açık eyvan**, ve **2,90 m'lik dükkân katının üstünde** (kaynak:
"yamaçta olduğu için altına dükkân eklenmiştir" — alt yapı süsleme değil,
belge). İlk üretimde eyvan yoktu; kaynağa dönünce yapının yarısını
atladığımı gördüm ve kite ekledim.

## Bilerek yapılmayanlar

- **İmaret-tabhâne** ve **Kurşunlu Han**: 1632'de ikisi de ayakta, ama
  **yerleri bilinmiyor** (TDV imaretin yerini "belirsiz" der; han tamamen
  kaldırılmış). Yeri bilinmeyeni koymak koordinat uydurmak olurdu.

## Sana sorduğum

1. **Medrese ölçeği** doğru mu duruyor? 24×28 m bir külliye medresesi için
   makul mü — yoksa büyük mü geliyor?
2. Mektebin **eyvanı** okunuyor mu, yoksa arkada kaybolmuş mu?

---

**Onay**: _(bekliyor — "OK vN" yaz)_
