# ADR 0043 — Koordinat denetimi: araziyi koordinata tanık yapmak

- **Tarih**: 2026-08-26
- **Durum**: Kabul
- **Bağlam**: Faz 3 boyunca **beş** konum hatası çıktı; A-kademenin tamamı
  aynı katalogun üstüne kurulacaktı.

## Sorun

Elle girilmiş (`approx`) koordinatlar **sessizce yanlıştır**. Faz 3'te
bulunanlar: Doğancılar **771 m**, Okmeydanı **700 m**, Üsküdar Mihrimah
**164 m**, İncili Köşk **156 m**, Yeni Cami **148 m**.

Hiçbiri gözle görülmedi. Her biri ancak **başka bir ölçümle çeliştiğinde**
ortaya çıktı — biri literatürdeki uçuş mesafesiyle, biri külliyenin kendi
belgeli göreli konumlarıyla, biri "denize taşan yapı 125 m içeride"
çelişkisiyle. Yani hatayı bulan şey her seferinde **tesadüftü**.

## Karar — arazi, koordinatın tanığıdır

Bir yapının nerede olduğu hakkında arazinin söyleyecek sözü vardır ve bu
söz **ölçülebilirdir**:

* tepesini taçlandıran bir cami **yerel zirveye yakın** durmalı,
* bir tersane **suyun kenarında** olmalı,
* bir ok meydanı **düz** olmalı.

`LandmarkAudit` bu iddiaları landmark başına sorar ve şüphelileri
listeler. **Amacı koordinatı düzeltmek değil, hangisine bakılacağını
söylemek.**

### Sonuç: 16 şüpheliden 3'ü

Denetim `approx` etiketli 16 koordinatı taradı ve **üçünü** işaretledi;
on üçü için kaynak aramak gerekmedi. İşaretlenenlerin üçü de gerçekten
yanlıştı:

| | önce | sonra |
|---|---|---|
| **Yavuz Selim** (beşinci tepeyi taçlandırır) | zirvenin 27,7 m altında | **4,7 m** — kot 51,9 → 63,8 |
| **Tersane** (gemi suya iner) | sudan 255 m, kot 14,3 | **20 m**, kot **1,4** |
| **Yedikule** | zirveden 11,4 m aşağıda | **5,4 m** |

Yavuz Selim ve Yedikule için ölçülü koordinat bulundu (Kültür Envanteri);
Tersane bir nokta değil bir **hat** olduğu için kendi 1632 kıyı
çizgimizde ölçüldü — kayma 247 m.

**Beyazıt bilerek düzeltilmedi**: zirvenin 10,4 m altında ama Beyazıt
Camii ikinci tepenin *omzundadır*, zirvesinde değil. Denetim listesine de
alınmadı; bir sınırı yapıya uydurmak, sınırı anlamsız kılar.

## Atlanan test, geçen test gibi görünür

Denetimi teste bağladığımda test sahnedeki `TR_Istanbul`'u arıyordu ve
**her koşumda atlandı** — ADR 0041'de eleştirdiğim hatanın aynısını aynı
gün ikinci kez yaptım. Arazi bir **varlıktır**: `LandmarkAudit` artık
`TerrainData`'dan okuyabiliyor ve test gerçekten koşuyor.

## Sonuç

- Yeni araç: **`Hezarfen → GIS → Landmark konumlarını denetle`**.
- Yeni bekçi: `EveryLandmarkPositionAgreesWithTheTerrain`.
- **31 landmark, 0 şikâyet.** EditMode 190/190, atlanan yok.

## Açık kalanlar

- Denetimin beklenti listeleri (`CrownsAHill`, `OnTheShore`,
  `NeedsFlatGround`) **elle** yazılıyor. A-kademe büyüdükçe genişletilmeli;
  listede olmayan bir landmark denetlenmez ve bu sessizdir.
