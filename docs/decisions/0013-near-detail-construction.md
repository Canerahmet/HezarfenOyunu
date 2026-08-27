# ADR 0013 — Yakın plan yapım kademesi (yaya seviyesi)

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — çıktı **TASLAK**, Caner onayı bekliyor
**Talep:** Caner, 2026-08-20: *"yakın plan detayına geçelim çünkü oyun oynanışında
karakter İstanbul içerisinde de gezecek, sokaklarda; sadece uçma olmayacak.
Atmosfer o yüzden gerçekçi olması lazım."*
**İlgili:** ADR 0012 (kit ve malzeme), ADR 0006 (inceleme paketi), plan Faz 2

Bu, ADR 0012'de sorulan **Karar 2**'nin cevabıdır: seçenek **(B)**.

---

## 1. Karar: iki yapım kademesi, tek parametre kümesi

Oyun artık iki farklı mesafeden bakılan bir şehir istiyor: uçarken 8 000 ev,
yürürken 2 metreden bir kapı. Bunlar **çelişkili** gereksinimlerdir ve tek bir
model ikisini birden iyi yapamaz.

Çözüm iki ev tipi üretmek değil, aynı üreticiye bir **kademe** parametresi
vermek (`--detail`):

| | `mass` | `near` |
|---|---|---|
| Duvar | tek kütle | **delikli panel** (gerçek açıklık) |
| Açıklık | cepheye yapışık koyu panel | söve derinliği, denizlik, eşik, kanat |
| Saçak altı | düz | **mertekler** |
| Ek | — | subasman silmesi, karkas dikme/hatıl, mahya, baca külahı, basamak |
| Üçgen LOD0 | **944** | **1 980** (köşe evi, üç cepheli: 4 540) |
| LOD1 / LOD2 | 56 / 20 | **56 / 20 — değişmez** |

Son satır kararın belkemiğidir: yakın plan detayı **uzak siluete hiçbir şey
ödetmez**. Kalabalık şehir yine LOD1/LOD2 görür. Öz-test bunu kilitliyor
(`mass ve near ayni kutle`): iki kademe aynı parametrelerden aynı ayak izini,
aynı yüksekliği ve aynı uzak LOD'ları üretmek zorunda. Aksi hâlde LOD geçişinde
ev yerinden oynardı.

## 2. Gerçek açıklık — boolean olmadan

ADR 0012 açıklığı "cepheye yapıştırılmış koyu panel" olarak çözüyordu. O çözüm
30 m'den doğru, **3 m'den ölü**: yaya gözü açıklığın delik olduğunu duvar
kalınlığından, yani **söve derinliğinden** anlar. Panelde derinlik yoktur.

Gerçek delik normalde boolean ister; 8 000 ev ölçeğinde boolean hem yavaş hem
kırılgandır (dejenere yüz, tutarsız normal). Bunun yerine duvar doğrudan
**delikli örülür** (`hz.make_wall_panel`): açıklık kenarlarından düşey kesitler
alınır, her kesit boşluğun altında ve üstünde kapanır, açıklığın dört yanına
söve yüzeyleri eklenir. Sonuç dörtgen, kapalı ve deterministiktir.

Bütün yüzler açıklıkların v seviyelerinden geçen ortak banda bölünür. Sebebi:
bölünmezse komşu yüzler arasında **T-kavşağı** kalır, kabuk manifold olmaktan
çıkar ve normal hesabı güvenilmez hâle gelir.

### Kapı neden eşiğin üstünde

`make_wall_panel` açıklığın panel kenarına değmesini **reddeder**. Değen bir
açıklık kabuğu açık kenarlı bırakır. Bu bir kısıt gibi görünüp aslında doğru
mimariyi zorluyor: Osmanlı konutunda kapı taş bir **eşiğin** üstüne oturur,
zemine sıfırlanmaz.

## 3. Normal yönü doğrulanır, varsayılmaz

`recalc_face_normals` normalleri tutarlı yapar ama kapalı bir kabuğu bütünüyle
içe çevirmesi mümkündür. **Blender bunu göstermez** — arka yüzleri de çizer.
Unity ise arka yüzü eler: duvar orada *görünmez* olur ve hata ancak oyun içinde,
sebebi anlaşılmadan fark edilir.

Bu yüzden panel üretimi işaretli hacmi (diverjans teoremi) ölçer ve negatifse
kabuğu çevirip **söyler**. Sessiz düzeltme, sessiz hata kadar kötüdür.

## 4. Ölçümün yakaladığı üç hata

Yakın plan render'ları (`renders/review/House_A_Eye_v1/`) üç kusuru gösterdi.
Üçü de **uzaktan görünmüyordu**; kademenin gerekçesi tam olarak budur.

### 4.1 Kapı havada asılıydı

Ölçüldü: subasman 0,60 m + eşik 0,14 m = kapı eşiği sokaktan **0,74 m** yukarıda,
ve önünde hiçbir şey yok. Uzaktan fark edilmez, yaya seviyesinde apaçık kırık.

Düzeltme taş basamak. Basamak sayısı **yükseltiden türetilir**
(`n = round(rise / 0,20)`), sabit yazılmaz: subasman parametresi değiştiğinde
elle düzeltilmesi gereken bir sabit bırakmak, aynı hatayı geri davet etmektir.
RESEARCH.md'nin "merdivenli sokaklar" tarifiyle de tutarlı.

### 4.2 Ahşap kata taş denizlik

Üst kat ahşap karkas ama denizlik malzemesi `stone` diye sabitlenmişti; render'da
kırmızı ahşap duvarda taş denizlikler çıktı. Taşıyıcısı ahşap olan bir duvara
taş denizlik oturmaz. Denizlik artık duvarın malzemesini izliyor.

### 4.3 Cumbanın altı açıktı

`near` kipinde duvarlar kabuktur; çıkmanın altından geçen oyuncu evin içini
görebilirdi. Ahşap döşeme eklendi — hem kapatır hem cumba ucunda gözün aradığı
kalınlık çizgisini verir.

## 5. Ne eklendi (ve neden o)

Yakın planda gerçekçilik hissi doku çözünürlüğünden çok **gölge çizgilerinden**
gelir. Eklenenler bu ölçüte göre seçildi:

* **Saçak altı mertekleri** — en büyük tek kazanç. Yürüyen göz geniş saçağın
  ALTINI görür; orası evin en büyük tek yüzeyidir ve boş bırakılırsa bina anında
  kutuya döner. Mertekler kaplamanın **altında** durur (mertek → kaplama →
  kiremit); üstüne konsaydı yapım sırası tersine dönerdi.
* **Denizlik, söve, eşik, kapı kanadı** — hepsi açıklık çevresinde.
* **Subasman silmesi** — taştan ahşaba geçişi bir gölge çizgisiyle ayırır.
* **Ahşap karkas dikme + hatıl** — köşe merkezine oturur, iki cepheye birden
  taşar; hımış duvarın doğru okunuşu budur.
* **Mahya ve baca külahı** — külahsız baca gökyüzüne karşı çizilmiş bir
  dikdörtgen gibi durur.

## 6. Yaya seviyesi inceleme kipi (`--eye`)

ADR 0006'nın yörünge kameraları varlığı bir **müze nesnesi** gibi gösterir: hep
dışarıdan, hep tam kadraja sığmış. Oyuncu evi öyle görmez. Yakın plan detayının
işe yarayıp yaramadığı ancak 1,65 m'den, 2-5 m mesafeden, saçağa yukarı bakan
bir kadrajda anlaşılır.

`--eye` bunu ayrı bir kip olarak ekler; mevcut kipler değişmedi. Mesafe kütle
merkezinden değil **dış yüzeyden** ölçülür, yoksa geniş ve dar ev aynı "3 m"de
farklı uzaklıkta görünür ve kadrajlar kıyaslanamaz.

## 7. Blender tarafına öz-test

Unity'nin Editor testleri vardı, Blender tarafının yoktu. Delikli panel sessiz
bozulabilecek geometrik değişmezler getirdi, `tools/blender/selftest.py` onları
kilitliyor:

| Test | Ölçü |
|---|---|
| Duvar paneli su geçirmez | 2 yüze bağlı olmayan kenar sayısı = **0** |
| Normaller dışa dönük | işaretli hacim > 0 ve analitik hacme eşit |
| Kenara değen açıklık reddediliyor | beklenen istisna |
| Kapı/pencere çakışmıyor | 15 genişlik×yoğunluk birleşimi |
| Pivot taban merkezde | 5 parametre birleşimi |
| `mass` ve `near` aynı kütle | ayak izi, yükseklik, LOD1/LOD2 üçgenleri |

Su geçirmezlik ilk yazımda yalnızca hacim karşılaştırmasıyla sınanıyordu ve test
**1,1e-4**'lük bir farkla kaldı. Fark sızıntı değildi, float32 köşe
koordinatlarının birikme hatasıydı. Toleransı gevşetmek testin dişini sökerdi;
doğru cevap ölçüyü değiştirmekti — açık kenar sayısı bir **tam sayıdır**.

## 8. Ölçülen durum

| | House_A (sokak) | House_B (köşe) | House_M (kalabalık) |
|---|---|---|---|
| Kademe / cepheler | near / street | near / sides | mass / street |
| Üçgen LOD0 | 1 980 | 4 540 | 944 |
| LOD1 / LOD2 | 56 / 20 | 56 / 20 | 56 / 20 |
| Ayak izi × yükseklik | 8,90 × 8,70 × 8,51 m | 9,36 × 8,70 × 8,51 m | 8,90 × 8,70 × 8,51 m |
| Pivot | taban merkez ✅ | ✅ | ✅ |

Köşe evi **4 540** üçgen — ADR 0012'de verdiğim "(B) ~2 500-3 000" tahminini
aşıyor. Sebep: üç cephede kafesli pencere. Sokak evi (tek cepheli) tahmin
aralığında. Bu sayı Faz 4 bütçesiyle birlikte tekrar bakılmalı.

## 9. Bu ADR'nin SÖYLEMEDİĞİ

- **Alaturka kiremidin saçak ucu düz.** Gerçek alaturka çatı saçakta yuvarlak
  kiremit uçlarıyla biter; şu an düz bir alınlık tahtası var. Geometriyle
  çözmek çevre boyunca ~160 parça demek; doğru yeri doku/alfa.
- **Trim sheet/atlas hâlâ yok** (ADR 0012 §8 aynen geçerli).
- **Unity HDRP malzemeleri hâlâ yok.**
- **Basamak collider'a girmiyor.** `UCX_` siluetten dar tutuluyor (uçuş için).
  Karakter yürümeye başladığında basamağın çarpışması ayrıca çözülmeli.
- **Yan/arka cephe kararı hâlâ Caner'de.** `--facades` bir bayrak olarak var ama
  varsayılan `street`; bkz. `docs/feedback/ottoman_house.md` Karar 1.
- **20 parametre kombinasyonu ve "Galata sokağı" sahnesi yapılmadı**; Faz 2
  kabulü onu istiyor.

## Yeniden üretim

```powershell
$b = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
& $b --background --factory-startup --python tools\blender\selftest.py
& $b --background --factory-startup --python tools\blender\gen_ottoman_house.py -- `
    --asset House_A --textured --detail near --window-detail kafes `
    --cumba-type corbel --out-blend art\blend\SM_House_A.blend
& $b --background --factory-startup --python tools\blender\render_preview.py -- `
    --in art\blend\SM_House_A.blend --asset House_A_Eye --eye --hdri --samples 96
& $b --background --factory-startup --python tools\blender\measure_render.py -- `
    --in renders\review\House_A_Eye_v3\02_sokak_gecis.png --grid 4
```
