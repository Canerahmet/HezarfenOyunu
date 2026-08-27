"""
Hezarfen: 1632 — Sur hatları (plan Faz 1 madde 3).

`refs/maps/walls_1632.geojson` üretir. Dört hat + kapılar:

  wall_land          Kara surları (Theodosius) — BUGÜN AYAKTA, elle izlendi
  wall_sea_marmara   Marmara deniz surları     — kendi 1632 kıyımızdan TÜRETİLDİ
  wall_sea_halic     Haliç deniz surları       — kendi 1632 kıyımızdan TÜRETİLDİ
  wall_galata        Galata surları            — KABA TASLAK, Caner onayı bekliyor

## Neden üç ayrı üretim yöntemi

Bunlar üç farklı kanıt sınıfıdır ve tek bir yöntemle üretmek, en zayıf halkanın
güvenini en güçlüsüne yamamak olurdu:

* **Kara surları** bugün fiziksel olarak duruyor. Elle izlenen çizgi ~100 m
  mertebesinde yaklaşıktır ama *bir şeyin* izidir. Uzunluk denetimi bunu ölçer:
  Yedikule–Ayvansaray arası yaygın olarak ~5,7 km verilir; ürettiğimiz çizgi bu
  aralığın dışına düşerse üretim durur.
* **Deniz surları** tanımı gereği kıyıyı izler. O yüzden elle nokta uydurmak
  yerine, her elle girilen çapa noktası **kendi `shoreline_1632` çizgimize
  yapıştırılır** ve karaya doğru sabit bir kadar içeri alınır. Böylece sur,
  kıyı çizgisi düzeldikçe onunla birlikte düzelir; iki ayrı taslak birbirinden
  bağımsız sürüklenmez. Yapıştırma mesafesi RAPORLANIR — elle izim ne kadar
  tuttuğunun ölçüsü odur.
* **Galata surları** 1860'larda yıkıldı ve elimizde georeferanslı dönem planı
  YOK. Bu yüzden metrik çizgi uydurulmaz: kaba bir çevre poligonu üretilir,
  `status: draft` ve T2 ile işaretlenir, çevre/alan ölçülüp Caner'e sorulur
  (CLAUDE.md: *"Kaynak niteliksel olduğunda metrik geometri UYDURMA"*).

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/walls_build.py --dir data/gis/istanbul
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

# `geodesy`, rasterio/`coastline_build` yerine: bu makinede rasterio'nun DLL'i
# Windows uygulama denetimince bloklu ve o iki bagimlilik bu betigi de
# calistirilamaz kiliyordu. `geodesy` ayni dort isi (ileri/geri UTM, DEM
# okuma, izgara donusumu) bagimsiz yapar ve dogrulugunu boru hattinin kendi
# kaydina karsi sinar. Ayrinti: geodesy.py basligi, SETUP.md.
from geodesy import (from_utm35n, load_dem, sample_utm,  # noqa: E402
                     self_check, to_utm)

# Konum guveni etiketleri
TRACED = ("traced (~100 m; yapi BUGUN AYAKTA, cizgi genel cografyadan elle "
          "girildi — georeferansli plandan DEGIL)")
DERIVED = ("derived (kendi shoreline_1632 cizgimize yapistirildi + karaya "
           "dogru sabit ofset; kiyi taslak oldugu icin bu da taslak)")
COARSE = ("coarse draft (yikilmis yapi, georeferansli donem plani YOK — "
          "kaba cevre poligonu; Caner onayi bekliyor)")

# --------------------------------------------------------------- elle izim
#
# Kara surlari: Marmara (Mermerkule) -> Halic (Ayvansaray), guneyden kuzeye.
# Kapi adlari RESEARCH.md §3 "Surlar ve kapilar" ile uyumlu.
LAND_WALL = [
    # GUNEY UCU OLCULEREK DUZELTILDI (2026-08-27, ADR 0050).
    #
    # Elle izlenen Yedikule noktasi (28,9214 / 40,9930) hisarin kendisinden
    # **152 m** uzaktaydi. Hangisinin dogru oldugu tahminle degil OLCUMLE
    # secildi: haritada "Yedikule Zindanlari" (historic=castle) 28,923209 /
    # 40,993040'ta ve "Yedi Kule Hisari Muzesi" onun 60 m yaninda; katalogdaki
    # LM_Yedikule ikisine **15-74 m** uzakta, sur hattina ise 152-222 m.
    # Yani landmark dogru, HAT yanlisti.
    #
    # Mermerkule de ayni izim hatasini paylasiyor ve ayni kadar dogu-guneye
    # kaydirildi; onun icin bagimsiz bir olcum YOK, duzeltme TURETILMISTIR.
    (28.9221, 40.9906),   # Mermerkule — Marmara ucu (turetilmis duzeltme)
    (28.923209, 40.993040),  # Yedikule — OLCULU (historic=castle)
    (28.9224, 40.9977),   # Belgradkapi
    (28.9243, 41.0037),   # Silivrikapi
    (28.9280, 41.0092),   # Mevlanakapi (Mevlevihanekapisi)
    (28.9300, 41.0143),   # Topkapi — Lykos vadisi, surun en alcak kesimi
    (28.9322, 41.0186),   # Sulukulekapi
    (28.9345, 41.0245),
    (28.9352, 41.0292),   # Edirnekapi — surun en yuksek noktasi (6. tepe)
    (28.9375, 41.0340),   # Egrikapi — Blakhernai donusu
    (28.9440, 41.0392),   # Ayvansaray — Halic ucu
]

# Deniz surlari: elle girilen CAPA noktalari. Bunlar nihai geometri DEGIL;
# asagida shoreline_1632'ye yapistirilir. Amaclari sadece "hangi kiyi seridi".
MARMARA_ANCHORS = [
    (28.9203, 40.9909),   # kara suruyla birlesme
    (28.9245, 40.9930),   # Narlikapi
    (28.9310, 40.9965),   # Samatya (Psamathia)
    (28.9400, 40.9990),   # Davutpasa
    (28.9490, 41.0005),
    (28.9560, 41.0010),   # Langa / Yenikapi — bostan kiyisi
    (28.9640, 41.0018),
    (28.9700, 41.0022),   # Kumkapi
    (28.9780, 41.0022),   # Catladikapi
    (28.9845, 41.0035),   # Ahirkapi
    (28.9880, 41.0080),
    (28.9895, 41.0130),   # Sarayburnu
]

HALIC_ANCHORS = [
    (28.9895, 41.0130),   # Sarayburnu
    (28.9840, 41.0155),   # Yalikosku / Sepetciler
    (28.9760, 41.0172),   # Bahcekapi
    (28.9700, 41.0180),   # Cifit Kapisi (Yenicami harabesinin yaninda)
    (28.9660, 41.0187),   # Odunkapi
    (28.9610, 41.0227),   # Ayazmakapi / Unkapani
    (28.9560, 41.0283),   # Cibali
    (28.9505, 41.0316),   # Fener
    (28.9487, 41.0342),   # Balat
    (28.9452, 41.0392),   # Ayvansaray — kara suruyla birlesme
]

# Galata suru: YELPAZE, tepesi KULE.
#
# Bicim artik bize ait degil, KAYNAGA ait. Erdogan (2013) rolovesi
# (Marie de Launay'in 1864 yikim oncesi kaydini aktararak) sunu yaziyor:
#
#   "Galata Surlari, sur ici yerlesim alanini KUZEYDE GALATA KULESI MERKEZ
#    OLMAK UZERE guneybati ve guneydogu yonlerinde iki noktaya dogru
#    acilarak bir YELPAZE bicminde kusatmistir."
#   "...batida Azap Kapi, kuzeyde Galata Kulesi ve kuzeydoguda bugunku
#    Tophane'ye kadar uzanmistir."
#
# Onceki halka bu iki cumleyle CELISIYORDU: kuleyi 80 m guneyde birakip
# ONU ICINE ALIYORDU. Oysa kule yelpazenin TEPE NOKTASIDIR, yani surun
# UZERINDE. Kuzey kenari artik kulenin tam koordinatindan geciyor.
#
# Olcu capalari ayni ve ayni kaynaktan: Launay'in mahalle mahalle olctugu
# alanlarin toplami 369.137 m2 = ~37 ha, ve ic+dis sur hatti ~2800 m.
#
# Guzergahin ARA noktalari hala bizim: sokak sokak hat cizmek OSM
# geometrisi ister (ODbL, oyun ici atif zorunlu) ve modern sokagi 1632
# suruyla ozdeslestirmek ayri bir iddiadir. Bicim ve uc noktalar kaynaga,
# ara noktalarin kivrimi bize ait.
GALATA_TARGET_HA = 37.0
GALATA_TARGET_PERIM_M = 2800.0

#: Kule tam olarak burada (ADR 0007 dunya orijini). Halkanin bir KOSESIDIR.
GALATA_TOWER_LONLAT = (28.974017, 41.025637)

#: KARA kolu: batidan doguya. Deniz kolu bu listede YOK — o, 1632 kiyi
#: cizgisinden gelir (bkz. `shore_arc`).
GALATA_LAND_ARM = [
    (28.9705, 41.0247),          # Azapkapi — BATI ucu (belgeli)
    (28.9700, 41.0252),          # bati kolu, Sishane / Petits Champs yonu
    GALATA_TOWER_LONLAT,         # GALATA KULESI — yelpazenin TEPESI (belgeli)
    (28.9793, 41.0252),          # dogu kolu, kuleden Tophane'ye inis
    (28.9822, 41.0242),          # Tophane — KUZEYDOGU ucu (belgeli)
]

# Kapilar. `on_land_wall=True` olanlar bugun ayakta/yeri kesin; digerleri
# sur hattina en yakin noktaya oturtulur ve YAKLASIKTIR.
GATES = [
    dict(id="GT_Yedikule",   name="Yedikule",       lon=28.923209, lat=40.993040, wall="wall_land"),
    dict(id="GT_Belgradkapi", name="Belgradkapı",   lon=28.9224, lat=40.9977, wall="wall_land"),
    dict(id="GT_Silivrikapi", name="Silivrikapı",   lon=28.9243, lat=41.0037, wall="wall_land"),
    dict(id="GT_Mevlanakapi", name="Mevlanakapı",   lon=28.9280, lat=41.0092, wall="wall_land"),
    dict(id="GT_Topkapi",     name="Topkapı",       lon=28.9300, lat=41.0143, wall="wall_land"),
    dict(id="GT_Edirnekapi",  name="Edirnekapı",    lon=28.9352, lat=41.0292, wall="wall_land"),
    dict(id="GT_Egrikapi",    name="Eğrikapı",      lon=28.9375, lat=41.0340, wall="wall_land"),

    dict(id="GT_Narlikapi",   name="Narlıkapı",     lon=28.9245, lat=40.9930, wall="wall_sea_marmara"),
    dict(id="GT_Samatya",     name="Samatya Kapısı", lon=28.9310, lat=40.9965, wall="wall_sea_marmara"),
    dict(id="GT_Davutpasa",   name="Davutpaşa Kapısı", lon=28.9400, lat=40.9990, wall="wall_sea_marmara"),
    dict(id="GT_Langa",       name="Langa Kapısı",  lon=28.9560, lat=41.0010, wall="wall_sea_marmara"),
    dict(id="GT_Kumkapi",     name="Kumkapı",       lon=28.9700, lat=41.0022, wall="wall_sea_marmara"),
    dict(id="GT_Catladikapi", name="Çatladıkapı",   lon=28.9780, lat=41.0022, wall="wall_sea_marmara"),
    dict(id="GT_Ahirkapi",    name="Ahırkapı",      lon=28.9845, lat=41.0035, wall="wall_sea_marmara"),

    dict(id="GT_Bahcekapi",   name="Bahçekapı",     lon=28.9760, lat=41.0175, wall="wall_sea_halic"),
    dict(id="GT_CifitKapisi", name="Çıfıt Kapısı (Yenicami)", lon=28.9700, lat=41.0180, wall="wall_sea_halic"),
    dict(id="GT_Odunkapi",    name="Odunkapı",      lon=28.9640, lat=41.0192, wall="wall_sea_halic"),
    dict(id="GT_Ayazmakapi",  name="Ayazmakapı (Unkapanı)", lon=28.9580, lat=41.0205, wall="wall_sea_halic"),
    dict(id="GT_Cibali",      name="Cibali Kapısı", lon=28.9560, lat=41.0283, wall="wall_sea_halic"),
    dict(id="GT_Fener",       name="Fener Kapısı",  lon=28.9505, lat=41.0316, wall="wall_sea_halic"),
    dict(id="GT_Balat",       name="Balat Kapısı",  lon=28.9487, lat=41.0342, wall="wall_sea_halic"),

    dict(id="GT_Azapkapi",    name="Azapkapı (Galata)", lon=28.9705, lat=41.0247, wall="wall_galata"),
    dict(id="GT_KuleKapisi",  name="Kule Kapısı (Galata)", lon=28.9760, lat=41.0215, wall="wall_galata"),
]

# Yaygin olarak verilen uzunluklar — DENETIM esigi olarak kullanilir, iddia degil.
# Ciziglerimiz basitlestirilmistir (kucuk koylar, burc cikintilari yok), bu yuzden
# OLCTUGUMUZ deger bu araligin ALTINDA kalabilir; ustune cikarsa izim bozuktur.
LENGTH_CHECKS = {
    "wall_land":        dict(cited_km=5.7, lo_km=4.8, hi_km=7.0,
                             cite="Yedikule–Ayvansaray arasi kara surlari"),
    "wall_sea_marmara": dict(cited_km=8.5, lo_km=5.0, hi_km=9.5,
                             cite="Marmara deniz surlari (koy girintileri dahil)"),
    "wall_sea_halic":   dict(cited_km=5.5, lo_km=4.0, hi_km=6.5,
                             cite="Halic deniz surlari"),
}


def log(msg):
    print(f"[HZ] {msg}", flush=True)


# ------------------------------------------------------------------ yardimci

def fit_ring_area(ring_utm, target_ha, about=None):
    """
    Halkayı ölçekleyip belgelenmiş alana oturtur.

    Neden ölçekleme: elimizde sınırın *biçimi* var (nereden geçtiği kabaca
    biliniyor) ama *büyüklüğü* yoktu. Kaynak büyüklüğü veriyorsa, biçimi
    koruyup boyu ona uydurmak, ikisini birden uydurmaktan dürüsttür.

    `about` verilirse ölçekleme **o noktaya göre** yapılır ve o nokta
    yerinden kıpırdamaz. Galata'da bu şart: kule yelpazenin tepesidir ve
    halkanın üstünde durmak zorundadır — ağırlık merkezine göre ölçeklemek
    onu belgeli koordinatından kaydırırdı (ölçüldü: 27 m).

    Alan ölçeğin karesiyle gittiği için tek adımda kapanır; yine de ölçüp
    döndürüyoruz — "hesapladım" ile "ölçtüm" aynı şey değil.
    """
    cur = ring_area(ring_utm) / 10000.0
    if cur <= 0:
        raise ValueError("halka alani sifir")
    s = math.sqrt(target_ha / cur)
    if about is None:
        cx = sum(p[0] for p in ring_utm) / len(ring_utm)
        cy = sum(p[1] for p in ring_utm) / len(ring_utm)
    else:
        cx, cy = about
    out = [(cx + (e - cx) * s, cy + (n - cy) * s) for e, n in ring_utm]
    return out, cur, ring_area(out) / 10000.0


def _seg_dist(p, a, b):
    """Noktanın [a,b] parçasına en kısa uzaklığı (m)."""
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    L2 = dx * dx + dy * dy
    t = 0.0 if L2 < 1e-9 else max(0.0, min(1.0, ((p[0] - ax) * dx
                                                 + (p[1] - ay) * dy) / L2))
    return math.dist(p, (ax + t * dx, ay + t * dy))


def polyline_length(pts):
    return sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1))


def ring_area(pts):
    """Kapali halkanin alani (m^2), ayakkabi baglama formulu."""
    a = 0.0
    for i in range(len(pts)):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % len(pts)]
        a += x0 * y1 - x1 * y0
    return abs(a) * 0.5


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


def resample(pts, step_m):
    """Cizgiyi sabit aralikla yeniden ornekler — yapistirma icin daha yogun capa."""
    out = [pts[0]]
    for i in range(len(pts) - 1):
        p0, p1 = pts[i], pts[i + 1]
        d = math.dist(p0, p1)
        steps = max(1, int(d / step_m))
        for s in range(1, steps + 1):
            t = s / steps
            out.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t))
    return out


# ------------------------------------------------------- kiyiya yapistirma

def load_shoreline_utm(dir_path, meta):
    """`coastline_1632_local.json`ten AVRUPA yakasi 1632 kiyi halkasini okur."""
    path = os.path.join(dir_path, "coastline_1632_local.json")
    with open(path, encoding="utf-8") as fh:
        data = json.load(fh)
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]

    best = None
    for feat in data["features"]:
        if feat["layer"] != "shoreline_1632":
            continue
        for ring in feat["rings"]:
            pts = [(p["x"] + gx, p["z"] + gz) for p in ring]
            # Avrupa yakasi: dunya orijininin (Galata) BATISINA uzanan halka.
            if min(p["x"] for p in ring) < -1000.0:
                if best is None or len(pts) > len(best):
                    best = pts
    if best is None:
        raise SystemExit("[HZ] shoreline_1632 Avrupa halkasi bulunamadi — "
                         "once coastline_build.py kostur.")
    return np.array(best, dtype=np.float64)


def nearest_on_polyline(shore, e, n):
    """
    Kıyının en yakın **noktasını** döndürür — en yakın köşeyi değil.

    Fark önemsiz değil: `shoreline_1632` Douglas-Peucker ile sadeleştirilmiştir
    ve köşe aralığı ortanca ~150 m'dir. Köşeye yapıştırmak, hiçbir uyarı vermeden
    ~75 m'ye varan bir hata ekler ve suru kıyının köşelerine basamaklandırır.
    Dönüş: (nokta, mesafe, birim_teget, parca_indisi).
    """
    a = shore[:-1]
    b = shore[1:]
    ab = b - a
    ap = np.array([e, n], dtype=np.float64) - a
    denom = (ab * ab).sum(axis=1)
    denom[denom == 0.0] = 1e-12
    t = np.clip((ap * ab).sum(axis=1) / denom, 0.0, 1.0)
    proj = a + ab * t[:, None]
    d2 = ((proj - np.array([e, n])) ** 2).sum(axis=1)
    i = int(np.argmin(d2))
    tan = ab[i] / (math.hypot(ab[i, 0], ab[i, 1]) or 1.0)
    return (float(proj[i, 0]), float(proj[i, 1])), math.sqrt(float(d2[i])), tan, i



def shore_arc(shore, a, b, meta, heights):
    """
    Kıyı halkasının `a` ile `b` arasındaki **kara tarafı kısa yayı**.

    Galata surunun güney kenarı bizim çizdiğimiz bir çizgi değildir: kaynak
    *"deniz tarafından Haliç ve Boğaz ile çevrelenmiş"* diyor — yani deniz
    kenarı **kıyının kendisidir**. Elle çizilen bir güney kenar ölçüldü ve
    hattın **%44'ü su altında** kalıyordu; sebep yerleştirme değil MODELDİ.

    İki yön arasından, üstünde daha çok KARA olan yay seçilir; halkanın hangi
    yöne dizildiği varsayılmaz.
    """
    ia = min(range(len(shore)), key=lambda i: math.dist(shore[i], a))
    ib = min(range(len(shore)), key=lambda i: math.dist(shore[i], b))
    n = len(shore)
    fwd = [shore[(ia + k) % n] for k in range(((ib - ia) % n) + 1)]
    bwd = [shore[(ia - k) % n] for k in range(((ia - ib) % n) + 1)]

    def land_score(arc):
        if len(arc) < 2:
            return -1e9
        hs = [sample_utm(meta, heights, e, nn) or -99.0
              for e, nn in arc[:: max(1, len(arc) // 24)]]
        return sum(1 for h in hs if h > 1.0) / max(1, len(hs))

    return fwd if land_score(fwd) >= land_score(bwd) else bwd


def snap_to_shore(anchors_utm, shore, meta, heights, inset_m, max_snap_m):
    """
    Her capayi kiyinin en yakin NOKTASINA tasir, sonra KARAYA dogru `inset_m` iter.

    Karanin hangi yon oldugu tahmin edilmez: kiyi tegetinin iki normali de
    ornekleneir ve arazisi yuksek olan secilir. Bu, kiyinin donduğu yerlerde
    (Sarayburnu, Ayvansaray) sabit bir "sag taraf kara" varsayimindan daha
    dayaniklidir.
    """
    out, moves, elevs, warns = [], [], [], []
    for (e, n) in anchors_utm:
        (sx, sy), move, tan, _ = nearest_on_polyline(shore, e, n)
        moves.append(move)
        if move > max_snap_m:
            warns.append((e, n, move))

        tx, ty = float(tan[0]), float(tan[1])
        best_pt, best_h = None, -1e9
        for nx, ny in ((-ty, tx), (ty, -tx)):
            cand = (sx + nx * inset_m, sy + ny * inset_m)
            h = sample_utm(meta, heights, cand[0], cand[1])
            if h is not None and h > best_h:
                best_h, best_pt = h, cand
        out.append(best_pt)
        elevs.append(best_h)
    return out, moves, elevs, warns


# ------------------------------------------------------------------- main

def main():
    p = argparse.ArgumentParser(description="1632 sur hatlari")
    p.add_argument("--dir", default="data/gis/istanbul")
    p.add_argument("--geojson", default="refs/maps/walls_1632.geojson")
    p.add_argument("--inset", type=float, default=15.0,
                   help="deniz surunun kiyidan iceri alinma mesafesi (m)")
    p.add_argument("--anchor-step", type=float, default=120.0,
                   help="deniz suru capalarinin yeniden ornekleme araligi (m)")
    p.add_argument("--max-snap", type=float, default=500.0,
                   help="bu mesafeden fazla kayan capa HATA sayilir (m)")
    args = p.parse_args()

    meta, heights = load_dem(args.dir)
    log(f"UTM oz-denetimi: boru hattinin kaydiyla sapma "
        f"{self_check(meta) * 1000:.2f} mm (ters donusum de kapaniyor)")
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]
    shore = load_shoreline_utm(args.dir, meta)
    log(f"kiyi referansi: {len(shore)} nokta (shoreline_1632, Avrupa)")

    lines = {}
    errors = []

    # --- 1. kara surlari: elle izim, DEM ile dogrulanir -------------------
    land = to_utm(LAND_WALL)
    land_elev = [sample_utm(meta, heights, e, n) for e, n in land]
    for (lon, lat), h in zip(LAND_WALL, land_elev):
        if h is None or h <= 1.0:
            errors.append(f"kara suru noktasi ({lon:.4f},{lat:.4f}) suda/disarida "
                          f"(arazi {h})")
    lines["wall_land"] = dict(pts=land, elev=land_elev, closed=False,
                              tier="Documented", conf=TRACED,
                              name="Kara surları (Theodosius)",
                              note="1632'de ayakta ve buyuk olcude bakimli (RESEARCH.md §3 "
                                   "'Surlar ve kapilar'). Cizgi elle izlendi; bugun ayakta "
                                   "olan surun genel guzergahidir, burc-burc dogru DEGILDIR.")

    # --- 2-3. deniz surlari: kendi kiyimiza yapistirilir -------------------
    for key, anchors, label, note in (
        ("wall_sea_marmara", MARMARA_ANCHORS, "Marmara deniz surları",
         "1632'de ayakta (T1). CIZGI kendi taslak shoreline_1632'mizden turetildi (T2): "
         "her capa kiyinin en yakin noktasina yapistirilip karaya dogru itildi."),
        ("wall_sea_halic", HALIC_ANCHORS, "Haliç deniz surları",
         "1632'de ayakta (T1). CIZGI kendi taslak shoreline_1632'mizden turetildi (T2). "
         "Bu kiyi ayrica Eminonu/Unkapani dolgu geri-alma duzeltmesinin icindedir — "
         "kiyi degisirse sur da degisir."),
    ):
        dense = resample(to_utm(anchors), args.anchor_step)
        pts, moves, elevs, warns = snap_to_shore(dense, shore, meta, heights,
                                                 args.inset, args.max_snap)
        log(f"{key}: {len(pts)} nokta, yapistirma kaymasi "
            f"med {np.median(moves):.0f} m / max {max(moves):.0f} m")
        for (e, n, mv) in warns:
            errors.append(f"{key}: capa {mv:.0f} m kaydi (esik {args.max_snap:.0f}) "
                          f"— elle izim bu kesimde bozuk olabilir")
        dry = [h for h in elevs if h is not None and h > 0.5]
        if len(dry) < len(elevs):
            errors.append(f"{key}: {len(elevs) - len(dry)} nokta karaya oturmadi "
                          f"(--inset degerini buyut)")
        lines[key] = dict(pts=pts, elev=elevs, closed=False,
                          tier="Reconstruction", conf=DERIVED, name=label, note=note,
                          snap_median_m=round(float(np.median(moves)), 1),
                          snap_max_m=round(float(max(moves)), 1))

    # --- 4. Galata: kaba taslak ------------------------------------------
    tower_e, tower_n = to_utm([GALATA_TOWER_LONLAT])[0]

    # HALKA IKI PARCADAN KURULUR: kara kolu KAYNAKTAN, deniz kolu KIYIDAN.
    #
    # Guney kenari elle cizmek iki kez basarisiz oldu: once hattin %35'i,
    # kiyiya yapistirma denemesinden sonra %44'u deniz seviyesinin altinda
    # kaldi. Sorun yerlestirme degil MODELDI — kaynak zaten soyluyor:
    # "Deniz tarafindan Halic ve Bogaz ile cevrelenmis". Yani surun deniz
    # kenari KIYININ KENDISIDIR, ayri bir cizgi degil.
    #
    # Kara kolu (Azapkapi -> ... -> Kule -> ... -> Tophane) kaynagin tarif
    # ettigi yelpazedir ve oldugu gibi kalir.
    land_arm = to_utm(GALATA_LAND_ARM)
    gal_raw = land_arm + shore_arc(shore, land_arm[-1], land_arm[0],
                                   meta, heights)[1:-1]

    # ARTIK OLCEKLENMIYOR — ve bu bir GERI ADIM DEGIL, bir olcum.
    #
    # Onceden halka belgeli 37 ha'ya olcekleniyordu, cunku bicim bize aitti
    # ve buyukluk kaynaga. Simdi bicmin iki parcasi da BELGELI: kara kolu
    # kaynagin tarif ettigi yelpaze, deniz kolu kendi 1632 kiyi cizgimiz.
    # Ikisini birden kaynaga uydurmak icin olceklemek, halkayi kiyidan
    # KOPARIRDI — olculdu: 37 ha'ya zorlandiginda deniz kenarinin %44'u
    # su altina giriyordu.
    #
    # Alan artik bir SONUC. Belgeli 37 ha ile arasindaki fark, sur hakkinda
    # degil KENDI KIYI CIZGIMIZ hakkinda bir sey soyluyor ve oyle raporlaniyor.
    gal = gal_raw
    gal_before = gal_after = ring_area(gal) / 10000.0
    log(f"wall_galata: kara kolu KAYNAKTAN + deniz kolu 1632 KIYISINDAN "
        f"-> olcekleme YOK")
    gal_elev = [sample_utm(meta, heights, e, n) for e, n in gal]
    lines["wall_galata"] = dict(pts=gal, elev=gal_elev, closed=True,
                                tier="Reconstruction", conf=COARSE,
                                name="Galata surları (kaba taslak)",
                                note="1632'de ayakta; Azapkapi, Kule Kapisi vb. (RESEARCH.md §3 "
                                     "Galata, Eremya Celebi). 1860'larda yikildi ve elimizde "
                                     "georeferansli donem plani YOK. Poligonun BICIMI kabadir "
                                     f"ama BUYUKLUGU belgelidir: ~{GALATA_TARGET_HA:.0f} ha, "
                                     f"cevre ~{GALATA_TARGET_PERIM_M:.0f} m. Guzergah iddiasi "
                                     "yok — Caner onayi bekliyor.")

    # KULE HATTIN UZERINDE olmali — icinde degil.
    #
    # Onceki denetim "kule poligonun ICINDE mi" diye soruyordu ve gecerdi;
    # ama kaynak kuleyi yelpazenin TEPESI diye tarif ediyor, yani surun
    # uzerinde. Olcu artik hatta olan uzaklik: 5 m'den fazlaysa hat yanlis
    # yere oturmus demektir.
    d_tower = min(_seg_dist((tower_e, tower_n), gal[i], gal[(i + 1) % len(gal)])
                  for i in range(len(gal)))
    if d_tower > 5.0:
        errors.append(f"Galata Kulesi sur hattindan {d_tower:.0f} m uzakta — "
                      "kaynak onu yelpazenin TEPESI olarak tarif ediyor "
                      "(RESEARCH.md 5.2).")
    gal_area_ha = ring_area(gal) / 10000.0
    gal_perim_km = polyline_length(gal + [gal[0]]) / 1000.0
    log(f"wall_galata: cevre {gal_perim_km:.2f} km, kapali alan {gal_area_ha:.0f} ha "
        f"(kule hatta uzakligi {d_tower:.1f} m)")

    # --- birlesim: sur KAPALI bir cevre olmali ----------------------------
    # Kara suru iki ucundan deniz surlarina baglanir. Deniz suru uclari kiyiya
    # YAPISTIRILDIGI icin bu bagin kendiliginden tutmasi beklenemez; tutmazsa
    # sahnede surun iki ucunda acik bir bosluk kalir ve sur "sehri cevrelemez".
    #
    # Uclari kara surunun (elle izlenmis, YAPI BUGUN AYAKTA) terminallerine
    # zorluyoruz — cunku ikisinden daha guvenilir olan odur. Zorlamadan ONCE
    # olculen acikligi metadata'ya yaziyoruz: bu, elle izim ile DEM'den turemis
    # kiyimizin o noktada ne kadar ANLASMADIGININ olcusudur ve gizlenmemelidir.
    junctions = {}
    for label, key, idx in (("kara↔Marmara", "wall_sea_marmara", 0),
                            ("kara↔Haliç", "wall_sea_halic", -1)):
        anchor = lines["wall_land"]["pts"][0 if idx == 0 else -1]
        d = math.dist(anchor, lines[key]["pts"][idx])
        junctions[label] = round(d, 1)
        log(f"birlesim {label}: {d:.0f} m acikti — kara suru ucuna zorlandi")
        if d > 600.0:
            errors.append(f"birlesim {label} {d:.0f} m — bu kadar buyuk bir "
                          f"anlasmazlik zorlanamaz; elle izim ya da kiyi bozuk.")
        lines[key]["pts"][idx] = anchor
        lines[key]["elev"][idx] = sample_utm(meta, heights, anchor[0], anchor[1])

    # --- uzunluk denetimi -------------------------------------------------
    lengths = {}
    for key, chk in LENGTH_CHECKS.items():
        km = polyline_length(lines[key]["pts"]) / 1000.0
        lengths[key] = round(km, 2)
        mark = "OK "
        if km > chk["hi_km"]:
            mark = "HATA"
            errors.append(f"{key} {km:.2f} km — ust esik {chk['hi_km']} km asildi "
                          f"({chk['cite']}); izim bozuk.")
        elif km < chk["lo_km"]:
            mark = "UYARI"
            log(f"UYARI {key} {km:.2f} km, alt esik {chk['lo_km']} km altinda. "
                f"Basitlestirme fazla olabilir.")
        log(f"{mark} {key}: olculen {km:.2f} km / yaygin verilen ~{chk['cited_km']} km "
            f"({chk['cite']})")

    if errors:
        for e in errors:
            log(f"HATA {e}")
        raise SystemExit(f"[HZ] {len(errors)} denetim hatasi. Sur dosyasi YAZILMADI.")
    log("denetim OK")

    # --- cikti ------------------------------------------------------------
    lons_lats = {}
    features, local_features = [], []

    for key, ln in lines.items():
        coords = [[round(a, 6), round(b, 6)]
                  for a, b in (from_utm35n(p[0], p[1]) for p in ln["pts"])]
        lons_lats[key] = coords
        if ln["closed"]:
            geom = {"type": "Polygon", "coordinates": [coords + [coords[0]]]}
        else:
            geom = {"type": "LineString", "coordinates": coords}

        props = {
            "layer": key, "id": key, "name": ln["name"],
            "tier": ln["tier"], "position_confidence": ln["conf"],
            "note": ln["note"],
            "length_m": round(polyline_length(ln["pts"] +
                                              ([ln["pts"][0]] if ln["closed"] else [])), 1),
            "min_terrain_m": round(min(h for h in ln["elev"] if h is not None), 1),
            "max_terrain_m": round(max(h for h in ln["elev"] if h is not None), 1),
            "status": "draft",
        }
        if "snap_median_m" in ln:
            props["shore_snap_median_m"] = ln["snap_median_m"]
            props["shore_snap_max_m"] = ln["snap_max_m"]
            props["derived_from"] = "shoreline_1632 (coastline_build.py)"
        if key == "wall_galata":
            props["enclosed_area_ha"] = round(gal_area_ha, 1)

        features.append({"type": "Feature", "geometry": geom, "properties": props})
        local_features.append({
            "layer": key, "id": key, "name": ln["name"], "tier": ln["tier"],
            "action": "wall",
            "note": f"{ln['note']} [{ln['conf']}]",
            "closed": ln["closed"],
            "rings": [[{"x": round(e - gx, 2), "z": round(n - gz, 2)} for e, n in ln["pts"]]],
        })

    # kapilar
    gate_count = 0
    for g in GATES:
        e, n = to_utm([(g["lon"], g["lat"])])[0]
        h = sample_utm(meta, heights, e, n)
        documented = g["wall"] == "wall_land"
        features.append({
            "type": "Feature",
            "geometry": {"type": "Point", "coordinates": [round(g["lon"], 6), round(g["lat"], 6)]},
            "properties": {
                "layer": "wall_gate", "id": g["id"], "name": g["name"], "wall": g["wall"],
                "tier": "Documented" if documented else "Reconstruction",
                "position_confidence": TRACED if documented else COARSE,
                "note": ("Kapi adlari Eremya Celebi + Evliya Celebi kapi listelerinden "
                         "(RESEARCH.md §3). Eremya suriçini 14 kapi uzerinden anlatir."),
                "terrain_elevation_m": None if h is None else round(h, 1),
                "status": "draft",
            },
        })
        local_features.append({
            "layer": "wall_gate", "id": g["id"], "name": g["name"],
            "tier": "Documented" if documented else "Reconstruction",
            "action": g["wall"],
            "note": f"{g['name']} — {g['wall']}. Konum yaklasik (Faz 1 madde 3 taslagi).",
            "closed": False,
            "rings": [[{"x": round(e - gx, 2), "z": round(n - gz, 2)}]],
        })
        gate_count += 1

    collection = {
        "type": "FeatureCollection",
        "name": "walls_1632",
        "metadata": {
            "title": "1632 İstanbul sur hatları ve kapıları",
            "status": "TASLAK — Caner onayı bekliyor (özellikle wall_galata)",
            "method": {
                "wall_land": "elle izlendi (yapı bugün ayakta), DEM ile kuru zemin denetimi",
                "wall_sea_*": (f"elle çapa → shoreline_1632'ye yapıştırma → karaya "
                               f"{args.inset:.0f} m ofset; kara yönü DEM'den seçildi"),
                "wall_galata": "kaba çevre poligonu — metrik güzergâh iddiası YOK",
            },
            "measured_lengths_km": lengths,
            "junction_gaps_m": junctions,
            "galata": {"perimeter_km": round(gal_perim_km, 2),
                       "enclosed_area_ha": round(gal_area_ha, 1)},
            "two_axes": ("'tier' 1632'deki VARLIĞI niteler, 'position_confidence' "
                         "GEOMETRİ kesinliğini. Deniz surlarının varlığı T1'dir; "
                         "çizgisi T2'dir çünkü taslak kıyımızdan türer."),
            "world_origin": meta["world_origin"],
            "copyright": "Bu çizim bize aittir; güzergâhlar RESEARCH.md ve genel coğrafyadan.",
        },
        "features": features,
    }

    geo_path = os.path.abspath(args.geojson)
    os.makedirs(os.path.dirname(geo_path), exist_ok=True)
    with open(geo_path, "w", encoding="utf-8") as fh:
        json.dump(collection, fh, indent=1, ensure_ascii=False)
    log(f"wrote {args.geojson} ({os.path.getsize(geo_path)//1024} KB, "
        f"{len(lines)} hat + {gate_count} kapi)")

    local_path = os.path.join(os.path.abspath(args.dir), "walls_1632_local.json")
    with open(local_path, "w", encoding="utf-8") as fh:
        json.dump({"world_origin": meta["world_origin"], "features": local_features},
                  fh, ensure_ascii=False)
    log("wrote walls_1632_local.json")
    log("walls_build OK")


if __name__ == "__main__":
    main()
