# HEZARFEN: 1632 — Unity + Blender Üretim Planı
**Sürüm 1.2 · Claude Code'a teslim edilmek üzere hazırlanmıştır**

> **[1.2 değişiklikleri]** Açık dünya katmanı eklendi (serbest dolaşım, yan görev ekonomisi, ases "aranma" sistemi, kayık ağı — Bölüm 11). NPC yapay zekâsı üç katmanlı mimariye bağlandı (simülasyon / offline-AI içerik / deneysel runtime LLM — Bölüm 11.3). Unity MCP seçimi iki adaylı yarışa çevrildi: resmî Unity MCP vs **MCP for Unity (CoplayDev)**; karar duman testinde (Bölüm 4.1, Görev 3).
> **[1.1 değişiklikleri]** Rol modeli: 3D modelleme, animasyon ve ışık dahil TÜM üretim Claude'dadır; Caner yönetmen/onaycıdır. MCP katmanı ve CC0 taban mesh stratejisi eklendi.

> Bu belge, Hezarfen Ahmed Çelebi'nin ana karakter olduğu, 1632 İstanbul'unda geçen 3D PC oyununun üretim planıdır. Claude Code bu belgeyi repo kökünde `docs/PLAN.md` olarak tutmalı, her fazın kabul kriterlerini karşılamadan bir sonrakine geçmemelidir. Tarihsel dayanaklar için `docs/RESEARCH.md` (araştırma raporu — Caner ekleyecek) esas alınır.


> **[2.0 · 2026-08-30]** Faz sırası **ADR 0078** ile yeniden kuruldu: aşağıdaki Bölüm 5–13 yapılan işin kaydıdır; yeni iş **Bölüm II**'den sıralanır. URP'ye geçmiyoruz, HDRP'de kalıyoruz (gerekçe ADR 0078).

---

# BÖLÜM II — ÜRETİM HATTI ANA PLANI (2026-08-30, **ADR 0078**)

> **Bu bölüm bundan sonraki faz sırasını belirler.** Aşağıdaki Bölüm
> 5–13 (Faz 0–8) **yapılan işin kaydı** olarak duruyor; oradaki fazların
> çoğu bitti ve o kayıt silinmiyor. Ama yeni iş **buradan** sıralanır.
>
> Gerekçe, neyin alındığı ve **neyin alınmadığı** (URP, Blender 4.5,
> MB-Lab, MetaHuman, araç/savaş sistemleri): `docs/decisions/0078-uretim-hatti-ana-plan.md`.

## II.0 İki değişmez kural

**1. Referans semt: `D_Galata`.** Yeni her katman önce Galata'da
bitirilir ve **orada ölçülür**. Ölçü kapıyı geçmeden öteki semtlere
yayılmaz. Sebep: "her yeri biraz iyi" yapmak, "bir yeri gerçekten iyi"
yapmaktan hem daha pahalı hem daha az inandırıcıdır. Bu, ChatGPT
listesindeki *vertical slice* fikrinin bize uyarlanmış hâlidir —
şehri küçültmüyoruz, kalite eşiğini bir yere çakıyoruz.

**2. Her özellik maliyetiyle birlikte gelir.** Bir faz, kendi
`Hezarfen → Olcum → Kare suresini bolustur` çıktısı olmadan bitmiş
sayılmaz. Kare bütçesi **16,7 ms** (60 FPS, RTX 4070 Laptop).

---

## II.A — Karakter hattı · **DEVAM EDİYOR**

Taban gövde artık MPFB2 (MakeHuman) ile parametrik üretiliyor.
Bugün kapatılan kusurlar ve ölçüleri ADR 0079'da.

| kalem | durum |
|---|---|
| MPFB2 headless taban, boy 1,70 m | ✅ |
| Erkek makro (`HEZARFEN_MAKRO`), T3 — portre iddiası yok | ✅ |
| Yön sözleşmesi (burun −Y) ayaktan ölçülüyor | ✅ |
| Kol/gövde/bacak ayrımı iki sayıyla (`kol_ayirici`) | ✅ |
| Etek konisi ölçülen alt zarftan çözülüyor | ✅ |
| Mest kalıptan (kabuk değil) | ✅ |
| Sakal kabuk + opak malzeme | ✅ |
| Giyinik üçgen ≤ 80.000 | ✅ 55.168 |
| FBX export + Unity'ye iniş + testler | ✅ |
| Mixamo locomotion klipleri → yeniden hedefleme | ✅ 20 klip (ADR 0080) |
| Oyun içi ayak kayması ölçümü | ✅ **0,05 m/s** orta duruşta (≈3 cm/basış) |
| **Ayak IK** (yokuşta ayak gömülmesin) | ✅ |

**Kapı:** boy 1,70 m ±2 cm · giyinik ≤ 80.000 üçgen · 22 Humanoid kemik ·
ayak kayması < 5 cm · yokuşta ayak boşlukta/gömülü değil.

---

## II.B — Zemin gerçeği: arazi öznitelik katmanları ve biyom

**Neden ilk sırada:** 40.765 ağacın binaların içinden bittiği kusuru
kapattık ama **sebebini** kapatmadık. Bitki yerleştirici zemin hakkında
hiçbir şey bilmiyor; her seferinde ayrı bir filtre yazıyoruz. Katman
yoksa kusur geri gelir.

Her arazi hücresinde okunabilir olacak:

```
Yukseklik · Egim · Normal · Nem · YolUzakligi · SuUzakligi · BinaUzakligi
```

Biyom kuralları bunlardan **türer**, elle yazılmaz. Örnek (ChatGPT
listesinden alınan mantık, bizim coğrafyamıza uyarlanmış):

| kural | sonuç |
|---|---|
| Eğim > 32° | büyük ağaç yok, çalı ve kaya |
| `YolUzakligi` < 4 m | ağaç yok (sokak açık kalır) |
| `BinaUzakligi` < 2 m | ağaç yok, bahçe bitkisi olabilir |
| `SuUzakligi` < 12 m | söğüt/sazlık artar |
| Nem düşük + güney bakı | servi, zeytin |

**Kapı:** bina içinde ağaç **0** ✅ · yol ekseninin 4 m'sinde ağaç **0** ✅
(1.671 ağaç elendi) · su kenarı yoğunluğu — **AÇIK**: Galata'nın kıyısı
liman, kural doğal kıyı için ve semt niteleyicisi bekliyor · Galata karesi
≤ 16,7 ms → **14,9 ms** ✅ (kalabalık dahil) · araç `AgacOznitelikDenetimi` ✅

---

## II.C — Ev çeşitliliği ve gerçekçilik katmanları

Bugün ev **iki** katman taşıyor (geometri, malzeme). Beş olacak:

| katman | ne |
|---|---|
| 1 Geometri | duvar, pencere, kapı, cumba, saçak |
| 2 Malzeme | sıva, ahşap, kiremit, taş |
| 3 **Kir** | alt kısım, yağmur izi, pencere altı |
| 4 **Yaşlanma** | boya dökülmesi, çatlak, yosun, ahşap gri |
| 5 **Prop** | kepenk, saksı, asma, çamaşır ipi, tabela, kandil |

Varyant üreteci: 26 → **~200**, tohumdan; taban alanı dağılımı
RESEARCH §4.1'den (36–715 zira², %80'i 172 m² altında). Yeni tipler:
dükkân üstü konut, konak, köşe evi, gayrimüslim varyantı.

Örnek başına değişim mesh çoğaltmadan: `MaterialPropertyBlock`.

**Kapı:** varyant ≥ 150 → **201** ✅ · yan yana özdeş çift **0** ✅
(en yakın komşusu aynı varyant olan ev: 0/2.651) · örnek başına ton
değişimi ✅ (`EvTonu`) · kir/yaşlanma/prop katmanları **⬜** ·
kare ≤ 16,7 ms → **14,9 ms** ✅.

---

## II.D — Girilebilir iç mekân

Kesintisiz geçiş (kapıyı aç gir). Sırasıyla: gerçek kapı boşluğu ve
kanadı → iç kabuk (zemin, tavan, bölme) → merdiven → tohumdan iç plan
(ortalama **4,12** oda, hayat merkezde, harem–selamlık) → dönem
mobilyası (sedir, minder, sandık, rahle, mangal, kilim, yüklük, ocak).

**Kapı:** ölçülen evlerin ≥ %95'i girilebilir → **%97,0** ✅
(332 örnek, `EvErisimi`) · iç kabuk: kat döşemesi ✅, **bölme duvarları ✅**
(zemin katta erişilen hacim **%100,0**), merdiven **⚠️ AÇIK** — geometri
**merdiven ✅** — üst kata çıkılabilen **%75,6**, Unity NavMesh ile
ölçüldü (ADR 0081) ·
tohumdan iç plan **⬜** · dönem mobilyası **⬜** · determinizm testi **⬜** ·
40 m'de kare ≤ 16,7 ms **⬜**.

---

## II.E — Su, hava ve ortam sesi

Haliç ve Boğaz oyunun yarısı; bugün ikisi de düz yüzey ve oyun sessiz.

- **Su:** yansıma, kırılma, derinlik solması, köpük, kıyı çizgisinde
  kara–su geçişi, dalga.
- **Hava:** açık / bulutlu / kapalı / yağmur / fırtına / sis / rüzgâr.
  Yağmur yalnız parçacık değil: **ıslaklık** → pürüzlülük ↓, yansıma ↑.
- **Ses:** biyom + saat + havaya göre ortam. Ezan ve namaz vakitleri
  `VakitHesabi` ile zaten bağlı; ses o iskelete oturur.

**Kapı:** Galata'da yağmurda ıslaklık ölçülebilir (pürüzlülük farkı) ·
ses kaynakları 3B ve mesafeyle sönümlü · kare ≤ 16,7 ms.

---

## II.F — Sinematik pas

ADR 0072'nin **hiç çalışmamış** temel katmanı burada kapanır.

- **APV pişirme** — *durum 2026-09-04:* mimari kuruldu ve **D_Galata
  pişti** (ADR 0087). Yol açık: prob hacimleri semt boyunda, açık suya
  hacim konmuyor, aralık 6 m, örnek 16, arka uç CPU, karma kip
  `IndirectOnly`. Ve asıl kusur bulundu — **fırında ışık yoksa prob da
  yok**: gökyüzünü beş kat parlatmak `CellData`yı iki desende bıraktı,
  güneşi `Mixed` yapmak 12.106 desene çıkardı.
  **Kalan:** öteki beş semt (her biri ~1-2 saat) ve gölge ölçümünün
  kapalı karelerde 0,26-0,30 ailesine katılması
  (`tools/olcum/golge_orani.py`).
- Ortam örtme (AO), temas gölgesi
- Bloom, renk derecelendirme (LUT), vinyet
- VFX: ocak dumanı, toz, kuş sürüsü, deniz serpintisi

**Kapı:** `renders/tur/` altına gündüz/gün batımı/gece üç kare ·
APV verisi diskte · kare ≤ 16,7 ms.

---

## II.G — İnsan DNA'sı ve kalabalığın dönüşü

ADR 0077 ile kapatılan kalabalık buradan geri gelir — ama tek gövdeyle
değil. **İnsan DNA'sı**:

```
Yas · Cinsiyet · Boy · Kilo · Kas · YuzBicimi · TenTonu
Sac · SacRengi · Sakal · Kiyafet · Aksesuar · Meslek
```

MPFB2 makro sistemi bunu doğrudan karşılıyor (`HEZARFEN_MAKRO`
deseninde). Kıyafet zaten gövdeden türüyor, yani DNA değişince kıyafet
kendini yeniden kurar — hat bunun için tasarlandı.

Üstüne **Utility AI**, var olan `Rutin` / `NPCMeslek` / `SehirGunu`
iskeletine eklenir: ihtiyaç (açlık, uyku, sosyal, güvenlik) → puan →
davranış. Kalabalık LOD zaten `NPCYonetici`'de.

**Kapı:** 200 NPC'de özdeş çift **0** · kare ≤ 16,7 ms · oturma
farkı 0,00 m (bugünkü ölçü korunur).

---

## II.H — Etkileşim, envanter, diyalog, kayıt

- `IEtkilesim` arayüzü: kapı, sandık, NPC, merdiven, kayık, kandil
  aynı yolu kullanır.
- Envanter (ScriptableObject yapılandırma).
- Diyalog: bark'tan seçimli konuşmaya; **çalışma zamanında bulut LLM
  yok**, içerik çevrimdışı üretilir ve statik gemiye konur.
- Kayıt kapsamı genişler: oyuncu, envanter, görev, NPC durumu, dünya
  durumu, hava, zaman, kapı durumu, yıkılmış/alınmış nesneler.

**Kapı:** kaydet–yükle turunda ölçülen dünya durumu birebir aynı.

---

## II.I — Cila ve Steam

LOD, culling, bellek, çökme testi, build hattı, Steam bütünleşmesi.
`refs/LICENSES.md` ↔ `Krediler` testi yeşil (bugün kuruldu).

---

## II.J — Sıra ve büyüklük (dürüst tahmin)

A en küçüğü, D en büyüğü. D tek başına bu projedeki en büyük özellik:
10.868 eve kesintisiz, tohumdan, mobilyalı iç mekân. Her fazın sonunda
**oynanabilir** bir şey bırakılır; bir faz yarım kalırsa oyun bozulmaz.

---

## 0. Proje Özeti ve Tasarım Direkleri

**Tek cümle:** Oyuncu, 1632 İstanbul'unda Hezarfen Ahmed Çelebi olarak açık dünya İstanbul'da yaşar — sokaklarda dolaşır, esnafla iş tutar, akçe ve itibar kazanır, kanat aygıtını geliştirir, Okmeydanı'nda talim eder — ve final olarak Galata Kulesi'nden Üsküdar Doğancılar'a tarihi süzülüşü gerçekleştirir.

**Tür:** Tarihî açık dünya aksiyon-macera (şiddetsiz; kovalamaca-saklanma-uçuş odaklı).

**Tasarım direkleri (her karar bunlara vurulur):**
1. **Rüzgârı hissettir.** Uçuş, oyunun kalbidir; yürüme/tırmanma ona hizmet eder. Lodos bir hava durumu değil, bir oynanış sistemidir.
2. **Şehir yaşar.** İstanbul bir arka plan değil, rutinleriyle işleyen bir açık dünyadır: NPC'ler ezan vakti ve çarşı saatine göre yaşar, oyuncu istediği an ana hikâyeden kopup dolaşır, iş tutar, başı derde girer.
3. **Landmark-doğru, doku-makul, efsane-şeffaf.** Üç katmanlı tarihsel doğruluk doktrini (Bölüm 2).
4. **Önce oynanış, sonra sanat.** Hiçbir sanat varlığı, graybox'ta eğlenceli olduğu kanıtlanmamış bir mekanik için üretilmez.
5. **Dilim önce, şehir sonra.** Sistemler (ekonomi, görev, aranma, NPC rutini) küreseldir ama İÇERİK semt kapılıdır: önce "uçuş ekseni" (Okmeydanı → Galata → Boğaz → Üsküdar), suriçi sonra. GTA hissi haritanın büyüklüğünden değil, doluluğundan gelir — "az semt, dolu semt."

**Hedef platform:** PC (Windows), tek oyunculu. Steam entegrasyonu **en son fazda** (önce lokal geliştirme ve test).

**Geliştirme ortamı:** Windows 11. Araçlar: Unity (motor), Blender (3D üretim), Claude Code (uygulama; Blender ve Unity'ye MCP köprüleriyle bağlı), Git + Git LFS (sürüm kontrol), Python 3.11+ (GIS/pipeline araçları).

**Rol dağılımı:**
- **Claude (Code):** Tüm üretim — kod, 3D modelleme, doku, rig, animasyon, ışıklandırma, prosedürel sistemler, NPC içerikleri, testler, dokümantasyon.
- **Caner:** Kurulumlar/hesaplar/lisans onayları; sanat yönetmenliği (inceleme paketlerine yazılı geri bildirim); oynanış hissi kararları; faz kapısı onayları. **Üretim araçlarına dokunmaz.**

---

## 1. Teknoloji Kararları ve Gerekçeleri

| Karar | Seçim | Gerekçe / Not |
|---|---|---|
| Motor | **Unity 6 (6000.x) en güncel LTS** | GPU Resident Drawer (kalabalık şehir için draw-call rahatlığı), HDRP Water System (Boğaz/Haliç için büyük gerçekçilik kazancı). Sürümü `ProjectVersion.txt`te sabitle, asla ara sürüm atlama. |
| Render pipeline | **HDRP** (karar kapısı: Faz 1 sonu) | "Gerçekçi" hedefi + PC-only hedef HDRP'yi haklı çıkarır. Faz 1 sonunda FPS bütçesi tutmuyorsa veya karmaşıklık üretimi yavaşlatıyorsa **URP'ye düşme kararı** verilecek (tek yönlü kapı — geç kalma). |
| 3D üretim | **Blender güncel LTS (4.5 LTS veya üstü)** | `bpy` Python API ile hem headless (arayüzsüz) hem MCP üzerinden canlı oturumda çalıştırılabilir. |
| Blender köprüsü | **blender-mcp (ahujasid)** — Blender eklentisi + MCP sunucusu | Claude canlı Blender oturumunu yönetir, sahneyi sorgular, Python çalıştırır; Poly Haven varlık entegrasyonu hazır (CC0 doku stratejimizle birebir). Ayrıntı: Bölüm 4.1. |
| Unity köprüsü | **İki aday — duman testinde seçilir (ADR):** (1) resmî Unity MCP Server, (2) **MCP for Unity (CoplayDev)** | Ayrıntı ve karşılaştırma: Bölüm 4.1. Karar `docs/decisions/0002-mcp-smoke.md`e. |
| Değişim formatı | **FBX** (deterministik export scripti ile) | Unity'nin yerleşik FBX importeri en sorunsuz yol. Export ayarları elle değil, her zaman `tools/blender/export_fbx.py` üzerinden — böylece ölçek/eksen hataları imkânsızlaşır. |
| Ölçek/eksen | **1 birim = 1 metre** (iki tarafta da) | Blender: Metric, Unit Scale 1.0. FBX exportta transform bake. Unity'de import testiyle otomatik doğrulanır. |
| Girdi | Unity **Input System** (yeni) | Gamepad + klavye/fare çift destek baştan. |
| Kamera | **Cinemachine 3.x** | Uçuş kamerası (FOV/rüzgâr sarsıntısı) ve yürüyüş kamerası ayrı sanal kameralar. |
| Sürüm kontrol | **Git + Git LFS** | `.blend`, `.fbx`, `.png`, `.exr`, `.wav` LFS'te. Unity için standart `.gitignore`. |
| Doku/HDRI | **Poly Haven (CC0)** + kendi bake'lerimiz | CC0 = ticari oyunda atıfsız kullanım serbest. Ahşap, kireç sıva, taş, kurşun (kubbe!), alaturka kiremit dokuları buradan. |
| Karakter taban geometrisi | **Blender Studio "Human Base Meshes" (CC0)** | Yüz/vücut sıfırdan sculpt edilmez; CC0 taban mesh oranlanır, dönem kıyafeti üstüne modellenir. NPC gövde varyantları da aynı tabandan türetilir. |
| Claude Code | Resmî kurulum ve yapılandırma: https://docs.claude.com/en/docs/claude-code/overview · MCP yapılandırması: https://docs.claude.com/en/docs/claude-code/mcp | Çalışma modeli Bölüm 4'te. Sürüm/kurulum ayrıntıları için her zaman resmî dokümana bak. |

**Gerçekçilik hakkında dürüst not:** Tüm üretim Claude'da olduğuna göre beklentiyi net koyalım. Gerçekçiliğin en büyük payı **ışık + malzemeden** gelir (HDRP aydınlatma, CC0 PBR dokular, HDRI gökyüzü); geometri tarafında parametrik/modüler üretim + render-geri-besleme iterasyonu + hazır CC0 taban geometriler kullanılır. Hero landmark'larda hedef, portre-fotogerçekçilik değil **"stilize-gerçekçi / dönem gravürü ruhu"**dur: tutarlı, atmosferik, tarihsel olarak doğru. Bir varlık referansa yakınsamıyorsa çözüm iterasyon bütçesini artırmak, yüksek-poli→normal bake hattına geçmek veya CC0 kaynak geometri bulmaktır (Bölüm 16).

---

## 2. Tarihsel Doğruluk Doktrini (üç katman)

Her sahne öğesi şu üç etiketten birini taşır (ScriptableObject alanı olarak da tutulur; kodeks UI'ında oyuncuya gösterilebilir):

- **T1 — Belgeli:** Konumu ve biçimi kaynaklarla desteklenen öğeler. Örnekler ve 1632 durumları (ayrıntı `docs/RESEARCH.md`):
  - **Yeni Cami YOK — "Zulmiyye" harabesi VAR:** Eminönü'nde pencere üstü hizasına kadar yükselmiş, terk edilmiş kâgir yarım yapı; çevresi sıkışık mahalle. Oyunun en çarpıcı "yokluk" görselidir.
  - **Topkapı'da Revan (1635-36) ve Bağdat (1639) köşkleri YOK.** 1635 sonrası zaman atlamalarında Revan Köşkü inşaatı gösterilebilir.
  - **Mısır Çarşısı YOK** (1660'lar), **Nuruosmaniye YOK** (18. yy), **Büyük Yeni Han YOK** (18. yy).
  - **Galata Kulesi bugünkünden farklı:** tersane ambarı/zindan işlevli, sivri kurşun külahlı, üst yapısı 1794/1875 sonrası hâlinden farklı; henüz yangın kulesi değil.
  - **Haliç'te köprü YOK** — tüm geçiş kayıkla (açık dünya ulaşımının da temeli: Bölüm 11.1).
  - **Kıyı çizgisi farklı:** Eminönü/Sirkeci dolguları yok; Langa (Vlanga) bostan; Yeni Cami harabesi neredeyse denize yakın.
  - Ayakta olanlar: Ayasofya, Süleymaniye, Sultanahmet (15 yıllık), Beyazıt, Şehzade, **Fatih Camii'nin 1766 öncesi ÖZGÜN hali**, Kapalıçarşı bedestenleri, kara surları, Yedikule, Kız Kulesi, Üsküdar Mihrimah, Rumeli/Anadolu Hisarları.
  - Zaman dokusu: kahvehaneler **1632'de açık, Eylül 1633'ten sonra yasak/yıkık**; 1633 sonrası gece fenersiz sokağa çıkma yasak; tulumba YOK — yangın söndürme = yıkıcılar + su zinciri.
- **T2 — Makul rekonstrüksiyon:** Ev-ev kaydı bulunmayan konut dokusu, sokak örüntüsü, renkler, donatılar. Kurallarla üretilir (Bölüm 8): ahşap karkas, cumba/çıkma, kafes pencere, geniş saçak, alaturka kiremit, aşı boyası tonları; dar organik sokaklar, bol çıkmaz; mezarlık-servi kütleleri, bostanlar.
- **T3 — Efsane:** Uçuşun kendisi ve Lagari'nin roketi **tek kaynaklıdır (Evliya Çelebi)**. Oyun bunu saklamaz: kodeks girdisi "Bu olayın tek kaynağı Seyahatnâme'dir" der. Bu dürüstlük, oyunun kimliğidir.

**Uçuş fiziği gerçeği:** Anlatılan süzülüş ~55:1 süzülme oranı gerektirir (modern planör sınıfı); kartal kanatlı bir aygıt için imkânsız. **Tasarım cevabı:** Oyun "efsaneyi oynatır" — rüzgâr akıntıları, yamaç yükselticileri (Galata sırtı), su üstü termikleri gibi *okunabilir ve ustalık isteyen* rüzgâr sistemleriyle mesafe kapatılır. Yani gerçekçilik iddiası fizik sabitlerinde değil, **rüzgârın davranışının tutarlılığında**dır.

---

## 3. Depo Yapısı

```
hezarfen-1632/
├─ CLAUDE.md                  # Claude Code çalışma sözleşmesi (Bölüm 4'teki şablon)
├─ .mcp.json                  # Proje kapsamlı MCP sunucu tanımları (blender + unity)
├─ docs/
│  ├─ PLAN.md                 # bu belge
│  ├─ RESEARCH.md             # tarihsel araştırma raporu (Caner ekler)
│  ├─ feedback/               # Caner'in inceleme notları — varlık başına 1 md
│  └─ decisions/              # ADR: her büyük karar 1 md dosyası
├─ refs/                      # SADECE kamu malı/CC0 referans görseller
│  ├─ lorck/                  # 1559 panorama levhaları (Wikimedia, kamu malı)
│  ├─ grelot/                 # 1680 gravürler (Gallica, kamu malı)
│  ├─ ralamb/                 # kıyafet albümü (LoC, kamu malı)
│  ├─ maps/                   # tarihi haritalar + kendi çizdiğimiz GeoJSON
│  └─ LICENSES.md             # her klasörün kaynağı ve lisansı — ZORUNLU
├─ art/
│  └─ blend/                  # KANONİK .blend kaynakları (LFS) — ADR 0005 ile eklendi
├─ data/                      # TÜRETİLMİŞ GIS çıktıları (heightmap vb.) — git'e girmez, ADR 0007
├─ tools/
│  ├─ blender/                # bpy jeneratörleri + export + render-preview
│  │  ├─ lib/                 # ortak modüller (hz_blender.py, ottoman_kit.py, materials.py)
│  │  ├─ gen_house.py         # parametrik Osmanlı evi
│  │  ├─ gen_landmark_*.py    # landmark blockout scriptleri
│  │  ├─ export_fbx.py        # TEK yetkili export yolu
│  │  └─ render_preview.py    # 4-açı/turntable PNG + inceleme kolajı üretici
│  ├─ gis/                    # DEM indirme, georeferans (GDAL), kıyı/footprint dönüştürücü
│  └─ content/                # offline NPC içerik üretimi (bark korpusu, diyalog varyasyonları)
├─ unity/HezarfenGame/        # Unity projesi
│  ├─ Assets/
│  │  ├─ _Project/            # bize ait her şey bunun altında
│  │  │  ├─ Art/ (Models, Materials, Textures, Prefabs — SM_/M_/T_/PF_ önekleri)
│  │  │  ├─ Code/ (Runtime, Editor, Tests)
│  │  │  ├─ Scenes/ (Boot, FlightSlice, Districts/…)
│  │  │  └─ Data/ (ScriptableObjects: WindProfiles, DistrictDefs, QuestDefs, NPCSchedules, HistoricalTags)
│  │  └─ _Import/             # Blender'dan gelen ham FBX iniş alanı
│  └─ Packages/, ProjectSettings/
└─ renders/
   └─ review/                 # inceleme paketleri: <varlık>_vN/ (git'e girmez)
```

**Adlandırma:** `SM_GalataTower_LOD0`, `SK_Hezarfen`, `M_Plaster_Worn`, `T_Plaster_Worn_BC/_N/_ORM`, `PF_House_A2`. Collider mesh: `UCX_` öneki. Commit mesajları İngilizce, kısa, emir kipi.

---

## 4. Claude Code Çalışma Modeli

**İş bölümü (kesin):**
- **Claude Code yapar:** C#, Python, bpy kodu; tüm modelleme (blockout → detay → doku → LOD); rig ve animasyon; HDRP ışıklandırma ve sahne kompozisyonu; prosedürel yerleşim; NPC davranış ve diyalog içerikleri; testler; veri dönüştürme; dokümantasyon.
- **Caner yapar (yalnızca):** yazılım kurulumları, hesap/lisans onayları, MCP bağlantı onayları (Unity Editor'deki izin diyaloğu dahil); inceleme paketlerine yazılı geri bildirim; faz kapısı onayları; tasarım kararları (Bölüm 17).
- Üretim görevi hiçbir koşulda Caner'e atanmaz. Sanat kararı belirsizse Claude inceleme paketi üretir ve yazılı not bekler.

**Geri Bildirim Protokolü (İNSAN pasının yerini alan mekanizma):**
1. Claude her varlık/ışık/sistem iterasyonu için **inceleme paketi** üretir: `renders/review/<varlık>_vN/` içinde 4-açı render, 1-2 yakın plan ve referans-görselle yan yana kolaj (tek PNG); sistemler için kısa video/GIF.
2. Caner serbest metinle not verir ("cumba %20 daha derin", "külah daha sivri", "ışık daha sıcak, gün batımına çek").
3. Claude notları `docs/feedback/<varlık>.md`e tarih + sürümle loglar, uygular, vN+1 paketini üretir.
4. Onay formatı: **"OK v3"** → varlık/ayar o sürümde kilitlenir; sonraki değişiklik yeni sürüm açar.
5. Işıklandırma için aynı döngü Unity MCP ekran yakalamalarıyla (Game view) yürür.

**Temel üretim döngüsü (Blender varlıkları için):**
1. Claude Code `refs/` altındaki referans görseli açar (görür).
2. Modeli üretir/değiştirir — hızlı iterasyonda MCP canlı oturumu, seri üretimde `gen_*.py` headless.
3. `render_preview.py` (veya MCP viewport yakalama) ile görüntü alır → **görüntüyü okur**, referansla kıyaslar.
4. Fark listesi çıkarır → düzeltir → 2-3'e döner. Yakınsayınca `export_fbx.py` → Unity `_Import/`.
5. Unity Editor scripti prefab'ı kurar (LOD Group, collider, malzeme bağlama), import testi koşar.

Bu **render-geri-besleme döngüsü**, üretimin tamamen Claude'da olmasını gerçekçi kılan tekniktir: Claude çıktısını *görerek* yineler; Caner yalnızca yönü tayin eder.

### 4.1 MCP Katmanı

**Blender — `blender-mcp` (ahujasid, topluluk; fiilî standart):**
- Mimari: Blender içine kurulan bir eklenti (addon.py, soket sunucusu) + Claude Code'un konuştuğu MCP sunucusu (`uvx blender-mcp`). Canlı sahne sorgulama, nesne/malzeme manipülasyonu, Blender içinde keyfî Python çalıştırma (`execute_blender_code`) ve Poly Haven varlık indirme araçları sunar.
- Kurulum `[İNSAN]`: addon.py'yi Blender'a ekle + `.mcp.json`a sunucuyu yaz. Windows'ta `uvx` PATH'te değilse tam yol verilir. Eklenti ve paket **birlikte** güncellenir; sürümler ADR'e yazılır.
- **Güvenlik:** `execute_blender_code` keyfî kod çalıştırır — sunucu yalnızca localhost dinler; "listen on all interfaces" seçeneği KAPALI kalır.

**Unity — iki aday, seçim duman testinde (Görev 3, karar ADR'e):**
1. **Resmî Unity MCP Server** (Unity AI paketi, beta): Project Settings → AI → Unity MCP'den etkinleştirilir; sahne hiyerarşisi, bileşen değerleri, konsol çıktısı, Editor eylemleri ve build ayarlarına erişim verir; bağlantı Editor'deki onay diyaloğuyla kurulur.
2. **MCP for Unity (CoplayDev, topluluk, MIT — Caner'in işaret ettiği repo):** Unity 2021.3 LTS → 6.x destekler; kurulum Package Manager → git URL `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity` (sürüm pinle, ör. `#v10.0.0`; alternatif OpenUPM `com.coplaydev.unity-mcp`); yapılandırma `Window → MCP for Unity → Configure All Detected Clients`; Python 3.10+ (uv) gerektirir. 47 odaklı MCP aracı: sahne/GameObject yönetimi, C# script düzenleme, varlık yönetimi, **test koşturma**, profiling ve build; ayrıca tool-groups, Roslyn script doğrulama ve çoklu Unity örneği yönlendirme gibi ileri özellikler. Olgun ve aktif (v10, 13k+ yıldız).
- **Karar kriterleri:** Claude Code ile bağlantı kararlılığı; araç kapsamı (konsol okuma, sahne düzenleme, test tetikleme); Unity 6 + HDRP uyumu; kurulum sürtünmesi. İkisi de duman testinde denenir; kazanan `.mcp.json`da kalır, kaybeden yedek olarak belgelenir.
- **Sınır:** MCP köprüleri `-batchmode`/CI'da kullanılmaz; deterministik testler ve build MCP'siz koşmak zorundadır.

**Claude Code yapılandırması:** Proje köküne `.mcp.json` (proje kapsamı) — her iki sunucu burada tanımlanır; ayrıntı ve güncel sözdizimi için resmî doküman esas alınır: https://docs.claude.com/en/docs/claude-code/mcp

**Ne zaman MCP, ne zaman headless CLI:**
- **MCP (etkileşimli laboratuvar):** tek varlık modelleme oturumu, sahne teşhisi, ışık ayarı, hata ayıklama, konsol okumalı düzeltme döngüleri.
- **Headless CLI (üretim bandı):** kit varyantlarının seri üretimi, LOD/export, CI testleri, deterministik yeniden üretim.
- **Kural:** Üretim bandı scriptte yaşar; MCP laboratuvardır. MCP oturumunda doğan her kalıcı değişiklik ya jeneratör scriptine geri taşınır ya da kanonik `.blend` olarak commit edilip `export_fbx.py` ile çıkarılır. Unity'de MCP ile yapılan sahne değişiklikleri sahne dosyasına kaydedilir ve commit edilir. "Sadece sohbette var olan" varlık/ayar yasaktır.

**`CLAUDE.md` şablonu (repo köküne aynen koy, gerektikçe güncelle):**

```markdown
# Hezarfen: 1632 — Çalışma Sözleşmesi

## Ne yapıyoruz
1632 İstanbul'unda geçen 3D açık dünya uçuş/keşif oyunu. Plan: docs/PLAN.md. Tarih: docs/RESEARCH.md.
Fazların kabul kriterleri karşılanmadan sonraki faza GEÇME.

## Rol
Tüm üretim (kod + 3D + animasyon + ışık + NPC içerikleri) bende. Caner yalnızca kurulum/onay
yapar ve yazılı geri bildirim verir. Ona üretim görevi atama; kararsızsan inceleme paketi üret,
notunu bekle. Notları docs/feedback/<varlık>.md'ye logla. Onay formatı: "OK vN".

## Araçlar
- MCP: blender (blender-mcp) + unity (duman testi kazananı: resmî Unity MCP veya MCP for Unity).
  Etkileşimli iterasyon için kullan.
- Headless Blender: `blender --background --python tools/blender/<script>.py -- <argümanlar>`
  (Windows: "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe")
- Önizleme/inceleme paketi: `... render_preview.py -- --in <blend/fbx> --out renders/review/<varlık>_vN/`
  → PNG'leri OKU, referansla kıyasla, düzelt, tekrar üret. (Temel döngü budur.)
- Export: SADECE tools/blender/export_fbx.py. Elle export yasak.
- Unity testleri: `Unity.exe -batchmode -projectPath unity/HezarfenGame -runTests -testResults results.xml -quit`
  (MCP'siz geçmek zorunda.)
- Unity build (Faz 7+): `-batchmode -executeMethod BuildPipelineEntry.BuildWindows -quit`

## Kurallar
- 1 birim = 1 metre. Eksen/ölçek doğrulaması Editor testiyle zorunlu.
- MCP oturumunda doğan kalıcı değişiklik ya scripte taşınır ya kanonik .blend/sahne olarak
  commit edilir. Sadece sohbette var olan varlık yasak.
- Assets/_Project dışına dosya koyma; _Import sadece iniş alanı.
- Her yeni sahne öğesine HistoricalTag (T1/T2/T3) ata; T1 için RESEARCH.md'den kaynak satırı yaz.
- Çalışma zamanında bulut LLM çağrısı YOK (v1.0). NPC içerikleri offline üretilir ve statik gemiye konur.
- refs/ altına lisansı LICENSES.md'de belgelenmemiş HİÇBİR görsel indirme.
- Şüphede kal → docs/decisions/ altına kısa ADR yaz, iki seçenek + öneri sun, Caner'e sor.

## Tanım: Bitti (Definition of Done)
Kod: derleniyor + testler yeşil + sahnede çalışır demo. Model: FBX importlu, ölçek testi
geçmiş, LOD'lu, prefab'lı, HistoricalTag'li, inceleme paketi renders/review/ altında ve
Caner onayı ("OK vN") docs/feedback/'te kayıtlı.
```

---

## 5. Faz 0 — Uçuş Prototipi (Graybox) · *tahmini 1–2 hafta*

**Amaç:** Sanat sıfırken uçuşun eğlenceli olduğunu kanıtlamak. Bu faz başarısızsa sanat üretimine hiç girilmez, mekanik yeniden tasarlanır.

**İçerik:**
- Düz zemin + ~100 m silindir "kule" + 3,4 km ötede hedef platform ("Doğancılar"). Gerçek DEM henüz yok.
- `GlideController` v0: basitleştirilmiş aerodinamik — `L = ½ρv²S·CL(α)`, `D = ½ρv²S·CD(α)`; ağırlık aktarımıyla pitch/roll; stall davranışı (α eşiği aşılınca burun düşer); tüm katsayılar `WindTuning` ScriptableObject'inde (kod değişmeden ayar).
- `WindField` v0: global lodos vektörü + elle yerleştirilen yükseltici hacimler (yamaç/termik). Rüzgâr **görünür** olmalı: partikül şeritleri + ses + kumaş sesi şiddet ipuçları.
- Kamera: Cinemachine — hıza bağlı FOV, rüzgârda hafif sarsıntı.
- Kaza/iniş koşulları: dikey hız + açı eşiği; suya iniş = yüzme yok, görev başa.

**Kabul kriterleri:**
- Kule→hedef uçuşu, rüzgâr akıntılarını aktif kullanan oyuncuyla 90–150 sn sürüyor ve **en az 3 farklı rota** ile tamamlanabiliyor.
- Rüzgârsız düz süzülmede etkin oran 8–12:1 (asılı planör inandırıcılığı); akıntılarla "efsane mesafesi" kapanıyor.
- Caner'in öznel onayı: "10 kez üst üste uçtum, hâlâ zevkli."

---

## 6. Faz 1 — Coğrafya ve Şehir İskeleti · *tahmini 2–3 hafta*

1. **DEM → arazi:** Copernicus GLO-30 (veya ALOS AW3D30) DEM'i indir (Python: `rasterio`), İstanbul bbox'ını kes (~28.90–29.08 D, 40.98–41.06 K), 16-bit heightmap'e çevir, Unity Terrain'e al. 30 m çözünürlük tepe silüetleri için yeter; kıyı ve oynanış alanları script + geri bildirimle rafine edilir.
2. **1632 kıyı çizgisi:** Modern kıyıdan başla (OSM), `docs/RESEARCH.md` kaynaklarına göre düzelt: Eminönü/Sirkeci/Unkapanı dolgularını geri al, Langa'yı bostan yap, Haliç kıyısını daralt. Çıktı: `refs/maps/coastline_1632.geojson` (kendi çizimimiz = kendi telifimiz).
3. **Georeferans ve footprint'ler:** Claude, GDAL scriptleriyle tarihi harita rasterlarını kontrol noktalarına oturtur (Müller-Wiener planı yalnızca **başvuru**; birebir kopyalanmaz — telifli). Bugün ayakta olan anıtların ayak izleri OSM'den (ODbL — oyun içi "Krediler" ekranında atıf zorunlu). Kaybolmuş yapılar script yardımıyla sayısallaştırılır; Caner hizalamayı yalnızca overlay PNG'ler üzerinden onaylar. Çıktı: `landmarks_1632.geojson`, `walls_1632.geojson`, `districts.geojson`.
4. **Unity importer:** GeoJSON → yerel metre koordinatı (orijin: Galata Kulesi tabanı) → Editor scripti sahneye marker/spline/bölge olarak basar.
5. **HDRP Water:** Boğaz + Haliç su yüzeyi; lodosla dalga durumu değişir.
6. **Bölge yayını (streaming) iskeleti:** Addressables + semt başına sahne (Galata, Suriçi-Doğu, Suriçi-Batı, Üsküdar, Haliç, sonra Eyüp). Uzak semtler için geçici düşük-poli "silüet kütleleri". Açık dünya için kesintisiz yükleme hedefi: semtler arası geçişte yükleme ekranı YOK.

7. **Arazi örtüsü:** ✅ **YAPILDI** (2026-08-21, ADR 0024). Madde 1'de arazi doğru ölçekte geldi ama `TerrainLit` malzemesine **hiç katman atanmadı** — zemin tek düz bir yüzeydi ve bu, ADR 0023'te ışık gelene kadar görünmedi. Dört prosedürel katman (toprak / ot+maki / kaya / kıyı — 4 katman = 1 splat dokusu, beşincisi belleği ikiye katlar); dağılım **kot + eğim + arazi eğriliği**nden türetilir, eğim eşikleri arazinin kendi yüzdeliklerinden çıkar (sabit açı yazılamaz: 7,5 m örnekli DEM'de karanın %99'u 24°'nin altındadır). Ayrıntı enerjisi 0,45 → 3,75. Menü: **Hezarfen → GIS → Arazi ortusunu kur** / **… inceleme paketi**.

8. **Mevsim ve güneş:** ✅ **YAPILDI** (2026-08-21, ADR 0025). Mevsim **ilkbahar** (Caner kararı); gerekçe iç tutarlılık — birinci tasarım direği lodostur ve lodos yılın soğuk yarısının rüzgârıdır. Güneş artık elle döndürülmüyor, **tarih ve saatten hesaplanıyor** (`SunPlacement`: 1 Mayıs, güneş saati 15:00 → yükseklik 43,2°, azimut 249,6°). Saat öğleden sonra, çünkü uçuş **doğuya**: sabah güneşi bütün uçuş boyunca oyuncunun gözüne gelirdi. Bir test güneşin bu enlemde **mümkün** bir yerde olmasını kilitliyor — önceki güneş 25° azimuttaydı ve orada güneş hiç bulunmaz.

**Kabul:** Gerçek topoğrafyada Galata sırtından kalkıp Üsküdar'a inilebiliyor; kıyı çizgisi 1632 düzeltmeleriyle; FPS hedefi (ör. 1080p'de 60) graybox'ta tutuyor. **HDRP/URP karar kapısı burada.**

> **Açık kalan — Faz 1c olarak sıraya girdi:** arazi artık dokulu ama **bitki örtüsü yok** — ne servi kütlesi, ne bahçe, ne bostan, ne ağaçlık. Unity'nin arazi ağaç/detay sistemi hiç kullanılmadı ve manzaranın çıplak görünmesinin asıl sebebi budur (ADR 0024 §10). Bkz. §6.1.

---

## 6.1 Faz 1c — Bitki örtüsü · ✅ **YAPILDI** (2026-08-21, ADR 0026; sınırlar toptan ele alındı 2026-08-23, **ADR 0029**) · *performans doğrulaması AÇIK*

> **2026-08-23 — sınırların dayanağı.** On bir alanın hepsi aynı şekilde
> çizilmişti ve tek sınandıkları yerde biri **yarı yarıya küçük**
> (Okmeydanı), bir başkası **altı kat büyük** (Galata) çıktı. Sınırlar artık
> çizilmiyor, türetiliyor: sur içi ve Galata sınırı **surun kendisi**,
> Yedikule şeridi sur çizgisinden, Kağıthane **DEM'den izlenen vadi tabanı**.
> Her alan `basis` taşır (`documented` / `walls` / `terrain` / `drawn`) ve
> arazi iddiaları ölçülür.

> **Neden ayrı bir madde:** arazi örtüsü turu bitince manzaranın hâlâ çıplak
> göründüğü ölçüldü ve sebebin doku olmadığı anlaşıldı — üstünde hiçbir şey
> yok. RESEARCH.md §4 ise İstanbul'u **yeşil kütleleriyle** anlatır: servi
> mezarlıkları, mesireler, bostanlar. Yeşil doku bir süs değil, şehrin
> siluetinin parçasıdır.

1. **Ağaç prototipleri:** `PF_Servi_A/B/C` ve `PF_Cinar_A/B` zaten üretildi ve LOD'lu. Unity Terrain'in ağaç sistemine prototip olarak bağlanır (uzakta billboard). Yeni model gerekmez; gereken şey **yerleştirme kuralı**.
2. **Kural — örtüden ve arazi verisinden türer** (arazi örtüsüyle aynı ilke): ağaç yalnız `Grass` katmanının baskın olduğu yerde; `Rock`, `Shore` ve deniz tabanında **yok**; yoğunluk eğimle azalır; sırt (dışbükey) seyrek, çukur (içbükey) sık — su orada durur.
3. **Adlı yeşil alanlar — YENİ VERİ GEREKİR:** mezarlıklar (Karacaahmet, Eyüp), mesireler (Kağıthane, Göksu), bostanlar (Langa, Yedikule). Kaynak **niteliksel**, o yüzden CLAUDE.md kuralı geçerli: metrik geometri UYDURULMAZ — kaba kutu + `tier: T2` + `status: draft` olarak `refs/maps/greenery_1632.geojson`'a çizilir ve **Caner'e sorulur**. Servi kütlesi mezarlığa, meyve ağacı bostana bağlanır.
4. **Ölçüm ve bütçe:** ağaç sayısı FPS'i belirler. Kabul ölçütü sayı değil **kare süresi**: RTX 4070 Laptop / 8 GB VRAM'de 1080p'de 60 FPS hedefi bozulmamalı (plan Bölüm 16 riski). Ölçüm mevcut benchmark sahnesiyle.

**Kabul:** Kuşbakışı karede yeşil kütleler siluete katılıyor ✅; mezarlık servi ormanı olarak okunuyor ✅; inceleme paketi ✅ (`Captures/yesil_*.png`); Caner "OK vN" **bekliyor**; **FPS hedefi DOĞRULANMADI** — editör render'ı kararlı bir ölçüm ortamı değil (saçılma farkın on katı) ve gerçek yargı bir oyuncu yapısı ister; batchmode build bu makinede bloklu. ADR 0026 §6.

> **Araştırma sonucu (ADR 0026 §2, RESEARCH.md §4.5):** kaynaklar bu alanların **varlığını ve yerini** belgeliyor, **dönüm ölçüsünü vermiyor** — kayıtlar kira geliri ve adet tutar. Sınırlar bu yüzden kaba kutudur ve `status: draft` taşır. En keskin bulgu bir **yokluk**: II. Bayezid vakfiyesi Okmeydanı'na bağ ve bahçe yapılmasını yasaklar; orası Hezarfen'in talim alanıdır ve ağaçsız kalır — testle kilitli.

---

## 7. Faz 2 — Modüler Osmanlı Yapı Kiti (Blender/bpy) · *tahmini 3–4 hafta*

- `ottoman_kit.py`: parametrik ev üretici. Parametreler: kat sayısı (1–2 [+nadiren 3]), cephe genişliği, **cumba/çıkma tipi ve derinliği**, kafes pencere yoğunluğu, saçak derinliği, çatı (alaturka kiremit) eğimi, taş subasman yüksekliği, renk paleti (aşı kırmızısı tonları, kireç badana; gayrimüslim mahalle varyantı: daha koyu ve alçak — RESEARCH.md notuyla T2).
- Kit parçaları: duvar panoları, pencere/kapı, saçak, baca, avlu duvarı, kuyu, ahşap iskele, kayıkhane, dükkân cephesi (kepenkli), çeşme, mezar taşı + servi, sur burcu/beden modülleri.
- **Doku stratejisi:** 2–3 trim sheet + 1 atlas (Poly Haven CC0 kaynaklı, bizim düzenlememiz). Tekil dokular sadece hero varlıklarda.
- Her üretimde otomatik: LOD1/LOD2 (decimate), lightmap UV2, `UCX_` collider, base-center pivot.
- Unity tarafı `AssetPostprocessor`: `_Import/`e düşen her FBX'e ölçek/eksen doğrulaması, malzeme eşleme, LOD Group kurulumu.
- **Açık dünya notu:** Kit, iç mekân gerektirmez — kapılar/avlular kapalıdır; istisna: görev mekânları (kahvehane, dükkân içi, kule içi) elle listelenir ve ayrı üretilir. "Her kapı açılır" tuzağına girilmez.

**Kabul:** 20 parametre kombinasyonundan üretilmiş bir "Galata sokağı" test sahnesi; 100 m'lik sokak, HDRP öğle ve gün batımı ışığında inceleme paketi olarak sunulur; Caner yazılı onayı ("OK vN") ile kilitlenir.

---

## 7.1 Faz 2b — Kamusal Yapı Kiti · *tahmini 2–3 hafta* · **(Caner, 2026-08-20 — plan boşluğu)**

> **Neden eklendi:** Caner *"cami, çeşme, dükkânlar vs düşünmemiştik"* dedi.
> Kısmen haklıydı: çeşme, dükkân cephesi, kuyu, avlu duvarı, iskele, mezar taşı
> Faz 2 kit listesinde **vardı**. Ama şehri şehir yapan **kamusal yapılar**
> hiçbir yerde yoktu — en başta **mahalle mescidi**. RESEARCH.md §4.1(g) mahallenin
> mescitten dallandığını söylüyor; mescitsiz bir mahalle üretmek, dokunun
> çekirdeğini eksik bırakmaktır. Gerekçe ve kaynaklar: ADR 0017.

Bunlar **hero landmark değildir** (o Faz 3'tür); tekrarlanan, parametrik, mahalle
ölçeğinde yapılardır. Ayrım şu: landmark **tek** ve **belgelidir** (T1); kamusal
kit üyesi **çok** ve **tipolojiktir** (T2).

### Dinî ve hayrî

| Yapı | 1632 notu | Kademe |
|---|---|---|
| **Mahalle mescidi** | Tek mekân + son cemaat yeri + tek şerefeli minare. Kubbeli ya da ahşap çatılı; taşra tipolojisi dört sınıfa ayrılır (merkezî kubbeli, tek birim kubbeli, ahşap çatılı, melez). **Mahallenin çekirdeği.** | T2 |
| **Orta ölçek cami** | ✅ **ÜRETİLDİ** (`Cami_Orta`). Tek **kubbe** + revaklı (5 gözlü) son cemaat yeri + 27 m minare; mahalle mescidiyle aynı kitten, farkı örtü ve ölçek. ADR 0030 | T2 |
| **Namazgâh** | ✅ **ÜRETİLDİ** (`Namazgah_Okmeydani` minberli, `Namazgah_Kucuk` minbersiz). Seki + kıbleye dönük **mihrap taşı** + minber; minaresiz ve duvarsız. Okmeydanı'nınkinin minberi **1624–25**'te eklendi, yani 1632'de yedi yaşında. RESEARCH.md §4.6(c)(d), ADR 0027 | T2 |
| **Türbe** | ✅ **ÜRETİLDİ** (`Turbe_A` sekizgen, `Turbe_B` altıgen). Kâgir gövde + **kurşun kubbe** + her yüzde şebekeli pencere; hazîrenin ucunda, kapısı mezarlığa bakar. Mahalleye adını veren yapı. RESEARCH.md §4.3(a), ADR 0021 | T2 |
| **Tekke / zaviye** | ✅ **ÜRETİLDİ** (`Tekke_Okcular`, `Tekke_Kucuk`). Avlulu külliyecik: kubbeli mescit arkada, derviş hücreleri avlunun iki yanında, revak üç yanda. Okçular Tekkesi 1632'de **MİNARESİZDİR** (minare 1770–71). RESEARCH.md §4.6(d), ADR 0027 | T2 |
| **Menzil taşı** | ✅ **ÜRETİLDİ** (`MenzilTasi_Ayak`, `_Bas`, `_Buyuk`). Tek parça **mermer** sütun; ayak taşı ile baş taşı arasındaki mesafe atışın kendisidir ve taşın üstünde **gez** olarak yazar. Menzilin **yönünü havası (rüzgârı) belirler**: ok azimutu = rüzgârın geldiği azimut + 180°. Beş menzil belgeli; meydan rekoru Arkurı, 1281,5 gez. RESEARCH.md §4.6(b)(b2)(b3), **ADR 0028** | T1 |
| **Sıbyan mektebi** | ✅ **ÜRETİLDİ** (`Mektep_A`). Tek oda, **kubbeli**, çeşmeli alt yapının üstünde **yükseltilmiş**; dışarıdan taş merdiven. Vakıf kurumu olduğu için yalnız müslüman mahallesine konur. RESEARCH.md §4.3(b), ADR 0021 | T2 |
| **Medrese** | ✅ **ÜRETİLDİ** (`Medrese_A` dershaneli, `Medrese_B` dershanesiz). Revaklı avlu + **tek katlı** hücre sırası + her kubbede baca; ritmi kıran tek büyük kubbe = **dershane**. Taçkapı damı aşar. RESEARCH.md §4.3(f), ADR 0022 | T2 |
| **İmaret / aşevi** | ✅ **ÜRETİLDİ** (`Imaret_A`, `Imaret_Kucuk`). Aşevi değil **mutfak tesisi**: yan yana dizilmiş kubbeli mutfak gözleri + ekmekhane (fodla) + kapılı avlu. İmza **farklı boydaki bacalar**. RESEARCH.md §4.7(a), ADR 0030 | T2 |
| **Kilise — Galata Latin** | ✅ **ÜRETİLDİ** (`Kilise_Latin_A/B`). Üç nefli bazilika, sivri kemer, **kare çan kulesi**. Galata 1453'te antlaşmayla teslim oldu, biçim korundu (San Domenico = Arap Camii). RESEARCH.md §4.2(a), ADR 0018 | T2 |
| **Kilise — suriçi/Fener** | ✅ **ÜRETİLDİ** (`Kilise_Rum_A/B`). Üç nef **tek beşik çatı** altında, **kulesiz**, sokaktan alçak. Zimmî kısıtı. RESEARCH.md §4.2(b) | T2 |
| **Sinagog** | ✅ **ÜRETİLDİ** (`Sinagog_A/B`). Balat/Hasköy: dikdörtgen salon, **kemersiz**, kadınlar mahfili ikinci pencere sırası olarak okunur; **yüksek duvarlı avlu içinde** (avlu duvarı var, Balat sahnesi yok). RESEARCH.md §4.2(c) | T2 |

### Ticarî ve üretim

| Yapı | 1632 notu | Kademe |
|---|---|---|
| **Dükkân / arasta** | ✅ **ÜRETİLDİ** (`Dukkan_A/B/C`; `Arasta_A` tonozlu, `Arasta_Acik`). Arasta dükkânların toplamı değil **tek yapıdır**: ayrı kapı yoktur, hepsi birlikte açılır. Göz genişliği **3,5 m** (Selimiye: 256 m'de 73 kemer). RESEARCH.md §4.7(b), ADR 0030 | T2 |
| **Han** | ✅ **ÜRETİLDİ** (`Han_A/B`). Avlulu, sağır dış duvar, tek taçkapı, iki kat revak, damda kubbe+baca. **Büyük Valide Han tartışmalı, Büyük Yeni Han YOK** (RESEARCH.md). ADR 0020 | T2 |
| **Hamam** | ✅ **ÜRETİLDİ** (`Hamam_A/B`). Soğukluk-ılıklık-sıcaklık + halvetler + külhan/baca; **fil gözü** aydınlatmalı kurşun kubbeler. ADR 0020 | T2 |
| **Fırın** | ✅ **ÜRETİLDİ** (`Firin_A/B`). Cepheden dükkân (kemerli açıklık + taş tezgâh + sundurma); **arkada kâgir kubbeli ocak ve kalın baca** — yapıyı fırın yapan şey odur. Her mahallede var. RESEARCH.md §4.3(e), ADR 0022 | T2 |
| **Kahvehane** | ✅ **ÜRETİLDİ** (`Kahvehane_A/B`). Ahşap cepheli oda + **geniş sundurma** + sokakta taş **seki** + ocak bacası. **1632'de AÇIK, 2 Eylül 1633 fermanından sonra yasak/yıkık** (BOA A.DVN 25/47) — oyunun tek zaman işareti; 1633 sahnesinden KALDIRILIR. RESEARCH.md §4.3(c), ADR 0021 §5 | T2 |
| **Bozahane** | ✅ **ÜRETİLDİ** (`Bozahane_A`). Oyunun **ikinci zaman işareti**: 1638 sayımında 300 bozahane var, IV. Murad döneminde **kapatıldı** — 1632'de AÇIK, kahvehane gibi. İmza arkadaki **mayalanma küpleri**. RESEARCH.md §4.7(c), ADR 0030 | T1 |
| **Değirmen** | ✅ **ÜRETİLDİ** (`Degirmen_Su`, `Degirmen_At`). Su değirmeninde ölçü **oluktadır**: 5–6 m taş oluk suyu çarka döker. At değirmeninde oluk ve çark yok, dönme direği var. RESEARCH.md §4.7(d), ADR 0030 | T2 |

### Su ve donatı

| Yapı | 1632 notu | Kademe |
|---|---|---|
| **Çeşme** | ✅ **ÜRETİLDİ** (`Cesme_A/B/C`). Duvar çeşmesi: sivri kemerli niş, ayna taşı, teknelik, kitabe, **duvar kanatları** (ADR 0019 §6) | T2 |
| **Sebil** | ✅ **ÜRETİLDİ** (`Sebil_A`). Sekizgen, **şebekeli** pencereler + her pencerede **mermer tezgâh** + çok geniş konsollu saçak + kurşun külah. Çeşmeden farkı: sudan **kendin almazsın, sana verilir**. Çekirdeğin köşesinde; vakıf kurumu olduğu için yalnız müslüman mahallesinde. RESEARCH.md §4.3(d), ADR 0022 | T2 |
| **Şadırvan** | ✅ **ÜRETİLDİ** (`Sadirvan`). Cami avlusunda, çok musluklu, çatılı | T2 |
| **Su terazisi** | ✅ **ÜRETİLDİ** (`SuTerazisi_A`, `SuTerazisi_Kisa`). Daralan kâgir kule + tepede **hazne** + iki künk. 1632'de var: Kırkçeşme (Sinan, ~1563) 55 km hat boyunca terazi taşır. **Hattın kendisi henüz çizilmedi.** RESEARCH.md §4.7(e), ADR 0030 | T2 |
| **Kuyu / sarnıç ağzı** | *(Faz 2 listesinde vardı)* | T2 |
| **Muvakkithane** | ✅ **ÜRETİLDİ** (`Muvakkithane_A`). 1632'de **vardır** (ilki Fatih Camii 1470) ama yayılması 18. yy sonu: **mahalle mescidine değil SELÂTİN camisine** aittir — kısıt varlıkta değil **yerde**. Testli. RESEARCH.md §4.7(f), ADR 0030 | T2 |

### Kabul (Faz 2b)

Mescidi merkez alan bir mahalle: mescit + şadırvan + çeşme + birkaç dükkân +
mezarlık; sokak yerleştiricisi (ADR 0016) mescidi **çekirdek** olarak kullanır
ve doku ondan dallanır. HDRP öğle ve gün batımı inceleme paketi; Caner "OK vN".

**Durum:** Sahne ✅ (`Faz2_GalataSokagi.unity` — mescit + şadırvan + çeşme + 4
dükkân + 12 taşlı duvarlı hazire + türbe + kilise + hamam/han + mektep/medrese
+ fırın + kahvehane + **bozahane**); kabul ölçütü **testle kilitli**
(`MahalleSceneTests`, üç test: listenin tamamı, çekirdeğe mesafeler, iki zaman
işareti). İnceleme paketi ✅ — `Captures/mahalle/`, 8 kadraj × 2 an, menüden
tekrar üretilebilir (**Hezarfen → GIS → Mahalle inceleme paketi**). ADR 0031.
Caner **"OK vN" bekliyor** (`docs/feedback/mahalle_sahnesi.md`, Karar 12 açık).

Paketi üretmek üç kusur buldu ve üçü de düzeltildi: kaldırımın yürünen yüzü
**tersti** (çarpıcısı fiilen yoktu — oyuncu düşerdi), göz hizası araziden
ölçülüyordu (kareler kaldırımın altında çıkıyordu), dükkân sırası dördü
deneyip **ikide** kalıyordu.

---

## 8. Faz 3 — Landmark'lar (öncelik sıralı hero varlıklar) · *tahmini 4–6 hafta, kitle paralel*

Üretim yolu her biri için: referans seti (`refs/`) → bpy blockout → render-geri-besleme döngüsü → MCP canlı oturumda detay pası (Claude; gerekirse yüksek-poli → normal bake) → doku → LOD/prefab → inceleme paketi → Caner onayı.

**S-kademe (uçuş ekseni — dikey dilim için zorunlu):**
| Yapı | 1632 doğruluk notu |
|---|---|
| Galata Kulesi | ✅ **ÜRETİLDİ** (`GalataKulesi` saçaklı, `GalataKulesi_Mazgalli`) — **D2**. Çap 16,45 m (ölçülü); kâgir gövde **34,5 m**, iki belgeli kottan türedi (1831'de yıkılan 32,60 + 1794'te alçaltılan 1,90); toplam 46,0 m, yani **bugünkü 62,59 m'den alçak**. Kurşun kaplı külah **vardı** (Evliya Çelebi) — yaygın "külahı 1832'de II. Mahmud ekledi" iddiası yanlış. Külahın BİÇİMİ D3, iki varyant. Tuğla kuşaklar 13,20 / 17,17 m; ilki 1509 onarımının dikişi. RESEARCH.md §5.1, **ADR 0033** |
| Galata surları + kapıları | ✅ **ÜRETİLDİ** (`SurBurcu`, `SurKapisi` + hat boyunca perde duvar mesh'i) — **D2, ölçülü**. Cenevizlilerce 1335–1349; **1864'e kadar eksiksiz ayakta**, yani 1632'de tam. Ölçüler **İTÜ rölövesinden** (Erdoğan 2013, dan. Ahunbay): duvar 2 m kalın / 7 m yüksek, çevre 2 800 m, alan 37 ha, hendek 15 m; burçlar **dörtgen VE U planlı** (U: 9,80×7,70/16,16 m ve 7,02×5,84/~10 m); Harup Kapı açıklığı 2,70 m, kemer 4,60 m. **Hat da belgeli**: yelpaze, tepesi kule, batıda Azapkapı, kuzeydoğuda Tophane, deniz kenarı 1632 kıyısının kendisi. Sahnede 33 burç (üç tip), 2 kapı, 2 510 m hat; ölçülen alan 30 ha (belgeli 37 — fark kendi kıyı çizgimizde). Hendek henüz yok. RESEARCH.md §5.2, **ADR 0034** |
| Okmeydanı | ✅ **KONSOLİDE EDİLDİ** — varlıklar Faz 2'de üretilmişti, artık dünyada. **Konum araziden ölçülerek 700 m düzeltildi**: eski nokta yeşil poligonun ağırlık merkeziydi ve yamaca düşüyordu (400×400 m'de **94,1 m** kot farkı) — 845,66 m'lik menzil rekoru orada atılamaz. Yeni nokta kot 94,5 m, 300×300 m'de 10,1 m, 30° yönünde 900 m'lik koridorda **5,6 m**; poligonun içinde. Gerekçe teste bağlı (`OkmeydaniHasGroundFlatEnoughForTheRecordShot`). **Okçular Tekkesi** (1624-25'te Gürcü Mehmed Paşa minber ekletti; **minaresiz** — minare 1770-71) ve **minberli namazgâh** yerleştirildi, ikisi de **kıbleye** dönük. Yerleştirici artık bütün `art/blend/*` kataloglarını tarıyor. **Menzil taşları** (132 âbide) üretildi ama dağıtılmadı → Faz 4. RESEARCH.md §5.8, **ADR 0041** |
| Sinan Paşa Köşkü / İncili Köşk (Sarayburnu) | ✅ **ÜRETİLDİ** (`IncliKosk` kubbeli, `IncliKosk_Ahsap`) — **D3, taslak**. 998-999/**1590-91**, Koca Sinan Paşa, mimar **Dâvud Ağa**; 1632'de 41 yaşında, **1871-72**'de sahil demiryolu için yıkıldı. Evliya'ya göre **IV. Murad uçuşu buradan izledi** (anlatı T3, yapı T1). **Konum 156 m düzeltildi ve ölçülerek türetildi**: kaynak 'Sarayburnu'ndan kıyı boyunca ~300 m' der, bu mesafe kendi 1632 kıyı çizgimizde ölçüldü (kot 0,1 m). Sayılan özellikler teste bağlı: Sarayburnu tarafında **1**, Ahırkapı tarafında **2** kemer; **4** köşe bacası; çift kemer arasında çeşme; camekânlı **cumba**. **Örtü tartışmalı** (TDV kubbe, Eldem ahşap) → iki varyant, seçim Caner'de. Kıyı yapıları artık **denize** bakıyor (yeni `Waterward` kuralı). RESEARCH.md §5.6, **ADR 0039** |
| Kız Kulesi | ✅ **ÜRETİLDİ** (`KizKulesi`) — **D3, taslak**. **1632'de AHŞAP**: kâgir kule + camlı köşk + kurşun kubbe 1725'tir (kule 1720'de yandı, Damat İbrahim Paşa kâgir fener kulesini yaptırdı); 1509 depreminden sonrakiler de "yine ahşap". İşlev **fener değil karakol** — zeytinyağı feneri 1718 sonrası; yatsıdan sonra ve seher vakti mehter çalar, bu yüzden tepesinde fener değil **nöbet sahanlığı** var. 1632'de YOK: kubbe, camlı köşk, fener, zincir (12. yy). Ölçülü çizim yok — tek sayısal kısıt teste bağlandı: 1725 kulesinden (~23 m) alçak; model su üstünde **20,0 m**, kayalık 26×20 m. **Adacık DEM'de yok** (ölçüldü: 150 m çevresi baştan başa −12 m) → kule su düzlemine (y=0) oturur, kayalık varlığın parçası. RESEARCH.md §5.3, **ADR 0035** |
| Üsküdar: Mihrimah (İskele) Camii + Doğancılar Meydanı | ✅ **CAMİ + KÜLLİYE + İSKELE ÜRETİLDİ** (`UskudarMihrimah`) — kubbe **D2**, gerisi türetilen **D3**. Kitabe 954/**1548**, Mimar Sinan; 1632'de 84 yaşında. Kubbe dış çap **11,40 m** / iç çap 10,00 m / kilit **24,20 m**. **Üç yarım kubbeli** planın İstanbul'daki ilk ve tek örneği — girişte yarım kubbe YOK. Çift minare, her biri **tek şerefeli**; beş kubbeli birinci revak (6 mermer sütun); **çift revak Sinan'ın özgün tipi** (beş gözlü çift revaklı yedi caminin ilki); set ~2 m. Saçak 11,69 m ölçülen kubbeden türedi. **Yön kıbleden**: ızgara kıblesi 150,40° (gerçek 151,73° − UTM yakınsaması 1,32°); 22 landmark'ta yayılım 0,198°. **1632'de YOK**: türbe/hamam/kasır/muvakkithane, güneş saati (18. yy), set çeşmesi (17. yy) — ve meydanın bugünkü iki simgesi **Yeni Valide Camii (1710)** ile **III. Ahmed Meydan Çeşmesi (1728)**. **Külliye**: medrese (**16 hücre** belgeli, caminin doğusunda) ve sıbyan mektebi (kışlık kubbeli oda + **yazlık açık eyvan**, dükkân katı üstünde, kıble tarafında) üretildi — ikisi de D3/draft. **Caminin koordinatı 164 m düzeltildi**: hatayı külliyenin kendi belgeli göreli konumları ele verdi (mektebin kıble bileşeni düzeltmeden sonra 1,00). İmaret-tabhâne ve Kurşunlu Han **yerleştirilemedi** — 1632'de ayaktalar ama yerleri bilinmiyor. ADR 0038. **İSKELE ÜRETİLDİ** (2026-08-27): 1632'de **ahşap**, kâgir rıhtımlar 19. yy'dır. Caminin adı ondan gelir — **"İskele Camii"** — yani iskele camiden bağımsız bir ayrıntı değil, **adının kaynağı**. Yönü kıyı çizgisinin yerel **normalinden** ölçüldü (306,8°); "en alçak arazi yönü" yetmez, iskele kıyıya **diktir**. ADR 0055. RESEARCH.md §5.4, §5.20, **ADR 0036**. **Doğancılar**: konum **771 m düzeltildi** (Galata'ya 3709 → **3336 m**, literatürdeki 3358 m'ye %0,7 yakın); **Çakırcıbaşı Hasan Paşa (Doğancılar) Camii** (1548 Sinan, 1580'lerde Hacı Ahmed Paşa yeniledi — kâgir duvar, **ahşap çatı**, tek minare) ve **Aziz Mahmud Hüdâyî tekke-camii** (1595; minber 1598-99) üretildi, ikisi de **D3/draft** çünkü ölçülü çizim yok (bugünküler 1857 ve 1855-56). **Hüdâyî türbesi** de yerleştirildi: **açık (baldaken) türbe**, dört mermer sütun. Tarih belgeli — Hüdâyî Safer 1038'de (Ekim 1628) öldü, türbe **aynı hicrî yıl içinde 1038'de (1628-29)** yapıldı, yani 1632'de üç-dört yaşında (Kültür Envanteri). Varlık **T1**, biçim **D3** (bugünkü kapalı kabuk, 7,40×8,80 m plan ve on üç dilimli kubbe 1272/1855-56'dır). **UÇUŞ ÖLÇÜLDÜ: 3336 m yatay / 51,7 m düşüş → gereken süzülme 64,6 : 1**, yani sakin havada imkânsız. Gerçek kanat ayarıyla ölçüldü: 11,56:1, menzil 597 m, **eksik 2739 m**. Rüzgâr tek başına çözmüyor (205 km/h gerekirdi); **gereken ortalama yükselen hava yalnızca ~0,9 m/s** — zayıf termiğin altında. Öneri: yükselen havayı mekanik yapmak (ADR 0037 Soru 1). Araç: **Hezarfen → Uçuş → Uçuş bütçesini ölç**; bekçi: süzülme oranı < 15:1. Meydanın zemini/çınarları henüz yok. RESEARCH.md §5.5, **ADR 0037** |
| Topkapı **silüeti** (uzaktan) | ✅ **BELİRLEYİCİLER + ALAY KÖŞKÜ ÜRETİLDİ** (`TopkapiAdaletKulesi`, `TopkapiBabusselam`) — **D3, taslak**, konumlar ölçülü. **Adalet Kulesi 1632'de bugünkünden ALÇAK**: üç taş kat + ahşap üst kat + kurşun piramidal külah; dördüncü taş kat ve yükseltilmiş külah **II. Mahmud (1819-20)**, bugünkü sivri külah **Abdülaziz** — Galata Kulesi'ndekiyle aynı hata ailesi. **Bâbüsselâm** çifte konik külahlı, kuleler 1632'de var (tartışma yalnızca Fatih mi Kanûnî mi — ikisi de 1632 öncesi). Siluet kuralı teste bağlı: kule kapıdan yüksek (79,4/75,1 m). Yerleştiriciye **`face_deg`** eklendi: varlık kendi belgeli yönünü bildirir (kapı güneye bakar, eğim onu batıya döndürüyordu). **Revan (1636) ve Bağdat (1639) YOK**; **ALAY KÖŞKÜ ÜRETİLDİ** (2026-08-27): 1632'de **ahşap**; bugünkü kâgir köşk **1810/1819-20** (II. Mahmud) ve o yapı **daha yüksek** bir köşkün yerine geçti — yani burada 1632 yapısı bugünkünden **ALÇAK DEĞİL, YÜKSEK**tir, Galata ve Adalet Kulesi'nin **tersi**. Kural "her şey farklıdır" değil, **farkın YÖNÜ de sorulur**. İncili Köşk'le aynı aile: duvar üstünde, taşan, padişahın seyrettiği yer. ADR 0055. Sur-ı Sultanî ve kütle denizi Faz 4'e. RESEARCH.md §5.7, §5.20, **ADR 0040** |

**A-kademe (suriçi genişlemesi) — ✅ TAMAMLANDI (2026-08-27).** Satır satır
tutuluyor; üç tur boyunca tek paragrafın içine yazıla yazıla okunmaz olmuştu.

| Yapı | Durum |
|---|---|
| **Süleymaniye** | ✅ **ÜRETİLDİ** — 1550-57 Sinan. **Burada tanıdık siluet DOĞRU**: 1557'den 1632'ye biçimi değişmedi (1660 yangını ve 1766 depremi sonra). Kubbe **26,5 m / 53 m** ölçülü, **2** yarım kubbe (ana eksende), **4 minare / 10 şerefe** (3+3+2+2), avlu + şadırvan. Tepe **124,8 m** — dünyanın en yüksek yapısı. RESEARCH.md §5.10, **ADR 0044** |
| **Ayasofya** | ✅ **ÜRETİLDİ** — kubbe **33,0 m dış / 31,87×30,86 iç**, kilit **55,60 m**, **40 kaburga / 40 pencere**. Basıklık **0,909**: Bizans kubbesi, Osmanlı 0,78 DEĞİL → **ayrı kit**, `validate` 0,78'i reddediyor. Dört minare **aynı değil** ve bunu **ölçü** söyledi: doğu çifti Ø3,6 (biri **tuğla**), batı çifti Ø4,0 (Sinan ikizleri) — üç kaynak tuğlanın köşesinde çelişiyordu, ölçü TDV'nin "güneybatı"sını eledi. **Kıbleye dönük DEĞİLDİR** (eksen 123,5°, sapma **26,9°**; mihrap apsise eğik). 1632'de yok: şadırvan (1740), I. Mustafa türbesi (1639 — vaftizhâne hâlâ **yağhâne**), Fossati'nin sıvası + kırmızı şeritleri (1847-49, bugünkü okra ondan da sonra). Fatih'in **ahşap** minaresi **1574'te sökülmüş** — kulecik var, minare yok. Kubbe 1632'de **dünyanın en büyüğü**, teste bağlı. RESEARCH.md §5.11, **ADR 0045** |
| **Yeni Cami harabesi** ("Zulmiyye") | ✅ **ÜRETİLDİ** — 1632'de cami DEĞİL, **çatısız kabuk**: 1597'de başlandı, **1603**'te ilk pencere seviyesinde durdu, 57 yıl öyle kaldı; 1660-63'te Turhan Sultan tamamlattı. Halk **"Zulmiye"** derdi. Harim **35,50 × 40,90 m** ölçülü, 4 fil ayağı, kubbesiz/minaresiz. Konum **148 m düzeltildi**; yön kıbleye alındı (mihrap duvarı 1603 öncesi örülmüştü). RESEARCH.md §5.9, **ADR 0042** |
| **Sultanahmet** | ✅ **ÜRETİLDİ** — 1609-1616, Sedefkâr Mehmed Ağa; 1632'de **on altı yaşında** ve külliyesi tamam (türbe 1619, medrese-imaret 1620). Sonradan eklenen tek şey III. Selim'in su haznesi (1802 sonrası). Kubbe açıklığı **23,50 m** — kaynaktan kopyalanmadı, **plandan çıkarıldı** (ayak eksenleri 30,75 − 2×3,65 duvar). Bir Osmanlı kubbesinin **üç** sayısı olduğu burada anlaşıldı: içten 22,40 / açıklık 23,50 / kurşun izi 27,7 (kasnak). **4** yarım kubbe, **12** eksedra, **4** fil ayağı (Ø5 m), **6** minare / **16** şerefe, avluda 26 sütun / 30 kubbe. Yarım kubbe **yarım küredir** — her biri bir büyük kemerin üstüne oturur. Zincir üçüncü kez doğrulandı (türetilen 28,97 / plandan 30). **ADR 0046'nın çıkış noktası bu yapıdır.** RESEARCH.md §5.13, **ADR 0047** |
| **Fâtih Camii** | ✅ **ÜRETİLDİ**, 1766 öncesi özgün şema — **Faz 3'ün en büyük tarihsel farkı**. 1766 depremi yapıyı yıktı ("zemine kadar"), bugünkü barok yapı **1767-71**. 1632'de: **1** yarım kubbe (mihrap yönünde, bugün 4), **2** ayak (bugün 4 fil ayağı), yanlarda **daha alçak** üçer küçük kubbe, **birer** şerefeli iki minare (bugün ikişer). Kubbe **26 m** — 1470'ten Süleymaniye'ye (1557) **87 yıl** en büyüğü. Avlunun **18 sütun / 22 kubbe / 3 kapı**'sı 1766'yı atlatan ölçülerdir (şadırvan, taçkapı, mihrap ve minare gövdeleriyle birlikte). Kilit kotu **türetildi, D3**. SALT görselleri CC BY-NC-ND: **yalnızca bakılır** — model metin kaynaklarından kuruldu. RESEARCH.md §5.14, **ADR 0048** |
| **Beyazıt + Kapalıçarşı bedestenleri** | ✅ **ÜRETİLDİ** (cami + iki bedesten). 1501-06; 1632'de 126 yaşında. **1632 burada bir AN**: şadırvanın kubbesini **IV. Murad** eklettirmiş (**1623-1640**) — oyunun yılı o aralığın tam ortası. Kubbe **konmadı** (Murad IV gerçek iktidarı 1632'de aldı), ama karar görünür kayıtlı ve bayrakla üretilebiliyor. Ölçülü: kubbe **16,78 m**, harim **37,06×36,80**, minareler arası **79 m** — ve o 79 m **kanat uzunluğunu belirliyor** (elle girilmedi). Kilit kotu yayımlanmamış: **türetildi** ve kilit/çap oranı ölçülü dört caminin bandına (1,68-2,12) düşmek zorunda — 2,09. Sayılan: 2 yarım kubbe, 4 pâye, **20 + 2×7 pencere**, 2 minare/birer şerefe, 4+4 tabhâne hücresi, 24 avlu kubbesi. RESEARCH.md §5.17, **ADR 0051**. — **BEDESTENLER**: Cevahir (45,30×29,50, **15 kubbe / 8 ayak**, kilit **14,89 m** ölçülü) ve Sandal (40×32, **20 kubbe / 12 ayak**). **Üç bağımsız sayı bir geometriyi kapatıyor**: kubbe = sütun×satır, ayak = (sütun−1)×(satır−1), ve ızgara ölçüyle de tutuyor (gözler kareye yakın). **1632'de Kapalıçarşı bugünkü değildir** — kâgir tonozlu sokaklar 1701 ve 1894 sonrasıdır; 17. yy'da aralar **ahşap** örtülüydü ve **1618 yangını** 1632'den yalnızca 14 yıl öncedir. RESEARCH.md §5.18, **ADR 0053** |
| **Kara surları + Yedikule** | ✅ **ÜRETİLDİ** (sur + 7 kapı + hisar). Theodosius surları 5. yy, **1632'de ayakta**. Kaynağın verdiği asıl sayı bir **toplamdır**: savunma derinliği **70 m**; ara ölçüler D3 ama toplamları o sayıya **oturmak zorunda** ve iki yerde denetleniyor. İç sur 12 m / **96 burç × 25 m**, dış sur 8,75 m, hendek 20×10 m. **Burç aralığı elle girilmedi**: sayılan 96, ölçülen hat (5 824 m) → **60,7 m**, ve kaynağın bağımsız verdiği 21-77 m bandına düşüyor. İki burç planı da üretildi (96'nın 19'u sekizgen). Sahnede 99 334 üçgen; 192 burcun hepsi araziye tam oturuyor. **Yedi kapı** kondu (`KaraSurKapisi`, kendi iki burcuyla; Galata'nın kapısı ne kondu ne ölçüsü alındı — orada duvar 2 m, burada 5 m). **Yedikule Hisarı** (Fatih 1457-58): **7 kule**, üçü Fatih'in dairesel kuleleri; **Altın Kapı 3 kemerli**; beşgen yarıçapı **15 000 m² belgeli alandan türedi**. Yön elle yazılmadı — sur hattının **dış normali** (261,2°). **Sur hattının güney ucu ölçümle düzeltildi**: landmark ile hat 186 m çelişiyordu, üçüncü bir ölçü landmark'ı doğruladı (186→53 m). Yan etkiyi yeşil doku testi yakaladı. RESEARCH.md §5.15-5.16, **ADR 0049**, **ADR 0050** |
| **Padişah türbeleri** | ✅ **ÜRETİLDİ** — II. Selim (1577, **Sinan**, kare köşeleri **pahlı**), III. Murad (1599, **altıgen**, revaklı, **mermer**, Osmanlı'nın en büyüklerinden), III. Mehmed (1604-08, **sekizgen**), Sultan Ahmed (1619). **Üç yapı, üç ayrı plan** — tek şablona indirmek kolaydı ve kaynaklar üçünü ayrı ayrı veriyor. Kare-pahlı plan **düzgün değildir** ve bu `face_spread` ile **ölçülüyor** (0,70 / 0,000): şeklin kendisi ölçülebilir bir sayıya bağlandı. Çift kabuk kaydedildi, iç kabuk üretilmedi. **1632'de hazirede DÖRT türbe var, beş değil**: I. Mustafa/İbrahim 1639'dur. Sultan Ahmed türbesinde **II. Osman** yatar (1622 Yedikule) — Genç Osman Kulesi ile aynı olayın iki ucu, ikisi de sahnede. RESEARCH.md §5.19, **ADR 0054** |

### 8.0 Koordinat denetimi **(2026-08-26)**

Faz 3 boyunca **beş** konum hatası çıktı (Doğancılar 771 m, Okmeydanı 700 m, Üsküdar Mihrimah 164 m, İncili Köşk 156 m, Yeni Cami 148 m) ve hiçbiri gözle görülmedi. Artık **arazi koordinatın tanığıdır**: tepeyi taçlandıran cami yerel zirveye yakın olmalı, tersane suyun kenarında, ok meydanı düz. `Hezarfen → GIS → Landmark konumlarını denetle` bunları sorar; 16 `approx` koordinattan **üçünü** ayıkladı (Yavuz Selim 150 m, Yedikule 160 m, Tersane 247 m) ve üçü de gerçekten yanlıştı. **31 landmark, 0 şikâyet.** ADR 0043.

### 8.2 1632'nin kıblesi **(2026-08-27)**

Faz 3 boyunca her cami **150,40°**'ye döndürüldü — büyük daire formülünden hesaplanmış, doğru bir sayı. **Ama 1632'nin camileri oraya bakmıyor.** Sultanahmet'in ekseni yedi bağımsız yolla **133,6°** ölçüldü; ardından **on tarihî cami** ölçüldü ve hepsi büyük daireden **doğuya** saptı (medyan **−16,6°**). Yöntem Şakirin Camii'nde (2009) **+0,04°** veriyor, yani sapmayı uydurmuyor. `QiblaDeg` **133,70°** oldu; `ModernQiblaDeg = 150,40` karşılaştırma için duruyor. Ölçülen ekseni olan yapı medyanı kullanmaz: Süleymaniye **139,0°**, Ayasofya **123,5°**. Sahnedeki yedi cami 16,7° döndü. **ADR 0046**, RESEARCH.md §5.12.

### 8.3 Yeşil ama bayat **(2026-08-27)**

`LandmarkTests`e dört test eklendi, dosyada bir **CS0102** vardı ve test assembly'si **derlenmedi** — koşum yine de **223/223 YEŞİL** döndü, çünkü Unity bir önceki sağlam assembly'yi çalıştırdı. Bu, üç kez yakalanan "atlanan test geçen test gibi görünür"den (ADR 0041, 0043, 0044) **daha kötüsüdür**: sayı bile yalan söylemiyor, 223 gerçekten koştu — yalan olan, o 223'ün *hangi kodun* 223'ü olduğu. Bekçi: `CompiledTestCountMatchesTheSource` kaynaktaki `[Test]` sayısıyla assembly'dekini karşılaştırır ve **eski assembly'de de bulunduğu için** derleme çöktüğünde çalışıp patlar. Kural: **yeşil bir koşum, derleme yeşilse yeşildir.** **ADR 0052**.

### 8.4 Ayrıntı geçişi — dilbilgisi, kopya değil **(Caner isteği, 2026-08-27)**

Caner: *"Faz 3'te üretilen modeller gerçek dünyadaki gibi detaylı olsun."*
36 anıt toplam **77 661** üçgendi — tek bir kahraman yapıdan az. Ama
fotoğrafı izleyemeyiz: Ayasofya'nın okra sıvası Fossati'nin (1847-49),
Fâtih'in bugünkü dış cephesinin tamamı 1767-71. Fotoğrafa bakıp "aynısını"
yapmak oyunu **yanlış yüzyıla** taşırdı; üstelik SALT (CC BY-NC-ND) ve
Müller-Wiener görselleri depoya giremez. Bu yüzden ayrıntı, mimarinin
**dilbilgisinden** kuruldu: ortak `tools/blender/lib/detay_kit.py` — silme,
mukarnas, kavsara, sütun, köşe ayağı, sivri/yuvarlak kemer, revak, kubbe
kaburgası, şerefe, âlem, taçkapı, konsol dizisi. **"Fotoğraftaki gibi"
değil, "fotoğraftaki dil kadar".** Kemer dili yapıya göre ayrılır: Osmanlı
**sivri**, Ceneviz (Galata) ve Bizans (sur burcu) **yuvarlak**.

Sonuç: Sultanahmet 8 282 → **102 952**, Süleymaniye 7 294 → **89 668**,
Beyazıt → 55 380, Fâtih → 48 854, Ayasofya → 39 682, bedestenler
2 418/3 168 → 16 170/20 880, türbeler 554-938 → 5 064-9 906, Galata
1 012 → 3 268. Sur burcu **192 örnek** basıldığı için bilerek 576'da
tutuldu (sınır 1 500).

Geçişin bulduğu şey bir sayı çelişkisinin çözümü oldu: TDV Sultanahmet
için *"yirmi altı sütun, otuz kubbeli birim"* der ve iki sayı çelişiyor
sanılıyordu. **Kapalı** bir revak halkasında mesnet sayısı göz sayısına
eşittir (30) ve dördü sütun değil **köşe ayağıdır** → 26. Fâtih bunu
bağımsız doğruladı (22/18, fark yine dört) ve avlusu 1471'den ayakta.
Ayrıca göz dağılımı artık elle tahmin edilmiyor: sayılan toplam kenar
uzunlukları oranında paylaştırılıyor — ilk tahminim (10/10/5/5)
Sultanahmet'in ön gözünü **13 m** yapmıştı, bir revak gözü değil bir salon.
**ADR 0056**, **ADR 0057**, RESEARCH.md §5.21-§5.22, EditMode **241/241**.

Geçişin en pahalı dersi teknikti: `hz.make_box` köşeleri **mesh'e** yazar ve
nesne dönüşümünü kimlik bırakır, bu yüzden parçayı yerine koyup **sonra**
döndürmek onu **dünya orijini** etrafında döndürür. Hatayı iki yere yazdım ve
ikisi de renderda **görüldüğü halde yanlış teşhis edildi**; sebebi bir sayı
ele verdi (Yedikule'nin ayak izi 7×13 m büyüdü). `ottoman_kit._donus_denetimi`
artık `|R·c − c| ≤ 0,35 m` şartını her birleştirmede sınıyor ve otuz üretecin
taranması **iki mevcut hata daha** buldu (Mihrimah'ın ikinci revak yan
örtüleri, su terazisinin simetrik olmayan künkleri) ile bir kırılgan yer
(değirmen çarkı göbeğinin elle telafisi). **ADR 0058**.

Bitmeyenler: **Kız Kulesi** ayrıntılanmadı (1632'de ahşap — taş oymacılığı
dili oraya ait değil); **doku çözünürlüğü**
hâlâ kütle geometrisi için ayarlı; küçük varlıklar (medrese, mektep,
tekke, köşk, iskele) dağarcığa bağlanmadı.

### 8.1 "Tamamen gerçeğe uygun" ne demek — doğruluk merdiveni **(Caner, 2026-08-20)**

Caner Ayasofya, Kapalıçarşı gibi yapıların *"tamamen gerçeğe uygun"* modellenmesini
istedi. Bu doğru hedef ama tek bir şey demiyor; **neyin ölçülebilir olduğuna** bağlı.
Her landmark için hangi basamakta olduğumuz yazılır ve inceleme paketinde belirtilir:

| Basamak | Ne demek | Neye dayanır |
|---|---|---|
| **D1 — Ölçülü** | Plan/kesit/cephe ölçüleriyle; kütle, açıklık ve oran **sayıyla** doğrulanabilir | Kamu malı ölçülü çizim |
| **D2 — Görsel** | Dönem gravürü/fotoğrafından oran çıkarımı; ölçü yok | PD gravür/panorama |
| **D3 — Tipolojik** | Yapı tipinin kurallarından; bu yapıya özgü kayıt yok | Tipoloji çalışmaları |

> ⚠️ **Telif kapısı — bu bir üretim kısıtıdır, ayrıntı değil.** CLAUDE.md:
> Müller-Wiener planları **telifli** (yalnızca bakılır), SALT görselleri
> **CC BY-NC-ND** (repoya girmez). Yani D1'e çıkmanın tek yolu **kamu malı ölçülü
> çizim** bulmaktır. Şansımız var: Ayasofya'nın ilk bilimsel plan ve kesitlerini
> **G. J. Grelot (1680)** çizdi ve kamu malı (Gallica bpt6k73264x; Heidelberg
> diglit doi 10.11588/diglit.1214). 1632'ye 38 yıl uzaklıkta.

| Yapı | Hedef | Kaynak durumu | 1632 kritik notu |
|---|---|---|---|
| Ayasofya | **D1** | Grelot 1680 (PD) — plan + kesit | Dört minare **var** (Mehmed II, Bayezid II, Sinan×2). I. Mahmud ekleri (kütüphane 1739, şadırvan 1740, imaret) **YOK**; muvakkithane (1853) **YOK** |
| Süleymaniye | D2 → D1 | Lorck 1559 (PD), dönem gravürleri | Tam faal külliye |
| Sultanahmet | D2 | Dönem gravürleri | **15 yıllık yeni yapı** — taş yıpranmamış, keskin |
| Kapalıçarşı | **D3 + D2** | TDV, Semavi Eyice | **Bugünkü kâgir hâl 1894 SONRASI.** 1632'de: bedestenler kâgir, **çevresi ahşap**. Bugünkü çarşıyı modellemek en büyük hata olur |
| Galata Kulesi | D2 | Lorck 1559, Pîrî Reis 1629 nüshası | **Sivri kurşun külah**, 1794 öncesi üst yapı |
| Yeni Cami | D2 | Thys-Şenocak | **Yarım harabe ("Zulmiyye")** — hero varlık olarak yarımlığı modellenir |
| Fatih Camii | D2 | Dönem planları | **1766 öncesi ÖZGÜN şema** — bugünkü değil |
| Topkapı silüeti | D2 | Grelot 1680 | Revan/Bağdat köşkleri **YOK**; Alay Köşkü var |

**Kural:** Bir landmark D1 iddia ediyorsa `refs/` altında **kamu malı** ölçülü
kaynağı bulunmalı ve `LICENSES.md`'de kayıtlı olmalıdır. Kaynak yoksa iddia D2'ye
düşürülür — "gerçeğe uygun" sanılan ama kaynaksız bir model, yanlış olduğunda
kimsenin fark edemeyeceği bir hatadır.

**Kabul (her landmark):** İnceleme paketi (4-açı + referans yan yana); T1 etiketi + kaynak satırı; **doğruluk basamağı (D1/D2/D3) yazılı**; 1632'de olmaması gereken hiçbir eklenti yok (kontrol listesi RESEARCH.md "yokluklar" bölümünden); Caner "OK vN".

---

## 9. Faz 4 — Şehri Doldurma (Prosedürel Yerleşim) · *tahmini 3–4 hafta*

- `DistrictDef` (ScriptableObject): semt karakteri — parsel yoğunluğu, kit paleti, renk dağılımı, dinî yapı oranı, servi/mezarlık kütleleri, bostan alanları.
- Deterministik (seed'li) yerleşim: sokak grafı → parseller → kit-bash evler; çıkmaz sokak oranı yüksek, organik örüntü. Aynı seed = aynı şehir (test edilebilirlik). Sokak grafı aynı zamanda **NPC navigasyon ve devriye rotalarının** temelidir (Faz 6'ya girdi).
- Donatı geçişi: çeşmeler, kuyular, dükkân kepenkleri, kayıklar (Haliç trafiği), çamaşır ipleri, kuş sürüleri.
- **Arazi yüzeyi** (Caner kararı, 2026-08-19 — plan boşluğu olarak tespit edildi, ADR 0009): Terrain katman karışımı eğim + irtifa + kıyı mesafesine göre kural tabanlı üretilir (kayalık yamaç, kuru ot, toprak, kumsal/çakıl kıyı bandı). Graybox checkerboard katmanı burada değişir. Dokular Poly Haven CC0.
- **Doğal örtü** (aynı karar): sur dışı, Okmeydanı, Boğaz yamaçları, Kâğıthane mesiresi ve mezarlıklar için kural tabanlı serpiştirme — servi/çınar kütleleri, çalı, kaya çıkıntıları, bostan parselleri. Uçuşta ekranın büyük kısmı burasıdır; yapılı doku kadar bütçe hak eder. Yerleştirme `DistrictDef` ile aynı deterministik seed'e bağlıdır ve Unity Terrain detail/tree sistemi + GPU instancing kullanır.
- Performans altyapısı bu fazda kurulur: LOD Group'lar, occlusion culling, uzak semt impostor/birleşik mesh, doku atlasları. Bütçe hedefleri: ekranda ≤ ~2,5 M üçgen, ≤ ~1500 draw call (GPU Resident Drawer ile), doku belleği ≤ 4 GB.

**Kabul:** Galata + Üsküdar semtleri dolu; kule tepesinden 360° bakışta FPS hedefi tutuyor; iki farklı seed görsel olarak "aynı şehir gramerinde farklı sokaklar" üretiyor; **sur dışı ve Boğaz yamaçları çıplak arazi değil** — doğal örtü ve arazi dokusu yerinde.

---

## 10. Faz 5 — Hezarfen Karakteri · *tahmini 2–3 hafta*

- **Taban geometri:** Blender Studio **Human Base Meshes** (CC0) — oranlar Claude tarafından uyarlanır; yüz hedefi **stilize-gerçekçi** (portre-fotogerçekçilik değil). Kaynak `refs/LICENSES.md`e işlenir. NPC gövde varyantları (Faz 6) aynı tabandan türetilir.
- **Kıyafet:** Dönem kıyafeti Ralamb/Mundy albümlerinden (kamu malı) uyarlanır; uçuş için işlevsel varyant (dar entari + dizlik + kanat kayışları). Saç/sakal: hair cards.
- **Kanat aygıtı:** Ayrı asset + kendi rig'i (açılma/çırpma/hasar durumları). Tarihî plan yoktur (dürüstlük notu kodekse); tasarım: ahşap çıta iskelet + kartal tüyü yüzey + deri kayış — dönem malzemesi kuralına uyar.
- **Rig/animasyon:** Blender Rigify → Unity Humanoid retarget. Set: locomotion (yürü/koş), tırmanma (kule içi merdiven), kanat kuşanma, kalkış, süzülüş pozları (pitch/roll blend tree), iniş/yuvarlanma, çakılma. Tümü Claude üretimi; her animasyon klibi için kısa döngü videosu/GIF inceleme paketine eklenir.
- Üçüncü şahıs varsayılan; uçuşta omuz-üstü ↔ geniş kamera geçişi.

**Kabul:** Kule tepesinde kuşanma → atlayış → süzülüş → Doğancılar inişi, kesintisiz animasyonlarla oynanabiliyor; karakter inceleme paketi Caner onaylı.

---

## 11. Faz 6 — Açık Dünya, NPC Yapay Zekâsı ve İçerik · *tahmini 6–8 hafta*

### 11.1 Açık dünya çekirdeği ("yaşayan İstanbul" döngüsü)
- **Serbest dolaşım:** yaya (yürü/koş + hafif tırmanma/parkur — dam ve duvar tırmanışı sınırlı ve "yasak bölge" kuralına bağlı); **kayık ağı** — Haliç/Boğaz iskeleleri arası "dönem taksisi": iskeleye git, akçe öde, karşıya geç (Haliç'te köprü olmaması bir eksik değil, ulaşım mekaniğidir).
- **Ekonomi:** **akçe** tek para birimi. Kazanç: görevler, minigame'ler, teslimat işleri. Harcama: kanat parça ve yükseltmeleri, kayık ücretleri, kıyafet, rüşvet(!) ve ceza ödemeleri.
- **İlerleme dört eksende:** (a) kanat beceri ağacı (Okmeydanı talimleriyle açılır — süzülme, dönüş, termik okuma), (b) **lonca itibarı** (esnaf görevleri → indirim, özel parçalar, yeni görev zincirleri), (c) kodeks tamamlama (T1/T3 kartları — keşifle açılır), (d) kıyafet/görünüm.
- **Kolluk/Ases "aranma" sistemi (dönemin GTA polisi):** İhlaller — gece fenersiz dolaşmak (1633 sonrası), yasak kahve/tütün taşımak, yasak bölgeye (saray duvarı, sur burçları) tırmanmak — ases/yeniçeri kolluğunun kademeli tepkisini tetikler: fark edilme → uyarı bağırışı → kovalamaca → yakalanma (para cezası / taşınan malın kaybı / görev başa). Kaçış yolları: kalabalığa karışma, avluya sıvışma, dama çıkıp kısa süzülme, kayıkla açılma. **Şiddetsiz tasarım:** silahlı çatışma yok; tamamen kovalamaca-saklanma (hem tarihe hem yaş derecelendirmesine uygun; üretim maliyeti de düşer — dövüş sistemi yok).
- **Dinamik olaylar:** mahalle yangını (söndürme zincirine katıl = itibar), pazar kurulumu, Cuma kalabalığı, esnaf alayı (1638, Perde 3 dönemi), gece devriye yoğunlaşması.
- **Zaman sistemi:** gün döngüsü 5 vakit ezanla yapılanır (Bölüm 11.3'teki NPC rutinlerinin de omurgası); takvim ana hikâye perdelerini taşır (1631→1633+).

### 11.2 Yan görev arketipleri (dönem-temelli; şablon + varyasyon)
Her arketip bir `QuestDef` şablonudur; varyasyonlar (NPC, yer, yük, ödül) veriden üretilir — el yapımı "altın" görevler + şablon türevleri karışımı.

| Arketip | Dönem dayanağı | Tipik ödül |
|---|---|---|
| Hamal/ulak teslimatları | iskele-çarşı yük akışı | akçe |
| Kayık yolcu taşıma / kayıkçı yarışı | Haliç pereme trafiği | akçe + kayıkçı itibarı |
| Okçuluk menzil yarışları | Okmeydanı menzil taşları | kanat beceri puanı |
| Esnaf tedarik zincirleri | Evliya'nın lonca listesi (RESEARCH.md) | lonca itibarı + kanat parçası |
| **Kahve/tütün kaçakçılığı (1633 sonrası)** | yasak dönemi | yüksek akçe + yüksek aranma riski |
| Yangın müdahale olayı | 1633 Cibali | mahalle itibarı |
| Kayıp eşya/kişi izleme | mahalle-imam kefalet düzeni | kodeks + akçe |
| Damdan dama "rota" meydan okumaları | serbest dolaşım becerisi | beceri puanı + görünüm |

Ana hikâye bu açık dünyanın üstünde akar: **Perde 1** (1631–32) talim + isyan fonu (Atmeydanı kalabalığı, sokak gerginliği), **Perde 2** (1632) büyük uçuş — zirve sekansı, IV. Murad Sinan Paşa Köşkü'nde, **Perde 3** (1633+) yasaklar/yangın/gece görevleri → sürgün finali → Cezayir epilogu (sinematik).

### 11.3 NPC yapay zekâsı — üç katmanlı mimari
- **Katman 1 — Simülasyon (tüm NPC'ler, her zaman açık):** Günlük rutin çizelgeleri (`NPCSchedule` SO) **5 vakit ezan** ve çarşı saatlerine bağlıdır: namaza akış, kepenk açma/kapama, öğle yoğunluğu, gece sokakların boşalması (1633 sonrası fener zorunluluğu). Davranış: utility-AI/behavior tree; navigasyon Faz 4 sokak grafı üzerinde; kalabalık GPU-instanced. Meslek tipleri: esnaf, hamal, kayıkçı, yeniçeri, ases (gece), su satıcısı, dilenci, çocuklar. **Şehri "yaşatan" asıl katman budur** — açık dünya hissinin büyük kısmı rutin ve tepkilerden gelir, diyalogdan değil.
- **Katman 2 — Yazarlıklı içerik, AI-üretimli (geliştirme zamanında, `tools/content/`):** Anahtar NPC diyalog ağaçları ve **binlerce satırlık ambiyans repliği korpusu** (satıcı bağırışları — Evliya'da belgeli, mahalle dedikoduları, dönem olaylarına göndermeler, hafif dönem Türkçesi tınısı) Claude tarafından **offline üretilir**, gözden geçirilir ve statik veri olarak gemiye konur. Çalışma zamanı maliyeti/riski sıfır; tamamı QA edilebilir; tarihsel ton kontrol altında. Bağlama duyarlılık, koşullu seçimle sağlanır (saat/hava/aranma durumu/perde → uygun replik havuzu).
- **Katman 3 — Çalışma zamanı LLM (deneysel, v1.0 SONRASI, karar kapılı — Bölüm 17):** Birkaç seçili NPC için gerçek zamanlı LLM diyaloğu teknik olarak mümkün, ancak dürüst uyarılar: (a) bulut API = oyuncu başına **sürekli maliyet** (satılan her kopya masraf üretir), (b) gecikme ve çevrimdışı oynanamama, (c) **moderasyon riski** — 1632 İstanbul'unda din, padişah ve cemaatler bağlamında kontrolsüz metin üretimi ciddi itibar riskidir, (d) QA edilemezlik. Denenecekse: yerel küçük model (oyunla gelen, çevrimdışı) veya oyuncunun kendi API anahtarı; katı persona kartları + konu sınırları + zaman aşımında Katman 2 repliğine düşme. v1.0 bunu İÇERMEZ; CLAUDE.md kuralı: çalışma zamanında bulut LLM çağrısı yok.

**Kabul (Faz 6):** Galata'da 30 dk kesintisiz serbest dolaşım (yükleme ekranı yok); ≥3 yan görev arketipi uçtan uca oynanabilir; aranma sistemi tam döngü (ihlal → kovalamaca → kaçış VE yakalanma sonuçları); NPC rutinleri sabah-öğle-akşam-gece geçişlerinde gözle görülür değişiyor; kayıkla Galata↔Üsküdar geçişi çalışıyor; Perde 2 dikey dilimi (talim → kule → uçuş → iniş → tepki sahnesi) baştan sona oynanabilir.

**Ses:** lodos rüzgârı = oynanış geri beslemesi (yükseltici hacimlerde ton değişir); şehir ambiyansı (martı, kayık, çarşı, Katman 2 bağırış korpusu); günde 5 vakit ezan zaman sistemine bağlı (uygulama saygılı ve doğru olmalı; Caner onayı şart); müzik: ney/tanbur ağırlıklı özgün beste (sonraki iş; seçimler Caner onayına sunulur).

---

## 12. Faz 7 — Görsel Cila ve Performans · *tahmini 2–3 hafta*

> ⚠️ **Faz 7 başlarken yapılacak İLK iş: geçici aydınlatma takımını SİLMEK.**
> `Hezarfen → Aydınlatma → Geçici aydınlatmayı kaldır`. Takım (ADR 0023)
> Faz 2b'de, yaya seviyesinden inceleme paketi üretilebilsin diye kuruldu ve
> fizikî değil: eksik **sıçrama** terimini gök terimini çarparak ve iki gölgesiz
> dolgu ışığıyla taklit ediyor, ayrıca pozu 14,5'ten 13,0 EV'ye çekiyor. Kalıcı
> ışık pası bunun **üstüne** kurulmaz, **yerine** kurulur. `LightingTests`
> gerekliliği (gölgedeki cephe okunabilir olmalı) ölçer, uygulamayı değil —
> kalıcı çözüm geldiğinde test yerinde kalır.

HDRP ışık sanat yönetimi tamamen Claude'dadır ve referans temellidir: `refs/` altından bir ışık mood-board'u (Lorck/Grelot gravür atmosferi + seçilmiş Poly Haven HDRI'ları) kurulur; fiziksel gökyüzü + saat sistemi, lodoslu hava profili (bulut hızı, dalga, ağaç savrulması senkron), volumetrik sis (Haliç sabahı), SSGI/RT seçenekleri donanıma göre kademeli. Post: hafif film grain + ton eğrisi (dönem gravür esintisi *çok* hafif). Her ışık profili için Unity MCP ile Game-view yakalamalarından inceleme paketi üretilir; Caner notlarıyla yakınsanır. Profil hedefleri sabit: 1080p/60 (orta segment GPU), 1440p/60 (üst). Otomatik performans testi: kule-turu + çarşı-kalabalığı benchmark sahneleri CI'da FPS raporlar (MCP'siz, batchmode).

> 📦 **Build sahne listesi kodda.** `Hezarfen → Boru Hattı → Build sahne
> listesini düzelt` (`BuildScenes`). Şu an açılış `Faz1_Terrain`, ikinci
> `FlightSlice`. Semt sahneleri **bilerek listede değil** — Addressables ile
> yükleniyorlar (ADR 0011) ve listeye de konursa iki kez paketlenirler.
> Gerçek açılış akışını (menü → yükleme → şehir) Faz 7 kararlaştırır ve
> `BuildScenes.Wanted` o gün güncellenir. ADR 0032.

## 13. Faz 8 — Paketleme, Test, Steam · *en son*
Windows build pipeline (`-batchmode`), kayıt sistemi, ayarlar menüsü, kısa kapalı test (5–10 kişi), **Steam entegrasyonu bu aşamada** (Steamworks, sayfa, capsule görselleri). Krediler ekranı: OSM (ODbL) atfı, Poly Haven, Blender Studio Human Base Meshes, kaynakça (RESEARCH.md kısaltması) — tarih meraklısı oyuncuya kaynakça vermek bu oyunun pazarlama gücüdür.

---

## 14. Varlık ve Telif Kuralları (İHLAL = görev reddi)

| Kaynak | Lisans | Oyunda kullanım |
|---|---|---|
| Lorck 1559, Grelot 1680, Ralamb albümü, Braun-Hogenberg, Nicolay | Kamu malı | Referans + istenirse doğrudan (kodeks görseli) |
| Poly Haven doku/HDRI | CC0 | Serbest, atıf gerekmez (yine de krediler ekranına yaz) |
| Blender Studio Human Base Meshes | CC0 | Karakter/NPC taban geometrisi olarak serbest |
| OSM verisi | ODbL | Footprint türetimi OK; **atıf zorunlu** |
| SALT Araştırma görselleri | CC BY-NC-ND | **Ticari oyunda kullanılmaz**; yalnızca insan gözüyle referans |
| Müller-Wiener planı, modern kitap/edisyonlar | Telifli | Yalnızca bilgi kaynağı; taranmış görsel repoya giremez |
| Assassin's Creed / GTA vb. oyun içerikleri | Telifli | Repoya giremez; sadece mekanik ilham |

`refs/LICENSES.md` her dosyanın kaynağını ve lisansını listeler; listede olmayan dosya `refs/`e giremez.

---

## 15. Claude Code İçin İlk 12 Görev (hemen başlanabilir)

1. Repo iskeleti: klasör ağacı (Bölüm 3), Unity `.gitignore`, `.gitattributes` (LFS), `CLAUDE.md` (Bölüm 4 şablonu), boş `refs/LICENSES.md`, `docs/feedback/` klasörü, `.mcp.json` iskeleti.
2. `[İNSAN]` kurulumlar: Unity 6 LTS + HDRP boş proje `unity/HezarfenGame`; Blender LTS; Claude Code; **blender-mcp eklentisi + iki Unity MCP adayının kurulumu** (resmî Unity MCP etkinleştirme VE MCP for Unity paketi — Bölüm 4.1 adımları); bağlantı onayları. Sürümler `docs/decisions/0001-versions.md`e.
3. **MCP duman testi + Unity köprüsü seçimi:** Claude Code, Blender'da MCP ile küp oluşturup siler; her iki Unity adayıyla sırayla: boş GameObject oluştur, konsol oku, basit test tetikle. Kararlılık/kapsam karşılaştırması ve **kazanan köprü kararı** `docs/decisions/0002-mcp-smoke.md`e; kaybeden yedek olarak belgelenir.
4. Graybox sahne: düz arazi, 100 m kule primitifi, 3,4 km ötede hedef platform, basit güneş.
5. `GlideController` v0 + `WindTuning` SO + Cinemachine uçuş kamerası; klavye ve gamepad.
6. `WindField` v0: global lodos + gizmo'yla yerleştirilebilir yükseltici hacimler + görünür rüzgâr partikülleri.
7. Blender headless boru hattı doğrulaması: `gen_box_house.py` (2 katlı kutu ev) → `export_fbx.py` → Unity import → **ölçek/eksen Editor testi** (1 m küp tam 1 m mi?). (MCP olsa da headless hat CI omurgası olarak kurulur.)
8. `render_preview.py` + inceleme paketi üretici: 4-açı + yakın plan + referans-kolaj tek komutla `renders/review/<varlık>_vN/`.
9. GIS: DEM indirme+kırpma+heightmap scripti (`tools/gis/dem_fetch.py`, rasterio); Unity Terrain import Editor aracı.
10. `coastline_1632.geojson` taslağı (modern kıyı + RESEARCH.md düzeltme listesi yorum satırlarıyla) + GeoJSON→sahne importer'ı.
11. `ottoman_kit.py` v1: parametrik ev (kat, genişlik, cumba) → ilk inceleme paketi Caner'e.
12. **Geri bildirim döngüsü provası:** Caner'in 11. görevdeki notlarını uygula, v2 paketi üret, `docs/feedback/ottoman_house.md`e logla. (Protokolün uçtan uca çalıştığının kanıtı.)

Sonrası: her faz kapısında Claude Code, kabul kriterleri karşılandığına dair kanıt (test çıktısı + inceleme paketi) sunar; Caner onaylarsa sonraki faza geçilir. Açık dünya sistemlerinin (ekonomi, görev, aranma, NPC rutini) ilk graybox denemeleri Faz 6'yı bekler; Faz 0–5 uçuş ve şehri kurar.

---

## 16. Riskler ve B Planları

| Risk | Belirti | B planı |
|---|---|---|
| Claude-üretimi modellerde gerçekçilik tavanı | Landmark render'ları referansa yakınsamıyor | İterasyon bütçesini artır; yüksek-poli → normal bake hattı; CC0 kaynak geometri/foto-doku ara; ışık/malzeme yatırımını öne çek; son çare: tek seferlik dış sanatçı desteği (Caner kararı) |
| MCP köprü kararsızlığı (beta yazılımlar) | Bağlantı kopmaları, zaman aşımları, sürüm uyumsuzluğu | Her iş headless CLI/batchmode ile de yapılabilir olmalı (MCP'siz tam işlerlik); sürümleri sabitle, addon+paket birlikte güncelle; iki Unity adayı arasında geçiş serbest (ADR) |
| **Açık dünya içerik değirmeni** | Yan görevler ve doluluk el yapımıyla yetişmiyor; harita "boş" hissettiriyor | Arketip şablonları + offline AI varyasyon üretimi; içerik semt kapılı ("az semt, dolu semt"); dinamik olaylar ve NPC rutinleri doluluk hissinin ana taşıyıcısı; suriçi genişlemesi ancak Galata+Üsküdar "dolu" onayından sonra |
| **Çalışma zamanı LLM NPC cazibesi** | Maliyet/gecikme/moderasyon riski fark edilmeden tasarıma sızması | v1.0'da kesin kural: runtime bulut LLM yok (CLAUDE.md'de yazılı); Katman 2 offline üretim ihtiyacı karşılar; Katman 3 ancak Bölüm 17 kararıyla, yerel model/opsiyonel olarak |
| HDRP karmaşıklığı/performansı | Faz 1 kapısında FPS/üretim hızı hedef altı | URP'ye geç (karar kapısı Faz 1 sonu; sonrası pahalı) |
| Kapsam şişmesi | Suriçi "de bitsin" baskısı | Eksen dilimi kuralı: Perde 2 bitmeden suriçi A-kademe başlamaz |
| Tarihsel eleştiri | "Uçuş efsane, oyun yanıltıyor" | T3 şeffaflık kodeksi + kaynakça ekranı zaten tasarımın parçası |
| Tek geliştirici tükenmişliği | Fazlar uzuyor | Her faz sonunda oynanabilir bir şey vardır; motivasyon dilimleri küçük tut; geri bildirim turlarını kısa ve sık yap |

---

## 17. Açık Kararlar (Caner onayı bekliyor)

1. **HDRP onayı** (öneri: evet, Faz 1 kapılı) — karşı görüş: URP daha hızlı üretim.
2. Kamera: yalnız üçüncü şahıs mı, uçuşta birinci şahıs seçeneği de mi? (öneri: TPS varsayılan + FPS uçuş opsiyonu sonra)
3. Perde 3'ün kapsamı: yangın sekansı büyük iş — v1.0'da mı, sürüm sonrası mı? (öneri: v1.0'da tek sokakla sınırlı sahne)
4. Dil: metinler TR öncelikli, EN yerelleştirme Faz 8'de. Onay?
5. **Katman 3 (çalışma zamanı LLM NPC):** v1.0 sonrası deney mi, tamamen kapsam dışı mı? (öneri: v1.0 sonrası, yerel model + opsiyonel özellik olarak; bulut API'li sürüm önerilmez)
6. Kayık geçişleri: hep gerçek zamanlı yolculuk mu, ilk binişten sonra hızlı seyahat seçeneği mi? (öneri: ilk biniş gerçek zamanlı, sonrasında iskeleden hızlı seyahat açılır)
