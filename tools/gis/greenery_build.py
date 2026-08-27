"""
Hezarfen: 1632 — Yeşil doku alanları (mezarlık, mesire, bağ, bostan) ve
**ağaçsız** bölgeler.

`refs/maps/greenery_1632.geojson` + `data/gis/istanbul/greenery_local.json` üretir.

## Her sınırın bir DAYANAĞI var ve dayanağı yazılı

İlk yazımda on bir alanın hepsi aynı şekilde çizilmişti: kaba kutu, T2,
`status: draft`. Sonra bir tanesinde — Okmeydanı'nda — gerçekten ölçülmüş bir
sayı çıktı ve taslak onun **yarısı** kadardı. Yöntem tek sınandığı yerde
kaldı; kalan on için sınama yoktu.

Bu tur hepsini birden ele alıyor. Değişen şey şu: sınırlar artık bağımsız
elle çizilmiş kutular değil, **elimizdeki geometriden türetiliyor** ya da
**belgelenmiş bir ölçüye oturtuluyor**. Her alan `basis` taşır:

| `basis.kind` | ne demek |
|---|---|
| `documented` | Yayımlanmış bir alan ölçüsü var; poligon ağırlık merkezine göre ölçeklenip o alana **oturtulur** ve ölçülerek doğrulanır. |
| `walls`      | Sınır sur çizgisinin **kendisidir** — ayrı bir çizim değil. |
| `terrain`    | Sınırı arazi tanımlar (vadi tabanı, tepenin iki yamacı, iki dere arası). Çizilir ama iddiası **ölçülür**. |
| `drawn`      | Çapası yok. Kaba kutudur ve öyle olduğunu söyler. |

Biçim bize, ölçü kaynağa aittir. `documented` bir alanda poligonun *nereden
geçtiği* hâlâ kabadır; düzeltilen şey **büyüklüğüdür**.

## Bulunan çapalar

* **Okmeydanı ≈ 4,9 km²** — 17. yüzyılda alanı ölçen Abdullah el-Kâtip
  kabaca **8150 gez** verir; HÜTAD makalesinin hesabıyla ≈4,9 km². Taslak
  2,74 km²'ydi.
* **Galata surları içi ≈ 37 ha**, çevre ≈2800 m. Taslak 216 ha'ydı — **altı
  kat** büyük. (`walls_build` da bu çapaya oturtuldu.)
* **Sur içi (tarihi yarımada)**: sınır artık kendi sur çizgilerimizin
  kapattığı halkadır, ayrı bir kutu değil. Ölçülen ≈1334 ha; bugünkü Fatih
  ilçesi 1562 ha — aradaki fark 20. yüzyıl kıyı dolgularıyla tutarlıdır,
  yani ters yönde bir hata değil beklenen bir fark.
* **Karacaahmet ≈ 750 dönüm (75 ha)** — ama bu **bugünkü** alandır ve bizim
  için ÜST SINIRDIR; 1632'de daha küçüktü. `upper_bound` işaretiyle duruyor.

## Çapası bulunamayanlar

Mesire, bostan ve bağ sınırları için ölçü **yok** ve aramak da sonuç vermedi.
Bostan literatürünün kendi ifadesi: *"precise acreage measurements are
largely absent from Ottoman sources"* (Erdem 2024). Kayıtlar kira geliri ve
adet tutar, alan tutmaz. Bunlar için CLAUDE.md kuralı sürüyor: **metrik
geometri uydurulmaz.** Ama artık boyutlarını neyin tuttuğu yazılı ve
ölçülüyor — "gözüme öyle geldi" yerine "vadi tabanında duruyor, kotu şu".

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/greenery_build.py --dir data/gis/istanbul
"""

import argparse
import json
import math
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

# `geodesy`, `coastline_build`/`walls_build` yerine bilerek: bu makinede
# rasterio'nun DLL'i Windows uygulama denetimi tarafından bloklandı.
# `geodesy` aynı işleri bağımsız yapar ve dönüşümünü boru hattının kendi
# kaydına karşı doğrular. Ayrıntı: geodesy.py başlığı ve SETUP.md.
from geodesy import (from_utm35n, load_dem, ring_area,  # noqa: E402
                     sample_utm, self_check, to_utm)


def log(msg):
    print(f"[HZ] {msg}", flush=True)


WALLS_GEOJSON = os.path.join("refs", "maps", "walls_1632.geojson")

# `species`: hangi ağaç prefab ailesi. `none` = ağaç YOK.
#   servi  — mezarlık ağacı; İstanbul'da dikilir, yabanda bitmez
#   cinar  — mesire ve koru ağacı, dere boyu
#   meyve  — bağ/bahçe; elimizde ayrı bir varlık yok, seyrek çınarla temsil
AREAS = [
    dict(
        id="G_Karacaahmet", name="Karacaahmet Mezarlığı", kind="mezarlik",
        species="servi", density=0.020, tier="Reconstruction",
        source="TDV 'Karacaahmet Mezarligi': I. Murad devrinden beri; servileriyle "
               "meshur; en eski tas 1520 (Seyh Hamdullah) — 1632'de coktan vardir.",
        basis=dict(kind="documented", target_ha=75.0, upper_bound=True,
                   note="Bugunku mezarlik ~750 donum (750 000 m2 = 75 ha). Bu "
                        "1632 olcusu DEGIL, UST SINIRDIR: mezarlik yuzyillar "
                        "boyunca buyudu. Poligon ust sinira oturtuldu, yani "
                        "buradaki servi sayisi bir TAVANDIR."),
        ring=[(29.0195, 41.0108), (29.0292, 41.0102), (29.0316, 41.0146),
              (29.0272, 41.0182), (29.0200, 41.0172), (29.0176, 41.0138)],
    ),
    dict(
        id="G_EyupMezarligi", name="Eyüp Mezarlığı", kind="mezarlik",
        species="servi", density=0.020, tier="Reconstruction",
        source="TDV 'Eyup Mezarligi': Halic'in kuzey yakasinda, Eyup yamaclarina "
               "yayilmis; Gumussuyu bolumu bir tepenin IKI YAMACI; 16. yy definleri "
               "Kasgari tekkesinden asagi iner.",
        basis=dict(kind="terrain", claim="hill",
                   note="Kaynak niteliksel ama TOPOGRAFIK: 'bir tepenin iki "
                        "yamaci'. Poligonun gercekten bir tepeyi kapsayip "
                        "kapsamadigi olculuyor (kot farki >= 40 m)."),
        ring=[(28.9292, 41.0452), (28.9378, 41.0448), (28.9402, 41.0492),
              (28.9358, 41.0532), (28.9288, 41.0524), (28.9264, 41.0488)],
    ),
    dict(
        id="G_PeraBaglari", name="Pera Bağları", kind="bag",
        species="meyve", density=0.004, tier="Reconstruction",
        source="Galata'nin UST SURLARININ OTESI bag, bahce, mezarlik ve korudur; "
               "yapilasma 18. yy ortasindan sonra baslar (Batur, 'Galata and Pera').",
        basis=dict(kind="terrain", claim="outside_galata",
                   note="Sinirin buyuklugu bilinmiyor ama YERI tanimli: Galata "
                        "surunun DISI. Poligonun hicbir kosesinin sur "
                        "poligonu icinde olmadigi olculuyor."),
        ring=[(28.9720, 41.0296), (28.9852, 41.0286), (28.9890, 41.0342),
              (28.9828, 41.0400), (28.9718, 41.0390), (28.9688, 41.0340)],
    ),
    dict(
        id="G_Kagithane", name="Kağıthane Mesiresi", kind="mesire",
        species="cinar", density=0.008, tier="Reconstruction",
        source="Evliya Celebi 17. yy: unlu mesire ve CAYIR. SADABAD 1722'DIR — "
               "1632'de kasir/saray YOKTUR.",
        basis=dict(kind="terrain", claim="valley",
                   trace=dict(lat0=41.0645, lat1=41.0785, lon_lo=28.9480,
                              lon_hi=28.9760, steps=14, half_w_m=290.0),
                   note="Mesire dere boyu CAYIRDIR, yamac degil. Sinir artik "
                        "cizilmiyor, DEM'den IZLENIYOR: her enlemde vadinin en "
                        "alcak noktasi bulunup iki yana aciliyor. Elle cizilmis "
                        "kutu olculdugunde ortalama kotu 46 m cikmisti — yani "
                        "cayirda degil yamactaydi."),
        ring=None,
    ),
    dict(
        id="G_Goksu", name="Göksu Mesiresi", kind="mesire",
        species="cinar", density=0.010, tier="Reconstruction",
        source="Evliya (17. yy): kayikla gezilen, gul bahceleri ve degirmenlerle "
               "cevrili. Goksu ile Kucuksu dereleri arasi 500-600 m; aradaki "
               "cayirin iki yani YUKSEK AGAC ve BAG.",
        basis=dict(kind="terrain", claim="width", lo_m=500.0, hi_m=750.0,
                   note="Elimizdeki TEK metrik ipucu: iki dere arasi 500-600 m. "
                        "Poligonun kisa kenari olculuyor."),
        ring=[(29.0638, 41.0788), (29.0762, 41.0798), (29.0772, 41.0856),
              (29.0644, 41.0846)],
    ),
    dict(
        id="G_LangaBostani", name="Langa Bostanı", kind="bostan",
        species="none", density=0.0, tier="Reconstruction",
        source="Dolmus Theodosius (Eleutherios) limani. Suleymaniye Vakfi 1583-1586 "
               "kayitlarinda kiralik bostan (29 290 akce) — 1632'den ONCE belgeli. "
               "Konum Yenikapi kazilariyla bilinir. BOSTANDA AGAC YOK: sebze tarhi.",
        basis=dict(kind="terrain", claim="filled_harbour",
                   note="Dolmus bir liman havzasidir: ALCAK ve SUR ICINDE olmak "
                        "zorunda. Ikisi de olculuyor (ortalama kot <= 12 m, "
                        "govde yarimadanin icinde). Ilk cizimde guney kenari "
                        "Marmara deniz surunun DISINDA, yani denizde kaliyordu; "
                        "sur cizgisi olculup iceri cekildi."),
        ring=[(28.9470, 41.0022), (28.9585, 41.0032), (28.9590, 41.0070),
              (28.9475, 41.0062)],
    ),
    dict(
        id="G_YedikuleBostanlari", name="Yedikule Bostanları", kind="bostan",
        species="none", density=0.0, tier="Reconstruction",
        source="'Yedikule ile Topkapi arasinda'; 1719'da 77 vakif ve ozel bostan. "
               "Kara surlari boyunca serit. BOSTANDA AGAC YOK.",
        basis=dict(kind="walls", wall="wall_land", strip_m=190.0,
                   between=("GT_Yedikule", "GT_Topkapi"),
                   note="Sinirin GUZERGAHI belgeli: kara surlari boyunca, "
                        "Yedikule ile Topkapi KAPILARI arasinda. Artik ayri bir "
                        "kutu degil, sur cizgisinin kendisinden turetilen bir "
                        "serit — kapilar da sur verisinden okunuyor. Seridin "
                        "ENI bilinmiyor ve tahmindir."),
        ring=None,
    ),
    dict(
        id="G_Okmeydani_Yasak", name="Okmeydanı — ağaçsız talim alanı",
        kind="yasak", species="none", density=0.0, tier="Documented",
        source="II. Bayezid vakfiyesi: meydana 'bir karis tecavuz edilmemesi, YAPI, "
               "MEZAR, SU YOLU, BAG VE BAHCE yapilmamasi' kesin olarak yasak. "
               "Hezarfen'in talim yaptigi yer (RESEARCH.md). Agac dikmek BELGEYE "
               "AYKIRI olur.",
        basis=dict(kind="documented", target_ha=490.0,
                   note="17. yuzyilda meydani olcen Abdullah el-Katip alani kabaca "
                        "8150 GEZ verir; HUTAD makalesinin hesabiyla ~4,9 km2. "
                        "Taslak 2,74 km2'ydi — yari yariya kucuktu."),
        ring=[(28.9450, 41.0500), (28.9620, 41.0490), (28.9670, 41.0580),
              (28.9580, 41.0640), (28.9440, 41.0620), (28.9400, 41.0550)],
    ),
    dict(
        id="G_Surici_Yerlesim", name="Suriçi (sur içi)", kind="yerlesim",
        species="none", density=0.0, tier="Documented",
        source="Sinir SURUN KENDISIDIR: kara surlari + Marmara ve Halic deniz "
               "surlari birlikte kapali bir cevre olusturur. Sur icinde yaban "
               "agaci serpmek her yerde yanlistir — sehir, bahce ve bostan olsa "
               "da yaban ormani degildir.",
        basis=dict(kind="walls", wall="peninsula",
                   note="Onceki taslak 1097 ha'lik ELLE CIZILMIS bir kutuydu ve "
                        "sur cizgisiyle bagimsizdi; ikisi bir gun ayrisirdi. "
                        "Artik ayni geometriden geliyorlar. Olculen alan bugunku "
                        "Fatih ilcesinden (1562 ha) kucuk cikar ve bu BEKLENEN "
                        "farktir: Marmara ve Halic kiyilari 20. yuzyilda dolduruldu."),
        ring=None,
    ),
    dict(
        id="G_Galata_Yerlesim", name="Galata surları içi", kind="yerlesim",
        species="none", density=0.0, tier="Documented",
        source="Ceneviz surlari 1632'de ayaktaydi (1860'larda yikildi). Sinir "
               "surun kendisidir.",
        basis=dict(kind="walls", wall="wall_galata",
                   note="Onceki taslak 216 ha'ydi; Ceneviz surlarinin cevreledigi "
                        "alan ~37 ha — yani kutu ALTI KAT buyuktu. Bu, toptan "
                        "ele almanin gerekcesinin kendisi: bir alanda yaptigim "
                        "hata (Okmeydani, yarim) baska bir alanda TERS yonde ve "
                        "cok daha buyuktu."),
        ring=None,
    ),
    dict(
        id="G_Uskudar_Yerlesim", name="Üsküdar (yapılı)", kind="yerlesim",
        species="none", density=0.0, tier="Graybox",
        source="Iskele ve Dogancilar cevresi.",
        basis=dict(kind="drawn",
                   note="CAPA BULUNAMADI. Uskudar'in 1632'deki yapili alani icin "
                       "ne olcu ne de sur var; sinirsiz bir yerlesimdir. Kaba "
                       "kutudur ve tarihsel sinir iddiasi TASIMAZ."),
        ring=[(29.0080, 41.0202), (29.0250, 41.0196), (29.0292, 41.0288),
              (29.0152, 41.0330), (29.0062, 41.0280)],
    ),
]


# ------------------------------------------------------------------ geometri

def point_in_ring(pt, ring):
    x, y = pt
    inside = False
    for i in range(len(ring)):
        x0, y0 = ring[i]
        x1, y1 = ring[(i + 1) % len(ring)]
        if (y0 > y) != (y1 > y):
            xc = x0 + (y - y0) * (x1 - x0) / (y1 - y0)
            if x < xc:
                inside = not inside
    return inside


def fit_area(ring_utm, target_ha):
    """
    Halkayı ağırlık merkezine göre ölçekleyip belgelenmiş alana oturtur.

    Biçimi korur, boyu değiştirir. Elimizde sınırın kabaca *nereden geçtiği*
    var ama *ne kadar büyük olduğu* yoktu; kaynak büyüklüğü veriyorsa
    ikisini birden uydurmaktansa biçimi koruyup boyu ona uydurmak dürüsttür.
    """
    cur = ring_area(ring_utm) / 10000.0
    s = math.sqrt(target_ha / cur)
    cx = sum(p[0] for p in ring_utm) / len(ring_utm)
    cy = sum(p[1] for p in ring_utm) / len(ring_utm)
    out = [(cx + (e - cx) * s, cy + (n - cy) * s) for e, n in ring_utm]
    return out, cur, ring_area(out) / 10000.0


def short_side_m(ring_utm):
    """Poligonun dar yönündeki genişliği — en küçük kenar-dik açıklık."""
    cx = sum(p[0] for p in ring_utm) / len(ring_utm)
    cy = sum(p[1] for p in ring_utm) / len(ring_utm)
    best = float("inf")
    for i in range(len(ring_utm)):
        ax, ay = ring_utm[i]
        bx, by = ring_utm[(i + 1) % len(ring_utm)]
        dx, dy = bx - ax, by - ay
        L = math.hypot(dx, dy)
        if L < 1e-6:
            continue
        nx, ny = -dy / L, dx / L
        span = max(abs((p[0] - ax) * nx + (p[1] - ay) * ny) for p in ring_utm)
        best = min(best, span)
    _ = (cx, cy)
    return best


def trace_valley(meta, heights, lat0, lat1, lon_lo, lon_hi, steps, half_w_m):
    """
    Vadi tabanını **izleyerek** bant üretir: her enlemde en alçak boylamı bul,
    o eksenin iki yanına `half_w_m` aç.

    Neden çizim değil izleme: Kağıthane mesiresi dere boyu **çayırdır**, ve
    elle çizilmiş kutu ölçüldüğünde ortalama kotu 46 m çıktı — yani kutu
    çayırda değil, vadinin yamaçlarındaydı. "Vadi tabanında" demek, kutuyu
    vadi tabanına koymakla aynı şey değil. Bant artık DEM'den çıkıyor;
    arazi değişirse sınır da onunla değişir.
    """
    axis = []
    for i in range(steps + 1):
        lat = lat0 + (lat1 - lat0) * i / steps
        # En alcak KARA noktasi — en alcak DEM degeri degil.
        #
        # Fark onemli cikti: Kagithane vadisinde DEM'in taban kotuna (-12 m)
        # oturmus 60x80 m'lik bir yama var; deniz doldurmasi dere agzindan
        # yukari kacmis. Eksen "en alcak" derse oraya kilitleniyor ve mesire
        # bir su birikintisinin etrafina diziliyordu. CAYIR karadadir.
        best = None
        for k in range(80):
            lon = lon_lo + (lon_hi - lon_lo) * k / 79.0
            e, n = to_utm(((lon, lat),))[0]
            z = sample_utm(meta, heights, e, n)
            if z is not None and z >= 1.0 and (best is None or z < best[1]):
                best = ((e, n), z)
        if best:
            axis.append(best[0])

    left, right = [], []
    for i, (e, n) in enumerate(axis):
        j, k = min(i + 1, len(axis) - 1), max(i - 1, 0)
        dx, dy = axis[j][0] - axis[k][0], axis[j][1] - axis[k][1]
        L = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / L, dx / L
        left.append((e + nx * half_w_m, n + ny * half_w_m))
        right.append((e - nx * half_w_m, n - ny * half_w_m))
    return left + right[::-1]


def strip_along(pts, width_m, toward):
    """
    Bir polyline'dan, `toward` noktasına doğru `width_m` genişliğinde şerit.

    Yedikule bostanları "sur boyunca" bir şerittir; sınırı ayrı çizmek yerine
    surun kendisinden türetmek, ikisinin bir gün ayrışmasını imkânsız kılar.
    """
    out = []
    for i, (x, y) in enumerate(pts):
        j = min(i + 1, len(pts) - 1)
        k = max(i - 1, 0)
        dx, dy = pts[j][0] - pts[k][0], pts[j][1] - pts[k][1]
        L = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / L, dx / L
        # Ic tarafi SEC: hedefe yaklastiran normal.
        if (toward[0] - x) * nx + (toward[1] - y) * ny < 0:
            nx, ny = -nx, -ny
        out.append((x + nx * width_m, y + ny * width_m))
    return list(pts) + out[::-1]


# --------------------------------------------------------------------- veri

def load_walls():
    """Sur geometrisini UTM olarak okur: hatlar, Galata halkası, kapılar."""
    with open(WALLS_GEOJSON, encoding="utf-8") as fh:
        data = json.load(fh)
    lines, gates, galata = {}, {}, None
    for f in data["features"]:
        pr = f.get("props") or f.get("properties") or {}
        g = f["geometry"]
        fid = pr.get("id", "")
        if g["type"] == "LineString":
            lines[fid] = to_utm([(p[0], p[1]) for p in g["coordinates"]])
        elif g["type"] == "Polygon" and fid == "wall_galata":
            galata = to_utm([(p[0], p[1]) for p in g["coordinates"][0]][:-1])
        elif g["type"] == "Point":
            gates[fid] = to_utm([(g["coordinates"][0], g["coordinates"][1])])[0]
    return lines, gates, galata


def peninsula_ring(lines):
    """Üç surun birlikte kapattığı halka — sur içinin sınırı budur."""
    return (lines["wall_land"]
            + lines["wall_sea_halic"][::-1][1:]
            + lines["wall_sea_marmara"][::-1][1:])


def elev_stats(meta, heights, ring, step_m=25.0):
    """Halka içindeki kot istatistikleri + kara oranı — tek geçişte."""
    xs = [q[0] for q in ring]
    ys = [q[1] for q in ring]
    vals, total, land = [], 0, 0
    y = min(ys)
    while y <= max(ys):
        x = min(xs)
        while x <= max(xs):
            if point_in_ring((x, y), ring):
                total += 1
                h = sample_utm(meta, heights, x, y)
                if h is not None:
                    vals.append(h)
                    if h >= 0.5:
                        land += 1
            x += step_m
        y += step_m
    if not vals:
        return dict(n=0, lo=0.0, hi=0.0, mean=0.0, land_frac=0.0, wet=0)
    # `wet`: alanin ICINDE kalan deniz seviyesi ALTI hucre sayisi. Bu bir
    # ARAZI kusurudur, sinir kusuru degil — ama sinirdan gorunur ve raporda
    # durmali. Kagithane'de 17 hucrelik bir yama boyle bulundu: deniz
    # doldurmasi dere agzindan yukari kacmis, mesirenin ortasinda -12 m'lik
    # bir gol birakmis (ADR 0007'nin isi).
    return dict(n=total, lo=min(vals), hi=max(vals),
                mean=sum(vals) / len(vals),
                land_frac=land / total if total else 0.0,
                wet=sum(1 for v in vals if v < 0.5))


# --------------------------------------------------------------------- main

def main():
    p = argparse.ArgumentParser(description="1632 yesil doku alanlari")
    p.add_argument("--dir", default="data/gis/istanbul")
    p.add_argument("--geojson", default="refs/maps/greenery_1632.geojson")
    args = p.parse_args()

    meta, heights = load_dem(args.dir)
    log(f"UTM oz-denetimi: boru hattinin kaydiyla sapma "
        f"{self_check(meta) * 1000:.2f} mm")
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]

    lines, gates, galata = load_walls()
    pen = peninsula_ring(lines)
    pen_cx = sum(q[0] for q in pen) / len(pen)
    pen_cy = sum(q[1] for q in pen) / len(pen)
    log(f"sur verisi okundu: yarimada halkasi {len(pen)} nokta, "
        f"{ring_area(pen) / 1e4:.0f} ha; Galata {ring_area(galata) / 1e4:.0f} ha")

    features, local, problems = [], [], []

    for a in AREAS:
        b = a["basis"]
        kind = b["kind"]
        note_extra = ""

        # --- 1. SINIRI KUR -------------------------------------------------
        if kind == "walls":
            if b["wall"] == "peninsula":
                ring = list(pen)
            elif b["wall"] == "wall_galata":
                ring = list(galata)
            else:
                line = lines[b["wall"]]
                g0, g1 = (gates[g] for g in b["between"])
                i0 = min(range(len(line)), key=lambda i: math.dist(line[i], g0))
                i1 = min(range(len(line)), key=lambda i: math.dist(line[i], g1))
                lo, hi = sorted((i0, i1))
                seg = line[lo:hi + 1]
                ring = strip_along(seg, b["strip_m"], (pen_cx, pen_cy))
                note_extra = (f" Sur uzerinde {b['between'][0]} ile "
                              f"{b['between'][1]} arasi {len(seg)} nokta, "
                              f"serit eni {b['strip_m']:.0f} m.")
        elif b.get("trace"):
            t = b["trace"]
            ring = trace_valley(meta, heights, t["lat0"], t["lat1"], t["lon_lo"],
                                t["lon_hi"], t["steps"], t["half_w_m"])
            note_extra = f" Vadi ekseni {t['steps'] + 1} noktada izlendi."
        else:
            ring = to_utm(a["ring"])

        # --- 2. BELGELI ALANA OTURT ---------------------------------------
        if kind == "documented":
            ring, before, after = fit_area(ring, b["target_ha"])
            note_extra = (f" Elle cizim {before:.0f} ha -> belgeli capaya "
                          f"olceklendi {after:.0f} ha.")
            if abs(after - b["target_ha"]) / b["target_ha"] > 0.02:
                problems.append(f"{a['id']}: alan capaya oturmadi "
                                f"({after:.0f} / {b['target_ha']:.0f} ha)")

        area_ha = ring_area(ring) / 10000.0
        st = elev_stats(meta, heights, ring)

        # --- 3. DENETIM ----------------------------------------------------
        #
        # Iki soru FARKLIDIR: poligon arazinin icinde mi (elle yazilmis lon/lat
        # kolayca kayar), ve AGAC DIKILEN bir alanin yeterince karasi var mi.
        # Ilk yazimda tek soru vardi ("hic kosesi deniz altinda mi") ve kiyiya
        # degen alti alani birden reddetti — kiyi alaninin kosesinin suda
        # olmasi NORMALDIR; onemli olan alanin GOVDESI.
        for e, n in ring:
            if sample_utm(meta, heights, e, n) is None:
                problems.append(f"{a['id']}: kose arazi disinda ({e:.0f}, {n:.0f})")
        if a["density"] > 0 and st["land_frac"] < 0.55:
            problems.append(f"{a['id']}: agac dikilecek alanin yalnizca "
                            f"%{st['land_frac'] * 100:.0f}'i kara")

        # Dayanagin KENDI iddiasi da olculur — "vadi tabaninda" demek bedava
        # olmamali.
        claim = b.get("claim")
        if claim == "hill" and st["hi"] - st["lo"] < 40.0:
            problems.append(f"{a['id']}: kot farki {st['hi'] - st['lo']:.0f} m — "
                            "kaynak 'bir tepenin iki yamaci' diyor, bu duz")
        if claim == "valley" and st["mean"] > 45.0:
            problems.append(f"{a['id']}: ortalama kot {st['mean']:.0f} m — "
                            "mesire dere boyu CAYIRDIR, yamac degil")
        if claim == "filled_harbour":
            if st["mean"] > 12.0:
                problems.append(f"{a['id']}: ortalama kot {st['mean']:.0f} m — "
                                "dolmus liman havzasi ALCAK olmali")
            inside = sum(1 for q in ring if point_in_ring(q, pen))
            if inside < len(ring) - 1:
                problems.append(f"{a['id']}: {len(ring) - inside} kosesi sur "
                                "DISINDA — Langa sur icindedir")
        if claim == "outside_galata":
            bad = sum(1 for q in ring if point_in_ring(q, galata))
            if bad:
                problems.append(f"{a['id']}: {bad} kosesi Galata surunun ICINDE — "
                                "baglar surun DISINDADIR")
        if claim == "width":
            w = short_side_m(ring)
            note_extra = f" Olculen dar kenar {w:.0f} m."
            if not (b["lo_m"] <= w <= b["hi_m"]):
                problems.append(f"{a['id']}: dar kenar {w:.0f} m, kaynak "
                                f"{b['lo_m']:.0f}-{b['hi_m']:.0f} m diyor")

        cx = sum(q[0] for q in ring) / len(ring)
        cy = sum(q[1] for q in ring) / len(ring)
        radius = max(math.dist((cx, cy), q) for q in ring)
        trees = int(area_ha * 10000 * st["land_frac"] * a["density"])

        log(f"{a['id']:24s} {kind:10s} {a['species']:6s} "
            f"{area_ha:7.1f} ha  kara %{st['land_frac'] * 100:3.0f}  "
            f"kot {st['lo']:5.0f}-{st['hi']:5.0f} m  -> ~{trees:6d} agac"
            + (f"  [ICINDE {st['wet']} SU HUCRESI]" if st["wet"] else ""))

        basis_text = b["note"] + note_extra
        props = dict(layer="greenery", id=a["id"], name=a["name"], kind=a["kind"],
                     species=a["species"], density_per_m2=a["density"],
                     tier=a["tier"], area_ha=round(area_ha, 1),
                     land_fraction=round(st["land_frac"], 3),
                     elev_min_m=round(st["lo"], 1), elev_max_m=round(st["hi"], 1),
                     source=a["source"], basis=kind, basis_note=basis_text,
                     status="draft" if kind in ("drawn", "terrain") else "anchored")
        coords = [list(from_utm35n(e, n)) for e, n in ring]
        features.append(dict(type="Feature", properties=props,
                             geometry=dict(type="Polygon",
                                           coordinates=[coords + [coords[0]]])))
        local.append(dict(id=a["id"], name=a["name"], kind=a["kind"],
                          species=a["species"], density=a["density"],
                          tier=a["tier"], area_ha=round(area_ha, 1),
                          land_fraction=round(st["land_frac"], 3),
                          basis=kind,
                          center_x=round(cx - gx, 2), center_z=round(cy - gz, 2),
                          radius_m=round(radius, 2),
                          ring=[{"x": round(q[0] - gx, 2), "z": round(q[1] - gz, 2)}
                                for q in ring]))

    if problems:
        for m in problems:
            log("SORUN: " + m)
        log("greenery_build BASARISIZ")
        return 1

    collection = {
        "type": "FeatureCollection",
        "name": "greenery_1632",
        "metadata": {
            "title": "Yeşil doku ve ağaçsız alanlar — 1632 İstanbul",
            "status": "Sınırların DAYANAĞI alan alan yazılı; Caner onayı bekliyor",
            "basis_kinds": {
                "documented": "Yayımlanmış alan ölçüsüne oturtuldu (biçim kaba, "
                              "BÜYÜKLÜK belgeli).",
                "walls": "Sınır sur çizgisinin kendisidir — ayrı bir çizim yok.",
                "terrain": "Sınırı arazi tanımlar; iddia ölçülerek doğrulanır.",
                "drawn": "Çapası yok; kaba kutudur ve öyle olduğunu söyler.",
            },
            "NOT_measured": (
                "Mesire, bostan ve bağ sınırları için ölçü YOKTUR ve arama sonuç "
                "vermedi. Bostan literatürünün kendi ifadesi: kayıtlar kira geliri "
                "ve adet tutar, alan tutmaz. CLAUDE.md: 'kaynak niteliksel olduğunda "
                "metrik geometri UYDURMA'."
            ),
            "negative_rule": (
                "G_Okmeydani_Yasak bir YOKLUK kaydıdır: II. Bayezid vakfiyesi "
                "meydanda yapı, mezar, su yolu, bağ ve bahçe yapılmasını yasaklar. "
                "Oraya ağaç dikmek belgeye aykırı olur."
            ),
            "world_origin": meta["world_origin"],
            "copyright": "Bu çizim bize aittir; hiçbir telifli harita kopyalanmadı.",
        },
        "features": features,
    }

    geo_path = os.path.abspath(args.geojson)
    os.makedirs(os.path.dirname(geo_path), exist_ok=True)
    with open(geo_path, "w", encoding="utf-8") as fh:
        json.dump(collection, fh, indent=1, ensure_ascii=False)
    log(f"wrote {args.geojson} ({len(features)} alan)")

    local_path = os.path.join(os.path.abspath(args.dir), "greenery_local.json")
    with open(local_path, "w", encoding="utf-8") as fh:
        json.dump({"world_origin": meta["world_origin"], "areas": local},
                  fh, ensure_ascii=False)
    log("wrote greenery_local.json")

    by_basis = {}
    for a, l in zip(AREAS, local):
        by_basis.setdefault(a["basis"]["kind"], []).append(l["id"])
    for k in ("documented", "walls", "terrain", "drawn"):
        if k in by_basis:
            log(f"  {k:10s} {len(by_basis[k])} alan: {', '.join(by_basis[k])}")

    planted = sum(a["area_ha"] * 10000 * a["land_fraction"] * a["density"]
                  for a in local)
    log(f"toplam ~{int(planted)} agac (adli alanlarda); genel yamac ayri hesaplanir")
    log("greenery_build OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
