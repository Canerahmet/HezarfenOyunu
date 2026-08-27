# ADR 0015 — Trim sheet / atlas: ölçüldü, şimdilik gerekmiyor

**Tarih:** 2026-08-20
**Durum:** Önerilen — **plandan sapma, Caner onayı bekliyor**
**İlgili:** plan Faz 2 doku stratejisi; ADR 0010 (HDRP kararı), ADR 0014 (HDRP malzemeleri)

---

## 1. Soru

Plan doku stratejisini "2–3 trim sheet + 1 atlas" diye kilitliyor. Gerekçe
mantıklı: ev başına 6 malzeme, 8 000 evde 48 000 malzeme bağlaması demek —
kulağa felaket gibi gelir.

ADR 0014 bunu "sıradaki iş" diye bıraktı ama bir şartla: **kararı ölçüme
dayandırmak.** Çünkü atlas bedava değil ve bizim durumumuzda özel bir bedeli
var: UV'ler dünya ölçeğinde üretiliyor ve 0-1 aralığını **aşıyor** (tekrar
mesh'te yaşıyor). Atlas tekrarı doğrudan desteklemez; trim sheet düzeni,
`Texture2DArray` ya da shader tarafında `frac()` gerekir. Üçü de dokunun
"gerçekten 2 m kaplaması" özelliğini (ADR 0012 §5) riske atar.

Yani: **önce ölç, sonra öde.**

## 2. Ölçüm

`Bench_Galata_Ottoman` sahnesi, gerçek arazi, 8 000 dağıtılmış + 400 yoğun
sokak evi, hepsi **gerçek kit evi** (`PF_House_A`: 1 980 üçgen LOD0, 6 malzeme).
RTX 4070 Laptop, Editor Play modu (yani ölçüm **kötümser**).

### Kule tepesinden — uçuş bakışı (1080p)

| | medyan | p95 | çizim | SRP Batcher | **setPass** | üçgen |
|---|---|---|---|---|---|---|
| boş arazi + su | 3,53 ms | 4,71 ms | 282 | 0 | **31** | 0,29 M |
| 8 000 yapı | 3,72 ms | 4,86 ms | 1 356 | 1 074 | **36** | 0,31 M |

### Sokak seviyesinden — yaya bakışı (1080p, 1,65 m)

| | medyan | p95 | çizim | SRP Batcher | **setPass** | üçgen |
|---|---|---|---|---|---|---|
| boş (kıyas) | 4,83 ms | 7,48 ms | 281 | 0 | **31** | 0,29 M |
| 8 000 + 400 yapı | 5,39 ms | 6,85 ms | 2 958 | 2 677 | **43** | 0,40 M |

> Dürüstlük notu: boş sokak adımının p95'i (7,48 ms) evli adımdan **kötü**
> çıktı. Bu ölçüm gürültüsüdür — o adım ağır bir adımın hemen ardından koştu.
> Güvenilir kıyas medyandır: 4,83 → 5,39 ms, yani evlerin payı **0,56 ms**.

## 3. Bulgu: SRP Batcher maliyeti zaten yutuyor

Kritik sayı çizim çağrısı değil, **setPass**'tir — GPU'ya "artık şu malzemeyle
çiziyoruz" demenin maliyeti odur.

* Sokakta 8 400 ev, ev başına 6 malzeme → naif beklenti on binlerce bağlama.
* **Ölçülen: setPass 31 → 43. Toplam +12.**
* Evlerin ürettiği 2 677 çizim çağrısının **2 677'si** (yani hepsi) SRP
  Batcher'a düştü.

Sebebi: 10 malzemenin **hepsi aynı shader'ı** kullanıyor (`HDRP/Lit`). SRP
Batcher malzeme başına sabit tamponu GPU'da tutar ve shader değişmediği sürece
malzeme değişimi neredeyse bedavadır. Atlas'ın çözmeye çalıştığı problem bu
mimaride büyük ölçüde **zaten çözülmüş**.

LOD de payını veriyor: 8 400 evin üçgen katkısı yalnızca **0,11 M** (0,29 →
0,40 M). LOD0 altı alt-mesh taşır ama LOD0'a yalnızca ~15 m'ye kadar giren
birkaç ev girer; geri kalan LOD2'dir (iki alt-mesh, 20 üçgen).

Bütçe: 60 fps = 16,67 ms. En kötü kadrajda p95 **6,85 ms = bütçenin %41'i**.

## 4. Karar

**Atlas/trim sheet YAPILMIYOR.** Yerine yapılan, aynı derdin ölçülebilir olan
kısmını çözüyor:

**Doku tekilleştirme.** Maske ve normal haritaları **kaynak dokuya** aittir,
role değil. İlk yazımda ikisi de malzeme adıyla yazılıyordu ve
`weathered_planks` dört rolde kullanıldığı için aynı 2K maske ve aynı 2K normal
**dört kez** diske yazıldı.

| | önce | sonra |
|---|---|---|
| Dosya | 27 | **21** |
| Disk | 271,5 MiB | **188,2 MiB** (−%31) |

Boyalı albedo role aittir (her rolün boyası farklı) — o paylaşılmadı.

Unity'den ölçülen çalışma zamanı maliyeti: bir ev (varsayılan palet)
**13 benzersiz doku, 109,8 MB VRAM**. Bu, 8 GB'lık karta göre rahat ama kitin
tamamı değil — plan cami, sur, dükkân, çeşme, iskele de istiyor ve VRAM §5'teki
tetikleyici listesinde bu yüzden var.

### 4.1 Yeniden adlandırmanın sessiz bedeli

Tekilleştirmeyle birlikte malzeme adları da ayrıştırılmıştı (ADR 0014 §6).
FBX, Blender'daki malzeme **adlarını** taşır; ad değişip FBX yeniden ihraç
edilmezse model eski ada bağlı kalır. Eski ad hâlâ var olan **başka** bir
malzemeyi gösteriyorsa hata tamamen sessizdir: "malzeme bulundu, HDRP,
maskesi var" testlerinin hepsi geçer, ama ev yanlış boyayı giyer.

Tam olarak bu oldu — varsayılan paletin trim'i `M_Timber_Dark` → `M_Timber_Trim`
oldu, eski ad gayrimüslim paletin **ahşabına** geçti, bayat FBX ona bağlandı.
Yakalayan şey bir test değil, VRAM ölçümü sırasında listeye bakmam oldu.

`OttomanHouseTests.House_UsesExactlyTheDefaultPalette` artık bu sınıfı kapatıyor:
LOD0'ın malzeme **kümesi** paletle birebir örtüşmek zorunda.

## 5. Bu karar ne zaman yeniden açılır

Ölçüm bugünün yapılandırmasına aittir. Şunlardan biri olursa **yeniden ölçülür**:

* **Kitte farklı bir shader gerekirse** — cam, bitki, saydam kafes, dekal.
  SRP Batcher'ın avantajı shader'ın aynı kalmasına bağlıdır; ilk farklı shader
  o grubu batch dışına çıkarır.
* **Malzeme sayısı ev başına belirgin artarsa** (şu an 6).
* **VRAM sıkışırsa.** Kit tek başına 197 MB; plan cami, sur, dükkân, çeşme,
  iskele de istiyor. Sınır bu makinede 8 GB.
* **Hedef donanım düşerse.** SRP Batcher bir **CPU** kazancıdır; daha zayıf bir
  GPU'da darboğaz yer değiştirir ve ölçüm baştan yapılmalıdır.

Varyant sayısının artması (Faz 4'te ~20 ev tipi) bu kararı **etkilemez**:
varyantlar mesh çeşitliliğidir, malzeme sayısını artırmaz.

## 6. Bu plandan bir SAPMADIR

Plan açıkça "2–3 trim sheet + 1 atlas" diyor. Ölçüm o gerekçenin bu mimaride
karşılanmadığını gösteriyor, ama planı ölçüme dayanarak değiştirmek Caner'in
kararıdır.

- **(A) Atlas'ı yapma, ölçümü tetikleyici listesiyle birlikte kabul et.**
  Kazanılan zaman Faz 2'nin kalan kabul maddesine ("20 kombinasyon + Galata
  sokağı sahnesi") gider. **Önerim bu.**
- **(B) Yine de yap.** Bedeli: dünya ölçekli UV'den vazgeçmek ya da özel shader
  yazmak; ikisi de ADR 0012 §5'in texel yoğunluğu garantisini zayıflatır.
  Kazancı ölçülemedi — setPass zaten 43.

## Yeniden üretim

```
Unity: Hezarfen -> Olcum -> Benchmark sahnesi kur (Osmanli evi)
Sonra Play (~90 sn). FrameTimeProbe adimlari konsola yazar.
```
