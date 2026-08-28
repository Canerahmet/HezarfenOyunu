# ADR 0072 — Kalıcı ışık pası: sıçramayı taklit etmek yerine hesaplamak

- **Durum:** uygulanıyor (Claude, 2026-08-28)
- **Bağlam:** Faz 7, görsel cila (PLAN Bölüm 12)
- **İlgili:** ADR 0023 (geçici aydınlatma takımı), ADR 0025 (güneş ve vakit)

## Sorun

PLAN Bölüm 12 Faz 7'nin ilk işini açıkça yazıyor:

> Faz 7 başlarken yapılacak **İLK iş**: geçici aydınlatma takımını
> **SİLMEK**. Kalıcı ışık pası bunun **üstüne** kurulmaz, **yerine**
> kurulur.

Geçici takım (ADR 0023) üç turdur açık duran bir boşluğu kapatmak için
kurulmuştu: gölgedeki sıva duvar 30/255, kaldırım 3/255 ölçülüyordu ve
yaya seviyesinden inceleme paketi üretilemiyordu.

Eksik olan şey "ışık" değildi. Sahnede fizikî gökyüzü vardı ve gök ışığı
geliyordu. Eksik olan tek terim **sıçrama**ydı: HDRP'de gerçek zamanlı
küresel aydınlatma yoktur, ışık pişirilmediği sürece bir duvar yalnızca
güneşi ve göğü görür — karşı duvardan ve yerden dönen ışığı görmez. Dar
bir Osmanlı sokağında ise gölgeyi asıl dolduran şey odur: kireç badanalı
cepheler ve taş kaldırım çok iyi yansıtır.

Geçici takım o terimi **taklit ediyordu**: iki gölgesiz dolgu ışığı
(biri güneşin karşısından "gök sarması", biri aşağıdan yukarı "yer
sıçraması"), gök teriminin 2,4 katına çıkarılması, ve pozun 14,5'ten
13,0 EV'ye çekilmesi. Üçü de fizikî değil, kadraja bakılarak ayarlanmış.

## Seçenekler

### A — Işık haritalarını pişir (klasik lightmap)

Statik geometri için ışık haritası, dinamikler için ışık probu.

- **Artı:** en yüksek kalite; sıçrama gerçekten hesaplanır.
- **Eksi:** şehir **akıyor** (Addressables ile semt sahneleri, ADR 0011)
  ve gün **dönüyor** (ZamanSistemi güneşi sürüyor). Işık haritası sabit
  bir güneş açısı varsayar. 1631–1633 arası bir takvim taşıyan ve beş
  vakti güneşten hesaplayan bir oyunda pişmiş bir öğle ışığı, sabahı ve
  akşamı yalan söyler.
- **Eksi:** on kilometrelik bir arazi + 1538 düğümlük bir şehir için UV
  ve pişirme maliyeti Faz 7'nin bütçesini tek başına yer.

### B — Adaptive Probe Volumes (APV) + kademeli SSGI (ÖNERİLEN, seçildi)

İki katman, ikisi de gerçek:

1. **APV** — temel. Sahne bir prob ızgarasıyla kaplanır, sıçrama
   pişirilir. Işık haritasının aksine **geometriye değil hacme** bağlıdır:
   UV gerekmez, akan içerikle çalışır, her donanım kademesinde açıktır.
2. **SSGI** — üstüne. Ekranda görünen yüzeylerden gelen anlık sıçrama;
   probların kaçırdığı ince ayrıntıyı (cumba altı, kemer içi, saçak
   gölgesi) tamamlar. PLAN'ın dediği gibi **kademeli**: Performant'ta
   kapalı, Balanced ve High'da açık.

- **Artı:** dar sokak tam da SSGI'ın en iyi çalıştığı durum — karşı cephe
  zaten ekranda.
- **Artı:** APV pişirmesi güneş açısından bağımsız bir *hacim* kaydı
  olarak kullanılabilir ve gün dönerken tümüyle yalan söylemez.
- **Eksi:** APV yine de bir pişirme; şehir büyüdükçe pişirme süresi ve
  bellek bütçesi (`probeVolumeMemoryBudget`) izlenmeli.
- **Eksi:** SSGI ekran uzayıdır — ekranda olmayan bir yüzeyden ışık
  gelmez. Bu yüzden **tek başına** yeterli sayılmadı; temel APV'dir.

### C — Geçici takımı olduğu gibi bırak

- **Eksi:** PLAN bunu açıkça yasaklıyor ve haklı: takım pozu da
  bozuyordu. Fizikî olmayan bir taban üstüne kurulan her ışık kararı
  o tabanın hatasını taşır.

## Karar

**B.** Poz da fizikî değerine döndü: sabit 13,0 EV yerine **otomatik poz**
(histogram), alt sınır 6 EV (alacakaranlık), üst sınır 16,5 EV. Sabit bir
EV gün boyu dönen bir güneşle çalışmaz — öğleyin doğru olan değer
alacakaranlıkta sahneyi karartır. Göz de uyum sağlar; kamera da sağlasın.
Uyum hızı **kasıtlı olarak yavaş** (karanlıktan aydınlığa 1,2): karanlık
bir hana girince bir an hiçbir şey görmemek, sonra gözün açılması o
mekânın duygusudur ve hızlı uyum onu tümden siler.

Sis de buraya bağlandı: Haliç sabahı bir efekt değil bir **yer** bilgisi
(taban 0 m = deniz seviyesi, tavan 140 m = Galata sırtının üstü).

## Ölçü değişmiyor

`LightingTests.StreetIsReadableAtEyeLevel` **yerinde kalıyor** ve eşiği
değişmiyor: gölgedeki cephenin ayrıntı enerjisi **> 1,2**.

Bu test bilerek bir *gerekliliği* ölçüyor, bir uygulamayı değil (ADR 0023
notu): "kim sağlarsa sağlasın, göz hizasından bakıldığında kare okunabilir
olmalı". Geçici takımla ölçülen değer **2,62**. Kalıcı pas o eşiği kendi
başına geçmek zorunda — geçemezse kalıcı pas eksiktir, test değil.

## Ölçülen sonuç

| | ayrıntı enerjisi | ortalama | koyu (<30) |
|---|---|---|---|
| geçici takım (taklit) | **2,62** | 55,7/255 | %30,4 |
| kalıcı pas (APV + SSGI) | **2,15** | 50,6/255 | %29,8 |
| eşik | 1,2 | | |

Kalıcı pas eşiği **kendi başına** geçiyor: taklit dolgular yok, gök
çarpanı yok, poz elle çekilmiyor. Taklitten biraz daha düşük çıkması
beklenen ve doğru olan şey — sahte dolgu gölgeyi gerçekte olduğundan
fazla dolduruyordu.

## Not: boş profil, sessiz karartma

İlk kurulumda ölçüm **0,55**'e düştü — eşiğin de, geçici takımın da çok
altına. İlk şüphem sıçramaydı; yanlıştı.

Sebep: `VolumeProfile.Add<T>()` bileşeni yalnızca **bellekte** kurar.
Diske yazılması için ayrıca `AssetDatabase.AddObjectToAsset` gerekiyor.
O çağrı olmadığı için profil dosyası oluştu, menü "kuruldu" dedi, ama
profilin içinde **hiçbir şey yoktu** — ne poz, ne sis, ne SSGI. Geçici
takımın 13,0 EV'si kalkınca sahne üç durak karardı.

*Boş bir profil, olmayan bir profilden daha kötüdür:* olmayan profil
hata verir, boş profil "kuruldu" der. Artık `Profil()` bileşen sayısını
sayıyor ve sıfırsa hem profili yeniden kuruyor hem hata basıyor.

## Not: prob sıklığı — 7 kat veri, sıfır kalite

İlk pişirme **191 MB** tuttu (tek bir sandık sahnesi için). Varsayılan
`minDistanceBetweenProbes = 1 m`, 729 × 162 × 729 m'lik bir kutuda.

3 m'ye çekildi:

| prob aralığı | veri | ayrıntı enerjisi |
|---|---|---|
| 1 m | 191 MB | 2,15 |
| 3 m | **26 MB** | **2,15** |

Yedi kat küçülme, ölçülebilir kalite kaybı **yok**. Sıçrama düşük
frekanslı bir terim — metrelik bir ızgara onu zaten yakalıyor, santimlik
ızgara aynı sayıyı yedi kat yer kaplayarak veriyor.

*Bir düzeltmenin hiçbir şeyi değiştirmemesi de bir ölçümdür* — burada
kaliteyi değiştirmedi ve tam da bu yüzden doğru düzeltme.

## Prob verisi depoya GİRMEZ

26 MB tek sahne için, ve Faz 7 boyunca ışık ayarlandıkça defalarca
yeniden pişecek. Her pişirme LFS'e **kalıcı** yeni bir kopya yazardı
(CLAUDE.md uyarısı birebir bu). Kural zaten yazılı: **türetilmiş veri
girmez, kaydı girer** — pişirme ayarları `Baking Set.asset`'te (45 KB) ve
bu ADR'de, komut menüde.

Faz 8'de (paketleme) ışık kesinleştiğinde yeniden değerlendirilecek.

## Not: prob hacmi ARAZİYİ kapsamamalı

İlk uygulamada prob hacmi `Mode.Global`di — "sahnenin tamamını kapla".
Pişirme **bitmedi**: editör on beş dakika donduktan sonra tek satır çıktı
bile üretmemişti.

Sebep ölçüldü: sokak sandığı (`Faz2_GalataSokagi`) **araziyi de
içeriyor**. Yani "sahnenin tamamı" 10 km × 10 km demek ve APV'ye 100
km²'lik boş bir yamacı prob prob pişirtmeye çalışmışım.

Doğrusu: prob hacmi **yapıların** sınırlarına oturur, arazinin değil
(`YapiSinirlari()`; kenar en çok 600 m). Sıçrama terimi binaların
arasında gerekli — boş yamaçta gökyüzü zaten her şeyi görüyor, orada
çözülecek bir sorun yok.

Bu, "sahnenin tamamı" gibi masum görünen bir ifadenin ne kadar
büyüyebileceğinin kaydı: şehir sahnesi bir sokak değil, bir coğrafya.

## Not: pişirme çağrısı

İlk uygulamada `Lightmapping.Bake()` çağrılmıştı. O çağrı sahnenin **her
şeyini** pişirir (ışık haritaları dahil), eşzamanlıdır ve editörü
kilitler — editör dakikalarca yanıt vermedi ve hangi işin sürdüğü bile
görünmedi. Doğrusu `AdaptiveProbeVolumes.BakeAsync()`: yalnız problar,
asenkron.
