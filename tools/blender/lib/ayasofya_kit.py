"""
Ayasofya — 532-537 / 562, 1453'ten beri cami. Faz 3, A-kademe.

## Bu kit neden ayrı

`sinan_kit` bir **Osmanlı** camisi kurar ve içindeki kubbe zinciri (ADR
0036) Osmanlı kubbesinin oranlarını varsayar: `DOME_RISE_RATIO = 0.78`,
yani kubbe basıktır ve kemer yarım dairedir. Ayasofya o ailenin üyesi
değil — **Bizans** yapısıdır ve kubbesi 562'de İsidoros'un yeğeni
tarafından **yükseltilerek** yeniden yapılmıştır. Ölçülen oran
15,00 / 16,50 = **0,909**; Osmanlı oranını buraya uygulamak kubbeyi
2,2 m alçaltır ve yapının en tanınan özelliğini siler.

Bu yüzden `DOME_RISE_RATIO` bu dosyada **kullanılmaz** ve
`AyasofyaParams.validate` onu kullanmayı reddeder.

## 1632'de ne var, ne yok

Faz 3'ün alışkanlığı "bildiğin hâli 1632 değildir" oldu. Süleymaniye'de
(ADR 0044) cevap "aynı" çıkmıştı. Ayasofya **ikisinin arası**:

**1632'de VAR** — dört minarenin dördü de (batıdaki ikizler III. Murad'ın
saltanatının ilk yıllarında tamamlandı), Sinan'ın payandaları, minber ve
mahfil (III. Murad), üç imparator türbesi (II. Selim 1577, III. Murad
1599, III. Mehmed 1608).

**1632'de YOK**:

* **I. Mustafa ve İbrahim türbesi (1639)** — o tarihte **vaftizhâne**
  hâlâ *yağhânedir*. Yapı ayakta ama işlevi başka; türbe değil.
* III. Ahmed'in hünkâr mahfili (1728),
* I. Mahmud'un kütüphanesi, **şadırvanı**, sıbyan mektebi, imareti
  (1739-40) — bugün avluda görülen şadırvan bunların içinde,
* Fossati onarımı (1847-49) ve onun **dışa vurduğu sıva + kırmızı yatay
  şeritler**; bugünkü tek ton okra ondan da sonradır,
* Kazasker Mustafa İzzet'in büyük hat levhaları (Fossati dönemi).

Ve bir tane de **kaldırılmış** ek: Fatih'in yarım kubbe üzerindeki
**ahşap** minaresi **1574'te sökülmüştür**. 1632'de kubbenin üstünde
minare yoktur.

## Dört minare birbirinin aynı DEĞİLDİR — ve bunu ölçtük

Kaynaklar hangi köşenin tuğla olduğunda **çelişir**: TDV güneybatı der,
iki popüler kaynak güneydoğu ve kuzeydoğu der. Kaynak seçmek yerine
plana bakıldı (OpenStreetMap izleri, ODbL — yalnızca **iki sayı**
alındı, veri depoya girmedi):

| | gövde çapı |
|---|---|
| doğu çifti | **3,6 m** |
| batı çifti | **4,0 m** |

Batıdaki ikisi birbirinin **aynı**; doğudaki ikisi ince ve birbirinden
farklı. Tuğla minare tek örnektir, yani **ikiz olamaz** — ölçü, TDV'nin
güneybatı iddiasını eler ve tuğlayı doğu köşesine bırakır. Hangi doğu
köşesi olduğu hâlâ **D3**'tür; model bunu tek başına taşımaz, gövde
kalınlıkları taşır.

Konumlar da simetrik değil (kuzey çifti eksenden 39,5 m, güney çifti
33,1 m): minareler farklı yüzyıllarda, var olan payandalara dayanarak
eklendi. Simetri yapmak burada bir **düzeltme değil, bozma** olurdu.

## Ölçüler

| | | |
|---|---|---|
| Kubbe iç açıklık | **31,87** (K-G) × **30,86** m (D-B) | ölçülü (D2) |
| Kubbe dış kütle | **33,0 m** | plandan; kaynak 32,7-33,5 |
| Kubbe kilidi | **55,60 m** (döşemeden) | ölçülü (D2) |
| Kaburga / pencere | **40** / **40** | sayılan |
| Yapı, uçtan uca | **106,3 × 75,4 m** | plandan |
| Yapı, ana kütle | **78 × 66 m** (+ payanda 4,7) | plandan |
| Minare | **dört**, ~60 m | boy D3 |

Yayımlanan **82 × 73 m** yalnızca ana kütleyi anlatır: dış narteksi ve
apsisi saymaz. İlk kurulum onu uçtan uca sanmıştı ve minareler yapının
12 m ötesinde, boşlukta duruyordu. Tek sayı, yapının **neresini**
kastettiğini söylemiyordu — kubbe çapındaki iç/dış ikiliğinin plandaki
eşi, ve bu turda ikinci kez aynı tuzak.

İç 31,87 ile dış 33,0 arasındaki ~1 m, kubbe kabuğunun kalınlığıdır.
Bu **dördüncü** kez karşılaşılan iç/dış ikiliği (Üsküdar Mihrimah §5.4,
Yeni Cami §5.9, Süleymaniye §5.10) — ve **ilk kez ikisi birden
ölçüldü**, yani kaynağın hangisini kastettiğini tahmin etmek gerekmedi.

## Yapı kıbleye DÖNÜK DEĞİLDİR

Ayasofya bir kilisedir: ekseni apsise, yani doğuya bakar. Ölçülen eksen
azimutu **123,5°**; yapı kıbleye dönük değildir ve mihrap apsise **eğik**
oturtulmuştur — içeri girildiğinde ilk fark edilen şey budur.

Sapmanın **büyüklüğü** neye göre ölçüldüğüne bağlı ve bu ilginç:

* bugünün büyük daire kıblesine göre (150,40°) → **26,9°**,
* **1632'nin ölçülen kıblesine** göre (133,7°, ADR 0046) → **10,2°**.

Yani Bizans'ın doğu ekseni, 1632'nin kıblesine bugünkü kıbleden çok daha
yakındır. Katalog ikincisini yazar, çünkü yerleştiriciyi o ilgilendirir.

Yerleştirici camileri kıbleye döndürür (`MosqueKinds`); bu yapı için
`face_deg` bildirilir ve bildirilen yön kıbleyi **yener**
(`LandmarkPlacer`, ADR 0040'ta Bâbüsselâm için açılan kapı). Üçüncü
kullanımı ve ilk kez bir **caminin** kıbleden muaf tutulması.

**Kaynaklar**: TDV İslâm Ansiklopedisi "Ayasofya"; Vikipedi (yapı
ölçüleri); ayasofyacamii.gov.tr; kubbe iç ölçüleri ve 40 kaburga/pencere
için Türkçe yapı tarifleri. Eksen azimutu ve minare gövde çapları
OpenStreetMap izlerinden **türetildi** (ODbL — veri kopyalanmadı,
iki ölçü okundu). RESEARCH.md §5.11, ADR 0045.
"""

import math

import detay_kit as dk
import hz_blender as hz
import ottoman_kit as kit


# ---------------------------------------------------------------- ölçüler

#: Kubbenin **iç** açıklığı (m). İki eksen farklıdır ve bu **tasarım
#: değil deformasyondur**: kubbe 558'de çöktü, 562'de yeniden kuruldu ve
#: yüzyıllar boyunca yayıldı. Elips diye modellemek, kaynağın söylemediği
#: bir tasarım niyeti uydurmak olurdu; fark 1,01 m, yani 1,70 m'lik ölçü
#: figürünün altında ve eğri yüzeyde okunmaz.
AYA_DOME_D_IN_NS = 31.87
AYA_DOME_D_IN_EW = 30.86

#: Kubbenin **dış** kütlesi (m). Plandan okundu (32,9 × 33,0) ve
#: yayımlanan 32,7-33,5 aralığıyla doğrulandı. Mesh dış yüzeydir, bu
#: yüzden kütleyi bağlayan sayı budur.
AYA_DOME_D_OUT = 33.00

#: Kubbe kilidi, harim döşemesinden (m) — ölçülü.
AYA_CROWN_Z = 55.60

#: Kubbenin kaidesinden kilidine yükselişi (m). **Osmanlı oranı DEĞİL**:
#: 15,00 / 16,50 = 0,909. `sinan_kit.DOME_RISE_RATIO` (0,78) buraya
#: uygulanırsa kilit 42,4 m'ye düşer.
AYA_DOME_RISE = 15.00

#: Kubbe eteğindeki **kırk** kaburga ve aralarındaki **kırk** pencere —
#: sayılan değer, geometriyi bağlar. Kubbe bu yüzden 40 dilimli üretilir:
#: pencereler kaburgaların *arasına* düşsün diye.
AYA_DOME_RIBS = 40

#: Yapı: eksen boyunca uzunluk × eksene dik genişlik (m).
#:
#: **İlk kurulumda 82 × 73 yazılmıştı** (Vikipedi) ve render'da minareler
#: yapıdan 12 m ötede, boşlukta duruyordu. Plan ölçülünce anlaşıldı:
#: 82 m yalnızca **ana kütledir**; dış narteksi ve apsisi saymaz. Uçtan
#: uca **106,3 m**. Yayımlanan tek sayı, yapının neresini kastettiğini
#: söylemiyordu — kubbe çapındaki iç/dış ikiliğinin plandaki eşi.
AYA_LEN, AYA_WID = 106.3, 75.4

#: Kütle basamakları — hepsi plandan **ölçüldü** (metre).
#:
#: Yan nef kabuğu payandasızdır; payandalar dışa **4,7 m** taşar ve
#: toplam 75,4'ü onlar tamamlar. Önceki kurulumda payandalar 73 m'lik
#: bir bloğun dışına konmuştu ve yapıyı 81 m'ye genişletiyordu.
AYA_AISLE_W, AYA_AISLE_L, AYA_AISLE_H = 66.0, 78.0, 25.0
AYA_NAVE_W, AYA_NAVE_L, AYA_NAVE_H = 45.0, 77.4, 31.0
AYA_ARCH_W, AYA_ARCH_L, AYA_ARCH_H = 45.0, 39.7, 41.0
AYA_BUTTRESS_JUT = 4.7

#: Yarım kubbe merkezleri, kubbe merkezinden (m). Plandan: ±18,6.
#: (Önceki değer ±15,5 idi ve o aslında **enine kemer duvarlarının**
#: yeriydi — plan ikisini ayrı ayrı gösteriyor.)
AYA_SEMI_U = 18.6

#: Apsis: çap, eksen üzerindeki merkezi, yüksekliği (m) — plandan.
AYA_APSE_D, AYA_APSE_U, AYA_APSE_H = 13.8, 37.0, 31.0

#: Narteks ve dış narteks: derinlik, yükseklik, genişlik (m) — plandan.
#: Narteks yan neflerle **aynı** kottadır (25 m); ilk kurulumda 17,5 m
#: yazılmıştı ve batı cephesi bir sundurma gibi okunuyordu.
AYA_NARTHEX_D, AYA_NARTHEX_H = 11.7, 25.0
AYA_EXO_D, AYA_EXO_H, AYA_EXO_W = 8.2, 9.0, 65.0

#: **Batı baskı kulecikleri**: 3,3 × 3,3 m, 34 m, eksenin ±10,2 m yanında.
#: Bunlar boş bir ayrıntı değil — TDV'ye göre **Fatih'in ahşap minaresi**
#: bu iki kulecikten *güneydekinin üstünde* duruyordu ve **1574'te
#: söküldü**. 1632'de kulecik var, minare yok; modelin söylediği tam
#: olarak budur.
AYA_TURRET_S, AYA_TURRET_H, AYA_TURRET_U, AYA_TURRET_V = 3.3, 34.0, -35.7, 10.2

#: Eksen azimutu (derece, ızgara kuzeyinden): apsis **123,5°**'ye bakar.
#: Girişin baktığı yön bunun tersidir ve kataloğa `face_deg` olarak yazılır.
AYA_AXIS_DEG = 123.5

#: **1632'nin** kıblesi (ızgara), ADR 0046 — on tarihî camiden ölçüldü.
#: Bugünün büyük daire kıblesi 150,40°'dir ve Ayasofya ona göre 26,9°
#: sapardı; ama yapıyı yerleştiren sayı o değil. Ölçülen Osmanlı kıblesine
#: göre sapma yalnızca **10,2°** — yani Bizans'ın doğu ekseni, 1632'nin
#: kıblesine bugünkü kıbleden **daha yakın**.
QIBLA_1632_DEG = 133.7

#: Minareler: (x, y, gövde yarıçapı, malzeme). Konumlar **ölçüldü** ve
#: simetrik değildir — dördü farklı yüzyıllarda, var olan payandalara
#: dayanarak eklendi. +Y apsis (doğu), +X güney yakası.
#:
#: Doğu çifti ince (Ø3,6) ve birbirinden farklı: biri **tuğla**dır.
#: Batı çifti kalın (Ø4,0) ve **ikiz**dir — Sinan, II. Selim'in siparişi,
#: III. Murad'ın ilk yıllarında tamam.
AYA_MINARETS = (
    (-39.9, +52.0, 1.80, "cutstone"),   # kuzeydogu — ince, tas
    (+32.8, +43.2, 1.80, "brick"),      # guneydogu — ince, TUGLA
    (-39.2, -54.3, 2.00, "cutstone"),   # kuzeybati — Sinan ikizi
    (+33.4, -53.5, 2.00, "cutstone"),   # guneybati — Sinan ikizi
)

#: Minare boyu (m). Yaygın kaynak "dördü de 60 m" der; plandan ölçülen
#: gövde çapları dördünün **aynı olmadığını** gösterdiği için eşit boy
#: iddiası da şüphelidir. Ölçülmüş bir boy bulunamadı — **D3**.
AYA_MINARET_H = 60.0


class AyasofyaParams(object):
    """
    Ayasofya, 1632 hali. Ayrıntı ve gerekçe modül başlığında.

    Kubbe zinciri (hepsi kilitten türer, hiçbiri elle girilmez)::

        kilit            55,60          ölçülü
        kubbe kaidesi    40,60          = kilit − yükseliş
        kemer uzengisi   24,10          = kaide − dış yarıçap
        yarım kubbe kil. 39,10          = uzengi + yükseliş

    **Zincir bağımsız olarak doğrulandı.** Yalnızca ölçülen kilitten ve
    çaptan türetilen bu iki kot, plandan okunan kütle basamaklarıyla
    karşılaştırıldı:

    ==================  ========  ========  =====
    \\                   türetilen  plandan   fark
    ==================  ========  ========  =====
    kubbe kaidesi          40,60     41,0    0,40
    kemer uzengisi /
    yan nef çatısı         24,10     25,0    0,90
    ==================  ========  ========  =====

    İki bağımsız yol bir metrenin altında buluşuyor. Zincir (ADR 0036)
    Osmanlı camilerinde kurulmuştu; Bizans oranıyla beslendiğinde de
    tutması, onun bir **üslup kuralı değil geometri** olduğunu gösteriyor.
    """

    def __init__(self, dome_d=AYA_DOME_D_OUT, crown_z=AYA_CROWN_Z,
                 dome_rise=AYA_DOME_RISE, length=AYA_LEN, width=AYA_WID,
                 ribs=AYA_DOME_RIBS, minarets=AYA_MINARETS,
                 minaret_h=AYA_MINARET_H, palette="default"):
        self.dome_d, self.crown_z = dome_d, crown_z
        self.dome_rise = dome_rise
        self.length, self.width = length, width
        self.ribs = ribs
        self.minarets = tuple(minarets)
        self.minaret_h = minaret_h
        self.palette = palette

    @property
    def r(self):
        return self.dome_d * 0.5

    @property
    def rise_ratio(self):
        """Ölçülen basıklık oranı — Osmanlı 0,78'i **değil**."""
        return self.dome_rise / self.r

    @property
    def dome_base_z(self):
        """Kubbenin kaidesi = kırk pencerenin alt kotu = dış korniş."""
        return self.crown_z - self.dome_rise

    @property
    def arch_spring_z(self):
        """Dört büyük kemerin uzengi kotu; yan nef çatısı da buradadır."""
        return self.dome_base_z - self.r

    @property
    def semi_crown_z(self):
        return self.arch_spring_z + self.dome_rise

    @property
    def face_deg(self):
        """Girişin (batı cephesi) baktığı yön — apsisin tersi."""
        return (AYA_AXIS_DEG + 180.0) % 360.0

    def validate(self):
        if abs(self.dome_d - AYA_DOME_D_OUT) > 0.01:
            raise ValueError(f"kubbe dis capi {self.dome_d} — olculen "
                             f"{AYA_DOME_D_OUT} m")
        if abs(self.crown_z - AYA_CROWN_Z) > 0.01:
            raise ValueError(f"kilit {self.crown_z} — olculen "
                             f"{AYA_CROWN_Z} m")
        # Osmanli orani buraya UYGULANAMAZ: Ayasofya'nin kubbesi 562'de
        # YUKSELTILEREK yeniden kuruldu. 0,78 kilidi 42,4 m'ye dusurur.
        if abs(self.rise_ratio - 0.78) < 0.02:
            raise ValueError(
                f"basiklik orani {self.rise_ratio:.3f} — bu OSMANLI orani; "
                "Ayasofya'nin olculen orani 0,909 (15,00 / 16,50)")
        if self.ribs != AYA_DOME_RIBS:
            raise ValueError(f"kaburga {self.ribs} — SAYILAN deger "
                             f"{AYA_DOME_RIBS}")
        if len(self.minarets) != 4:
            raise ValueError("Ayasofya'da DORT minare var")
        kalin = sorted(set(round(m[2], 2) for m in self.minarets))
        if len(kalin) != 2:
            raise ValueError(f"minare govde yaricaplari {kalin} — dogu cifti "
                             "ince, bati cifti kalin olmali (olculu)")
        if sum(1 for m in self.minarets if m[3] == "brick") != 1:
            raise ValueError("TAM BIR minare tugladir; otekiler tastir")
        return self


# ------------------------------------------------------------ parçalar

def _window_slots(parts, mats, col, name, n, radius, z0, h, w, phase=0.0):
    """Halka boyunca `n` karanlık yarık — kubbe eteği ve kasnak pencereleri."""
    for i in range(n):
        a = 2.0 * math.pi * (i + 0.5) / n + phase
        parts.append(hz.assign(
            hz.make_box(f"{name}_{i}", (w, w, h),
                        (radius * math.cos(a), radius * math.sin(a),
                         z0 + h * 0.5), col), mats["shadow"]))


def _minare(p, mats, col, name, x, y, r, mat_key, base_z, top_z):
    """
    Ayasofya minaresi — **tek şerefeli**, kaidesi payandaya dayalı.

    Gövde malzemesi çağırandan gelir: biri tuğladır ve kırmızıdır. Aynı
    fonksiyona `cutstone` verip tuğlayı yorumla anlatmak, farkı görünmez
    kılardı; fark **renktedir**.
    """
    out = []
    kaide_h, pabuc_h = 9.0, 4.5
    out.append(hz.assign(hz.make_box(f"{name}_Kaide", (r * 3.0, r * 3.0,
                                                       kaide_h),
                                     (x, y, base_z + kaide_h * 0.5), col),
                         mats["cutstone"]))
    out.append(hz.assign(hz.make_tube(f"{name}_Pabuc", r * 1.50, r * 1.10,
                                      pabuc_h, (x, y), base_z + kaide_h,
                                      segments=8, col=col), mats["cutstone"]))

    z = base_z + kaide_h + pabuc_h
    serefe_z = base_z + top_z * 0.70
    shaft_h = serefe_z - z
    if shaft_h < 10.0:
        raise ValueError(f"minare govdesi {shaft_h:.1f} m — oran bozuk")
    out.append(hz.assign(hz.make_tube(f"{name}_Govde", r, r * 0.90, shaft_h,
                                      (x, y), z, segments=16, col=col),
                         mats[mat_key]))

    # SEREFE: Ayasofya minarelerinde BIRERDIR. Olculu kaynak bulunamadi;
    # tipolojik ve fotograftan okunan deger — **D3**.
    out.append(hz.assign(hz.make_tube(f"{name}_SerefeTabla", r * 1.70,
                                      r * 1.70, 0.32, (x, y), serefe_z,
                                      segments=16, col=col), mats["cutstone"]))
    out.append(hz.assign(hz.make_tube(f"{name}_Korkuluk", r * 1.64, r * 1.56,
                                      1.10, (x, y), serefe_z + 0.32,
                                      segments=16, cap_top=False,
                                      cap_bottom=False, col=col),
                         mats["marble"]))

    petek_z = serefe_z + 1.42
    petek_h = base_z + top_z - petek_z
    out.append(hz.assign(hz.make_tube(f"{name}_Petek", r * 0.84, r * 0.78,
                                      petek_h, (x, y), petek_z, segments=14,
                                      col=col), mats[mat_key]))
    kulah_h = 6.5
    out.append(hz.assign(hz.make_tube(f"{name}_Kulah", r * 0.92, 0.0, kulah_h,
                                      (x, y), base_z + top_z, segments=14,
                                      col=col), mats["lead"]))
    out.append(hz.assign(hz.make_tube(f"{name}_Alem", 0.10, 0.02, 1.4,
                                      (x, y), base_z + top_z + kulah_h,
                                      segments=6, col=col), mats["lead"]))
    return out


def _payanda(parts, mats, col, name, x, y, w, d, h, step_h):
    """
    Payanda — Bizans'ın ve Sinan'ın (II. Selim dönemi) takviyeleri.

    Basamaklı: alt kütle geniş, üstü dar. Tek kutu yapılınca render'da
    "duvara yapışmış kule" gibi okunuyordu; payanda yukarı doğru
    **incelir** ve okunmasını sağlayan şey o basamaktır.
    """
    parts.append(hz.assign(hz.make_box(f"{name}_Alt", (w, d, h),
                                       (x, y, h * 0.5), col), mats["stone"]))
    parts.append(hz.assign(hz.make_box(f"{name}_Ust", (w * 0.62, d * 0.70,
                                                       step_h),
                                       (x, y, h + step_h * 0.5), col),
                           mats["stone"]))
    parts.append(hz.assign(hz.make_box(f"{name}_Sapka", (w * 0.68, d * 0.76,
                                                         0.5),
                                       (x, y, h + step_h + 0.25), col),
                           mats["lead"]))


# ------------------------------------------------------------------ kütle

def build_ayasofya(p, col, asset_name, textured=False):
    """Ayasofya, 1632 hali. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    L, W, r = p.length, p.width, p.r

    # --- Stilobat --------------------------------------------------------
    #
    # Yapinin ekseni SIMETRIK DEGIL: batida iki narteks, doguda apsis var
    # ve kubbe merkezi ortada degil. Stilobati y=0'a ortalamak, bati ucunu
    # bosluga birakip dogu ucunda 12 m sarkitiyordu — kutlenin GERCEK
    # sinirlarindan olculur.
    pod_h = 1.10
    y_west = -(AYA_AISLE_L * 0.5 + 0.5) - AYA_NARTHEX_D - AYA_EXO_D
    y_east = AYA_APSE_U + AYA_APSE_D * 0.5
    parts.append(hz.assign(hz.make_box(f"Stilobat_{asset_name}",
                                       (W + 3.0, (y_east - y_west) + 4.0,
                                        pod_h),
                                       (0.0, (y_east + y_west) * 0.5,
                                        pod_h * 0.5), col),
                           mats["cutstone"]))
    z0 = pod_h

    # Ana kutlenin eksen uzerindeki merkezi. Yapinin AGIRLIK merkezi
    # kubbenin merkezi DEGILDIR: bati ucunda iki narteks, dogu ucunda
    # apsis var. Kubbe merkezi y=0'da tutulur ve oteki kutleler ondan
    # olculur — plandaki butun konumlar oyle okunmustu.
    aisle_h, aisle_w, aisle_l = AYA_AISLE_H, AYA_AISLE_W, AYA_AISLE_L
    aisle_y = -0.5                      # plandan: u -39,5 .. +38,5

    # --- Yan nefler + galeriler ------------------------------------------
    #
    # Cati kotu plandan 25,0 m; zincirin turettigi kemer uzengisi 24,10 m.
    # Iki bagimsiz yol bir metrenin altinda bulusuyor — turetilen degil
    # OLCULEN kullanilir ("olculen turetileni yener").
    # DORT CEPHE, GERCEK KEMERLI PENCERELERLE.
    #
    # Onceki hali tek bir kutu ve ona yapistirilmis koyu dikdortgenlerdi.
    # Ayasofya'nin yan cephesi IKI KATLIDIR (nef + galeri) ve o iki sirayi
    # gercek acikliklarla kurmak, kutleyi depo olmaktan cikaran sey.
    kat = aisle_h / 2.0
    kabuk, _ = dk.kabuk(mats, col, f"Nefler_{asset_name}", aisle_w, aisle_l,
                        1.8, z0, ((13, 1.8, kat), (13, 1.6, kat)),
                        cy=aisle_y)
    parts += kabuk
    for o in dk.silme_at(f"NefSacak_{asset_name}", 0.0, aisle_y,
                         aisle_w, aisle_l, z0 + aisle_h - 0.9, col,
                         steps=3, h=0.9, out=0.5):
        parts.append(hz.assign(o, mats["cutstone"]))
    parts.append(hz.assign(hz.make_box(f"NefOrtu_{asset_name}",
                                       (aisle_w + 1.2, aisle_l + 1.2, 0.6),
                                       (0.0, aisle_y, z0 + aisle_h + 0.3), col),
                           mats["lead"]))

    # --- Klerestori (nef) bloku: 45,0 x 77,4 x 31,0 — plandan ------------
    nave_y = -0.5
    parts.append(hz.assign(hz.make_box(f"Nef_{asset_name}",
                                       (AYA_NAVE_W, AYA_NAVE_L, AYA_NAVE_H),
                                       (0.0, nave_y, z0 + AYA_NAVE_H * 0.5),
                                       col), mats["stone"]))
    parts.append(hz.assign(hz.make_box(f"NefUstOrtu_{asset_name}",
                                       (AYA_NAVE_W + 1.0, AYA_NAVE_L + 1.0,
                                        0.5),
                                       (0.0, nave_y, z0 + AYA_NAVE_H + 0.25),
                                       col), mats["lead"]))

    # --- Narteks + dis narteks (bati) ------------------------------------
    #
    # Narteks yan neflerle AYNI kotta (25 m). Ilk kurulumda 17,5 m
    # yazilmisti ve bati cephesi bir sundurma gibi okunuyordu — narteksin
    # ustunde GALERI vardir, yani iki katlidir.
    nar_y = aisle_y - aisle_l * 0.5 - AYA_NARTHEX_D * 0.5
    parts.append(hz.assign(hz.make_box(f"Narteks_{asset_name}",
                                       (aisle_w + 8.0, AYA_NARTHEX_D,
                                        AYA_NARTHEX_H),
                                       (0.0, nar_y,
                                        z0 + AYA_NARTHEX_H * 0.5), col),
                           mats["stone"]))
    parts.append(hz.assign(hz.make_box(f"NarteksOrtu_{asset_name}",
                                       (aisle_w + 9.0, AYA_NARTHEX_D + 1.0,
                                        0.5),
                                       (0.0, nar_y,
                                        z0 + AYA_NARTHEX_H + 0.25), col),
                           mats["lead"]))
    exo_y = nar_y - AYA_NARTHEX_D * 0.5 - AYA_EXO_D * 0.5
    parts.append(hz.assign(hz.make_box(f"DisNarteks_{asset_name}",
                                       (AYA_EXO_W, AYA_EXO_D, AYA_EXO_H),
                                       (0.0, exo_y, z0 + AYA_EXO_H * 0.5),
                                       col), mats["stone"]))
    parts.append(hz.assign(hz.make_box(f"DisNarteksOrtu_{asset_name}",
                                       (AYA_EXO_W + 1.0, AYA_EXO_D + 1.0, 0.5),
                                       (0.0, exo_y, z0 + AYA_EXO_H + 0.25),
                                       col), mats["lead"]))
    # Dis narteksin bati yuzundeki payeler: plandan 12 m, ALTI adet.
    for i in range(6):
        u = -AYA_EXO_W * 0.5 + AYA_EXO_W * (i + 0.5) / 6.0
        parts.append(hz.assign(
            hz.make_box(f"DisNarteksPaye_{i}", (2.6, AYA_EXO_D + 1.4, 12.0),
                        (u, exo_y, z0 + 6.0), col), mats["stone"]))
    for i in range(5):
        u = -AYA_EXO_W * 0.42 + AYA_EXO_W * 0.84 * i / 4.0
        parts.append(hz.assign(
            hz.make_box(f"Kapi_{i}", (2.4, 0.7, 6.0),
                        (u, exo_y - AYA_EXO_D * 0.5 - 0.35, z0 + 3.0), col),
            mats["shadow"]))

    # --- BATI BASKI KULECIKLERI ------------------------------------------
    #
    # Plandan: 3,3 x 3,3 m, 34 m, eksenin +-10,2 m yaninda, u = -35,7.
    # FATIH'IN AHSAP MINARESI bunlardan GUNEYDEKININ ustundeydi ve 1574'te
    # SOKULDU. Kulecikler duruyor, minare durmuyor — modelin 1632 hakkinda
    # soyledigi en ince sey bu ve yalnizca BOSLUKLA anlatilabiliyor.
    for sg in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"BaskiKulecik_{sg}",
                        (AYA_TURRET_S, AYA_TURRET_S, AYA_TURRET_H),
                        (sg * AYA_TURRET_V, AYA_TURRET_U,
                         z0 + AYA_TURRET_H * 0.5), col), mats["stone"]))
        parts.append(hz.assign(
            hz.make_tube(f"BaskiKulecikKulah_{sg}", AYA_TURRET_S * 0.78, 0.0,
                         2.6, (sg * AYA_TURRET_V, AYA_TURRET_U),
                         z0 + AYA_TURRET_H, segments=8, col=col),
            mats["lead"]))

    # --- Apsis (dogu) -----------------------------------------------------
    apse_r = AYA_APSE_D * 0.5
    apse_y = AYA_APSE_U
    parts.append(hz.assign(hz.make_tube(f"Apsis_{asset_name}", apse_r, apse_r,
                                        AYA_APSE_H, (0.0, apse_y), z0,
                                        segments=14, col=col), mats["stone"]))
    parts.append(hz.assign(
        hz.make_half_dome(f"ApsisYarimKubbe_{asset_name}", apse_r,
                          apse_r * p.rise_ratio, (0.0, apse_y),
                          z0 + AYA_APSE_H,
                          facing=math.pi * 0.5, segments=14, rings=5, col=col),
        mats["lead"]))
    for i in range(3):
        parts.append(hz.assign(
            hz.make_box(f"ApsisPencere_{i}", (2.0, 0.7, 5.0),
                        ((i - 1) * 3.4, apse_y + apse_r - 0.3,
                         z0 + AYA_APSE_H * 0.55), col), mats["shadow"]))

    # --- IKI YARIM KUBBE: ANA EKSENDE (dogu-bati) -------------------------
    #
    # Suleymaniye'nin de kullandigi sema BURADAN gelir (ADR 0044) — ama
    # orada kubbe 26,5 m, burada 33,0 m. Yarim kubbeler ana kubbeyle AYNI
    # yaricaptadir; Osmanli semasinda kucultulur, Bizans'ta kucultulmez.
    #
    # Merkezler plandan +-18,6 m. Onceki deger +-15,5 idi ve o aslinda
    # ENINE KEMER DUVARLARININ yeriydi; plan ikisini ayri ayri gosteriyor.
    halfs = ((0.0, +AYA_SEMI_U, math.pi * 0.5),
             (0.0, -AYA_SEMI_U, -math.pi * 0.5))
    for i, (cx, cy, facing) in enumerate(halfs):
        parts.append(hz.assign(
            hz.make_half_dome(f"YarimKubbe_{i}", r, p.dome_rise, (cx, cy),
                              z0 + p.arch_spring_z, facing=facing,
                              segments=24, rings=7, col=col), mats["lead"]))
        for o in dk.kubbe_kaburga(f"YarimKaburga_{i}", cx, cy, r,
                                  z0 + p.arch_spring_z, p.dome_rise, col,
                                  n=20, a0=facing - math.pi * 0.5,
                                  a1=facing + math.pi * 0.5):
            parts.append(hz.assign(o, mats["lead"]))
        # EKSEDRALAR BU MESH'TE **YOK** — ve bu bir eksiklik degil, bir
        # duzeltme.
        #
        # Her yarim kubbenin iki yaninda birer yarim kubbecik vardir; sayi
        # DORT ve plani tanimlar. Iki kez modele kondular ve iki kez
        # GORUNMEDILER: uzengileri neresi olursa olsun kilit kotlari 30 m
        # civarinda kaliyor, cevrelerindeki klerestori bloku ise **31 m**.
        # Yani gomulu kaliyorlar — cunku gercekte de oyleler: eksedralar
        # Ayasofya'nin IC mekan ogesidir, disaridan gorunmezler.
        #
        # Bu, Suleymaniye dersinin TERSI (ADR 0044): orada avluyu atlamak
        # yapinin yarisini silmisti. Burada tersi tuzak var — sayilan bir
        # degeri "geometriye baglamis olmak icin" gorunmez yere gomulu bir
        # kutle eklemek. Kendi denetimimi gecmek disinda bir isi olmayan
        # geometri, katalogda yasayip meshte yasamayan sayinin AYNASIDIR.
        # Katalog eksedralari `exedrae_interior` diye kaydeder ve uretici
        # onlari mesh'te ARAMAZ.

    # --- Buyuk kemer katı (tympanumlar + pandantif bolgesi) --------------
    #
    # Plandan 45,0 x 39,7 x 41,0. Zincirin turettigi kubbe kaidesi 40,60 —
    # 0,40 m fark.
    tymp_h = p.dome_base_z - p.arch_spring_z
    tymp_w, tymp_l = AYA_ARCH_W, AYA_ARCH_L
    parts.append(hz.assign(hz.make_box(f"KemerKati_{asset_name}",
                                       (tymp_w, tymp_l, tymp_h),
                                       (0.0, 0.0,
                                        z0 + p.arch_spring_z + tymp_h * 0.5),
                                       col), mats["stone"]))
    # Kuzey ve guney tympanumlarindaki pencere siralari — Ayasofya'nin
    # icini aydinlatan asil kaynak ve disaridan da okunan bir desen.
    for sgn in (-1, 1):
        for i in range(7):
            u = -tymp_l * 0.5 + tymp_l * (i + 0.5) / 7.0
            parts.append(hz.assign(
                hz.make_box(f"Tympanum_{sgn}_{i}", (0.7, 1.9, tymp_h * 0.30),
                            (sgn * (tymp_w * 0.5 - 0.3), u,
                             z0 + p.arch_spring_z + tymp_h * 0.42), col),
                mats["shadow"]))
    parts.append(hz.assign(hz.make_box(f"Kornis_{asset_name}",
                                       (tymp_w + 1.6, tymp_l + 1.6, 0.8),
                                       (0.0, 0.0, z0 + p.dome_base_z - 0.4),
                                       col), mats["cutstone"]))

    # --- ANA KUBBE: kirk kaburga, kirk pencere ---------------------------
    #
    # `segments = 40` bilinclidir: dilimler KABURGALARDIR ve pencereler
    # tam aralarina duser. 32 dilimle sayilan deger geometride yasamaz,
    # yalnizca katalogda yazar.
    dome = hz.make_dome(f"Kubbe_{asset_name}", r, p.dome_rise, (0.0, 0.0),
                        z0 + p.dome_base_z, segments=p.ribs, rings=9, col=col)
    hz.assign(dome, mats["lead"])
    parts.append(dome)

    # KUBBE, BIRLESMEDEN ONCE OLCULUR (Galata dersi, ADR 0033).
    dmn, dmx = hz.bounds(dome)
    measured_d = max(dmx[0] - dmn[0], dmx[1] - dmn[1])
    if abs(measured_d - p.dome_d) > 0.10:
        raise ValueError(f"kubbe mesh capi {measured_d:.3f} m — olculen "
                         f"{p.dome_d:.2f} m olmali")
    measured_crown = dmx[2]
    if abs(measured_crown - (z0 + p.crown_z)) > 0.05:
        raise ValueError(f"kilit {measured_crown:.2f} m — {z0 + p.crown_z:.2f} "
                         "olmali")

    _window_slots(parts, mats, col, f"KubbePencere_{asset_name}", p.ribs,
                  r * 0.985, z0 + p.dome_base_z + 0.9, 3.4, 0.85,
                  phase=math.pi / p.ribs)

    # KIRK KABURGA MESH'TE: kubbe zaten 40 dilimli uretiliyor (ADR 0045);
    # dikisler o dilimlerin uzerine oturur ve sayiyi GORUNUR kilar.
    for o in dk.kubbe_kaburga(f"KubbeKaburga_{asset_name}", 0.0, 0.0, r,
                              z0 + p.dome_base_z, p.dome_rise, col,
                              n=p.ribs, w=0.20):
        parts.append(hz.assign(o, mats["lead"]))
    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0, z0 + p.crown_z, col,
                     scale=1.8):
        parts.append(hz.assign(o, mats["lead"]))

    # --- Payandalar -------------------------------------------------------
    #
    # Bizans'in ve Sinan'in (II. Selim donemi) takviyeleri.
    #
    # TASMA olculu: yan nef kabugu 66,0 m, yapinin toplam genisligi 75,4 m,
    # yani payanda her yanda **4,7 m** disa cikar. Onceki kurulumda 73 m'lik
    # bir blogun disina 3,6 m ofsetle konmuslardi ve yapiyi 81 m'ye
    # genisletiyorlardi — payandanin tasmasi tahmin edilecek bir sey degil,
    # iki olcunun FARKI.
    #
    # SAYISI kaynaklarda verilmiyor ve planda ayri cizilmemis (yan nef
    # kabugunun konturuna dahil) — dort tanesi **D3**'tur.
    for sgn in (-1, 1):
        for i in range(4):
            py = aisle_y - aisle_l * 0.5 + aisle_l * (i + 0.5) / 4.0
            # Kutu duvar cizgisine ORTALANIR: yarisi icerde, yarisi disarda.
            # Ilk kurulumda merkez yarim tasma kadar disa kaydirilmisti ve
            # payanda 7,05 m cikiyordu — yapiyi 80,1 m'ye genisletiyordu.
            _payanda(parts, mats, col, f"Payanda_{sgn}{i}",
                     sgn * aisle_w * 0.5, py,
                     AYA_BUTTRESS_JUT * 2.0, 11.0,
                     z0 + aisle_h * 0.80, 8.5)

    # --- Vaftizhane: 1632'de YAGHANE ------------------------------------
    #
    # Yapi ayakta ama TURBE DEGIL. I. Mustafa 1639'da, Sultan Ibrahim
    # 1648'de buraya gomuldu; 1632'de burasi hala yag deposudur. Kutleyi
    # koyup islevini kataloga yazmak, "yok" demekten daha dogru.
    vf_r, vf_h = 6.2, 9.5
    vf = (aisle_w * 0.5 + 2.0, aisle_y - aisle_l * 0.5 - 2.0)
    parts.append(hz.assign(hz.make_tube(f"Vaftizhane_{asset_name}", vf_r, vf_r,
                                        vf_h, vf, z0, segments=8, col=col),
                           mats["stone"]))
    parts.append(hz.assign(hz.make_dome(f"VaftizhaneKubbe_{asset_name}",
                                        vf_r * 0.96, vf_r * 0.60, vf,
                                        z0 + vf_h, segments=12, rings=4,
                                        col=col), mats["lead"]))

    # --- DORT MINARE ------------------------------------------------------
    for i, (mx_, my_, mr, mkey) in enumerate(p.minarets):
        parts += _minare(p, mats, col, f"Minare{i}", mx_, my_, mr, mkey,
                         z0, p.minaret_h)

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_Nefler", (W, aisle_l, aisle_h),
                                    (0.0, aisle_y, z0 + aisle_h * 0.5), col),
                        mats["stone"]))
    l1.append(hz.assign(hz.make_box("L1_Nef", (AYA_NAVE_W, AYA_NAVE_L,
                                               AYA_NAVE_H),
                                    (0.0, nave_y, z0 + AYA_NAVE_H * 0.5), col),
                        mats["stone"]))
    l1.append(hz.assign(hz.make_box("L1_Narteks", (aisle_w + 8.0,
                                                   AYA_NARTHEX_D,
                                                   AYA_NARTHEX_H),
                                    (0.0, nar_y, z0 + AYA_NARTHEX_H * 0.5),
                                    col), mats["stone"]))
    l1.append(hz.assign(hz.make_box("L1_KemerKati", (tymp_w, tymp_l, tymp_h),
                                    (0.0, 0.0,
                                     z0 + p.arch_spring_z + tymp_h * 0.5), col),
                        mats["stone"]))
    l1.append(hz.assign(hz.make_dome("L1_Kubbe", r, p.dome_rise, (0.0, 0.0),
                                     z0 + p.dome_base_z, segments=16, rings=5,
                                     col=col), mats["lead"]))
    for i, (cx, cy, facing) in enumerate(halfs):
        l1.append(hz.assign(
            hz.make_half_dome(f"L1_Yarim{i}", r, p.dome_rise, (cx, cy),
                              z0 + p.arch_spring_z, facing=facing,
                              segments=12, rings=4, col=col), mats["lead"]))
    for i, (mx_, my_, mr, mkey) in enumerate(p.minarets):
        l1.append(hz.assign(
            hz.make_tube(f"L1_Minare{i}", mr, mr * 0.8, p.minaret_h + 6.5,
                         (mx_, my_), z0, segments=8, col=col), mats[mkey]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["cutstone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="selatin", palette=p.palette, status="draft",
                accuracy="D2",
                dome_d=round(p.dome_d, 2),
                dome_d_in_ns=AYA_DOME_D_IN_NS, dome_d_in_ew=AYA_DOME_D_IN_EW,
                measured_dome_d=round(measured_d, 2),
                measured_crown_z=round(measured_crown - z0, 2),
                dome_crown_z=round(p.crown_z, 2),
                dome_rise=round(p.dome_rise, 2),
                rise_ratio=round(p.rise_ratio, 3),
                dome_ribs=p.ribs, dome_windows=p.ribs,
                half_domes=len(halfs), exedrae_interior=4,
                minarets=len(p.minarets), sherefe_each=1,
                sherefe_total=len(p.minarets),
                brick_minarets=sum(1 for m in p.minarets if m[3] == "brick"),
                # Dort minarenin AYNI OLMADIGI olculu bir iddiadir ve
                # katalogda sayiyla durur: iki ince (dogu), iki kalin
                # (bati ikizleri). Tek bir "minarets=4" bunu tasiyamaz.
                minaret_r_thin=round(min(m[2] for m in p.minarets), 2),
                minaret_r_thick=round(max(m[2] for m in p.minarets), 2),
                minaret_top=round(p.minaret_h + 6.5, 2),
                harim_w=p.width, harim_d=p.length,
                face_deg=round(p.face_deg, 1),
                qibla_offset_deg=round(abs(QIBLA_1632_DEG - AYA_AXIS_DEG), 1),
                sadirvan=False, turbe_of_mustafa=False)
    return lod0, lod1, ucx, info
