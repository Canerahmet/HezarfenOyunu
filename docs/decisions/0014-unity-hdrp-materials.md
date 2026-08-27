# ADR 0014 — Unity HDRP malzeme hattı

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — çalışıyor, testlerle kilitli
**İlgili:** ADR 0012 (kit ve malzeme), ADR 0013 (yakın plan), ADR 0005 (varlık hattı)

ADR 0012 §8 "Unity tarafı henüz yok" diyordu. Bu ADR o boşluğu kapatır: ev artık
Unity'de HDRP malzemeleriyle, LOD'lu, collider'lı, prefab'lı ve testlerle kilitli.

---

## 1. Sorun: Blender ile Unity aynı haritaları farklı paketler

Poly Haven `ARM` verir, HDRP `_MaskMap` ister ve ikisi **aynı şey değildir**:

| | R | G | B | A |
|---|---|---|---|---|
| Poly Haven `ARM` | AO | Roughness | Metallic | — |
| HDRP `_MaskMap` | **Metallic** | **AO** | Detay | **Smoothness** |

Yani kanallar yer değiştirir **ve** pürüzlülük tersine çevrilir
(`smoothness = 1 − roughness`). Bu dönüşüm atlanırsa hata **sessizdir ve
tuhaftır**: metalik olması gereken hiçbir şey yokken duvar metalik olur, mat
yüzeyler parlar. Doku "yüklenmiş" göründüğü için sebebi aramak saatler alır.

Dönüşümü `tools/textures/build_unity_maps.py` yapar. Maske alfa taşıdığı için
JPG olamaz — PNG yazılır.

## 2. Boyalı albedo Unity'de yeniden karıştırılamaz

Aşı boyası Blender'da düğüm grafiğiyle karışıyor (gamma → MIX/COLOR). HDRP'nin
taban renk tonu yalnızca **çarpar**; aynı sonucu vermez. Bu yüzden boyalı
yüzeylerin albedo'su Blender'daki **aynı matematikle** offline pişirilir.

AO çarpımı pişirmede **uygulanmaz**: Blender'daki AO çarpımı yalnızca inceleme
render'ı içindir ve Unity AO'yu maskenin G kanalında taşır. İki yerde birden
uygulamak girintileri iki kez karartırdı.

## 3. Bağlama: `OnAssignMaterialModel`

İlk denemem `MaterialLocation.External` idi. **Unity 6'da kaldırılmış** —
obsolete uyarısı verir ve çalışmaz. Desteklenen yol, her FBX malzemesi için
`AssetPostprocessor.OnAssignMaterialModel` kancasından proje varlığını
döndürmektir; Unity bunu kalıcı bir yeniden eşleme olarak kaydeder.

Arama **tek klasörle** sınırlı (`Art/Materials/Ottoman`). "Projenin her yerinde
ada göre ara" seçeneği de vardı ve daha kolaydı; bir gün adaş bir malzemeyi
sessizce bağlayacağı için tercih edilmedi.

Neden gömülü malzeme yetmez: FBX'ten gömülü üretilen malzeme yalnızca taban
rengi taşır — maske, normal, parlaklık yoktur. Elle düzeltilse bile **bir
sonraki Blender koşusunda silinir**.

## 4. Doku import politikası scriptte yaşar

`TextureImportPolicy` dosya adının son ekine göre karar verir:
`_BC` sRGB, `_MASK` ve `_N` ham veri. Ayrıca `wrapMode = Repeat` — dünya ölçekli
UV 0-1 aralığını **aşar**; Clamp olsaydı duvarın ilk 2 metresinden sonrası tek
renge yayılırdı.

Inspector'dan yapılan ayar kalıcı değildir; `ModelImportPolicy`'nin gerekçesi
burada da geçerli: boru hattının iki ucu da scriptte yaşamalı.

## 5. Yazdığını geri oku

Bu hattın hataları görünmez olduğu için her adım kendi çıktısını denetler:

* **Maske denetimi** (Python): yazılan PNG geri okunur, üç kanal eşlemesi
  kaynakla karşılaştırılır. Ölçülen sapma **0,0000** (PNG kayıpsız).
  Denetimin *ayırt ettiğini* de ölçer: kasten yanlış bir eşlemenin farkı
  0,78–1,00 çıkıyor. "Sessizce geçti" ile "doğru" aynı şey değildir.
* **Malzeme denetimi** (C#): maske bağlı mı, maske/normal dokuları sRGB
  işaretli mi.
* **EditMode testleri**: gömülü malzeme var mı, shader HDRP mi, sarma kipi
  Repeat mi, ölçüler Blender'la tutuyor mu.

## 6. Yol boyunca bulunan gerçek hata: malzeme adı çakışması

Unity tarafını hazırlarken üç çakışma çıktı. `M_Timber_Dark` hem varsayılan
paletin trim'i hem gayrimüslim paletin **hem ahşabı hem trim'iydi** — üç farklı
boya parametresi, tek ad. `M_Roof_Alaturka` ise iki farklı kiremit dokusuydu.

Blender bunu sessizce `.001` ekleyerek geçiştirir; hata ancak Unity'ye malzeme
yazarken ortaya çıkar ve o noktada hangi `.001`in hangi ev olduğu belli değildir.
Adlar ayrıştırıldı ve `tools/blender/selftest.py` artık bunu kilitliyor: bir ad
her zaman aynı dokuyu ve aynı boya parametrelerini göstermek zorunda.

## 7. Ölçülen durum

Unity'den okunan değerler Blender'ın bildirdikleriyle **birebir** örtüşüyor:

| | Değer |
|---|---|
| Ayak izi (Unity X × Z) | 8,900 × 8,700 m |
| Pivot | `bounds.min.y = 0` |
| LOD0 / LOD1 / LOD2 tepe | 8,8453 / 8,7603 / 8,5115 m |
| Cumba asimetrisi (+Z) | 0,800 m — "evin önü +Z" sözleşmesi |
| Malzeme | 10 adet, hepsi `HDRP/Lit`, hiçbiri gömülü değil |
| Testler | **EditMode 95/95, PlayMode 9/9** |

Üç LOD'un farklı tepe noktası vermesi anlamlıdır ve test edilir: LOD0 baca
külahıyla, LOD1 külahsız bacayla, LOD2 çatıyla biter. Sayılar birbirine
yaklaşırsa bir katman sessizce düşmüş demektir.

Sahne yakalamaları: `unity/HezarfenGame/Captures/faz2_house_*.png`
(gerçek arazi üzerinde, Galata kotu 51,96 m — Faz 1'de ölçülen 52,0 m ile aynı).

> **Tuzak — yeni oluşturulan nesne aynı çağrıda render edilmez.** Prefab'ı
> örnekleyip hemen `Camera.Render()` çağırınca ev **görünmedi**, yalnızca
> gölgesi düştü. Renderer'lar etkin ve kadrajın tam ortasındaydı (ölçüldü);
> eksik olan HDRP'nin görünürlük listesiydi. Örnekleme ile render ayrı
> çağrılara bölününce düzeldi.

## 8. Bu ADR'nin SÖYLEMEDİĞİ

- **Trim sheet / atlas hâlâ yok.** Her rol kendi 2K dokusunu kullanıyor; ev
  başına 6 malzeme, yani 6 çizim çağrısı. Plan "2–3 trim sheet + 1 atlas"
  istiyor. **Sıradaki iş bu** ve kararı ölçüme dayandırmak gerekiyor: 8 000 ev
  ölçeğinde SRP Batcher'ın ne kadarını kapattığı henüz ölçülmedi.
- **Dünya ölçekli UV atlası zorlaştırır.** UV'ler 0-1'i aşar (tekrar mesh'te);
  atlas tekrarı doğrudan desteklemez. Seçenekler: trim sheet düzeni,
  `Texture2DArray`, ya da shader tarafında `frac()`. Bu bir ADR konusudur.
- **Gayrimüslim palet Unity'de denenmedi** — malzemeler üretildi ama o palette
  bir ev import edilmedi.
- **Basamak collider'a girmiyor** (ADR 0013 §9 aynen geçerli).

## Yeniden üretim

```powershell
$b = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
& $b --background --factory-startup --python tools\textures\build_unity_maps.py
& $b --background --factory-startup --python tools\blender\gen_ottoman_house.py -- `
    --asset House_A --textured --detail near --window-detail kafes --cumba-type corbel `
    --out-blend art\blend\SM_House_A.blend `
    --out-fbx  unity\HezarfenGame\Assets\_Import\SM_House_A.fbx
```
Ardından Unity'de sırayla:
**Hezarfen → Boru Hatti → Osmanli malzemelerini uret**, sonra
**Hezarfen → Boru Hatti → _Import'u yerlestir ve prefab uret**.
