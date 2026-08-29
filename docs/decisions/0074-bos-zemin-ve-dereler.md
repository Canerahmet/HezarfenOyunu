# ADR 0074 — Sur ile mahalle arasındaki boşluk, ve dereler

- **Durum:** KABUL EDİLDİ (Caner, 2026-08-29)
- **Tarih:** 2026-08-29
- **Bağlam:** Faz 8, Caner'in ilk oynanış geri bildirimi

## Sorun

Caner (2026-08-29, oynarken):

> "acik dunya zemini gercekci degil ve cok fazla bos duruyor mesela surdan
> mahalleye kadar bosluk cim var onu yerine araba yollari agaclar vs
> olabilir. onun disinda oyun haritasinda irmak vs olabilir, daha dolu bir
> dunya olsun."

Şikâyet doğru ve ölçülebilir: kara surları ile yerleşim arasındaki alan tek
tip çayır. Şehir 10 km ve o alan yürüyerek geçilen bir hiçlik.

## Neden bu bir ADR

Boşluğu doldurmak kolay; **doğru** doldurmak kolay değil. CLAUDE.md'nin
kuralı burada bağlayıcı:

> Kaynak niteliksel olduğunda metrik geometri UYDURMA — alanı kaba kutu +
> T2 + `status: draft` olarak işaretle ve Caner'e sor.

Elimizdeki kaynak durumu:

| konu | kaynakta ne var | ne yok |
|---|---|---|
| Kağıthane | RESEARCH §"Eyüp": *"mezarlıklar, Kağıthane mesiresi"* — mesire olarak anılır | derenin **güzergâhı**, genişliği, debisi |
| Suriçi batısı | Eremya 14 kapıyı sayar, yol adları geçer | parsel dokusu, bostan sınırları |
| Lykos (Bayrampaşa deresi) | RESEARCH'te **hiç geçmiyor** | her şey |

Yani "İstanbul'da dere vardı" doğru bir cümle, ama Unity'ye bir dere yatağı
çizmek **metrik bir iddiadır** ve o iddianın arkasında bu depoda kaynak yok.
Aynı şey suriçi batısının parsel dokusu için de geçerli.

Bu projede bu tuzağa bir kez düşüldü ve ADR 0024 onu haklı olarak reddetti:
niteliksel bir kaynaktan sınır çizmek.

## Tarihsel not — boşluk aslında boş DEĞİLDİ

17. yüzyılda Theodosius surlarının iç tarafındaki batı üçtebir, bugünkü
anlamda "şehir" değildi; ama çayır da değildi. Dönem anlatıları ve
haritaları burayı **bostan (sulu sebze bahçesi), bağ, meyve bahçesi ve
büyük servili mezarlıklar** ile kapılara giden yollar olarak anar.

Bu, Caner'in istediğiyle **aynı yöne** bakıyor: "daha dolu bir dünya" ile
tarih burada çatışmıyor. Çatışan tek şey **kesinlik derecesi**.

## Seçenekler

### A — Tipolojik doku (öneri)

Boşluğu, konumu kaynaktan gelen **tekil** yapılarla değil, dönemin
**yapı türleriyle** doldur:

- kapılardan yerleşime giden **araba yolları** (toprak/çakıl, T2)
- yol kenarında **bostan parselleri** (dikdörtgen, su arkı, çardak)
- **bağ ve meyvelik** kümeleri
- **servili mezarlık** alanları
- Haliç'in başında **Kağıthane deresi** — ağzı ve genel yönü kaynaktan,
  yatağın kendisi araziden (DEM'in en alçak çizgisi) **türetilir**

Her öğe `HistoricalTag = T2`, `status: draft`. İddia şudur: *"burada bu
türden bir doku vardı"* — *"burada tam olarak bu vardı"* değil.

**Artısı:** boşluk dolar, kural çiğnenmez, iddia dürüst kalır.
**Eksisi:** parsel sınırları bizim uydurmamız — ama T2 bunu zaten söylüyor.

### B — Yalnız yol ve ağaç, parselsiz

Sadece yolları ve ağaç kümelerini koy; bostan/mezarlık dokusuna girme.

**Artısı:** en az iddia.
**Eksisi:** boşluk büyük ölçüde boş kalır, yani asıl şikâyet çözülmez.

## Öneri

**A.** Gerekçe: şikâyet "boş duruyor"; B onu çözmüyor. A'nın taşıdığı risk
parsel sınırlarının uydurma olması, ama bu risk T2 etiketiyle **görünür** ve
geri alınabilir — oysa boş bırakmanın maliyeti her oyun oturumunda ödeniyor.

Derede özellikle dikkatli olunacak: **yatak araziden türetilir**, elle
çizilmez. DEM'in o havzadaki en alçak çizgisi neredeyse dere oradadır; bu
bir ölçüm, bir çizim değil.

## Caner'e soru

1. A mı B mi?
2. Dere için: yalnız **Kağıthane** (kaynakta anılan) mı, yoksa Alibey deresi
   ve suriçindeki Lykos da mı? Son ikisi için bu depoda kaynak satırı yok;
   istenirse önce RESEARCH.md'ye kaynak eklenmeli.
3. "Araba yolları" — yolların **taş döşeli** mi yoksa **toprak** mı olduğu
   ayrı bir iddia. Suriçi ana ekseni (Divanyolu) ile kapı yolları aynı
   olmayabilir.


---

## Karar (Caner, 2026-08-29)

1. **Seçenek A** — tipolojik doku: araba yolları + bostan + bağ/meyvelik +
   servili mezarlıklar.
2. **Üç dere de**: Kağıthane, Alibey, ve suriçinden Marmara'ya inen Lykos
   (Bayrampaşa deresi).
3. **Karışık yol yüzeyi**: suriçi ana ekseni taş, sur kapılarına giden
   yollar sıkışmış toprak.
4. Kamera açılışta **omuz üstü** kalır.

### Kararın kayda geçen bedeli

Alibey ve Lykos için bu depoda **kaynak satırı yok** ve bu ADR onu
gizlemiyor. İkisi de `HistoricalTag = T2`, `status: draft` ile girecek ve
`sourceNote` alanı şunu yazacak: *"güzergâh DEM'den türetildi; RESEARCH.md
bu dere için kaynak satırı taşımıyor, dahil etme kararı ADR 0074."*

Bu, CLAUDE.md'nin "kaynak niteliksel olduğunda metrik geometri UYDURMA"
kuralını çiğnemez çünkü **geometri uydurulmuyor, ölçülüyor**: yatak, DEM'in
o havzadaki en alçak çizgisidir. Uydurulan şey derenin *varlığı* değil —
üçü de bilinen akarsulardır — yalnızca yatağın bugünkü araziden okunması
bir yaklaşımdır ve burada yazılıdır.

Bir gün RESEARCH.md'ye kaynak eklenirse etiketler T1'e yükseltilebilir.

---

## Ek: önerdiğim yöntem ÖLÇÜLDÜ ve çöktü (2026-08-29)

ADR'nin önerisi şuydu: *"yatak elle çizilmez, DEM'in o havzadaki en alçak
çizgisinden türetilir; bu bir ölçüm, bir çizim değil."* O cümleyi sınadım
ve **yanlış çıktı**.

`Hezarfen → GIS → Dere aglarini olc` (kaynak: `DereAgi.cs`) iki ayrı yoldan
baktı:

**1. D8 akış birikimi.** Denize ulaşan en büyük kolun havzası **0,83 km²**.
Kağıthane deresinin gerçek havzası 100 km²'nin üstündedir. Sebep basit ve
düzeltilemez: arazi 15,3 km ve Galata merkezli, yani **derelerin havzaları
haritanın dışında**. Dereler haritaya kenardan, zaten büyümüş olarak girer;
birikim sayacı onları yerel bir yağmur oluğundan ayıramaz.

**2. Kenardan giren vadi tabanları.** Havza büyüklüğünden bağımsız olarak,
arazinin biçimine bakıldı: 2.040 kenar başlangıcı, denize varan yolların
oyuk derinliği **0,4 m**. Yani yatak, 300 m yarıçapındaki çevresinin yarım
metre altında bile değil.

**Sonuç: bu DEM'de oyulmuş vadi yok.** Elimizdeki yükseklik verisi o
ölçekte düzleşmiş. "En alçak çizgiyi izle" dediğimde varsaydığım şey —
derenin araziye bir iz bırakmış olması — bu veride doğru değil.

### Bu neyi değiştirir

Caner üç dereyi de istedi ve bu karar duruyor. Değişen şey **nasıl**
konacağı: artık "ölçüldü" diyemem. Üç seçenek var ve üçünün de bedeli
farklı:

| | ne yapılır | bedeli |
|---|---|---|
| **C1** | Su şeridi, arazinin en alçak sürekli hattına oturtulur (oyuk 0,4 m olsa da o hat en alçak olandır). Arazi oyulmaz. | Güzergâh zayıf bir ölçüme dayanır; dere "vadide akıyor" gibi durmaz, düzlükte akar. |
| **C2** | Yatak **oyulur** (heightmap değişir), sonra su konur. | Görsel olarak doğru dere. Ama arazi değişince üstündeki bütün semtlerin oturması bozulur — **semtlerin tamamı yeniden üretilmeli**. Bugün düzelttiğim sıfır boşluk yeniden ölçülmeli. |
| **C3** | Daha yüksek çözünürlüklü DEM aranır (ADR 0007'nin kaynağı değişir). | En doğrusu, ama Faz 8'in içinde değil; arazi değişirse her şey değişir. |

**Önerim C2**, ve şu sırayla: önce yatak oyulur, sonra semtler yeniden
üretilir, sonra zemin denetimi tekrar koşulur. Semt üretimi bir turda ~8
dakika ve zaten bu oturumda dört kez koştu; asıl maliyet risk değil süre.

C1 daha ucuz ama şikâyeti çözmez: düzlükte akan bir şerit "ırmak" gibi
durmaz, ıslak bir yol gibi durur.

**Caner'e soru:** C1 mi C2 mi? (C3 ayrı bir iş, Faz 8'den sonra.)
