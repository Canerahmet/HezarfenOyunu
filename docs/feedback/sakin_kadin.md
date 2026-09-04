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

**Kalan üç seçenek — hangisi?**

1. **Bel bandı geri gelsin.** Feracenin kendi kumaşından, dar, bağsız
   bir bant dikişi örter. Kodun daha önceki turu bunu koymuş, sonra
   "ferace kuşak taşımaz" diyerek kaldırmıştı; kare o kaldırmanın
   bedelini gösteriyor. En ucuz ve en emin çözüm.
2. **Etek daha yukarıdan başlasın**, üst ucu kabuğun altında kalsın.
   Bel kalçadan dar olduğu için etek orada gövdeden ayrı durur —
   toplanmış bir etek gerçekten böyledir, ama siluet belde şişer.
3. **Olduğu gibi kalsın.** 1,60 m'lik bir figür oyunda çoğunlukla 10 m
   öteden görülüyor; dikiş o mesafede okunmuyor olabilir. Ölçülmedi.

Önerim **1**, ve gerekçesi artık tercih değil **gözlem**: aynı dikiş
erkekte de var (`Sakin_Erkek`, entari eteği belden başlıyor) ama
inceleme karesinde **görünmüyor** — çünkü erkekte kuşak orada duruyor
ve tam olarak o dikişi örtüyor. İki figür yan yana konunca fark
kumaşta değil, o bantta. Kodun eski turu bunu zaten yazmıştı ("etek
kuşağın altında başlarsa dikiş kuşakla örtülür"); kadında bant
kaldırılınca dikiş açığa çıktı.

Tarihsel olarak da savunulabilir: feracenin beli toplanır. Üçgen
maliyeti birkaç yüz, ve kusuru kaynağında değil göründüğü yerde
kapatıyor.

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
