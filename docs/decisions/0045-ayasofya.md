# ADR 0045 — Ayasofya: kaynak çelişince plana bakmak

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/ayasofya.md`)
- **Bağlam**: Faz 3, A-kademe. Şehrin en tanınan yapısı ve 1632'de dünyanın
  en büyük kubbesi.

## Karar 1 — Bizans kubbesi ayrı bir kittir

`sinan_kit`in kubbe zinciri (ADR 0036) Osmanlı oranını varsayar:
`DOME_RISE_RATIO = 0.78`. Ayasofya'nın kubbesi 558'de çöktü ve 562'de
**yükseltilerek** yeniden kuruldu; ölçülen oran **0,909** (15,00 / 16,50).
Osmanlı oranı uygulansa kilit 55,60 m'den **42,4 m**'ye düşerdi.

Yeni bir kit (`ayasofya_kit`) yazıldı ve `validate` 0,78'i **reddediyor**.
Ekonomi ("zaten kubbe zinciri var") burada yanlış cevaptır: zincir aynı,
**besleyen oran** farklı.

## Karar 2 — Zincir bağımsız olarak doğrulandı

Ölçülen kilit ve çaptan türeyen iki kot, plandan bağımsız okunan kütle
basamaklarıyla karşılaştırıldı:

| | türetilen | plandan | fark |
|---|---|---|---|
| kubbe kaidesi | 40,60 | 41,0 | 0,40 |
| kemer uzengisi / yan nef çatısı | 24,10 | 25,0 | 0,90 |

İki bağımsız yol bir metrenin altında buluşuyor. Zincir Osmanlı
camilerinde kurulmuştu; Bizans oranıyla beslendiğinde de tutması onun bir
**üslup kuralı değil geometri** olduğunu gösteriyor. Bu bir test.

## Karar 3 — Kaynaklar çelişince **plana** bakıldı

Dört minarenin hangisinin tuğla olduğunda üç kaynak üç şey söylüyor: TDV
güneybatı, bir kaynak güneydoğu, bir başkası kuzeydoğu. Kaynak seçmek
yerine gövde çapları ölçüldü:

* **doğu çifti Ø3,6 m** — ince, birbirinden farklı,
* **batı çifti Ø4,0 m** — kalın, birbirinin **ikizi** (Sinan; II. Selim'in
  siparişi, III. Murad'ın ilk yıllarında tamam).

Tuğla minare **tektir**, yani bir ikiz çiftin üyesi olamaz. Ölçü böylece
TDV'nin "güneybatı" iddiasını **eler** ve tuğlayı doğu köşesine bırakır.
Hangi doğu köşesi olduğu hâlâ **D3**; modeli bağlayan şey köşe değil,
**gövde kalınlıkları ve renk**.

Konumlar da simetrik değil (kuzey çifti eksenden 39,5 m, güney çifti
33,1 m). Minareler farklı yüzyıllarda, var olan payandalara dayanarak
eklendi; simetri yapmak burada bir düzeltme değil **bozma** olurdu.

## Karar 4 — Ayasofya kıbleye dönük değildir

Ölçülen eksen azimutu **123,5°** (apsis), ızgara kıblesi **150,40°** —
arada **26,9°**. Yapı bir kilisedir; mihrap apsise **eğik** oturtulmuştur.
Katalog `face_deg = 303,5` bildirir ve bildirilen yön kıbleyi yener
(ADR 0040'ta Bâbüsselâm için açılan kapının üçüncü kullanımı, ve ilk kez
bir **caminin** kıbleden muaf tutulması).

## Karar 5 — Testin varsayımı bir yapı tarafından çürütüldü

`DeclaredFacingOverridesDerivedFacing` şöyle diyordu: *"kıble kuralını
kullanan bir yapı `face_deg` bildiremez — yoksa iki kural birbirini
sessizce eziyor demektir."* Ayasofya bunu düşürdü: 1632'de bir camidir ve
kıbleye dönük **değildir**.

Testi silmek yanlış olurdu — yakalamak istediği hata gerçek. Kural
**yasaktan bildirim zorunluluğuna** çevrildi:

* bir cami kıbleden sapabilir,
* ama sapma **10°'den büyük** olmalı (yoksa iki kaynak aynı şeyi söylüyor
  ve hangisinin kazandığı belirsiz — testin asıl derdi buydu),
* ve `qibla_offset_deg` olarak **kayıtlı**, bildirilen yönle **tutarlı**
  olmalı.

İstisna serbest değil; **beyanlı**.

## Karar 6 — Eksedralar mesh'te **yok**

Dört eksedra plandaki gerçek bir sayıdır ve iki kez modele kondu, iki kez
görünmedi: kilit kotları ~30 m, çevrelerindeki klerestori bloku **31 m**.
Gömülü kalıyorlar — çünkü gerçekte de öyleler; eksedralar **iç mekân**
öğesidir.

Bu, Süleymaniye dersinin tersidir (ADR 0044): orada avluyu atlamak yapının
yarısını silmişti. Buradaki tuzak simetriği — sayılan bir değeri
*"geometriye bağlamış olmak için"* görünmeyen yere gömmek. **Kendi
denetimimi geçmekten başka işi olmayan geometri**, katalogda yaşayıp
meshte yaşamayan sayının aynasıdır. Katalog `exedrae_interior: 4` yazar,
üretici mesh'te aramaz.

## Ölçü hatası: "82 × 73 m" yapının neresi?

İlk kurulum yayımlanan **82 × 73 m**'yi uçtan uca sandı ve render'da
minareler yapıdan 12 m ötede, boşlukta duruyordu. Plan ölçülünce anlaşıldı:
82 m yalnızca **ana kütledir**, dış narteksi ve apsisi saymaz. Uçtan uca
**106,3 × 75,4 m**.

Yani kubbe çapındaki iç/dış ikiliğinin **plandaki eşi**: tek sayı, yapının
neresini kastettiğini söylemiyor. Aynı tuzak bu turda iki kez kuruldu.

Payandanın taşması da böyle çözüldü: yan nef kabuğu 66,0 m, toplam
75,4 m → payanda her yanda **4,7 m**. Taşma tahmin edilecek bir şey değil,
**iki ölçünün farkı**.

## Sonuç

- `Ayasofya` LOD0 4 448; ayak izi 84,2 × 116,5 m, yükseklik 69,00 m.
- Yerleşim (550,7, 48,8, −1888,4), yön **303,5°**; tepe **117,8 m**
  (Süleymaniye 124,8 m ile en yüksek olmayı sürdürüyor).
- Kubbe **33,0 m** — 1632'de şehrin (ve dünyanın) en büyüğü; teste bağlandı.
- Sahnede **16 landmark**. EditMode **201/201**, atlanan yok; PlayMode 9/9;
  Blender öz-testi 11/11 (yeni: kubbe dilimleri sayılan kaburga sayısını
  taşıyor).

## Yükümlülük

Eksen azimutu ve minare gövde çapları **OpenStreetMap** izlerinden
türetildi (ODbL). Veri depoya girmedi, ölçüler okundu — ama üretilen eser
**atıf ister**. `refs/LICENSES.md`'ye eklendi ve Copernicus'un yanına,
oyun içi Krediler ekranına girecek.

## Açık kalanlar

- **Dış cephe rengi** Caner'e soruldu: bugünkü okra ve Fossati'nin kırmızı
  yatay şeritleri (1847-49) 1632'de **yok**; model kâgir duvar olarak
  duruyor ve bu bir **D3** seçimdir.
- Minare **boyu** (60 m) ve **şerefe sayısı** (birer) ölçülü kaynağa
  dayanmıyor — D3.
- Üç imparator türbesi (II. Selim 1577, III. Murad 1599, III. Mehmed 1608)
  1632'de ayakta ve **üretilmedi**: ayrı varlık olacak (`LM_AyasofyaTurbeler`),
  çünkü kubbe merkezinden 71-85 m güneydedirler ve aynı varlığa katmak
  çarpışma kutusunu 180 m'ye çıkarırdı.
