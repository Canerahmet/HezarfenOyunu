"""
Hezarfen: 1632 — Kıyı çizgisi taslağı (plan Görev 10, Faz 1 madde 2).

İki katman üretir:

  1. **modern_shoreline** — DEM'in 0 m konturu. Ölçülmüş, tartışmasız veri (T1'e en
     yakın katman) ama MODERN kıyıdır.
  2. **correction_zone** — 1632'de farklı olduğu BİLİNEN alanlar. Geometrileri
     `docs/RESEARCH.md`e dayanır ama **metrik ofsetleri kaynaklı DEĞİLDİR** —
     kaba kutulardır, Caner onayı ve daha iyi kaynak bekler. Hepsi T2/T3.

Neden OSM değil de DEM konturu (plan "OSM" öneriyordu):
    Kıyı çizgisi ile arazi AYNI kaynaktan gelmezse birbirini tutmaz — deniz düzlemi
    karayı keser ya da kıyıda görünmez bir uçurum kalır. Oyuncu suyun üstünde
    uçacağı için bu tutarsızlık doğrudan görünür. OSM daha keskin bir kıyı verir
    ama Copernicus DEM'le hizalanmaz. Rafinasyon yolu olarak OSM açık bırakıldı
    (bkz. ADR 0008); o gün geldiğinde ODbL atıf yükümlülüğü de doğar.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/coastline_build.py --dir data/gis/istanbul
"""

import argparse
import json
import math
import os

import sys

import numpy as np

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

from geodesy import (from_utm35n as _from_utm35n,  # noqa: E402
                     to_utm35n as _to_utm35n)


# rasterio.warp.transform yerine gecen kabuk — imza AYNI, gerekce geodesy.py
# basliginda: bu makinede rasterio'nun DLL'i Windows uygulama denetimince
# bloklu ve butun GIS araclarini birden calistirilamaz kiliyordu.
# Yalnizca WGS84 <-> UTM 35N (EPSG:32635) yonu desteklenir; bu boru hattinin
# kullandigi tek donusum odur ve baska bir CRS istenirse SESSIZCE yanlis
# sonuc vermek yerine yukselir.
def warp_transform(src_crs, dst_crs, xs, ys):
    wgs, utm = "EPSG:4326", "EPSG:32635"
    if (src_crs, dst_crs) == (wgs, utm):
        pairs = [_to_utm35n(x, y) for x, y in zip(xs, ys)]
    elif (src_crs, dst_crs) == (utm, wgs):
        pairs = [_from_utm35n(x, y) for x, y in zip(xs, ys)]
    else:
        raise ValueError(f"desteklenmeyen donusum: {src_crs} -> {dst_crs}")
    return [p[0] for p in pairs], [p[1] for p in pairs]

from dem_fetch import hillshade, write_png   # noqa: E402

SEA_LEVEL_CONTOUR = 0.5      # m — deniz (0) ile kara arasindaki esik
SEA_SEED_LON = 29.0250       # Bogaz ortasi — dem_probe.py orada 0,0 m olcuyor
SEA_SEED_LAT = 41.0400

# 1632 kiyisi icin dolgu alanlarinda kullanilan deniz esigi.
#
# YONTEM (Caner karari 2026-08-18: "makul bir tahminle geri alabilirsin"):
# Modern dolgu alanlari YAPAY olarak duz ve alcaktir; dogal kiyi, arazinin
# yukselmeye basladigi yerdir. Bu yuzden sabit bir metre ofseti uydurmak yerine
# esik ARAZIDEN turetilir: dolgu bolgesinde "deniz" sayilan irtifa 0,5 m'den
# 5,0 m'ye cikarilir ve kiyi, dogal yamacin etegine geri ceker.
#
# 5,0 m secimi: Istanbul limanlarindaki doldurma zemin tipik olarak deniz
# seviyesinin 2-5 m ustundedir; 5 m'nin uzeri dogal yamactir. Tek ve ayni deger
# butun dolgu alanlarinda kullanilir — alan alan ayar yapmak, kaynagi olmayan
# sayilara sahte bir kesinlik verirdi.
#
# Sonuc OLCULUR ve raporlanir (asagidaki "ic kayma" satirlari). T2'dir.
FILL_SEA_THRESHOLD_M = 5.0

# Alan sinirinda sert basamak, kiyi cizgisinde yapay bir sicrama birakir.
# Esik bu mesafede yumusak gecisle taban degerine doner.
ZONE_BLEND_M = 150.0
MIN_RING_METRES = 400.0      # bundan kisa halkalar atilir (DEM gurultusu/kayaliklar)
SIMPLIFY_TOLERANCE_M = 18.0  # Douglas-Peucker; ~2.4 ornek araligi


def log(msg):
    print(f"[HZ] {msg}", flush=True)


# ------------------------------------------------------- 1632 düzeltme alanları
#
# KAYNAK: docs/RESEARCH.md Bolum 4 satir 169 —
#   "Kiyi cizgisi: Eminonu-Sirkeci dolgulari YOK; Halic ve Marmara kiyisi
#    bugunkunden iceride. Langa/Vlanga bostanlari (eski Theodosius limani dolmus
#    alani) yesil/bostan alani."
#
# Bu satir NITELIKSELDIR. Asagidaki kutular, o niteliksel ifadenin sahnede
# gorunur hale gelmesi icin cizilmis KABA TASLAKLARDIR; metrik dayanaklari yoktur.
# Her biri T2/T3 ve `status: draft`. Kesinlestirme yolu: donem haritalarinin
# georeferanslanmasi (Gorev 3/Faz 1 madde 3) + Caner onayi.
CORRECTION_ZONES = [
    {
        "id": "CZ_Eminonu_Sirkeci_dolgu",
        "name": "Eminönü–Sirkeci dolgusu",
        "action": "remove_fill",
        "tier": "Reconstruction",
        "bbox": [28.9680, 41.0145, 28.9870, 41.0200],
        "research_note": "RESEARCH.md §4: 'Eminönü–Sirkeci dolguları YOK'.",
        "design_note": ("1632'de Haliç kıyısı burada bugünkünden içeridedir. Yeni Cami "
                        "(o tarihte 'Zulmiyye' harabesi) denize YAKINDIR — RESEARCH.md §3."),
        "confidence": "kaba taslak; metrik ofset kaynaklı değil",
    },
    {
        "id": "CZ_Unkapani_dolgu",
        "name": "Unkapanı kıyısı",
        "action": "remove_fill",
        "tier": "Reconstruction",
        "bbox": [28.9520, 41.0180, 28.9690, 41.0245],
        "research_note": "RESEARCH.md §4: 'Haliç ... kıyısı bugünkünden içeride'.",
        "design_note": "Unkapanı ve Balıkpazarı iskeleleri 1632'de faal (RESEARCH.md §3).",
        "confidence": "kaba taslak; metrik ofset kaynaklı değil",
    },
    {
        "id": "CZ_Langa_Vlanga_bostan",
        "name": "Langa / Vlanga bostanları",
        "action": "convert_to_gardens",
        "tier": "Reconstruction",
        "bbox": [28.9440, 40.9985, 28.9605, 41.0055],
        # ESIK YOK — bilincli. Langa modern bir dolgu DEGILDIR: Theodosius limani
        # Osmanli donemine cok once dolmus ve bostana donusmustur. Olcum de bunu
        # dogruluyor (medyan irtifa 4,6 m — dolmus liman tabani). Buraya dolgu
        # esigi uygulansaydi, 1632'de bostan olan alani yeniden DENIZE cevirirdik.
        "sea_threshold_m": None,
        "research_note": ("RESEARCH.md §4: 'Langa/Vlanga bostanları (eski Theodosius "
                          "limanı dolmuş alanı) yeşil/bostan alanı.'"),
        "design_note": ("Su değil, KARA — ama yapılaşmamış bostan. Marmara kıyı kapısı "
                        "'Langa' burada (RESEARCH.md §3, Eremya Çelebi kapı listesi). "
                        "Kıyı geometrisi DEĞİŞMEZ; bu bir arazi-kullanım işaretidir."),
        "confidence": "alan konumu güvenli, sınırları taslak",
    },
    {
        "id": "CZ_Galata_Karakoy_dolgu",
        "name": "Galata / Karaköy kıyısı",
        "action": "remove_fill",
        "tier": "Reconstruction",
        "bbox": [28.9690, 41.0200, 28.9880, 41.0260],
        "research_note": "RESEARCH.md §4: Haliç kıyısı bugünkünden içeride.",
        "design_note": ("Galata surları 1632'de ayakta ve deniz kapıları kıyıya çok "
                        "yakındır — dolgu geri alınınca sur-deniz mesafesi daralır."),
        "confidence": "kaba taslak; metrik ofset kaynaklı değil",
    },
    {
        "id": "CZ_Marmara_kiyisi_geneli",
        "name": "Marmara kıyısı (Ahırkapı–Narlıkapı)",
        "action": "remove_fill",
        "tier": "Reconstruction",
        "bbox": [28.9200, 40.9950, 28.9900, 41.0100],
        "sea_threshold_m": FILL_SEA_THRESHOLD_M,
        "research_note": "RESEARCH.md §4: 'Marmara kıyısı bugünkünden içeride'.",
        "design_note": ("Sahil yolu (Kennedy Cad.) dolgusu 20. yy'dır; deniz surları "
                        "1632'de suya çok yakındır. Kapılar: Ahırkapı, Çatladıkapı, "
                        "Kumkapı, Langa, Davutpaşa, Samatya, Narlıkapı (Eremya Çelebi)."),
        "confidence": "kaba taslak; metrik ofset kaynaklı değil",
    },
]

# Dolgu alanlarina esigi ekle (Langa haric — yukarida acikca None).
for _z in CORRECTION_ZONES:
    _z.setdefault("sea_threshold_m", FILL_SEA_THRESHOLD_M)


# ------------------------------------------------------------- marching squares

def marching_squares(grid, level):
    """
    Eş yükselti eğrisi segmentleri (ızgara koordinatında, alt-sol orijinli).

    Kütüphane yerine elle: matplotlib/scipy bu boru hattına yalnızca kontur için
    girecekti. Algoritma 16 durumdan ibarettir ve deterministiktir — sürüm
    değişiminde kıyı çizgisinin kayma riski de böylece ortadan kalkar.
    """
    h, w = grid.shape
    a = grid[:-1, :-1]     # (y,   x)   sol-alt
    b = grid[:-1, 1:]      # (y,   x+1) sag-alt
    c = grid[1:, 1:]       # (y+1, x+1) sag-ust
    d = grid[1:, :-1]      # (y+1, x)   sol-ust

    case = ((a > level).astype(np.uint8) |
            ((b > level).astype(np.uint8) << 1) |
            ((c > level).astype(np.uint8) << 2) |
            ((d > level).astype(np.uint8) << 3))

    def lerp(v0, v1):
        denom = np.where(np.abs(v1 - v0) < 1e-12, 1e-12, v1 - v0)
        return np.clip((level - v0) / denom, 0.0, 1.0)

    ys, xs = np.mgrid[0:h - 1, 0:w - 1]
    # Hucre kenarlarindaki kesim noktalari
    bottom = (xs + lerp(a, b), ys.astype(np.float64))
    right = (xs + 1.0, ys + lerp(b, c))
    top = (xs + lerp(d, c), ys + 1.0)
    left = (xs.astype(np.float64), ys + lerp(a, d))

    edges = {"B": bottom, "R": right, "T": top, "L": left}
    # 16 durum -> baglanacak kenar ciftleri. 5 ve 10 belirsiz (saddle): iki segment.
    table = {
        1: [("L", "B")], 2: [("B", "R")], 3: [("L", "R")],
        4: [("R", "T")], 5: [("L", "T"), ("B", "R")], 6: [("B", "T")],
        7: [("L", "T")], 8: [("T", "L")], 9: [("T", "B")],
        10: [("T", "R"), ("L", "B")], 11: [("T", "R")],
        12: [("R", "L")], 13: [("R", "B")], 14: [("B", "L")],
    }

    segments = []
    for code, pairs in table.items():
        mask = case == code
        if not mask.any():
            continue
        for e0, e1 in pairs:
            x0 = edges[e0][0][mask]; y0 = edges[e0][1][mask]
            x1 = edges[e1][0][mask]; y1 = edges[e1][1][mask]
            segments.append(np.stack([x0, y0, x1, y1], axis=1))

    if not segments:
        return np.empty((0, 4))
    return np.concatenate(segments, axis=0)


def zone_grid_box(bbox, meta):
    """WGS84 bbox → ızgara koordinatında (x0, y0, x1, y1)."""
    w, s, e, n = bbox
    xs, ys = warp_transform("EPSG:4326", meta["crs"], [w, e, w, e], [s, s, n, n])
    pts = utm_to_grid(list(zip(xs, ys)), meta)
    gx = [p[0] for p in pts]
    gy = [p[1] for p in pts]
    return min(gx), min(gy), max(gx), max(gy)


def build_threshold_field(meta, zones):
    """
    Hücre başına "deniz sayılan irtifa" alanı.

    Taban her yerde 0,5 m'dir. Dolgu alanlarının içinde 5,0 m'ye çıkar ve alan
    sınırından itibaren `ZONE_BLEND_M` boyunca tabana geri döner. Yumuşak geçiş
    şart: sert bir basamak, kıyı çizgisinde alan sınırını izleyen dümdüz yapay
    bir sıçrama bırakırdı.
    """
    n = meta["resolution"]
    cell = meta["meters_per_sample_x"]
    blend_cells = max(1.0, ZONE_BLEND_M / cell)

    ys, xs = np.mgrid[0:n, 0:n].astype(np.float64)
    field = np.full((n, n), SEA_LEVEL_CONTOUR, dtype=np.float64)

    for zone in zones:
        threshold = zone.get("sea_threshold_m")
        if not threshold or threshold <= SEA_LEVEL_CONTOUR:
            continue

        x0, y0, x1, y1 = zone_grid_box(zone["bbox"], meta)
        # Eksen hizali kutuya uzaklik (icerde 0)
        dx = np.maximum(np.maximum(x0 - xs, xs - x1), 0.0)
        dy = np.maximum(np.maximum(y0 - ys, ys - y1), 0.0)
        dist = np.hypot(dx, dy)

        weight = np.clip(1.0 - dist / blend_cells, 0.0, 1.0)
        field = np.maximum(field, SEA_LEVEL_CONTOUR +
                           (threshold - SEA_LEVEL_CONTOUR) * weight)

    # KORUMA PASI — maksimum almanin ardindan calisir.
    #
    # Esigi acikca None olan alanlar (Langa) su basmamalidir. Ama alanlar ic ice
    # gecebilir: Langa, Marmara kiyisi alaninin TAMAMEN icindedir. Yalnizca
    # maksimum alinsaydi, Marmara'nin 5 m esigi Langa'nin muafiyetini ezer ve
    # 1632'de bostan olan dolmus limani yeniden denize cevirirdik. (Bu tam olarak
    # yasandi ve onizlemede Langa kutusunun icinde kapali bir su halkasi olarak
    # gorundu.) Bu yuzden muafiyet, birlestirmeden SONRA geri yazilir.
    for zone in zones:
        if zone.get("sea_threshold_m"):
            continue

        x0, y0, x1, y1 = zone_grid_box(zone["bbox"], meta)
        dx = np.maximum(np.maximum(x0 - xs, xs - x1), 0.0)
        dy = np.maximum(np.maximum(y0 - ys, ys - y1), 0.0)
        protect = np.clip(1.0 - np.hypot(dx, dy) / blend_cells, 0.0, 1.0)
        field = field * (1.0 - protect) + SEA_LEVEL_CONTOUR * protect

    return field


def flood_fill_sea(low, seed_xy):
    """
    Tohum noktasından bağlantılı **deniz** kütlesini bulur (tarama-satırı doldurma).

    Neden gerekli: eş yükselti eğrisi "deniz"i değil "alçak her yeri" izler. İlk
    denemede kıyı çizgisi Haliç'in başından kuzeye, Kâğıthane deresi vadisi boyunca
    kilometrelerce içeri uzadı; ayrıca denizle hiç bağlantısı olmayan alçak
    düzlükler de kıyı sanıldı. Doldurma, "buraya sudan yüzerek gidilebilir mi"
    sorusunu sorar — kıyı çizgisinin tanımı budur.

    Haliç'in Kâğıthane/Alibeyköy haliçlerine doğru devam etmesi kusur DEĞİLDİR:
    1632'de orası gerçekten sulak ve kayıkla çıkılabilir bir haliçtir
    (RESEARCH.md §4: Kâğıthane mesiresi).
    """
    h, w = low.shape
    sea = np.zeros_like(low, dtype=bool)
    sx, sy = seed_xy
    if not low[sy, sx]:
        raise SystemExit(f"[HZ] HATA: deniz tohumu ({sx},{sy}) suda degil.")

    stack = [(sx, sy)]
    while stack:
        x, y = stack.pop()
        if sea[y, x] or not low[y, x]:
            continue

        x0 = x
        while x0 > 0 and low[y, x0 - 1] and not sea[y, x0 - 1]:
            x0 -= 1
        x1 = x
        while x1 < w - 1 and low[y, x1 + 1] and not sea[y, x1 + 1]:
            x1 += 1
        sea[y, x0:x1 + 1] = True

        for ny in (y - 1, y + 1):
            if not (0 <= ny < h):
                continue
            row = low[ny, x0:x1 + 1] & ~sea[ny, x0:x1 + 1]
            idx = np.flatnonzero(row)
            if idx.size == 0:
                continue
            breaks = np.flatnonzero(np.diff(idx) > 1)
            starts = np.concatenate(([idx[0]], idx[breaks + 1]))
            for s in starts:
                stack.append((x0 + int(s), ny))

    return sea


def chain_segments(segments, snap=1e-6):
    """Segmentleri uç uca ekleyerek polilinelere çevirir."""
    from collections import defaultdict

    def key(x, y):
        return (round(x / snap), round(y / snap))

    adjacency = defaultdict(list)
    for i, (x0, y0, x1, y1) in enumerate(segments):
        adjacency[key(x0, y0)].append((i, 0))
        adjacency[key(x1, y1)].append((i, 1))

    used = np.zeros(len(segments), dtype=bool)
    lines = []

    for start_idx in range(len(segments)):
        if used[start_idx]:
            continue
        used[start_idx] = True
        x0, y0, x1, y1 = segments[start_idx]
        line = [(x0, y0), (x1, y1)]

        # Iki yone de buyut
        for _ in range(2):
            while True:
                px, py = line[-1]
                nxt = None
                for idx, end in adjacency.get(key(px, py), ()):
                    if used[idx]:
                        continue
                    sx0, sy0, sx1, sy1 = segments[idx]
                    nxt = (idx, (sx1, sy1) if end == 0 else (sx0, sy0))
                    break
                if nxt is None:
                    break
                used[nxt[0]] = True
                line.append(nxt[1])
            line.reverse()

        lines.append(line)

    return lines


def douglas_peucker(points, tolerance):
    """Poliline sadeleştirme. Kıyı çizgisi 2049² ızgaradan çok yoğun çıkar."""
    if len(points) < 3:
        return points

    pts = np.asarray(points, dtype=np.float64)
    keep = np.zeros(len(pts), dtype=bool)
    keep[0] = keep[-1] = True
    stack = [(0, len(pts) - 1)]

    while stack:
        i0, i1 = stack.pop()
        if i1 <= i0 + 1:
            continue
        p0, p1 = pts[i0], pts[i1]
        seg = p1 - p0
        length = math.hypot(seg[0], seg[1])

        chunk = pts[i0 + 1:i1]
        if length < 1e-12:
            dist = np.hypot(chunk[:, 0] - p0[0], chunk[:, 1] - p0[1])
        else:
            dist = np.abs(seg[0] * (p0[1] - chunk[:, 1]) -
                          (p0[0] - chunk[:, 0]) * seg[1]) / length

        j = int(np.argmax(dist))
        if dist[j] > tolerance:
            split = i0 + 1 + j
            keep[split] = True
            stack.append((i0, split))
            stack.append((split, i1))

    return [tuple(p) for p in pts[keep]]


# ----------------------------------------------------------------------- ana

def load_dem(dir_path):
    with open(os.path.join(dir_path, "dem_meta.json"), encoding="utf-8") as fh:
        meta = json.load(fh)
    n = meta["resolution"]
    raw = np.fromfile(os.path.join(dir_path, meta["heightmap_file"]), dtype="<u2")
    # Satir 0 = guney; marching squares icin alt-sol orijin tam da bu.
    grid = raw.reshape(n, n).astype(np.float64)
    heights = meta["base_elevation_m"] + (grid / 65535.0) * meta["height_range_m"]
    return meta, heights


def grid_to_utm(points, meta):
    minx, miny, maxx, maxy = meta["bounds_utm"]
    n = meta["resolution"]
    sx = (maxx - minx) / (n - 1)
    sy = (maxy - miny) / (n - 1)
    return [(minx + px * sx, miny + py * sy) for px, py in points]


def utm_to_grid(points, meta):
    minx, miny, maxx, maxy = meta["bounds_utm"]
    n = meta["resolution"]
    sx = (maxx - minx) / (n - 1)
    sy = (maxy - miny) / (n - 1)
    return [((x - minx) / sx, (y - miny) / sy) for x, y in points]


def ring_length_m(utm_points):
    return sum(math.dist(utm_points[i], utm_points[i + 1])
               for i in range(len(utm_points) - 1))


def draw_overlay(heights, meta, modern_lines, lines_1632, zone_boxes, out_path):
    """
    Kıyı çizgisini tepe gölgesinin üzerine bindirir.

    Gerekçe: kontur sayıları ("65,9 km") doğru görünebilir ama çizgi yanlış yerde
    olabilir. Bindirme, kıyının araziyle örtüşüp örtüşmediğini tek bakışta
    gösterir — ve bu boru hattında kabul kararı gözle değil ölçümle verilir demek,
    gözle bakmayı BIRAKMAK anlamına gelmez.
    """
    n = meta["resolution"]
    shade = hillshade(heights, meta["meters_per_sample_x"], exaggeration=3.0)
    # DEM dizisinde satir 0 = GUNEY. Asagidaki `plot` goruntu duzeni icin
    # `n-1-yi` kullanir, yani satir 0 = KUZEY varsayar. Taban rasteri
    # cevrilmezse arazi ile cizgiler DIKEYDE TERS oturur.
    img = np.flipud(shade * 200.0 + 30.0)

    def plot(px, py, value):
        xi, yi = int(round(px)), int(round(py))
        if 0 <= xi < n and 0 <= yi < n:
            img[n - 1 - yi, xi] = value      # satir 0 = kuzey (goruntu duzeni)

    def line(p0, p1, value):
        steps = int(max(abs(p1[0] - p0[0]), abs(p1[1] - p0[1]))) + 1
        for t in np.linspace(0.0, 1.0, steps * 2 + 2):
            plot(p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t, value)

    for pts in modern_lines:
        for i in range(len(pts) - 1):
            line(pts[i], pts[i + 1], 120)     # bugunku kiyi: soluk gri

    for pts in lines_1632:
        for i in range(len(pts) - 1):
            line(pts[i], pts[i + 1], 255)     # 1632 kiyisi: parlak beyaz

    for box in zone_boxes:
        for i in range(len(box) - 1):
            line(box[i], box[i + 1], 0)       # duzeltme alani: siyah

    write_png(out_path, np.clip(img, 0, 255).astype(np.uint8))


def main():
    p = argparse.ArgumentParser(description="1632 kiyi cizgisi taslagi")
    p.add_argument("--dir", default="data/gis/istanbul")
    p.add_argument("--geojson", default="refs/maps/coastline_1632.geojson")
    p.add_argument("--tolerance", type=float, default=SIMPLIFY_TOLERANCE_M)
    p.add_argument("--min-ring", type=float, default=MIN_RING_METRES)
    args = p.parse_args()

    meta, heights = load_dem(args.dir)
    log(f"DEM {meta['resolution']}^2 @ {meta['meters_per_sample_x']:.2f} m")

    # Deniz tohumu: Bogaz ortasi — dem_probe.py orada 0,0 m olctu.
    sx, sy = warp_transform("EPSG:4326", meta["crs"], [SEA_SEED_LON], [SEA_SEED_LAT])
    seed = utm_to_grid([(sx[0], sy[0])], meta)[0]
    seed_xy = (int(round(seed[0])), int(round(seed[1])))

    # --- iki deniz maskesi: bugun ve 1632 ---
    sea_modern = flood_fill_sea(heights <= SEA_LEVEL_CONTOUR, seed_xy)
    threshold_field = build_threshold_field(meta, CORRECTION_ZONES)
    sea_1632 = flood_fill_sea(heights <= threshold_field, seed_xy)

    log(f"deniz kutlesi: bugun %{sea_modern.sum() / sea_modern.size * 100:.1f} -> "
        f"1632 %{sea_1632.sum() / sea_1632.size * 100:.1f}")

    features = []
    overlay_modern = []
    overlay_1632 = []
    overlay_boxes = []

    layers = [
        ("modern_shoreline", sea_modern, overlay_modern,
         "Copernicus GLO-30 DEM'in 0,5 m konturu. BUGUNKU kiyi — kiyas icin."),
        ("shoreline_1632", sea_1632, overlay_1632,
         f"1632 taslagi: dolgu alanlarinda deniz esigi {FILL_SEA_THRESHOLD_M:.1f} m'ye "
         f"cikarilarak kiyi dogal yamacin etegine cekildi. T2 — REKONSTRUKSIYON."),
    ]

    for layer_name, mask, overlay, note in layers:
        segments = marching_squares((~mask).astype(np.float64), 0.5)
        lines = chain_segments(segments)

        kept = 0
        total_len = 0.0
        for line in lines:
            utm = grid_to_utm(line, meta)
            length = ring_length_m(utm)
            if length < args.min_ring:
                continue

            simplified = douglas_peucker(utm, args.tolerance)
            xs = [pt[0] for pt in simplified]
            ys = [pt[1] for pt in simplified]
            lons, lats = warp_transform(meta["crs"], "EPSG:4326", xs, ys)

            kept += 1
            total_len += length
            overlay.append(utm_to_grid(simplified, meta))
            features.append({
                "type": "Feature",
                "geometry": {
                    "type": "LineString",
                    "coordinates": [[round(lo, 7), round(la, 7)]
                                    for lo, la in zip(lons, lats)],
                },
                "properties": {
                    "layer": layer_name,
                    "tier": "Reconstruction",
                    "length_m": round(length, 1),
                    "vertices": len(simplified),
                    "note": note,
                },
            })

        log(f"{layer_name}: {kept} halka, {total_len / 1000:.1f} km")

    # --- Olculen ic kayma: "makul tahmin"in ne kadar oldugunu SAYIYLA soyle ---
    cell_area = meta["meters_per_sample_x"] * meta["meters_per_sample_z"]
    gained = sea_1632 & ~sea_modern
    for zone in CORRECTION_ZONES:
        if not zone.get("sea_threshold_m"):
            zone["measured_shift_m"] = 0.0
            continue

        x0, y0, x1, y1 = zone_grid_box(zone["bbox"], meta)
        xi0, yi0 = max(0, int(x0)), max(0, int(y0))
        xi1, yi1 = min(meta["resolution"], int(x1) + 1), min(meta["resolution"], int(y1) + 1)

        area = gained[yi0:yi1, xi0:xi1].sum() * cell_area
        # Alan icindeki BUGUNKU kiyi uzunlugu
        shore_len = 0.0
        for pts in overlay_modern:
            for i in range(len(pts) - 1):
                ax, ay = pts[i]
                bx, by = pts[i + 1]
                if (x0 <= ax <= x1 and y0 <= ay <= y1) or (x0 <= bx <= x1 and y0 <= by <= y1):
                    shore_len += math.dist(pts[i], pts[i + 1]) * meta["meters_per_sample_x"]

        shift = area / shore_len if shore_len > 1.0 else 0.0
        zone["measured_shift_m"] = round(shift, 1)
        zone["measured_area_ha"] = round(area / 1e4, 1)
        log(f"  {zone['id']}: {area/1e4:.1f} ha geri alindi, "
            f"kiyi ~{shift:.0f} m iceri cekildi")

    for zone in CORRECTION_ZONES:
        w, s, e, n = zone["bbox"]
        bx, by = warp_transform("EPSG:4326", meta["crs"],
                                [w, e, e, w, w], [s, s, n, n, s])
        overlay_boxes.append(utm_to_grid(list(zip(bx, by)), meta))
        features.append({
            "type": "Feature",
            "geometry": {
                "type": "Polygon",
                "coordinates": [[[w, s], [e, s], [e, n], [w, n], [w, s]]],
            },
            "properties": {
                "layer": "correction_zone",
                "id": zone["id"],
                "name": zone["name"],
                "action": zone["action"],
                "tier": zone["tier"],
                "status": "draft",
                "sea_threshold_m": zone.get("sea_threshold_m"),
                "measured_shift_m": zone.get("measured_shift_m", 0.0),
                "measured_area_ha": zone.get("measured_area_ha", 0.0),
                "research_note": zone["research_note"],
                "design_note": zone["design_note"],
                "confidence": zone["confidence"],
            },
        })

    collection = {
        "type": "FeatureCollection",
        "name": "coastline_1632",
        "metadata": {
            "title": "İstanbul kıyı çizgisi — 1632 taslağı",
            "status": "TASLAK — Caner onayı bekliyor",
            "generated_utc": meta["generated_utc"],
            "base_geometry": "Copernicus DEM GLO-30, 0,5 m konturu (modern kıyı)",
            "base_licence": meta["licence"],
            "corrections_source": "docs/RESEARCH.md §4 (niteliksel; metrik ofset içermez)",
            "corrections_method": (
                f"Dolgu alanlarında deniz eşiği {SEA_LEVEL_CONTOUR} m → "
                f"{FILL_SEA_THRESHOLD_M} m çıkarıldı; kıyı doğal yamacın eteğine çekildi. "
                f"Alan sınırında {ZONE_BLEND_M:.0f} m yumuşak geçiş. Sabit metre ofseti "
                "KULLANILMADI — eşik araziden türetilir, kayma miktarı ölçülür."),
            "decision": ("Caner, 2026-08-18: 'makul bir tahminle geri alabilirsin.' "
                         "Bkz. docs/feedback/coastline_1632.md"),
            "copyright": "Bu çizim bize aittir (kendi türetimimiz) — plan Faz 1 madde 2.",
            "world_origin": meta["world_origin"],
            "crs_note": "GeoJSON WGS84'tür (RFC 7946). Unity için yerel metre dosyası ayrıca üretilir.",
            "warning": ("correction_zone geometrileri KABA TASLAKTIR. RESEARCH.md yalnızca "
                        "'dolgular yok / kıyı içeride / Langa bostan' der; metrik ofset vermez. "
                        "Kesinleştirme: dönem haritalarının georeferanslanması (Faz 1 madde 3)."),
        },
        "features": features,
    }

    geo_path = os.path.abspath(args.geojson)
    os.makedirs(os.path.dirname(geo_path), exist_ok=True)
    with open(geo_path, "w", encoding="utf-8") as fh:
        json.dump(collection, fh, indent=1, ensure_ascii=False)
    log(f"wrote {args.geojson} ({os.path.getsize(geo_path) // 1024} KB, {len(features)} ozellik)")

    # --- Unity icin yerel metre surumu (turetilmis; depoya girmez) ---
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]
    local = {"world_origin": meta["world_origin"], "features": []}

    for feat in features:
        props = feat["properties"]
        geom = feat["geometry"]
        rings = ([geom["coordinates"]] if geom["type"] == "LineString"
                 else geom["coordinates"])
        out_rings = []
        for ring in rings:
            lons = [pt[0] for pt in ring]
            lats = [pt[1] for pt in ring]
            xs, ys = warp_transform("EPSG:4326", meta["crs"], lons, lats)
            out_rings.append([{"x": round(x - gx, 3), "z": round(y - gz, 3)}
                              for x, y in zip(xs, ys)])
        local["features"].append({
            "layer": props["layer"],
            "id": props.get("id", ""),
            "name": props.get("name", ""),
            "tier": props["tier"],
            "action": props.get("action", ""),
            "note": props.get("note", props.get("research_note", "")),
            "closed": geom["type"] == "Polygon",
            "rings": out_rings,
        })

    local_path = os.path.join(os.path.abspath(args.dir), "coastline_1632_local.json")
    with open(local_path, "w", encoding="utf-8") as fh:
        json.dump(local, fh, ensure_ascii=False)
    log(f"wrote coastline_1632_local.json ({os.path.getsize(local_path) // 1024} KB)")

    draw_overlay(heights, meta, overlay_modern, overlay_1632, overlay_boxes,
                 os.path.join(os.path.abspath(args.dir), "preview_coastline.png"))
    log("coastline_build OK")


if __name__ == "__main__":
    main()
