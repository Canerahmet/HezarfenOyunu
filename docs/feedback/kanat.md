# Kanat aygıtı — inceleme paketi v2

**Durum:** onay bekliyor
**Tarih:** 2026-08-28
**Paket:** `renders/review/Kanat_Acik_v2/contact_sheet.png`
**Karar kaydı:** [ADR 0064](../decisions/0064-kanat-aygiti.md)

---

## Önce bunu bilmen gerekiyor

**Bu kanadın tarihî planı yok.**

RESEARCH.md'nin kaydettiği şey şu: uçuşun tek tanığı Evliya Çelebi'dir,
Murad IV'ün verdiği söylenen kese altının mali kayıtlarda izi yoktur,
uçuşun tarihi kaynağın kendi içinde çelişir (1632 / 1638), ve
aerodinamik uzmanları Galata'dan Doğancılar'a süzülmek için gereken
~55:1 oranını imkânsız bulur — modern bir delta kanat ~15:1 verir.

Yani bu varlığı yaparken önümde üç yol vardı: bir plan uydurup tarihîymiş
gibi sunmak, kanadı hiç göstermemek, ya da **tasarımı malzemeden
türetmek.** Üçüncüsünü seçtim.

Plan Bölüm 10 malzemeyi zaten söylüyor: *ahşap çıta iskelet + kartal
tüyü yüzey + deri kayış*. Biçim oradan çıkıyor — merkezde omurga,
yelpaze gibi açılan çıtalar, uçları bağlayan hücum kenarı. Yarasa kanadı
ve uçurtma mantığı, çünkü 1632'de bir zanaatkârın gözlemleyebileceği şey
buydu.

Kanat **T3 (Efsane)** etiketli ve `sourceNote` alanının ilk cümlesi
"TARİHÎ PLAN YOKTUR". Kodeks ekranında oyuncu bunu okuyacak. Bu bir özür
değil — bence oyunun en dürüst özelliği.

## Serbestlik biçimde, fizikte değil

Tasarımın tek bağlayıcı sayısı **alan**: 15 m². Uçuş bütçesi ve termik
sınavı o sayıyla ölçüldü. Görünen kanat başka bir alana sahip olsaydı sen
bir şey görüp başka bir şeyin fiziğini yaşardın.

Bu yüzden üretici alanı **ölçüyor** ve %6'dan fazla sapmada duruyor. İlk
koşuda gerçekten durdu (13,63 m²). Sayıyı elle düzeltmek yerine
bağımlılığı ters çevirdim: açıklık artık alandan **türüyor**, elle
yazılmıyor.

Ayrıca Unity tarafına da bir test bağladım: model kataloğu ile
`WindTuning.wingArea` ayrışırsa test kırmızı yanar. Blender'daki bekçi
Unity'yi göremiyordu; iki zincirin ucu artık birbirine bağlı.

## Üç durum

| durum | açıklık | alan | LOD0 / LOD1 | ne için |
|---|---:|---:|---:|---|
| `Kanat_Acik` | 9,46 m | 15,00 m² | 772 / 88 | uçuş |
| `Kanat_Katli` | 2,84 m | 4,50 m² | 772 / 88 | sırtta taşınan; kule merdiveninde bu |
| `Kanat_Kirik` | 7,85 m | 13,47 m² | 640 / 76 | kaza sonrası |

Dihedral 7°: iki yarı yataydan yukarı bakıyor. Süs değil — düz bir levha
yuvarlanmaya karşı kayıtsızdır, dihedral kanadın kendini toplamasını
sağlar. 1632'de bunun adı yoktu ama uçurtma yapan herkes biliyordu.
İzdüşüm kaybı %0,7, yani alan bütçesini bozmuyor.

## Bu turda bulduğum üç yanlış

Üçünü de **sayı** yakaladı, hiçbirini render değil — ve bunu yazıyorum
çünkü hangi aracın neyi göremediği önemli:

1. **Kırık kanat 15,00 m² bildiriyordu.** Çıtalar ve tüyler düşüyordu ama
   zar tamdı. Yani hasar görsel bir süstü: kırık kanat sağlamıyla aynı
   fiziği taşırdı. Zar artık asimetrik kısalıyor.
2. **Katalog nominal açıklığı yazıyordu** (9,46), ölçüleni değil. Kırık
   kanadın gerçek açıklığı 7,85 m.
3. **LOD1 dört üçgendi.** Render bunu gösteremezdi — render her zaman
   LOD0'ı çizer.

Bir de boru hattı **"3 model yerleştirildi"** dedi ve üç kanadın da
etiketi Graybox kaldı: katalog anahtarını `SM_` önekiyle yazmıştım, diğer
on katalog çıplak ad kullanıyor. *Başarılı görünen bir adım eksik iş
yapabilir.*

## Senden gereken

Sözleşme gereği kanadın kabulü senin. **`OK v2`** yazman yeterli; sorun
görürsen maddeleyip yaz, düzeltip v3 üretirim.

Özellikle bakmanı istediğim iki şey:

1. **Tüy yüzeyi ahşap gibi mi okunuyor?** Doku tabanı `weathered_planks`
   ve tüy tonuyla boyandı. Bence uzaktan iş görüyor ama yakın planda
   (oyuncunun sırtında, üçüncü şahıs kamerasında sürekli görünecek)
   tahta çıta gibi durabilir. Ayrı bir tüy dokusu üretmem gerekirse söyle
   — bu bir tur daha demek.
2. **Ölçek doğru geliyor mu?** Karelerde 1,70 m'lik figür var. 9,46 m
   açıklık bir insanın taşıyacağından büyük görünebilir; ama alan
   fizikten geliyor ve fizik zaten onaylandı (ADR 0037). Küçültmek
   istersek `wingArea`'yı ve uçuş bütçesini birlikte değiştirmek gerekir.

## Bilerek yapılmayanlar

- **Kanat rig'i (açık/çırpma/hasar geçişleri)** — animasyon turunda.
  Şimdilik üç ayrı mesh; geçişler oradan gelecek.
- **Kumaş/tüy simülasyonu** — çalışma zamanı maliyeti ölçülmeden karar
  verilmez.
- **Kayışların vücuda oturması** — karakter taban geometrisi gelmeden
  ölçüsü yok.
