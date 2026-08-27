# ADR 0032 — Yerleşimin çiğnediği zemin, ve build sahne listesi

**Tarih:** 2026-08-24
**Durum:** Kabul edildi — üretildi, testli
**Tetikleyen:** Caner: *"karar 12 ve 13 senin önerine bırakıyorum."*
**İlgili:** ADR 0016 (sokak dokusu), 0024 (arazi örtüsü), 0011 (bölge yayını),
0031 (mahalle kabul sahnesi)

---

## 1. Karar 12 — mahalle çayırın üstünde duruyordu

Kaldırım düzeldikten sonra bile mahallenin **kaldırım dışındaki** zemini arazi
örtüsünün çayırıydı: hazire tabanı, avlu çevresi, dükkân önü. Bir mahallenin
içi otlak değildir.

Seçilen yol **(a)**: yerleştirici, yapıların çevresine basılmış toprak boyar.

### Bu, ADR 0024'ün reddettiği şey DEĞİL

Arazi örtüsü kurulurken "mahalle maskesi" bilerek yapılmamıştı ve gerekçesi
hâlâ geçerli: `districts.geojson` kendi içinde *"bu bir OYUN bölgesidir,
mahalle sınırı değildir"* yazıyor; onu yerleşim sınırı gibi kullanmak kendi
uyarımızı çiğnemek olurdu. Surlar da yetmez — 1632'de suriçinin batısı
bostanlıktı.

Yeni maske o iddiayı kurmuyor. Kaynağı bir **sınır** değil, sahneye **fiilen
koyduğumuz yapılar**: yerleştiricinin zaten tuttuğu çakışma daireleri
(`taken` — 98 daire). İddia şu kadar: *"buraya yapı koyduk, o hâlde burası
basılmış topraktır."* Tarih hakkında hiçbir şey söylemiyor.

### Çözünürlük 7,49 m — ve bu dürüstlüktür

Splatmap 2048², arazi 15 337 m: bir texel **7,49 m**, yani DEM'in kendi örnek
aralığı. 4,6 m'lik bir sokak bir texel'den dardır. Bu maske **sokağı boyayamaz,
mahalleyi boyar**; sokağın kendi zemini zaten bir mesh (`Kaldirim`). Daha ince
bir splatmap, olmayan bir bilgiyi taklit etmek olurdu (ADR 0024).

### Kenar yumuşak, geçiş ölçülü

Daire yarıçapı + **6 m** tam basılmış, sonraki **16 m**'de düzgün geçişle
otlağa iner (smoothstep). Sert kenar doğada yoktur; kesme-yapıştır gibi
okunurdu.

Ölçüldü (çekirdekten doğuya): 0–40 m'de toprak **1,00**, 60 m'de 0,89,
100 m'de 0,11 — yani doğal kurala dönüyor.

### Zemin ve yapı AYNI turda güncellenir

`OttomanStreetBuilder` mahalleyi kurduğu anda maskeyi yazıyor ve o bölgeyi
yeniden boyuyor. İki ayrı menü komutu olsaydı bir gün biri unutulur ve
mahalle yine çayırda kalırdı.

Dosya (`data/gis/settlement.json`) **türetilmiştir**, depoya girmez; yoksa
maske boştur ve örtü tam olarak eskisi gibi davranır. Her mahalle **yalnız
kendi kaydını** değiştirir — üzerine yazsaydı Galata kurulunca Balat'ın zemini
otlağa dönerdi (aynı cinsten bir hata bir tur önce kaldırımda yakalanmıştı).

## 2. `alphamapResolution` ATAMAK SPLATMAP'İ SİLER

Kısmi boyama eklenince ortaya çıkan sessiz felaket. `Paint` başında
`data.alphamapResolution = 2048` satırı vardı ve zararsız görünüyordu — tam
boyamada zaten her texel yeniden yazılıyor. Ama Unity bu atamada, **aynı değer
atansa bile**, bütün alphamap'i `(1,0,0,0)`'a döndürüyor.

Sonuç: bütün İstanbul toprağa düştü, geri yazılan tek şey 400 m'lik mahalle
dikdörtgeni oldu.

**Gözle yakalanamazdı.** Kuşbakışı kare "kahverengi bir yamaç" gösteriyordu ve
bu makul görünüyor; hatta doğru bir yamaç gibi. Yakalayan şey örtü testleri
oldu:

```
TL_TerrainGrass arazinin %1'inden azini kapliyor   (0,02%)
Karanin en dik %0,5'inde kaya payi 0,00
Deniz tabani kiyi katmaniyla kaplanmali            (0,00)
```

Üç test, tek satırlık bir yan etkiyi üç ayrı yönden gösterdi. Çözünürlük artık
yalnız **değişiyorsa** atanıyor; kısmi boyama sırasında değişmesi gerekiyorsa
kısmi boyama reddedilip tamamı boyanıyor ve bu loglanıyor.

Doğrulama: kısmi boyamadan sonra bütün arazinin payları tam boyamayla **birebir
aynı** — Toprak %15,9, Ot %46,8, Kaya %1,3, Kıyı %36,0.

## 3. Karar 13 — build listesinde bizim olmayan tek sahne vardı

Kayıtlı tek sahne `Sandbox/OutdoorsScene.unity` idi: HDRP şablonunun boş örnek
sahnesi (kamera, güneş, gökyüzü, ışık probu — hepsi bu). Bugün bir şey
bozmuyordu çünkü build almıyoruz; **Faz 7'de bu hâliyle HDRP örneği
paketlenirdi** ve kimse fark etmezdi.

Seçilen yol **(a)**: şimdi düzeltildi.

Liste artık **koddan** geliyor (`BuildScenes`), gerekçesiyle birlikte, ve elle
değil menüyle uygulanıyor — elle düzeltmek aynı tuzağı bir yıl sonra tekrar
kurardı.

```
0  Assets/_Project/Scenes/Faz1_Terrain.unity     (acilis)
1  Assets/_Project/Scenes/FlightSlice.unity
```

**Semt sahneleri bu listede yok ve olmamalı:** `Districts/D_*.unity`
Addressables ile yükleniyor (ADR 0011). Addressable bir sahne build listesine
de konursa Unity onu **iki kez** paketler. Sandbox ve ölçüm sahneleri de
girmez.

Gerçek açılış akışı (menü → yükleme → şehir) Faz 7'nin kararıdır; bu liste o
güne kadar **doğru** olanı tutar, nihai olanı değil. `BuildScenesTests` dört
gerekliliği kilitliyor: liste boş olmayacak, yalnız bizim sahnelerimiz,
sandbox/bench yok, semt sahnesi yok.

## 4. Ölçüm

- EditMode **151/151** (turdan önce 147; +4 build listesi testi).
- Örtü payları kısmi ve tam boyamada aynı.
- Mahalle inceleme paketi yeniden üretildi: `Captures/mahalle/`.
