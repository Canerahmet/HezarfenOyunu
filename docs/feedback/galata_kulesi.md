# İnceleme — Galata Kulesi (1632)

**Üretim:** 2026-08-24 · **ADR:** 0033 · **Kaynak:** RESEARCH.md §5.1
**Kademe:** T1 · **Doğruluk basamağı:** **D2**

Faz 3'ün ilk landmark'ı. Oyunun dünya orijini ve simgesi.

## Bakılacak

| Paket | Ne |
|---|---|
| `renders/review/GalataKulesi_v3/contact_sheet.png` | **seçilen hâl** — saçaklı + tuğla kuşaklı |
| `renders/review/GalataKulesi_v2/contact_sheet.png` | v2 — kuşaklar henüz kesme taş (kıyas) |
| `renders/review/GalataKulesi_Mazgalli_v1/contact_sheet.png` | seçilmeyen mazgallı varyant |

Her karede 1,70 m'lik ölçü figürü var.

## Senin kararını isteyen tek şey: KÜLAH

Kaynak (Sağlam, *YILLIK*) iki dönem tasvirini karşılaştırıyor ve ikisi farklı
bir kule gösteriyor. Hangisinin 1632'ye ait olduğu **kaynaktan çıkmıyor** —
bu yüzden ikisini de yaptım.

**~~Karar 14~~ — KAPANDI (2026-08-24, Caner: "önerine göre devam edelim").**

* **(a) Saçaklı** ✅ **SEÇİLDİ** — külah basık ve geniş (8,5 m), saçağı
  mazgalların üstünden taşıyor. Toplam **46,00 m**. `PF_GalataKulesi`.
* (b) Mazgallı — külah dar ve yüksek (14 m), mazgallı siperin içinden.
  Toplam 48,50 m. `PF_GalataKulesi_Mazgalli` **duruyor**, silinmedi:
  kaynak hangisinin 1632'ye ait olduğunu söylemiyor ve bir gün yeni bir
  tasvir çıkarsa karşılaştıracak elimizde bir şey olsun.

Gerekçe: 1632'de görülen üst gövde 1510 Osmanlı onarımıdır ve saçaklı varyant
kaynağın *ikinci* (daha geç) tasvirine karşılık gelir.

## Ölçü nereden geldi

Hiçbiri uydurulmadı. Çap **16,45 m**, iç çap 8,95, duvar 3,75 — ölçülmüş
(TDV). Yükseklik iki belgeli kottan türedi:

> II. Mahmud 1831'de **32,60 m'den yukarısını** yıktırdı; o kot 1794 yangını
> onarımında zaten **1,90 m alçaltılmıştı** → 1632'de kâgir gövde **≈34,5 m**.

Üstüne kurşun kaplı külah. Toplam 46 m — **bugünkü 62,59 m'nin altında**,
çünkü 1831 sofası ve 1875 sekizgen gözlem katları 1632'de yok. Bu, testin
kilitlediği baş iddia.

## İki şeyi araştırma düzeltti

**Yaygın İngilizce iddia yanlışmış.** Birçok kaynak konik çatıyı II.
Mahmud'un 1832'de *eklediğini* söylüyor. Evliya Çelebi kuleyi *"tepesinde
kurşun kaplı bir külah"* ile tarif ediyor ve 1794'te **yanan** bir ahşap
külah kaydı var. Mahmud külahı eklemedi, yenisini yaptı. Projenin eski notu
doğruymuş.

**Evliya'nın sayısını kullanmadım.** Kuleyi "118 mimarî arşın" diyor; bu
≈89 m eder, bugünkünden bile yüksek. Tanıklığını külahın **varlığı** için
kullandım, **boyu** için kullanmadım.

## Render'ın gösterdiği iki kusur (düzeltildi)

- **24 mazgalın hiçbiri görünmüyordu** — saçak hepsini yutuyordu. Oysa kaynak
  "saçaklar mazgallardan taşar" diyor, yani mazgal görünür. Külah artık kendi
  ahşap kasnağının üstünde.
- **Pencere düzeni sarhoş okunuyordu**: sıralar tuğla kuşakların üstüne
  biniyordu, her sıra farklı faz alıyordu ve gövdenin üst 12 metresi bomboştu.

## Bildiğim eksikler

- ~~Tuğla kuşakların dokusu yok~~ — **KAPANDI (v3).** Prosedürel tuğla
  dokusu üretildi; kuşaklar artık tuğla okunuyor
  (`renders/review/GalataKulesi_v3/`). Karo boyu seçilmedi, belgeli tuğla
  (35×35×4,5 cm) ve derz (2,5–3 cm) ölçülerinden hesaplandı.

  Ölçüm bir tuzak gösterdi ve kaydetmeye değer: **tuğla tek başına taştan
  ΔE 30,8, harç 23,3 — ama karışımlarının ortalaması ΔE 12,3 çıktı**, yani
  taşın tam üstüne düştü. İki bileşeni de apayrı olan bir doku uzaktan
  ayırt edilemez olabiliyor. Eşiği indirmek yerine dokuyu düzelttim;
  şimdi ΔE 21,1.
- **Kapı düz bir dikdörtgen**; kemer ve söve yok. Kapı üstündeki kitâbe 1832
  onarımını anar, **YOK** ve öyle kalmalı.
- **İç mekân yok.** 1632'de kule tersane levazım ambarı ve zindandı.
- ~~Kule henüz sahneye konmadı~~ — **KAPANDI.** Kule dünya orijininde
  (`Faz1_Terrain`, ve ondan türeyen semt sahnelerinde). Kot **52,2 m**, ayak
  izi altında kot farkı **0,37 m**, kapı **205°** — yokuş aşağı, limana bakıyor
  (yön eğimden türedi, elle yazılmadı). Kareler:
  `Captures/kule_yaya.png`, `kule_hava.png`, `kule_ucus.png`.

  Yerleştirici **genel**: katalogdaki 22 landmark'tan üretilmiş olanları
  koyuyor, kalan 21'i "henüz üretilmedi" diye yazıyor. Faz 3 ilerledikçe
  satır eklenecek.
- **Kule tek başına duruyor.** Galata surları da S-kademe landmark'ı ve henüz
  üretilmedi; semtin kendisi Faz 4.
- **`refs/` altına hiçbir görsel indirilmedi.** Model metinsel ve metrik
  kaynaklardan kuruldu. Lorck panoraması bu yapı için işe yaramıyor: Lorck
  Galata'dan **şehre** bakıyor, kule arkasında kalıyor. Kullanılabilir kamu
  malı tasvirler (Matrakçı Nasuh 1537, Braun-Hogenberg 1572, Pîrî Reis 1629)
  sıradaki turda. **D1'e çıkmanın tek yolu kamu malı ölçülü çizim bulmak** ve
  şimdilik yok — bu yüzden D2.

## Onay

```
OK v1        (ya da: düzeltme istekleri)
Karar 14:    (a) saçaklı  /  (b) mazgallı
```
