# ADR 0078 — Üretim hattı ana planı (GTA/RDR ölçeğinde görsel hedef)

- **Tarih:** 2026-08-30
- **Durum:** kabul edildi
- **Bağlam:** Caner, ChatGPT'den aldığı 51 maddelik bir "AAA açık dünya
  üretim hattı" listesini getirdi ve *"bunu bizim oyuna en uygun şekilde
  uygulayalım, temel planlardan biri bu olsun, baştan sona oyunu buna göre
  düzenle"* dedi.

## Karar özeti

Listeyi **ana plan olarak kabul ediyorum** — ama olduğu gibi değil,
**ölçülmüş duruma göre** uyarlayarak. Üç grup var:

1. **Zaten var** (23 madde) — listedeki sistemin karşılığı depoda çalışıyor.
   Yapılacak iş: eksik katmanı tamamlamak, sıfırdan kurmak değil.
2. **Yeni ve alıyoruz** (14 madde) — gerçek boşluk; plana faz olarak girdi.
3. **Almıyoruz** (7 madde) — gerekçesi aşağıda, her biri ölçüye dayalı.

Kapsam dışı kalan maddeler (araç fiziği, ateşli silah, ragdoll savaşı,
şehir trafiği) oyunun türüyle ilgili: bu bir **1632 uçuş/keşif** oyunu,
suç-aksiyon oyunu değil. Listenin *sistem düşüncesini* alıyoruz, *içerik
listesini* değil.

---

## 1. Zaten var — ölçüldü

Listeyi yazan taraf depoyu görmüyordu; bu maddelerin çoğu Türkçe adlarla
kurulu olduğu için "yok" sanılmış olabilir.

| ChatGPT maddesi | Bizdeki karşılığı | Durum |
|---|---|---|
| #2 Chunk sistemi | `DistrictStreamer`, `DistrictDef`, `DistrictRegistry`, `DistrictAnchor` + Addressables | çalışıyor (semt tabanlı) |
| #3 Terrain | Copernicus DEM GLO-30 → `TD_Istanbul.asset`, dünya orijini Galata Kulesi | çalışıyor, gerçek coğrafya |
| #5 Modüler bina | `ottoman_kit` + 26 parametrik varyant, 10.868 yerleşim | çalışıyor, **çeşitlilik yetersiz** |
| #9 Karakter tabanı | MPFB2 (MakeHuman) headless, bugün entegre edildi | çalışıyor |
| #11 Modüler kıyafet | `kiyafet_kit` — gömlek/şalvar/entari/kuşak/mest/sarık ayrı kabuklar | çalışıyor |
| #12 Saç kartları | `sac_kit` alfa kartları + sakal kabuğu | çalışıyor |
| #14 Animasyon sistemi | `HezarfenAnimator`, `WalkController`, `anim_kit` 13 klip | çalışıyor |
| #17–19 NPC AI | `NPCAjan`, `NPCMeslek`, `Rutin`, `SehirGunu`, `SehirOlayi`, `AranmaSistemi`, `Ihlal` | **ADR 0077 ile kapalı**, kod duruyor |
| #19 Crowd LOD | `NPCYonetici` mesafeye göre gövde bütçesi + dilimli hedef yenileme | çalışıyor |
| #21 Lighting | HDRP + `VP_Kalici_Aydinlatma` (SSGI, sis, poz, tonemap, grain) | **kısmi** — AO, temas gölgesi, bloom, derecelendirme yok |
| #22 Gün/gece | `ZamanSistemi`, `VakitHesabi` (namaz vakitleri dahil) | çalışıyor |
| #23 Light Probes | 19 ProbeVolume referansı | **APV pişmemiş** — ADR 0072 yarım |
| #30 Kamera | Cinemachine 3.1.7, `UcusKamerasi`, `KameraKipi` | çalışıyor |
| #37 Etkileşim | — | yok (aşağıda) |
| #39 Görev | `Gorev.cs`, `Kronoloji` | iskelet var |
| #40 Diyalog | `BarkKorpusu`, `BarkGosterici` (çevrimdışı üretilmiş replikler) | bark düzeyinde |
| #41 Save | `Kayit.cs`, `KayitBaglayici` | **dar** — kapsam genişletilecek |
| #42 Performans ölçümü | `FrameTimeProbe`, `Hezarfen → Olcum → Kare suresini bolustur` | çalışıyor, her fazda kullanılıyor |
| #43 Addressables | semt sahneleri paketleniyor | çalışıyor |
| #44 ECS kullanma | kullanmıyoruz | zaten öyle |
| #45–46 Claude Code rolü + CLAUDE.md | `CLAUDE.md` var ve bağlayıcı | çalışıyor |
| #47 Klasör yapısı | `Assets/_Project/{Art,Code,Data,Scenes,Settings}` + asmdef | çalışıyor |
| #51 Vertical slice | — | **alınıyor**, aşağıda |

**Ekonomi sistemi** (`Ekonomi.cs`) listede hiç yok ama bizde var.

---

## 2. Yeni ve alıyoruz

| # | Ne | Neden gerçek boşluk |
|---|---|---|
| 3 | **Terrain öznitelik katmanları** — eğim, nem, yol/su/bina uzaklığı | Bugünkü ağaç yerleşimi bu katmanlardan okumadığı için 40.765 ağaç binaların içinden bitmişti. Kusur kapatıldı ama **sebep** duruyor. |
| 4 | **Biyom kuralları** (eğim > 35° → büyük ağaç azalt vb.) | Bitki dağılımı bugün kurala değil filtreye dayanıyor |
| 6 | **Bina gerçekçilik katmanları**: kir, yaşlanma, prop | Bugün yalnız geometri + malzeme var (2/5 katman) |
| 10 | **İnsan DNA'sı** (yaş, kilo, kas, yüz, ten, sakal, kıyafet) | Bugün tek gövde var; NPC'ler döndüğünde hepsi aynı olur |
| 15 | **Ayak IK** | Yokuşta ayak zemine gömülüyor; İstanbul'un tamamı yokuş |
| 25–26 | **Hava sistemi + ıslaklık** | Hiç yok. `HavaProfili` uçuş rüzgârıdır, hava durumu değil |
| 27 | **Su shader'ı** (yansıma, kırılma, köpük, derinlik) | Haliç ve Boğaz oyunun yarısı; bugün düz yüzey |
| 28–29 | **VFX** (toz, duman, ocak dumanı, kuş) | Yok |
| 35–36 | **Ortam sesi** (biyom + saat + hava) | Hiç ses yok |
| 37 | **`IEtkilesim` arayüzü** | Kapı, sandık, NPC, merdiven hepsi ayrı yazılacaktı |
| 41 | **Save kapsamı** | Bugün dar; NPC durumu, dünya durumu, hava, kapı durumu yok |
| 6/21 | **Sinematik pas**: AO, temas gölgesi, bloom, renk derecelendirme, **APV pişirme** | ADR 0072'nin temel katmanı hiç çalışmadı |
| 51 | **Vertical slice disiplini** | Aşağıda ayrı başlık |

---

## 3. Almıyoruz — ve neden

### 3.1 URP'ye geçmek — HAYIR

Öneri: *"Ben ilk sürümde URP kullanırdım."*

Bu tavsiye donanımı ve projenin durumunu bilmeden verilmiş genel bir
tavsiyedir. Bizim ölçülmüş durumumuz:

- **ADR 0004** HDRP'yi kilitledi; proje o gün bugündür HDRP.
- Bütün `M_*` malzemeleri HDRP/Lit üzerine kurulu (30'dan fazla).
- Kalıcı ışık profili, SSGI, ProbeVolume, hacimsel sis HDRP özelliği.
- Hedef makine **RTX 4070 Laptop** — URP'nin çözdüğü sorun (düşük
  segment donanım) bizde yok.
- Geçişin bedeli: her malzemenin yeniden yazımı, ışık profilinin
  yeniden kurulumu, APV'nin karşılığının olmaması.

Üstelik listenin kendi hedefi "GTA V / RDR2 hissi" — o hissi veren
şeylerin (ekran uzayı GI, hacimsel sis, hacimsel bulut, gerçek zamanlı
prob hacimleri) HDRP'de karşılığı var, URP'de ya yok ya sınırlı.

**Karar: HDRP'de kalıyoruz.** ADR 0004 geçerliliğini koruyor.

### 3.2 Blender 4.5 LTS'e inmek — HAYIR

ADR 0001 Blender 5.2 LTS'i kilitledi ve bütün `tools/blender/` hattı
onunla koşuyor. Geometry Nodes 5.2'de eksiksiz. Sürüm indirmenin
kazancı yok, bedeli bütün hattın yeniden doğrulanması.

### 3.3 MB-Lab — HAYIR

MB-Lab Blender 2.8x döneminden kalma ve **bakımı bırakılmış**. Aynı
soyun bakımı sürdürülen halkası MPFB2'dir (MakeHuman) ve **bugün**
kuruldu, entegre edildi, ilk gövde üretildi. Geriye gitmiyoruz.

### 3.4 MetaHuman — HAYIR (lisans)

Listenin kendisi de "Unity-first hedefinde Blender tabanlı tut" diyor
ama yine de "ileride değerlendirilebilir" bırakıyor. Bizde
değerlendirilemez: **CLAUDE.md'nin ticari yayın koşulu** her varlığın
ticari kullanıma açık olmasını şart koşuyor ve MetaHuman varlıkları
Unreal tabanlı ürünlere bağlıdır. Steam'de Unity ile yayınlanacak bir
oyunda kullanılamaz. Kapalı konu.

### 3.5 Araç sistemi (#34), savaş (#37), ragdoll (#32), yıkım (#33)

Oyun türü uymuyor. 1632 İstanbul'unda uçan bir mucit anlatıyoruz;
araç fiziği, ateşli silah ve yıkım sistemi bu oyunun parçası değil.
`AranmaSistemi` ve `Ihlal` zaten "yakalanma" gerilimini araçsız
karşılıyor.

### 3.6 ECS / Entities (#44)

Listenin kendi tavsiyesine katılıyorum: şimdi değil. Kalabalık
`NPCYonetici`'nin mesafe bütçesiyle 16,7 ms altında kalıyor.

### 3.7 Klasör yapısını değiştirmek (#47)

Öneri `Assets/_Game/`, `Assets/Art/` gibi kök klasörler. Bizde
**CLAUDE.md kuralı** var: *"Assets/_Project dışına dosya koyma."*
İçerik ayrımı zaten var (`_Project/{Art,Code,Data,Scenes,Settings}`).
Kök yapıyı değiştirmek her `.meta` yolunu ve Addressables gruplarını
kırar — kazancı sıfır, bedeli yüksek.

---

## 4. Vertical slice — alıyoruz, uyarlayarak

Öneri: *"10 km² ile başlama, 500×500 m tek bölge yap."*

Bu tavsiye **yeni proje** için doğru. Bizde şehir zaten kurulu (10.868
ev, 4 semt, gerçek DEM). Geriye dönüp küçültmek yapılmış işi çöpe atmak
olur. Ama tavsiyenin **asıl değeri** ölçekte değil **kalite eşiğinde**:

> Bir bölgeyi "bitmiş" saymadan bütün bölgeleri iyileştirme.

Bu yüzden şunu benimsiyoruz:

**D_Galata referans semttir.** Yeni her katman (biyom, hava, su, ses,
ışık, iç mekân, NPC) **önce Galata'da** bitirilir ve orada ölçülür.
Ölçü kapıyı geçmeden öteki semtlere yayılmaz. Böylece her an
oynanabilir ve *bir yeri gerçekten iyi* olan bir oyun kalır.

---

## 5. Sonuç — plan yeniden sıralandı

`docs/PLAN.md` bu karara göre yeniden yazıldı. Faz sırası artık
listenin mantığını izliyor (önce temel ve render, sonra dünya, sonra
varlık fabrikası, sonra karakter/NPC, sonra oyunbilim, en son cila) ama
**bizim bitmiş işimizi tekrar etmiyor**.

Değişmeyen kurallar: 1 birim = 1 m · HistoricalTag zorunlu · çalışma
zamanında bulut LLM yok · her varlık ticari kullanıma açık ve ücretsiz ·
lisans kaydı `refs/LICENSES.md` · şüphede kal → ADR yaz.
