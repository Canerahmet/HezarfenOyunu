# Şehir sakini — kadın (ferace + yaşmak)

**Varlık:** `SK_Sakin_Kadin` · inceleme: `renders/review/Sakin_Kadin_v18/`
**Ölçüler:** boy 1,601 m · 54.340 / 16.302 / 4.346 üçgen · 9 parça · 30 kemik

## Caner'e soru: feracenin bel dikişi

İnceleme karesinde figürün belinde net bir yatay çizgi var; ferace
oradan ikiye bölünmüş gibi okunuyor. Önce "basamak var" sanıldı ve
ölçüldü:

```
ferace dikisi: kabuk 0.218/0.150 -> etek 0.226/0.153
               (basamak +0.008/+0.003 m)
```

Yani yarıçap farkı **8 mm**. Görünen şey basamak değil, **teğet
kırılması**: gövde kabuğu aşağı-içeri inerken etek konisi birden
aşağı-dışarı açılıyor, normal bir anda zıplıyor ve göz orayı dikiş
olarak okuyor.

**Denendi ve ölçüm reddetti.** Eteğin üst ucu dikeye yaklaştırıldı
(yarıçap `t^1,6` ile ilerledi). Dikiş yumuşadı — ama aynı turda eteğin
yanından **kırmızı şalvar göründü**. Sayıyla:

| sürüm | profil | kırmızı sızıntı (piksel) |
|---|---|---:|
| v16 | düz koni, 2 halka | 0 |
| v17 | `t^1,6`, 5 halka | **5.027** |
| v18 | düz koni, 5 halka | 0 |

Sebep yapısal: eteğin üst yarıçapı kabuğa, alt yarıçapı da altta
kalanların zarfına (`alt_zarf`) sabit. Bu iki ucu birleştiren
eğrilerden **içe bükük** olanı zarfın içine giriyor. "Üst uç dikey
başlasın" ile "altındakini örtsün" aynı anda sağlanamıyor; düz koni bu
iki şartın tek çözümü.

Sevk edilen sürüm **v18**: profil düz, halka sayısı 2'den 5'e çıktı.
Geometri aynı, ama etek animasyonda kalçadan dize tek bir dörtgenle
deforme olmuyor.

**Önerimi denedim ve kare reddetti — ama başka bir şey buldu.**

Bandı dikişin kendi kotuna taşıdım (kadında dikiş belde değil
**kalçada**: eski bant birkaç santim yukarıdaydı), feracenin kendi
kumasından yaptım, boyunu 8,6 cm'den 3,3 cm'ye indirdim. Sonuç bantsız
hâlden **daha kötü**: aynı renkte bir halka, giysinin üzerinde duran
bir fıçı çemberi gibi okuyor ve "kova" izlenimini azaltmak yerine
artırıyor. Erkekte kuşağın işe yaramasının sebebi **kırmızı** olması —
orada bir giysi ögesi olarak okunuyor. Yani erkekteki çözümün kadında
da işleyeceği bir benzetmeydi, ölçüm değil.

**Asıl kusur başkaymış.** Eteğin üst yarıçapı kabuktan 8 mm dışarıda
ve aradaki halka **kapalı değildi**: belde görülen koyu şerit gölge
değil, eteğin iç duvarıydı. Etek artık üst halkasından içeriye,
kabuğun yarıçapına doğru yatay bir raf örüyor (`kiy.etek(...,
ic_kapak=...)`) ve delik kapanıyor. Kumaş gerçekte de öyle davranır:
etek belde içe kıvrılıp gövdeye dikilir.

Üç hâl yan yana konunca sıra net: **yalnız kapak** > bantsız-kapaksız >
bant+kapak. Sevk edilen sürüm **v25**: kapak var, bant yok.
Maliyet 256 üçgen.

Geriye kalan koyu çizgi iki kumaş katmanının değdiği yerdeki **temas
gölgesi**dir ve gerçek bir bel dikişinde de vardır; kaldırılacak bir
kusur olarak görmüyorum. Yine de karar senin.

## Ayrıca kayda geçen, henüz sorulmayanlar

- **Yaşmak düzeltildi (v22).** İki kusur vardı ve ikisi de ölçülüp
  kapandı:
  1. Yüz açıklığı **dikdörtgendi** — dört köşesi de dik. Çocuk için
     aynı kusur bir tur önce bulunup kemere çevrilmiş, ama yetişkin
     için uygulanmamıştı. Artık iki ucunda kapanan bir mercek.
  2. İlk denemede kemer **merdiven** çıktı (v20): eğri, çocuğunkinden
     daha kısa bir z aralığına sığıyor, yani aynı bölmeyle daha az
     örnek. Yüz bandının bölmesi 6'dan 12'ye, dilim 40'tan 48'e
     çıkarıldı. Çene altına da eteğinkine benzer dikey kırışıklar
     kondu — düzgün koni kumaş değildir.

  Bedeli: 54.340 → **57.340** üçgen (+%5,5). Aynı değişiklik kızın
  başörtüsünün kenarını da düzeltti (54.692 → 57.092).
- **Ön kol çıplak.** Sokak kıyafetinde ferace kolu bileğe iner; şu an
  dirsek altı ten görünüyor.
- **Ayakkabı** sarı bir kütle; mest/pabuç ayrımı yok.

## Ölçülüp ELENEN bir şüphe

Kızın yüzü yetişkin gibi duruyor diye çocuk oranları ölçüldü — kafa
boyu / boy:

| figür | boy | kafa | oran |
|---|---:|---:|---:|
| Sakin_Erkek | 1,778 | 0,277 | 1/6,41 |
| Sakin_Oglan | 1,246 | 0,184 | 1/6,76 |
| Sakin_Kiz | 1,228 | 0,192 | 1/6,41 |

Çocukta kafa/boy oranı 1/6 civarındadır; ölçüm bunu veriyor. Yani
**oranlar doğru**; kızın yetişkin görünmesi orantıda değil yüz
hatlarında. Ayrı bir soru, ve şimdilik açık.

## Onay

_(Caner: "OK vN" ya da düzeltme.)_
