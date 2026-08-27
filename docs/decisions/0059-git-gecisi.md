# ADR 0059 — Git'e geçiş ve ikili varlık politikası

**Tarih:** 2026-08-27
**Durum:** Kabul edildi
**Karar veren:** Caner (depoyu açtı ve ikili politikasını seçti)
**Yerine geçtiği:** [ADR 0003](0003-local-first-vcs.md) — lokal önce

## Bağlam

ADR 0003 sürüm kontrolünü bilinçli olarak ertelemişti; klasör yapısı,
`.gitignore` ve `.gitattributes` hazır bekliyordu. Proje o karardan bu yana
Faz 3'ün sonuna geldi: 36 anıt, 241 test, 58 ADR. Caner geçiş vaktinin
geldiğine karar verdi ve `HezarfenOyunu` deposunu açtı.

## Ölçüm

Geçişten önce depoya ne gireceği ölçüldü — tahmin edilmedi:

| grup | dosya | boyut | niteliği |
|---|---:|---:|---|
| `unity .../Art/Textures` | 61 | 334 MB | türetilmiş (`build_unity_maps.py`) |
| `art/textures/polyhaven` | 49 | 244 MB | **indirilebilir** (`fetch_polyhaven.py`) |
| `unity .../Art/Models` | 153 | 29 MB | türetilmiş (`export_fbx.py`) |
| `art/blend` | 126 | 21 MB | **kanonik** (ADR 0005) |
| `art/textures/hdri` | 1 | 20 MB | **indirilebilir** |
| `art/textures/generated` | 21 | 15 MB | türetilmiş (`gen_*_texture.py`) |

**663 MB LFS yükünün yalnızca 21 MB'ı kanonikti.**

Asıl mesele ilk boyut değil **büyüme hızı** çıktı: LFS'te türetilmiş bir dosya
her yeniden üretildiğinde yeni bir nesne yaratır ve eskisi depolamada kalıcı
olarak durur. Tek bir tam doku yeniden üretimi +334 MB kalıcı ekler; ayrıntı
geçişi sırasında 56 FBX'i iki kez aktarmak bir günde ~58 MB demekti.
GitHub'ın ücretsiz kotası 1 GB depolama / 1 GB aylık indirmedir.

## Karar

**Yeniden indirilebilir üçüncü taraf kaynakları dışarıda kalır; Unity'nin
okuduğu her şey depoya girer.**

Dışarıda: `art/textures/polyhaven/**/*.jpg|png|exr`, `art/textures/hdri/**`.
İçeride: `meta.json` kayıtları, `refs/LICENSES.md`, ve Unity tarafındaki
bütün dokular/modeller.

Gerekçe `data/` için verilmiş kararın (ADR 0007) aynısı: **türetilmiş ya da
tekrarlanabilir biçimde indirilebilir veri depoya girmez, ama kaydı girer.**
`fetch_polyhaven.py` sabit bir `CATALOG`tan indirir, her dokunun gerçek dünya
ölçüsünü `meta.json`a yazar ve üreticileri `refs/LICENSES.md`ye ekler — yani
*hangi* dokunun, *hangi* çözünürlükte, *hangi* ölçekte alındığı bilgisi proje
bilgisidir ve kalır; pikseller bir indirmedir.

Sonuç: **1 427 dosya, 492 MB.**

### Unity tarafı neden türetilmiş olduğu hâlde girer

GUID'ler yüzünden. Bu projedeki her malzeme ve prefab referansı bir `.meta`
dosyasındaki GUID'dir. Bir varlığı `.meta`sı olmadan yeniden üretmek yeni bir
GUID doğurur ve ona yapılan bütün referansları **sessizce** kırar. Boru hattı
çıktısı ile `.meta` dosyaları birlikte yolculuk etmek zorundadır.

## Geçişte bulunan dört kusur

Hepsi ölçülerek bulundu:

1. **`TD_Istanbul.asset` (32 MB) İKİLİ ama metin kuralına düşüyordu.**
   Unity'nin "Force Text" ayarına rağmen TerrainData ikili serileşir; genel
   `*.asset merge=unityyamlmerge` kuralı onu YAML birleştiricisine veriyordu.
   Hem birleştirmeyi bozar hem her düzenlemede geçmişe 32 MB'lık yeni bir blob
   yazardı. Yola özgü bir kuralla LFS'e alındı. (Dosyanın başında `%YAML`
   imzası olmadığı ölçülerek doğrulandı.)

2. **`art/textures/polyhaven/**` yazınca `meta.json` geri-dahil kuralı hiç
   çalışmıyordu.** Git dışlanmış bir **dizinin** içine hiç inmez, dolayısıyla
   `!.../meta.json` asla değerlendirilmez. `git check-ignore` ile görüldü;
   dışlama dosya **tipine** göre yeniden yazıldı, dizinler gezilebilir kaldı.

3. **`data/` kuralı Unity'nin `_Project/Data`'sını yutuyordu.** GIS türevleri
   için yazılmış kural köke sabitlenmemişti; git'te baştaki `/` olmayan bir
   kural **her derinlikte** eşleşir ve Windows'ta `core.ignorecase` açık
   olduğu için `Data` ile `data` da aynı sayılıyordu. 28 ScriptableObject
   (ilçe tanımları, rüzgâr profilleri, girdi eylemleri) hiç commit'e girmedi.

   **Sessiz olmasının sebebi:** `git add` yok sayılan bir yolu uyarı vermeden
   atlar, üstelik klasörün kendi `.meta` dosyası takipteydi — yani ağaç
   makul görünüyordu. Ortaya çıkaran şey **sayımdı**: commit edilmiş ağaçta
   `Data` 0 dosya derken diskte 109 KB duruyordu. Düzeltmeden sonra diskle
   dizin karşılaştırması baştan sona yapıldı; sessizce düşen başka dosya yok.

4. **Depo kökünde başıboş bir `Assets/_Import/tmp.fbx`.** Ayrıntı geçişinde
   göreli bir yol yüzünden oraya düşmüş; planlanan yapıda kökte `Assets/`
   diye bir dizin yok. Silindi.

## Çalışma biçimi

- Tek dal: **`main`**. Tek kişilik üretim ve faz kapılı bir projede dal
  yönetimi maliyeti karşılığını vermiyor; faz kabulleri **etiketle**
  (`faz3-kabul` gibi) işaretlenir.
- Commit mesajları CLAUDE.md kuralına uyar: **İngilizce, kısa, emir kipi**
  başlık; ardından **gerekçeyi** anlatan gövde. Gövde "ne" değil "niçin"
  yazar — "ne" zaten diff'te durur.
- İlk `git push` bir kez tarayıcıdan GitHub girişi ister (Git Credential
  Manager kurulu, `gh` CLI yok). Kurulum ve onay Caner'in rolüdür; sonrası
  Claude'da.

## Kabul edilen bedel

- Taze bir klon oyunu açar ama `art/textures/polyhaven` boş gelir. Doku
  **yeniden üretmek** gerekirse önce `python tools/textures/fetch_polyhaven.py
  --res 2k --hdris` koşulur. Unity'yi açmak için gerekmez.
- 492 MB ücretsiz 1 GB depolamaya sığar ama rahat değil. Türetilmiş Unity
  dokularının yeniden üretimi sıklaşırsa sıradaki adım onları da dışlamak ve
  bir bootstrap scripti yazmaktır; `.meta` dosyaları kalacağı için GUID'ler
  korunur. O adım **şimdi atılmadı** çünkü bedeli (her klonda yeniden üretim
  zinciri) bugünkü faydasından büyük.

İlgili: [ADR 0003](0003-local-first-vcs.md), [ADR 0005](0005-asset-pipeline.md),
[ADR 0007](0007-dem-terrain.md)
