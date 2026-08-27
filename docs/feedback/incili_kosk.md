# İncili Köşk (Sinan Paşa Köşkü) — inceleme notu

İnceleme paketi: `renders/review/IncliKosk_v2/contact_sheet.png`
Karar kaydı: **ADR 0039**. Araştırma: RESEARCH.md §5.6.

## Bu yapı neden burada

**Evliya'ya göre IV. Murad, Hezarfen'in uçuşunu buradan izledi.** Finalin
kamerası bu yapıya bakar. Anlatı tek kaynaklı (T3) ama yapının kendisi
belgeli (T1): 998-999/**1590-91**, Koca Sinan Paşa, mimar **Dâvud Ağa**.
1632'de 41 yaşında. **1871-72**'de sahil demiryolu için yıkıldı.

## Konumu 156 m düzelttim — ve ölçerek

Katalog değeri yapıyı **denizden 125 m içeride, 14,7 m yukarıda**
bırakıyordu; oysa köşk denize **taşar**. Ölçülü koordinat yok (yapı yıkıldı),
ama kaynak ölçülebilir bir tarif veriyor: *"Sarayburnu'ndan kıyı boyunca
~300 m."* Bunu kendi 1632 kıyı çizgimizde uyguladım: uçtan 300 m'de su hattı
kot **0,1 m**. Konum uydurulmadı, **ölçüldü**.

Bu turun üçüncü koordinat hatası (Doğancılar 771 m, Mihrimah 164 m, bu 156
m). Artık şunu söyleyebilirim: kataloğun `APPROX` etiketli her koordinatı
şüpheli.

## Sayılan özellikler

Ölçü yok ama kaynak sayı veriyor, ve hepsi teste bağlı:

- çıkmanın yanlarında **Sarayburnu tarafında 1, Ahırkapı tarafında 2 kemer**
  (asimetri belgeli — simetrik yapmak "daha düzgün" görünürdü, o yüzden
  kilitledim)
- esas mekânın **dört köşesinde birer baca**
- denize açılan **çift kemerin arasında çeşme**
- ahşap konsollara oturan, denize **taşan cumba**

## Render iki kusur gösterdi, ikisini de düzelttim

1. **Cumba düz bir sıva levhasıydı**, tabela gibi okunuyordu. Oysa
   padişahlar töreni "köşkün **pencerelerinden**" seyrediyor — camekânsız
   cumba, cumba değil. Ahşap iskelet + beş gözlü cam şeridi eklendi.
2. **Bacalar köşe payandası gibiydi**; inceltip yükselttim, külah ekledim.

## Sana sorduğum — bir karar gerekiyor

**Örtü tartışmalı ve iki varyant ürettim:**

- **`IncliKosk`** — kubbeli (TDV'nin tarifi: ortada yükselen kare kütle +
  kubbe)
- **`IncliKosk_Ahsap`** — piramidal ahşap örtü (**Sedat Hakkı Eldem** gerçek
  örtünün ahşap olduğunu savunur)

Şu an **kubbeli** olan sahnede. Galata Kulesi külahında da aynı yolu
izlemiştik (ADR 0033). Hangisi dursun?

## Bilerek eksik

- Köşkün oturduğu **Bizans deniz suru** henüz yok; alt yapı tek başına
  duruyor. Deniz surları ayrı bir landmark.

---

**Onay**: _(bekliyor — "OK vN" yaz)_
