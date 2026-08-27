# ADR 0023 — Geçici aydınlatma: eksik olan ışık değil, sıçrama

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — **GEÇİCİ**; Faz 7'nin ilk işi bunu silmek
**Tetikleyen:** Caner (2026-08-21): *"istersen önden geçici bir aydınlatma yap,
sonra plana göre daha detaylı bir ışıklandırma yapılır."*
**İlgili:** ADR 0019 §11, 0021 §7, 0022 §7 (üç turdur açık duran boşluk);
PLAN.md §12

---

## 1. Neden şimdi

Üç turdur her raporun sonunda aynı cümle vardı: *"sokak seviyesinde
bakılamıyor."* Bu, bir yapı boşluğu değil bir **iş görme** boşluğuydu — yaya
seviyesinden inceleme paketi üretilemediği için yeni yapı üretmeye devam etmek
körlemesine çalışmak demekti.

## 2. Sebep "ışık yok" değildi

Sahneyi açıp baktım, tahmin etmedim:

```
LIGHT SUN_Directional  42° / 205°  100 000 lux  gölgeli
VOLUME SkyAndFog_Global  VisualEnvironment + PhysicallyBasedSky + Fog + Exposure
       skyAmbientMode = Dynamic      ← gök ışığı GELİYOR
       Exposure = Fixed, EV 14,5
```

Yani gökyüzü ve ortam sondası çalışıyordu. Eksik olan **sıçrama** terimiydi:
HDRP'de gerçek zamanlı küresel aydınlatma yoktur; ışık pişirilmediği sürece bir
duvar yalnızca güneşi ve göğü görür, karşı duvardan ve yerden döneni görmez.
Dar bir Osmanlı sokağında gölgeyi asıl dolduran şey ise odur — kireç badanalı
cepheler ve taş kaldırım çok iyi yansıtır.

İkinci sebep pozdu: EV 14,5 yaz öğle değeridir, sahnenin güneşi 42°'de. Ölçüm
bunu tek başına gösterdi — **güneş gören zemin bile 90/255**'te kalıyordu.

## 3. Takım

Üçü de fizikî değil; bu yüzden **ayrı bir kök nesnede ve ayrı bir Volume'da**
yaşıyorlar (`GECICI_Aydinlatma`, öncelik 100). Kaldırmak tek komut. Geçici
olduğunu yorumda söylemek yetmez — **yapının kendisi söylemeli.**

| | Değer | Ne yerine geçiyor |
|---|---|---|
| `IndirectLightingController` | 2,4× | gök teriminin çarpanı = sıçramanın kabası |
| `FILL_Gok` (gölgesiz, spekülersiz) | 11 000 lx, güneşin **karşı** azimutu | gök sarması |
| `FILL_Sicrama` (gölgesiz, spekülersiz) | 6 500 lx, **aşağıdan yukarı** | yer sıçraması |
| `Exposure` | Fixed, **EV 13,0** | 14,5'in düzeltmesi |

Dolgular **gölgesiz** olmak zorunda: ikinci bir gölge kaynağı güneşinkiyle
çakışır ve sahne iki güneşli görünür. **Spekülersiz** de olmalı — sahte bir
kaynağın yansıması metalde ve camda hemen yakalanır (kurşun kubbeler, ADR 0021).

Dolgu yönü güneşten **türetilir**, elle yazılmaz: güneş dönerse dolgu da döner.

## 4. Aleti üç kez değiştirmek zorunda kaldım

Bu turun asıl işi ışık değil, **ışığı ölçen alet** oldu.

### 4.1 Yanlış yere bakıyordu (iki kez)

İlk ölçüm "çekirdeğin 14 m önü"ydü ve orası avlu duvarının dibine düştü: kare
2 m ötedeki bir duvarla doluydu. İkinci deneme sokak koridoruna baktı ve karenin
yarısını yamacın çıplak arazisi kapladı — sayı mimariyi değil araziyi ölçüyordu.

Gereklilik neyse ölçü o olmalı: **gölgede kalan bir cephenin dokusu okunuyor
mu.** Kadraj artık o cepheyle dolar (8 m, göz hizası, güneşten yüz çevirmiş ilk
ev). Göz **arazinin** üstünde, evin değil — bir kez ev taş kaidesinin üstünde
durduğu için göz yerden 3,03 m yukarı çıkmıştı.

Ev seçimi de belirleyici değildi: evlerin çoğu aynı prefab adını taşıyor,
`List.Sort` eşitlikte kararsız ve aynı sahne iki koşumda iki farklı eve
bakıyordu. Sıra artık **kardeş sırası** — ki o da sokak boyunca gitmek demek.

### 4.2 Yanlış ŞEYİ ölçüyordu

Ölçü "30/255'in altındaki piksel oranı"ydı. Balat'ın paleti **bilerek** koyudur
(zimmî renk kısıtı: koyu ahşap, gri sıva) ve gayet okunabilir bir Balat cephesi
**%56 "okunmaz"** çıktı. Karanlık ışıkla karanlık malzeme aynı sayıyı veriyordu.

Doğru soru parlaklık değil: **doku deseni görünüyor mu.** Ölçü artık *ayrıntı
enerjisi* — her pikselin 3×3 komşu ortalamasından sapmasının ortalaması. Ezilmiş
siyahta sıfıra iner, doku okunduğunda yükselir, malzemenin koyu olmasından
etkilenmez:

| | takımsız | takımla |
|---|---|---|
| Galata (kireç badana) | 0,53 | **2,28** |
| Balat (koyu palet) | 0,46 | **2,22** |

İki semtin parlaklığı iki kat farklı, ayrıntı enerjisi aynı. Alet artık doğru
şeyi ölçüyor.

## 5. Ölçüm ANINA bakan iki tuzak

**Volume kaydı.** Sahne diskten açıldığında `Volume` bileşenleri Volume
yöneticisine henüz kayıtlı olmayabiliyor; bir güncelleme tıkı geçmeden yapılan
`Camera.Render()` o sahnenin Volume'larını hiç görmüyor. Aynı sahne, aynı kod,
aynı bakış açısı: biri 18,8/255, öteki 73,2/255. Ölçüm artık render'dan önce
Volume'ları yeniden kaydediyor ve 8 ısınma karesi çiziyor (fizik tabanlı
gökyüzünün ortam sondası da tek karede hazır olmuyor).

**Profil diske hiç yazılmamış.** Asıl hata buydu ve tanıdık bir aileden:
`VolumeProfile.Add<T>()` bileşeni yalnızca **bellekte** kurar. Kalıcı olması
için `AssetDatabase.AddObjectToAsset` ile profilin alt varlığı yapılması şart.
Yapılmayınca profil diskte **bomboş** kaydedildi ve sahne yeniden açıldığında
poz ile dolaylı çarpan yok oldu; geriye yalnız dolgu ışıkları kaldı.

Sinsiliği şurada: sahne "aydınlatılmış" **görünüyordu**, çünkü ışıklar vardı.
Eksik olanı yalnızca sayı gösterdi. Aynı hata daha önce `HistoricalTag`'te
yaşanmıştı (ADR 0021 öncesi): *oturum içinde çalışan, yeniden yüklendiğinde
sessizce kaybolan durum.*

## 6. Poz nasıl seçildi

Gözle değil, süpürerek. Ölçüt "güzel" değil **işe yararlık**: gölgedeki cephe
okunsun, hiçbir şey patlamasın.

```
 EV     golgedeki cephe (ort)   kusbakisi p99   patlak%
 14,5          23                  —              —
 13,2          —                  179            0,00
 13,0          73                  —              —
 12,5          92                 206            0,00
 12,0         113                  —              —
```

12,5 ve altı hiçbir şeyi patlatmıyor ama sahneyi **kapalı hava gibi
düzleştiriyor**. 13,0 seçildi: gölge okunuyor, gündüz kontrastı duruyor.

## 7. Test gerekliliği kilitler, uygulamayı değil

`LightingTests.StreetIsReadableAtEyeLevel` ayrıntı enerjisinin **1,2**'nin
üstünde olmasını ister. Takım silinip yerine pişirilmiş GI konduğunda test
yerinde kalır ve hâlâ doğru şeyi ölçer. Takımın varlığını sınayan bir test,
kalıcı çözüm geldiğinde yanlış yere düşerdi.

Testin sahneyi **tek** olarak açması da ölçüldü: ek (additive) açılışta önceki
sahnenin küresel Volume'u ve güneşi de yüklü kalıyor, iki gökyüzü aynı öncelikte
yarışıyor ve aynı sokak %12 yerine %52 okunmaz çıkıyor. Geometri ölçen testler
bundan etkilenmez; **render ölçen bir test sahne yalıtımı olmadan yalan söyler.**

## 8. Işık gelince görünen ilk kusur

Fırının kemer başlığının arkası açıktı: sokaktan bakınca açıklığın üstünden
**çatının altı** görülüyordu. Karanlık levha yalnız basma kotuna kadar
çıkıyordu. Handa ve medresede aynı açıklığın arkası avludur, yani görmek
doğrudur — fırının arkası kapalı bir odadır. Kusur karanlıkta üç tur boyunca
görünmedi; bu, aydınlatmanın neden bir "cila" işi olmadığının kanıtı.

## 9. Kalan boşluklar

- Takımın kendisi geçici ve **fizikî değil**: gölgeler renksiz doluyor, gerçek
  sıçramanın renk taşıması (kireçten beyaz, kiremitten kızıl) yok.
- ~~**Arazi dokusu** artık en zayıf halka~~ → **ADR 0024'te kapandı**
  (dört katmanlı örtü; ayrıntı enerjisi 0,45 → 3,75). Yerine geçen boşluk:
  **bitki örtüsü** — arazi artık dokulu ama üstünde ne ağaç ne servi kütlesi
  var; manzaranın çıplaklığı buradan geliyor.
- Kaldırım yalnız ana sokakta; çoğu yerde zemin çıplak arazi.
- Volumetrik sis kapalı (`enableVolumetricFog = False`) — Haliç sabahı Faz 7.
