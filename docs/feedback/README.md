# docs/feedback/ — Caner'in İnceleme Notları

Varlık başına bir dosya: `<varlık>.md` (ör. `ottoman_house.md`, `galata_tower.md`).

**Döngü (plan Bölüm 4):**
1. Claude `renders/review/<varlık>_vN/` altında inceleme paketi üretir (4-açı + yakın plan + referans kolajı).
2. Caner serbest metinle not verir — "cumba %20 daha derin", "külah daha sivri", "ışık daha sıcak".
3. Claude notu buraya **tarih + sürüm** ile loglar, uygular, `vN+1` paketini üretir.
4. Onay: **"OK vN"** → varlık o sürümde kilitlenir. Sonraki değişiklik yeni sürüm açar.

**Dosya şablonu:**

```markdown
# <varlık>

## v1 — 2026-08-17
**Paket:** renders/review/<varlık>_v1/
**Caner notu:** (buraya birebir yapıştırılır)
**Uygulanan:** (Claude'un yaptığı değişiklikler)

## v2 — ...
```

<!-- INDEKS BASLANGIC (uretilmis; tools/olcum/geri_bildirim_indeksi.py) -->

## İçindekiler

_50 inceleme dosyası. Bu bölüm üretilmiştir; elle düzenlenmez._

| dosya | ne | durum |
|---|---|---|
| [arap_camii.md](arap_camii.md) | Arap Camii (San Domenico) — inceleme | soru bekliyor |
| [arazi_ortusu.md](arazi_ortusu.md) | İnceleme — arazi örtüsü (Faz 1 geri dönüşü) | onaylı |
| [ayasofya.md](ayasofya.md) | Ayasofya — inceleme notu | kayıt |
| [ayrinti_gecisi.md](ayrinti_gecisi.md) | Ayrıntı geçişi — inceleme paketi | onaylı |
| [bedestenler.md](bedestenler.md) | Bedestenler — inceleme notu | kayıt |
| [beyazit.md](beyazit.md) | Beyazıt II Camii — inceleme notu | kayıt |
| [box_house.md](box_house.md) | Geri bildirim günlüğü — Kutu Ev (`BoxHouse`) | onaylı |
| [coastline_1632.md](coastline_1632.md) | Geri bildirim günlüğü — 1632 Kıyı Çizgisi | onaylı |
| [denetim_turu.md](denetim_turu.md) | Denetim turu — Faz 0'dan bugüne | kayıt |
| [dogancilar.md](dogancilar.md) | Doğancılar Meydanı — inceleme notu | kayıt |
| [ev_cesitliligi.md](ev_cesitliligi.md) | Ev çeşitliliği — kapının üç kriteri, ölçüldü | soru bekliyor |
| [ev_ic_mekani.md](ev_ic_mekani.md) | Ev iç mekânı — ölçülen durum | soru bekliyor |
| [fatih_camii.md](fatih_camii.md) | Fâtih Camii — inceleme notu | kayıt |
| [faz3_kapi.md](faz3_kapi.md) | Faz 3 — Kapı Paketi | onaylı |
| [faz4_kapi.md](faz4_kapi.md) | Faz 4 — Kapı Paketi | onaylı |
| [faz5_kapi.md](faz5_kapi.md) | Faz 5 — Kapı Paketi | kayıt |
| [faz6_kapi.md](faz6_kapi.md) | Faz 6 kapısı — açık dünya, NPC yapay zekâsı ve içerik | soru bekliyor |
| [faz7_kapi.md](faz7_kapi.md) | Faz 7 kapısı — görsel cila ve performans | soru bekliyor |
| [faz7_performans.md](faz7_performans.md) | Faz 7 — performans ölçümü (ilk tur) | kayıt |
| [galata_kulesi.md](galata_kulesi.md) | İnceleme — Galata Kulesi (1632) | onaylı |
| [galata_surlari.md](galata_surlari.md) | İnceleme — Galata surları ve kapıları (1632) | onaylı |
| [hamam_han.md](hamam_han.md) | İnceleme — hamam ve han (Faz 2b) | onaylı |
| [incili_kosk.md](incili_kosk.md) | İncili Köşk (Sinan Paşa Köşkü) — inceleme notu | kayıt |
| [iskele_ve_alay.md](iskele_ve_alay.md) | Üsküdar iskelesi ve Alay Köşkü — inceleme notu | kayıt |
| [kanat.md](kanat.md) | Kanat aygıtı — inceleme paketi v2 | onaylı |
| [kara_surlari.md](kara_surlari.md) | Kara surları — inceleme notu | kayıt |
| [karakter.md](karakter.md) | Karakter ve kıyafet — inceleme paketi v5 | onaylı |
| [kible.md](kible.md) | Kıble — şehrin bütününü ilgilendiren bir düzeltme | kayıt |
| [kilise_sinagog.md](kilise_sinagog.md) | İnceleme — kilise ve sinagog (Faz 2b) | onaylı |
| [kiz_kulesi.md](kiz_kulesi.md) | Kız Kulesi — inceleme notu | kayıt |
| [mahalle_sahnesi.md](mahalle_sahnesi.md) | İnceleme — Galata mahallesi (Faz 2b KABUL sahnesi) | onaylı |
| [medrese_sebil_firin.md](medrese_sebil_firin.md) | İnceleme — medrese, sebil, fırın (Faz 2b) | onaylı |
| [mihrimah_kulliyesi.md](mihrimah_kulliyesi.md) | Mihrimah Külliyesi — medrese ve sıbyan mektebi | kayıt |
| [okmeydani.md](okmeydani.md) | İnceleme — Okmeydanı: menziller, namazgâh, tekke (Faz 2b) | onaylı |
| [okmeydani_konsolidasyon.md](okmeydani_konsolidasyon.md) | Okmeydanı — konsolidasyon notu | kayıt |
| [ottoman_house.md](ottoman_house.md) | Geri bildirim günlüğü — Osmanlı konutu (Faz 2 kiti) | onaylı |
| [oyun_geri_bildirim.md](oyun_geri_bildirim.md) | Oynarken gelen geri bildirim — Faz 8 | kayıt |
| [sakin_kadin.md](sakin_kadin.md) | Şehir sakini — kadın (ferace + yaşmak) | soru bekliyor |
| [suleymaniye.md](suleymaniye.md) | Süleymaniye Camii — inceleme notu | kayıt |
| [sultanahmet.md](sultanahmet.md) | Sultanahmet Camii — inceleme notu | kayıt |
| [topkapi.md](topkapi.md) | Topkapı silueti — inceleme notu | kayıt |
| [turbe_mektep_kahvehane.md](turbe_mektep_kahvehane.md) | İnceleme — türbe, sıbyan mektebi, kahvehane + kurşun dokusu (Faz 2b) | onaylı |
| [turbeler.md](turbeler.md) | Padişah türbeleri — inceleme notu | kayıt |
| [uretim_su_yapilari.md](uretim_su_yapilari.md) | İnceleme — üretim, ticaret ve su yapıları (Faz 2b'nin kalanı) | onaylı |
| [uskudar_mihrimah.md](uskudar_mihrimah.md) | Üsküdar Mihrimah Sultan (İskele) Camii — inceleme notu | kayıt |
| [walls_districts.md](walls_districts.md) | Geri bildirim günlüğü — Sur hatları ve semtler | onaylı |
| [yedikule.md](yedikule.md) | Yedikule Hisarı ve kara sur kapısı — inceleme notu | kayıt |
| [yeni_cami_harabe.md](yeni_cami_harabe.md) | Yeni Cami harabesi ("Zulmiye") — inceleme notu | kayıt |
| [yesil_doku.md](yesil_doku.md) | İnceleme — yeşil doku (Faz 1c) | onaylı |
| [yorumcu_turlari.md](yorumcu_turlari.md) | Yorumcu turları — inceleme kaydı | soru bekliyor |

<!-- INDEKS SON -->
