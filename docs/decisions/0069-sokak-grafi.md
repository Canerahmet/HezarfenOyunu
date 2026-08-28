# ADR 0069 — Sokak grafı: NavMesh değil, yerler

**Durum:** Kabul edildi (uygulandı)
**Tarih:** 2026-08-28
**Bağlam:** Faz 6, NPC navigasyonunun temeli

---

## Karar

NPC navigasyonu **NavMesh üzerinde değil**, sahneden okunan bir
**yer grafı** üzerinde yürür.

## Neden

**NavMesh serbest yüzey verir:** ajan iki nokta arasında yürünebilir her
yerden geçer. 12 248 evlik bir şehirde bu hem pahalıdır hem **yanlıştır**
— 17. yüzyıl İstanbul'unda insanlar avludan avluya değil sokaktan yürür,
ve avlu duvarı bir engel değil bir **mahremiyet sınırıdır** (mahalle
dokusunun kurulduğu ilke, ADR 0062).

**Graf rutinin dilini konuşur.** NPC "12 metre kuzeye git" demez, "öğle
ezanında mescide, sonra dükkâna" der. Düğümler yer değil **yerler**dir:
mescit, çeşme, fırın, dükkân, avlu kapısı, mektep, iskele.

## Düğümler üretilmez, sahneden OKUNUR

Faz 4 sokak omurgalarını hesaplayıp attı; o kayıt yok. Yeniden üretip
kaydetmek 1,5 milyon satırlık sahne diff'i ve LFS'e kalıcı ikinci bir
kopya demekti (CLAUDE.md, yeniden üretim gürültüsü). Sahnede zaten duran
prefab'lar okunuyor — hem ucuz, hem tek doğruluk kaynağı sahnede kalıyor.

**1530 düğüm, 5379 kenar.** 130 mescit, 568 dükkân, 272 çeşme, 142 ev
kapısı, 130 mektep…

## Bağlantı iki katmanlı

İlk yazımda tek yarıçap (95 m) vardı ve graf **paramparça** çıktı: en
büyük bileşen %2. Sebep yapısaldı — bir mahalle sıkı bir cep, mahalleler
arası mesafe o yarıçapın çok üstünde. Şehir 130 ada oldu.

Doğru yapı: cep içinde **sık** (en yakın 5 komşu), cepler arasında
**seyrek ama uzun** (farklı bileşenlerdeki en kısa çiftler, Kruskal ile).
Osmanlı mahallesi zaten böyledir: içeride çıkmaz sokaklar, dışarıya
birkaç geçit. Bu, grafın bağlı olmasını bir umut değil bir **sonuç**
yapar.

## Kenar bir varsayım değil, bir ÖLÇÜMDÜR — ama doğru şeyi ölçmeli

İlk denetim yalnızca **eğime** bakıyordu ve **hiçbir aday reddedilmedi**.
Sıfır ret, testin çalıştığının değil çalışmadığının işaretiydi: Haliç'in
tabanı yumuşak eğimlidir, dik değil. Eğim testi yanlış soruyu soruyordu.

**Su denetimi** eklendi (`y < 0,6 m` = kara değil) ve ret sayısı
0 → **4790** oldu. Bileşen sayısı 28 → 5. Bu denetim olmadan NPC Haliç'i
yürüyerek geçerdi.

Örnek sayısı da uzunlukla artıyor: 1 km'lik bir kenarda sabit sekiz örnek
125 metrede bir bakmak demek ve arada koca bir koy sığar.

## Bileşen sayısı bir hata değil, bir OLGU olabilir

Tek bir "%36 bağlı" sayısı burada teşhis değildi: 1632'de Haliç'te köprü
yok ve Boğaz'ı yürüyerek geçemezsin. Grafın kara parçalarına bölünmesi
**tarihsel olgudur**; ulaşım kayıkladır (RESEARCH §6: *"Haliç'te köprü
olmaması bir eksik değil, ulaşım mekaniğidir"*).

Doğru soru "graf bağlı mı" değil, **"her semt kendi içinde bağlı mı"**.
Ölçüm buna geçince tablo okunur oldu:

| semt | düğüm | en büyük parça |
|---|---:|---:|
| Suriçi Doğu | 462 | **%100** |
| Üsküdar | 325 | **%100** |
| Suriçi Batı | 277 | **%100** |
| Eyüp | 103 | **%100** |
| Galata | 358 | %85 |

## Galata'nın %85'i: sayı değil, yer

Kopuk parçanın **koordinatı soruldu** ve cevap geldi: 53 düğüm, kulenin
**1330 m batısı, 479 m güneyi**, 519 m yayılım. Yani **Kasımpaşa** —
Galata'dan bir dere vadisiyle ayrılan, tersaneye ait ayrı yerleşim.

Oraya zorla bir yaya kenarı çakmak, **olmayan bir köprü uydurmak**
olurdu. Bağlantıyı graf değil **ulaşım mekaniği** verecek: kayık ağı.

Bu yüzden iki eşik var — %50'nin altı gerçekten bozuk (hata), %50-90
arası coğrafyanın ayırdığı bir cep (uyarı).

## Merdiven ayrımı

Galata bir yamaçtır ve yokuş sokakları merdivenlidir; insanlar oralardan
yürür. Kısa ve dik = merdiven (46°'ye kadar), uzun ve dik = uçurum (34°).
Bir kilometrelik merdiven yoktur.

*Not: bu ayrım eklendiğinde sayılar **hiç değişmedi** — Galata'nın cebini
ayıran şeyin eğim değil su olduğunu gösteren şey de bu oldu. Bir
düzeltmenin hiçbir şeyi değiştirmemesi de bir ölçümdür.*

## Doğrulama

`SokakGrafiTests` (6 test): türlerin varlığı, semt içi bağlılık,
**hiçbir kenarın sudan geçmemesi**, kenar uzunluklarının uçlarla
tutması, A*'ın komşudan komşuya gerçek yol bulması, tür süzgeci.

282 test yeşil, sıfır atlanan.

## Sırada

Kayık ağı: iskele düğümleri + kayık kenarları. Şu an grafta **tek**
iskele var (`PF_UskudarIskelesi`); Galata, Eminönü, Kasımpaşa
iskeleleri üretilecek ve Kasımpaşa'nın kopukluğu orada kapanacak.
