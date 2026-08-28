# ADR 0067 — Animasyonun doğruluğu ölçülebilir bir şeydir

**Durum:** Kabul edildi (uygulandı)
**Tarih:** 2026-08-28
**Bağlam:** Faz 5, animasyon seti

---

## Sorun

Bir yürüyüş döngüsünün "iyi göründüğü" bir **görüştür.** Bu projede
görüşle iş yapmıyoruz; ama animasyon, bu projenin şimdiye kadar
ölçemediği ilk şey gibi görünüyordu.

Değil. Yanlış olduğu **ölçülebilir** ve tek bir sayıya iner:

> **Yere basan ayak, gövde ilerlerken kaymamalı.**

Adım uzunluğu ile hız tutmuyorsa ayaklar paten kayar. Oyun
animasyonundaki en görünür kusurdur ve gözle bakınca "biraz tuhaf" diye
geçiştirilir; ölçünce santimetre verir.

## Karar

Klipler scriptle üretilir ve her döngü için **ayak kayması ölçülür**.
5 cm'i aşarsa üretim durur.

İkinci bir ölçüt eklendi ve gerekliydi: **tempo.** Yalnız kaymayı
sıfırlamak yetmiyor, çünkü adım uzunluğunu serbest bırakıp süreyi
uzatarak da sıfırlanır — ve karakter **74 adım/dakika** ile bir cenaze
temposunda yürür. Gerçekten oldu. Adım uzunluğu ile tempo bağımsız
seçilemez: çarpımları hızdır ve hız `WalkController`da zaten yazılıdır.

Sonuç: genlik ve süre **birlikte** çözülür, yinelemeli olarak — çünkü
temas oranı da genliğe bağlıdır ve doğrusal varsaymak ucuz ama yanlıştı
(24,6 cm kayma verdi).

## Ölçülen sonuçlar

| klip | süre | tempo | ayak kayması |
|---|---:|---:|---:|
| Yürüme | 1,10 s | 110 adım/dk | **0,7 cm** |
| Koşma | 0,73 s | 165 adım/dk | **1,8 cm** |
| Merdiven | 1,27 s | 96 adım/dk | **0,4 cm** |

13 klip: duruş, yürüme, koşma, merdiven, kuşanma, kalkış, süzülüş (+4
blend ucu), iniş, çakılma.

## Merdiven: farklı bir şey, farklı bir ölçüt

Düz zemin ölçütünü tırmanışa uygulamak **yanlış şeyi ölçmekti.** Tırmanan
adamın gövdesi yükselir; ayağı basamakta durur ve gövde onun üzerinden
geçer. Sinüs tabanlı poz bunu tarif etmiyordu — iki ayak temas evresinde
**ters yönlere** gidiyordu, yani hareket bir tırmanış değil yerinde
saymaydı.

Merdiven artık **ayak yolundan** kurulur ve açılar IK ile çözülür. Yol,
merdivenin geometrisinden (rıht 0,19 m, basamak 0,26 m — **T2, taslak**)
ve tempodan türer. Kayma tanım gereği sıfırdır; ölçüm artık düzeltmiyor,
**doğruluyor**.

IK analitik değil **sayısal**: kosinüs teoremi "uyluk sıfır dönüşte tam
aşağı bakar" varsayar ve bu iskelette öyle değil — kemik roll'u otomatik,
`Hips` 12° eğik, ve dinlenme duruşunda bacak zaten neredeyse tam açık
(kalça-ayak 0,820 m, bacak 0,827 m). Formül ayak bileğini 60 cm öteye
koyuyordu. Kemik çerçevesini tahmin etmek yerine Blender'ın kendi ileri
kinematiğine karşı çözüyoruz.

## Yedi ölçüm hatası — ve hepsi ölçümün kendisindeydi

Bu turun öğrettiği asıl şey bu: **bozuk olan çoğu zaman animasyon değil,
aleti.**

1. İşaret ters: fark yanlış yönde çıkarılıyordu.
2. Temas evresi **dairesel**di (döngü başında ve sonunda), ilk/son
   indeksi almak bütün döngüyü temas saydı → 149 cm.
3. Temas eşiği mutlaktı; oransal olmalıydı.
4. Temas penceresi **iki ayağa da aynı** veriliyordu, oysa sağ ayak yarım
   faz kaymalı — sağın "teması" aslında salınımıydı → 58,5 cm.
5. Tırmanışta "en alçak nokta = temas" sezgisi geçersiz.
6. Ölçülen kemik (parmak ucu), IK'nın yerleştirdiği kemik (ayak bileği)
   değildi → 128 cm.
7. Ayak yolu **erişilemez** hedefler istiyordu (0,924 m derinlik, bacak
   0,827 m); kırpılan her karede ayak kayıyordu.

## Unity: mesh'siz FBX'ten klip çıkmıyor

13 klip Unity'ye geldi ve **hiçbirinde klip yoktu.** Blender eğrileri
yazmıştı (70 eğri düğümü). Sebep tahmin edilmedi, **deneyle ayrıldı**:
aynı klip mesh'le birlikte aktarıldığında Unity 1,07 s'lik klibi okudu;
avatar ayarını (Human + CopyFromOther) sabit tutup yalnızca mesh'i
değiştirdiğim için değişken tekti.

Çözüm tam mesh'i her klibe koymak değil (13 × 6,7 MB = 87 MB, hepsi
LFS'e): armature'a deri bağlı **tek üçgenlik bir vekil** yeter. Dosyalar
~230 KB kalıyor.

Döngü bayrağı da **katalogdan** okunuyor, isim listesinden değil — ve
`SaveAndReimport`tan **önce** yazılıyor: ilk yazımda sonra çağırıyordum,
ayar belleğe yazılıp diske hiç gitmiyordu ve bütün klipler "tek"
kalıyordu. Klipler vardı, yalnızca dönmüyorlardı.

## Sonuç

262 test yeşil, sıfır atlanan. `_Import` boş.
Caner onayı `docs/feedback/karakter.md`'ye yazılacak.

İlgili: ADR 0066 (rig), ADR 0065 (karakter).
