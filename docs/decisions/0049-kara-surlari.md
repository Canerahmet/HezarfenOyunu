# ADR 0049 — Kara surları: uydurulan sayı yok, paylaşılan bir toplam var

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/kara_surlari.md`)
- **Bağlam**: Faz 3, A-kademe. Şehrin **sınırı**; uçarken "burada şehir
  bitiyor" diyen şey.

## Bulgu — kaynağın verdiği asıl sayı bir toplamdır

Theodosius surları tek bir duvar değil, **üç katmanlı** bir sistemdir ve
kaynak katmanların hepsinin genişliğini vermiyor. Verdiği şey:

* iç sur **4,5–6 m** kalın, **12 m** yüksek, **96** burçla; burçlar
  **25 m**, aralıkları **21–77 m** (çoğu 40–60), planları **çoğunlukla
  kare**, bazıları sekizgen/altıgen/beşgen,
* dış sur tabanda **2 m**, **8,5–9 m** yüksek,
* hendek **20+ m** geniş, **10 m** derin,
* **toplam savunma derinliği 70 m**.

Ara ölçüler (peribolos, parateikhion, glasi) belgede yok. "Kaynak
niteliksel olduğunda metrik geometri UYDURMA" kuralı burada şu biçimi
aldı: ara ölçüler tipolojik (**D3**) ama **toplamları belgeli sayıya
oturmak zorunda**.

```
iç sur          5,0
peribolos      20,0
dış sur         2,0
parateikhion   17,0
hendek         20,0
glasi           6,0
--------------------
               70,0  = belgeli
```

Hem Blender kiti hem `LandWallBuilder` bunu **denetliyor**. Uydurulan sayı
yok; paylaşılan bir toplam var ve payları değiştirmek serbest, toplamı
değiştirmek değil.

## Karar 1 — Burç aralığı **elle girilmez**

Galata'da `TowerSpacingDraft = 60f` diye bir **taslak** sayı yazmıştım ve
kaynak vermiyordu. Burada gerek yok: kaynak burç **sayısını** veriyor
(**96**) ve hattın uzunluğu **ölçülü** (5 824 m). Aralık ikisinin
bölümüdür: **60,7 m**.

Ve üçlü kendi kendini denetliyor: kaynak aralığı **bağımsız olarak**
"21–77 m, çoğu 40–60" diye veriyor. 60,7 o bandın içinde. Sayılan 96,
ölçülen hat ve belgeli aralık **birbirini tutuyor** — üç bağımsız ifade,
tek geometri.

## Karar 2 — İki burç planı da üretildi

Kaynak "çoğunlukla **kare**, bazıları **sekizgen**, altıgen ve beşgen"
der. Galata'da bu dersi bir kez almıştım (ADR 0034: *hayatta kalan örnek
örneklem değildir*) ve orada kaynağı zorlayarak çıkarmıştım; burada
kaynak doğrudan söylüyor, yani tek tip üretmenin mazereti bile yok.
Sahnede 96 burcun **19'u** sekizgen.

## Karar 3 — "Dışarısı" ölçülür, yazılmaz

Hangi tarafın şehir olduğu elle yazılmadı: **deniz surlarının** (Marmara +
Haliç) noktalarının ağırlık merkezi alınıyor ve normal ondan uzaklaşan
yöne çevriliyor. Şehir kendi sınırlarından türüyor; bir sabit koysaydım
hat değiştiğinde sessizce ters dönerdi.

## Karar 4 — Arazi **oyulmadı**

Hendek arazinin içine kazılmadı, çukur bir **kabuk** kondu. İki sebep:
arazi oyma DEM'i kalıcı olarak bozar ve geri alınamaz; ve hendek 20 m
genişliğinde, arazi çözünürlüğü **30 m** — yani DEM onu zaten taşıyamaz.

## İki kez kendi kuralıma takıldım, ikisi de öğreticiydi

**1.** `KaraSurBurcuParams.validate` şöyle diyordu: *"burç duvarın
1,6 katı olmalı — kaynak burcu 25, iç suru 12 m verir."* Kural **dış sur
burcunda patladı** (12 / 8,75 = 1,37) ve haklı patladı ama **yanlış
yerde**: 25/12 oranı **iç surun** olgusudur, kaynak dış sur burcuna hiç
yükseklik vermiyor.

Bu kiti `wall_kit`ten ayırma sebebimin aynısını kendi içimde
tekrarlamışım: bir olguyu, geçerli olduğu yapıdan başka bir yapıya
sessizce taşımak. Genel kural artık yalnızca "burç duvardan belirgin
yüksek olmalı" diyor; **25/12 oranı ait olduğu yerde**, üreticinin iç sur
denetiminde duruyor.

**2.** 25 m'yi burcun **gövdesine** vermiştim ve mesh 28,0 m çıktı
(25/12 = 2,08 beklerken 2,33). Ayakta duran bir yapının yayımlanan
yüksekliği **zeminden tepesine** ölçülür; korkuluk ve mazgal dişi o
sayının içindedir. 25 artık **toplam**, gövde ondan türüyor.

İkisini de kendi denetimlerim yakaladı — render değil.

## Sonuç

- `KaraSurBurcu` (kare), `KaraSurBurcu_Sekizgen`, `KaraSurBurcu_Dis`;
  LOD0 312 / 324 / 312.
- Sahnede: hat **5 824 m**, kesit **70,0 m**, aralık **60,7 m**,
  **96 iç burç** (19 sekizgen) + **96 dış burç**, 971+971 mazgal,
  hendek 2 913 dörtgen. Toplam **99 334 üçgen**.
- 192 burcun **hepsi** araziye tam oturuyor (sapma 0,00 m).
- Dış burçların iç hatta dik uzaklığı ortalama **23,69 m** (beklenen
  23,50; sapma dönüşlerdeki poligon ofsetinden).
- EditMode **218/218**.

## Açık kalanlar

- **Kapılar üretilmedi**: Yedikule, Belgradkapı, Silivrikapı, Mevlânâkapı,
  Topkapı, Edirnekapı, Eğrikapı. Galata kapısı (2 m duvar için) buraya
  **konmadı** — 12 m duvarda ve 25 m burçların arasında yanlış okunurdu.
  Yanlış kapı koymaktansa kapısız bırakmak dürüst.
- **Yedikule Hisarı** ayrı varlık olacak: Fatih **1457**'de Altın Kapı'nın
  arkasına **yedi kuleli** yaptırdı; 1632'de 175 yaşında.
- Hattın kendisi **11 noktalı** ve kabadır; sur 5,8 km boyunca yalnızca on
  kez kırılıyor. Gerçek hat daha çok kırıklıdır.
- 99 334 üçgen ölçüldü ama **FPS ölçülmedi**.
