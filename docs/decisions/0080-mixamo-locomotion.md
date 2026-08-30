# ADR 0080 — Yer hareketi Mixamo'ya geçti; ve altı cetvel daha kırıldı

- **Tarih:** 2026-08-30
- **Durum:** kabul edildi ve uygulandı
- **İlişki:** ADR 0068 (karakter hattı), ADR 0076 (klip yeniden üretimi —
  **artık konu dışı**), ADR 0078 (ana plan), ADR 0079 (yanlış cetveller)

## Karar

Yer hareketi (duruş, yürüme, koşma, merdiven, basamak, sıçrama, düşme,
iniş, dönüş, yana adım, başla/dur) **Mixamo**'dan gelir. Uçuş
(süzülüş pozları, kalkış, iniş, çakılma) **elde kalır** — süzülüşün
mocap'i yok ve olamaz.

Caner 1. öbeği indirdi: **20 dosya**, hepsi mesh'siz (Without Skin
doğru), hepsi 30 FPS, 65 kemik.

## ADR 0076 bununla kapanmıyor — gereksizleşiyor

ADR 0076 açık bir kusurdu: on üç klibi toptan yeniden üretmek karakteri
parçalıyordu ve kök sebep bulunamamıştı. Mixamo klibi bizim Blender
hattımızdan **hiç geçmiyor**; yani o üretimi yapmıyoruz. Kusur duruyor
ama artık yolun üstünde değil. Kalan beş uçuş klibi elle yapıldı ve
yeniden üretilmiyor.

## "In Place" işaretlenmemişti — ve iyi ki

Ölçüldü: yürüme klibinde kök **1,845 m**, koşmada **3,324 m** yol
alıyor. Yeniden indirtmek yerine kök XZ Unity'de *Bake Into Pose* ile
alındı; sonuç birebir aynı. Üstelik o kök hareketi klibin öz hızını
verdi ve ilk çarpan hesabı ondan çıktı.

---

## Ve sonra altı cetvel kırıldı

Bu bölüm, sayıların kendisinden daha değerli.

### 1. "Unity Mixamo FBX'ini okuyamıyor" — okuyordu

Unity dosyada sıfır animasyon gösterdi: `importedTakeInfos = 0`,
`clipAnimations = []`. Dosya biçimini (FBX 7700), Unity'nin okuyucusunu
ve eksik deriyi suçladım; **üçü de masumdu**. Blender aynı dosyada 371
animasyon eğrisi okuyordu.

Suçlu bizim kendi kodumuzdu — `ModelImportPolicy`:

```csharp
if (!Path.GetFileName(assetPath).StartsWith("SK_")) {
    importer.animationType = ModelImporterAnimationType.None;
    importer.importAnimation = false;
}
```

Yorumu bile söylüyordu: *"İskeletli varlıklar (SK_ öneki) ayrıca ele
alınacak; **şu an boru hattında yoklar**."* O gün doğruydu. Mixamo
klipleri `MX_` ile başlıyor.

**Kusuru ortaya çıkaran şey kontroldü:** çalıştığı bilinen bir klibi
aynı yoldan geçirdim ve o da sıfır klip verdi. Bir ölçüm kendi
doğruluğunu kanıtlayamıyorsa, önce ölçümü sınamak gerekir.

Bu arada yanlış teşhis üzerine bir **Blender dönüştürme adımı**
(`mixamo_donustur.py`) ve ona bağlı bir "taşıyıcı mesh" yazılmıştı.
Politika düzelince ham dosyanın doğrudan çalıştığı ölçüldü
(`takes=1, clips=1, avatar=geçerli`) ve adım **silindi**. Yanlış
teşhisle kurulan bir aracı, işe yaramadığı anlaşıldığında tutmak
borçtur.

### 2. Klibin hızı Blender'da ölçülemez

Blender'daki 1,786 m/s **Mixamo'nun iskeletinin** hızı. Humanoid
yeniden hedefleme pozları kas uzayında taşır; adım boyu **hedefin**
oranlarıyla ölçeklenir. Ölçülen: bizim gövdemizde aynı klip
**1,693 m/s**. Oran 0,948 — ölçülen `humanScale` oranıyla (0,928)
tutarlı.

### 3. `SampleAnimation` bu iş için yanlış alet

Editörde örneklendiğinde kök hareketi nesnenin **kendisine** uygulanıyor;
ayak köke göre neredeyse duruyor. Ölçüm 0,133 m/s dedi.

### 4. `Animator.Update` Edit kipinde değerlendirmiyor

0,000 m/s.

### 5. Kamerasız sahnede Animator hiç çalışmıyor

Varsayılan culling "ekranda değilse güncelleme"dir. Test sahnesinde
kamera yok. `AnimatorCullingMode.AlwaysAnimate` şart.

### 6. Temas eşiği üç kez yanlış ölçtü

| tanım | sonuç |
|---|---|
| "ayak 6 cm'den alçaksa basıyor" | sallanma başı/sonu pencereden geçiyor; kayma %15 fazla |
| "alçak ayağın hızının üst çeyreği" | koşu yürümeden yavaş çıktı |
| "en alçak kotu ayrı pencerede bul" | koşuda pencereler örtüşmedi, sıfır örnek |

Doğrusu (`OrtaDurus`): en alçak kot **ölçümün kendi örneklerinden**
bulunur ve ±1 cm'lik banttaki kareler okunur. Üstüne iki düzeltme daha
gerekti:

- **240 Hz örnekleme.** 60 Hz'de 6 m/s'lik koşuda gövde kare başına
  10 cm gidiyor ve temasın orta anı bir-iki kareye sığıyor; ölçüm
  turdan tura 0,67 ile 2,45 m/s arasında zıplıyordu.
- **Eksenel bileşen, büyüklük değil.** Basan ayak yana da salınır ve o
  hareket kayma değildir; büyüklük almak yürümede ~0,12 m/s'lik sahte
  bir taban bırakıyordu.

---

## Ölçülen sonuç

| klip | ham (yeniden hedeflenmiş) | eşik | çarpan | orta duruş kayması |
|---|---|---|---|---|
| Yürüme (Walking) | **1,693 m/s** | 2,2 | 1,299 | **0,05 m/s** (ort), max 0,21 |
| Koşma (Fast Run) | **~6,0 m/s** | 6,0 | ~1,0 | **0,05 m/s** (ort), max 2,17 |

0,05 m/s, yürüyüşte tipik bir basış boyunca (~0,6 s) **3 cm** eder —
tarihsel 5 cm kapısının altında. Koşudaki 2,17 m/s'lik tepe, dokunuş ve
kalkış anlarının geçici değeridir; orta duruş temiz.

**Kapı artık oransal:** yol hızının %5'i. Sebep: 5 cm bir *mesafe*
eşiğiydi ve basış süresi yürüyüşte ~0,6 s, koşuda ~0,2 s. %5 her iki
gaitte de ≈6 cm eder; tarihsel kapının anlamı korunur, gaitten bağımsız
yazılır.

## Sayının tek sahibi

`art/mixamo/meta.json`:

- `oz_hiz_ms` — Blender'da, Mixamo iskeletinde ölçüldü (yedek)
- `unity_hiz_ms` — **oyunda**, bizim avatarımızda ölçüldü (kullanılan)

Yazan taraf `KlipYerHiziOlcumu` (PlayMode), okuyan taraf `AnimatorKur`,
kapıyı tutan `AyakKaymasiTests`. Üçü de aynı `OrtaDurus` tanımını
kullanır — iki farklı tanım olsaydı biri ötekini hiçbir zaman
doğrulayamazdı.

## Durum

- 20 klip yerleşti, `_Import` boş.
- Animator: yer hareketi `MX_*`, uçuş `SK_*` — hangisinin elendiği
  loglanıyor.
- **368 EditMode + 48 PlayMode yeşil.**
- Henüz bağlanmayanlar: yana adım, başla/dur, yerinde dönüş, basamak,
  sıçrama/düşme/iniş. Bunlar durum makinesinin genişletilmesini
  gerektiriyor ve kendi kapısıyla ayrı bir iş.
