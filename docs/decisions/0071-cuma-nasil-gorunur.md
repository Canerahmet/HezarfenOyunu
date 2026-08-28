# ADR 0071 — Cuma oyunda nasıl görünür

- **Durum:** kabul edildi (Claude, 2026-08-28)
- **Bağlam:** Faz 6, dinamik olaylar (PLAN Bölüm 11.1)
- **İlgili:** ADR 0069 (sokak grafı), ADR 0070 (NPC rutini saf işlev),
  ADR 0018 (kilise ve sinagog)

## Sorun

Cuma haftanın tek özel günüdür ve oyunun haftalık ritmi odur. Rutin bunu
bilmezse Cuma sıradan bir gün olur — yani oyunda **hiç görünmez**.

Cuma namazı **mahalle mescidinde kılınmaz**: minberi olan bir camide,
cemaatle kılınır. Yani Cuma iki şey yapar:

1. **Toplar.** Beş vaktin dağınık akışı yerine tek bir yere yönelme.
2. **Çoğaltır.** Sıradan bir öğleden belirgin kalabalık.

Bunun ölçülebilir olması gerekiyor, yoksa "Cuma var" demek bir iddia
olarak kalır.

## Yakalanan hata

İlk yazımda `CumaHedefi` şuydu:

```csharp
normal == SokakGrafi.Tur.Mescit ? SokakGrafi.Tur.Mabet : normal
```

Ve testi **geçti**. Çünkü test yalnızca eşlemenin kendisiyle tutarlı
olduğuna baktı.

Oysa `SokakGrafi.Tur.Mabet` grafta **kilise/sinagog** demektir — sınıfın
kendi belgesi öyle yazıyor, graf kurucusu `PF_Kilise` ve `PF_Sinagog`'u
oraya bağlıyor (ADR 0018). Yani kod, Cuma namazı cemaatini **kiliseye**
yolluyordu. Sayı doğruydu, adres yanlıştı.

Bu, projenin kendi kuralının bir örneği daha: *bozuk olan çoğu zaman
ölçtüğün şey değil, ölçme biçimin.* Test eşlemeyi kendisiyle kıyasladı,
şehirle değil.

## Seçenekler

### A — Mescit düğümlerinden birini "Cuma mescidi" say

Her semtte bir `Mescit` düğümü Cuma günü hedef olarak işaretlenir; kalan
129'u boş kalır.

- **Artı:** graf değişmez, yeniden kurulmaz, iş bugün biter.
- **Eksi:** tarihsel olarak **yanlış**. Mescidin minberi yoktur; Cuma
  namazı orada kılınmaz. Şehrin en görünür haftalık olayını, olmadığı
  bir yere kurmak olurdu.

### B — `Cami` düğüm türü ekle, selâtin camilerini bağla (ÖNERİLEN)

`SokakGrafi.Tur`'a `Cami = 15` eklenir ve graf kurucusu **zaten dünyada
duran** selâtin camilerini bu türe bağlar: Süleymaniye, Ayasofya,
Sultanahmet, Fatih, Beyazıt (Suriçi); Üsküdar Mihrimah, Doğancılar
Camii, Hüdâyî tekke-camii (Üsküdar).

- **Artı:** doğru. Cami zaten üretilmiş, zaten yerinde, zaten belgeli
  konumda (ADR 0044/0045/0046/0048). Yapılan tek şey grafın onları
  **tanıması**.
- **Artı:** Cuma akışı gerçek bir mesafe doğurur — mahalleden camiye
  yürünür, sokaklar dolar. Mescit versiyonunda kimse 40 metreden fazla
  yürümezdi.
- **Eksi:** graf yeniden kurulur.
- **Eksi:** **Galata'nın Cuma camisi yok.** Arap Camii katalogda var ama
  üretilmedi (`LandmarkPlacer.Built` listesinde yok). Eyüp'ünki de yok.

`Yeni Cami` bu listeye **girmez**: 1632'de yarım kalmış bir harabedir
("Zulmiyye"), cemaati yoktur.

## Karar

**B.** Ayrıca Galata'nın boşluğu kapatılır: **Arap Camii üretilir**.

Galata'yı boş bırakmak seçenek değil — Faz 6'nın kabul kriteri
*"Galata'da 30 dakika kesintisiz serbest dolaşım"* diyor. Cuma, oyuncunun
fiilen bulunduğu yerde hiçbir şey yapmıyorsa üretilmemiş demektir.
*Üretilen ama görünmeyen bir öğe, olmayan bir öğedir.*

Arap Camii'nin üretimi ucuz çünkü yapı zaten elimizde: **Arap Camii,
San Domenico'dur** — 1475'te camiye çevrilen Dominiken kilisesi, çan
kulesi minareye dönüştürülmüş. Üç nefli bazilika, ~40 × 15 m, moloz taş
(RESEARCH §4.2(a), Koç Ü. İstanbul Surları). `gen_church_kit.py` bu
biçimi zaten üretiyor; fark çan kulesinin minareye çevrilmesi — yani
tarihin kendisinin yaptığı değişiklik.

Eyüp Sultan bu turda **kapsam dışı** ve öyle kaydediliyor: Eyüp'te 103
düğüm var ama Cuma camisi yok, dolayısıyla o semtte Cuma ölçülemez. Bu
bir eksik, sessiz bir varsayım değil.

## Rutinin tek sahibi

Aynı turda ikinci bir sorun görüldü: kahvehane kronolojisi **iki yerde**
yazılıydı — `SehirGunu.Olc` ve `NPCYonetici.HedefleriYenile`. İkisi de
aynı kuralın kendi kopyasını taşıyordu.

*Bir sayının iki sahibi varsa er ya da geç iki değeri olur.* Cuma'yı da
aynı şekilde iki yere yazmak, simülasyonun ölçtüğü Cuma ile oyuncunun
yürüdüğü Cuma'nın ayrışması demekti — ve bu ayrışma **sessiz** olurdu,
çünkü testler simülasyona bakar.

Bu yüzden `Rutin` sınıfı yazıldı: `(meslek, vakit, tohum, yıl, gün) →
hedef`. Kronoloji ve olaylar **yalnız orada** uygulanır. Hem simülasyon
hem canlı şehir aynı işlevi çağırır, yani ölçülen gün oynanan gündür.

## Kalabalık nasıl çoğalır

`Olaylar.CumaKatsayisi = 1.8`. Uygulama, rutinin saf olma özelliğini
bozmadan:

- temel hedefi cami olan zaten camiye gider (pay `p` korunur),
- olmayanlar `q = p(k−1)/(1−p)` olasılığıyla camiye çekilir.

Sonuç pay `p + (1−p)q = pk` — yani ölçülen Cuma payı, sıradan öğle
payının tam **k katı**. Katsayı bir yerde yazılı ve ölçüm onu doğruluyor;
elle ayarlanmış bir sayı değil.

`k = 1.8` bir **T2** okumasıdır: Cuma namazının zorunlu ve toplu olduğu
belgelidir, katılım oranı sayıyla kayıtlı değildir. Sayı burada yazıyor
ve tek yerde duruyor.
