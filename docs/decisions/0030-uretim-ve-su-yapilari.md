# ADR 0030 — Üretim ve su yapıları: Faz 2b'nin kalan yedisi

**Tarih:** 2026-08-23
**Durum:** Kabul edildi — üretildi, testli, Caner onayı bekliyor
**Tetikleyen:** Caner: *"devam edelim. faz 3'e geçmeden duralım."*
**İlgili:** ADR 0017 (kamusal kit), 0020/0021/0022 (civic), 0027/0028 (Okmeydanı)

---

## 1. Yedi madde, tek kit

Faz 2b'nin listesinde kalan yedi yapı üretildi: **orta ölçek cami, imaret,
arasta, bozahane, değirmen, su terazisi, muvakkithane**. Altısı yeni bir kitte
(`works_kit.py` — üretim, ticaret ve su yapıları); orta ölçek cami mevcut
`mosque_kit`in bir **parametresi** olarak çıktı, yeni kod gerekmedi.

Ortak yanları şu: hiçbiri anıt değil, hepsi şehrin **çalışan** parçası.

| Varlık | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|
| `Imaret_A` | 30,70 × 15,66 | 9,62 | 1 094 |
| `Arasta_A` | 28,30 × 12,50 | 6,96 | 4 072 |
| `Bozahane_A` | 7,00 × 9,40 | 5,55 | 588 |
| `Degirmen_Su` | 8,34 × 9,47 | 4,90 | 578 |
| `SuTerazisi_A` | 2,75 × 3,85 | 10,41 | 120 |
| `Muvakkithane_A` | 6,10 × 5,50 | 4,25 | 140 |
| `Cami_Orta` | (kubbeli, 13 m harim) | 27 m minare | — |

Ayrıca dört varyant: `Imaret_Kucuk`, `Arasta_Acik`, `Degirmen_At`,
`SuTerazisi_Kisa`.

## 2. İkisi TARİH riski taşıyordu, ikisi de çözüldü

**Bozahane — oyunun İKİNCİ zaman işareti.** IV. Murad'ın emriyle yapılan 1638
esnaf sayımında İstanbul'da **300 bozahane** ve ~1100 bozacı vardır; ayrıca
sarhoş edecek kadar alkollü **acı boza** üreten ~40 esnaf. Bozahaneler **IV.
Murad döneminde kapatılmıştır**. Yani kahvehane gibi: **1632'de açık**, hemen
sonrasında yasak. 1633 sahnesi kurulursa ikisi birlikte kaldırılır ve bunu
söyleyen tek kayıt etiketidir — `WorksKitTests` etiketin oraya ulaştığını
ölçüyor.

**Muvakkithane — varlık değil YER kısıtlı.** İstanbul'un ilki Fatih Camii
(1470) ve 17. yüzyılda çalışır durumda (Süleymaniye'de Ahmed Nakşî Efendi,
Fatih'te Müneccimek Mehmed). Ama yaygınlaşması 18. yy sonudur: 1632'de
muvakkithane bir **mahalle mescidine değil selâtin camisine** aittir.

Bu, tekkenin minaresizliğiyle aynı cinsten bir kısıt — ve aynı şekilde
korunuyor. Fark şu: minare için `TekkeParams`'a parametre **koymamıştım**;
burada yapı var, kısıtlı olan **yerleştirme**. O yüzden kural etikete yazıldı
ve test etiketi arıyor.

## 3. Ölçü nereden geliyor

Yedisinden yalnız birinde sayısal çapa vardı ve o da dolaylı: **Selimiye
Arastası 256 m'de 73 kemer** taşır → göz genişliği **3,51 m**. `bay_w`
varsayılanı odur ve test kütlenin bunu taşıdığını doğruluyor.

Ötekilerde ölçü değil **tarif** var, ve tarifler doğrudan doğrulama koşuluna
çevrildi:

* İmaret: *"yan yana dizilmiş"* → `bays >= 3`; *"farklı büyüklükte bacalar"*
  → baca boyu ve kesiti göze göre değişiyor.
* Değirmen: *"5–6 m taş oluk"* → `4 <= oluk_len <= 8`, dışı reddediliyor.
* Su terazisi: *"kule şeklinde kâgir yapı"* → `base_side >= height/8`,
  yoksa yapı baca olur; test de bunu ölçüyor.
* Muvakkithane: *"bir iki odadan büyük olmayan"* → `rooms in (1, 2)` ve
  ayak izi tavanı; pencere cephenin üçte birinden dar olamaz (muvakkit hem
  ışık ister hem **görünür** olmalı).

## 4. `join` nesne dönüşümünü yok sayıyordu — kaynakta düzeltildi

Arasta tonozunun on parçası döndürülüp yerleştirildi ve birleştirmeden sonra
**hepsi başlangıç noktasına yığıldı**; modelin tabanı −0,11 m'ye kaydı.

Sebep: `hz.join` parçaları `bm.from_mesh(obj.data)` ile okuyordu — ham mesh.
Nesnenin `location` ve `rotation_euler`ini **görmüyor**. Yani "döndür, sonra
birleştir" sessizce çalışmıyordu ve bugüne kadar fark edilmemişti çünkü hiçbir
kit nesne dönüşümü kullanmamıştı.

`join` artık `matrix_basis` birim değilse mesh'in bir kopyasını dönüştürüp
okuyor. Birim olduğunda hiçbir şey değişmez — mevcut ve **onaylanmış** bütün
varlıklar bit bit aynı kalır. (`matrix_world` değil `matrix_basis`: ilki
depsgraph çevrimi ister, ikincisi anında günceldir.)

## 5. İnceleme paketinin gösterdiği beş kusur

**(a) Su değirmeninin çarkı DOLU BİR DİSKTİ.** Çember `make_tube(r, r, ...)`
ile kuruluyordu ve `cap_top` varsayılan olarak kapalı olduğu için daire
doldu; kanatlar da içeride kaldı. Çember bir **halkadır** — kapaksız. Kanatlar
artık çemberin **dışına** taşıyor; çarkı çark yapan şey dışarıdan sayılabilen
o dişlerdir.

**(b) Oluk çarkın ALTINDA bitiyordu** (`z_lo = 1,55 r`, çark tepesi `2 r`) ve
eğimi duvar yüksekliğine bağlıydı, neredeyse düz çıkıyordu. Su yukarıdan
döker: oluk artık çark tepesinin üstünde başlıyor ve eğimi kendi boyundan
türüyor.

**(c) Ahşap AŞI KIRMIZISIYDI.** Aşı boyası **ev** boyasıdır; değirmen çarkı ve
tahıl teknesi boyanmaz. İkisi de koyu ahşaba çevrildi.

**(d) İmaretin avlusu KAPISIZDI** — üç gevşek duvar gibi okunuyordu. Tekkede
tam bu hataya düşülmüştü (ADR 0027 §6d). İki paye + lento eklendi. Avlu ayrıca
**mutfak bloğuna** göre ortalandı; ekmekhaneyle genişleyen toplama göre
ortalanınca yana kayık duruyordu.

**(e) Su terazisinin üst künkü HAVADA duruyordu.** İki künkü de taban
ölçüsüne göre koymuştum; gövde yukarı doğru inceliyor. Künk artık kulenin
**o yükseklikteki** yüzünden çıkıyor.

## 6. Kemer taşmaları: sabit sayı yerine türetme

`arched_panel` iki kez haklı olarak reddetti: *"kemer tepesi 3,75 m, panel
3,60 m — üstünde duvar kalmıyor"* (arasta) ve *"4,60 m / 3,30 m"* (bozahane).

İkisinde de sebep aynıydı: kemer başlangıcı **sabit bir sayıydı** ve açıklık
genişliği değişince tepe taştı. Artık `spring_z` duvardan türüyor
(`wall_h × 0,44`) ve `validate()` tepe ile duvarı karşılaştırıp **üretim
anında** hata veriyor — render'da değil.

## 7. Kalan boşluklar

- **Arasta bir sokak tipolojisidir** ve şu an tek prefab olarak duruyor.
  Sokak yerleştiricisinin onu bir *dizi* olarak kullanması Faz 4'ün işi.
- **Bozahane küpleri boş**: mayalanma sıvısı, kepçe, tas yok. Yakın plan
  donatısı Faz 4.
- **Değirmen taşı içeride ve görünmüyor** — iç mekân yok (bütün kitte olduğu
  gibi).
- **Muvakkithane'de saat/rubu tahtası yok**; muvakkiti muvakkit yapan aletler
  yakın plan işidir.
- **Su terazisi hattı yok:** tek tek kuleler var ama Kırkçeşme **güzergâhı**
  çizilmedi. Terazinin anlamı hattadır — bir sıra hâlinde dizilmeleri gerekir
  (GIS işi, Faz 4).
- Faz 2b'nin **kabul ölçütü** hâlâ açık: mescidi çekirdek alan bir mahalle
  sahnesi + öğle/gün batımı inceleme paketi + Caner onayı.
