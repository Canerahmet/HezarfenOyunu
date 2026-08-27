"""
Hezarfen: 1632 — Bağımsız geodezi: WGS84 → UTM 35N ve DEM okuma.

## Neden rasterio kullanmıyor

2026-08-21'de bu makinede `rasterio` yüklenemez oldu:

    ImportError: DLL load failed while importing _base:
    An Application Control policy has blocked this file.

Bu bir Windows uygulama denetimi engelidir, kodla ilgili değil — ve **bütün**
GIS araçlarını (coastline, walls, districts, landmarks) birden etkiler, çünkü
hepsi `rasterio.warp.transform`u koordinat dönüşümü için kullanıyor.
Bkz. SETUP.md `[İNSAN]` maddesi.

Oysa bu boru hattı rasterio'dan **iki şey** istiyor: bir koordinat dönüşümü ve
bir raster okuma. İkisi de burada var ve hiçbiri raster kütüphanesi
gerektirmiyor:

* DEM zaten ham `.r16` olarak okunuyor (numpy yeter — `dem_fetch.py` COG'u
  indirirken rasterio'ya ihtiyaç duyar, sonrası duymaz).
* UTM ileri dönüşümü kapalı formüldür (Krüger serisi), milimetre mertebesinde.

## Doğruluk kendi kendine sınanır

Formül **uydurulmuş bir sayı olamaz**: `dem_meta.json` Galata Kulesi'nin hem
enlem/boylamını hem de daha önce rasterio ile hesaplanmış UTM karşılığını
kayıtlı tutuyor. `self_check()` ikisini karşılaştırır ve 0,05 m'yi aşan sapmada
yükseltir. Yani bu modül, yerini aldığı kütüphaneye karşı doğrulanıyor.
"""

import json
import math
import os

import numpy as np

# WGS84
A_AXIS = 6378137.0
F_FLAT = 1.0 / 298.257223563
K0 = 0.9996
FALSE_E = 500000.0
ZONE = 35
LON0 = math.radians(6.0 * ZONE - 183.0)      # 35. dilim → 27° D


def to_utm35n(lon_deg, lat_deg):
    """WGS84 (derece) → UTM 35N (metre). Krüger serisi, 4. mertebe."""
    n = F_FLAT / (2.0 - F_FLAT)
    n2, n3, n4 = n * n, n ** 3, n ** 4
    a_rect = A_AXIS / (1.0 + n) * (1.0 + n2 / 4.0 + n4 / 64.0)

    al = (n / 2.0 - 2.0 * n2 / 3.0 + 5.0 * n3 / 16.0,
          13.0 * n2 / 48.0 - 3.0 * n3 / 5.0,
          61.0 * n3 / 240.0,
          49561.0 * n4 / 161280.0)

    phi = math.radians(lat_deg)
    dl = math.radians(lon_deg) - LON0

    e_ecc = 2.0 * math.sqrt(n) / (1.0 + n)
    t = math.sinh(math.atanh(math.sin(phi))
                  - e_ecc * math.atanh(e_ecc * math.sin(phi)))
    xi = math.atan2(t, math.cos(dl))
    eta = math.atanh(math.sin(dl) / math.sqrt(1.0 + t * t))

    e_out, n_out = eta, xi
    for j, aj in enumerate(al, start=1):
        e_out += aj * math.cos(2 * j * xi) * math.sinh(2 * j * eta)
        n_out += aj * math.sin(2 * j * xi) * math.cosh(2 * j * eta)

    return FALSE_E + K0 * a_rect * e_out, K0 * a_rect * n_out


def to_utm(points_lonlat):
    """`walls_build.to_utm` ile aynı imza — çağıranlar değişmesin diye."""
    return [to_utm35n(lon, lat) for lon, lat in points_lonlat]


def load_dem(dir_path):
    """`dem_meta.json` + ham `.r16` → (meta, METRE cinsinden yükseklikler)."""
    with open(os.path.join(dir_path, "dem_meta.json"), encoding="utf-8") as fh:
        meta = json.load(fh)
    n = meta["resolution"]
    raw = np.fromfile(os.path.join(dir_path, meta["heightmap_file"]), dtype="<u2")
    grid = raw.reshape(n, n).astype(np.float64)     # satır 0 = güney
    heights = meta["base_elevation_m"] + (grid / 65535.0) * meta["height_range_m"]
    return meta, heights


def utm_to_grid(e, n, meta):
    """UTM → heightmap ızgara koordinatı (kesirli)."""
    e0, n0, e1, n1 = meta["bounds_utm"]
    res = meta["resolution"]
    return ((e - e0) / (e1 - e0) * (res - 1),
            (n - n0) / (n1 - n0) * (res - 1))


def sample_utm(meta, heights, e, n):
    """Bilineer yükseklik (m). Dünya dışındaysa None."""
    x, y = utm_to_grid(e, n, meta)
    res = meta["resolution"]
    if not (0 <= x <= res - 1 and 0 <= y <= res - 1):
        return None
    x0, y0 = int(x), int(y)
    x1, y1 = min(x0 + 1, res - 1), min(y0 + 1, res - 1)
    tx, ty = x - x0, y - y0
    return float(heights[y0, x0] * (1 - tx) * (1 - ty)
                 + heights[y0, x1] * tx * (1 - ty)
                 + heights[y1, x0] * (1 - tx) * ty
                 + heights[y1, x1] * tx * ty)


def from_utm35n(e, n):
    """
    UTM 35N (metre) → WGS84 (derece). `to_utm35n`'in tersi, aynı seri.

    Neden gerekli: sınır poligonlarını **metrik** olarak düzeltmek istiyoruz
    (belgelenmiş bir alana oturtmak, sur çizgisinden türetmek), ama depoya
    yazdığımız `refs/maps/*.geojson` WGS84'tür — ADR 0008: projeksiyon
    dönüşümü yalnızca Python'da olur. Ters dönüşüm olmadan metrik düzeltme
    coğrafi koordinata geri yazılamıyordu; `walls_build` bunun için hâlâ
    rasterio'ya bağlıydı ve bu makinede rasterio bloklu.
    """
    nn = F_FLAT / (2.0 - F_FLAT)
    n2, n3, n4 = nn * nn, nn ** 3, nn ** 4
    a_rect = A_AXIS / (1.0 + nn) * (1.0 + n2 / 4.0 + n4 / 64.0)

    be = (nn / 2.0 - 2.0 * n2 / 3.0 + 37.0 * n3 / 96.0 - n4 / 360.0,
          n2 / 48.0 + n3 / 15.0 - 437.0 * n4 / 1440.0,
          17.0 * n3 / 480.0 - 37.0 * n4 / 840.0,
          4397.0 * n4 / 161280.0)
    de = (2.0 * nn - 2.0 * n2 / 3.0 - 2.0 * n3,
          7.0 * n2 / 3.0 - 8.0 * n3 / 5.0,
          56.0 * n3 / 15.0,
          4279.0 * n4 / 630.0)

    xi = n / (K0 * a_rect)
    eta = (e - FALSE_E) / (K0 * a_rect)
    xi_p, eta_p = xi, eta
    for j, bj in enumerate(be, start=1):
        xi_p -= bj * math.sin(2 * j * xi) * math.cosh(2 * j * eta)
        eta_p -= bj * math.cos(2 * j * xi) * math.sinh(2 * j * eta)

    chi = math.asin(math.sin(xi_p) / math.cosh(eta_p))
    phi = chi
    for j, dj in enumerate(de, start=1):
        phi += dj * math.sin(2 * j * chi)

    lam = LON0 + math.atan2(math.sinh(eta_p), math.cos(xi_p))
    return math.degrees(lam), math.degrees(phi)


def ring_area(pts):
    """Kapalı halkanın alanı (m²) — ayakkabı bağlama formülü."""
    a = 0.0
    for i in range(len(pts)):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % len(pts)]
        a += x0 * y1 - x1 * y0
    return abs(a) * 0.5


def self_check(meta, tol_m=0.05):
    """
    Dönüşümü, boru hattının **kendi kaydına** karşı doğrular.

    `dem_meta.json` Galata Kulesi'nin enlem/boylamını ve daha önce rasterio ile
    hesaplanmış UTM karşılığını birlikte tutuyor. İkisi tutmuyorsa bu modül
    yanlıştır ve üretim durmalıdır — sessizce 10 m kaymış bir şehir, gözle
    fark edilmeyen ama her ölçümü bozan bir hatadır.
    """
    o = meta["world_origin"]
    e, n = to_utm35n(o["lon"], o["lat"])
    de, dn = e - o["utm_easting"], n - o["utm_northing"]
    err = math.hypot(de, dn)
    if err > tol_m:
        raise AssertionError(
            f"UTM donusumu boru hattiyla uyusmuyor: {err:.3f} m sapma "
            f"(dE {de:+.3f}, dN {dn:+.3f}). Beklenen kaynak: dem_meta.json "
            f"world_origin (rasterio/PROJ ile hesaplanmisti).")

    # TERS donusum kendi kendini sinar: ileri-geri gidip gelen bir nokta
    # basladigi yere donmeli. Ileri donusumun dis kaydi var, tersinin yok —
    # bu yuzden tek denetimi kapanma hatasidir. Sehrin dort kosesinde
    # bakiyoruz ki hata dilim ortasinda gizlenmesin.
    back = 0.0
    for lon, lat in ((28.90, 40.95), (29.09, 40.95), (28.90, 41.10),
                     (29.09, 41.10), (o["lon"], o["lat"])):
        ee, nn = to_utm35n(lon, lat)
        lo, la = from_utm35n(ee, nn)
        back = max(back, math.hypot((lo - lon) * 84000.0, (la - lat) * 111320.0))
    if back > tol_m:
        raise AssertionError(f"UTM ters donusumu kapanmiyor: {back:.4f} m")
    return err
