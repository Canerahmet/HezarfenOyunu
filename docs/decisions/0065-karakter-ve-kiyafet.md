# ADR 0065 — Karakter: tek istisna, ve kıyafetin nereden geldiği

**Durum:** Kabul edildi (uygulandı)
**Tarih:** 2026-08-28
**Bağlam:** Faz 5

---

## 1. Taban geometri: projenin tek istisnası

Bu projede her varlık scriptle sıfırdan üretildi — 36 landmark, 142
mahalle, 12 248 ev, tekneler, kanat. Karakter tek istisna ve istisnayı
plan koydu (Bölüm 10): taban gövde Blender Studio **Human Base Meshes**
paketinden gelir (CC0).

Sebep anatomi değil **deformasyon**. Dirsek, diz, omuz ve göz çevresindeki
kenar halkaları animasyon için özel olarak yerleştirilir. Bunu prosedürel
üretmek zor değil; **ürettiğinin iyi olduğunu ölçmek** zor. Ev üretebilirim
ve iyi olup olmadığını üçgen sayısıyla, ölçüyle, siluetle söyleyebilirim.
Omuz halkası için öyle bir sayı yok — ancak deforme edip bakarsın, ve o da
render'dır, kanıt değil.

Paketten sonrası yine scripttir: yön, boy, oran, kıyafet, LOD.

### Hangi gövde ve neden

Pakette dört tam gövde var. Seçim **ölçümle** yapıldı:

| gövde | boy | |
|---|---:|---|
| `body_male_realistic` | **1,69 m** | ✅ seçilen |
| `body_male_stylized` | 1,80 m | |
| `body_female_realistic` | 1,64 m | Faz 6 |
| `body_female_stylized` | 1,62 m | |

Bu projenin bütün inceleme paketleri karede duran **1,70 m'lik bir ölçek
figürüne** göre yargılandı. Stilize gövdeyi seçmek şimdiye kadar verilmiş
her ölçek kararını sessizce %6 kaydırırdı. Plan "stilize-gerçekçi" diyor
ve bu projede stilizasyon **orandan değil malzeme/gölgeleme dilinden**
gelir.

## 2. Kıyafet: Rålamb'dan okundu, kopyalanmadı

Dört plaka indirildi (kamu malı, `refs/ralamb/`, kayıt
`refs/LICENSES.md`). Mimaride uygulanan kural burada da geçerli:
*fotoğraftaki gibi değil, fotoğraftaki dil kadar.* Minyatür kopyalanmadı;
okunan şey oranlardır.

Türeyen altı kural (ayrıntı `docs/RESEARCH.md`):

1. **Entarinin boyu işi söyler** — oturan adam ayak bileğine, çalışan adam
   dize. Uçuş varyantının kısalığı bir tasarım tercihi değil, plakaların
   söylediği şey.
2. Kuşak doğal belde, dar, kontrast renkte.
3. Şalvar entarinin altından **görünür** — entari onu örtmez, ortaya
   çıkarır.
4. Kol ağzı astarı ters çevrilir.
5. **Dizlik gerçek bir öğedir** (plaka 50) — planın istediği "dizlik"
   uydurma değil, gözlenmiş.
6. Başlık rütbe göstergesidir; Hezarfen ne paşa ne asker → orta hacim.

**Uyarı, ve kapatılamaz:** albüm **1657–58**, oyun **1632**. Yirmi beş yıl.
Mundy albümü (1618) öbür yandan yaklaşıyor; **tam 1632'nin kaynağı yok.**
Kıyafet bu yüzden T2. Gövde T3 (Hezarfen'in portresi yok). Giyinik varlık
**en düşük bileşeninden** etiketlenir: T3.

## 3. Giysi gövdeden TÜRER

Giysi elden modellenmiyor: ilgili bölgenin yüzleri kopyalanır, normalleri
boyunca dışarı itilir, kalınlık verilir. Böylece oturması bir göz kararı
değil **yapısal bir garanti**dir — elden modellenen bir entari poz
değişince gövdeye batar, kabuk batmaz çünkü zaten o gövdenin ofsetidir.

Bundan bir kural doğdu: **üst katman alt katmandan her kotta daha çok
şişmeli.** Kumaş kalınlığı değil, şişme sırası belirler kimin üstte
olduğunu.

## 4. Bu turun bulduğu yanlışlar

Hepsi **sayıyla** bulundu:

| ne | nasıl ortaya çıktı |
|---|---|
| Yön ölçümü ayak parmağını değil ayağın **dış yanını** buluyordu (iki ayak arası açıklık, parmak boyundan uzun) | omuz genişliği **0,247 m** — yetişkin omzu değil |
| "En dar dilim boyundur" araması **kafatasının tepesini** buluyordu | boyun 0,088 m, omuz 0,151 m — bir bebeğe bile dar |
| Getirilen gövdenin paket içindeki **kendi konumu** vardı; boru hattı kimlik dönüşümü bekler | giyinik karakter **3,29 m** genişliğinde |
| Kesit ölçüsü A-pozunda **kolları** sayıyordu | kuşak 0,84 m çapında — hula hoop |
| Şalvar belde 0,030, entari 0,021 şişiyordu | karında kırmızı bir leke — alt katman üstü deldi |
| Kuşak entarinin **içinde** kalmıştı | sırtın ve karnın çukurunda damla gibi sızıyordu |
| Sarık alından başlıyordu | **gözleri örtüyordu**; bir başlık yüzü kapatmaz |
| Kısa etek 0,37 m'de bitiyordu, diz 0,48 m'de | dizlik üretiliyor ama **hiç görünmüyordu** — üretilen ama görünmeyen bir öğe, olmayan bir öğedir |

## 5. Ve Unity'de duran çelişki

Karakter 1,70 m çıkınca üç sayının üç ayrı insanı tarif ettiği görüldü:

| yer | değer | ima ettiği insan |
|---|---:|---|
| model | 1,70 m | 1,70 m |
| `WalkSpawner` kapsülü | 1,80 m | 1,80 m |
| `WalkController.eyeHeight` | 1,70 m | **1,81 m** |

Göz alanının yanındaki not *"1,70 m: ölçü figürüyle aynı"* diyordu. Nota
bakınca doğru; sayıya bakınca değil — **1,70 o figürün boyudur, gözü
değil.** Bir yorum, bir sayının yanlış olduğunu gizleyebilir.

Düzeltildi: göz **1,59 m** (modelin kendi ölçüsünden: `boy − baş/2`),
kapsül **1,70 m**. Sarığın 9 cm'i kapsüle girmiyor — insan şapkasıyla
çarpışmaz; çarpışsaydı geçtiği kapıdan geçemezdi.

`KarakterTests` üç yeri birbirine bağlıyor: katalog, `eyeHeight`, kapsül.

## 6. Sonuç

Gövde T3, kıyafet T2, ikisi de `status: draft`. FBX **yazılmadı**:
rig'siz bir karakter oyun varlığı değil ve `_Import` boş kalır.
Caner onayı `docs/feedback/karakter.md`'ye yazılacak.

İlgili: ADR 0064 (kanat), ADR 0005 (varlık hattı).
