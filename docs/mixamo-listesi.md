# Mixamo indirme listesi — Caner için

Lisans kaydı yazıldı (`refs/LICENSES.md`): Mixamo klipleri telifsiz,
ticari kullanıma açık, atıf gerekmiyor. Tek kısıt kendilerini animasyon
paketi olarak yeniden satmamak — oyunun içinde kullanmak amaçlanan
kullanımdır.

---

## Her indirmede aynı olan ayarlar

`mixamo.com` → Adobe ID ile gir. Karakter olarak Mixamo'nun kendi
**X Bot**'unu seç — bizim modelimizi yüklemeye gerek yok, çünkü:

| ayar | değer | neden |
|---|---|---|
| Format | **FBX Binary (.fbx)** | Unity doğrudan okur |
| Skin | **Without Skin** | Gövde bizim (MPFB2). Mesh gelmesin; dosyada yalnız iskelet + hareket olur ve Mixamo'nun modeli oyuna hiç girmez |
| Frames per Second | **30** | `anim_kit.FPS` ile aynı; farklı olursa hız yeniden örnekleme gerektirir |
| Keyframe Reduction | **none** | "uniform" hareketi yumuşatıp ayak temasını kaydırır |
| **In Place** | **işaretli** (döngü ve locomotion için) | Karakteri `WalkController` yürütüyor; kök hareketi bizde. İşaretlenmezse karakter hem klip hem kontrolcü tarafından itilir ve iki kat hızlı gider |

**In Place kutusu bazı kliplerde yoktur** (dönüş, oturma, kapı açma gibi
yerinde olanlarda zaten gerekmez). Yoksa merak etme, olduğu gibi indir.

**Dosya adını değiştirme.** Mixamo ne verirse o kalsın; hangi klibin ne
olduğunu ben o addan eşleştireceğim.

**Nereye:** tek bir klasör aç (ör. `D:\Hezarfen_Mixamo\`), hepsini
oraya at, yerini söyle. Ben `Assets/_Project/Art/Animation/Mixamo/`
altına indirir, Humanoid olarak ayarlar, döngü bayraklarını kurar ve
`art/mixamo/meta.json` kaydını yazarım.

> **Not:** Mixamo tek tek indirtiyor, toplu indirme yok. Bir klip
> ~20–30 saniye. Aşağıdaki **1. Öbek** 18 klip, yani yaklaşık 10 dakika.
> Öbek 2 ve 3'ü şimdi indirmene gerek yok — o fazlar geldiğinde
> isteyeceğim.

---

## 1. ÖBEK — şimdi lazım (Faz II.A) · 18 klip

Bunlar oyunun **bugün** kullandığı hareketler. Animator grafiğinde
karşılıkları hazır; geldikleri gün devreye girerler.

| # | Mixamo'da ara | ne için | döngü | In Place |
|---|---|---|---|---|
| 1 | `Breathing Idle` | duruş — karakterin çoğu zaman yaptığı şey | ✔ | ✔ |
| 2 | `Idle` | duruş çeşitlemesi (aynı pozda donup kalmasın) | ✔ | ✔ |
| 3 | `Walking` | yürüme — **en çok görülecek klip** | ✔ | ✔ |
| 4 | `Walking Backwards` | geri adım | ✔ | ✔ |
| 5 | `Left Strafe Walking` | yana adım (üçüncü şahıs kamerada nişan/bakış) | ✔ | ✔ |
| 6 | `Right Strafe Walking` | yana adım | ✔ | ✔ |
| 7 | `Running` | koşma | ✔ | ✔ |
| 8 | `Fast Run` | hızlı koşu (aseslerden kaçarken) | ✔ | ✔ |
| 9 | `Start Walking` | duruştan yürüyüşe geçiş | ✘ | ✔ |
| 10 | `Walking To Stop` | yürüyüşten duruşa | ✘ | ✔ |
| 11 | `Left Turn` | yerinde sola dönüş | ✘ | — |
| 12 | `Right Turn` | yerinde sağa dönüş | ✘ | — |
| 13 | `Jumping Up` | sıçrama | ✘ | ✔ |
| 14 | `Falling Idle` | düşerken (uçuş değil — çatıdan düşme) | ✔ | ✔ |
| 15 | `Hard Landing` | sert iniş | ✘ | ✔ |
| 16 | `Climbing Ladder` | merdiven — **kuleye çıkış** | ✔ | ✔ |
| 17 | `Walking Up Stairs` | merdiven basamağı (kule sarmal merdiven) | ✔ | ✔ |
| 18 | `Walking Down Stairs` | inerken | ✔ | ✔ |

**Adı birebir bulamazsan:** en yakınını al ve bana **indirdiğin
dosyanın adını** söyle. Mixamo isimleri zaman zaman değişiyor; benim
tahminim değil senin indirdiğin dosya esastır.

---

## 2. ÖBEK — kalabalık dönünce (Faz II.G) · 18 klip

NPC'ler şu an kapalı (ADR 0077). Geri döndüklerinde **hepsi aynı
şekilde durursa** kalabalık bir kopya ordusu gibi görünür — asıl
çeşitlilik burada.

| # | Mixamo'da ara | ne için |
|---|---|---|
| 19 | `Standing Idle` | duruş çeşidi 2 |
| 20 | `Idle 2` / `Standing Idle 01` | duruş çeşidi 3 |
| 21 | `Looking Around` | çevreye bakınan adam |
| 22 | **`Looking Up`** | **Hezarfen uçarken herkes yukarı bakmalı — oyunun imza anı** |
| 23 | `Surprised` / `Standing React` | şaşırma (aynı an) |
| 24 | `Pointing` | "bakın, uçuyor!" |
| 25 | `Talking` | ayakta konuşma |
| 26 | `Standing Greeting` | selamlaşma |
| 27 | `Bow` / `Bowing` | temenna — dönemin selamı |
| 28 | `Sitting Idle` | oturan adam (dükkân önü, kahve) |
| 29 | `Sitting Down` | oturma geçişi |
| 30 | `Standing Up` | kalkma geçişi |
| 31 | `Sitting Talking` | oturup sohbet |
| 32 | `Slow Walking` / `Old Man Walk` | yaşlı yürüyüşü — yaş DNA'sı için |
| 33 | `Walking With Heavy Object` / `Carrying` | hamal, yük taşıyan |
| 34 | `Hammering` | zanaatkâr (works katalogunda 15 iş yeri var) |
| 35 | `Scared Running` / `Running Away` | kaçış (yangın, ases baskını) |
| 36 | `Kneeling Down` | diz çökme (yerde iş yapan, dilenci) |

---

## 3. ÖBEK — iç mekân ve etkileşim (Faz II.D / II.H) · 10 klip

Evlerin içi girilebilir olduğunda ve `IEtkilesim` geldiğinde.

| # | Mixamo'da ara | ne için |
|---|---|---|
| 37 | `Opening A Door` | gerçek kapı — Faz II.D'nin görünen yüzü |
| 38 | `Picking Up Object` | yerden alma |
| 39 | `Putting Down Object` | bırakma |
| 40 | `Sitting` (bağdaş) | **sedirde bağdaş kurmak** — dönemin oturuşu |
| 41 | `Getting Up From Ground` | yerden kalkma |
| 42 | `Sleeping Idle` / `Laying` | yatak/döşek |
| 43 | `Drinking` | kahve, şerbet |
| 44 | `Eating` | sofra |
| 45 | `Reading` | rahle başında — Hezarfen bir âlim |
| 46 | `Writing` | not alma, çizim yapma |

---

## İNDİRME — ve neden

### Uçuş kliplerinin hiçbiri

Süzülüş, kalkış, iniş, çakılma. **Mocap'i yok** ve olamaz: kimse
17. yüzyıl kanadıyla Galata'dan Üsküdar'a süzülmedi. Bu beş klip elde
yapıldı ve elde kalacak (ADR 0068). Mixamo'da "flying" arayıp bulacağın
şey süper kahraman uçuşu olur ve oyunun tonunu bozar.

### Namaz

Mixamo'da yok. `Praying` diye geçen klip **Hristiyan diz çökmesi** —
rükû ve secde değil. Oyunda namaz vakitleri zaten işliyor
(`VakitHesabi`), o yüzden hareketi elle üreteceğim ve **T2** olarak
işaretleyeceğim. Yanlış bir dua hareketi koymaktansa doğrusunu yapmak.

### Silah, dövüş, ateş etme, araç

Oyunda yok. ADR 0078 bunları kapsam dışı bıraktı: bu bir uçuş/keşif
oyunu. Yakalanma gerilimini `AranmaSistemi` kaçış ve saklanmayla
kuruyor, kavgayla değil.

### Modern duruşlar

Eller cepte, taktik nişan, dans, telefon, kaykay. Entari giymiş
17. yüzyıl adamı böyle durmaz. Listedekiler bilinçle **nötr** seçildi.

---

## Bir dürüst uyarı

Mixamo hareketleri **modern insanın** hareketleridir. Uzun entari
giymiş, kuşak bağlamış bir adamın yürüyüşü daha kısa adımlı ve daha
dik gövdelidir. İyi haber: entari bacakları zaten örtüyor, o yüzden
fark çoğunlukla gövde ve kolda kalır ve onu yeniden hedefleme sırasında
ayarlayabilirim.

Kötü haber şu olurdu: klipleri olduğu gibi alıp "tamam" demek. Öyle
yapmayacağım — her klip geldiğinde **oyunda ayak kaymasını ölçeceğim**
ve 5 cm'yi geçen klip girmez. Bugünkü gözden geçirmede bu ölçünün
oyunda hiç yapılmadığı ortaya çıktı (`docs/GOZDEN-GECIRME-2026-08-30.md`,
Bölüm 2); cetveli klipler gelmeden kuracağım.
