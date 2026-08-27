# İnceleme — türbe, sıbyan mektebi, kahvehane + kurşun dokusu (Faz 2b)

**Üretim:** 2026-08-21 · **ADR:** 0021 · **Kaynak:** RESEARCH.md §4.3

| Varlık | Ne | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|---|
| `Turbe_A` | sekizgen kâgir türbe, kurşun kubbe | 7,31 × 7,76 | 8,77 | 2 802 |
| `Turbe_B` | altıgen küçük türbe, dar hazîre için | 6,89 × 6,54 | 7,87 | 2 176 |
| `Mektep_A` | sıbyan mektebi — çeşme üstünde yükseltilmiş, kubbeli | 7,45 × 8,65 | 9,00 | 1 930 |
| `Kahvehane_A` | çarşı kahvehanesi — geniş sundurma, taş seki | 8,50 × 9,05 | 6,82 | 508 |
| `Kahvehane_B` | küçük kahvehane, ocaksız | 7,00 × 7,75 | 5,05 | 424 |

## Bakılacak paketler

- `renders/review/Turbe_A_v2/contact_sheet.png`
- `renders/review/Mektep_A_v4/contact_sheet.png`
- `renders/review/Kahvehane_A_v3/contact_sheet.png`
- `unity/HezarfenGame/Captures/faz2_turbe_hazire.png` — türbe hazîrenin ucunda
- `unity/HezarfenGame/Captures/faz2_mahalle_cekirdegi.png` — mescit + hazîre + doku
- `unity/HezarfenGame/Captures/faz2_han_avlusu.png` — hanın yeni avlusu ve kurşun damı
- `unity/HezarfenGame/Captures/faz2_cinar_damlar.png` — kahvehanenin çınarı, damların üstünden

## Üçü de mahallenin neden orada olduğunu anlatıyor

**Türbe** mahalleye adını veren yapıdır: mahalle bir vakıf etrafında kurulur,
vakfı kuran kişi kendi mescidinin hazîresine gömülür ve mahalle onun adıyla
anılır. Bu yüzden türbe mezar taşı değil **yapı**dır — sekizgen kâgir gövde,
kurşun kubbe, her yüzde şebekeli pencere. Hazîrenin **ucunda** durur ve
sınırını oluşturur; kapısı mezarlığa bakar.

**Sıbyan mektebi** tek odadır ve **yükseltilir**: altı çeşme, üstü ders odası,
çıkışı dışarıdan taş merdiven. Merdiveni silinirse yapı küçük bir mescide
döner — mektebi mektep yapan ikinci işaret odur.

**Kahvehane** anıt değildir: ahşap cepheli bir oda, sokağa geniş bir sundurma,
önünde taş **seki**. Oturulan yer sokaktır. Gölgeyi çınar tamamlar.

> **Zaman işareti.** Kahvehane 1632'de açıktır ve **2 Eylül 1633** fermanıyla
> yasaklanıp yıktırılmıştır (BOA A.DVN nr. 25/47). Oyuna zaman katmanı
> eklendiğinde sahneden kaldırılacak **ilk varlık** budur — bir yıl sonra aynı
> yerde durmayacak tek yapı.

## Kurşun artık dokulu

Kubbe kurşunla örtülür; şehre yukarıdan bakınca görülen yüzeylerin çoğu odur.
Poly Haven'da kurşun örtü yok, o yüzden **prosedürel** üretildi (bizim
eserimiz): levha dikişleri, oksit pusu, yıkanmış sırtlar.

Kubbede doku **meridyen dilimleri** hâlinde gider ve tepeye doğru daralır —
gerçek kurşun dilimi gibi. İlk denemede "kırılmış fayans" çıkmıştı; sebep
dokunun kendisi değil UV izdüşümüydü (ADR 0021 §3).

## Yerleşim (ölçüldü, Galata)

| | mescide | en yakın eve | ayak izi altında kot farkı |
|---|---|---|---|
| Türbe | 21,3 m | 6,8 m | 0,29 m |
| Mektep | 30,7 m | 9,3 m | 0,21 m |
| Kahvehane | 27,1 m | 14,6 m | 1,45 m |

**Balat'ta türbe ve mektep yok** — ikisi de müslüman vakıf kurumudur; oradaki
karşılık cemaatin kendi okuludur (Talmud Tora) ve ayrı bir tiptir, üretilmedi.
Kahvehane her iki mahallede de var.

## Bildiğim eksikler

- **Sokak seviyesinde ışık yok**: gölgedeki duvar ~30/255, kaldırım 3/255.
  Yaya seviyesinden inceleme paketi üretilemiyor — bu tur denedim, kare
  neredeyse tamamen siyah çıktı. Aydınlatma fazının ilk işi.
- İç mekân yok: türbenin sandukası, kahvehanenin ocak içi, hamamın göbek taşı.
- Kahvehanede insan yok — seki boş duruyor (kalabalık Faz 6).
- Ağaç tacı hâlâ katı kabuk; silüet kenarı sert.
- Mezar taşlarında ve kitabelerde yazı yok.

## Sormak istediğim tek şey

**Karar 6 — kahvehanenin sundurması.** Şu an aşı boyalı ahşap (`timber`), yani
evlerin cumbasıyla aynı renk. Alternatif: boyasız/gri ahşap — kahvehane bir ev
değil, ticarî bir yapı ve boya vakıf işiyse orada olmayabilir.

- **A)** Aşı boyalı kalsın — sokakta tek dil, kahvehane mahalleye ait görünür.
- **B)** Boyasız gri ahşap — ticarî yapı konut dokusundan ayrılır. *(önerim: A;
  çünkü kahvehane mahallenin uzantısıdır, çarşının değil)*

## Onay

```
OK v1        (ya da: düzeltme istekleri)
Karar 6:     A / B
```

> **Karar 6 = A** (Caner, 2026-08-21: *"senin önerine uyalım"*). Sundurma aşı
> boyalı ahşap kalıyor — kahvehane çarşının değil mahallenin uzantısıdır ve
> sokakta tek ahşap dili korunur. Geometride değişiklik gerekmedi.
