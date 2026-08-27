# İnceleme — kilise ve sinagog (Faz 2b)

**Üretim:** 2026-08-20 · **ADR:** 0018 · **Kaynaklar:** RESEARCH.md §4.2

## Ne var

| Varlık | Ne | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|---|
| `Kilise_Latin_A` | Galata Ceneviz kilisesi — üç nefli bazilika, **çan kuleli** | 19,77 × 29,37 | 19,46 | 6 852 |
| `Kilise_Latin_B` | küçük Latin kilisesi, kulesiz | 14,40 × 21,80 | 11,59 | 4 844 |
| `Kilise_Rum_A` | suriçi/Fener kilisesi — **tek beşik çatı, kulesiz, alçak** | 15,20 × 25,09 | 9,55 | 3 692 |
| `Kilise_Rum_B` | mahalle kilisesi, küçük | 12,80 × 17,62 | 8,33 | 2 226 |
| `Sinagog_A` | Balat sinagogu — kadınlar mahfilli | 12,90 × 17,36 | 10,47 | 1 772 |
| `Sinagog_B` | küçük cemaat sinagogu | 10,40 × 13,36 | 8,32 | 716 |

## Bakılacak paketler

- `renders/review/Kilise_Latin_A_v4/contact_sheet.png` *(haçsız)*
- `renders/review/Kilise_Rum_A_v3/contact_sheet.png`
- `renders/review/Sinagog_A_v2/contact_sheet.png`
- `unity/HezarfenGame/Captures/faz2_galata_kilise_1.png` — Galata sahnesinde

## Neden üç tip

1632'de **iki ayrı hukukî durum iki ayrı mimarî** üretir:

- **Galata** 1453'te fethedilmedi, **antlaşmayla teslim oldu**. Latin
  kiliseleri biçimlerini korudu — üç nefli bazilika, sivri kemer, **kare çan
  kulesi**. (San Domenico = bugünkü Arap Camii; kulesi minareye çevrildi.)
- **Suriçi/Fener/Balat**'ta zimmî kısıtı işler: kulesiz, alçak, gösterişsiz.
- **Sinagogun** kendine özgü bir mimarîsi yoktur; sokaktan **ev gibi** okunur,
  kapısı avluya bakar. Bu bir eksiklik değil, tipin kendisidir.

## Karar 5 — haç ✅ **CEVAPLANDI: B** (Caner, 2026-08-20)

> *"haç olmasın"*

Uygulandı: `KiliseParams.cross` artık **varsayılan kapalı**. Parametre duruyor
(`cross=True`) — belge çıkarsa karar geri alınabilir olmalı — ama artık onu
istemek gerekir, sessizce gelmez. `Kilise_Latin_A` 20,76 → **19,46 m**.

## Balat sahnesi ✅ (aynı gün eklendi)

Sinagog artık **avlusunun içinde** duruyor: `Hezarfen → GIS → Balat sokagi
sahnesi kur`. Balat 98 ev (9 gayrimüslim varyant), avlulu sinagog, Rum
kilisesi, 4 çıkmaz. Şadırvan **yok** — abdest içindir, sinagog avlusuna
"elimizde vardı" diye konmaz.

Bak: `Captures/faz2_balat_sinagog_2.png`

## Eksikler kapatıldı ✅ (2026-08-20, ADR 0019)

| Eksik | Durum |
|---|---|
| Kilise avlusu (mezarlık, servi) | ✅ `PlaceHazire` — servi + şahide, **batı-doğu** eksenli |
| Cami avlusu ağaçsızdı | ✅ 4 servi + sığarsa çınar; ayrıca **hazire** (kıbleye dik) |
| Apsis örtüsü yarım koni | ✅ **yarım kubbe** (konka) |
| Balat'ta çeşme yerleşmedi | ✅ 6 konum × 2 yan denenir, elenirse **loglanır** |
| Çeşme serbest duruyordu | ✅ harpuştalı **duvar kanatları** (`Cesme_A/C`) |

Yeni varlıklar: `Servi_A/B/C`, `Cinar_A/B`, `Mezar_Erkek/ErkekB/Kadin`.
Bak: `renders/review/Servi_A_v1/`, `Cinar_A_v1/`, `Mezar_Erkek_v1/`,
`Cesme_A_v3/`, `Captures/faz2_galata_hazire.png`.

**Not:** Sinagogun yanına mezarlık **konmadı** — Yahudi defni yerleşimin
dışında yapılır (Hasköy, Kuzguncuk). İlk yazımda koymuştum, dönem hatasıydı.

## İkinci tur — bu üçü de kapandı ✅ (2026-08-21, ADR 0019 §10)

| Eksik | Durum |
|---|---|
| Ağaç yaprağı dokusuz | ✅ **prosedürel üretildi** (kendi telifimiz); kabuk için `bark_platanus` indirildi |
| Sokak yüzeyi çıplak | ✅ **kaldırım** + bordür; eğimde kendiliğinden merdivenleniyor (Galata 67, Balat 76 basamak) |
| Mezar taşı kitabesiz, hazire duvarsız | ✅ oyulmuş kitabe panosu + harpuştalı duvar halkası |

Bak: `Captures/faz2_hazire.png`, `faz2_kaldirim_3.png`,
`renders/review/Cinar_A_v2/`.

## Kalan eksikler (listede)

- **Sokak seviyesi kapkaranlık** — ölçüldü 3/255. Sebep kaldırım değil,
  sahnede **dolaylı aydınlatma olmaması**; aydınlatma fazına ait.
- Ağaç tacı hâlâ katı geometri (silueti keskin); alfa kartı ayrı iş.
- Mezar taşında yazı yok (pano var, harf yok).
- Çıkmazlar da taş döşeli; gerçekte arka sokakların çoğu toprak.
- İç mekân yok — kapı ardında karanlık levha var, mekân yok.

## Onay

```
OK v1        (ya da: düzeltme istekleri)
```
