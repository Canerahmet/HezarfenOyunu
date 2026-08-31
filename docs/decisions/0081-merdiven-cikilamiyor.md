# ADR 0081 — Merdiven duruyor ama üst kata çıkılamıyor

- **Durum:** **AÇIK KUSUR** — geometri ve çarpışma yerinde, yürüyüş yolu kapanmıyor
- **Tarih:** 2026-08-31
- **Bağlam:** Faz II.D, iç mekân
- **İlişki:** ADR 0078 (ana plan), CLAUDE.md "ölçüm, imza değil"

## Ölçülen durum

D_Galata, 92 örnek ev:

| ölçü | değer |
|---|---|
| Girilebilen ev | **%97,8** |
| Zemin katta erişilen hacim | **%100,0** |
| **Üst kata çıkılabilen** | **3/77 — %3,9** |

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
