# Faz 5 — Kapı Paketi

**Durum:** kriterler **ölçümle** karşılandı; onay tüm fazlar bitince
(Caner, 2026-08-28: geri bildirim oyunu oynarken tek tek gelecek)
**Tarih:** 2026-08-28

---

## Kabul ölçütü ne diyordu

> *"Kule tepesinde kuşanma → atlayış → süzülüş → Doğancılar inişi,
> **kesintisiz animasyonlarla oynanabiliyor**; karakter inceleme paketi
> Caner onaylı."*

Onay kısmı sona bırakıldı. Geri kalanı **ölçüldü**.

## 1. Kesintisiz mi — koşarak doğrulandı

Kurulumun doğru olduğunu EditMode testleri söylüyordu: kemik var, klip
bağlı, eşikler tutuyor. Ama **"kurulum doğru" ile "çalışıyor" aynı şey
değil**, ve aradaki farkı ancak çalıştırmak gösterir.

`UcusDizisiTests` (PlayMode, **14/14 yeşil**) zinciri gerçekten koşturur:

| ne sınanıyor | neden önemli |
|---|---|
| Yerde başlar, kapsül açık, Rigidbody kinematik | Kinematik olmayan bir gövde kendi ağırlığıyla zemine gömülür |
| Kuşanma bir **süredir**; bitmeden atlanamaz | Yoksa oyuncu kanat sırtındayken uçar |
| Atlayınca fizik **tam bir kez** el değiştirir | Açık kalan kapsül Rigidbody'nin her kuvvetini yutar — "uçuyorum ama düşmüyorum" |
| İnince zincir başa döner | Tek yönlü bir zincir ilk inişte oyunu bitirirdi |
| Sert çarpma **çakılmadır**, iniş değil | Ayrım olmasa uçuşun tek gerçek riski yok olurdu |

**Bu test yazılmasaydı bir hata oyuna girecekti:** dizi girdiyi
`UnityEngine.Input` ile okuyordu, oysa proje yeni Input System'e geçmiş.
O kipte eski API **çalışma anında istisna atar** — derleme sessizdir.
İlk oynayışta çıkardı.

## 2. Uçuş hâlâ yapılabilir

`ThermalFlightSim`, oyuncunun kurulduğu noktadan (Galata şerefesi,
0/98,2/0 — `HezarfenSpawner` ile **aynı** +46 m):

```
mesafe 3278 m, suzulme 11,48:1, alcalma 1,08 m/s, ruzgar 9,0 m/s
en iyi kaldirac 1,79 m/s, kuleden 380 m
gereken irtifa 246 m
tirmanis 298 s (5,0 dk) -> 246 m
gecis  163 s (2,7 dk), varis kotu 50,3 m (hedef 46,1 m)
```

İki yerde iki farklı kule tepesi olsaydı uçuş sınavıyla oyun aynı
uçuştan bahsetmiyor olurdu.

## 3. Karakter — ölçülen sayılar

| ölçü | değer | neden bu |
|---|---:|---|
| boy | **1,700 m** | Bütün inceleme paketleri 1,70 m'lik figüre göre yargılandı |
| baş oranı | 1/7,7 | yetişkin 1/7–1/8 |
| omuz | 0,472 m | biakromiyal + deltoid |
| giyinik boy | 1,793 m | sarık 9 cm ekler |
| üçgen | 62 040 / 18 612 | LOD0 / LOD1 |
| kemik | **22** | Unity Humanoid'in tam takımı, avatar **2/2 geçerli** |

Taban CC0 (kayıt `art/base/blender-studio/meta.json`), kıyafet Rålamb
plakalarından **okundu** (T2 — albüm 1657, oyun 1632, ve tam ortada
kaynak yok), saç/sakal prosedürel kart dokusuyla (kendi telifimiz).

## 4. Animasyon — 13 klip, ölçülü

| klip | süre | tempo | ayak kayması |
|---|---:|---:|---:|
| Yürüme | 1,10 s | 110 adım/dk | **0,7 cm** |
| Koşma | 0,73 s | 165 adım/dk | **1,8 cm** |
| Merdiven | 1,27 s | 96 adım/dk | **0,4 cm** |

Diğerleri tek atımlık: duruş, kuşanma, kalkış, süzülüş (+4 blend ucu),
iniş, çakılma.

**Animasyon bu projenin ölçemediği ilk şey gibi görünüyordu. Değilmiş.**
Yere basan ayak kaymamalı; bu bir sayı. Tempo da ölçülüyor, çünkü tek
başına kaymayı sıfırlamak yetmiyor — adımı ve süreyi birlikte uzatarak
da sıfırlanır ve karakter 74 adım/dakika ile cenaze temposunda yürür.

## 5. Kanat

| durum | açıklık | alan | üçgen |
|---|---:|---:|---:|
| Açık | 9,46 m | **15,00 m²** | 772 |
| Katlı | 2,84 m | 4,50 m² | 772 |
| Kırık | 7,85 m | 13,47 m² | 640 |

Alan `WindTuning.wingArea` ile aynı olmak zorunda ve iki zincirde birden
bağlı: Blender üretimde ölçüyor, `KanatTests` Unity'de karşılaştırıyor.

## 6. Bu fazın bulduğu yanlışlar

Faz 3 ve 4 gibi, kapıda asıl bakılacak şey bunlar. Hepsi **ölçümle**
bulundu, hiçbiri gözle:

- **Yön ölçümü ayağın dış yanını buluyordu** (iki ayak arası açıklık
  parmaktan uzun) → omuz 0,247 m çıktı, ölçü aleti kendi hatasını
  raporladı.
- **"En dar dilim boyundur" araması kafatasının tepesini buldu** → boyun
  0,088 m. Nerede olabileceğini bilmeyen bir arama hep kenarı bulur.
- **Getirilen gövdenin paket içinde kendi konumu vardı** → giyinik
  karakter 3,29 m genişliğinde.
- **Kesit ölçüsü A-pozunda kolları sayıyordu** → kuşak 0,84 m çapında.
- **Şalvar entariden çok şişiyordu** → alt katman üstü deldi. Kural:
  üst katman her kotta daha çok şişer.
- **Kısa etek dizin altında bitiyordu** → dizlik üretiliyor ama hiç
  görünmüyordu. *Üretilen ama görünmeyen bir öğe, olmayan bir öğedir.*
- **Animasyonda yedi hata ve yedisi de ölçüm aletindeydi** — temas
  evresinin dairesel olması, pencerenin iki ayağa da aynı verilmesi,
  ölçülen kemiğin IK'nın yerleştirdiği kemik olmaması. *Bozuk olan çoğu
  zaman ölçtüğün şey değil, ölçme biçimin.*
- **Unity mesh'siz FBX'ten klip üretmiyor** — 13 klibin hiçbiri
  gelmemişti. Tahmin edilmedi, deneyle ayrıldı (avatar ayarı sabit, tek
  değişken mesh).
- **51 sakal kartı kafanın içinde kalıyordu** — çene tek bir kot değil
  bir bölge.

## 7. Unity'de duran bir çelişki de düzeldi

Model 1,70 m, yürüyüş kapsülü **1,80 m**, göz **1,70 m** (yani 1,81 m'lik
bir adamın gözü). Göz alanının yanındaki not *"1,70 m: ölçü figürüyle
aynı"* diyordu — **1,70 o figürün boyu, gözü değil.** Bir yorum, bir
sayının yanlış olduğunu gizleyebilir.

Göz artık 1,59 m (modelin kendi ölçüsünden: `boy − baş/2`), kapsül
1,70 m. **Şehir 11 cm daha alçaktan görünüyor** — onaylandığı yerden.

## 8. Doğrulama

- EditMode **268 / 268 yeşil**, sıfır atlanan
- PlayMode **14 / 14 yeşil**, sıfır atlanan
- `Assets/_Import` boş
- git + LFS, `main` güncel ve push'lu
- ADR 0064–0068

## 9. Bilerek yapılmayanlar

- **Kanat rig'i** (açılma/çırpma/hasar geçişleri) — üç ayrı mesh var,
  geçişler Faz 7 cila turunda.
- **Kaş, kirpik, yüz detayı** — Hezarfen'in portresi yok; olmayan bir
  yüzü ayrıntılandırmak iddia üretmek olurdu.
- **Kumaş/tüy simülasyonu** — çalışma zamanı maliyeti ölçülmeden karar
  verilmez.
- **MakeHuman / Mixamo** — Caner'in isteğiyle tüm fazlar bitince
  bakılacak; lisans ve entegrasyon zemini [ADR 0068](../decisions/0068-karakter-yukseltme-yolu.md)'de hazır.
