# ADR 0073 — Uzak ağaçlar: silmeden ucuzlatmak

- **Durum:** kabul edildi ve uygulandı (Claude, 2026-08-29)
- **Bağlam:** Faz 7, performans (PLAN Bölüm 12)
- **İlgili:** ADR 0019 (dokusuz yaprak — telifli alfa atlası yok),
  `docs/feedback/faz7_performans.md`

## Ölçülen sorun

Kule turu 360°, planın 1080p/60 ve 1440p/60 hedefini kaçıran **tek**
adım. Sebep tek tek elenerek bulundu:

| şüpheli | p95 |
|---|---|
| gölge kapalı | 17,55 → 17,35 — değil |
| su kapalı | 17,03 → 16,7 — değil |
| **ağaçlar kapalı** | **17,03 → 6,99** |

Ağaçlar kapalıyken yöne bağlılık da tümüyle kayboluyor (12 kovanın
hepsi 5,1–6,6 ms). Yani hem maliyet hem de "kara yönü pahalı, deniz yönü
ucuz" örüntüsü tek bir kaynaktan geliyor.

**Sayılar:** 42 857 ağaç örneği, ~18 100 çizim çağrısı, 1,44 M üçgen.

## Mekanizma: doldurma değil, çizim çağrısı

İlk tahminim alfa-testli yaprak doldurmasıydı. **Yanlış.** ADR 0019
gereği yaprak dokusuz **katı geometri** — telifli bir yaprak alfa atlası
elimizde yok ve lisanssız görsel indirmek yasak. Yani overdraw sorunu
yok.

Son LOD zaten **80–84 üçgen**. Üçgen bütçesi de sorun değil.

Geriye kalan: **42 857 ayrı nesne, 42 857 ayrı çizim kararı.** SRP
Batcher bunların çoğunu topluyor (30 307/30 361) ama sayı o kadar büyük
ki toplama bile pahalı.

Denenen ve **işe yaramayan**: dört ağaç malzemesinde GPU örneklemesi.
Çizim çağrısı kımıldamadı (30 361 → 30 361).

## Seçenekler

### A — Ağaç sayısını azalt

42 857 → ~15 000.

- **Artı:** bugün, tek satırda, ölçülebilir.
- **Eksi:** manzaranın karakteri değişir. Ağaç yoğunluğu T2/T3 ama
  *yönü* belgeli: Okmeydanı bilerek boştur (RESEARCH §4.6 — oraya ağaç
  dikmek belgeye aykırı), Göksu "değirmenlerle çevrili" anlatılır,
  serviler mezarlıkların imzasıdır. Sayıyı kırpmak bu ayrımları da
  körleştirir.
- **Eksi:** *bir uçuş oyunu*. Ağaç kütlesi siluetin parçası ve kuş
  bakışı en çok görülen şey.

### B — Ağaçları ÖRNEKLEMEYLE çiz (ÖNERİLEN, seçildi)

Ağaçlar katı, düşük poligonlu ve hareketsiz. Yani birleştirilebilirler.

Belli bir mesafenin ötesindeki ağaçlar, arazi hücrelerine (ör. 64 × 64 m)
göre **tek bir mesh'te** birleştirilir: hücre başına bir çizim. Yakındaki
ağaçlar Unity'nin arazi ağaç sistemiyle, tek tek ve LOD'lu kalır.

Kaba hesap: 42 857 ağaç, ağaçlı ~2 000 hücre → hücre başına ~21 ağaç →
**~2 000 çizim**, yani dokuz kat azalma.

- **Artı:** hiçbir ağaç kaybolmaz. Siluet birebir durur.
- **Artı:** üçgen sayısı değişmez (zaten sorun değil).
- **Eksi:** araç işi — kümeleri pişiren bir üretici ve hücre başına mesh
  varlıkları gerekiyor.
- **Eksi:** kümelenmiş ağaçlar tek tek LOD değiştiremez; hücre bir bütün
  olarak LOD1'de kalır. Uzakta zaten öyle olduğu için bedeli düşük.

### C — Hedefi gevşet

Kule turunda 54 FPS kabul edilir denir.

- **Eksi:** PLAN "profil hedefleri **sabit**" diyor. Ve kule turu
  oyunun imza kadrajı — uçuş oradan başlıyor.

## Öneri

**B**, A'yı yedekte tutarak. Önce kümeleme ölçülür; hedefe ulaşmazsa
ağaç sayısı da azaltılır ve o zaman **hangi ağacın** azaldığı belgeye
göre seçilir (Okmeydanı zaten boş, mezarlık servileri korunur).

C reddediliyor: bir ölçütü karşılayamadığımız için ölçütü değiştirmek,
ölçüt tutmanın anlamını ortadan kaldırır.

## Ölçüm kapısı

Kümeleme başarılı sayılır ancak:

1. Kule turu p95 **≤ 16,67 ms** (1080p ve 1440p),
2. Ağaç sayısı **değişmemiş** (42 857),
3. Yön kovalarındaki fark belirgin biçimde azalmış olursa.

Değişkenlik payı **±1 ms** ölçüldü; bunun altındaki fark iyileşme
sayılmaz.

## Sonuç: kapı geçildi

| ölçüt | hedef | sonuç |
|---|---|---|
| kule turu p95 1080p | ≤ 16,67 | **8,86** ✅ |
| kule turu p95 1440p | ≤ 16,67 | **10,94** ✅ |
| ağaç sayısı | değişmemiş | **42 857** ✅ |
| yön farkı | azalmış | 6,7–8,3 (önce 6,8–17,0) ✅ |

Ve tek adım değil, **on iki adımın on ikisi** geçiyor. Boş arazide çizim
çağrısı 19 607 → **1 469**.

## Uygulamada: geometri ÜRETMEK yanlıştı

İlk iki uygulama ağaçları 64 m hücrelerde tek mesh'te **birleştiriyordu**.
Performans hedefi tutuyordu ama üretilen geometrinin bir yeri olması
gerekiyordu ve iki denemede de yer bulunamadı:

| deneme | sonuç |
|---|---|
| mesh'ler bellekte | Unity onları **sahneye** gömdü: 23,7 MB → **805 MB** |
| mesh'ler varlık olarak | aynı şişkinlik klasöre taşındı: **~900 MB** |

İlki bir kez **commit'lendi** ve geri alındı. Push edilmemişti; edilseydi
kalıcı olurdu (CLAUDE.md: *"yeniden üretim gürültüsü LFS'e KALICI
yazılır"*).

İkisi de aynı hatanın iki yüzü: **zaten var olan bir bilgiyi ikinci kez
saklamak.** Ağaçların yeri arazi verisinde duruyor
(`TerrainData.treeInstances`); ondan geometri türetip diske yazmak aynı
ormanı iki kere depolamaktı.

Doğrusu hiçbir şey üretmemek: konumlar arazi verisinden okunur,
dönüşüm matrisleri belleğe kurulur (42 857 × 64 bayt ≈ 2,7 MB) ve GPU
örneklemesiyle çizilir. **Diskte sıfır bayt**; sahne 1,4 KB büyüdü.

| ölçü | orijinal | küme mesh | **örnekleme** |
|---|---|---|---|
| kule turu 1080p | 17,83 ❌ | 8,86 | **7,23** ✅ |
| kule turu 1440p | 17,92 ❌ | 10,94 | **8,31** ✅ |
| boş arazi çizim çağrısı | 19 607 | 1 469 | **419** |
| boş arazi üçgen | 2,03 M | 2,67 M | **0,53 M** |
| diskte üretilen | — | ~900 MB | **0** |

Örnekleme her boyutta kazandı, üstelik hücre başına kesme yaptığı için
üçgen sayısı da düştü.

## Kaydedilen çıkmaz: yakın/uzak karışımı YÜRÜMEDİ

Tasarım "yakında arazinin tek tek LOD'lu ağaçları, uzakta kümeler"di.
`Terrain.treeDistance` ara değerlerde ağaç çizimlerini **hiç
azaltmadı**:

```
  treeDistance 3000 -> 19 607 cizim
  treeDistance  400 -> 20 875   (+ kumeler)
  treeDistance  100 -> 20 885   (+ kumeler)
  treeDistance    0 ->  1 459
```

Billboard mesafesini eşitlemek de değiştirmedi. Ayar bu kurulumda bir
açma/kapama gibi davranıyor ve **sebebi açıklanamadı**. Tahminle
uğraşmak yerine ölçülmüş iyi yapılandırma seçildi: kümeler bütün
ağaçları taşıyor, arazinin kendi ağaç çizimi kapalı.

**Bedeli:** ağaçlar her mesafede kaba LOD'da çiziliyor. Bir gerileme ve
**açık madde**. Ağaçlar zaten dokusuz, üsluplu katı geometri (ADR 0019);
kaba ile ince LOD arasındaki fark bir serviye bakarken küçük ama sıfır
değil. Çizici hücre başına LOD seçebilir — sonraki tur.
