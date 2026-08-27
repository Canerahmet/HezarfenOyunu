# ADR 0016 — Mahalle dokusu: organik sokak yerleştiricisi

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — çıktı **TASLAK**, Caner onayı bekliyor
**Tetikleyen:** Caner, 2026-08-20: *"evler niye böyle tek sıra gibi… tek bir
kusursuz çizgi üzerinde olması biraz şüphelendiriyor beni, sanki doğallık ve
gerçekçilik bozuluyormuş gibi."*
**İlgili:** RESEARCH.md §4.1 (yeni), ADR 0013 (yakın plan), plan Faz 2 kabulü

---

## 1. İtiraz haklıydı

Şüphelenilen ızgara aslında bir **ölçüm kurgusuydu** (ADR 0015'in yoğun sokak
kümesi; yükü sabit tutmak için 13×20 m adımlı düzgün ızgara). Ama itiraz yine de
yerindeydi, çünkü şehir yerleştiricisi henüz yoktu ve yazılsaydı büyük ihtimalle
o ızgaradan türeyecekti.

Araştırma yapıldı ve bulgular RESEARCH.md **§4.1**'e kaynaklarıyla yazıldı. Özet:
tarihi yarımadanın dokusu **organiktir**, ızgara oraya 19. yüzyıl yangın sonrası
düzenlemeleriyle girer. Çıkmaz sokak bir kusur değil **hukuki bir kategoridir**
(hususi yol, üzerindeki komşuların ortak mülkü). Ev sokak çizgisine **duvarıyla
oturur**, bahçe arkadadır, cumba sokağa taşar.

## 2. Yerleştiricinin uyduğu kurallar

`OttomanStreetBuilder` yedi kuralı doğrudan uygular; her biri kodda numarasıyla
anılır. Hepsi **T2**'dir — nitel kaynaktan çıkarım.

1. Sokak ekseni **eş yükselti eğrisini izler**. Arazi eğimi ölçülür, ona dik yön
   alınır, üstüne ±12° gürültü eklenir. Rastgele bir eğri değil, **araziye
   bakan** bir yürüyüş.
2. Ev cephesi eksene **yerel olarak** diktir, küresel bir hatta değil.
3. Cephe hattında düzensizlik: −0,10…+0,40 m geri/ileri, ±6° sapma.
4. Ana sokaktan **çıkmazlar** dallanır.
5. Ev **duvarıyla** sokak çizgisine oturur — ayak izi değil, `wall_depth`.
   Ayak izi saçağı içerir; onu kullanmak evleri yarım metre geri iter.
6. Saçak ve cumba sokağın **üstüne** taşar; istenen budur.
7. Köşelerde iki cepheli varyant (`--facades sides`) kullanılır.

Sokak genişliği **4,6 m**: fıkıhtaki alt sınır (~3,4-3,8 m, "yüklü deve") ile
1848'de ana yollar için hedeflenen 7,6 m arasında. 1848 bir **genişletme**
hedefi olduğuna göre 1632 bundan dardı — T2.

## 3. Kural 8: ölçümden doğdu

İlk yerleştirmeden sonra ölçüldü ve doku kırıktı:

| | değer |
|---|---|
| Ayak izi altında kot farkı | medyan **3,22 m** |
| Hem havada hem gömülü ev | **108'in 89'u** |

Önce eğimin gerçek mi yoksa DEM gürültüsü mü olduğu ayrıldı — çünkü ikisi
tamamen farklı düzeltmeler ister:

| Ölçüm adımı | Eğim medyanı | p90 |
|---|---|---|
| 4 m | %14,2 | %29,0 |
| 20 m | %14,3 | %27,5 |

İki ölçek **aynı** cevabı verdi → eğim gerçek, gürültü değil. Galata gerçekten dik.

Sebep de ortaya çıktı: sokak eş yükseltiyi izlediği için evler ona **dik**, yani
yamacın **en dik yönüne** oturur. 8 m'lik bir ayak izi altında 1-2,5 m kot farkı
olağandır; bizim subasmanımız 0,60 m.

**Kural 8 — ev ayak izinin EN YÜKSEK köşesine oturur, altındaki boşluk taş
kaideyle dolar.** Ortalama kota oturtmak yarı gömer, en alçağa oturtmak havada
bırakır. En yükseğe oturtup altını doldurmak, yamaç evinin gerçekte yapıldığı
şeydir: taş istinat/subasman duvarı.

| | önce | sonra |
|---|---|---|
| Gömülen ev | **89** | **0** |
| Kaidenin kapattığı boşluk | — | 91 ev, en büyük **3,80 m** |
| Kaide maliyeti | — | **1 020 üçgen**, tek mesh |

Kaideler **tek mesh**te birleştirilir: ev başına ayrı nesne 100+ çizim çağrısı
ekler ve hiçbir şey kazandırmaz. UV **dünya ölçeğinde** üretilir (metre / 2,0 m),
yani taş dokusu evlerdekiyle aynı yoğunlukta okunur — ölçeklenmiş bir küp
kullanmak tam da bunu bozardı (ADR 0012 §5).

## 4. Testler kuralı değil, ÖLÇÜYÜ kilitler

Bir yerleştiricinin zamanla düzleşmesi kolaydır: bir sabit değişir, bir gürültü
katsayısı sıfırlanır ve doku sessizce ızgaraya döner. `OttomanStreetTests` bunu
yakalar — **sokağın eğri olduğunu ölçer**:

| Test | Ölçü |
|---|---|
| `MainStreet_IsNotAStraightLine` | ev konumlarına uydurulan doğrudan sapma RMS > **8 m** |
| `Facades_AreNotPerfectlyAligned` | komşu çiftlerin > **%60**'ı açıca sapmalı |
| `NoHouseIsBuriedInTheTerrain` | arazi altında kalan köşe = **0** |
| `StonePodiums_ExistAsOneMesh` | kaideler tek mesh, taş malzeme |
| `Scene_HasNeighbourhoodWithTier2Tag` | T2 etiketi ve kaynak notu |

## 5. Varyantlar

Faz 2 kabulünün istediği **20 parametre kombinasyonu** üretildi
(`gen_house_variants.py`). Rastgele bir tarama değil: her varyant bir
**tipolojik durumu** temsil eder ve tablo hangi durumun neden orada olduğunu
söyler — dar sokak evi, tek katlı arka sokak evi, üç katlı ana sokak evi,
köşe evi, gayrimüslim varyantları, saçak/eğim/pencere yoğunluğu uçları.

LOD0 ortalaması **2 424** üçgen; toplam 48 484.

Kitle tutarlılığı (RESEARCH.md §4.1(e)): ayak izlerimiz 30-70 m² bandında;
18. yy dağılımının "%80'i 300 zira² (≈172 m²) altında" bandının alt-orta
kısmında — sıradan mahalle evi. Konak/saray ölçeği bu kitin işi değil.

## 6. Bu ADR'nin SÖYLEMEDİĞİ

- **Bu bir şerittir, doku değildir.** Tek ana sokak + 4 çıkmaz = 108 ev. Gerçek
  mahalle bir **ağ**tır. Faz 4'ün işi.
- **Mescit çekirdeği yok.** RESEARCH.md §4.1(g) kural 7 mahallenin mescitten
  dallandığını söylüyor; yerleştirici henüz mescit bilmiyor.
- **Sokak yüzeyi yok** — kaldırım/arnavut kaldırımı, merdiven basamakları,
  bahçe duvarları, çeşme yok. Sokak şu an çıplak arazi.
- **Kaide collider'ı statiktir** (convex değil); karakter yürüyüşü geldiğinde
  ev collider'larıyla birlikte gözden geçirilmeli.
- **Yerleşim konumu TASLAK.** Mahalle Galata'da ama *hangi* mahalle olduğu
  iddia edilmiyor; 1632 mahalle sınırları kadastral değildi (ADR 0011).
- **Dönem görselleri repoya indirilmedi** — Lorichs 1559 panoraması ve Matrakçı
  Nasuh yalnızca metinsel atıfla anıldı (CLAUDE.md telif kuralı).

## Yeniden üretim

```powershell
$b = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
& $b --background --factory-startup --python tools\blender\gen_house_variants.py
```
Unity: **Hezarfen → Boru Hatti → _Import'u yerlestir ve prefab uret**, sonra
**Hezarfen → GIS → Galata sokagi sahnesi kur**.
