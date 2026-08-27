# Kara surları — inceleme notu

Karar kaydı: **ADR 0049**. Araştırma: RESEARCH.md §5.15.

**Bu varlığın render paketi yok** çünkü Blender'da tek bir model değil:
5,8 km boyunca Unity'de kuruluyor. Editörde `Hezarfen → GIS → Kara
surlarını kur` ile bakabilirsin; aşağıdakiler **ölçülen** sayılar.

## Kaynağın verdiği asıl sayı bir toplam

Kaynak katmanların hepsinin genişliğini vermiyor. Verdiği şey **toplam
savunma derinliği: 70 m**. Ara ölçüleri (peribolos, parateikhion, glasi)
tipolojik koydum ama **toplamları o sayıya oturmak zorunda** ve hem
Blender hem Unity tarafı bunu denetliyor.

```
iç sur          5,0      (12 m yüksek, 96 burç × 25 m)
peribolos      20,0
dış sur         2,0      (8,75 m yüksek)
parateikhion   17,0
hendek         20,0      (10 m derin)
glasi           6,0
--------------------
               70,0  = belgeli
```

Uydurulan sayı yok; **paylaşılan bir toplam** var. Payları değiştirmek
serbest, toplamı değiştirmek değil.

## Burç aralığını yazmadım — çıktı

Galata'da "burçlar arası 60 m" diye bir **taslak** yazmıştım, kaynağım
yoktu. Burada gerek kalmadı: kaynak burç **sayısını** veriyor (**96**),
hat **ölçülü** (5 824 m), aralık bölümleri: **60,7 m**.

Ve kaynak aralığı **ayrıca** "21-77 m, çoğu 40-60" diye veriyor — yani
sayılan 96, ölçülen hat ve belgeli aralık birbirini tutuyor. Üç bağımsız
ifade, tek geometri.

## Sahnedeki ölçüler

| | |
|---|---|
| Hat | **5 824 m** (belgeli 7,5 km Blachernae uzantısını da sayar) |
| Kesit | **70,0 m** = belgeli |
| Burç aralığı | **60,7 m** (türetilen) |
| İç burç | **96** (19'u sekizgen) |
| Dış burç | **96** |
| Mazgal | 971 + 971 |
| Toplam üçgen | **99 334** |

192 burcun **hepsi** araziye tam oturuyor (sapma 0,00 m).

## İki kez kendi kuralıma takıldım

1. **"Burç duvarın 1,6 katı olmalı"** kuralını yazmıştım, gerekçesi
   "kaynak 25/12 verir" idi. Dış sur burcunda patladı — ve haklı patladı
   ama **yanlış yerde**: 25/12 **iç surun** olgusu, kaynak dış sur burcuna
   yükseklik vermiyor. Kuralı ait olduğu yere taşıdım.
2. **25 m'yi gövdeye vermiştim**, mesh 28 m çıktı. Ayakta duran bir
   yapının yayımlanan yüksekliği zeminden tepesine ölçülür — korkuluk ve
   mazgal o sayının içinde.

İkisini de render değil, **kendi denetimlerim** yakaladı.

## Sana sorduğum

1. **Kesitin dağılımı** makul duruyor mu? Toplam belgeli ama peribolos
   20 / parateikhion 17 benim tercihim.
2. **Hendek** su dolu mu görünmeli? Kaynaklardan biri "su dolu hendekler"
   diyor, öteki kuru. Şimdilik kuru bir çukur.

## Bilerek eksik

- **Kapılar üretilmedi**: Yedikule, Belgradkapı, Silivrikapı, Mevlânâkapı,
  Topkapı, Edirnekapı, Eğrikapı. Galata kapısını (2 m duvar için)
  buraya **koymadım** — 12 m duvarda ve 25 m burçların arasında yanlış
  okunurdu. Yanlış kapı koymaktansa kapısız bırakmak dürüst.
- **Yedikule Hisarı** ayrı varlık olacak (Fatih 1457, **yedi kule**).
- Hat **11 noktalı** ve kaba: sur 5,8 km boyunca yalnızca on kez
  kırılıyor, gerçeği daha kırıklıdır.
- **FPS ölçülmedi.** 99 334 üçgen ve 192 örnek; ölçülmeden "sorun yok"
  demem.

---

**Onay**: _(bekliyor — "OK vN" yaz)_
