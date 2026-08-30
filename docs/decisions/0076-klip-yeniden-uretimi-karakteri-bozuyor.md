# ADR 0076 — Animasyon kliplerini toptan yeniden üretmek karakteri bozuyor

- **Durum:** AÇIK ama **YOL ÜSTÜNDE DEĞİL** (2026-08-30, ADR 0080) —
  kök sebep hâlâ bulunamadı; yer hareketi Mixamo'ya geçtiği için o
  üretim artık yapılmıyor. Kalan beş uçuş klibi elle yapıldı ve yeniden
  üretilmiyor. Kusur, üretici yeniden çalıştırılırsa geri gelir.
- **Tarih:** 2026-08-30
- **Bağlam:** Faz 8, oynanış turu
- **İlişki:** ADR 0068 (karakter hattı), CLAUDE.md "Commit'lemeden önce ölç"

## Ne oldu

Caner oynarken bildirdi:

> "karakter gorunumundeyken kosmaya baslayinca problem oluyor"

Ölçüldü ve haklıydı: koşu klibi **3,6 m/s** için üretilmişti,
`WalkController.VarsayilanKosma` ise **6,0 m/s**. Ayaklar yerde döngü
başına ~2,4 m kayıyordu. Yürüme de aynı durumdaydı (1,4 e karşı 2,2).

Kök sebep "bir sayının iki sahibi": hız hem C#'ta hem
`gen_animasyon.py`'de yazılıydı ve yorumda *"Hiz WalkController'dan
gelir"* diyordu — **gelmiyordu, kopyalanmıştı**. Üreteç artık sayıyı
C# dosyasından **okuyor** (`oyuncu_hizlari`), bulamazsa patlıyor.

## Ama yeniden üretim karakteri parçaladı

`gen_animasyon.py --export` on üç klibi birden yeniden üretti. Bütün
sayılar doğruydu: çözülen ayak kayması 1,3 ve 2,0 cm, tempo 130 ve 190
adım/dk, 366 EditMode testi yeşil.

Oyuna girip **bakınca** karakter parçalanmıştı:

- kaftan gövdeyi yutan içi boş bir silindir,
- sarık başın yanında ayrı duran bir halka,
- gövde ve kollar çıplak.

Hiçbir test kırmızı yanmadı, çünkü hepsi **sayı** okuyordu.

## Nasıl daraltıldı

Deneyle, tahminle değil:

| deney | sonuç |
|---|---|
| İskelet ve bağlanma pozları ölçüldü | Hips 0,89 m, Head 1,42 m, ayak 0,08 m — **sağlam** |
| Klipler `git checkout` ile geri alındı | karakter **düzeldi** |
| Yalnız `Yurume` + `Kosma` yeni bırakıldı | karakter **düzgün kaldı**, hız da doğru |

Yani bozan, yeniden üretilen **öteki on bir klipten biri** (en güçlü
aday `Durus`: kusur görüldüğünde animatör tam onu, ağırlık 1,00 ile
oynatıyordu).

## Karar

1. **Yürüme ve koşma** yeni hâlleriyle kalır — istenen düzeltme budur.
2. Öteki on bir klip **depodaki hâlinde** bırakılır. Bu aynı zamanda
   CLAUDE.md'nin "yeniden üretim gürültüsü LFS'e kalıcı yazılır"
   kuralına uyar: içeriği değişmemesi gereken dosya değiştirilmez.
3. Kök sebep bulunana kadar `gen_animasyon.py --export` **toptan
   çalıştırılmaz**. Çalıştırılırsa, istenmeyen klipler `_Import`'tan
   silinerek yalnız gerekli olan indirilir.

## Kusuru artık ne yakalar

`KarakterSiluetiTests.TheCharacterStillFitsInsideAHumanEnvelope`:
deri pişirilir ve dünya ölçüsü alınır. Sağlam karakter
**0,79 × 1,80 × 0,62 m**; bir parça gövdeden koparsa zarf taşar ya da
siluetin merkezi kalçadan uzaklaşır.

`renderer.bounds` bilerek kullanılmadı: deri değişen mesh'te şişiktir
ve **sağlam** karakterde bile (2,25 × 2,48 × 2,63) okur — yani bozuğu
sağlamdan ayırmaz. Bu oturumda yanlış cetvel zaten defalarca kusuru
gizledi; buraya bir tane daha eklemek istemedim.

## Açık soru (Caner'e)

Kalan on bir klibin yeniden üretilmesi gerekirse kök sebep aranmalı.
İki yol var:

- **A.** Blender tarafında rest/bind pozunu ölçen bir değişmez ekle
  (`gen_animasyon.py` her klipten sonra iskeletin dinlenme pozunu
  sınar). Maliyet: yarım gün. Kalıcı çözüm.
- **B.** Kliplerin FBX ayarlarını (avatar kaynağı) Unity tarafında
  sabitle ve yeniden içe aktarımın avatarı değiştirmesini engelle.
  Maliyet: birkaç saat. Belirtiyi keser, sebebi bilmeyiz.

**Önerim A** — bu proje boyunca "ölçmediğin şey bozulur" kuralı altı kez
doğrulandı, ve bu kusur tam olarak ölçülmeyen bir yerde doğdu.
