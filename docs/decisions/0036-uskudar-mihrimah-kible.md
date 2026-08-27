# ADR 0036 — Üsküdar Mihrimah Sultan Camii; caminin yönü kıbleden türer

- **Tarih**: 2026-08-25
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/uskudar_mihrimah.md`)
- **Bağlam**: Faz 3, S-kademe. Hezarfen'in **iniş noktasının** silüeti.

## Karar 1 — Yeni kit: `sinan_kit.py`

`mosque_kit.py` mahalle mescidini kurar ve kubbesi **yoktur** (tipolojik
karar, kendi giriş notunda). Selâtin ölçeğindeki cami öteki uçtur: kubbe
yapının kendisidir. İkisini tek modüle sıkıştırmak, mescidin "kubbesizlik"
kuralını bir bayrağa indirgerdi.

`hz_blender`'a yeni bir ilkel eklendi: **`make_half_dome`**. Yön
parametrelidir, çünkü bu yapıda yarım kubbe **üç** tanedir ve dördüncüsünün
olmaması planın kendisidir.

## Karar 2 — Kubbe D2, gerisi kubbeden türer (D3)

Ölçülen: dış çap **11,40 m**, iç çap **10,00 m**, kilit **24,20 m**
(Vardar 2021). Kaynaklardaki "yaklaşık 10 m" ile "11,40 m" çelişkisi
görünürdedir — biri iç, biri dış ölçüdür.

Sayılanlar (D2): üç yarım kubbe; çift minare, her biri **tek şerefeli**;
beş kubbeli birinci revak, altı mermer sütun; çift revak; set ~2 m.

Geri kalan **uydurulmadı, türetildi**: kemer açıklığı = kubbe dış çapı;
yarım kubbe yarıçapı = ana kubbe yarıçapı (aynı kemerlerden doğarlar).
Minare için ölçü yok; yazılı kural şerefeyi ana kubbe kilidine koyar.

### Türetme hatası ve düzeltmesi

İlk yazımda yarım kubbeler kemer **kilidine** oturtulmuştu. Sonuç
ölçülebilirdi: duvar 5,70 m fazla yükseldi, yarım kubbeler ana kubbenin
yalnızca 2,36 m altına düştü ve kubbe kütlesi gövdenin üstünde bir şapka
gibi kaldı. Doğrusu geometriden çıkar — kemer yarım dairedir, açıklığı
kubbe çapıdır, **kabarması yarıçap kadardır**; yarım kubbe kemeri doldurur,
tabanı kemerin **eteğindedir**. Zincir: kilit 24,20 → kubbe eteği 19,75 →
pandantif 2,36 → kemer kilidi 17,39 → **kemer eteği / saçak 11,69 m**.
Ana kubbe artık yarım kubbelerden 8,06 m yüksek.

Aynı turda kasnak da düzeltildi: saçaktan kubbe eteğine uzanan 8,06 m'lik
tek silindir render'da bir **kule** gibi okunuyordu. Dışarıdan görünen şey
önce dört kemerin taşıdığı **kare tympanum kütlesi**, sonra pandantifin
kısa kasnağıdır.

## Karar 3 — **Caminin yönünü eğim değil KIBLE belirler**

`LandmarkPlacer` yönü arazi eğiminden türetiyordu; Galata Kulesi için doğru
(kapı yokuş aşağı, şehre bakar), cami için **yanlış**. Üsküdar Mihrimah
eğimle 322° çıkmıştı: doğruya 8,4° uzak ve daha kötüsü, yanlış gerekçeyle.
Bir caminin mihrabı arazinin eğimine bakmaz.

Kıble büyük daire formülünden hesaplandı (Kâbe 21,4225 K / 39,8262 D):
gerçek kuzeye göre **151,73°**. Oyun dünyası **UTM 35N ızgarasındadır**
(ADR 0007), gerçek kuzeyde değil; meridyen yakınsaması (λ−27°)·sin φ =
**1,32°** çıkarılınca **ızgara kıblesi 150,40°**. Bu 1,3°'yi atlamak sessiz
bir sapma olurdu.

Tek sabit yeter: kataloğun **22 landmark'ının** tamamında hesaplanan ızgara
kıblesi 150,210°–150,408°, yayılım **0,198°**. Şehir ölçeğinde kıble
sabittir — ve bu bir varsayım değil, ölçüm.

Yerleştirici artık türü **Blender kataloğundan** okur (`kind`); tür
listesini Unity tarafında elle tutmak, üreticiyle yerleştiricinin iki ayrı
gerçeği olması demekti. Testte formül yeniden kurulup sabit doğrulanıyor.

## Karar 4 — Yarım kubbenin tabanı kapatıldı

Yeni bir öz-test (`t_half_dome_watertight_and_outward`) yazıldı, çünkü
sarım hatası **iki kez** yapıldı ve ikisinde de kendi yorumum "sarım doğru"
diyordu. Test ilk koşuşunda **17 açık kenar** saydı: yarım kubbenin altı
açıktı ve tam kubbeden farklı olarak duvara gömülmüyor, dışarıdan
görünüyordu. Kapak eklendi; ilk denemede yelpaze merkezden açıldığı için
**3 kenar** açık kaldı (iki yarıçap + kirişin alt kenarı), yelpaze kirişin
ucundan açılınca kabuk kapandı. Hiçbiri render'a bakarak görülemezdi.

## Sonuç

- LOD0 5 008 üçgen, LOD1 378; `UCX_UskudarMihrimah`; LODGroup 2 kademe;
  boş/gömülü malzeme yok.
- Yerleşim (3428,7 , 3,29 , 85,4); tepe 40,84 m; **mihrap 150,40°**.
- Toplam yükseklik 36,75 m (set dahil), kubbe kilidi 26,20 m.

## Açık kalanlar

- Külliyenin öteki 1632 yapıları (medrese, sıbyan mektebi, imaret-tabhâne,
  Kurşunlu Han) **henüz yok** — hepsi 1632'de ayakta ve yapılmalı.
- **Doğancılar Meydanı** ile iskele/kayıklar ayrı bir tur.
- İkinci revak örtüsünün 1632'deki biçimi bilinmiyor (Kuban'ın 17. yy notu
  örtüye ait); ahşap sundurma olarak D3 kuruldu.
