# ADR 0055 — İskele ve Alay Köşkü: iki 🟡 satırı kapatmak

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/iskele_ve_alay.md`)
- **Bağlam**: Faz 3, S-kademe'de kalan iki eksik parça.

## Ne kapandı

| satır | eksik olan |
|---|---|
| Üsküdar: Mihrimah + Doğancılar | *"Eksik: iskele"* (ADR 0036) |
| Topkapı silüeti | *"Alay Köşkü 1632'de AHŞAP — kayıtlı, üretilmedi"* (ADR 0040) |

İkisi de **kayıtlıydı ama üretilmemişti**. Kaydın işi buydu: unutulmadılar.

## Karar 1 — İskele caminin adının kaynağıdır

Üsküdar Mihrimah Sultan Camii'nin yaygın adı **"İskele Camii"**dir ve
sebebi yanı başındaki iskeledir. Yani iskele camiden bağımsız bir ayrıntı
değil — **caminin adının kaynağı**. Camiyi üretip iskeleyi bırakmak,
adı açıklayan şeyi eksik bırakmaktı.

**1632'de ahşap**: kâgir rıhtımlar 19. yüzyıldır. Yapısal ahşap
**boyanmaz** (`timber_bare`, ADR 0035).

Konum türetildi: kendi 1632 kıyı çizgimizde camiye **en yakın** nokta
bulundu (134 m) ve oradan ~20 m denize alındı.

## Karar 2 — Alay Köşkü'nde 1632 yapısı bugünkünden **yüksektir**

Bugünkü kâgir köşk **1810** ya da **1819-20**, II. Mahmud'undur. Kaynak
iki şeyi birden söylüyor ve ikincisi beklenmediktir: 16. yüzyılda aynı
yerde **ahşap** bir köşk vardı, ve II. Mahmud'un yapısı **daha yüksek**
bir köşkün ya da kulenin yerine geçti.

Galata Kulesi'nde (ADR 0033) ve Adalet Kulesi'nde (ADR 0040) 1632 yapısı
bugünkünden **alçaktı** ve o iki bulgudan sonra refleks olarak "eski olan
alçaktır" diye düşünmeye başlamıştım. Burada **tersi** çıktı.

Süleymaniye'de öğrenilen kuralın (ADR 0044) başka bir yüzü: kural
"her şey farklıdır" değil, "**her şey sorulur**" — ve farkın **yönü** de
sorulur.

## Karar 3 — İncili Köşk'le aynı aile

İkisi de bir **duvarın üstünde** durur, ikisi de **taşar**, ve ikisinde
de padişah bir şeyi **seyreder**: İncili Köşk'ten Hezarfen'in uçuşu
(ADR 0039), Alay Köşkü'nden devlet ricalinin alayları. Aynı yapı tipi,
aynı işlev ailesi — bu yüzden aynı kitte (`kosk_kit`).

## Üç hata, üçü de ölçümle yakalandı

**1. `face_deg: 0` "bildirilmedi" değil, "kuzeye bak".**
Alay Köşkü'nün kaydına 0,0 yazmıştım ve yerleştirici köşkü kuzeye
çevirdi. Sıfır hem "yok" hem "kuzey" anlamına gelemez. Sözleşme: kuzeye
bakan bir yapı **360** yazar; sıfır ya da negatif bildirilmemiştir.
Yerleştirici artık öyle okuyor ve bir test kataloğun hiçbir yerinde tam
sıfır kalmadığını sınıyor.

O testi de bir kez **yanlış** yazdım: `Load()` üzerinden `face_deg != 0`
diye sınamıştım, ama alanı hiç yazmayan bir varlıkta `JsonUtility` zaten
0 verir — "yazmadı" ile "sıfır yazdı" ayırt edilemez. Ayrımı **ancak ham
metin** taşır; test kataloğun kendisini okuyor.

**2. İskele suya ters uzanıyordu.**
Eksen sözleşmesi (CLAUDE.md): prefabın **+Z**'si ön cephedir ve Blender'da
**−Y**'ye karşılık gelir. İskeleyi +Y'de kurmuştum; yerleştirici +Z'yi
suya çevirdiği için iskele **karaya** doğru uzanıyordu. Ölçüldü
(iskelenin ortası camiye pivotundan daha yakındı) ve çevrildi.

Yön için `Waterward` (en alçak arazi yönü) **yetmedi**: iskele zaten
suyun içindedir ve orada "en derin yön" boğazın **boyunca** çıkabilir.
İskele kıyıya **dik** uzanır; yön kıyı çizgisinin yerel **normalinden**
ölçüldü (**306,8°**) — Yedikule'de kullanılan yöntemin aynısı (ADR 0050).

**3. Alay Köşkü de `kind="kosk"` olunca İncili Köşk'ün sayıları ona
uygulandı** ve iki test patladı ("Sarayburnu tarafında BİR kemer" —
Alay Köşkü'nde öyle bir şey yok). **Üçüncü kez** aynı hata
(Süleymaniye/Mihrimah, ADR 0044): `OfKind` bir **tür** süzgecidir ve bir
tür birden çok yapı içerir. Süzgeç ada çevrildi.

## Ve bekçi ilk işini gördü

Testleri eklerken bir **CS1503** yaptım ve test assembly'si derlenmedi.
Bir gün önce yazılan `CompiledTestCountMatchesTheSource` (ADR 0052)
**patladı**: *"kaynakta 239 [Test], derlenmiş assembly'de 236."*

Bekçi olmasaydı koşum yine yeşil dönecekti ve dört yeni test hiç
koşmayacaktı. Yazıldığı günün ertesinde gerçek bir vakayı yakaladı.

## Sonuç

- `UskudarIskelesi` LOD0 884 (8,2 × 34,2 m, ahşap, 9 kazık çifti);
  yön **306,8°** — uzak ucunun altındaki arazi **−12,0 m** ✓
- `AlayKosku` LOD0 284 (17,0 × 13,6 × 19,1 m, ahşap, 2,2 m taşma);
  yön **279,4°** (dışa).
- Sahnede **28 landmark**. EditMode **240/240**.

## Açık kalanlar

- Alay Köşkü'nün yönü **ölçülmedi**: sarayın merkezinden köşke giden
  yönden türedi (dışa bakar). Eğimden gelen 90° köşkü sarayın **içine**
  çeviriyordu; bu daha iyi ama ölçü değil.
- İskelenin ölçüsü yok (**D3**); kayık, kayıkhane içi ve merdiven yok.
- Doğancılar meydanının zemini ve çınarları hâlâ yok (Faz 4).
