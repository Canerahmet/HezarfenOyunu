# ADR 0010 — HDRP / URP Karar Kapısı

**Tarih:** 2026-08-19
**Durum:** Kabul edildi — **HDRP'de kalınıyor**
**Karar veren:** Claude (ölçüm Caner tarafından istendi: *"sen ölç"*)
**İlgili:** plan Bölüm 1 (teknoloji kararı), Faz 1 kabul; ADR 0009

## Bağlam

Plan bu kapıyı bilinçle Faz 1 sonuna koymuş ve **tek yönlü** ilan etmiş:

> HDRP karmaşıklığı/performansı → Faz 1 kapısında FPS/üretim hızı hedef altıysa
> URP'ye geç (karar kapısı Faz 1 sonu; **sonrası pahalı**).

Hedefler: **1080p/60** (orta segment GPU), **1440p/60** (üst segment).
Ölçüm donanımı: **RTX 4070 Laptop, 8 GB VRAM** (üst-orta).

## Ölçüm yöntemi

`Bench_Galata.unity` — gerçek arazi, gerçek deniz, gerçek atmosfer; kamera **Galata
Kulesi tepesinde** suriçine bakıyor (Faz 4'ün kabul kriteriyle aynı bakış). Yük,
Galata çevresine 1800 m yarıçapla, yalnızca karaya, deterministik seed (1632) ile
serpilmiş `PF_BoxHouse` kopyalarıyla kademelendiriliyor.

Ölçen: `FrameTimeProbe`. **Ortalama FPS raporlanmaz** — ortalama takılmayı gizler,
oyuncu ise en kötü kareleri hisseder. Medyan ve **p95** ayrı verilir; hedefi belirleyen
p95'tir. Çözünürlük kameranın hedef dokusuyla sabitlenir (Game view boyutuna güvenmek
ölçümü tekrarlanamaz yapardı). vSync kapalı.

Ölçüm sırasında **açık olanlar:** PhysicallyBasedSky, volümetrik sis (`meanFreePath`
8000 m), 100 klux güneş + yumuşak gölge, HDRP Water (Infinite), tonemapping/bloom.

## Sonuçlar

| Yapılandırma | Üçgen | Medyan | **p95** | 60 fps bütçesinin |
|---|---|---|---|---|
| Boş arazi + deniz, 1080p | ~70 k | 3,59 ms (278 fps) | **5,08 ms (197 fps)** | %30 |
| 1 000 yapı, 1080p | | 3,55 ms (282) | 4,95 ms (202) | %30 |
| 3 000 yapı, 1080p | | 3,78 ms (265) | 4,92 ms (203) | %30 |
| **8 000 yapı, 1080p** | 229 k | **3,99 ms (251)** | **4,75 ms (211)** | **%28** |
| 8 000 yapı, **1440p** | 229 k | 5,72 ms (175) | **8,66 ms (116)** | %52 |
| Boş arazi, 1440p | | 6,19 ms (162) | 8,62 ms (116) | %52 |

Çizim çağrısı **151**, setPass **31** — plan bütçesi 1500 çizim çağrısı / 2,5 M üçgen.

## Karar: **HDRP'de kalınıyor**

Üç gerekçe:

1. **Performans kısıt değil.** Tam atmosfer yığınıyla (fiziksel gökyüzü + volümetrik
   sis + su + gölge) 1080p'de p95 **4,75 ms**; 60 fps bütçesinin dörtte biri.
   1440p'de bile yarısının altında. Bu, "URP'ye geç" gerekçesi üretmiyor.
2. **Yükün eğimi çok düşük.** 0 → 8 000 yapı arasında medyan yalnızca 3,59 → 3,99 ms
   arttı. Darboğaz çizim çağrısı ya da örnekleme değil; sabit boru hattı maliyeti.
3. **HDRP Water URP'de YOK.** Oyunun ana ekseni Boğaz üzerinde uçmak. URP'ye geçmek,
   deniz yüzeyini sıfırdan yazmak ya da satın almak demek — kazanç değil kayıp.

### Karşı gerekçe (kayda geçirilir)

HDRP'nin **üretim sürtünmesi gerçek**: bu oturumda beş ayrı sessiz tuzağa düşüldü
(ışık birimi Lux, `skyAmbientMode = Static`, dört ayrı HDRP asset'i, su kare ayarı,
volume bileşenlerinin diske yazılmaması). Hiçbiri uyarı üretmedi. Bunlar tek seferlik
öğrenme maliyetleridir ve `docs/SETUP.md`'ye tuzak olarak yazıldı; ama planın
"karmaşıklık üretimi yavaşlatıyorsa" endişesi haksız değildi.

## Bu ölçümün SÖYLEMEDİĞİ

Dürüstlük için ayrı başlık — bu sayılar aşağıdakileri **kanıtlamaz**:

- **İçerik ağırlığı.** `PF_BoxHouse` 44 üçgendir. Faz 2'nin gerçek Osmanlı evi
  20–50 kat ağır olacak; 8 000 gerçek ev ~12 M üçgen eder ve planın 2,5 M bütçesini
  aşar. LOD, impostor ve atlas (Faz 4) **zorunludur** — bu ölçüm onların yerine geçmez.
- **NPC, bitki örtüsü, kalabalık** yok.
- **Build değil, Editor Play modu.** Editor ek yükü ölçümü *kötümser* yapar, yani
  yönü güvenlidir; yine de Faz 7'de batchmode benchmark ile tekrarlanmalı.
- **Daha zayıf donanım.** 4070 Laptop üst-ortadır. Bir RTX 3060/4060 yaklaşık 1,5–2×
  yavaş → p95 ~7–10 ms; hâlâ bütçe içinde. 2016 kuşağı kartlar hedef değil.

### Ölçüm kirliliği notu

"3 000 yapı" adımında en kötü kare **943 ms** göründü. Bu bir oyun takılması değil:
o an ölçümü MCP üzerinden yokluyordum ve Editor'ü durdurdum. Medyan ve p95 etkilenmedi
(3,78 / 4,92 ms). **Ders:** ölçüm koşarken Editor'e dokunma; en kötü kare metriği
kirlenir. Medyan ve p95 dayanıklıdır, maksimum değildir.

## Yeniden üretim

```
Unity menüsü: Hezarfen → Olcum → Benchmark sahnesi kur
Play'e bas; FrameTimeProbe altı adımı sırayla ölçüp konsola yazar (~25 sn).
```

Sahne `Assets/_Project/Scenes/Bench_Galata.unity`, seed 1632 — aynı seed, aynı şehir.

## Yan çıktı: kalıcı olmayan volume bileşeni hatası

Bu ölçüm ilk koşuşunda **yanlış yapılandırmayı** ölçtü: `VP_Faz1_Sky` profilinin
bileşenleri diske yazılmamıştı (`VolumeProfile.Add<T>()` alt-nesne oluşturmaz;
`AssetDatabase.AddObjectToAsset` gerekir) ve ilk domain reload'da sessizce kayboldular.
Sis kapalıyken ölçülen sayılar iyimserdi.

Yakalanma sebebi: sonuçları yorumlamadan önce *ne ölçtüğümü* doğrulamak için sahnedeki
volume bileşen sayısını okumak. Düzeltme `SkyProfileBuilder.cs`de; profil artık reload
sonrası 4 bileşenle geliyor ve yukarıdaki tablo **düzeltilmiş** koşudandır.
