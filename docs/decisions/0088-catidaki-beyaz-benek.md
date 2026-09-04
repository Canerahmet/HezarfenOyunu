# ADR 0088 — Çatılardaki beyaz benek: dokuz aday elendi, kusur Unity tarafında

**Durum:** açık — sebep daraltıldı, çözüm Editor gerektiriyor.
**Tarih:** 2026-09-04

## Kusur

Oyun turunun kalabalık karelerinde çatılar ve aşı boyalı duvarlar
**küçük, krem-beyaz beneklerle** kaplı görünüyor. Karede sayıldı:

| kare | küçük benek |
|---|---:|
| 03_galata_sokak | 378 |
| 03_galata_sokak_kalabalik | 1.184 |
| 04_surici | 35 |
| 04_surici_kalabalik | 473 |
| 10_uskudar_kalabalik | 1.059 |

Sayı, karede **görünen çatı alanıyla** birlikte artıyor. Benekler
1 piksel (ortanca), rengi **0,93 / 0,81 / 0,70** ve altındaki yüzeyin
rengi 0,89 / 0,42 / 0,21 — yani benek yüzeyin *parlak hâli* değil,
**başka bir renk**. Fark (0,04 / 0,39 / 0,49) soğuk: güneşin sıcak
beyazı değil, göğe yakın bir şey.

## Elenenler — hepsi ölçüldü

| aday | ölçü | sonuç |
|---|---|---|
| LOD taramalı geçişi (dither) | satranç ilintisi 0,0001; periyot 2/4/8'de faz yok | elendi |
| LOD'lar arası UV kayması | `uv_yogunlugu.py`: oran 1,00 | elendi |
| Taban renk dokusunda benek | 4,2 M pikselde 49 soluk piksel; 256'ya inince **0** | elendi |
| Maske pürüzsüzlüğü (ayna nokta) | `A` kanalı max **0,18** (çatı), 0,40 (ahşap) | elendi |
| Maske alfası import'ta düşüyor mu | `alphaSource: FromInput`, sRGB kapalı | elendi |
| Paralaks (POM) | `_DisplacementMode: 0`, yükseklik haritası yok | elendi |
| Decal | projede `DecalProjector` yok | elendi |
| Çatıya serpilen küçük varlık | kitlerde yok (yosun/kuş/kiremit kırığı aranmadı-yok) | elendi |
| Önde uçuşan parçacık | benekler **çatı kenarında tam olarak bitiyor** | elendi |
| Çatı mesh'inde iğne deliği | üstten ortografik render: yüzey masif | elendi |

Son ölçü elemenin kendisinden daha çok şey söylüyor: **aynı varlık,
aynı dokular, benzer kadraj ve ışıkla Blender'da render edildiğinde
benek YOK.** Yani kusur modelde ya da dokuda değil, **Unity/HDRP
tarafında**.

## Geriye kalan

Editor açılınca bakılacak sıra:

1. `M_Roof_Alaturka`'nın **normal haritası + spekülerlik**: HDRP güneşi
   100.000 lux ve poz sabit; küçültme altında normal haritasının
   yüksek frekansı kıvılcım (firefly) verebilir. `_NormalScale`,
   `_SpecularAAScreenSpaceVariance` (0,5) ve `_SpecularAAThreshold`
   (0,2) tek tek denenmeli. Benek rengi **soğuk** olduğu için ilk
   şüpheli gökten gelen yansımadır.
2. **Yansıma probu**: sahnede prob yoksa HDRP gökyüzü küpünü kullanır;
   pürüzlü bir yüzeyde bu düz bir yıkama olmalı, benek değil. Yine de
   okunacak.
3. Kareyi **TAA kapalı** yakala: benek kalıyorsa gölgelendirici,
   kayboluyorsa zamansal örnekleme.

## Neden bu ADR var

Dokuz adayın elenmesi bir buçuk saat sürdü ve hepsi ölçümle kapandı.
Kayda geçmezse bir sonraki tur aynı dokuz kapıyı yeniden çalar. Bu
depoda tekrar eden ders — *"bozuk olan çoğu zaman ölçtüğün şey değil,
ölçme biçimin"* — burada iki kez daha çıktı: benek "LOD taraması"
sanıldı (satranç ilintisi çürüttü) ve "çatıdaki delik" sanıldı
(macenta dedektörü çatının kendisini de macenta saydı; üstten render
yüzeyin masif olduğunu gösterdi).
