# İnceleme — medrese, sebil, fırın (Faz 2b)

**Üretim:** 2026-08-21 · **ADR:** 0022 · **Kaynak:** RESEARCH.md §4.3(d)(e)(f)

| Varlık | Ne | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|---|
| `Medrese_A` | revaklı avlulu medrese, dershaneli | 28,32 × 28,82 | 7,83 | 7 734 |
| `Medrese_B` | küçük medrese, dershanesiz | 22,32 × 19,21 | 5,64 | 6 322 |
| `Sebil_A` | sekizgen sebil, geniş konsollu saçak | 5,91 × 5,91 | 5,89 | 2 124 |
| `Firin_A` | mahalle fırını, arkada kubbeli ocak | 8,00 × 12,92 | 8,39 | 486 |
| `Firin_B` | küçük fırın, dar parsel | 7,00 × 11,20 | 7,94 | 486 |

## Bakılacak paketler

- `renders/review/Medrese_A_v2/contact_sheet.png`
- `renders/review/Sebil_A_v2/contact_sheet.png`
- `renders/review/Firin_A_v1/contact_sheet.png`
- `unity/HezarfenGame/Captures/faz2_medrese.png` — medrese, han ve hazîre bir arada
- `unity/HezarfenGame/Captures/faz2_sebil.png` — sebil çekirdeğin köşesinde, solunda fırın

## Medrese: han ile aynı gramer, farklı cümle

İkisi de avlu + revak + kubbeli dam. Ayıran üç şey var, üçü de siluetten okunur:

| | Han | Medrese |
|---|---|---|
| Kat | **iki** | **tek** |
| Dam kubbeleri | oda başına, bacalar seyrek | **hücre başına eşit**, her kubbede **baca** |
| Ritmi kıran | yok | **dershane** — tek büyük kubbe |

Bacaların sayısı anlamlıdır: her hücrede bir ocak var, yani yapı kaç talebe
barındırdığını damından söyler.

## Sebil çeşme değildir

Aynı şeyi verirler, farklı biçimde: **çeşmeden kendin alırsın, sebilden sana
verilir.** İçeride bir görevli durur; bu yüzden sebil bir niş değil küçük bir
odadır ve her şebekeli pencerenin önünde bardağın uzatıldığı bir **mermer
tezgâh** vardır. Geniş saçak bekleyeni örter — kısa saçaklı bir sebil, sekizgen
bir kule olur.

## Fırını fırın yapan şey arkasıdır

Cepheden dükkândır: kemerli açıklık, taş tezgâh, sundurma. Arkada kâgir
**kubbeli ocak** ve **kalın, yüksek baca** var. Baca damdan en az 2 m yükselir
ve bu keyfî değil: ahşap bir dokuda kıvılcım komşu çatıya düşmemeli — 1633
Cibali yangınının hatırlattığı risk tam budur.

## Yerleşim (ölçüldü, Galata)

| | mescide | en yakın eve |
|---|---|---|
| Sebil | 18,7 m | 21,9 m |
| Fırın | 28,6 m | 14,1 m |
| Medrese | 45,9 m | 21,8 m |

**Balat'ta medrese ve sebil yok, fırın var.** İkisi de müslüman vakıf
kurumudur; ekmek ise cemaate göre değişmez.

## Öğrendiğim şey: taçkapı damı aşar

Doğrulama medreseyi ilk üretimde reddetti — sivri kemerli 2,60 m'lik kapı
5,65 m yükseklik istiyordu, medrese ise tek katlı ve 3,90 m. Kapıyı daraltmak
yanlış cevaptı: taçkapı zaten **öne taşan ve damı aşan ayrı bir kütledir**,
adının "tâc" olmasının sebebi bu. Kısıt yapının katına değil kapının kendi
bloğuna uygulanıyor artık. Medrese_A'da cephe 3,90 m, taçkapı 4,90 m.

## Sonradan: sokak artık görünüyor

Bu turdan sonra **geçici aydınlatma** kuruldu (ADR 0023) ve yaya seviyesinden
kare alınabiliyor. Yeni bakılacaklar:

- `unity/HezarfenGame/Captures/faz2_sokak_firin.png` — fırın, sokaktan
- `unity/HezarfenGame/Captures/faz2_sokak_han.png` — hanın taçkapısı, göz hizası
- `unity/HezarfenGame/Captures/faz2_sokak_mahalle.png` — mescit, sebil, servi
- `unity/HezarfenGame/Captures/cephe_galata_once.png` ↔ `cephe_galata_sonra.png`
  ve `cephe_balat_once.png` ↔ `cephe_balat_sonra.png` — aynı cephe, öncesi/sonrası

Işık gelir gelmez bir kusur da göründü ve düzeltildi: fırının kemer başlığının
arkası açıktı, açıklığın üstünden çatının altı görülüyordu. Karanlıkta üç tur
boyunca fark edilmemişti.

> Bu aydınlatma **geçicidir** ve fizikî değildir; kalıcı ışık pası (Faz 7)
> onun üstüne değil **yerine** kurulacak.

## Bildiğim eksikler

- **Arazi dokusu artık en zayıf halka**: yaya seviyesinde zemin düz bir kum
  rengi, yamaç çıplak bir kütle. Işık gelmeden görünmüyordu.
- Kaldırım yalnız ana sokakta; çoğu yerde zemin çıplak arazi.
- İç mekân yok: dershane, ocak, sebilin içi.
- Sebilin şebekesi düz demir çubuk; gerçekte dökme tunç kafes olurdu.
- Medresenin avlusunda şadırvan yerine kuyu var (`Sadirvan` prefabı mevcut,
  medreseye yerleştirilmedi).

## Onay

```
OK v1        (ya da: düzeltme istekleri)
```
