# Doğancılar Meydanı — inceleme notu

İnceleme paketleri:
`renders/review/DogancilarCamii_v2/contact_sheet.png`,
`renders/review/HudayiTekkesi_v1/`, `renders/review/HudayiTurbesi_v1/`
Karar kaydı: **ADR 0037**. Araştırma: RESEARCH.md §5.5.

## Önce en önemli iki şey

**1. İniş noktamız 771 m yanlış yerdeydi.** `LM_Dogancilar` elle girilmişti
ve Galata'ya 3709 m veriyordu — modern kaynakların 3358 / 3400 / 3558 m'sinin
hiçbirine uymuyordu. Gerçek koordinatla mesafe **3336 m**: en düşük değere
%0,7 yakın. Düzeltme yalnız konumu değil, dünyamızın literatürle uyumunu
tamir etti. (Yan not: `landmarks_build.py` bu uyumsuzluğu her koşuşta
yazıyormuş; ben bakmamışım.)

**2. Uçuş sakin havada mümkün değil.** Ölçüldü:

| | |
|---|---|
| Kule tepesi | 98,2 m |
| Doğancılar arazi | 46,6 m |
| Yatay | **3336 m** |
| Düşüş | **51,7 m** |
| **Gereken süzülme** | **64,6 : 1** |

**Sonra gerçek kanat ayarıyla ölçtüm ve kendi önerimi çürüttüm.** İlk turda
"rüzgârı mekanik yapalım" demiştim; yanlıştı:

```
en iyi suzulme 11,56 : 1   trim 12,4 m/s   alcalma 1,08 m/s
sakin hava menzili 597 m   -   gereken 3336 m   =   EKSIK 2739 m
```

Bu açığı **yalnız rüzgârla** kapatmak **205 km/h** arkadan rüzgâr isterdi.
Çünkü rüzgâr uçuş *süresini* kısaltır, *alçalmayı* değil — bağlayıcı kısıt
süzülme oranı değil **alçalma hızı**.

Doğru büyüklük **yükselen hava** ve şaşırtıcı derecede küçük:

| arkadan rüzgâr | süre | **gereken yükselen hava** |
|---|---|---|
| yok | 268 s | **0,88 m/s** |
| 9 m/s | 156 s | 0,74 m/s |

Zayıf termik 1-2 m/s. Yani uçuş **ortalama 0,9 m/s'lik bir tırmanmayla
mümkün** — kanadı şişirmeye gerek yok, fizik dürüst kalıyor.

**Düzeltilmiş önerim: yükselen havayı mekanik yap.** Oyuncu Boğaz'ı geçerken
yükselen havayı bulup içinde kalır; final bir beceriye dönüşür. `WindTuning`
zaten rüzgâr alanı taşıyor, gereken şey alanın dikey bileşeni.

Yeni araç: **Hezarfen → Uçuş → Uçuş bütçesini ölç** (elle hesaplanan bir sayı
ilk değişiklikte sessizce yanlışa dönerdi). Yeni bekçi: süzülme oranı 15:1'i
aşarsa test patlıyor — finali "çalıştırmak" için oranı 65:1'e çekmek hiçbir
render'da görünmez ve oyunun bütün iddiasını sessizce çöpe atardı.

## Üretilenler

| | | |
|---|---|---|
| **Doğancılar Camii** | Çakırcıbaşı Hasan Paşa, **1548 Sinan**; 1580'lerde Hacı Ahmed Paşa yeniledi | harim 13 m, minare 26 m |
| **Hüdâyî tekke-camii** | 1589 başladı, **1595** bitti, **1598-99** minber eklendi | harim 11 m, minare 21 m |
| **Hüdâyî türbesi** | **açık türbe**, 1038 (1628-29) — 1632'de 3-4 yaşında | 4 sütun, 7,2 m |

İkisinin de **ölçülü çizimi yok** (bugünkü yapılar 1857 ve 1855-56). Ölçüler
**tipolojik varsayılan**, ölçüm değil — yuvarlak sayılar bilerek. `D3 /
draft`.

Kaynağın kesin söylediği tek biçim niteliği kullanıldı: duvarlar **kâgir**,
çatı **ahşap**, tek minare. Üretici çatının ahşap kalmasını zorluyor.

## Bu turda düzelttiğim

**Revak direkleri aşı kırmızısı ahşap çıkmıştı.** Bir çakırcıbaşının Sinan'a
yaptırdığı kâgir yapıda olmaz; ayakta kalan özgün parçalar "mermer çerçeveli
kapı" ve "ince kesme taş minare kaidesi". `mosque_kit`e taş sütun seçeneği
eklendi; mahalle mescidinin varsayılanı değişmedi.

## Sana iki sorum var (ikisi de senin kararın)

1. **Uçuşun fiziği** — yükselen hava mekaniği (önerim), mesafeyi kısaltmak,
   ya da fiziği görmezden gelmek? (ADR 0037 Soru 1)
2. ~~Hüdâyî türbesi~~ — **kararı bana bıraktın, verdim: 1632'de duruyor.**
   Kültür Envanteri türbenin **1038'de (1628-29)** yapıldığını kaydediyor,
   yani Hüdâyî'nin ölümünden aylar sonra; 1632'de yapı üç-dört yaşında.
   TDV yangın öncesi hâli **açık türbe** diye tanımlıyor ve bugünkü kubbe
   **dört mermer sütun** üzerine oturuyor — model dört ayaklı baldaken.
   Varlık belgeli (T1), biçim tipolojik (D3). Yerleştirildi.

   *Bu arada bir tuzak çıktı: kitte `acik` bayrağı **vardı ama hiçbir şey
   yapmıyordu** — kapalı bir türbe kurulup "açık" diye kataloglanıyordu.
   Kendi denetimim de bayrağın değerine bakıyordu, yapıya değil.*

## Bilerek eksik

- **Meydanın kendisi**: zemin, çınarlar, doğancı ocağı. Bu tur yapılardı.
- Doğancılar Camii avlusundaki **Hacı Ahmed Paşa türbesi** (1580'ler).
- Üsküdar Mihrimah külliyesinin öteki 1632 yapıları (önceki turdan).

---

**Onay**: _(bekliyor — "OK vN" yaz)_
