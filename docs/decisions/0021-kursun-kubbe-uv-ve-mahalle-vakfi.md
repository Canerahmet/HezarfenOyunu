# ADR 0021 — Kurşun, kubbe UV'si ve mahallenin vakfı

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — üretildi, Galata ve Balat'a yerleşti; Caner onayı bekliyor
**Tetikleyen:** ADR 0020 §6'daki boşluklar + Faz 2b listesinin devamı.
**İlgili:** ADR 0017 (doku), 0019 (ağaç/hazire, §11 aydınlatma), 0020 (hamam/han);
RESEARCH.md §4.3; PLAN.md §7.1

---

## 1. Kurşun artık dokulu — ve bu bir doku kararından fazlası

Kubbe kurşunla örtülür: hamamın, hanın, mescidin, türbenin ve mektebin **üstü**
budur. Yani şehre yukarıdan bakıldığında görülen yüzeylerin çoğu kurşundur.
Bir **uçuş** oyununda en çok bakılan yüzeye düz gri bir renk koymak, en çok
bakılan yere hiç bakmamaktı. ADR 0017 bunu "uygun CC0 dokusu yok" diye boş
bırakmıştı.

Poly Haven'da kurşun örtü yok. Çözüm yaprakta olduğu gibi indirmek değil
**üretmek**: `tools/textures/gen_lead_texture.py`, çıktı bizim eserimiz
(LICENSES.md). İki üretici artık ortak `proclib.py`yi kullanıyor.

### Kurşunu kurşun yapan şey: dikiş

Kurşun tek parça dökülmez; el ile açılmış levhalar hâlinde serilir ve
birleşimler katlanarak kapatılır — eğim yönünde **rulo dikiş** (belirgin sırt),
eğime dik **enine kat** (alçak). Levha 0,50 × 1,00 m: taşınabilir ağırlık
sınırının içinde ve 2 m'lik döşenebilir karede tam 4 × 2 levha eder. Kesirli
levha, dikişi karonun kenarında kırardı.

Dikiş aralığı gözün kubbenin **büyüklüğünü** okuduğu cetveldir. Onsuz doku
"gri metal levha" olur.

### Ölçülen: patina püstür, sıçrama değil

İlk üretimde oksit lekeleri 420 adet ve res/20 yarıçapındaydı. Ölçüm taban
tonun **50 seviye** üstüne çıktıklarını gösterdi (p99 = 164, std 12,2) ve doku
sıçramış boya gibi okundu. Patina bir sıçrama değil pustur: çok, küçük, az
kontrastlı.

| | leke sayısı | yarıçap | albedo karışımı | std | p99 |
|---|---|---|---|---|---|
| ilk | 420 | res/20 | 0,78 | 12,2 | 164 |
| **son** | **950** | **res/34** | **0,42** | **6,5** | **133** |

Son hâl: ortalama parlaklık 111,9, R−B = −7,9 (kurşun **soğuk** gridir).

### Metaliklik neden 1,0 değil

Havaya açık kurşun bazik kurşun karbonatla kaplanır ve o tabaka **dielektrik**
tir. Yüzey bu yüzden karışımdır: yıkanan sırtlar çıplak metale yakın, düzlükler
oksitle örtülü. Maske R kanalı bu **örtü oranını** taşır (ölçüldü: 0,12–0,78).

Üst sınırın 1,0'a dayanmaması ayrıca bilinçli: sahnede henüz dolaylı aydınlatma
pişmemiş (§7). Tam metal bir yüzeyin taban rengi yoktur, yalnızca yansıması
vardır; yansıtacak bir şey olmadığında **siyah** çıkar. GI pişince bu tavan
yeniden ölçülmeli.

## 2. İki sessiz tuzak, ikisi de ölçüyle bulundu

### 2.1 `_Metallic = 0` bir gün doğruydu, ertesi gün yalan oldu

HDRP maskenin R kanalını `_Metallic` ile **çarpar**. Kitte metal yokken
`OttomanMaterialBuilder`ın `mat.SetFloat("_Metallic", 0f)` satırı doğruydu ve
gerekçesi de yorumda yazılıydı. Kurşun gelince aynı satır sessizce yanlış oldu:
maske oksit örtüsünü piksel piksel taşıyor ama çarpan 0 olduğu için kubbe yine
mat gri kalırdı — doku "yüklenmiş" görünür, hata görünmez.

Çarpan artık **bildirimden** gelir (`metallic` alanı) ve `Verify` malzemeyi
geri okuyup bildirimle karşılaştırır. Ders eskisiyle aynı: *bugünkü kit için
doğru olan sabit, yarınki kit için denetlenmemiş bir varsayımdır.*

### 2.2 `T_LeadSheet_BC.jpg` aslında PNG'ydi

`build_unity_maps.py` boyasız albedoları kaynaktan kopyalarken uzantıyı
**sabit** `.jpg` yazıyordu. Poly Haven albedoları JPG olduğu için yıllarca fark
etmedi; prosedürel dokular PNG gelince dosyalar ".jpg" adıyla PNG içeriği
taşımaya başladı (yaprak dokularında da öyleydi). Unity içe aktarmayı
**uzantıya göre** seçer. Uzantı artık kaynaktan alınıyor.

## 3. Kubbenin UV'si: "kırılmış fayans"

Kurşun dokusu ilk kez kubbeye giydiğinde türbenin kubbesi **kırılmış fayans**
gibi çıktı. Render bir gözlemdir; ölçtüm ve sebep geometri değil **UV**di.

`materials.uv_project` yüze hizalı, dünya ölçekli izdüşüm yapar ve düz mimarî
yüzeylerde doğru olan budur — bedeli de dosyanın başında yazılıydı: *"farklı
yönlü yüzler arasında dikiş oluşur; kiremit/taş gibi düzensiz dokularda
görünmez."* Kubbe bu varsayımın tam kırıldığı yer: **her yüzü ayrı bir teğet
düzlem** ve kurşunun **düz çizgili dikiş ızgarası** var.

Çözüm: eğri yüzeyler UV'lerini kendileri kurar ve `hz_blender.UV_METRIC`
bayrağıyla işaretlenir; `uv_project` işaretli yüzü yeniden yansıtmaz, yalnızca
dokunun metre ölçüsüne **böler**. Birim metredir, dokuya çevirme tek yerde kalır.

Kubbede UV **meridyen dilimleridir**: `u` çevre boyu (yarıçapla ölçülür, halka
yarıçapıyla değil — dilim tepeye doğru **daralır**, gerçek kurşun dilimi gibi),
`v` meridyen yay uzunluğu. Konide ve silindirde çevre × eğim boyu.

Bu yalnız kurşuna yaramadı: minare gövdesi, külah, apsis yarım kubbesi,
şadırvan — hepsi aynı yoldan geçiyor.

**Öz-test:** `t_dome_uv_continuous` iki yüzün paylaştığı köşeye aynı UV'yi verip
vermediğini sayar. Beklenen tek kopukluk azimut dikişidir (`rings` kadar kenar);
düzlemsel izdüşümde bu sayı yüzlerce olurdu. **Tepe noktası ölçüye girmez** —
ilk yazımda girmişti ve test 21 kopukluk saydı, 16'sı tepedeydi: kutupta her
dilimin farklı `u` taşıması kopukluk değil, kutupun tanımıdır. Ölçünün kendisi
yanlıştı.

### Yan etki: bmesh katman tuzağı

bmesh'e sonradan özel veri katmanı eklemek **mevcut eleman referanslarını
geçersiz kılar** (`ReferenceError: BMFace has been removed`). `metric_layers(bm)`
artık yüzler kurulmadan önce çağrılıyor; hata doğduğu yerde bitiyor.

## 4. ADR 0020'nin han boşlukları kapandı

| Boşluk | Durum |
|---|---|
| Avlu zemini yok | ✅ kaldırım taşı döşendi + **kuyu** (bilezik, direk, kiriş) |
| Revak sütunsuz | ✅ kürsü + gövde + **başlık**; köşe ayakları masif kaldı |
| Kurşun dokusuz | ✅ §1 |
| Göbek taşı/kurna | ✗ **iç mekân** — §7 |

Sütunda ölçülen hata: ayak 0,44 m, sütun çapı 0,40 m'ydi ve silindir düz ayak
yüzünün **arkasında** kaldı — cepheden hiç görünmedi, revak yine "delikli
duvar"dı. Sütunu gösteren şey konumu değil, **ayağı örtmesi**dir. İlişki artık
kodda yazılı: `2·COL_R > PIER_W` ve `2·COL_R > REVAK_T`.

Ayak inceldiği için avlu **köşelerine masif ayak** kondu — iki revak orada dik
kesişir ve yük köşeye biner. Köşe ayağı iki revakın ortak noktasına **bir kez**
konur; her revak kendi ucuna koysaydı aynı yerde iki kutu üst üste binerdi.

`Han_A` 7 858 → **11 466** üçgen.

## 5. Mahallenin vakfı: türbe, mektep, kahvehane

Kaynak ve tipoloji RESEARCH.md §4.3'te; burada yalnızca **kararlar**.

| | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|
| `Turbe_A` (sekizgen) | 7,31 × 7,76 | 8,77 | 2 802 |
| `Turbe_B` (altıgen) | 6,89 × 6,54 | 7,87 | 2 176 |
| `Mektep_A` | 7,45 × 8,65 | 9,00 | 1 930 |
| `Kahvehane_A` | 8,50 × 9,05 | 6,82 | 508 |
| `Kahvehane_B` | 7,00 × 7,75 | 5,05 | 424 |

**Türbe** sekizgenin her yüzünü ayrı **delikli panel** olarak kurar. Paneller
dış kenar uzunluğuyla ölçülür, yani köşede birbirine girer; iç kenarla
kurulsalardı köşe dışında V biçimli bir yarık kalır ve ışık oradan sızardı.
Çapraz yüzlerde eksene hizalı kutu kullanılamaz — kitin `_put_shadow` ve
`iron_grille` yardımcıları `abs(u_axis[0]) > 0.5` diye **bakarak** eksen seçiyor
ve bu 45°'de sessizce yanlış cevap veriyor. `mahalle_kit.oriented_box` varsayım
yapmaz.

**Mektep** yükseltilir; merdiveni silinirse yapı küçük bir mescide döner. İki
ölçülen hata:
- Çeşme nişi ortadaydı ve kapının önündeki sahanlığın **arkasına** düşüp
  tamamen görünmez kaldı. Merdiven cepheyi ortadan işgal ettiği için çeşme
  yana kayar.
- Korkuluk tek parça ve sabit kotluydu (üstü 2,02 m): bütün basamaklar
  arkasında kaldı, cepheden merdiven diye bir şey görünmüyordu. Korkuluk artık
  **eğimi izler** — sorun ölçüyle değil biçimle çözüldü.

**Kahvehane** anıt değildir. Çatı ilk denemede tamamen yanlıştı: `_shed`
imzasında uzaklık ve kot ayrı listelerdeydi (`n_near, n_far, z_near, z_far`),
çağıran `n_axis` zaten yönü taşırken bir de uzaklığa işaret çarptı ve iki çatı
levhası **aynı tarafa** düştü. İmza artık çiftleri bitişik alıyor
(`n_a, z_a, n_b, z_b`); o hata imzada imkânsız.

`KahvehaneParams.validate()` iki şeyi **ölçer**: seki yüksekliği 0,36–0,52 m
dışındaysa oturulacak yer değildir, ve saçak sekiyi örtmüyorsa yağmurda
oturulamaz — kahvehaneyi kahvehane yapan şey o örtülü sekidir.

### Zaman işareti

Kahvehane 1632'de açıktır ve **2 Eylül 1633'ten sonra yoktur** (BOA A.DVN
25/47). Oyuna zaman katmanı eklendiğinde sahneden kaldırılacak **ilk varlık**
budur; `OttomanStreetBuilder` içindeki yerleştirme satırı bunu yazılı taşıyor.

## 6. Yerleştirme

| Yapı | Kural | Ölçülen (Galata) |
|---|---|---|
| Türbe | hazîrenin **ucunda**, kapısı mezarlığa | mescide 21,3 m, en yakın ev 6,8 m, kot farkı 0,29 m |
| Mektep | çekirdeğe yakın, sokak kenarında | mescide 30,7 m, en yakın ev 9,3 m, kot farkı 0,21 m |
| Kahvehane | çarşı ucunda (hanla aynı kural) | mescide 27,1 m, en yakın ev 14,6 m |
| Çınar | kahvehanenin **yanında** (önünde değil) | 9,1 m |

Türbe hazîrenin **içine** değil ucuna: 7,3 m'lik sekizgen, 7 × 9 m'lik bir
hazîrenin duvarları arasına sığmaz ve zorlanınca mezar taşlarının üstüne
oturur. Gerçekte de türbe hazîrenin sınırını **oluşturur**.

**Balat'ta türbe ve mektep YOK** — ikisi de müslüman vakıf kurumudur.
`QuarterSpec.HasVakif` bayrağı elle değil **çekirdekten türer**
(`CoreKind == "mescit"`); iki yerde tutulsa bir gün ayrışırdı. Kahvehane her
iki mahallede de var.

### Test kendi muafiyetini yine elle tutuyormuş — ama bu sefer ters yönde

Çınar eklenince `NoHouseIsBuriedInTheTerrain` düştü: 1 köşe 0,96 m gömülü. Ölçüm
kusurun ağaçta olduğunu gösterdi ve **kusur değildi** — kural "en yüksek köşeye
otur, altını taş kaideyle doldur" diyor, kaidesi olan şey **yapı**dır. Çınarın
gövdesi tek noktada zemine değer, tacı havadadır; 6 m yarıçaplı bir tacın sınır
kutusunun yamaçta araziye girmesi doğaldır.

Muafiyet **ad listesiyle** verilemezdi (ADR 0020 §4'ün dersi). Katalogda zaten
`kind` alanı var: `AssetCatalog.IsBuilding` onu okur. Test ayrıca **kaç varlığın
muaf tutulduğunu** da sınıyor — sıfırsa katalog okunamamış demektir ve muafiyet
sessizce etkisizdir.

## 7. Kalan boşluklar

- ~~**Sokak seviyesinde dolaylı aydınlatma hâlâ yok**~~ → **ADR 0023'te
  kapandı.** Bu turdaki teşhis ("dolaylı aydınlatma yok") yarı yanlıştı: gök
  ışığı geliyordu, eksik olan **sıçrama** terimiydi — ve ikinci sebep pozdu
  (EV 14,5, yaz öğle değeri). Geçici takım ikisini de karşılıyor; kalıcı
  çözüm Faz 7.
- İç mekân yok (hamamın göbek taşı, kahvehanenin ocağı, türbenin sandukası).
  Bu artık yapı bazında değil **genel** bir boşluktur; PLAN.md §7.1 iç mekânı
  görev mekânlarıyla sınırlıyor.
- Cam **bilerek dokusuzdur ve öyle kalır**: fil gözü 20 cm'lik bir kabarcıktır,
  onu cam yapan şey albedo deseni değil pürüzsüzlüktür.
- Ağaç tacı hâlâ katı kabuk (ADR 0019); silüet kenarı sert.
- Kahvehanede insan yok — seki boş. Kalabalık Faz 6.
- Faz 2b listesinde kalanlar: ~~medrese~~, tekke, imaret, ~~fırın~~, bozahane,
  değirmen, arasta, ~~sebil~~, su terazisi, muvakkithane, namazgâh.
  *(Üstü çizililer ADR 0022'de üretildi.)*
