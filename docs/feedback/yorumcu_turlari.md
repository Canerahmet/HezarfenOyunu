# Yorumcu turları — inceleme kaydı

Caner (2026-08-31): *"3 tane oyun yorumcusu gibi davranan agent oluştur…
bunları verdiği geri bildirimlere göre oyunu iyileştir… 10 tur boyunca
bunu sürdür… en sonunda bir son kullanıcı agent olsun."*

## Yöntem ve sınırı — açıkça

Yorumcular oyunu **oynamıyor**. Ellerinde oyun içinden yakalanmış kareler,
ölçüm tabloları, kod ve tasarım belgeleri var. Her tur başında kanıt
tazeleniyor (tur karesi, kare bütçesi, uçuş denemesi). Bir gözlemin
uydurulmaması için her iddianın bir dosyaya, kareye ya da sayıya
dayanması isteniyor — ve bu kural bir kez işe yaradı: 1. turda bir
yorumcu "ahşap dokularında normal haritası yok" dedi, `.mat` dosyasına
bakılınca `T_WeatheredPlanks_N`'i kullandığı görüldü ve iddia düştü.
3. tur talimatına bu örnek eklendi.

---

## Tur 1 — harita / kontroller / grafik

**En ağır bulgu, ve bir yazılı iddiamı çürüttü:** aydınlatma pası diske
hiç yazılmamış. `VolumeProfile.Add<T>()` bileşeni yalnız bellekte kurar;
`AddObjectToAsset` çağrılmadığı için profil beş bileşende kalmış. Geçiş
konsola altı satır "eklendi" yazmış, oyun ortam örtme, temas gölgesi,
bloom, renk derecelendirme ve vinyet olmadan çalışıyormuş. Aynı tuzak bu
depoda ikinci kez kurulmuş — `KaliciAydinlatma` yaşamış, düzeltmiş ve
gerekçesini yanına yazmış; yeni dosya o deyimi kullanmamış.

| bulgu | kaç yorumcu | sonuç |
|---|---|---|
| Gece kör karanlık | 3/3 | ay + fener + poz tabanı −1 EV |
| Aydınlatma pası diske yazılmamış | 1 | 11 bileşen + varlık dosyasını okuyan test |
| Replikler üst üste biniyor | 3/3 | ayrım ekran uzayına taşındı |
| NPC'ler tek renk manken | 2 | yalnız dış giysi boyanır (→ 3. turda düzeltildi) |
| Işınlanma çatıya/çeşmeye koyuyor | 2 | eşik 2 m → 0,35 m |
| `E` iki işe bağlı | 1 | kanat `G`'ye |
| Etkileşim duvar geçiyor | 1 | görüş hattı ışını |

## Tur 2 — uçuş

Uçuş sistemleri (`WindField`, `WindVolume`, `TerrainThermal`,
`UcusKamerasi`, `FlightHud`) yazılmış, test edilmiş ve **sahneye hiç
konmamış**. Bedeli aritmetik: kule–Doğancılar 3.336 m, kot farkı 53,2 m,
kanat 11,56:1 → menzil 616 m. Oyunun finali bitirilemezdi ve hiçbir test
sormuyordu, çünkü bütün uçuş testleri kanadın **fiziğini** soruyordu,
kanadın **gittiği yeri** değil. ADR 0082.

Ölçümü çalışır hâle getirmek **altı koşum** aldı ve altısı da uçuşun
dışında bir yerde takıldı: kalkış noktası kule tabanı (yer seviyesi),
kuşanma sırasında dama düşme, kalkışta zeminin ayağın altında bulunması
(bu **gerçek bir oyun hatasıydı** — düz damın ortasından kalkan anında
iniyordu), ve en sonunda `transform` ile `Rigidbody` arasındaki sahiplik.

## Tur 3 — uçuş fiziği / görsel / oynanış

**Oynanış:** `GorevUretici`, `Kese`, `FenerVar`, `DurumuGeriYukle`,
`SeviyeyiGeriYukle`, `AsamaDegisti`, `SuAnkiIhlal` — yedisi de yalnız
kendi dosyasında geçiyor, hiçbirini çağıran yok. Ve Faz 6 kapısı yeşildi:
kapıyı geçen test görevi **kendisi oynuyordu**
(`while (!q.Bitti) q.DurakTamam();`). Bir kapının en tehlikeli hâli,
yanlış şeyi ölçüp yeşil yanmasıdır.

**Uçuş fiziği:** yorumcu fizik motorunu Python'da yeniden kurdu ve iki
bağımsız çapayla doğruladı (teorik L/D 11,56 ve ADR 0037'nin termik
sayıları). Sonuç: **kanat sağlam** (eller serbest 11,22:1). Kusur üçlü —
sürekli yatışta sarmal ıraksaması, ilk kareden tam yatış komut eden
otomatik pilot, ve yer eksenli "tırmanıyor" ölçütünün fugoid salınımı
termik sanması. Ayrıca termik **var**, kuleden 160 m **batıda** (+1,87
m/s) — pilot doğuya, hedefe doğru uçuyordu.

**Kendi aracımda bir dürüstlük hatası:** deneme 20 uçuş rapor ediyordu,
gerçekte 5'i tekrarlanıyordu; türbülans yok ve zaman adımı sabit olduğu
için tekrarları farklılaştıracak hiçbir kaynak yoktu.

**Bir teşhis, doğru kısmıyla birlikte yanlış kısmını da taşıyabilir.**
Yatış telafisi ölçülebilir kazanç verdi (batış 2,49 → 2,12 m/s, hiçbir
testi kırmadan). Aynı raporun aynı güvenle yazdığı ikinci yarısı
(kararlılık terimini komut edilen açıya itmek) düz uçuşu 11,2:1'den
2,57:1'e çökertti ve beş testi birden kırdı — geri alındı, gerekçesi
koda yazıldı.

**Cırcır:** kapanmamış bir kusuru dürüstçe taşımanın yolu, bugünkü sayıyı
tavan yapıp hedefi yazılı bırakmak. Kalıcı kırmızı bir test yapıyı
bloke eder ve kırmızıya bakmayı öğretir.

## 8. tur — 2026-09-01

Üç yorumcu ajanı (dünya/görev/ekonomi, kontrol/his, grafik/animasyon).
Yirmi dört bulgu, on dokuzu bu turda kapatıldı.

### Bu turun asıl dersi

Bulguların **üçü, benim daha önce "yaptım" diye rapor ettiğim işlerin
ekranda hiç görünmediğini** gösterdi:

| yazdığım | gerçekte olan |
|---|---|
| "Balonlara kontur verildi" | öteleme bakış ekseninde → ekranda 0,075 px |
| "LOD geçişleri yumuşatıldı" | komut 26 varyantken koşmuş; 201 evin hepsi sert |
| "Gece oynanabilir oldu" | `limitMin −1` kelepçeyi ısırtmıyor, gece = gündüz |
| "Toplanan su bir yere gider" | `SatisFiyati = Fiyat−1` → 1 akçelik mal satılamaz |

Dördü de aynı biçimde kaçtı: **düzeltme yazıldı, diske geçti ve
ölçülmedi.** Bu oturumun tekrar eden cümlesinin yeni hâli — *bozuk olan
çoğu zaman ölçtüğün şey değil, ölçme biçimin* — burada şu: bir
düzeltmeyi ölçmemek, onu yapmamakla aynı şeydir.

### İkinci ders: tahmin edilen darboğaz ≠ ölçülen darboğaz

Yorumcu replik maliyetini (+1,0 ms) görüş hattı ışınına bağladı. Işını
tembel yaptım — doğru bir düzeltmeydi ve **sayı oynamadı**. Ölçüm asıl
sebebi söyledi: `BarkGosterici` her karede 40.000 sakinin tamamını
tarıyor, gövdesi olan altmışı bulmak için. Düzeltildi: +1,0 → **+0,3 ms**.

### Üçüncü ders: cetvel düzeltmenin kopyası olmamalı

`InsanYerlesimi` gövdeyi oturtan ışının **birebir aynısını** atıp farkı
soruyordu; "havada/gömülü" sütunları yapısal olarak 0 çıkacaktı. Artık
ayak kemiğini ölçüyor. Bu depoda üçüncü kez aynı tuzak (`PermeTests`
sentetik graf, `OrtamSesiTests` atanmamış katman) ve bu kez ben kurmuşum.

Ve dördüncü: kare ölçümünü toplu kipe taşıdım, ilk koşum "0,1 ms,
çizim çağrısı 0" yazdı. Cetvel bir sayı üretti, hiçbir şey ölçmedi.
Artık sahneyi kendisi açıyor ve boş sahneyi **reddediyor**.

### Ölçülen durum

| ölçü | önce | sonra |
|---|---|---|
| EditMode testi | 423 | **426 yeşil**, 0 derleme hatası |
| Görev arketip çeşitliliği | 1 (20/20 `Kayip`) | **4** |
| Ortalama görev yolu | 882 m | 942 m (7,1 dk) |
| Bağlı iskele | 5 | **6** (Üsküdar iki yönlü) |
| Sert LOD sıçraması | 215 prefab | **0** (kapı testi) |
| Gölge mesafesi | 150 m | **320 m** |
| Gece poz tabanı | −1,0 EV (ısırmıyor) | **2,0 EV** (kapı testi) |
| Kare (toplu kip) | 11,6 ms | **11,1 ms** (bütçe 16,7) |
| Replik ücreti | +1,0 ms | **+0,3 ms** |

### Kapatılmadan devredilenler

- **NPC'lerle konuşulamıyor.** `IEtkilesim`'i üç sınıf uyguluyor,
  hiçbiri insan değil. Görevi kimse vermiyor: `Start()` sıfırıncı
  saniyede iş atıyor. 1. yorumcunun en ağır bulgusu ve en büyük iş.
- **Graf şehrin %1'ini tanıyor**: 12.248 evin 142'si düğüm.
- **LOD1 siluetin %2'si** (2.328 → 56 üçgen); finalin 3.336 m'lik
  süzülüşü 20 üçgenlik kutuların üstünde geçiyor.
- **Volumetrik bulut üç HDRP varlığında da kapalı** → `HavaProfili`nin
  bulut dalı ölü kod. Uçuş oyununda boş gökyüzü.
- **SSGI profilde var, etkin ardışık düzende derlenmiyor** — ADR gerek.
- **Uçuş kamerası** eğimi donuyor, roll yok; `UcusKamerasi` yalnız
  `FlightSlice`'ta. Uçuşta ses hıza tepki vermiyor.
- **Duraklat menüsü kolla kullanılamıyor** (açılış menüsü bu dersi
  öğrenmiş, duraklat menüsü habersiz).
- `Kacakcilik` 196 gerçek saat arkasında kilitli.
- ADR 0084 hâlâ Caner'in kararını bekliyor.

## 9. tur — 2026-09-01

Üç yorumcu, yirmi bir bulgu. Turun iki büyük devredilen maddesi kapandı
ve bir yayımlanmış sayı düzeltildi.

### Düzeltme: "862 çizim çağrısı" sahnenin değildi

8. turun özetinde bu sayıyı sahnenin çizim çağrısı diye yazdım.
Değilmiş: `AgacCizici.CizimCagrisi`, yani yalnız ağacın
`RenderMeshInstanced` çağrıları. 12.248 ev, 60 gövde, arazi ve son
işlem o sayıya hiç girmiyordu. **Ekranı belirleyen iki sayı — sahne
çizim çağrısı ve sahne üçgeni — hiç ölçülmüyordu**, ve doğru cetvel
(`FrameTimeProbe` → `UnityStats`) projede zaten vardı.

Ve cetveli düzeltmeye çalışırken iki kez cetveli bozdum:
1. `UnityStats` toplu kipte **sıfır** dönüyor (Game view'a bağlı).
   Sıfırı sonuç diye yazmak, tam da düzeltmeye çalıştığım kusurdu.
2. Profil sürücüsünü açtım: sayaçlar yine sıfır, **ve kare 10,8'den
   12,4 ms'ye çıktı**. Hiçbir şey kazandırmayan 1,6 ms'lik bir ölçüm
   bozulması. Geri alındı.

Bugünkü hâl: rapor sıfır yazmıyor, **okunamadığını söylüyor**. Sahne
sayaçları Editor'den okunmalı — bu bilinen ve yazılı bir sınır.

### Kapatılanlar

**Dünya / sistem**
- Kalabalıkla konuşulamıyordu; iş konveyörden geliyordu →
  `Sakin` + iş **konuşulan kişinin yanında** üretiliyor. Görevi bir
  düğüme bağlamak, oraya kimse uğramazsa oyunu kilitlerdi.
- Oyun artık **işsiz başlıyor**, biten işin yerine kendiliğinden
  yenisi konmuyor.
- İşi kim verdiği önemsizdi: 7.200 çocuk kaçakçılık teklif ediyordu →
  `GorevUretici.Verebilir(meslek)` tablosu. Ases ve yeniçeri iş
  vermez ama **susmaz** — susan NPC dekordur.
- Ana hikâye ekranda hiç yoktu (`AsamaDegisti` sıfır abone) → kalıcı
  hedef satırı, yön ve talim sayacı (n/3).

**Uçuş / his**
- Nötr çubuk en iyi süzülüş değildi: 12,5° (en az batış) yerine 6,2°.
  Ölçülen bedel menzilin %15'i, ve klavye o noktayı hiç tutturamıyordu.
- Yatış telafisi 33,6°+ dönüşte **stall komut ediyordu** (tavan 24°,
  stall 15°). Tavan artık yalnız telafinin fazlasını kırpıyor —
  ilk hâlinde bilerek stall'a girmeyi de engellemiştim, ölçüm yakaladı.
- Alan açısı oyunda **hiç yoktu** (`UcusKamerasi` iki sahnenin de
  sıfırında, `fieldOfView` yazan çalışma zamanı satırı yok) →
  `KameraKipi` 55°→78°.
- Uçuşta ses yoktu → `UcusSesi`: hava hızına bağlı şiddet ve perde,
  stall'da kumaş katmanı.
- HUD yön ve "yetişir mi" demiyordu → pusula + süzülme konisi.
- Duraklat menüsü kolla kullanılamıyordu → D-pad + görünür seçim.

**Grafik**
- Atmosfer 64 m'de bitiyordu → sis derinliği 900 m, ileri saçılma
  0,45, çoklu saçılma 0,15. Ölçülen maliyet: **0,0 ms** (tahmin tuttu).
- Kaskad bandı yoktu — geçen tur mesafeyi 150→320 yapıp bandı
  koymamıştım, yani uzatmak halkaları daha görünür yapmıştı.
- SSGI profilde tam donanımlı duruyordu ve etkin ardışık düzen onu
  hiç derlemiyordu → profilden silindi (APV zaten pişirilmiş).

### Ölçülen durum

| ölçü | 8. tur | 9. tur |
|---|---|---|
| EditMode testi | 426 | **434 yeşil**, 0 derleme hatası |
| Kare (toplu kip) | 11,1 ms | **11,0 ms** (bütçe 16,7) |
| Replik ücreti | +0,3 ms | **+0,1 ms** |
| İş veren | yönetici (konveyör) | **insan**, mesleğine uygun iş |

### Devredilenler

- **Okmeydanı build'in içinde yok**: 9 menzil taşı `Sandbox/`de kaldı,
  250 m yarıçapında **0 graf düğümü, 0 sakin, 0 iş veren**. Oyunun
  ilk perdesi şehrin tek boş odasında geçiyor.
- **İtibar bağlanmamış**: `Odul`un beş bayrağından dördü hiçbir yerde
  okunmuyor. 30 görev sonunda değişen tek şey kese.
- **Kanat parçası enum adı**: dünyada 0 tane, satılmıyor, hiçbir görev
  taşıtmıyor — oyunun adını taşıyan aygıtın parçası.
- **Aranma katmanı 196 gerçek saat kilitli**: iki ihlalin ikisi de
  1633 fermanına bağlı; `YasakBolge` ve `Kacmak` hiç aday olmuyor.
  Feneri fermandan ayırmak bir tarih kararı → ADR gerek.
- **775 `HistoricalTag` diskte, sıfır okuyucu** — kodeks yok, harita
  yok.
- **Adres katmanı**: 12.248 evin 142'si düğüm; "kayıp eşya" hep aynı
  24 kapıdan birine gidiyor.
- **NPC LOD2 yok**: 60 gövde × 16.548 üçgen; `KarakterUc` merdiveni
  yazılı, prefabta üçüncü kademe olmadığı için hiç seçilmiyor.
- **Kalabalık tek fazda yürüyor** (`m_CycleOffset: 0`), gövde tek
  karede yok oluyor.
- **LOD1 5 malzeme taşıyor** (56 üçgen, 5 alt-mesh); 201 varyantın
  hepsi LOD1'de tek mesh'e çöküyor.
- **Gökyüzü boş**: `cloudType: 0` ve gökyüzü rüzgârı literal olarak 0.
- **SSR kapalı, su var**: Galata Kulesi Haliç'te görünmüyor.
- **DLSS/dinamik çözünürlük kapalı** — 4070'te masada bırakılmış süre.
- ADR 0084 hâlâ Caner'in kararını bekliyor.

## 10. tur — 2026-09-01 (son yorumcu turu)

Üç yorumcu, on yedi bulgu. Bu tur ayrıca "ilk iki saat", "ilk uçuş" ve
"ilk bakış" anlatılarını istedi; onlar son kullanıcı aşamasına
devredilen asıl belge.

### İki yayımlanmış iddiam çürütüldü

**1. "Menzil %15 arttı" (9. tur) — gerçek kazanç 12 metre.**
İki tork terimi aynı anda çalışıyor: `pitchAuthority` (2,2) hedefe
çevirir, `pitchStability` (0,8) açıyı sıfıra geri çeker. Denge
`α = hedef × 2,2/3,0`, yani kanat komut edilenin **%73'ünü** uçuyor.
`BestGlideRatio` 6,23° diyor, kanat 4,57° uçuyordu ve en iyi süzülüşe
hiç ulaşmamıştı. Kazancı komut edilen açıların L/D'sinden hesaplamıştım;
kanat o açıların hiçbirini uçmuyordu. 568 → 580 m.

**2. "SSGI profilden silindi" (9. tur) — diskte duruyordu.**
Silme kodu çalışıyor, sonra aynı dosyanın elli satır aşağısındaki
`Ensure<GlobalIllumination>` onu geri ekliyordu. Yazdım, ölçmedim.
Şimdi silme profil kurulumunun sonunda, alt-varlık da kaldırılıyor ve
**dosyanın kendisini okuyan** bir kapı testi var.

Bir de 9. turdaki "kalabalık tek fazda yürüyor" bulgum yanlış cetvele
bakmış: faz kayması `anim.Play(0, 0, dna.faz)` ile çalışıyor.

### Bu turda benim yazdığım koddaki üç kusur

- `UcusSesi` inişten sonra hiç susmuyordu (kapalı `GlideController`
  `AirspeedMps`'i dondurur) — oyuncu çarşıda bitmeyen rüzgârla.
- 9. turda eklediğim süzülme konisi oyun sahnesinde **hiç
  çizilmiyordu**: hedefi yalnız `FlightSlice`'ta var olan bir nesneyle
  arıyordu.
- Alan raporu süslü parantez eksikliğinden doğru sayıları toplayıp
  **yanlış sonucu** yazdı.

### Oyuncunun oyunu kapatacağı üç an — üçü de kapatıldı

1. **Talim duvarı.** Eşik 3×60 m; düz zeminden bir süzülüş 22 m ve 60 m
   için 5,2 m düşüş gerek. Sayaç `0/3` diyor, sebebini söylemiyordu.
   → Artan merdiven (30/60/120) + her denemede sebep.
2. **Çıkılamayan kule.** Konveks kabuk, kapı yok, külah 45,9° / eğim
   sınırı 45°. Perde ise yalnız yatay yakınlık soruyordu: kule dibinde
   G+Space "kalkış" sayılıyor, 3.336 m'lik final **iki vapur biletiyle**
   geçilebiliyordu. → `KuleKapisi` + kalkış 40 m irtifa + iniş 800 m
   uçulan yol istiyor.
3. **Sessiz son.** `TepkiKodeksi`'nin 400 karakteri, sıfır okuyucu.
   → Tam ekran kapanış paneli.

Ayrıca: denizin çarpıştırıcısı yok, oyuncu −12 m'de yürüyordu; kamera
55° yatışta ufku düz tutuyordu; bir test ölü SSGI override'ını
koruyordu.

### ADR 0084: süzülüş yarısı kapandı, kalanı bir tasarım kararı

| ölçü | önce | sonra |
|---|---:|---:|
| Uçuş başına ortalama yatay | 210 m | **1.437 m** |
| Kaldıraç (ölçüldü, ilk kez) | — | 1,99 m/s @ 480 m batı |
| Dönme eşiği (türetildi) | 2,12 elle | **1,23** |
| Dönüşün net kazancı | — | **+0,76 m/s** |

Ölçüm aracının kendisinde dört kusur bulundu: pilot havaya göre
tırmanmayı arıyordu (süzülen kanat havaya göre hep batar), dönüş eşiği
elle yazılmıştı, arama yarıçapı 120 m iken kaldıraç 480 m ötedeydi, ve
deneme yalnız Editor'dan koşuyordu — kapıyı tutan sayı, kapıyı
değiştiren commit'ten üç commit eskiydi.

Ve dürüst sonuç: **doğru uçan pilot, yanlış uçandan daha kısa gidiyor**
(1.437 → 764 m), çünkü ulaşılabilir kaldıraç bedelini geri ödemiyor.
Kapı pilotu iyileştirerek açılmaz; ADR 0084'e (a)/(b)/(c) seçenekleri
ölçülmüş sayılarla yazıldı, önerim (b) — hedefi Sarayburnu'na almak.

### Ölçülen durum

| ölçü | 9. tur | 10. tur |
|---|---|---|
| EditMode testi | 434 | **435 yeşil**, 0 derleme hatası |
| Kare (toplu kip) | 11,0 ms | 11,0 ms (bütçe 16,7) |
| Uçuş başına yatay | 210 m | **1.437 m** (saf süzülüş) |
| Uçuş kapısı | 0/21 | 0/21 — sebep artık tasarım |

## Son kullanıcı — 1. tur (2026-09-01)

Bir oyuncu, kod okumadan, 1 saat 51 dakika oynadı. Steam incelemesi:
**👎 Tavsiye edilmez.** İade eder ama takip listesine ekler.

> *"Muhteşem bir dekor, içinde henüz oyun yok."*

### En ağır iki şikâyet — ikisi de tartışılmaz

**1. Denizde kapana kısıldım (5/5).** Kuleden hedefe doğru uçarsan kıyı
**652 m**'de bitiyor; oyun ise uçuşun sayılması için **800 m** istiyor.
Yani kuralına uyan her uçuş denize düşüyor. Suya değince bir kare
yüzeyde, sonra yerçekimi oyuncuyu **deniz tabanına** (−12 m) indiriyor;
kıyı basamağı 58°, tırmanma sınırı 45° — **çıkış yok**. Tek çare oyunu
yeniden başlatmak.

**Bu eşiği ben koydum, bu turda.** Perdenin iki vapur biletiyle
atlanmasını engellemek için. 500 m'ye indi ve suya düşeni kayıkçılar
kıyıya çıkarıyor (Haliç'te 373 kayık zaten sahnede).

**2. Kanat yok (5/5).** `PF_Kanat_Katli/Acik/Kirik` modellendi, dışa
aktarıldı, kataloglandı, depoya girdi — ve GUID taraması **sıfır
referans** döndü. Oyuncunun cümlesi: *"Oyunun adı Hezarfen. Oyunun
tamamı bir kanat için. Kuleden atladığımda sırtımda, elimde, hiçbir
yerimde kanat yok."*

Bu oturumda "üretildi ama bağlanmadı" deseninin **en pahalı** örneği.
Kanat artık duruma göre sırtta katlı / açık / kırık.

### Kapatılan diğerleri

- **Uzun süzülünce talim sayılmıyordu**: çember **inişte** ölçülüyordu,
  üçüncü eşik 120 m ve çember 250 m — iyi süzülen cezalandırılıyordu.
  Artık kalkışta ölçülüyor.
- **Okmeydanı bomboş**: üreteç arazi sahnesini açıp taşları oraya
  diziyor, sonra `Sandbox/`e **farklı kaydediyordu**. Farklı kaydetmek
  bir yayın kararı değil, bir kaza. Gemide artık tekke, namazgâh,
  5 menzil, 9 taş var.
- **Ayak sesi yok**: dört varyant üretildi (`tools/audio/gen_ortam.py`),
  adım zamanlayıcıya değil **kat edilen yola** bağlı.
- **Ana menüde "Devam et" yok**: kayıt on iki alan tutuyordu ve menüde
  bir yüzü yoktu.

Kanadı takınca görsel gövdeye üç çarpıştırıcı sızdı ve
`OyunSahnesiTests` aynı turda yakaladı.

### Oyuncunun beğendikleri (kayda değer)

Galata sokakları, gece (ay + fener), kule şerefesinden manzara, ezanî
saat, uçuş göstergesinin dürüstlüğü ("2903 m EKSİK" — *"en azından
neden başaramadığımı biliyorum"*), ve **kapanış metni**: *"Oyunun en
iyi otuz saniyesi."*

### Devredilenler

- Şehir sessiz: müzik yok, kalabalık uğultusu yok, ezan bir altyazı.
- Herkes aynı adam; kadın yok; kalabalığın içinden geçiliyor.
- Sonuç yok: 30 görev = sadece kese. Bedava su → dükkâna sat döngüsü.
- Harita ve pusula yok.
- Kayık görünmüyor (3 akçe, ekran değişmeden karşı kıyı).
- Gökyüzü boş.

## Son kullanıcı — 2. tur (2026-09-01)

Aynı oyuncu yamayı denedi. Sekiz maddenin **beşi tam, ikisi yarım,
biri hâlâ bozuk**. Steam: hâlâ 👎, ama sebebi değişti.

> *"Geçen sefer eksik olan şey işti, bu sefer eksik olan şey bir
> kontrol."*

### ❌ Kule kapısı — iki turdur kapatıldığı söylenen kusur

Kapıyı ben koydum ve **taşın içine gömdüm.** Kule çarpıştırıcısı
8,225 m yarıçapında dolu bir silindir; kapıyı eksenden 6,5 m'ye
koydum, yani duvarın **1,7 m içine**. `EtkilesimAlgila` birinci turda
tam bu iş için görüş hattı ışını eklemişti ve kapıyı doğru biçimde
reddediyordu: "Kuleye çık" yazısı **hiç belirmedi**. Oyuncu otuz
dakika kuleyi dolandı.

Ve aynı sahnede kapının **dört üst üste binmiş kopyası** vardı: üreteç
her koşuşta bir yenisini ekliyor, eskisini silmiyordu. Yani üreteç
dört kez koşmuş ve kimse bakmamış.

**Düzeltme:** kapının yeri artık çarpıştırıcının kendi sınırlarından
hesaplanıyor (sihirli sabit yok), eskiler siliniyor, ve
`KuleKapisiTests` üç şeyi ölçüyor — kapı tam bir tane mi, taşın
dışında mı (`ClosestPoint`), şerefe kalkış eşiğinin üstünde mi.

Oyuncunun son cümlesi: *"Bu yamayı çıkarmadan önce birinin oturup
kuleye çıkmayı denemesi yeterdi."* Artık deneyen bir test var.

### Doğrulanan düzeltmeler (✅)

500 m eşiği (kıyı 650 m'de — su değmeden sayılıyor), Okmeydanı'nın
taşları, talim merdiveni ve sebebi söylemesi, ayak sesi.

### Yarım kalanlar — bu turda kapatıldı

- **Deniz kurtarması yalnız uçarken çalışıyordu**; kıyıdan kayan
  oyuncu yine −12 m'de yürüyordu. `WalkController`'a da kondu.
- **Katlı kanat göğüsten geçen 2,84 m'lik yatay levhaydı** (üç hâle de
  aynı sıfır dönüş verilmişti). Sırtta dik ve %55 ölçekli.
- **Otomatik kayıt yoktu**, dolayısıyla "Devam et" düğmesi ona en çok
  ihtiyacı olan oyuncuya görünmüyordu. İş bitince ve perde ilerleyince
  kaydediyor.

### Devredilenler

- **Okmeydanı'nda sıfır insan**: 1.000 m yarıçapında 0 graf düğümü.
  Oyunun ilk perdesi şehrin insan ayağı basmayan tek köşesinde.
- **Final uçarak bitirilemiyor** (ADR 0084 Caner'in kararını bekliyor).
- Tekke ve namazgâhın 1 km ötede ikinci kopyası.
- Birinci şahısta kanat kayboluyor.
- Şehir dilsiz (8 ses dosyası), kadın yok.

---

## Tur 15 — NPC çeşitliliği ve "baştan sona benzer hatalar" taraması
*(Caner, 2026-09-02: "npcleri de üret, kadın çocuk yaşlı genç yetişkin
kız erkek farklı türde npcler olsun… tüm oyunu baştan sona benzer
hatalar üzerine incele… iteratif, durma.")*

### Üretilen
Yedi arketip (`sakin_kit.ARKETIPLER`), MPFB2'nin kendi makro
kaydırıcılarından: yetişkin erkek 1,70 · genç 1,68 · yaşlı 1,63 ·
kadın 1,58 · yaşlı kadın 1,54 · oğlan 1,24 · kız 1,21 m. Giysi tipe
göre: ferace + yaşmak, takke, ak sakal. Kimlik (cinsiyet / yaş bandı /
çıplak boy) `catalog.json` → `SakinGovde` bileşeni yoluyla akıyor;
Unity'de ikinci bir tablo yok.

### Aynı sınıftan bulunan kusurlar — "sabit, ölçünün yerine geçmiş"
| nerede | ne yazıyordu | ne olması gerekiyordu |
|---|---|---|
| `karakter_kit.olcu_al` | baş boyu = boyun %13'ü | boynun en dar kotundan tepeye — oran her gövdede 1/7,69 çıkıyordu |
| `rig_kit` denetimi | diz kotu / 1,70 m | ölçülen gövdenin boyu — 1,24 m'lik oğlan reddediliyordu |
| entari kolu | yarıçap = boy × 0,052 | kolun **ölçülen** yarıçapı + kumaş payı |
| bilek | parmak ucu + 10,5 cm | kolun en ince yeri (13 cm yerine 4,5 cm) |
| başlık | y = 0'a kuruluyordu | başın kendi merkezi (aynı kusur etek ve kuşakta bir kez ödenmişti) |
| sakin hızı | kurulumda ayrı formül | `InsanDNA` — aynı kişi bakılırken başka hızda yürüyordu |
| replik kotu | yerden 1,95 m | gövdenin kendi boyu (çocuğun 71 cm üstünde uçuyordu) |
| kaptan kapsülü | 1,7 m | gövdenin kendi boyu |

### Aynı sınıftan bulunan kusurlar — "yazıldı, bağlanmadı"
* Sakin gövdelerinde **Animator kontrolcüsü yoktu**; `SetFloat("hiz")`
  kontrolcüsüz bir Animator'da sessizce hiçbir şey yapar. Dokuz bin
  kişi bind pozunda kayıyordu.
* Adım sıklığı oyuncunun 2,2 m/s'sine ayarlıydı, sakin 1,4 m/s
  yürüyor. Ölçüldü: kayma artık yer hızının %3,6'sı.
* Kadının feracesi ve çocuğun takkesi ton listesinde yoktu: bütün
  kadınlar aynı mor.
* **204 prefabta 346 boş malzeme yuvası** — HDRP bunları macenta çizer.
  `M_Beard.mat` hiç üretilmemişti.
* Katlı kanat hiç katlanmıyordu (3,08 × 2,70 m); artık 1,64 × 0,60 m.
* Kamera kolu 1,40 m'nin altına inemiyor, yani duvarın içinde kalıyordu;
  sıkışınca birinci şahsa düşüyor.
* Doğum noktası mektebin kurşun kubbesindeydi.

### Ölçüm
* EditMode **450/450**, PlayMode **50/50** — ayak IK testi ilk kez
  geçiyor ve bu kez gerçekten IK'yı ölçüyor (rampası başka bir testin
  düz zeminini ölçüyormuş).
* Kare **7,1 ms** / 16,7 — yedi arketip ve hacimsel bulutlarla.
* Kalabalık dağılımı (1.200 tohum): tek gövdeye yığılma < %55, kadın
  %30–62 arası, çocuk %8–32 arası, her arketip en az bir kez seçiliyor.

### Açık kalan
* Doğum noktası artık zeminde ama **boş bir alanda**; şehir dokusunun
  içinde bir meydan aranmalı.
* Toplu kipte otomatik histogram pozu oturmuyor; tur kendi pozunu
  sabitliyor ve bunu raporuna yazıyor. Hacimsel bulutların **GPU**
  bedeli toplu kipte okunamıyor.
* ADR 0084 (uçuş kapısı) hâlâ Caner'in kararını bekliyor.

---

## Tur 16 — Yüzey: kumaş, ten, göz ve kişi başına ayrışma
*(Caner, 2026-09-02: "yüzleri ve karakterleri daha gerçekçi nasıl
oluşturabiliriz, kıyafetleri ile birlikte. lisans problemi olmadan." →
"üç aşamayı da yap. fakat npcler birbirinin aynısı olmasın.")*

### Kök sebep tek satırdı
`gen_hezarfen` giysi parçalarını bmesh'ten kuruyor ve **hiç UV
üretmiyordu**. Ölçüm: on iki kumaş malzemesinin ve tenin hepsi
`kind=untextured`. HDRP'de dokusuz albedo her zaman plastik okur —
"gerçekçi değil" görüntüsünün sebebi modelin biçimi değil yüzeyiydi.

### Üretilenler (hepsi kendi eserimiz, lisans sorusu doğuşta yok)
| doku | dokuma | nerede |
|---|---|---|
| `kumas_keten` | bez ayağı 1/1, 8 iplik/cm | gömlek, sarık, yaşmak |
| `kumas_cuha` | dimi 2/1, dinklenmiş | entari, ferace, şalvar |
| `kumas_ipek` | atlas 4/1 | kuşak |
| `kumas_kece` | dokuma yok | kavuk, takke |
| `deri_insan` | — | ten (MPFB2'nin CC0 bölge maskelerinden bestelendi) |

Albedolar **nötr** — renk paletten gelir ve kişiden kişiye `_BaseColor`
ile çarpılır. Hazır bir fotoğraf dokusu kendi rengini getirir ve o
çarpımı boğardı: yedi gövdelik çeşitlilik bir doku yüzünden geri
alınırdı. `tinted` bayrağı bu ayrımı hem Blender hem Unity tarafında
tek yerde tutuyor.

### Ölçülüp reddedilen
* **Göz küresi.** MakeHuman `helper-l-eye` grubu göz gibi duruyor;
  ölçüldü, merkezine en yakın gövde köşesi **100,7 mm** — kafes yüzün
  on santim önünde. O bir *kafes*, ve oturacağı göz varlığı kurulu
  değil. Göz artık deriye çiziliyor, yerini bilen tek uzayda: UV.
* **Daire iris.** Kapak adasının ortasına daire çizmek hiçbir şey
  göstermedi; maskeleri çiğ renkle boyayan bir tanı turu sebebini
  gösterdi — adanın yalnızca ince bir şeridi görünür geometri. Göz
  artık adanın kendi biçimini kullanıyor.

### Her şeyi gizleyen kusur
Birleşmiş ağda **iki UV katmanı** vardı: `Float2` (bmesh'in adı) ve
`UVMap` (MPFB2'nin adı). `join_parts` katmanları **ada göre** eşleştiriyor;
etkin katmanda gövdenin verisi yoktu ve **bütün deri tek bir köşe
texel'ini** örneklüyordu (UV kutusu 0,000–0,000). Doku bozuk
görünmüyordu — **düz renk** görünüyordu, yani dokusuz hâlinin aynısı.
Bir kusurun en pahalısı, düzeltilmiş hâline benzeyenidir.

### Kişi başına ayrışma — ölçüldü
* Ten çarpanı 0,62–1,20 + sıcaklık kayması (önce hiç değişmiyordu).
* Her giysi **kendi** tonuna kayıyor (önce hepsi tek `dna.ton`).
* Etek dokuz dikey kıvrımla düşüyor (önce düz koni).
* Yeni test: 60 kişilik kalabalıkta ayrı görünüş sayısı — kırk karenin
  en kötüsünde **54/60**. Ten tek başına 8 kovaya, 0,35 aralığa yayılıyor.

### Ölçüm
EditMode **452/452**, PlayMode **50/50**, kare **7,1 ms** / 16,7.

### Açık kalan
* Gövde kabuğu hâlâ anatominin bir kısmını taşıyor (yumuşatma 3→9,
  4→11, 5→13 yapıldı; göğüs formu kaldı — feracede doğru, gömlekte
  tartışılır).
* Sakak kartları çeneden omza inen ince teller bırakıyor (UV düzelince
  görünür oldu).
* Sûriçi sokağında kapı açıklığı gölge dörtgenleri **yere yatık**
  duruyor.

---

## Tur 17 — Yüzdeki teller, dumanın rengi ve gölgenin gerçeği
*(Caner: "devam et")*

### Düzeltilenler
| kusur | ölçü | sebep |
|---|---|---|
| Çeneden omza inen ince teller | saç malzemesi y −0,105, yüzün önü −0,031 | saç kartları `kesit` ile yerleştiriliyordu; `kesit` yarı-derinliği `max(\|y\|)` verir ve gövde y=0'da ortalı değil. **Aynı kusur bu depoda dördüncü kez** (etek, kuşak, sarık, şimdi saç) |
| Bıyık yüzün önünde | `-boy * 0,052` sabiti | ağız kotundaki kesitin kendi önünden hesaplanıyor artık |
| Sakal enseye dolanıyor | y +0,200'e kadar | çene yayına uzaklık tek başına yetmiyor (yay aynalı); sakal tanımı gereği ÖNDE |
| Şakak tutamı tel gibi | — | kök %94 → %80, boy kısaldı, en iki katı, eğim üçte bir |
| Baca dumanı **macenta** | — | `ParticleSystem` kuruluyor, malzemesi hiç verilmiyor. Prefablardaki 346 boş yuvanın çalışma zamanı kardeşi — kapı oraya konmuştu, bu çizici kodda doğuyor |
| Sahnede aydınlatma ayarı yok | `m_LightingSettings: {fileID: 0}` | varsayılan → **Baked GI kapalı** → `AdaptiveProbeVolumes.BakeAsync` hiçbir şey yapmıyor |

### Kişi başına silüet
Kafa oranı artık kişiden kişiye ±%5 (genişlik ve derinlik ayrı).
Humanoid yeniden hedefleme kemiğe **ölçek yazmaz**, bu yüzden bir kez
verilen değer animasyon boyunca duruyor; havuz bırakırken sıfırlıyor.
**Kalabalıkta ayrı görünüş 54/60 → 58/60.**

### Ölçülüp "kusur değil" denilenler
* **Simsiyah gölge.** Ölçüm: sokak gölgesi (36, 15, 0), pozu iki durak
  açınca güneşli çatı 196→247 çıktı ama gölge 41'de kaldı — yani az
  pozlanmış değil. Ama farklı gölgeler ölçülünce anlaşıldı: açık ağaç
  gölgesi (51, 37, 20), güneşli zemin (230, 210, 180) — **normal bir
  gölge**. Siyah sandığım yer üstü kapalı dar bir sokak; orada gök
  görünmüyor ve ışık yalnız sıcak sekmeden geliyor.

  > **DÜZELTME (Tur 18).** Bu yargı yanlıştı ve yanlışlığın sebebi
  > ölçünün kendisiydi: *parlaklık* ölçtüm, oysa "gölge" ile "hiç ışık
  > almayan yüzey"i ayıran şey **rengin mavisi**. Aynı karede
  > mavi/kırmızı oranı gölgede 0,000; açık gök altındaki bir gölge
  > güneşten daha mavidir, daha az değil. Şehrin **105.192 çizicisinin
  > tamamı** GI'ya katılmıyordu — ayrıntı Tur 18'de.
* **Kubbedeki benekler.** Bulut gölgesi sanıldı; kapatılıp ölçüldü,
  değişmedi. Kurşunun kendi oksit örtüsü — kasıtlı ve kayıtlı.

### Yan kazanç
APV fırını toplu kipten koşabiliyor artık: `AdaptiveProbeVolumes.BakeAsync`
penceresiz kipte hiç başlamıyor (120 sn boyunca `Lightmapping.isRunning`
false), klasik `Lightmapping.BakeAsync` başlıyor. **2.829.507 prob,
1,7 dakika.** Bekleyicinin ilk hâli işi görmeden "pişti" diyordu —
bir bekleme, beklediği şeyin başladığını görmeden bitirmez.

### Ölçüm
EditMode **452/452**, PlayMode **50/50**, kare **7,1 ms** / 16,7.

## Tur 18 — Işığın olmadığı şehir, yığılan kalabalık, çıplak sırt

Bu turun tek dersi var ve üç kez tekrarlandı: **bir şeyin ölçülmesi,
doğru şeyin ölçüldüğü anlamına gelmiyor.**

### Kök sebep: şehrin hiç dolaylı ışığı yokmuş

Üstten çekilen denetim karesinde Sûriçi sokağı simsiyahtı. Karanlık
bölge üç ayrı noktada **tıpatıp aynı** rengi okuyordu — (37,0/15,7/0,2),
(36,5/14,9/0,2), (36,5/14,8/0,2). Gerçek gölge altındaki yüzeye göre
değişir; değişmeyen renk gölge değil, **hiç ışık almayan yüzey**.

Kesin kanıt aynı duraktan geldi: **aynı yer, aynı saniye, iki kamera,
iki sonuç.** Göz hizasında gölgeli kaldırım mavi/kırmızı 0,63, gölgeli
sıva 0,86 — tertemiz. Yukarıdan dar sokağa bakınca 0,005. Farkı açı
yaratıyorsa ışık ekran uzayından geliyor demektir; yani şehrin dolaylı
ışığının tamamını **SSGI** taşıyordu.

Sebep sahne dosyalarında sayıldı:

```
D_Surici_Dogu:  498 nesne, m_StaticEditorFlags: 0
D_Galata:       401 nesne, m_StaticEditorFlags: 0
```

Şehrin **105.192 çizicisinin tamamı** "Contribute GI" işaretsizdi. Prob
fırını probu ışığa katılan geometrinin çevresine koyar; katılan hiçbir
şey yoksa boşluğu pişirir — ve her seferinde "başarılı" der.

Peşine düşerken **iki gerçek kusur daha** çıktı ve ikisi de kareyi bayt
bayt değiştirmedi; bunu her seferinde yeni ölçüm söyledi:

1. **Fırının gökyüzü bağlı değildi.** `StaticLightingSky` sahnede
   duruyor, `m_Profile` alanı `{fileID: 0}`. 2.829.507 prob gökyüzüsüz
   pişmiş.
2. **Prob verisi akmıyordu.** Günlükte tek satır: *"Max Memory Budget
   for Adaptive Probe Volumes has been reached, but there is still more
   data to load."* Etkin kalite seviyesinin (Balanced) APV akışı
   kapalıydı; üç varlıkta da açıldı.
3. **Pişirme kümesi tek sahne içeriyordu** (`singleSceneMode: 1`) ve o,
   binaların olduğu sahne değildi: şehir sekiz semt sahnesinde
   (35+41+34+27 MB…), taban sahne 1,1 MB. Kendi yorumum *"bir semt 600
   m'yi aşarsa prob hacmi semt başına BÖLÜNMELİ"* diyordu; bölme hiç
   yapılmamıştı. Artık her semtin kendi `Mode.Global` hacmi var — hacim
   kendi sahnesinin sınırından türüyor, yani güncellenmeyi unutacak bir
   sayı kalmıyor.

Dört ölçü teste bağlandı: `FirinGokyuzuTests`, `ProbAkisiTests`,
`SemtProblariTests`, `GIKatilimiTests`.

### `receiveGI` diske hiç geçmiyormuş

Sahne dosyasında `m_ReceiveGI` **sıfır** girdi, `m_StaticEditorFlags`
280 girdi. Sebep: bu nesneler **prefab örneği** ve bir örnekte değişen
alan ancak kaydedilirse diske geçer — `SetStaticEditorFlags` bunu kendi
yapıyor, düz atama yapmıyor. Bedeli: araç her koşumda aynı 104.748
çiziciyi yeniden işaretleyip **150 MB'lık sekiz sahneyi** yeniden
kaydediyordu; CLAUDE.md'nin yeniden üretim gürültüsü kuralının tam
olarak yasakladığı şey. `RecordPrefabInstancePropertyModifications` ile
düzeldi ve ikinci koşum **0 çizici** dedi.

Fırının 60 dakikalık bekleme sınırı da bu turda kalktı (3 saate): o sayı
şehrin GI'ya *katılmadığı* zamandan kalmaydı ve fırın hiçbir şey
bulamadığı için 1,7 dakikada bitiyordu. Ölçülen işten kısa bir sınır
koruma değil, bir saatlik işi çöpe atan bir şeydir.

### Kalabalık 142 noktaya yığılıyormuş

Tur tablosu bunu on durakta gösteriyordu ve sayıyı okuyup sebebini
aramamıştım: dört durakta 40 m'de **0** kişi, bir durakta **272**.
Sahne dosyalarında sayıldı — şehirde **10.900 ev** (`PF_House_Aile_*`)
ama grafta "ev" demek **avlu kapısı** demek ve kapı sayısı **142**.
Kapı başına 282 sakin düşüyor ve evdekilerin hepsi tek noktada duruyor.

Her evi düğüm yapmayı ölçüp eledim: kenar kurucu iki kez O(n²) ve
1.544 → 12.400 düğüm, 2,4 milyon çiftten 154 milyona çıkardı. Ev bir
**varış noktası**, kavşak değil: yol arama avlu kapısına kadar koşuyor,
kapıdan eve son adımı sakin kendi atıyor (`NPCAjan.sonNokta`).

Yolu açan iki hızlandırma da bu turda:

| ne | önce | sonra |
|---|---|---|
| `EnYakin` | bütün düğümleri tarar (kare başına ~1,2 milyon mesafe) | 48 m'lik ızgara indeksi |
| `Komsuluk()` | her yol aramasında düğüm sayısı kadar liste ayırır | önbellek, sayıyla geçersizleşir |

### Çıplak sırt — kaynak giyinik, oyun çıplak

Oyunun **ilk karesinde** Hezarfen'in sırtı çıplaktı: kollar giyinik,
kuşak yerinde, ama omuzlarla kuşak arasında ten görünüyor — omuz kemiği
ve omurga çizgisiyle. Blender'ın bind-poz karesi ise tertemiz giyinik.
Aradaki tek fark **hareket**.

Ölçüldü: birleşik ağda giysi köşelerinin **%67'si**, hemen altındaki ten
köşesinden 0,30'dan fazla farklı ağırlık taşıyor (ortalama fark
**1,20**, en büyüğü 2,20 — tamamen başka kemikler). `ARMATURE_AUTO` ısı
yayılımını her köşe için ayrı çözüyor ve iç içe iki kabukta komşu iki
nokta farklı sonuç alabiliyor; iki kabuk ayrı hareket edince gömleğin
**8 mm**'lik payı gövdeyi içeride tutmuyor.

Çözüm zaten kodun kendi yorumunda adıyla yazılıydı: *"Daha zarif bir
çözüm (tenin ağırlıklarını en yakın komşudan aktarmak) daha doğru olurdu
ama **ölçülebilir farkı belirsiz**."* Belirsizliği ölçüm kapattı.

| karakter | önce | sonra |
|---|---:|---:|
| Hezarfen_Sivil | 1,184 | 0,014 |
| Hezarfen_Ucus | 1,188 | 0,018 |
| Sakin_Erkek | 1,196 | 0,013 |
| Sakin_Erkek_Genc | 1,413 | 0,018 |
| Sakin_Erkek_Yasli | 1,491 | 0,017 |
| Sakin_Kadin | 1,371 | 0,031 |
| Sakin_Kadin_Yasli | 1,349 | 0,023 |
| Sakin_Kiz | 1,348 | 0,017 |
| Sakin_Oglan | 1,431 | 0,019 |

Onunun onunda da vardı. `KarakterTests` katalogdan okuyup 0,05 eşiğiyle
tutuyor.

### Martı siyah çıkıyormuş

09_marmara karesinde gökyüzünde ince koyu dilimler gördüm ve önce
**çizim bozukluğu sandım**; büyütünce 24 martı oldukları anlaşıldı.
Işıksız bir malzemede renk yansıma oranı değil doğrudan **parlaklık**
değeridir; martıya yazılan 0,93, gündüz gökyüzünün binde biri. Bu depoda
üçüncü kez aynı sınıf: duman malzemesiz olduğu için macenta, prefab
yuvaları boş olduğu için macenta, martı ışıksız olduğu için siyah —
hiçbiri hata vermiyor, hepsi çiziyor. Kuş artık ışıklı ve **çift
yüzlü** (martı çoğu zaman alttan görünür).

### Turun kendi ölçüm kusurları

* **Replik sütunu on durakta da 0 yazıyordu** ve bark sistemi
  çalışıyordu: sayı, kalabalık denetim karesi kamerayı 13 m yukarı
  taşıdıktan **sonra** okunuyordu.
* **İki durak yanlış yeri fotoğraflıyordu.** 05_ayasofya ve 09_marmara
  bomboş arazi gösteriyor; sayılar ise "açık düğüm: E, kayma 0,0 m, tepe
  açık: E" diye kusursuz okunuyordu. Ölçülmeyen şey şuydu: yerleştirici
  en yakın *açık* düğümü ararken bütün şehri tarıyor ve **mesafe sınırı
  yok**. Sınır 150 m'ye bağlandı, "durak sapması" sütunu eklendi.
* **Tabloda oyuncunun konumu yoktu** — bir tur raporunun yazması gereken
  en basit şey. Eklendi.
* **Kadrajda ne olduğu sorulmuyordu.** Kameranın merkezinden bir ışın:
  ne var, ne kadar uzakta. Bir gözlem aracının en az söylemesi gereken
  şey, neye baktığıdır.
* **Denetim karesinde beyaz benekler.** Çatıların ve pencerelerin 
  üzerinde yüzlerce beyaz nokta vardı — kar gibi. Aynı yerin göz 
  hizası karesi (210 kare oturma) tertemiz; denetim karesi dört 
  kare sonra çekiliyordu. Hacimsel bulut 0,90 zamansal birikim 
  kullanıyor, SSGI ve hacimsel sis de kare kare temizleniyor: 
  dört karede hiçbiri oturmuyor. 90 kareye çıkarıldı. Aynı sınıf 
  kusur bu araçta zaten yazılıydı (otomatik poz penceresiz kipte 
  yakınsamıyor) — bir gözlem aracı, gözlediği şeyin oturmasını 
  **beklemek** zorunda.

### Karakterlerde üçüncü kademe — merdiven vardı, basamak yoktu

`ImportLanding.KarakterUc` üç kademelik eşik merdivenini
(0,22 / 0,04 / 0,010) taşıyor ama karakterler **iki** kademeyle
geliyordu: üçüncü basamak ölü koddu. Artık 58.400 / 17.500 / **4.670**
üçgen. Kalabalık bütçesi 60 gövde için 969 bin üçgenden **258 bin**e
iniyor.

Kural da vardı ve kapsamı eksikti: `AssetPipelineTests` *"20.000
üçgenden ağır varlık üç kademeli olmalı"* diyor ama üçgeni yalnız
`MeshFilter`dan sayıyordu. Karakterler `SkinnedMeshRenderer` taşıyor —
yani 58.000 üçgenlik bir gövde o kurala göre **sıfır üçgendi** ve
sessizce geçiyordu. Bir ölçü, ölçmesi gereken şeyi hiç görmüyordu.

Ekleme eski bir kusuru da açığa çıkardı: kemik sayısı 38'den 46'ya
çıktı. `etek_kemikleri` her LOD için çağrılıyor ve `edit_bones.new`
aynı adı ikinci kez alınca Blender sessizce `Etek_0_0.001` yapıyor —
yani iskelet LOD başına **sekiz fazla kemik** topluyordu ve her LOD
kendi kopyasına bağlanıyordu. İki kademede de vardı, görünmüyordu.
Şimdi 30 kemik (22 humanoid + 8 etek) ve üç LOD aynı zincirde.

### Sakal bir maskeydi

`M_Beard`ın taban rengi haritası **yoktu** (`_BaseColorMap: {fileID: 0}`)
ve yakın planda sakal, çeneye geçirilmiş kahverengi bir maske gibi
duruyordu: tek parça, tek renk, hiç kırılma yok. Kumaş için zaten
yazılı olan ders: *dokusuz albedo HDRP'de plastik okur.*

Kart atlası (`gen_hair_texture.py`) bu işi göremez — o bir **alfa**
atlası ve döşenmez; sakal ise kart değil **kabuk**. Yeni
`gen_sakal_texture.py` döşenebilir bir yüzey üretiyor.

Yolda üç şey daha çıktı:

* **Bir sayının iki sahibi.** Ak sakal paletten (`beard_ak`), kestane
  sakal `sac_kit.sakal_material()`ten geliyordu. İkisi aynı rengi ve
  aynı pürüzlülüğü yazıyordu — fark yoktu, ta ki palete doku eklenene
  kadar. O an ayrıştılar: yaşlının sakalı dokulu, yetişkininki düz.
  Tek sahip artık palet.
* **Albedo çok koyuydu.** İlk doku 0,30 tabanla yazılmıştı; palet
  rengiyle (0,105/0,072/0,052) çarpılınca sakal simsiyah çıktı ve
  teller siyah yarık gibi okundu. Doku bir renk değil bir **yüzey**
  taşır; koyuluk AO ve normalden gelir. Kumaş dokularının sözleşmesi
  0,68–0,78 taban; sakal da o aileye alındı.
* **UV gövdeden geliyordu.** Kabuk gövdeden kopyalandığı için
  MakeHuman'ın bütün vücut yerleşimini de kopyalıyor; 6 cm'lik
  döşenebilir bir doku yüze yayıldı. Kendi dokusunu isteyen parça kendi
  yansıtmasını da ister.

`selftest.py` bu turda eski bir tutarsızlığı da yakaladı: karakter
rolleri yalnız `default` paletinde yazılıydı, `nonmuslim`de yoktu —
yani `M_Skin` iki farklı tanım gösteriyordu ve aynı ada iki tanım
Blender'da sessizce `.001` üretir. Kural zaten doğruydu: bir Rum'un
sakalı da sakaldır; cemaate göre değişen şey **evdir**. Roller ortak
`KARAKTER_ROLLERI` tablosuna taşındı.

### Mest dokusuzdu, kayış kereste giyiyordu

İki ölçüm: `M_Leather_Mest`in taban rengi haritası yok
(`_BaseColorMap: {fileID: 0}`) — ve o yüzey şehirde en çok tekrar
edenlerden biri, altmış gövdede yüz yirmi tane. `M_Leather` ise
dokuluydu ama dokusu **kereste**ydi (`weathered_planks`, boyanmış);
satırın kendi gerekçesi *"elde CC0 deri/tüy dokusu yok"* diyordu.
Doğruydu — ta ki üretilene kadar.

`gen_kosele_texture.py` gözenek tanesini (0,9 mm, dana köselesi) ve
kırık ağını üretiyor. İlk denemede kırışıkları yedi uzun sinüs eğrisi
olarak çizdim; dokuya **bakınca** görüldü — kareyi baştan başa geçen
solucanlar. Deri öyle kırılmaz: kırık kısa, açı yapan bir **çokgen
ağıdır**. `proclib.worley`in kendi açıklaması bunu zaten yazıyormuş
(*"kırık çizgisi F2 − F1 ≈ 0 olan yerdir"*); kütüphaneyi tarif edildiği
gibi kullanmak, ona benzeyen bir şey uydurmaktan iyi çıktı.

Böylece karakterin **her** malzemesi dokulu:

```
M_Beard  M_Cloth_Entari  M_Cloth_Gomlek  M_Cloth_Kavuk  M_Cloth_Kusak
M_Cloth_Salvar  M_Cloth_Sarik  M_Hair  M_Leather_Mest  M_Skin
```

Bu, "12 kumaş malzemesinin hepsi `kind=untextured`" ölçümüyle açılan
dizinin sonu.

### Kart bitti: saç ve bıyık da kabuk oldu

Oğlanın yakın planı çekildi ve iki şey göründü: kulakların iki yanında
ince **teller**, boynun çevresinde soluk bir **fırfır**. Yetişkinin yan
görünüşünde de yüzün önünde asılı duran kahverengi bir **çubuk** vardı.

Üçü de aynı şeyin sonucu: **bir kart, kenardan bakıldığında bir
çizgidir** ve altındaki biçimi izlemez. Bu kusur bu depoda altı kez
ödendi. Çözümü sakalda zaten bulunmuştu (*"SAKAL: KART DEĞİL KABUK"*)
ama kararın yalnızca yarısı uygulanmıştı — saç, bıyık ve sakal ucu
tutamları kart kalmıştı. Bir kararın yarısını uygulamak, kusurun
yarısını bırakmaktır.

| parça | önce | sonra |
|---|---|---|
| sakal | kabuk | kabuk (yumuşatma 2 → 6) |
| sakal ucu | 8 kart | **kaldırıldı** — kabuk zaten çene biçimini veriyor |
| bıyık | 2 kart | kabuk, üst dudak şeridi |
| saç (şakak/ense) | 6 kart | kabuk, başlığın altından çıkan kütle |

Bıyığın ilk kabuğu fazla genişti (0,897–0,921 ve %62 yarıçap) ve
renderda sakalla birleşip ağzı tamamen kapatan koyu bir **dikdörtgen**
oldu; ölçü daraltıldı (0,899–0,915, %50) ve ağız yerine geldi. Kart
malzemesi tamamen kaybolmadı — o alfa kesmeli malzeme artık hiçbir
yerde kullanılmıyor ve bu, bir sonraki turda temizlenecek bir borç
olarak yazılıyor.

Aynı kareyle bir kusur daha kapandı: **gömleğin yakası** entarinin
içinde bitmiyordu ve kabuk kesimi gövdenin üçgen kenarlarını izlediği
için ağız tırtıklı çıkıyordu — oğlanın boynunda soluk, fırfırlı bir
halka olarak okunuyordu. Gömleğin üstü 2 cm aşağı çekildi; görünen tek
kenar entarininki ve o zaten yaka gibi okunuyor.

### Örtü: kulaklar, koni ve içinden çıkan burun

Kızın ve kadının yakın planı üç kusur gösterdi ve üçü de aynı yerden —
`sakin_kit.yasmak` — geliyordu:

* **Kızın başında iki beyaz "kulak."** Yetişkin profili omuzda 1,52
  katına açılıyor ve bu *ön kapalıyken* doğru: örtü yüzün önünde
  birleşir, iki yan birbirini taşır. Yüz açılınca o taşıma kalkıyor ve
  geriye iki serbest panel kalıyor. Çocuğun başındaki şey bir
  **başörtüsüdür**: saçı ve kulağı örter, çenede biter, omza yayılmaz.
* **Tepe bir koni.** İki denetim noktası arası doğrusal örneklendiği
  için tepede 0,34, alında 1,06 olan profil arada düz bir koni
  veriyordu — bir cadı şapkası, ve tepesi ön tarafta kafa derisinin
  içinden geçiyordu. Araya kafanın kendi kubbesi kondu.
* **Kadının burnu örtünün dışında.** Halka `r × 0,90` ile kuruluyordu,
  yani örtü önden arkaya kafadan **daha sığ**. İnsan kafası önden
  arkaya yanlardan uzundur ve burun önde daha da çıkar; sonuç yakın
  planda görüldü — burun ve dudaklar bezin dışında. 1,06 yapıldı.

Üçü de "bir sayı, ölçtüğü şeyin yerine geçmiş" ailesinden: 0,90 bir
kafanın oranı değil, bir tahmindi.

Saç kabuğu da bir kez daha ölçüldü: sınırları sakallıya göre
seçilmişti (alt uç 0,858·boy, ön sınır kafa yarıçapının %30'u) ve
**sakalsız gençte** yüzün iki yanından çeneye inen uzun **zülüfler**
bıraktı. Sakalsızda saç kulak hizasında biter (0,878) ve yanağa
inmez (%2). Aynı sayı, iki farklı yüz için iki farklı şey demek.

### Yaşlının kolları dirsekten geriye kırıkmış

İnceleme paketinde yaşlı sakinin kolları **dirsekten geriye kırılmış**
duruyordu: eller kalçada, kumaş kendi üstüne katlanmış. Üç sürümdür
öyleymiş (`Sakin_Erkek_Yasli_v3` de aynı) — kimse o kareye bakmamış ve
hiçbir sayı bunu söylemiyordu.

Ölçülünce sebep tek satırda çıktı. Uzuv tarayıcısı dilimleri **kot**
ekseninde alıyor ve kol filtresi `abs(x) ≥ kol_eşiği`; gövde iki
bacaklı olduğu için **uyluk da bu şartı geçiyor**. Yaşlının kol çizgisi
bacakla başlıyordu:

```
(+0,175, +0,090, 0,751)   <- uyluk
(+0,176, +0,081, 0,711)
(+0,181, +0,082, 0,661)
(+0,190, +0,098, 0,506)
(+0,208, +0,111, 0,397)   <- bacak biter
(+0,235, +0,097, 1,243)   <- OMZA ATLAR
...
```

Üç düzeltme üst üste kondu ve her biri ölçüldü:

| ne | yaşlıda dönüş |
|---|---:|
| başlangıç (kot ekseni, izlemesiz) | 99° |
| eksen x'e alındı | 138° |
| izleme eklendi (önceki noktaya yakınlık) | 171° |
| **tohum eklendi** (uzuv omuzdan başlar) | **44°** |

İkinci ve üçüncü adım işi *kötüleştirdi* ve bu bilgi vericiydi: eksen
seçmek de izlemek de doğru şeylerdi ama ikisi de **nereden
başlanacağını** bilmiyordu. Bir uzuv tarayıcısının bilmesi gereken ilk
şey budur; iki bacaklı bir gövdede kolu bacaktan tahminle ayırmasını
beklemek, ölçüyü tahmine bırakmaktır.

Sağlıklı aralık artık 18,6–51,8°; `KarakterTests` 70° eşiğiyle tutuyor
ve sayı `catalog.json`'a yazılıyor.

### Ölçülüp geri alınan bir düzeltme

Kadının belinde iki yanda koyu birer oyuk gördüm ve *"bandın iç duvarı
görünüyor"* diye teşhis edip payı 4 mm içeri çektim. Sonra kesitleri
ölçtüm:

```
z 0,96-1,10   Ferace iç yarıçap 0,092
              Gömlek dış yarıçap 0,128-0,146
```

Band zaten gövde kabuğunun **içinde**; görünen şey bir yarık değil, iki
kumaş arasındaki dar aralıkta biriken ortam örtmesi. Hipotez çökünce
değişiklik geri alındı — yanlış bir gerekçeyle konulan doğru görünümlü
bir sayı, sonraki turda yanlış yerde aranan bir kusur olur.

Kadının silueti yine de **üç üst üste silindir** okunuyor (gövde → bel
bandı → etek) ve bu bir kusur değil bir **biçim sorusu**: ferace önden
kapalı bir dış giysidir ve belinde kuşak taşımaz, yani omuzdan eteğe
tek sürekli bir yüzey olmalı. İki yol var — (a) kabuğu kalçanın altına
kadar indirip eteği onun altından başlatmak, (b) eteği göğsün altından
başlatıp bel dikişini hiç kurmamak. Ölçüyle seçilecek bir şey değil;
Caner'e soruluyor.

### Turun altı durağı boş çıktı — çünkü şehir henüz yüklenmemişti

Turun kendi raporu on durağın **altısında** ayak altında `TR_Istanbul`
(çıplak arazi), 40 m'de 0 NPC ve 0 çizilen gövde yazıyordu. Kareler de
öyle: `03_galata_sokak` boş bir kum düzlüğü, şehir ufukta ince bir
şerit. Bu, turlar boyunca *"orada şehir yok"* diye okundu.

Ölçüm başka bir şey söylüyor. Durak (120, 60); `D_Galata`'nın kendi
sınırı x −1944…1296, z −972…1944 — **durak semtin tam içinde.** Şehir
yok değil, **henüz yüklenmemiş**: akış Addressables ile asenkron
yüklüyor ve tur onu **doksan kare** bekliyordu. Doksan kare bir sayıdır,
bir koşul değil.

Bu deponun tekrar eden dersinin bir örneği daha: *bir bekleme,
beklediği şeyin bittiğini görmeden bitiyorsa bekleme değildir.* Aynı
kusur APV fırınında da vardı.

Tur artık `DistrictStreamer.LoadsInFlight` sıfıra inene kadar bekliyor
(akışın bir kez değerlendirmesi için yarım saniye önden, otuz saniye
üst sınır) ve her satırda **kaç semt yüklü, ne kadar beklendi** yazıyor.
Bundan sonra boş çıkan bir durak gerçekten boştur.

### Fırın: dört ölçüm, dört yanlış varsayım, ve sonunda projenin kendi kuralı

Bu turda pişirme dört kez "başarılı" dönüp diske hiçbir şey yazmadı.
Her seferinde sebep başkaydı ve her seferinde **ölçüm** söyledi.

1. **Prob hacimleri dünya boyuydu.** Her semtin hacmi `Mode.Global`'dı
   ve `Global` sahnenin değil **yüklü olan her şeyin** sınırını alır;
   kurulum sekiz semti birlikte açıyor. Kümenin kendi varlığında
   yazılıydı: `m_Extent: {x: 7776, y: 364.5, z: 7897.5}` — 15,5 × 15,8
   km, ve **sekizi de aynı kutu**. Bedeli: *"the number of APV probes
   exceeds the current system limit of 67.180.350"*, yerleştirme daha
   başta düştü. → Hacimler artık her semtin **kendi** çizicilerinden.

2. **Sanal kaydırma GPU'daydı.** Fırın bir tur önce CPU'ya alınmıştı
   (7,25 GB sahne girdisi, 8 GB kart) ama `VirtualOffsetBake` hâlâ
   karta gidiyordu: `d3d12: Unrecoverable GPU device error`, 100 MB'lık
   istek 20 MB'lık tampona. → İşi CPU'ya vermek, işin **tamamını**
   vermekmiş.

3. **Kısmi pişirmede her koşum kendi ızgarasını üretiyordu.** Tek semt
   yüklüyken hücre ızgarası başka çıkıyor ve Unity sonucu *"partially
   baking the set with an incompatible cell layout"* diyerek atıyor.
   `partialBakeSceneList` "yalnız bunu YÜKLE" değil, "yalnız bunu
   PİŞİR" demek. → Bütün semtler yüklendi. Ama o zaman ışık hesabı
   bütün şehrin geometrisine karşı koşuyor: en küçük semt dokuz
   dakikada **%6,2** ve hız düşüyordu.

4. **`freezePlacement` ızgarayı değil, PİŞMİŞ YERLEŞİMİ dondurur.**
   Onu "ızgarayı sabitler, semtler tek tek pişer" diye kullandım.
   D_Okmeydani pişip beş hücre yazdı; sonra D_Eyup yirmi altı dakika
   pişti ve diske **hiç dokunmadı** — donuk yerleşim o beş hücreydi ve
   Eyüp'ün probları onların dışındaydı. Yani kısmi pişirme, önce **tam**
   bir pişirme ister; tam pişirme de bu makineye sığmıyor.

Buradan sonrası projenin kendi kuralı (ADR 0078): **referans semt
D_Galata**, yeni katman önce orada bitirilir ve **orada ölçülür.**
Galata tek başına pişiyor; ölçü `tools/olcum/golge_orani.py`.

İki denetim de bu turda doğdu ve ikisi de bir daha aynı sessiz
başarısızlığa izin vermeyecek:

* **Yerleştirme hatası eşzamanlı yakalanır** — `Lightmapping.BakeAsync`
  daha dönmeden düşer; artık dinleniyor ve koşum orada biter (çıkış 6),
  on bir dakika boşa gitmez.
* **Ürün denetimi imza karşılaştırır** — hücre sayısı, diskteki toplam
  bayt ve son yazılma anı, pişirmeden önce ve sonra. Yalnız hücre
  sayısına bakmak yetmiyordu: D_Eyup'un boş pişirmesi, D_Okmeydani'nin
  yazdığı beş hücreyle "başarılı" görünüyordu. *Başkasının işiyle
  karşılanabilen bir denetim, denetim değildir.*

### Fırın niye bu kadar yavaş: sahne girdisi 4,17 GB

`D_Galata` tek başına, taban sahneyle birlikte, altmış dakikada %21,9'a
geldi. Kayıt sebebi bir satırda söylüyor:

```
Transformed OOTS snapshot into LightBaker scene input … Size: 4171.12MB
Extracted OOTS snapshot with 11260 instances, 404 geometries, 0 lights
```

11.260 örnek 4 GB etmez; **arazi** eder — 15 km × 15 km'lik yükseklik
haritası ışın izleme için üçgenleşince girdinin neredeyse tamamı o olur.

**Araziyi fırından çıkarmak denenmedi ve sebebi yazılı:** arazi aynı
zamanda probun ALT yarım küresini kapatan şeydir. Çıkarılsaydı yerdeki
problar aşağıdan da gökyüzü görürdü ve sokak, karanlık yerine
gerçekdışı biçimde aydınlık okurdu. Yani arazi maliyeti bir israf
değil, örtmenin bedeli.

Kesilen şey bu yüzden örnekleme ve aralık oldu (32 → 16, 4 m → 6 m),
ikisi de kayıtta gerekçeli.

### Kürek: bir sayı iki yerden türemişti

Kayığın küreği yakın planda **iki parça** okuyordu — siyah bir çubuk,
bir boşluk, ve ondan ayrı duran küçük bir tahta. Sayı da aynı şeyi
söylüyor: sap 16° eğik ve yarı boyu 1,25 m, yani ucu merkezinden
`1,25 · sin16 = 0,34 m` aşağıda. Palanın düşüşü ise elle **0,48 m**
yazılıydı. Pala, parçası olduğu sapın ucundan **14 cm** aşağıdaydı.

Açıyı bilen tek bir yer vardı ve pala orası değildi. Artık palanın yeri
sapın kendi açısından hesaplanıyor. Genişlik de 8,5 cm'den 15 cm'ye
çıktı — gerçek kürek palası 15-18 cm ve 8,5 cm'lik bir tahta uzaktan
sapın kendisinden ayırt edilmiyor.

Katalog değişikliği taşıyor: `footprint_y` 5,404 → 5,301 (kosinüs
düzeltmesi palayı içeri aldı), üçgen sayısı aynı.

### Vapur iskelesi ışınlanma, ama tekne HAZIR

Taşınan maddelerden biri *"ferry is a teleport with no visible boat"*.
Tekne var: `SM_Pereme`, 8,67 m, iki kürek çifti, ahşap dokulu, 502
üçgen. Eksik olan model değil, **yerleştirme** — Unity tarafı.

### Üç boş döngü silindi

Sakal ucu, bıyık ve saç kartları tur tur kabuğa çevrilmişti ve her
seferinde döngünün **içi boşaltılıp gövdesi bırakılmıştı**
(`for sx in ():`). Altmış sekiz satır ölü kod, `sac_mat` ve
`sac_kit.kart`/`hair_material`'a giden tek çağrılar. Dosyanın kendi
cümlesi kendine uyuyor: *bir kararın yarısını uygulamak, kusurun
yarısını bırakmaktır.* Gerekçe yorumları yerinde; kod gitti.
Yeniden üretildi ve katalog on karakterde de **birebir aynı** —
hiçbir geometri kımıldamadı.

### Çınar bir yeşil kütle — çünkü 356 üçgen

Ağaç yakın planda gövdesi kararmış, dalsız, kapalı bir yeşil blob
okuyor: siluet çokgen kenarlarını gösteriyor, ışık içinden geçmiyor.
Sebep katalogda yazılı ve bir kusur değil bir **bütçe**:
`tris_lod0 = 356`. Üç yüz elli altı üçgenle dal olmaz.

Şehirde binlerce ağaç var, yani bu sayı kare süresiyle birlikte
konuşulacak bir sayıdır. Bu yüzden burada **karar değil kayıt**
duruyor: ağacı düzeltmek istiyorsan önce `Hezarfen → Olcum → Kare
suresini bolustur` koşacak ve bütçede yer olduğunu gösterecek.

### Kahvehane: kusur sanılan iki şey ölçümle düştü

* Sundurmanın düz pembe göründüğü — yakın planda tahta derzleri
  görünüyor; malzeme `timber`, rengi aşı boyası. Düşük çözünürlükte
  yanlış okumuşum.
* Çatı kiremitlerinin ince göründüğü — 8,5 m'lik çatıda kiremit sırası
  ~9-15 cm ediyor; alaturka kiremit gerçekte 15-20 cm. Sınırda ama
  kusur değil.

Kayda geçiyor ki bir sonraki tur aynı iki şeye yeniden bakmasın.

### İki landmark gözlemi (dokular açıldıktan sonra görünenler)

* **Yedikule'nin Altın Kapı bölümü** çevresindeki surlar taş dokusunu
  taşırken düz beyaz üç kütle olarak duruyor. Mermer kaplama doğru
  (Porta Aurea mermerdi) ama geometri üç kutu ve tek kemer; kapının
  kendi mimarisi yok. T2 boşluğu, ayrı bir işin konusu.
* **Süleymaniye'nin kesme taş derzleri iri okunuyor.** Doku
  `large_sandstone_blocks` ve kendi `meta.json`'ı 3 × 3 m diyor; UV
  dünya ölçeğinde, yani doku tarif edildiği gibi kullanılıyor. Blok
  boyu bizim değil kaynağın; kusur değil, kayıt.

### İnceleme aletinin kendisi iki kez yanlış bakıyordu

CLAUDE.md şunu yazıyor: *"Render bir gözlemdir, kanıt değil."* Bu turda
cümlenin daha keskin bir hâli çıktı — **gözlemin kendisi yanlıştı.**

**1. Çarpıştırıcı modelin üstüne çiziliyordu.** Galata Kulesi'nin
inceleme karesinde kurşun külahın yerinde düz tepeli beyaz bir silindir
vardı. Külah yerindeydi: blend'de ölçüldü, `Kulah` 8,5 m'lik bir koni ve
tepesi tam 46,00 m'de — katalogun yazdığı sayı. Göreni üreten şey
`UCXB_GalataKulesi`, yani **çarpıştırıcının kendisi**.

`render_preview` yalnız `UCX_` önekini eliyordu. `UCXB_` sonradan geldi
(içi boş, dışbükey **değil**; ev ve kule kademelerinde gerekti) ve alet
güncellenmedi. Kural artık Unity tarafındaki iniş sözleşmesiyle aynı:
`UCX` ile başlayıp `_` ile devam eden her önek çarpıştırıcıdır.

**2. Landmark'ların hiçbirinde doku yoktu.** Çarpıştırıcı kalkınca
altından ikincisi çıktı. Bütün kanonik ağaç tarandı
(`tools/olcum/blend_dokusu.py`):

```
önce:  35 dokusuz / 336 blend
sonra:  2 dokusuz / 336 blend
```

Otuz üçü landmark'tı — Ayasofya, Süleymaniye, Sultanahmet, Yedikule,
Topkapı, kara surları, türbeler, iki bedesten. Yani **uçuş oyununun en
çok baktığı yapılar**, `--textured` verilmeden kurulmuştu.

Oyun bundan etkilenmiyor (Unity malzemeleri paletten yeniden kuruluyor);
**inceleme** etkileniyor. Dokusuz incelenen bir yüzeyde yüzey kusuru
görünmez — kanadın kereste dokusu turlarca tam bu yüzden fark edilmedi.

Kalan iki dosya `SM_AxisCalibration` (ölçü aleti) ve `SM_BoxHouse`
(yer tutucu kutu); ikisi de çıplak olmalı.

Katalogda tek bir sayı bile kımıldamadı — geometri aynı, yalnız
malzemeler doku kazandı. Bu yüzden **sayının kendisi kayıt oldu**:
yeniden üretim gürültüsü kuralı, kaydı olmayan bir değişikliği geri
aldırır ve haklıdır; eksik olan kural değil kayıttı.

### Kanadın yüzeyi kerestedeydi — ve doku denetimi artık kapandı

`M_Feather`, 9,71 m'lik kanadın **bütün** yüzeyi, doku olarak
`weathered_planks` kullanıyordu. Köselede bir kez ölçülüp kapatılan
kusurun aynısı: *bir yüzeye ait olmadığı bir doku giydirmek, dokusuz
bırakmaktan daha az görünür ama daha yanlıştır.* Varlığın kendi kaynak
notu yüzeyi zaten yazıyordu — *"ahşap çıta iskelet + kartal tüyü yüzey
+ deri kayış"*.

`tools/textures/gen_tuy_texture.py` üretildi: bindirmeli tüy sıraları,
omurga, 35°'lik teller. Ölçüler kuştan — birincil tüy 5-8 cm en, tel
aralığı 0,5-1 mm.

Ardından **bütün palet tarandı** ve sonuç temiz:

* Kalan `weathered_planks` kullanıcıları `timber`, `timber_bare`,
  `trim` — üçü de gerçekten ahşap.
* Paletteki on altı rolden dokusuz olan yalnız üç tanesi:
  `goz_ak`, `goz_bebek`, `goz_iris`. Bunlar bilerek düz — göz ten
  dokusunun üstüne prosedürel olarak boyanıyor.

Yani "dokusuz albedo plastik okur" cephesi kapandı; bir sonraki tur
burayı yeniden taramasın.

### `06_kara_surlari`: oyuncunun başının üstünde bir taş tavan

Turun `tepe acik` sütunu on durakta bir kez **H** diyor ve o durak bu.
Kareye bakıldığında ne demek olduğu görülüyor: karenin üst yarısını
baştan başa **düz bir taş kütle** kaplıyor, oyuncunun üç metre
üstünde, taşıyıcısı görünmeden. Oyuncu bir tavanın altında duruyor.

Durak (−3300, 0, −1200). Kapı kemerinin altında durmak kendiliğinden
kusur değil ama bu kemer karenin tamamını kaplıyor ve alt kenarı
eğrisiz. Gölge ölçümü de aynı yeri işaret ediyor: `06_kara_surlari`
mavi/kırmızı **0,000**, saçılım 0,006 — gökyüzü kapalı olduğu için
zaten hiç dolaylı ışık gelmiyor.

Alet kusuru zaten görüyor (`tepe acik`); sıradaki tur koşumunda
`kadrajda` sütunu neyin üstte olduğunu **adıyla** yazacak.

### Gölgenin rengi: turun tek sayısı, artık bir aletle

`tools/olcum/golge_orani.py` karenin en karanlık %25'inin
mavi/kırmızı oranını veriyor (gökyüzü pikselleri dışarıda). Fırın öncesi
taban ölçüm tek cümleye iniyor: **binanın olduğu her karede gölge
siyah.**

| kare | gölge mavi/kırmızı | saçılım |
|---|---:|---:|
| çıplak araziye bakanlar (01, 02, 03, 05, 08, 10) | 0,26 – 0,30 | 0,10 – 0,17 |
| şehre bakanlar (04, 06, 07, 09) | 0,000 – 0,016 | 0,006 – 0,10 |

Saçılım da anlatıyor: `04_surici_kalabalik`'te 0,006 — karanlık her
pikselde **aynı** renk. Değişmeyen bir karanlık gölge değil, ışık
almayan yüzeydir.

### Yüzün ölçüleri: dört sabit, dört ölçüm

Yakın plan kareleri dört ayrı kusur gösterdi ve dördü de aynı cinsti —
**boyun bir kesri, bir ölçümün yerinde duruyordu.**

| ne | yazılıydı | ölçüldü | karede ne görünüyordu |
|---|---|---|---|
| sakalın üstü | `boy * 0.897` | dudak altı `0.885` | ağzın üstünden geçen bir **sargı** |
| sakalın altı | `boy * 0.806` | çene `0.869` | boynu saran bir **boyunluk** |
| bıyık bandı | `0.899–0.915` | dudak `0.885–0.895` | burun köküne yapışık bir **tahta** |
| saçın üstü | `0.912 / 0.925` | başlık tabanı `0.948 / 0.946` | başlıkla saç arasında **çıplak kafa derisi** |

Ölçünün kaynağı yeni değil: MPFB2'nin kendi bölge maskeleri
(`mpfb_face`, `mpfb_lips`) gövdenin UV atlasında duruyor ve ten dokusu
zaten onlardan besteleniyor. `sac_kit.bolge_kotu` maskeyi gövdenin
UV'leri üzerinden okuyup bölgenin kot aralığını döndürüyor — yani
kaynak ikinci kez okundu.

Başlık taban kotları da tek sahibe indi (`BASLIK_TABANI`); saçın üstü
artık ondan türüyor.

### Takke: kubbe üç kez ölçüldü, üçünde de yanlış yerdeydi

Takke bir **kubbe** olarak kuruluyordu ve üç ayrı ölçüm üç ayrı kusur
gösterdi: taban yarıçapı kafanın en geniş diliminden alınınca takke
kafanın çevresinde **havada** durdu; kendi kotunda ölçülünce **külah**
gibi sivrildi (yükseklik hâlâ boydan türüyordu ve kafanın tepesini
4,5 cm aşıyordu); yükseklik de kafadan ölçülünce kubbe önde derinin
**altına** girdi ve takke tepede küçük bir yamaya döndü.

Üçünün sebebi aynı: **bir kubbe kafayla yalnız bir halkada buluşur.**
Bu depo dersi sakalda, bıyıkta ve saçta zaten ödemişti. Takke artık
`kopya_kabuk` — kafaya geçirilen bir bez, kafanın biçiminde.

### Ölçülüp reddedilen iki değişiklik

* **Saç çizgisini öne taşımak.** Saç kabuğunun ön kenarı y +0,0117'de,
  yüzün önü -0,0553'te: kafanın tam ortası. Eşik 0,62'ye taşındı ve
  sonuç **daha kötü** oldu — saç yanağa taştı. Sebep sayıda değil
  biçimde: saç çizgisi bir **düzlem** değil bir eğridir. (`SemtProblari`
  aynı dersi bir kutu ile bir yamaç için yazmıştı.)
* **Yaşmağın açıklık kenarını yaya oturtmak.** Yüzler `m + 0,5`
  açısına göre siliniyor, köşeler `m` açısında duruyor; "ilk duran
  köşe" kenarın kendisi değil. Karede ölçülebilir fark çıkmadı, geri
  alındı.

### Feracenin üç silindiri — sorulan soru ölçümle kapandı

Yukarıdaki soru ("kabuğu kalçanın altına indir" mi, "eteği göğsün
altından başlat" mı) Caner'e soruldu ve kayda geçti; cevap beklenmeden
ölçülerek ilerlendi (çalışma sözleşmesi: *sorular sorulur, cevabı
beklenmez*).

Kusurun kökü bir **kot uyuşmazlığı**ydı, bir biçim tercihi değil. Kabuk
**belde** bitiyordu; eteğin üst yarıçapı ise **kalçayı** — gövdenin en
geniş yerini — içermek zorundaydı. İki sayı iki farklı kotta ölçülünce
aralarında bir basamak kaldı ve o basamağı bir **bant** örtüyordu. Bant
kuşak gibi okunuyordu; ferace kuşak taşımaz.

Dikiş kalçaya taşındı: kabuk kalçanın 4 cm altında biter, etek kalçadan
başlar. Basamak `+0,018 m`'den `+0,008 m`'ye indi ve bant tamamen
kaldırıldı.

**Sıfıra indirmek denendi ve ölçüm reddetti.** `etek_acikligi`'ye
tepeyi büyütmeyen bir kip (`ust_sabit`) eklendi ve basamak tam sıfır
oldu (0,218/0,150 → 0,218/0,150). İnceleme karesi v15 sonucu gösterdi:
**kırmızı şalvar eteğin içinden çıktı** — kalçada bir bant, eteğin
önünde lekeler hâlinde. Yani 8 mm süsleme değil; şalvar kalçada kabuğun
dış yüzeyinden geniş ve etek onu içermek zorunda. Kip geri alındı.

Kabuğun eteğin ağzından 4 cm aşağı inmesi de ölçüldü ve **kaldı**:
üstten bakıldığında görünen şey halka boşluğu değil kabuğun duvarı
oluyor. (v14'te bu 4 cm yokken üstten bakınca figür belden aşağı açık
bir kovanın içinde duruyordu.)

Kalan sayı: `ferace dikişi: kabuk 0,218/0,150 → etek 0,226/0,153
(basamak +0,008/+0,003 m)`, `catalog.json` üzerinden değil `[HZ]`
kaydından okunuyor — bir sonraki tur "göze öyle geldi" ile değil bu
sayıyla konuşsun diye.

### Fırın iki kez çöktü — sebep VRAM'di

Toplu pişirme iki kez süreç hiçbir şey yazmadan yok olarak bitti,
günlükte hata yok. Sebebi tek satır söylüyor: *"Transformed OOTS
snapshot into LightBaker scene input … **Size: 7251,37 MB**"* ve bu
makinenin kartı **8 GB**. Sahne girdisi tek başına VRAM'in tamamına
yakın. Sistem belleği 32 GB (ölçüldü), yani işi CPU fırınına vermek
yeterdi. Şehir küçültülmedi, prob aralığı seyreltilmedi: kusur kalite
ayarında değil, işin yanlış yere verilmesindeydi.

Bekleme sınırı da bu turda 60 dakikadan 3 saate çıktı — o sayı şehrin
GI'ya *katılmadığı*, yani fırının hiçbir şey bulamadığı için 1,7
dakikada bittiği zamandan kalmaydı.

CPU fırını da **üç saatte bitmedi**. İki ölçüm aynı şeyi söylüyor:
iş bu makineye göre fazla. Neyi keseceğimi seçerken prob **ızgarasını
korudum** — aralığı 3 m'den 6 m'ye çıkarmak 7,2 m'lik bir sokağa enine
tek prob bırakırdı ve sokak tam da ışığın ölçülmesi gereken yer.
Kesilen şey **örnekleme** oldu: 128 dolaylı örnek → 32, sıçrama 2 → 1.
Prob bir küresel harmoniktir, yani zaten ağır ortalaması alınmış bir
şey; 32 örnek onda gürültü bırakmaz, aynı sayı bir ışık haritasında
leke yapardı. Açık hava şehrinde ikinci sıçrama da ilkinin yanında
ölçüm gürültüsü kadar kalır — kapalı iç mekân gelince bu sayı yeniden
sorulur.

Örnek sayısı kesildikten sonra da bitmedi: 32 örnek / 1 sıçrama ile
**172 dakika** koştu ve hâlâ sürüyordu. Yani iş örnekleme değil
**geometri** ağırlıklı; şehrin tamamı bu makinede bir oturuşta
pişmiyor.

İş bölünebilir ve bölüneceği yer belli: **semt**. Oyun zaten semt semt
akıtıyor ve APV verisi de sahne sahne saklanıyor. APV bunun için kendi
mekanizmasını taşıyor — `partialBakeSceneList`: listede olmayan
sahnelerin hücreleri **korunur**. Alan `internal`, yansımayla
yazılıyor ve bulunamazsa koşum **durur**; sessizce tam pişirmeye
dönmek, bu işin varlık sebebini ortadan kaldırırdı.

İlk deneme yine başlamadı ve sebebi bu depoda zaten yazılıydı:
`AdaptiveProbeVolumes.BakeAsync()` toplu kipte `Lightmapping.isRunning`i
hiç `true` yapmıyor. Klasik `Lightmapping.BakeAsync()` **aynı APV
yolundan** geçiyor (yığın izi: `BakeAsync → OnBakeStarted →
PrepareBaking`) ve kısmi listeyi orada okuyor — yani klasik çağrı kısmi
pişirmeyi bozmuyor, tam tersine tek çalışan yol o.

Semt semt pişirmede bile bellek 14 GB'ta duruyor ve sebebi ölçüldü:
taban sahnedeki **arazi** tam statik (`m_StaticEditorFlags: 2147483647`)
ve 10 km²'lik bir yüzey her pişirmede ışın izleniyor. Açık bir şehirde
zeminin sıçraması baskın kaynaktır, yani onu kapatmak ucuz değil
**yanlış** olurdu; ama maliyetin nerede olduğu artık yazılı ve bir
sonraki turun kaldıracı bu.

### Ölçülüp "kusur değil" denilenler

* **Arazideki keskin üçgen kırıklar.** `heightmapPixelError` zaten 1 (en
  ince) ve kaynak Copernicus GLO-30, yani 30 m örnekleme. Kırıklar
  verinin kendi çözünürlüğü; daha ince arazi uydurmak kaynağın
  söylemediğini söylemek olurdu.
* **Havada duran yapı.** `ZeminDenetimi` 36.302 yapıyı ölçmüş, görünür
  boşluğu olan **0**. Gökte gördüğüm nesneler martıydı.
* **Kapı gölgesi dörtgenleri.** Tur 16'da *"yere yatık"* diye kaydedilmişti; kod okundu ve öyle kurulamıyor: `_frame`in 
  `size(du, dv, dn)` eşlemesi her iki cephe yönünde de üçüncü bileşene
  **yüksekliği** koyuyor, yani panel tanımı gereği düşey. Gözlem bir
  başka şeye ait olmalı; yeni kare gelmeden düzeltme yapılmadı —
  görülmeyen bir kusuru tahminle onarmak bu depoda üç kez geri tepti.
* **Kubbenin parlaklığı.** Kurşun örtü karede cilalı alüminyum gibi
  okunuyor (pürüzlülük ortalaması 0,49, metaliklik 0,63). Ama
  `gen_lead_texture.py`nin kendi başlığı şunu yazıyor: *"Aydınlatma
  fazında GI pişince bu tavan yeniden ölçülmeli."* Fırın daha
  bitmedi; dolaylı ışık olmadan hem pürüzlülük hem metaliklik
  yanlış okunur. **Dokunulmadı** — dayandığı ölçüden önce bir sayıyı
  değiştirmek, sonraki turda yanlış yerde aranan bir kusur üretir.

