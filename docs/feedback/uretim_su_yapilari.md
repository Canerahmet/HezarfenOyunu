# İnceleme — üretim, ticaret ve su yapıları (Faz 2b'nin kalanı)

**Üretim:** 2026-08-23 · **ADR:** 0030 · **Kaynak:** RESEARCH.md §4.7

Faz 2b listesinde kalan **yedi madde** bitti. Bunlarla birlikte Faz 2b'nin
yapı üretimi tamamlandı; kabul ölçütü (mahalle sahnesi) hâlâ açık.

## Bakılacak

| Kare | Ne |
|---|---|
| `renders/review/Imaret_A_v2/contact_sheet.png` | imaret — kubbe sırası + **farklı boyda bacalar** |
| `renders/review/Arasta_A_v1/contact_sheet.png` | arasta — tonoz örtülü, karşılıklı sekiz göz |
| `renders/review/Bozahane_A_v1/contact_sheet.png` | bozahane — arkada mayalanma küpleri |
| `renders/review/Degirmen_Su_v3/contact_sheet.png` | su değirmeni — taş oluk + düşey çark |
| `renders/review/SuTerazisi_A_v2/contact_sheet.png` | su terazisi — kâgir kule + hazne |
| `renders/review/Muvakkithane_A_v1/contact_sheet.png` | muvakkithane — şebekeli pencere + mermer tezgâh |
| `renders/review/Cami_Orta_v1/contact_sheet.png` | orta ölçek cami — kubbeli, revaklı |

Her karede 1,70 m'lik ölçü figürü var.

## İkisi tarih riski taşıyordu

**Bozahane oyunun ikinci zaman işareti çıktı.** IV. Murad'ın emriyle yapılan
**1638 esnaf sayımında** İstanbul'da **300 bozahane** ve ~1100 bozacı var;
ayrıca sarhoş edecek kadar alkollü **acı boza** üreten ~40 esnaf. Bozahaneler
**IV. Murad döneminde kapatıldı**. Yani kahvehaneyle aynı hikâye: 1632'de
açık, hemen sonrasında yasak. 1633 sahnesi kurulursa ikisi birlikte kalkar.

**Muvakkithane'de kısıt varlıkta değil YERDE.** 1632'de vardır — İstanbul'un
ilki Fatih Camii'ninkidir (1470) ve 17. yüzyılda çalışır durumdadır. Ama
yaygınlaşması 18. yüzyıl sonudur: 1632'de muvakkithane bir **mahalle
mescidine değil, selâtin camisine** aittir. Tekkenin minaresizliği gibi bir
kural, ve aynı şekilde testle korunuyor.

## Ölçü nereden geldi

Yedisinden yalnız birinde sayı vardı: **arasta göz genişliği 3,5 m** —
Selimiye Arastası 256 m'de 73 kemer taşıyor (256/73). Ötekilerde ölçü değil
**tarif** var, ve tarifleri doğrulama koşuluna çevirdim: "5–6 m taş oluk" →
oluk uzunluğu 4–8 m dışında reddediliyor; "kule şeklinde" → gövde
yüksekliğin sekizde birinden ince olamaz (yoksa baca olur); "bir iki odadan
büyük olmayan" → muvakkithane bir ya da iki oda.

## Render'ın gösterdiği beş kusur

- Değirmenin **çarkı dolu bir disk**ti — çember kapaksız olmalıydı, kanatlar
  da içeride kalıyordu. Artık kanatlar çemberin dışına taşıyor.
- **Oluk çarkın altında bitiyordu**: su çarkın içinden geçiyordu. Artık
  tepesinin üstünde başlıyor.
- **Ahşap aşı kırmızısıydı.** Aşı boyası ev boyasıdır; değirmen çarkı boyanmaz.
- **İmaretin avlusu kapısızdı** — tekkede düzelttiğim hatanın aynısı.
- **Su terazisinin üst künkü havada duruyordu**: gövde yukarı doğru inceliyor,
  ben iki künkü de taban ölçüsüne göre koymuşum.

Bir de görünmeyen bir hata: `join` fonksiyonu parçaların **döndürülmüş
konumunu yok sayıyordu** ve arasta tonozunun on parçası üst üste yığılıyordu.
Kaynakta düzeltildi; onaylanmış varlıklar bit bit aynı kaldı.

## Bildiğim eksikler

- **Su terazisi hattı yok.** Tek tek kuleler var ama Kırkçeşme **güzergâhı**
  çizilmedi — terazinin anlamı hattadır, bir sıra hâlinde dizilmeleri gerekir.
  GIS işi; Faz 4'e bıraktım.
- **Arasta bir sokak tipolojisi** ve şu an tek prefab. Yerleştiricinin onu
  dizi olarak kullanması Faz 4.
- Bozahane küpleri boş; değirmen taşı içeride ve görünmüyor; muvakkithanede
  saat/rubu tahtası yok. Üçü de yakın plan donatısı (Faz 4).
- **Faz 2b'nin kabul ölçütü açık:** mescidi çekirdek alan bir mahalle sahnesi
  + öğle/gün batımı inceleme paketi + senin onayın.

## Onay

```
OK v1        (ya da: düzeltme istekleri)
```
