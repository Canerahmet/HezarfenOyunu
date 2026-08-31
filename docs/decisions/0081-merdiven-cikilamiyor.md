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
| **Üst kata çıkılabilen** | **1/80 — %1,3** |

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

## Kalan bilinmeyen

Kesit, merdiven zincirinin **var olduğunu** gösteriyor: k=3'ten k=11'e
her katta 1-3 durulabilir hücre, üstünde 414 hücrelik üst kat. Yani
basamaklar duruyor ve üst kat yürünebilir; **zincir bir yerde
kopuyor** ve nerede kopduğu henüz bulunamadı.

Denenip işe yaramayanlar: yatay ızgarayı 0,35 → 0,25 → 0,15 m
küçültmek, dikey ızgarayı basamak yüksekliğine (0,22 m) oturtmak.

En güçlü aday: basamağın üstündeki **baş boşluğu**. Orta basamaklarda
gövdenin tepesi (ayak + 1,45 m) döşeme kotunu aşıyor ve merdiven
boşluğunun kenarına sürtüyor olabilir — kapsül yarıçapı 0,255 m,
boşluğun payı dar.

## Karar

1. Yapılan iş **kalıyor**: kapı, bölme, döşeme ve merdiven geometrisi
   ölçülebilir biçimde doğru ve zemin kat %100 erişilebilir.
2. Üst kat erişimi **açık kusur** olarak kayda geçiyor; Faz II.D'nin
   "tohumdan iç plan" adımına geçmeden önce kapanmalı, çünkü oda
   sayısının (ortalama 4,12) yarısı üst kattadır.
3. Bir sonraki adım tahmin değil **ölçüm** olacak: merdiven kolunun
   üstündeki baş boşluğu kot kot ölçülecek ve zincirin koptuğu hücre
   adıyla bulunacak.
