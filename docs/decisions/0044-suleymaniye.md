# ADR 0044 — Süleymaniye: tanıdık siluetin doğru olduğu yer

- **Tarih**: 2026-08-26
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/suleymaniye.md`)
- **Bağlam**: Faz 3, A-kademe.

## Karar 1 — Burada "1632 farklıdır" refleksi **yanlış** olurdu

Faz 3 boyunca beş yapıda tanınan siluet sonraki yüzyılların eseri çıktı
(Galata Kulesi, Adalet Kulesi, Kız Kulesi, Yeni Cami, Alay Köşkü).
**Süleymaniye 1557'de tamamlandı ve 1632'ye kadar biçimini değiştiren bir
olay yok**; 1660 yangını ve 1766 depremi sonradır.

Bunu açıkça yazmak refleksi dengeler: kural "her şey farklıdır" değil,
**"her şey sorulur"**. Bir yapının değişmemiş olması da bir bulgudur ve
kayda geçer.

## Karar 2 — Sayılar geometriyi bağlar

Kubbe **26,5 m / 53 m** (D2); **iki** yarım kubbe ana eksende; **dört**
minare, **on** şerefe (3+3+2+2). Üçü de teste bağlandı. Kubbe birleşmeden
önce mesh'ten ölçülüyor (Galata dersi, ADR 0033).

Kubbe çapındaki 26,5 / 27,5 ikiliği Mihrimah ve Yeni Cami'dekiyle aynı
**iç/dış** ikiliğidir — üçüncü kez aynı desen; artık bir kayıt değil bir
beklenti.

## Karar 3 — Avlu modellendi

Kısa iki minare avlunun **dış köşelerindedir**. İlk kurulumda avlu yoktu
ve o minareler render'da **boşlukta** duruyordu. Yapının yarısını atlayınca
öteki yarısı da yanlış okunuyor. Avlu duvarı, kubbeli revak sırası ve
şadırvan eklendi.

## Karar 4 — Test süzgeci **türe değil ada** göre

Süleymaniye de `kind="selatin"` olunca Üsküdar Mihrimah'ın iddiaları
(11,40 m kubbe, üç yarım kubbe, çifte minare) ona uygulandı ve **dört test
birden** patladı.

Doğru test, yanlış nesne — katalogda bir kez düşülmüş tuzağın (sur burcu
"gövde çapı 16,45 m" testine takılmıştı) bir kat derini: `OfKind` bir
**tür** süzgecidir ve bir tür birden çok yapı içerebilir. Bir yapıya özgü
sayı artık `Named(...)` ile, **adıyla** aranıyor.

## Atlanan test, üçüncü kez

Siluet testi sahnedeki `LANDMARK_1632`'yi arıyordu ve atlandı — bugün
üçüncü kez aynı hata (ADR 0041, 0043). Üstelik sahneye bakmak **yanlış
ölçüydü**: doğru soru "sahnede ne var" değil, "dünyada hangi yapının tepesi
en yüksek". O da konum + arazi + yapı yüksekliğiyle hesaplanır ve sahne
gerektirmez.

## Sonuç

- `Suleymaniye` LOD0 7 294; ayak izi 82,0 × 104,3 m, yükseklik 66,70 m.
- Yerleşim (−828, 58,1, −1034), kıble 330,4°; **tepe 124,8 m** — dünyanın
  en yüksek yapısı (Galata Kulesi 98,2 m).
- Sahnede **15 landmark**. EditMode **193/193**, atlanan yok.

## Açık kalanlar

- Külliyenin öteki yapıları (dört medrese, darüşşifa, imaret, hamam,
  **Süleyman ve Hürrem türbeleri**) — hepsi 1632'de ayakta, üretilmedi.
- Yan cephe payandaları ve galeri kemerleri kabaca geçildi; siluet ve orta
  mesafe için yeterli.
