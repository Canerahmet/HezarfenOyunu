# ADR 0089 — Uzak sırtta basamaklanma: ölçüldü, üç sebep elendi, açık

**Durum:** açık — sebep daraltıldı, çözüm bilinmiyor.
**Tarih:** 2026-09-04

## Kusur

`08_halic_basi` karesinde orta mesafedeki sırt, silueti basamaklı ve
yüzü düzenli aralıklı trapez "payandalar"la kaplı görünüyor. Uçuş
oyununda her açık manzarada görülecek bir yüzey.

Ölçüldü (ufuk çizgisi, x 620-1270):

```
duz kosu uzunlugu : ortanca 7 px, ortalama 9,4, en uzun 99
8 px'ten uzun kosu: %35,2   (kosu sayisi 71)
sirt yuzunde tepe : 6 tane, araliklar 132/105/102/111/120 px (~114 px)
```

Yaklaşık 400 m mesafede 114 px ≈ **41 m** — yani düzenli aralıklı,
tekrar eden bir yapı. Rastgele arazi böyle bir periyot vermez.

**Karşılaştırma:** aynı ölçü `06_kara_surlari` ve daha önceki bir
`08` karesinde yapıldığında düz koşu ortancası **1 px** ve 8 px üstü
koşu oranı **%3,1** çıkmıştı. Yani kusur her karede yok; belirli bir
bakışta ortaya çıkıyor. (Bir tur önce bu iddiayı ölçüm doğrulamadığı
için **geri çekmiştim** — o geri çekme o kare için doğruydu.)

## Elenenler

| aday | ölçü | sonuç |
|---|---|---|
| DEM'in dikey nicemlemesi | 0..59.558'de 49.703 benzersiz değer, ardışık fark 1 | elendi |
| Yükseklik haritası en-yakın-komşu ile büyütülmüş | karada komşu farkı sıfır olan yalnız **%3,5**, 4×4 sabit blok **%0,00**, x%4 fazlarının ortalaması eşit (142,3) | elendi |
| Arazi LOD'u | `m_HeightmapPixelError: 1`, `m_HeightmapMaximumLOD: 0` | elendi |

Yani yükseklik verisi düzgün enterpolasyonlu ve arazi en keskin
ayarıyla çiziliyor.

## Geriye kalan

Editör açıkken bakılacak sıra:

1. **Arazi yaması sınırları.** 2049 örnek / 15.337 m = **7,49 m**
   örnek aralığı; Unity araziyi yamalar hâlinde çizer ve yama sınırı
   sığ açıdan dikiş verebilir. Ölçülen ~41 m periyot 5,5 örneğe denk
   geliyor — tam oturmuyor, ama aynı ailede.
2. **Gölge haritası kademesi (cascade).** Koyu bantların düzenli
   aralıklı olması gölge kademesi sınırına da uyar; `HDShadowSettings`
   okunacak.
3. **Kaynağın kendi çözünürlüğü.** Copernicus GLO-30 **30 m** adımlı;
   7,49 m'ye çıkarmak bilgi eklemez. Sırt gerçekten 30 m'lik
   basamaklarla tanımlıysa çözüm yükseklik haritasını büyütmek
   **değildir** — o zaman soru "kabul mü, yoksa el ile yumuşatma mı"
   olur ve Caner'e sorulur.

## Neden bu ADR var

Üç aday ölçümle kapandı ve her biri yeniden ölçülmeye değmeyecek kadar
kesin. Kayda geçmezse bir sonraki tur aynı üç kapıyı çalar — bu
oturumda tam olarak bu, çatı benekleri için de yaşandı (ADR 0088).
