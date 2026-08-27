# ADR 0035 — Kız Kulesi 1632: ahşap nöbet kulesi, deniz üstünde yerleştirme

- **Tarih**: 2026-08-25
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/kiz_kulesi.md`)
- **Bağlam**: Faz 3, S-kademe landmark. Uçuş hattının tam üstünde.

## Karar 1 — Kule ahşap kurulur, kâgir değil

1632'de Kız Kulesi bugünkü kule **değildir**. Kâgir gövde, camlı köşk ve
kurşun kubbe **1725**'tir; kule 1720'de yanmış, Damat İbrahim Paşa yerine
kâgir bir fener kulesi yaptırmıştır. 1509 depreminden sonra yapılan da
*"yine ahşap"*tır (Göksoy Özkan 2012). Kaynak ve tam yokluk listesi:
RESEARCH.md §5.3.

Model bu yüzden **kayalık + kâgir subasman + ahşap gövde + kurşun piramit
örtü** olarak kurulur. Kubbe **yoktur** — kubbe 1725'in işaretidir.

## Karar 2 — İşlev fener değil karakol

Zeytinyağı feneri 1718 sonrasıdır. 1632'de kule bir karakoldur: Fatih
1453'ten sonra nöbetçi birliği yerleştirmiş, her akşam yatsıdan sonra ve
seher vakti **mehter** çalar. Model bu yüzden tepesinde fener değil,
korkuluklu bir **nöbet sahanlığı** taşır. Sahanlık payandalarla çıkma
yapar; v2 renderinde çıkma gövdenin yalnızca %10'u kadardı (0,90/9,00 m) ve
sahanlık bir çıkıntı gibi değil bir kenar gibi okundu — çıkma 1,40 m'ye
alındı, altına payanda sırası eklendi, korkuluk dolu levhadan direk+kuşağa
çevrildi (dolu levha çalgıcıyı gizliyordu).

## Karar 3 — Ölçü uydurulmadı; tek kısıt mantıksal

1632 kulesi yanmıştır ve ölçülü çizimi yoktur. Kütle **D3** (tipolojik),
`status: draft`. Tek sayısal sınır teste bağlandı: ahşap kule, yerine geçen
1725 kâgir kulesinden (~23 m) **alçak** kalmalı. Model su üstünde 20,0 m.

## Karar 4 — Deniz üstündeki landmark su düzlemine oturur

**Ölçüldü**: `LM_KizKulesi` çevresinde 150 m yarıçapta sekiz yönün sekizi
de −12,0 m. Copernicus GLO-30 adacığı hiç görmüyor. Arazi kotuna oturtmak
kuleyi deniz tabanına gömerdi.

Bu yüzden `LandmarkPlacer` yeni bir kural taşıyor: ayak izi altındaki en
yüksek arazi kotu **0,5 m'nin altındaysa** yapı **y=0**'a (su düzlemi)
oturtulur ve bu rapora yazılır. Kayalık, arazinin değil **varlığın**
parçasıdır ve −2,50 m'ye kadar iner — su çizgisinde kesilmiş durmasın diye.

Yön de eğimden türetilemez (çevre baştan başa deniz tabanı). `Seaward`
**üç halka** tarar: 200 / 400 / 800 m. Tek 200 m'lik halkada en yüksek
örnek −7,2 m çıkıyordu; yön doğru sonuç veriyordu ama gerekçe deniz tabanı
gürültüsüydü. 800 m'de kara +43,8 m'ye çıktığı için karar artık ölçüye
dayanıyor. Kule ESE'ye, Salacak kıyısına bakar.

## Karar 5 — `timber_bare`: boyasız ahşap ayrı bir malzeme rolüdür

v1 renderinde gövde kırmızı okundu. Ölçüldü: kod yorumu "boyasız yapısal
ahşap" diyordu ama kullanılan `trim`, `ASI_DARK` ile %70 MIX tintlenmiş —
yani **boyalı**. Aşı kırmızısı bir EV boyasıdır (ADR 0030 §5c); nöbet
kulesi boyanmaz.

Yeni rol `timber_bare` **her iki palette aynıdır** (kesme taş / mermer /
tuğla ile aynı ilke: boyanmamış kereste cemaate göre değişmez).

Ayırt edici nitelik **açıklık değil DOYGUNLUK**: boyasız kereste ile koyu
aşı, açıklıkta yakın durur (ΔE yalnızca 8,4) ama kromada ayrışır —
boyasız 5,4, aşı ailesi 11–28. Selftest bu eşiği kilitliyor
(`t_bare_timber_is_not_painted`: boyasız < 8, aşı ailesi > 11).

Bir yan ölçüm: COLOR karıştırma kipi tonu çıkarırken **değeri de** götürdü
(gövde/kaya parlaklık oranı 0,43 → 0,30, gövde neredeyse siyah).
`value_gamma=0.66` değeri geri kaldırdı; oran 0,47, kırmızı sapma R/G 1,51
→ 1,08.

### Yeni palet rolünün SIRASI (sessiz tuzak)

Yeni bir palet rolü eklendiğinde önce `build_unity_maps.py`, sonra Unity'de
**Osmanlı malzemelerini üret**, ancak ondan sonra FBX boru hattı
çalıştırılmalı. Sıra bozulunca hata **sessizdir ve boş yuva vermez**: FBX
içe alınırken gövdeye, FBX'e gömülü, dokusuz, albedosu 0,906 beyaz bir
malzeme bağlandı. Mevcut "boş malzeme yuvası yok" denetimi bunu geçirdi;
ayırt eden şey malzemenin bir **varlık yolu** taşımasıdır.

Yeni test `LandmarkPrefabsUseAuthoredMaterialsNotEmbeddedOnes` bunu
`LandmarkPlacer.Built` içindeki her prefab için kilitliyor (aynı denetim ev
kitinde vardı, landmark'larda yoktu).

Ayrıca `Assets/_Import/Materials` altında eski bir içe alımdan kalan 6
malzeme bulundu; hiçbir prefab veya sahne onlara bağlı değildi (bağımlılık
taramasıyla doğrulandı) ve silindi. İniş alanı yeniden boş.

## Sonuç

- Kayalık tek koni yerine **üç kütleli obek** (tek koni kum yığını gibi
  okunuyordu). Yan kütleler subasmanın altında kalır, kâgir tabanı delmez.
- LOD0 799 üçgen, LOD1 var, `UCX_KizKulesi` çarpışma kütlesi, LODGroup 2
  kademe, boş malzeme yuvası yok.
- Yerleşim: (2540,8 , 0,0 , −446,0), tepe +20,30 m, taban −2,50 m,
  rotY 112,5°.

## Açık kalanlar

- Kayalık dokusu hâlâ `M_Stone_Rubble` (duvar dokusu). Kayalık için ayrı
  doku yok; D3 taslakta kabul edildi.
- Kule ile kıyı arasındaki ~100 m'lik açıklık su malzemesiyle
  doğrulanmadı — su yüzeyi Faz 5 işi.
