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

Önerim **1**: tarihsel olarak savunulabilir (feracenin beli toplanır),
üçgen maliyeti birkaç yüz, ve kusuru kaynağında değil göründüğü yerde
kapatıyor.

## Ayrıca kayda geçen, henüz sorulmayanlar

- **Yaşmak** hâlâ sert bir kabuk gibi duruyor: ön yüzü düz, başa değil
  kendi profiline oturuyor. Profil başın kotlarından türetilmiş
  (`sakin_kit.yasmak`) ama omuzda 1,52 kat açılma yüzeyi levhalaştırıyor.
- **Ön kol çıplak.** Sokak kıyafetinde ferace kolu bileğe iner; şu an
  dirsek altı ten görünüyor.
- **Ayakkabı** sarı bir kütle; mest/pabuç ayrımı yok.

## Onay

_(Caner: "OK vN" ya da düzeltme.)_
