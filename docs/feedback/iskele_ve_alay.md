# Üsküdar iskelesi ve Alay Köşkü — inceleme notu

İnceleme paketi: `renders/review/AlayKosku_v1/contact_sheet.png`
Karar kaydı: **ADR 0055**. Araştırma: RESEARCH.md §5.20.

## İki eski borç kapandı

| satır | eksik olan |
|---|---|
| Üsküdar: Mihrimah + Doğancılar | *"Eksik: iskele"* |
| Topkapı silüeti | *"Alay Köşkü 1632'de AHŞAP — kayıtlı, üretilmedi"* |

İkisi de **kayıtlıydı ama üretilmemişti**. Kaydın işi buydu: unutulmadılar.

## İskele caminin adının kaynağı

Üsküdar Mihrimah'ın yaygın adı **"İskele Camii"** ve sebebi yanı
başındaki iskele. Camiyi üretip iskeleyi bırakmak, adı açıklayan şeyi
eksik bırakmaktı.

1632'de **ahşap** (kâgir rıhtımlar 19. yy). Yönü kıyı çizgisinin yerel
**normalinden** ölçüldü — "en alçak arazi yönü" yetmiyor, çünkü iskele
zaten suyun içinde ve orada en derin yön boğazın *boyunca* çıkabilir.
İskele kıyıya **diktir**.

## Alay Köşkü: bu kez fark ters yönde

Galata Kulesi'nde ve Adalet Kulesi'nde 1632 yapısı bugünkünden
**alçaktı** — iki bulgudan sonra "eski olan alçaktır" refleksi oluşmuştu.

Burada **tersi**: bugünkü kâgir köşk 1810/1819-20'dir ve kaynak o yapının
**daha yüksek** bir köşkün yerine geçtiğini söylüyor. Yani 1632'de Alay
Köşkü bugünkünden **yüksek**.

Süleymaniye'de yazdığım kuralın başka bir yüzü: kural "her şey
farklıdır" değil, **"her şey sorulur"** — ve farkın **yönü** de sorulur.

## Üç hata yaptım, üçü de ölçümle çıktı

1. **`face_deg: 0` yazdım.** Yerleştirici onu "kuzeye bak" diye okudu ve
   köşkü kuzeye çevirdi. Sıfır hem "yok" hem "kuzey" olamaz. Sözleşme
   artık açık: kuzeye bakan **360** yazar. Bunu sınayan testi de bir kez
   **yanlış** yazdım — alanı hiç yazmayan varlıkta değer zaten 0 gelir,
   yani ayrımı ancak **ham metin** taşır.
2. **İskele suya ters uzanıyordu.** Prefabın +Z'si Blender'da −Y'dir;
   ben +Y'de kurmuştum, iskele karaya doğru uzanıyordu. "İskelenin ortası
   camiye pivotundan uzak mı" diye ölçünce çıktı.
3. **Alay Köşkü de `kind="kosk"` olunca İncili Köşk'ün sayıları ona
   uygulandı** — "Sarayburnu tarafında BİR kemer" diye patladı. Üçüncü
   kez aynı hata: tür süzgeci bir türü değil bir **yapıyı** aramamalı.

## Ve dünkü bekçi ilk işini gördü

Testleri eklerken bir derleme hatası yaptım. Dün yazdığım
`CompiledTestCountMatchesTheSource` **patladı**: *"kaynakta 239 [Test],
derlenmiş assembly'de 236."*

Bekçi olmasaydı koşum yeşil dönecekti ve dört yeni test hiç
koşmayacaktı. Yazıldığı günün ertesinde gerçek bir vaka yakaladı.

## Sana sorduğum

1. **Alay Köşkü'nün yönü** ölçülmedi: sarayın merkezinden köşke giden
   yönden türettim (dışa bakar, 279,4°). Eğimden gelen 90° köşkü sarayın
   **içine** çeviriyordu. Daha iyi ama ölçü değil.
2. **İskelenin ölçeği** (34 × 6 m) tipolojik. Üsküdar iskelesi 1632'de
   bundan büyük müydü?

## Bilerek eksik

- Kayık, kayıkhane içi, iskele merdiveni.
- Doğancılar meydanının **zemini ve çınarları** — Hezarfen'in indiği yer.
  Faz 4.
- Sur-ı Sultanî'nin geri kalanı ve saray kütle denizi — Faz 4.

---

**Onay**: _(bekliyor — "OK vN" yaz)_
