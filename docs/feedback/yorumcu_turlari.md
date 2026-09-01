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
