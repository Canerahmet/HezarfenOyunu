# ADR 0057 — Ayrıntı geçişi: fotoğraftaki *gibi* değil, fotoğraftaki *dil kadar*

- **Durum:** kabul
- **Tarih:** 2026-08-27
- **Bağlam:** Caner'in isteği — *"Faz 3'te üretilen modeller gerçek
  dünyadaki gibi detaylı olsun. Sonuçta bu yapılar hakkında fotoğraf,
  görsel veya yazılı birçok kaynak var."*

## Neden kopyalamıyoruz

İstek yerinde: 36 anıt toplam **77 661** üçgendi — tek bir kahraman
yapıdan az. Ama fotoğrafı doğrudan izleyemeyiz, iki nedenle:

1. **Fotoğraflar 1632'yi göstermiyor.** Ayasofya'nın okra sıvası
   Fossati'nin (1847-49); Fâtih'in bugünkü dış cephesinin tamamı
   1767-71 onarımı. Fotoğrafa bakıp "aynısını" yapmak, oyunu
   **yanlış yüzyıla** taşır.
2. **Görselleri depoya kopyalayamayız.** SALT görselleri CC BY-NC-ND
   (yalnız bakılır), Müller-Wiener planları telifli. `refs/LICENSES.md`
   kuralı: lisansı belgelenmemiş hiçbir görsel indirilmez.

## Karar

Ayrıntı, fotoğraftan **kopyalanmaz**; Osmanlı/Bizans/Ceneviz mimarisinin
**dilbilgisinden** kurulur. Ortak bir dağarcık yazıldı —
`tools/blender/lib/detay_kit.py` — ve bütün yapılar ondan beslenir:

| öğe | ne yapar |
|---|---|
| `silme` / `silme_at` | kademeli saçak; duvar bir çizgiyle değil gölgeyle biter |
| `mukarnas`, `mukarnas_konsol` | sarkıtlı halka ve konsol |
| `mukarnas_kavsara` | taçkapı nişinin yarım tonozu |
| `sutun`, `kose_ayagi` | kaide-gövde-başlık; halkanın dönüş ayağı |
| `kemer` (`sivri=True/False`) | tek kemer; Osmanlı sivri, Ceneviz/Bizans yuvarlak |
| `revak_sirasi`, `revak_ust`, `gozleri_dagit` | sütun+kemer+alınlık+kubbe; örtü kotu; göz dağılımı |
| `kubbe_kaburga` | kurşun örtünün dikiş çizgileri |
| `serefe`, `minare_govde`, `alem` | minare parçaları |
| `kemerli_pencere_sirasi`, `cephe`, `kabuk` | gerçek kemerli pencereli cepheler |
| `konsol_dizisi` | siperi taşıyan konsol sırası (Ceneviz) |
| `tackapi` | sekiz parçalı anıtsal giriş |

Dağarcık **paylaşıldığı için** bir düzeltme her yapıya birden gider; bu
hem gücü hem riski: bir kural yanlışsa hepsine yanlış gider. Bu yüzden
sayıya bağlı kurallar kitin değil, **yapının** doğrulayıcısında durur
(bkz. ADR 0056).

## Ölçülen sonuç

| yapı | önce | sonra |
|---|---:|---:|
| Sultanahmet | 8 282 | 102 952 |
| Süleymaniye | 7 294 | 89 668 |
| Beyazıt | 4 802 | 55 380 |
| Fâtih Camii | 4 424 | 48 854 |
| Ayasofya | 4 448 | 39 682 |
| Sandal Bedesteni | 3 168 | 20 880 |
| Üsküdar Mihrimah | 5 008 | 20 354 |
| Cevahir Bedesteni | 2 418 | 16 170 |
| Türbe (Sultan Ahmed) | 938 | 9 450 |
| Galata Kulesi | 1 012 | 3 268 |
| Yedikule | 2 658 | 4 098 |
| Mahalle mescidi | 1 928 | 4 236 |
| Bâbüsselâm | 550 | 3 958 |
| Adalet Kulesi | 286 | 1 262 |
| Kara sur burcu | 312 | 576 |

Bütçeler tutuyor: selâtin cami 40–70k hedefinin üstünde ama tekil
kahraman yapılar için kabul edilebilir (RTX 4070, 8 GB); sur burcu
**192 örnek** basıldığı için 1 500 sınırının çok altında tutuldu.

## Ayrıntı geçişinde bulunan kusurlar

Hepsi **ölçülerek** bulundu, gözle değil:

- Revak gözü ters yöne açılıyordu (`bay_dir` işareti); kubbeler duvara
  değil avlunun ortasına bakıyordu.
- Revak örtüsü avlu duvarını **1,4 m aşıyordu**; kot elle yazılmıştı.
  Artık `revak_ust` ile türetiliyor.
- Bedestende pencere kemerleri **saçağı deliyordu**; denizlik kotu
  silmeden türetildi.
- Türbede köşe sütunçeleri duvarın **içine** düşmüştü: çokgende köşe
  yarıçapı `half` değil `half/cos(π/n)`.
- **En büyüğü, en sona kaldı:** `hz.make_box` köşe koordinatlarını mesh'e
  yazar ve nesne dönüşümünü kimlik bırakır. Bu yüzden kutuyu yerine koyup
  **sonra** `rotation_euler` vermek, onu kendi merkezi değil **dünya
  orijini** etrafında döndürür. Bu hatayı iki yere yazdım
  (`konsol_dizisi`, `mukarnas_kavsara`) ve ikisi de **görüldü ama yanlış
  teşhis edildi**: türbe duvarındaki beyaz benekleri "mukarnas hücreleri
  fazla küçük" sandım ve mihrabiyeyi kapattım. Yanlıştı — benekler
  yapının üstüne savrulmuş taçkapı parçalarıydı.

  Gerçek sebebi **bir sayı** ele verdi: Yedikule'nin ayak izi 165,9×161,2
  m'den **173,0×174,6** m'ye çıktı. Ölçü, gözün göremediğini söyledi.
  Düzeltme `detay_kit.donuk_kutu` — *orijinde kur → döndür → yerine taşı* —
  ve düzeltmeden sonra mihrabiye **açıkken de** benek yok, Yedikule
  166,1×161,2.

  Ders: **bir kusurun sebebini, en son değiştirdiğin şeyde arama; ölçülen
  şeyde ara.** Ve: *"gördüğün kusuru düzeltmeden önce ölç"* kuralı bir kez
  daha, bu sefer düzeltmenin kendisi için geçerliydi — yanlış teşhisle
  yapılan "düzeltme" (mihrabiyeyi kapatmak) belirtiyi azaltmış, sebebi
  gizlemişti.

  Bunun üzerine `ottoman_kit._donus_denetimi` bekçisi yazıldı ve otuz
  üretecin hepsi tarandı: **iki mevcut hata daha** çıktı (Mihrimah'ın
  ikinci revak yan örtüleri yanlış yerdeydi; su terazisinin iki künkü
  simetrik değildi) ve bir **kırılgan** yer (değirmen çarkı göbeği elle
  telafi edilmişti). → **ADR 0058**
- **Mukarnas hücresi bir oran değil, bir ölçüdür.** `mukarnas_kavsara`
  kat başına sabit yedi hücre üretiyordu; Bâbüsselâm'ın 5,8 m'lik nişinde
  bu, hücreyi 1,5 m'ye çıkardı ve kavsara sarkıt değil **dama tahtası**
  okundu. Hücre sayısı artık nişin **fiziksel genişliğinden** türüyor
  (`HUCRE_EN = 0,52 m` — taş yontma ölçeği), yani türbenin 2,2 m'lik
  nişinde de sarayın 5,8 m'linde de hücre aynı boyda.
- **Görünmeyen geometri eklemek, üçgeni ödünç vermektir.** Mahalle
  mescidine saçak silmesi koydum; renderda hiç görünmedi. Sebep ölçüldü:
  mescit **ahşap çatılıdır** ve kiremit saçak duvardan silmeden çok daha
  fazla taşar, onu tümüyle yutar. Üstelik yanlıştı da — taş korniş kâgir
  saçakla biten yapının öğesidir; burada o işi saçağın kendisi yapar.
  Silme kaldırıldı, gerekçesi koda yazıldı.
- Sultanahmet'in taçkapısı sessizce **atlandı**: Süleymaniye ile aynı
  silme değerlerini paylaşıyordu ve metne göre yapılan ekleme ilk
  eşleşmeye gitti. Ders: *paylaşılan metin, hedef ayırt etmez.*

## Bitmeyenler

- **Kız Kulesi** (799) ayrıntılanmadı ve bu bilinçli: 1632'de **ahşap**
  bir yapıdır, taş oymacılığı dili oraya ait değil ve konsol, kuşak,
  korkuluk zaten var. Yedikule ayrıntılandı (2 658 → 4 098).
- **Doku çözünürlüğü — endişe yersizmiş, ölçüldü.** "UV ölçekleri kütle
  geometrisi için ayarlıydı" diye not düşmüştüm. Değil:
  `materials.uv_project` **dünya ölçekli ve yüz başına** çalışıyor, her
  yüz kendi malzemesinin metre cinsinden doku boyunu kullanıyor. Yeni
  geometri texel yoğunluğunu bozmuyor, kendiliğinden doğru UV alıyor.
  Docstring'in uyardığı gerçek tuzak başkaydı — "nesne dönüşümü
  uygulanmış varsayılır" — ve o da ölçüldü: birleştirilmiş LOD0'ın
  dönüşümü kimlik, yani güvenli.
- Küçük varlıklardan **Topkapı Adalet Kulesi** (eliböğründe payandalar,
  kademeli silme, kemerli söveler), **Bâbüsselâm** (gerçek taçkapı — bu
  yapının adı da işlevi de kapıdır) ve **mahalle mescidi** (mukarnaslı
  şerefe, delikli korkuluk, hafifletme kemerleri) yapıldı. Medrese,
  mektep, tekke ve köşkler hâlâ dağarcığa bağlı değil.

İlgili: [ADR 0056](0056-kapali-revak-halkasi.md), [ADR 0058](0058-donus-bekcisi.md)
