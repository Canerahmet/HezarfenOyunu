# Ayrıntı geçişi — inceleme paketi

**Durum:** onay bekliyor
**Tarih:** 2026-08-27
**İstek:** *"Faz 3'te üretilen modeller gerçek dünyadaki gibi detaylı olsun.
Sonuçta bu yapılar hakkında fotoğraf, görsel veya yazılı birçok kaynak var."*

## Ne yaptım

Fotoğrafı kopyalamadım — iki nedenle kopyalayamam:

1. **Fotoğraflar 1632'yi göstermiyor.** Ayasofya'nın okra sıvası Fossati'nin
   (1847-49); Fâtih'in bugünkü dış cephesinin tamamı 1767-71 onarımı.
   Fotoğrafa bakıp "aynısını" yapmak oyunu yanlış yüzyıla taşırdı.
2. **Görselleri depoya kopyalayamayız.** SALT görselleri CC BY-NC-ND,
   Müller-Wiener planları telifli.

Bunun yerine ortak bir **ayrıntı dağarcığı** yazdım
(`tools/blender/lib/detay_kit.py`) ve bütün yapıları ona bağladım: silme,
mukarnas, kavsara, sütun, köşe ayağı, sivri/yuvarlak kemer, revak, kubbe
kaburgası, şerefe, âlem, taçkapı, konsol dizisi.

Kısacası: **"fotoğraftaki gibi" değil, "fotoğraftaki dil kadar".**

## Ölçülen sonuç

| yapı | önce | sonra | inceleme paketi |
|---|---:|---:|---|
| Sultanahmet | 8 282 | 102 952 | `renders/review/Sultanahmet_v4/` |
| Süleymaniye | 7 294 | 89 668 | `renders/review/Suleymaniye_v8/` |
| Beyazıt | 4 802 | 55 380 | `renders/review/Beyazit_v2/` |
| Fâtih Camii | 4 424 | 48 854 | `renders/review/FatihCamii_v4/` |
| Ayasofya | 4 448 | 39 682 | `renders/review/Ayasofya_v4/` |
| Sandal Bedesteni | 3 168 | 20 880 | `renders/review/SandalBedesteni_v1/` |
| Üsküdar Mihrimah | 5 008 | 20 354 | — |
| Cevahir Bedesteni | 2 418 | 16 170 | `renders/review/CevahirBedesteni_v2/` |
| Türbe (Sultan Ahmed) | 938 | 9 450 | `renders/review/TurbeSultanAhmed_v3/` |
| Galata Kulesi | 1 012 | 3 268 | `renders/review/GalataKulesi_v5/` |
| Yedikule | 2 658 | 4 098 | — |
| Mahalle mescidi | 1 928 | 4 236 | `renders/review/Mescit_v1/` |
| Bâbüsselâm | 550 | 3 958 | `renders/review/TopkapiBabusselam_v3/` |
| Adalet Kulesi | 286 | 1 262 | `renders/review/TopkapiAdaletKulesi_v2/` |
| Kara sur burcu | 312 | 576 | `renders/review/KaraSurBurcu_v1/` |

Sur burcu **192 örnek** basıldığı için bilerek küçük tutuldu (sınır 1 500).

## Bakarken şuna dikkat et

1. **Avlu revakı artık gerçek** — sütun, sivri kemer, alınlık, kubbe. Önceki
   hali duvar üstünde düz kubbelerdi.
2. **Taçkapı** beş camide de var: mukarnas kavsaralı niş, kitabe, sövelerle.
3. **Kubbelerde kurşun dikiş çizgileri** (kaburga) — silüeti uzaktan
   tanıtan şey bu.
4. **Galata Kulesi Ceneviz** yapısıdır: kemerleri **yuvarlak**, Osmanlı
   sivrisi değil. Sur burcu da Bizans, o da yuvarlak.
5. **Bâbüsselâm'ın kapısı** artık gerçek bir taçkapı. Yapının adı da
   işlevi de kapıdır — saray halkının dışında herkesin attan indiği eşik;
   onu cephede bir delik olarak bırakmak, yapıyı tanımlayan şeyi
   modellememekti.
6. **Mahalle mescidinin minaresi** mukarnaslı şerefe ve delikli korkuluk
   kazandı. Bu tek değişiklik **şehirdeki bütün mescitleri** yükseltiyor.

## Bulduğum bir şey — sorulacak bir şey değil, ama bilmelisin

Sultanahmet için TDV *"yirmi altı sütun, otuz kubbeli birim"* der ve iki
sayı çelişiyor sanılıyordu. Çelişmiyor: **kapalı** bir revak halkasında
mesnet sayısı göz sayısına eşittir (30) ve dördü sütun değil **köşe
ayağıdır** → 26 sütun. Fâtih bunu bağımsız doğruladı (22 kubbe / 18 sütun,
fark yine dört) ve avlusu 1471'den ayakta. Artık `ClosedArcadeRingHasFour­CornerPiers`
testi iki yapıyı da tutuyor. → [ADR 0056](../decisions/0056-kapali-revak-halkasi.md)

## Yanlış teşhis ettiğim bir şey — ve nasıl yakalandığı

Türbenin duvarında renderda beyaz benekler çıktı. "Mukarnas hücreleri bu
ölçekte fazla küçük" diye yorumladım ve kapının yan nişlerini kapattım.
Belirti azaldı, **sebep duruyordu**.

Gerçek sebep şuydu: `hz.make_box` köşeleri doğrudan mesh'e yazar ve nesne
dönüşümünü kimlik bırakır; bu yüzden kutuyu yerine koyup **sonra**
döndürmek onu kendi merkezi değil **dünya orijini** etrafında döndürür.
Türbe duvarındaki benekler, oraya savrulmuş taçkapı parçalarıydı.

Bunu göz değil **sayı** yakaladı: Yedikule'ye aynı yardımcıyı ekleyince
ayak izi 165,9×161,2 m'den **173,0×174,6** m'ye çıktı — eklediğim 0,4 m'lik
konsolların açıklayamayacağı bir büyüme. Düzeltmeden sonra 166,1×161,2 ve
yan nişler **açıkken de** benek yok.

Not ediyorum çünkü kuralımızın ("render bir gözlemdir, kanıt değil") bu
sefer **düzeltmenin kendisi için** geçerli olduğunu gösterdi.

Bunun üzerine bir bekçi yazdım (`ottoman_kit._donus_denetimi`) ve otuz
üretecin hepsini taradım. **İki mevcut hata daha** çıktı, ikisi de bu
geçişten önceydi:

- **Üsküdar Mihrimah'ın ikinci revağının yan kanat örtüleri** yanlış
  yerdeydi — 90°'lik dönüş örtüyü de taşıyordu.
- **Su terazisinin gelen ve giden künkleri simetrik değildi**; biri 0,95 m
  daha dışarı taşıyordu. Oysa iki künkün anlamı simetrilerinde: su bir
  yerden gelir, bir yere gider.

Bir de **kırılgan** bir yer: değirmen çarkının göbeği doğru duruyordu ama
elle yazılmış bir `−0,39` telafisiyle — göbek boyu değişse sessizce
bozulurdu. → [ADR 0058](../decisions/0058-donus-bekcisi.md)

## Bu geçişte yapmadıklarım

- **Kız Kulesi** (799) ayrıntılanmadı — bilinçli: 1632'de **ahşap**tır,
  taş oymacılığı dili oraya ait değil ve konsol, kuşak, korkuluk zaten
  var. (Yedikule yapıldı.)
- **Doku çözünürlüğü konusunda kendimi düzeltiyorum**: geçen sefer
  "UV ölçekleri kütle için ayarlı, yakın planda gerilmiş görünebilir"
  demiştim. Ölçtüm, öyle değil — UV üretimi **dünya ölçekli ve yüz
  başına** çalışıyor, yeni geometri kendiliğinden doğru texel
  yoğunluğunu alıyor. Yapılacak bir iş yok.
- **Medrese, mektep, tekke ve köşkler** hâlâ dağarcığa bağlı değil. Bu
  turda Topkapı'nın iki yapısı ve mahalle mescidi yapıldı.

## Doğrulama

- EditMode **241/241 yeşil** (yeni test dahil; bayat derleme koruması
  sayının 240→241 çıktığını gördü).
- `Assets/_Import` boş, 28 anıt sahnede.
- Bütün üreteçler kendi sayım denetimlerini geçiyor.

---

**Onay formatı:** `OK v1` (ya da düzeltilecekler)
