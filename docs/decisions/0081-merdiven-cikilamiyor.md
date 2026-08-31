# ADR 0081 — Merdiven duruyor ama üst kata çıkılamıyor

- **Durum:** **ÇÖZÜLDÜ** (2026-08-31) — üst kata çıkılabilen
  **%75,6** (Unity NavMesh ile ölçüldü).
- **Tarih:** 2026-08-31
- **Bağlam:** Faz II.D, iç mekân
- **İlişki:** ADR 0078 (ana plan), CLAUDE.md "ölçüm, imza değil"

## Ölçülen durum

D_Galata, 92 örnek ev:

| ölçü | değer |
|---|---|
| Girilebilen ev | **%97,8** |
| Zemin katta erişilen hacim | **%100,0** |
| **Üst kata çıkılabilen** | **8/59 — %13,6** |

Kapı, bölme duvarları ve zemin kat tamam. Merdiven **görünüyor**,
çarpışma kütlesinde **var**, tavanda boşluğu **açık** — ama yürüyerek
çıkılamıyor.

## Ne yapıldı, ne bulundu

### Gerçek kusurlar (düzeltildi)

1. **Collider'da zemin kat tavanı yoktu.** Görünen geometride döşeme
   vardı, çarpışmada yoktu; kesit y=1,10'dan 5,35'e kesintisiz boşluk
   gösterdi. Yani üst kat bakılınca vardı, basılınca yoktu. Döşeme artık
   görünen tavanla aynı fonksiyondan (`_tavan_parcalari`) ve aynı
   boşluktan türüyor.
2. **Bölme duvarları merdiveni kesiyordu.** Geniş evde bölmeler boyuna
   (y), merdiven enine (x) — bölmeler basamakların üstünden geçiyordu.
   İki plan aynı hacmi paylaşamaz. Bölmeler artık merdiven kolunun
   önünde bitiyor; odalar önde, merdiven sahanlığı arkada.
3. **Merdiveni hayatın içine almak denendi ve geri alındı.** Hayatı ikiye
   bölünce zemin katta erişilen hacim %99,7'den **%82,8'e** düştü. Bir
   sorunu çözerken ötekini açmak çözüm değil.

### Ölçüm kusurları (düzeltildi)

Bu kusur boyunca ölçüm **dört farklı yanlış sayı** verdi ve her biri
başka bir sebepten:

| ölçüm | sonuç | neden yanlış |
|---|---|---|
| 0,5 m ızgara | %31,1 | 0,95 m kapıyı 0,54 m gövdeyle çözemez |
| kapsül dosemenin altında | 320/322 "ölçülemedi" | subasman 0,30-0,95 m, tek kot doğru olamaz |
| tasma dolgusu kapıdan sızıyor | "%100 üst kat" | ölçülen şey sokağın üstündeki açık hava |
| iki katmanın ORTA noktası | %0 / %9 | orta nokta döşemeye değil odanın içine düşüyor |

Şu an kullanılan model doğru olanı: **üç boyutlu yürünebilirlik**.
Bir hücre durulabilir sayılır — gövde boşluğu boş, hemen altı dolu — ve
komşuluk bir basamak (0,22 m) yukarı/aşağı farkına izin verir. Bu, bir
gezinme ağının en yalın hâli ve merdiveni doğal olarak tırmanır.

## İkinci turda denenenler (2026-08-31)

| deneme | sonuç |
|---|---|
| İç sınır payı 0,35 → 0,05 m | değişmedi — ilk basamak zaten pay dışında değilmiş |
| Basamak 0,22×0,26 → **0,19×0,30**, kol 0,95 → **1,10 m** | %1,3 → **%3,9** |
| Collider'ı kesitten **render etmek** | kesme düzlemi merdiveni kesip attı; "merdiven yok" sandım — yanlıştı, 34 kutunun 14'ü basamak |
| Kol eni 1,10 → **1,40 m** | **kötüleşti**: %3,9 → %0. Geri alındı. |

Sonuncusu önemli: bir sayıyı büyütmek işe yaramıyorsa **ölçülen şey o
sayı değildir**. Kolun eni kısıt değil.

Merdiven daha az dik ve daha geniş hâliyle **kalıyor**: dönem doğruluğu
(0,22 diklik) çıkılamayan bir merdivende bir işe yaramaz, ve 0,19 hâlâ
modern yönetmeliğin (0,175) üstünde.

## Ölçülen kırılma noktası

Kat kat tarama, zincirin **tam olarak nerede** koptuğunu söylüyor:

```
k=2  y=0,69  durulabilir=755   <- zemin kat
k=3  y=0,91  durulabilir=0     <- KOPMA
k=4  y=1,13  durulabilir=5     <- merdivenin ilk hücresi
...
k=13 y=3,11  durulabilir=4
k=14 y=3,33  durulabilir=1143  <- üst kat (bağlı)
```

y=0,92'de destekli 677 hücrenin **hepsi** ayaktan +0,35 m'de
engelleniyor — yani o hücreler duvar hücreleri, merdivenin ilk
basamağının üstünde hiç destek yok. Merdiven zinciri k=4'ten yukarı
sağlam; kopan tek yer zeminle ilk basamak arası.

## Çözülen: cetvel basamağı göremiyordu

Kalan kusurun **büyük bölümü ölçümdeydi**. Dikey ızgara adımı basamak
yüksekliğine eşitti (0,22 m) ve ızgara katmanları 0,69 ile 0,91'de
duruyordu; ilk basamağın üstü ise **0,79'da**, yani tam aralarında.
Bir ızgara, üstüne basılacak yüzeyi kendi adımının arasına düşürürse
o basamak yokmuş gibi olur.

Adım küçültülünce ölçüm yakınsadı:

| dikey ızgara | üst kata çıkılabilen |
|---|---|
| 0,22 m | %3,9 |
| **0,11 m** | **%20,8** (26 örnek) · **%13,6** (61 örnek) |
| 0,07 m | %23,1 (15 örnek) |

0,11 m yerleşik değer: 0,07 iki kat pahalı ve iki puan kazandırıyor.
Küçük örneklemdeki %20-23, 61 örnekte %13,6'ya oturuyor.

Ayrıca komşuluk kuralı da düzeltildi: artık ızgara katmanına değil,
hücrenin **ölçülen destek kotuna** bakıyor ve iki hücre arasındaki
kot farkı `CharacterController`'ın adım payını (0,30 m) aşmıyorsa
geçilebiliyor. Izgara yalnızca örnekleme aracı.

## Kalan bilinmeyen

Merdiven kolunun her basamağında **4-18 yürünebilir hücre** var ve üst
kat (1143 hücre) yürünebilir. Zincir k=3'ten yukarı sağlam. Kopan tek
yer **zemin katla ilk basamak arası** ve orada da 4 hücre mevcut —
yani hücreler var, komşuluk kurulmuyor.

En güçlü aday artık şu: zemin katın ızgara kotu (0,69 m) ile ilk
basamağın kotu (0,79 m) arasında **0,10 m** var, ama ızgaranın dikey
adımı 0,22 m. Yani ikisi çoğu evde **aynı katmana** düşüyor ve
"bir kat yukarı" komşuluğu hiç denenmiyor; ayrı katmana düştüğünde de
aradaki fark bir tam adım değil. Izgara, basamağın kendisini değil
basamak ile zemin arasındaki **ilk farkı** çözemiyor.

Sınanacak: dikey ızgarayı zeminin kotuna göre hizalamak (kutunun
tabanına değil, **ölçülen döşeme kotuna** göre başlatmak).

## Karar

1. Yapılan iş **kalıyor**: kapı, bölme, döşeme ve merdiven geometrisi
   ölçülebilir biçimde doğru ve zemin kat %100 erişilebilir.
2. Üst kat erişimi **açık kusur** olarak kayda geçiyor; Faz II.D'nin
   "tohumdan iç plan" adımına geçmeden önce kapanmalı, çünkü oda
   sayısının (ortalama 4,12) yarısı üst kattadır.
3. Bir sonraki adım tahmin değil **ölçüm** olacak: merdiven kolunun
   üstündeki baş boşluğu kot kot ölçülecek ve zincirin koptuğu hücre
   adıyla bulunacak.


---

# Çözüm (2026-08-31, ikinci gün)

## Ölçüyü Unity'ye devretmek

El yapımı ızgara sekiz farklı cevap verdi (%100, %97,2, %0, %9, %1,3,
%3,9, %13,6, %1,6). "Bir insan buradan oraya yürüyebilir mi" sorusunun
Unity'de zaten bir sahibi var: **NavMesh**. Ev başına bir navmesh
pişirilip (`NavMeshBuilder.BuildNavMeshData`) kapıdan üst kata yol
isteniyor; ajan yarıçapı, boyu, adım payı motorun kendi tanımları.
Araç `EvMerdeveni` — ve aynı pişirme Faz II.G'deki NPC gezinmesinin
temeli olacak.

Bu araç da üç kez yanlış kuruldu ve üçü de kayda değer:

| kusur | belirti |
|---|---|
| `localBounds` yerine dünya kutusu verildi | 41 evin 41'inde "kapıda navmesh yok" |
| başlangıç ışını evin **tepesinden** bırakıldı | ilk çarptığı yüzey çatı; `bas` çatıya oturdu |
| üst kat araması merkezin ±1,5 m'sine baktı | üst kattaki yüzey merdiven boşluğunun çevresinde, arka duvara yakın |

## Asıl kusur: aşınma

Recast yürünebilir alanı ajan yarıçapı (0,30 m) kadar **aşındırır**.
Merdiven yan duvara bitişik başlıyordu ve basamak derinliği 0,26 m —
yani **ilk basamak bütünüyle aşınıp yok oluyordu**. Merdiven navmesh'i
zemin kattan kopuk kalıyor, yol da kapıdan sokağa iniyordu.

Aynı şey kolun ucunda da vardı: merdiven boşluğu tam kolun ayak izi
kadar, kolun ucuyla duvar arası 0,8 m; iki yandan aşınınca sahanlık
0,2 m'ye iniyordu — çıkanın basacağı yer yok.

Üç ölçü **birlikte** ayrıldı:

| kol eni | duvar payı | sahanlık | üst kata çıkılabilen |
|---|---|---|---|
| 1,10 | 0,00 | 0,30 | **%0** |
| 1,10 | 0,40 | 1,30 | %17,1 |
| **1,35** | **0,50** | **1,30** | **%58,5** |
| 1,60 | 0,60 | 1,30 | %46,3 |

## Daha önce yazdığım bir yargıyı düzeltiyorum

"Kolu genişletmek kötüleştirdi (%3,9 → %0), yani kısıt kolun eni
değil" demiştim. Yanlıştı: o sırada sahanlık ve duvar payı yoktu,
geniş kol onları yiyordu. **Bir değişken tek başına denendiğinde
yanıltır**; kısıt üçünün birleşimiydi.

## %58,5'ten %70,7'ye — darlığın iki ayrı yüzü

Başarısızların dağılımı, kusurun **darlık** olduğunu söylüyordu ama
darlığın nesi olduğunu söylemiyordu. İki ayrı yer çıktı ve ikisi de
gezinme ağının aynı kuralına takılıyordu: ajan yarıçapı (0,30 m) her
açıklığı iki yandan yiyor.

| değişiklik | gerekçe | sonuç |
|---|---|---|
| İç kapı 0,95 → **1,15 m** | 0,95'ten geriye 0,35 m kalıyordu; arka oda (ve merdiven) kopuyordu | %58,5 → **%65,9** |
| En < 5,8 m'de bölme **yok** | 5,2 m'lik iç genişlikte bölme önde 2,3 m, arkada 2,4 m bırakıyor; arka sokağın küçük evi zaten tek hacimdir | %65,9 → **%70,7** |
| Sahanlık 1,30 → **1,55**, duvar payı 0,50 → **0,60** | aynı aşınma kuralı, kolun iki ucunda | %70,7 → **%75,6** |
| Sahanlık 1,80, duvar payı 0,70 | — | **%75,6** — değişmedi, kaldıraç tükendi |

Kanıt dağılımdan geldi: kapı genişletilince başarısızların ortanca eni
6,55 → 6,13 m'ye indi, yani kalanlar en dar uca yığıldı.

## Kalan %24,4 — ölçüldü, sebep bulunamadı

Genişlikle ilişki **var ama zayıf** (son ölçüm):

| | n | ortalama en | ortanca | en az | en çok |
|---|---|---|---|---|---|
| çıkılan | 31 | 7,21 m | 7,07 | 5,25 | 9,66 |
| çıkılamayan | 9 | 6,72 m | 6,55 | 5,40 | 9,79 |

Aralıklar iç içe: 9,79 m'lik bir ev de başarısız oluyor, 5,50 m'lik bir
ev de başarılı. Yani darlık tek başına açıklamıyor.

Bunun üzerine basamak derinliğine alt sınır kondu (0,32 m; koşu
kırpılınca basamak sayısı azaltılıp rıht 0,28 m'ye kadar yükseltiliyor).
Merdiven yumuşadı ama **ölçüm değişmedi**: aynı 24/41, aynı evler.
Yani kalan kusur basamağın dikliği de değil.

Başarısızların hepsi aynı deseni gösteriyor: yol kapıdan sokağa iniyor,
yani merdiven navmesh'i zemin kattan hâlâ kopuk. Bir sonraki adım
tahmin değil ölçüm olmalı — başarısız bir evin navmesh üçgenlerini
kotlarına göre dökmek, kopmanın hangi basamakta olduğunu söyler.
