# Ev iç mekânı — ölçülen durum

**Tarih:** 2026-09-04 · **Varlık:** `art/blend/variants/*.blend` (201 varyant)
**Neden bu not:** Caner'in isteği *"evlerin içi de erişilebilir olsun,
döneme göre dekore edilsin, kesintisiz geçiş"*. Planda bu iş
"başlanmamış" görünüyordu; ölçüldü ve **büyük kısmı zaten var**.

## Var olanlar — kanıtı da yanında

| ne | kanıt |
|---|---|
| **Cephe gerçekten delik** | `_dress_near`: *"Duvar zaten delinmiştir (`make_wall_panel`)"*; kapı bir niş, sövesi ve eşiği var |
| **Çarpışma kütlesinde kapı boşluğu** | `_carpisma` zemin katı boşaltıyor ve kapı açıklığını `_opening_layout`'tan **okuyor**; docstring bunu Caner'in isteğine bağlıyor |
| **Kat döşemesi + merdiven** | içeriden çekilen kare: ahşap merdiven birinci kata çıkıyor, taş zemin, sıvalı duvar, kirişli tavan |
| **İç bölmeler** | `ottoman_kit.ic_bolmeler`, `merdiven_plani`, `_build_floor(..., bosluk=…)` |
| **Dönem mobilyası** | `_mobilya` zemin katta çağrılıyor: sedir, yüklük, ocak, sandık, rahle, mangal, kilim (`mobilya_kit`) |
| **İç yüzeyler** | kapı içeriden de sıvalı bir duvarın içinde |

Yani "girilebilir, döşenmiş iç mekân" **geometri ve fizik olarak
kurulu**. Planın Faz 3 ve Faz 5'i büyük ölçüde bitmiş; kayıt bunu
söylemiyordu.

## Kalan tek görünür engel: kapı kanadı

Kapı kanadı LOD0'ın içine **kaynatılmış** durumda. Çarpışmada boşluk
olduğu için oyuncu ondan **geçebiliyor** — ama kapalı görünen bir
kapının içinden geçiyor. Yani kusur erişimde değil, **inandırıcılıkta**.

Çözümü dört adım ve üçü Unity tarafında:

1. `build_house` kanadı LOD0'a katmasın, `KAPI_<varlik>` adıyla ayrı
   nesne bıraksın. `export_fbx` koleksiyondaki her şeyi aldığı için
   FBX'e kendiliğinden girer.
2. `ImportLanding` prefabda onu çocuk nesne olarak bıraksın.
3. Menteşe: kapıya yaklaşınca dönen küçük bir bileşen.
4. LOD1/LOD2'de kanat yok — o mesafede kapı zaten okunmuyor.

Adım 1 Blender tarafında ve tek başına zararsız; ama dördü birden
bitmeden "kapı açılıyor" denmez. Bu yüzden bu turda **başlanmadı**:
yarım bir boru hattı değişikliği, 201 varyantı ve altı üreteci birden
etkiler.

## Ölçülmemiş olan

- İç mekânın **kare bütçesindeki** payı — **kısmen ölçüldü.** Mobilya
  üç varyantta:

  | ev | LOD0 | mobilya | pay |
  |---|---:|---:|---:|
  | House_A_Dar | 2.100 | 204 | %10 |
  | House_B_Orta | 2.472 | 228 | %9 |
  | House_C_Genis | 3.016 | 156 | %5 |

  Yani döşeme, evin üçgen bütçesinin onda birinden az. İç bölme ve
  merdiven bu ölçümde **ayrıştırılamadı**: harness'ın kurduğu
  `HouseParams` gerçek üretimdekiyle birebir olmadığı için ikisi de 0
  döndü — oysa içeriden çekilen kare merdiveni açıkça gösteriyor.
  (`ic_bolmeler` zaten `width < 5,8` iken boş döner ve House_A_Dar'ın
  duvar eni 5,6; o satır doğru. Ötekiler doğrulanmadı.) Sayı gerekince
  ölçüm üreticinin kendi içinden alınmalı, dışarıdan yeniden kurarak
  değil.
- İçeride **ışık**. ADR 0087'nin konusu tam da bu: kapalı gölgeye
  dolaylı ışık gelmiyor. Bir odayı inandırıcı yapan şey sıçrama
  ışığıdır; fırın oturmadan iç mekân değerlendirilemez.

## Onay

_(Caner: "OK vN" ya da düzeltme.)_
