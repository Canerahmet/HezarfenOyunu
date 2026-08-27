"""
Hezarfen: 1632 — Heightmap georeferans denetimi.

`dem_fetch.py` çıktısını bilinen noktalardan yoklar. Amaç: arazinin DOĞRU YERDE
olduğunu ölçümle kanıtlamak. Tepe gölgesine bakıp "İstanbul'a benziyor" demek
yeterli değildir — yarım hücre kayma da İstanbul'a benzer, ama şehir yerleşimi
o kaymayla birlikte tümüyle yanlış olur.

Aynı zamanda bir REGRESYON testidir: DEM boru hattı değişirse (kaynak, yeniden
örnekleme, kareye tamamlama) bu sayılar kayar ve fark hemen görülür.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/dem_probe.py --dir data/gis/istanbul
"""

import argparse
import json
import os
import sys

import numpy as np

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

from geodesy import to_utm35n as _to_utm35n   # noqa: E402


def warp_transform(src_crs, dst_crs, xs, ys):
    """
    `rasterio.warp.transform` yerine geçen kabuk — imza aynı.

    Gerekçe geodesy.py başlığında: bu makinede rasterio'nun DLL'i Windows
    uygulama denetimince bloklu. Bu denetim aracının çalışmaması, DEM'in
    georeferansını **hiç** sınayamamak demekti.
    """
    if (src_crs, dst_crs) != ("EPSG:4326", "EPSG:32635"):
        raise ValueError(f"desteklenmeyen donusum: {src_crs} -> {dst_crs}")
    pairs = [_to_utm35n(x, y) for x, y in zip(xs, ys)]
    return [p[0] for p in pairs], [p[1] for p in pairs]

# (ad, lon, lat, beklenen irtifa m, tolerans m, not)
# Beklenen degerler yaklasiktir; amac mertebe ve SIRALAMA dogrulugudur.
# Beklenen irtifasi None olanlar DENIZ noktalaridir: beklenen deger meta'daki
# deniz tabani kotudur (y=0 deniz seviyesi, taban ondan seabed_depth asagida).
LANDMARKS = [
    ("Bogaz ortasi (su)",   29.0250, 41.0400,  None,   1.0, "deniz tabani"),
    ("Halic ortasi (su)",   28.9600, 41.0300,  None,   1.0, "deniz tabani"),
    # Dunya orijini — dem_fetch.GALATA_LON/LAT ile AYNI deger olmali, yoksa
    # "Unity X/Z" sutunu sifir cikmaz ve sahte bir kayma izlenimi verir.
    ("Galata Kulesi",     28.974017, 41.025637, 37.0, 20.0, "dunya orijini (0,0)"),
    ("Ayasofya",            28.9802, 41.0086,  32.0,  20.0, "1. tepe"),
    ("Suleymaniye",         28.9639, 41.0165,  55.0,  25.0, "3. tepe"),
    ("Uskudar Dogancilar",  29.0181, 41.0245,  35.0,  25.0, "Hezarfen'in inis noktasi"),
    ("Buyuk Camlica",       29.0700, 41.0272, 265.0,  35.0, "bolgenin en yuksegi"),
]


def load(dir_path):
    with open(os.path.join(dir_path, "dem_meta.json"), encoding="utf-8") as fh:
        meta = json.load(fh)

    n = meta["resolution"]
    raw = np.fromfile(os.path.join(dir_path, meta["heightmap_file"]), dtype="<u2")
    if raw.size != n * n:
        raise SystemExit(f"[HZ] HATA: {raw.size} deger, {n*n} bekleniyordu.")

    # Dosyada satir 0 = GUNEY. Kuzey-yukari duzene cevir ki indeksleme sezgisel olsun.
    grid = np.flipud(raw.reshape(n, n).astype(np.float64))
    heights = meta["base_elevation_m"] + (grid / 65535.0) * meta["height_range_m"]
    return meta, heights


def sample(meta, heights, lon, lat):
    """WGS84 noktasindan irtifa (bilinear)."""
    e, north = warp_transform("EPSG:4326", meta["crs"], [lon], [lat])
    e, north = e[0], north[0]

    minx, miny, maxx, maxy = meta["bounds_utm"]
    n = meta["resolution"]

    # Piksel merkezleri izgara dugumlerinde: 0..n-1
    fx = (e - minx) / (maxx - minx) * (n - 1)
    fy = (maxy - north) / (maxy - miny) * (n - 1)      # satir 0 = kuzey

    if not (0 <= fx <= n - 1 and 0 <= fy <= n - 1):
        return None, (e, north)

    x0, y0 = int(np.floor(fx)), int(np.floor(fy))
    x1, y1 = min(x0 + 1, n - 1), min(y0 + 1, n - 1)
    tx, ty = fx - x0, fy - y0

    v = (heights[y0, x0] * (1 - tx) * (1 - ty) + heights[y0, x1] * tx * (1 - ty) +
         heights[y1, x0] * (1 - tx) * ty + heights[y1, x1] * tx * ty)
    return v, (e, north)


def main():
    p = argparse.ArgumentParser(description="Heightmap georeferans denetimi")
    p.add_argument("--dir", default="data/gis/istanbul")
    args = p.parse_args()

    meta, heights = load(args.dir)
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]

    print(f"[HZ] {meta['resolution']}x{meta['resolution']} "
          f"@ {meta['meters_per_sample_x']:.2f} m/ornek, "
          f"dunya {meta['size_x_m']:.0f} x {meta['size_z_m']:.0f} m")
    print(f"[HZ] irtifa araligi: {meta['min_elevation_m']:.1f} .. "
          f"{meta['max_elevation_m']:.1f} m (terrain {meta['height_range_m']:.0f} m)")
    print()
    print(f"{'Nokta':<22}{'olculen':>9}{'beklenen':>10}{'fark':>8}  "
          f"{'Unity X':>9}{'Unity Z':>9}  durum")
    print("-" * 88)

    seabed = meta.get("base_elevation_m", 0.0)

    failures = 0
    for name, lon, lat, expected, tol, note in LANDMARKS:
        if expected is None:
            expected = seabed          # deniz noktalari taban kotunda olmali
        value, (e, north) = sample(meta, heights, lon, lat)
        if value is None:
            print(f"{name:<22}{'ALAN DISI':>9}")
            failures += 1
            continue

        diff = value - expected
        ok = abs(diff) <= tol
        failures += 0 if ok else 1
        # Unity dunya koordinati: orijin Galata Kulesi, X=dogu, Z=kuzey
        print(f"{name:<22}{value:>8.1f}m{expected:>9.0f}m{diff:>+7.1f}m  "
              f"{e - gx:>+8.0f} {north - gz:>+8.0f}  {'OK' if ok else 'SAPMA'}  {note}")

    print()
    if failures:
        print(f"[HZ] {failures} nokta toleransi asti — georeferansi denetle.")
        raise SystemExit(1)
    print("[HZ] georeferans OK: tum noktalar toleransta.")


if __name__ == "__main__":
    main()
