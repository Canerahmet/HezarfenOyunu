# ADR 0024 — Arazi örtüsü: kural araziden, doku bizden

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — üretildi ve kuruldu; Caner onayı bekliyor
**Tetikleyen:** ADR 0023 §9 — "arazi dokusu artık en zayıf halka".
**İlgili:** ADR 0005 (varlık hattı), 0007 (DEM), 0019 (prosedürel doku), 0023 (geçici ışık)

---

## 1. Ne eksikti

Faz 1'de arazi DEM'den **doğru ölçekte** geldi ve `TerrainLit` malzemesine
**hiç katman atanmadı**. Katmansız arazi tek düz bir yüzeydir. Bu, ışık gelene
kadar görünmedi: karanlıkta düz zeminle dokulu zemin aynı görünüyor.

Ölçüldü (aynı piksel ölçeğinde, benzer parlaklıkta):

| | ayrıntı enerjisi | ortalama |
|---|---|---|
| Dokusuz arazi (sevk edilen hâl) | **0,45** | 110/255 |
| Dört katmanlı örtü | **3,75** | 103/255 |

Parlaklık neredeyse aynı, ayrıntı sekiz kat — yani değişen şey ışık değil
**yüzeyin kendisi**.

## 2. Dört katman, çünkü beşincisi bedava değil

Unity splatmap'i RGBA dokularda taşır: **4 katman = 1 doku**. Beşinci katman
ikinci bir splat dokusu açar, belleği ikiye katlar ve arazi kabuğunu bir kez
daha örnekletir. Dört, bedava olan sınır:

| Katman | Ne | Karo | Arazi payı |
|---|---|---|---|
| `Earth` | işlenmiş/çiğnenmiş düzlük | 6 m | %15,9 |
| `Grass` | maki + ot — yamacın varsayılanı | 5 m | %46,8 |
| `Rock` | dik yamaçta çıkan anakaya | 9 m | %1,3 |
| `Shore` | deniz seviyesi bandı ve deniz tabanı | 4 m | %36,0 |

Dokular prosedüreldir (`tools/textures/gen_terrain_textures.py`), girdi yalnızca
tohumlanmış sayı üretecidir; **kendi telifimizdir**. Blender'a gitmezler:
tek tüketici `TerrainLit`, ve onları okuyan hiçbir `.blend` yok.

## 3. Eğim eşiği SABİT AÇI olarak yazılamaz

İlk yazımda "kaya 26°'nin üstünde başlar" diyordu. Sonuç: **kaya %0,0**.

Ölçüm sebebi gösterdi — jeoloji değil **ölçek**:

```
karada eğim:  ortanca 5,0°   p95 18,2°   p99 24,3°   en dik 60,4°
26°'yi aşan kara:  %0,60
```

7,49 m örnek aralıklı bir DEM'de eğim 15 m tabanla ölçülür; gerçek bir kaya
yarı ortalamayla silinir. 30 m'lik bir yükseklik modeli 5 m'lik bir sarplığı
gösteremez.

Doğru kural açı değil **oran**: *"karanın en dik ~%5'i çıplak anakayadır."*
Açılar bundan çıkar (`SlopeQuantiles`, yalnız kara üstünden — Boğaz kenarı
DEM'in en dik yeridir ve karışıma girerse yüzdelikleri yukarı çeker). Bugünkü
arazide türetilen değerler **18,2°–26,5°**. DEM yeniden üretilirse eşik
kendiliğinden kayar; **test de aynı yüzdelikleri kullanır**, yani iki taraf
birlikte hareket eder.

## 4. Mahalle maskesi neden YOK

"Yerleşim yerinde çiğnenmiş toprak" doğru bir kuraldır ama uygulanacak veri
yok. `districts.geojson` kullanılabilirdi — kullanılmadı, çünkü o dosya kendi
içinde şunu yazıyor:

```json
"historical_claim": "none — bu bir OYUN bölgesidir, mahalle sınırı değildir"
```

Oyun bölgesini yerleşim sınırı saymak, kendi yazdığımız uyarıyı çiğnemek
olurdu. Surlar da yetmez: 1632'de suriçinin batısı bostanlıktı, baştan başa
yapı değil (RESEARCH.md §4). Toprak bunun yerine **düz ve alçak** yerde çıkıyor
— insanların yerleştiği yer de zaten orası. Bağıntı iddia edilmiyor, doğuyor.

## 5. Orta mesafeyi arazi doldurur, gürültü değil

Yaya 20 m öteyi, uçan 200 m öteyi görür. Arasındaki ölçekte dokunun kendi
ayrıntısı çoktan mip ortalamasına inmiştir ve zemin düz bir levha olur.

Oraya konan değişim gürültü değil **jeomorfoloji**: dışbükey sırt toprağını
kaybeder (ince, kuru, çıplak), içbükey çukur toprak ve nem tutar (otlu).
Ölçüt, noktanın ~30 m yarıçaplı komşularının ortalamasından kaç metre yüksekte
olduğu. Sonuç manzarada görülüyor: kahverengi toprak lekeleri **sırtları
izliyor** — ve arazinin kendisinden geldiği için hiçbir zaman karo tekrarına
benzemiyor.

## 6. Ölçü aleti üç kez değişti — üçünde de aynı ders

Bu turun asıl işi doku değil, dokuyu ölçen alet oldu.

**(a) "Kaba" ölçüt ince taneyi sayıyordu.** Ölçü "her pikselin 20 cm komşu
ortalamasından sapması"ydı. Dokulara yakın ayrıntı eklendiğinde **kaba sayı da
yükseldi** — yani iki bandı ayıramıyordu. Doğrusu önce 20 cm bloklara
indirgemek, sonra o küçük görüntünün kendi enerjisini ölçmek.

**(b) Tek bir "ince/kaba oranı" iki ayrı derdi tek sayıya sıkıştırıyordu.**
Kaya yakın-ayrıntı eşiğini rahat geçtiği hâlde oran yüzünden "zayıf" damgası
yiyordu. İki bağımsız eşiğe ayrıldı: ince ≥ 2,0 (yakından içerik var mı),
kaba ≤ 3,0 (metre ölçeğinde lekelenme).

**(c) Parlaklık ölçütü RENK kusurunu göremedi.** Maki denemesinde çalılar
1,1 m çapındaydı ve 5 m'lik karoya ~4,5 tane sığıyordu; 3×3 döşendiğinde yeşil
lekeler ızgara gibi okunuyordu. Ne "ince" ne "kaba" yakaladı: ikisi de
parlaklık ölçüyor, tekrarı ele veren şey ise **renk**ti. Üçüncü ölçüt Lab'da
kuruldu (`makro-ΔE`) ve kusuru anında ayırdı: DryGrass 1,77, ötekiler 0,49–0,86.

Aynı aile ADR 0023'te de yaşanmıştı ("karanlık ışık" ile "karanlık malzeme"yi
tek sayı ayırt edemez). **Bir bandı ölçmeyen alet, o bandın kusurunu göremez.**

### Palet ayrımı da ölçülür

> **Sonradan:** palet ilkbahara çevrildi (ADR 0025); aşağıdaki ölçümler ve
> dersler geçerli, yalnız renkler değişti.

İlk üretimde dört katmanın da ton açısı **32°–43°** arasındaydı ve Kaya ile
Kıyı'nın ortalamaları arasında yalnızca 5 seviye vardı. Yakından dördü de
doğru görünüyordu; **havadan manzara tek renk bir çöl** oluyordu, çünkü uzakta
doku mip ortalamasına iner ve geriye yalnızca ortalama renk kalır. Ölçüt:
her katman çiftinin CIE76 farkı ≥ 12. Bugün 12,1–24,6.

Kıyıyı **açarak** ayırmak ilk çözümdü ve ölçütü geçti ama yanlıştı: kıyı şeridi
havadan bembeyaz bir kordon gibi okundu. İstanbul kıyısı beyaz kum değil koyu
çakıldır. Ayrım artık parlaklıktan değil **tondan** geliyor.

## 7. Ölçüm ölçtüğü şeyi bozdu

Öncesi/sonrası için katmanları geçici olarak boşaltıp geri koydum:

```csharp
var saved = data.terrainLayers;
data.terrainLayers = new TerrainLayer[0];   // <-- splatmap SIFIRLANIR
...
data.terrainLayers = saved;                 // katmanlar döner, SPLATMAP DÖNMEZ
```

`TerrainData.terrainLayers` ataması alphamap'i sıfırlıyor ve geri atama onu
geri getirmiyor. Arazi baştan sona %100 `Earth` kaldı; sonraki inceleme paketi
bu bozuk hâli gösterdi ve ben bir süre **kuralı** suçladım. Doğrusunu yalnız
örtü haritası gösterdi.

Ders iki katlı: ölçüm aracı ölçtüğü şeyi değiştirmemeli; ve *"kural yanlış"*
demeden önce durumu **doğrudan** oku.

## 8. Örtü haritası

`Captures/faz1_arazi_haritasi.png` — arazinin tamamı, her texel'de baskın
katman renklendirilmiş. Bu tek görüntü, 4,2 milyon texel'lik bir splatmap'in
gözle denetlenebilir tek özeti: Haliç ve Boğaz tanınıyor, toprak kıyı
şeridinde ve alçak düzlükte, kaya vadi yarıklarında, ot yamaçta. Kıyı katmanı
**karada yalnızca %1,9**.

## 9. Kararlar

| Konu | Karar | Gerekçe |
|---|---|---|
| Splat çözünürlüğü | 2048 (7,49 m/texel) | DEM'in kendi örnek aralığı; daha incesi olmayan bilgiyi taklit eder |
| Basemap | 1024, mesafe 2000 m | uçuş oyunu; varsayılan 1000 m ufku bulanıklaştırıyordu |
| Anizotropi | 8 | zemin yatık açıyla örneklenir; izotropik mip uzağı şeride çevirir |
| Yükseklik harmanı | açık | maskenin B kanalı bunun için üretildi; kapalıyken geçiş düz solmadır |
| Doku kökü | doğrudan `Assets/.../Art/Textures/Terrain` | Blender tarafı okumuyor; kanonik kopya kimsenin açmadığı 40 MB olurdu |
| Mevsim | **ilkbahar** | Caner kararı; gerekçe ve güneş düzeltmesi **ADR 0025** |

## 10. Kalan boşluklar

- ~~**Bitki örtüsü yok.**~~ → **ADR 0026'da kapandı**: 45 296 ağaç, belgeli
  alanlara dikildi; Okmeydanı belgeye uyarak **ağaçsız** bırakıldı. Yerine
  geçen boşluk: **asma ve sebze varlığı yok** — bağ sıraları ve bostan tarhı
  ayrı model ister, şimdilik bağ seyrek çınarla temsil ediliyor.
- Karo tekrarının asıl çözümü doku bombalama (stokastik örnekleme) —
  gölgelendirici işi.
- Kaldırım yalnız ana sokakta; mahalle içinde zemin çıplak arazi.
- Kıyı çizgisi 7,5 m'lik texel yüzünden havadan **basamaklı** okunuyor.
- Örtü hiçbir yerde yapıya tepki vermiyor: evin dibinde de, 200 m ötede de
  aynı kural. Mahalle ölçeğinde çiğnenmiş toprak için veri yok (§4).
