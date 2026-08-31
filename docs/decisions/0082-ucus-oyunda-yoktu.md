# ADR 0082 — Uçuş oyunun kalbiydi ve oyunda yoktu

- **Tarih:** 2026-08-31
- **Durum:** kabul edildi, ölçüm sürüyor
- **Bağlam:** yorumcu turu 1 (kontroller ve his)

## Karar

Uçuş sistemleri (`WindField`, `TerrainThermal`, `FlightHud`) oyun
sahnesine bağlanır ve uçuşun **bitirilebilirliği** tekrarlanabilir bir
ölçümle kapıya alınır: `Hezarfen → Olcum → Ucus denemesi (20 ucus)`,
kapı **varan ≥ %70**.

## Sebep

PLAN'ın bir numaralı tasarım direği şudur:

> Rüzgârı hissettir. Uçuş, oyunun kalbidir.

Ölçüldü ve kalp sahnede yoktu. `Faz1_Terrain.unity` içinde
`GlideController`, `FlightLaunch`, `PlayerFlightInput`, `UcusDizisi`
vardı; `WindField`, `WindVolume`, `TerrainThermal`, `UcusKamerasi` ve
`FlightHud` **hiçbiri** yoktu. Beşi de yazılmış, test edilmiş ve
sahneye hiç konmamıştı.

Sonucu bir tercih değil, bir **aritmetik** olarak yaz:

| ne | değer | nereden |
|---|---|---|
| Kanat en iyi süzülme oranı | **11,56 : 1** | `CL = √(cd0/k) = √(0,03/0,0624)`, `WT_Faz0_Default` |
| Trim hızı | 12,4 m/s | `√(2·m·g / (ρ·S·CL))`, m=100, S=15 |
| Batış | 1,07 m/s | trim ÷ oran |
| Kule → Doğancılar yatay | **3.336 m** | `Perde2Dilimi.kule` ↔ `.dogancilar` |
| Kot farkı | **5,4 m** | 52,0 − 46,6 |
| **Gereken süzülme oranı** | **618 : 1** | 3.336 ÷ 5,4 |
| Durgun havada menzil (52 m'den) | 601 m | 52 × 11,56 |
| Sabit 9 m/s kuyruk rüzgârıyla | 1.037 m | (12,4+9) × 48,5 s |
| **Açık** | **2.299 m** | |
| **Gereken net yükselme** | **283 m** | 3.336 ÷ 11,56 − 5,4 |

Yani oyuncu kuleden atlayıp Boğaz'a düşecek, `Perde2Dilimi` hiç
ilerlemeyecekti. Bu bir denge ayarı değil, **bitirilemeyen bir oyun**.

`GlideController` rüzgâr alanı bulamayınca `tuning.globalWind`'e
düşüyor: `(9, 0, 0)`. Şehrin her yerinde, her irtifada, her an aynı.
Termik yok, yamaç kaldırması yok. `WindTuning`'in kendi belgesi bunu
zaten öngörmüş:

> Efsanenin istediği 33:1'i fizik sabitleriyle DEĞİL, rüzgâr
> akıntılarıyla kapatıyoruz.

O akıntılar hiç konmamıştı.

## Neden hiçbir test yakalamadı

Uçuşun testleri vardı ve hepsi geçiyordu — `AerodynamicsTests`,
`KanatTests`. Hepsi kanadın **fiziğini** soruyor: taşıma katsayısı
doğru mu, stall kurtarılabilir mi, süzülme oranı beklenen mi. Hiçbiri
kanadın **gittiği yeri** sormuyordu.

Bu, bu projede tekrar eden kusurun uçuş hâli: *ölçtüğün şey doğru,
ölçmediğin şey oyunun kendisi.* Aynı desen bugün üç kez daha çıktı —
avlu eşyası sayıldı ama semt semt sayılmadı, etkileşim geçişi kendi
niyetini ölçtü etkisini değil, aydınlatma pası konsola yazdı diske
yazmadı.

## Nasıl kapatılıyor

Açık **termikle** kapatılır, fizik sabitleriyle değil (ADR 0037 kararı:
kaldıraç bir tasarım nesnesi değil, arazinin sonucu). `TerrainThermal`
bunu zaten araziden türetiyor: güneye bakan yamaçta yükselir, su
üstünde çöker, tavanı 620 m. Uçuş rotası da tam bunu istiyor —
**önce Galata yamacında yüksel, sonra Boğaz'ı geç**.

Gereken 283 m'lik kazancın gerçekten toplanıp toplanmadığı
`UcusDenemesi` ile sayılır: 20 uçuş, beş başlangıç yönü, aynı tohum,
sabit 60 Hz. Pilot bilerek basit — en iyi süzülmeyi tut, tırmanış
bulunca dön. İnsandan kötü uçar, yani ölçüm kötümser tarafta durur ve
bir kapı için doğru yön budur.

## Sonuçlar

- Uçuşun bitirilebilirliği artık bir sayı; iyileşip iyileşmediği
  turlar arasında karşılaştırılabilir.
- `TerrainThermal` sahnede olduğu için rüzgâr artık yere göre değişir;
  `WindVisualizer` ve `HavaProfili`'nin lodos anlatısı ilk kez
  oyunda karşılık buluyor.
- **Açık kalan:** `UcusKamerasi` (hıza göre FOV) hâlâ sahnede değil ve
  uçuşta fare bakışı donuk — `UcusDizisi.HavayaGec()` `WalkController`'ı
  kapatıyor, bakış açısı da onunla birlikte donuyor. Ayrı bir
  `BakisGirdisi` bileşenine çıkarılması gerekiyor.

## Seçenekler ve neden bu

1. **Kanadı güçlendir** (L/D 11,56 → 33). Reddedildi: ADR 0037 ve
   `WindTuning` belgesi bunu açıkça elemiş; 33:1 modern bir yarış
   planörünün oranıdır ve 1632'nin kanadına verilirse oyunun tarihsel
   iddiası çöker.
2. **Hedefi yakınlaştır.** Reddedildi: Doğancılar uydurma bir nokta
   değil, anlatının kendisi (Üsküdar'a iniş).
3. **Termiği bağla** — seçilen. Zaten yazılmış, zaten araziden türüyor,
   ve uçuşu şanstan beceriye çeviriyor.
