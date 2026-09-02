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
