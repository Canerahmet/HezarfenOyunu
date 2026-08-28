# ADR 0064 — Kanat aygıtı: plan yokken tasarım nereden gelir

**Durum:** Kabul edildi (uygulandı)
**Tarih:** 2026-08-28
**Bağlam:** Faz 5, ilk varlık

---

## Sorun

Oyunun adı bir uçuştan geliyor ama **o uçuşun aygıtının planı yok.**

RESEARCH.md'nin kaydettiği şey şu: olayın tek tanığı Evliya Çelebi'dir,
Murad IV'ün verdiği söylenen kese altının mali kayıtlarda izi yoktur,
uçuşun tarihi kaynak içinde bile çelişir (1632 / 1638), aerodinamik
uzmanları Galata'dan Doğancılar'a süzülmek için gereken ~55:1 oranını
imkânsız bulur (modern delta kanat ~15:1), ve Dankoff Evliya'nın
sistematik abartı üslubunu belgeler.

Yani kanat hakkında bilinen tek şey **bilinmediğidir.** Bu durumda üç
yol var ve üçü de aynı derecede meşru değil:

- **(A) Bir plan uydur** ve tarihîymiş gibi sun. Bu, projenin T1/T2/T3
  ayrımının varlık sebebine aykırı: uydurmayı belgeye eşitler.
- **(B) Kanadı hiç gösterme.** Uçuş oyunu, uçuş aygıtı olmadan olmaz.
- **(C) Tasarımı MALZEME KURALINDAN türet**, T3 (Efsane) etiketle ve
  planın olmadığını varlığın kendi kaydına yaz.

## Karar

**(C).** Plan Bölüm 10 malzemeyi zaten söylüyor: *ahşap çıta iskelet +
kartal tüyü yüzey + deri kayış*. Biçim oradan çıkar — 1632'de bir
zanaatkârın elinde ne varsa o. Merkezde omurga, yelpaze gibi açılan
çıtalar, uçları bağlayan hücum kenarı: yarasa kanadı ve uçurtma
mantığı, çünkü dönemin insanının gözlemleyebildiği şey buydu.

`sourceNote` alanı bunu **varlığın içinde** taşır ve ilk cümlesi
"TARİHÎ PLAN YOKTUR"dur. Kodeks ekranında oyuncu bunu okuyacak. Bu bir
özür değil: oyunun en dürüst özelliği.

## Tek sert sayı: alan

Serbestlik biçimdedir, **fizikte değil.** `WindTuning.wingArea` 15 m²
ve uçuş bütçesi (`FlightBudget`, `ThermalFlightSim`) o sayıyla ölçüldü.
Görünen kanat başka bir alana sahip olursa oyuncu **bir şey görüp başka
bir şeyin fiziğini yaşar** — ve bu, hiçbir sanat kararının ödeyemeyeceği
bir bedeldir.

Bu yüzden üretici alanı **ölçer** (`_mesh_area`) ve %6'dan fazla
sapmada durur. İlk koşuda gerçekten durdu: 13,63 m². Elle düzeltmek
yerine bağımlılık ters çevrildi — açıklık artık alandan **türüyor**:

```python
SPAN = TARGET_AREA / ((ROOT_CHORD + TIP_CHORD) * 0.5)   # 9,46 m
```

Böylece veter değişince açıklık kendini düzeltir; sayı iki yerde
yazılmaz.

## Üç durum

| durum | açıklık | alan | üçgen | ne için |
|---|---:|---:|---:|---|
| `Kanat_Acik` | 9,46 m | 15,00 m² | 772 | uçuş |
| `Kanat_Katli` | 2,84 m | 4,50 m² | 772 | sırtta taşınan; kule merdiveninde bu |
| `Kanat_Kirik` | **7,85 m** | 13,47 m² | 640 | kaza sonrası |

## Dihedral 7° — süs değil

Düz bir levha yuvarlanmaya karşı kayıtsızdır. Dihedral alçalan yarının
hücum açısını artırır ve kanat kendini toplar. 1632'de bunun adı yoktu
ama uçurtma yapan herkes biliyordu; yarasa kanadı da düz değildir.

İzdüşüm kaybı %0,7 — alan bütçesini bozmaz. `_mesh_area` zarı **düzken**
ölçer, çünkü fizik yüzey alanını değil izdüşüm alanını ister.

## Bu turun bulduğu üç yanlış

Üçü de **sayıyla** bulundu, hiçbiri render'la:

1. **Kırık kanat 15,00 m² bildiriyordu.** Çıtalar ve tüyler düşüyordu
   ama zar tamdı — yani kırık kanat sağlamıyla aynı fiziği taşırdı. Zar
   artık asimetrik kısalıyor: 13,47 m².
2. **Katalog nominal açıklığı yazıyordu** (9,46), ölçüleni değil. Kırık
   kanadın gerçek açıklığı 7,85 m. Katalog artık ölçtüğünü yazar,
   istediğini değil.
3. **LOD1 dört üçgendi.** 772'den 4'e düşmek bir merdiven değil, bir yok
   oluş — ve kanadın okunan şeyi zar değil, yelpaze gibi açılan çıta
   silueti. Artık 88 üçgen (%11). Render bunu gösteremezdi: render hep
   LOD0'ı çizer.

Ayrıca boru hattı **"3 model yerleştirildi"** dedi ve üç kanadın da
HistoricalTag'i Graybox kaldı: katalog anahtarını `SM_` önekiyle
yazmıştım, diğer on katalog çıplak ad kullanıyor. *Başarılı görünen bir
adım eksik iş yapabilir.*

## Sonuç

Kanat T3, `status: draft`. Caner'in onayı (`OK vN`)
`docs/feedback/kanat.md`'ye yazılacak.

İlgili: ADR 0037 (termik), ADR 0005 (varlık hattı), ADR 0063 (LOD).
