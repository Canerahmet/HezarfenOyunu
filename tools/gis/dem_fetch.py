"""
Hezarfen: 1632 — DEM indirme → kırpma → Unity heightmap (plan Görev 9, Faz 1 madde 1).

Kaynak: **Copernicus DEM GLO-30** (ESA/Airbus, 30 m), AWS Open Data üzerinden
kimlik doğrulamasız. Dosyalar COG (Cloud-Optimized GeoTIFF) olduğu için GDAL
yalnızca ihtiyaç duyulan pencereyi HTTP range isteğiyle çeker — 1°×1°'lik 20 MB'lık
karodan birkaç yüz KB iner.

**Atıf ZORUNLUDUR** (bkz. refs/LICENSES.md). Oyun içi "Krediler" ekranına girer.

Boru hattı:
    Copernicus karoları  →  mozaik + bbox kırpma (WGS84)
        →  UTM 35N'e yeniden projeksiyon (metrik, 1 birim = 1 metre)
        →  deniz seviyesi kırpması + isteğe bağlı DSM tepe bastırma
        →  16-bit heightmap (.r16) + meta (.json) + tepe gölgesi önizleme (.png)
        →  Unity: Hezarfen/GIS/DEM'den Terrain uret

Koordinat sözleşmesi (plan Faz 1 madde 4):
    Dünya orijini **Galata Kulesi tabanıdır**. Terrain'in güneybatı köşesi, meta
    dosyasındaki `world_origin_offset_m` kadar bu orijinden ötelenmiş olarak
    yerleştirilir. Böylece DEM, GeoJSON (Görev 10) ve landmark'lar aynı çerçeveyi
    paylaşır. Y ekseninde 0 = **deniz seviyesi** — uçuş oyununda irtifa okuması
    deniz seviyesine göredir; kule tabanına göre değil.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/dem_fetch.py --out data/gis/istanbul
"""

import argparse
import json
import math
import os
import sys
import time

import numpy as np
import rasterio
import rasterio.shutil
from rasterio.io import MemoryFile
from rasterio.merge import merge as rio_merge
from rasterio.warp import (Resampling, reproject, transform as warp_transform,
                           transform_bounds)

# --- Plan Bolum 6 madde 1'de verilen bbox (batı, güney, doğu, kuzey) ---
PLAN_BBOX = (28.90, 40.98, 29.08, 41.06)

# --- Dunya orijini: Galata Kulesi (plan Faz 1 madde 4) ---
GALATA_LON = 28.974017
GALATA_LAT = 41.025637

TARGET_CRS = "EPSG:32635"          # UTM 35N — 24°-30° D arasi, bbox tamamen icinde
SOURCE_CRS = "EPSG:4326"

COP30_BASE = "https://copernicus-dem-30m.s3.amazonaws.com"
LICENCE = ("Produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014 and "
           "(c) Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS "
           "by the European Union and ESA; all rights reserved.")

# Unity Terrain heightmap cozunurlugu 2^n+1 olmak ZORUNDA.
VALID_RESOLUTIONS = (513, 1025, 2049, 4097)


def log(msg):
    print(f"[HZ] {msg}", flush=True)


# ------------------------------------------------------------------- karolar

def tile_name(lat, lon):
    ns = "N" if lat >= 0 else "S"
    ew = "E" if lon >= 0 else "W"
    return (f"Copernicus_DSM_COG_10_{ns}{abs(int(math.floor(lat))):02d}_00_"
            f"{ew}{abs(int(math.floor(lon))):03d}_00_DEM")


def tiles_for_bbox(bbox):
    """bbox'ı kapsayan 1°×1° karo URL'leri."""
    w, s, e, n = bbox
    urls = []
    for lat in range(int(math.floor(s)), int(math.floor(n)) + 1):
        for lon in range(int(math.floor(w)), int(math.floor(e)) + 1):
            name = tile_name(lat, lon)
            urls.append(f"/vsicurl/{COP30_BASE}/{name}/{name}.tif")
    return urls


# --------------------------------------------------------------- coğrafi iş

def fetch_mosaic(bbox, urls):
    """Karoları aç, bbox'a kırpılmış tek mozaik döndür. (dizi, transform)"""
    datasets = []
    for url in urls:
        try:
            ds = rasterio.open(url)
            datasets.append(ds)
            log(f"opened {url.rsplit('/', 1)[-1]}  {ds.width}x{ds.height} {ds.crs}")
        except rasterio.RasterioIOError as exc:
            # Deniz uzerindeki karolar Copernicus'ta HIC YOK; bu bir hata degil.
            log(f"WARN karo yok/acilamadi (deniz olabilir): {url.rsplit('/', 1)[-1]} — {exc}")

    if not datasets:
        raise SystemExit("[HZ] HATA: hicbir DEM karosu acilamadi. Ag baglantisini kontrol et.")

    try:
        arr, transform = rio_merge(datasets, bounds=bbox)
    finally:
        for ds in datasets:
            ds.close()

    log(f"mosaic: {arr.shape[2]}x{arr.shape[1]} ornek (WGS84)")
    return arr[0], transform


def utm_grid(bbox, square=True):
    """
    bbox'ı UTM 35N'de eksen hizalı bir dikdörtgene çevirir.

    `square=True` ise kısa eksen simetrik olarak büyütülüp kare yapılır. Sebep:
    Unity heightmap'i KAREDİR; dikdörtgen bir alanı kare bir ızgaraya sığdırmak,
    X ve Z'de farklı metre/örnek değeri üretir. Bu, arazi detayının bir yönde
    ezilmesi demektir ve düzeltmesi sonradan çok pahalıdır.
    """
    w, s, e, n = bbox
    xs, ys = warp_transform(SOURCE_CRS, TARGET_CRS,
                            [w, e, w, e], [s, s, n, n])
    minx, maxx = min(xs), max(xs)
    miny, maxy = min(ys), max(ys)

    if square:
        side = max(maxx - minx, maxy - miny)
        cx, cy = (minx + maxx) * 0.5, (miny + maxy) * 0.5
        minx, maxx = cx - side * 0.5, cx + side * 0.5
        miny, maxy = cy - side * 0.5, cy + side * 0.5

    return minx, miny, maxx, maxy


def fetch_bbox_for(bounds_utm, pad_m=600.0):
    """
    Hedef metrik alanı kapsayan WGS84 bbox'ı — İNDİRME bunun için yapılır.

    Kareye tamamlama hedef ızgarada yapılır; mozaik hâlâ ham bbox için indirilirse
    büyütülen kuzey/güney şeritleri veri bulamaz ve deniz seviyesine düşer.
    (Bu bir kez yaşandı: örneklerin %42,88'i boş çıktı — oran, kareye tamamlamanın
    eklediği alanla birebir örtüşüyordu.) Pay, yeniden örnekleme çekirdeğinin
    kenarda veri bulması içindir.
    """
    minx, miny, maxx, maxy = bounds_utm
    w, s, e, n = transform_bounds(TARGET_CRS, SOURCE_CRS,
                                  minx - pad_m, miny - pad_m,
                                  maxx + pad_m, maxy + pad_m)
    return w, s, e, n


def to_utm_grid(src_arr, src_transform, bounds_utm, resolution, nodata):
    """Coğrafi mozaiği hedef metrik ızgaraya yeniden projekte eder."""
    minx, miny, maxx, maxy = bounds_utm
    res_x = (maxx - minx) / (resolution - 1)
    res_y = (maxy - miny) / (resolution - 1)

    # Piksel MERKEZLERI izgara dugumlerine otursun: heightmap bir yukseklik ALANI
    # degil, dugum ORNEKLERI dizisidir. Yarim piksel kaymasi, arazinin GeoJSON
    # landmark'larina gore yarim hucre kaymasi demektir.
    dst_transform = rasterio.Affine(
        res_x, 0.0, minx - res_x * 0.5,
        0.0, -res_y, maxy + res_y * 0.5)

    dst = np.full((resolution, resolution), np.nan, dtype=np.float32)
    reproject(
        source=src_arr,
        destination=dst,
        src_transform=src_transform,
        src_crs=SOURCE_CRS,
        src_nodata=nodata,
        dst_transform=dst_transform,
        dst_crs=TARGET_CRS,
        dst_nodata=np.nan,
        resampling=Resampling.bilinear,
    )

    log(f"reprojected -> {resolution}x{resolution} @ {res_x:.2f} x {res_y:.2f} m/ornek")
    return dst, res_x, res_y


# ------------------------------------------------------------------- işleme

def suppress_spikes(z, cell_m, radius_m):
    """
    DSM tepe bastırma (medyan filtresi).

    Copernicus GLO-30 bir **DSM**'dir: yüzey modelidir, çıplak zemin değil. Modern
    binalar ve ağaçlar irtifaya karışır. 1632 şehri için bu bir tarihsel hatadır —
    Levent'in gökdelenleri arazi tepesi olarak görünür.

    Medyan filtresi bina ölçeğindeki (birkaç hücre) sivrilikleri siler, tepe
    ölçeğindeki (yüzlerce hücre) formu korur. Tam bir DTM değildir ve öyle
    sunulmamalıdır — bkz. ADR 0007'deki dürüstlük notu.
    """
    if radius_m <= 0:
        return z

    k = max(3, int(round(radius_m / cell_m)) | 1)      # tek sayi olmali
    pad = k // 2
    padded = np.pad(z, pad, mode="edge")
    windows = np.lib.stride_tricks.sliding_window_view(padded, (k, k))
    out = np.median(windows, axis=(-2, -1)).astype(np.float32)
    log(f"spike suppression: {k}x{k} medyan (~{k * cell_m:.0f} m pencere)")
    return out


def hillshade(z, cell_m, exaggeration=4.0, azimuth_deg=315.0, altitude_deg=45.0):
    """Tepe gölgesi — arazinin DOĞRU indiğini gözle denetlemenin en hızlı yolu."""
    dy, dx = np.gradient(z * exaggeration, cell_m)
    slope = np.arctan(np.hypot(dx, dy))
    aspect = np.arctan2(-dx, dy)

    az = math.radians(360.0 - azimuth_deg + 90.0)
    alt = math.radians(altitude_deg)
    shaded = (math.sin(alt) * np.cos(slope) +
              math.cos(alt) * np.sin(slope) * np.cos(az - aspect))
    return np.clip(shaded, 0.0, 1.0)


def write_png(path, arr_u8):
    """
    GDAL'in PNG suruculu yalnizca CreateCopy destekler; bellekte bir GTiff kurup
    kopyalamak dis bagimlilik (Pillow) eklemeden PNG yazmanin dogru yoludur.
    """
    h, w = arr_u8.shape
    profile = dict(driver="GTiff", height=h, width=w, count=1, dtype="uint8")
    with MemoryFile() as mem:
        with mem.open(**profile) as ds:
            ds.write(arr_u8, 1)
        rasterio.shutil.copy(mem.name, path, driver="PNG")
    log(f"wrote {os.path.basename(path)}")


# --------------------------------------------------------------------- ana

def main():
    p = argparse.ArgumentParser(description="Hezarfen DEM -> Unity heightmap")
    p.add_argument("--out", default="data/gis/istanbul", help="Cikti klasoru")
    p.add_argument("--bbox", nargs=4, type=float, metavar=("W", "S", "E", "N"),
                   default=list(PLAN_BBOX), help="WGS84 bbox (varsayilan: plan Bolum 6)")
    p.add_argument("--resolution", type=int, default=2049,
                   choices=VALID_RESOLUTIONS, help="Heightmap cozunurlugu (2^n+1)")
    p.add_argument("--no-square", action="store_true",
                   help="Alani kare yapma (X/Z'de farkli metre/ornek olusur)")
    p.add_argument("--smooth", type=float, default=90.0,
                   help="DSM tepe bastirma penceresi (m). 0 = kapali")
    p.add_argument("--sea-level", type=float, default=0.0,
                   help="Bu degerin altindaki irtifalar kirpilir (m)")
    p.add_argument("--seabed-depth", type=float, default=12.0,
                   help="Deniz tabaninin deniz seviyesi altindaki derinligi (m)")
    p.add_argument("--height-margin", type=float, default=1.10,
                   help="Terrain yukseklik araligi payi")
    args = p.parse_args()

    out_dir = os.path.abspath(args.out)
    os.makedirs(out_dir, exist_ok=True)
    bbox = tuple(args.bbox)

    log(f"bbox WGS84 (plan): {bbox}")

    # SIRALAMA ONEMLI: once hedef metrik alan belirlenir, INDIRME onun icin yapilir.
    bounds_utm = utm_grid(bbox, square=not args.no_square)
    fetch_bbox = fetch_bbox_for(bounds_utm)
    log(f"indirme bbox (kare + pay): {tuple(round(v, 4) for v in fetch_bbox)}")

    urls = tiles_for_bbox(fetch_bbox)
    log(f"{len(urls)} karo gerekli")

    t0 = time.time()
    with rasterio.Env(GDAL_DISABLE_READDIR_ON_OPEN="EMPTY_DIR",
                      CPL_VSIL_CURL_ALLOWED_EXTENSIONS=".tif",
                      AWS_NO_SIGN_REQUEST="YES"):
        mosaic, src_transform = fetch_mosaic(fetch_bbox, urls)
        nodata = -32767.0

        z, res_x, res_y = to_utm_grid(mosaic, src_transform, bounds_utm,
                                      args.resolution, nodata)
    log(f"indirme + projeksiyon: {time.time() - t0:.1f} s")

    # NaN (kapsama disi) deniz kabul edilir; Copernicus acik denizde karo tasimaz.
    holes = int(np.isnan(z).sum())
    if holes:
        log(f"{holes} bos ornek deniz seviyesine cekildi ({holes / z.size * 100:.2f}%)")
    z = np.nan_to_num(z, nan=args.sea_level, posinf=args.sea_level, neginf=args.sea_level)

    raw_min, raw_max = float(z.min()), float(z.max())
    z = suppress_spikes(z, res_x, args.smooth)

    # --- DENIZ TABANI ---
    # Deniz hucreleri deniz seviyesine DEGIL, altina indirilir.
    #
    # Neden: su duzlemi y=0'dadir. Deniz tabani da y=0 olsaydi iki yuzey ayni
    # kotta cakisir; su ya hic gorunmez ya z-fighting yapar, ayrica denizin
    # derinligi olmazdi. (Bu tam olarak yasandi: ilk Faz 1 karesinde Bogaz'in
    # yerinde kara rengi duz bir zemin vardi.)
    #
    # Copernicus batimetri TASIMAZ; 12 m sabit bir oyun degeridir, olcum degil.
    # Gercek derinlik (Bogaz'da 30-100 m) gerekirse Faz 3'te batimetri eklenir.
    sea_mask = z <= args.sea_level
    z = np.maximum(z, args.sea_level)
    z[sea_mask] = args.sea_level - args.seabed_depth

    base_elev = args.sea_level - args.seabed_depth
    z_min, z_max = float(z.min()), float(z.max())
    height_range = max(1.0, math.ceil((z_max - base_elev) * args.height_margin))
    log(f"irtifa: ham {raw_min:.1f}..{raw_max:.1f} m -> islenmis {z_min:.1f}..{z_max:.1f} m")
    log(f"deniz tabani {base_elev:.0f} m ({sea_mask.sum()/sea_mask.size*100:.1f}% hucre), "
        f"terrain araligi {height_range:.0f} m")

    # --- 16-bit heightmap ---
    # Satir 0 = GUNEY. Unity'nin TerrainData.SetHeights(x, y) cagrisinda y ekseni
    # +Z (kuzey) yonundedir; kaynagi bu duzende yazmak, Unity tarafinda ters
    # cevirme gerektirmez ve "arazi aynalanmis" hatasini bastan imkansiz kilar.
    normalized = np.clip((z - base_elev) / height_range, 0.0, 1.0)
    south_up = np.flipud(normalized)
    heights_u16 = np.round(south_up * 65535.0).astype("<u2")

    raw_name = f"heightmap_{args.resolution}.r16"
    raw_path = os.path.join(out_dir, raw_name)
    heights_u16.tofile(raw_path)
    log(f"wrote {raw_name} ({os.path.getsize(raw_path)} bayt)")

    # --- onizlemeler (kuzey yukarida — insan gozu boyle okur) ---
    write_png(os.path.join(out_dir, "preview_height.png"),
              (normalized * 255.0).astype(np.uint8))
    write_png(os.path.join(out_dir, "preview_hillshade.png"),
              (hillshade(z, res_x) * 255.0).astype(np.uint8))

    # --- meta ---
    gx, gy = warp_transform(SOURCE_CRS, TARGET_CRS, [GALATA_LON], [GALATA_LAT])
    galata_e, galata_n = gx[0], gy[0]
    minx, miny, maxx, maxy = bounds_utm

    meta = {
        "source": "Copernicus DEM GLO-30 (COG, AWS Open Data)",
        "source_type": "DSM (yuzey modeli - ciplak zemin DEGIL)",
        "licence": LICENCE,
        "attribution_required": True,
        "crs": TARGET_CRS,
        "bbox_wgs84_plan": list(bbox),
        "bbox_wgs84_fetched": list(fetch_bbox),
        "bounds_utm": [minx, miny, maxx, maxy],
        "world_origin": {
            "name": "Galata Kulesi tabani",
            "lon": GALATA_LON, "lat": GALATA_LAT,
            "utm_easting": galata_e, "utm_northing": galata_n,
        },
        # Terrain'in guneybati kosesinin, dunya orijinine gore Unity konumu.
        "world_origin_offset_m": {"x": minx - galata_e, "z": miny - galata_n},
        "resolution": args.resolution,
        "size_x_m": maxx - minx,
        "size_z_m": maxy - miny,
        "meters_per_sample_x": res_x,
        "meters_per_sample_z": res_y,
        # Terrain nesnesi bu kota yerlestirilir. y=0 hala DENIZ SEVIYESIDIR;
        # taban ondan seabed_depth kadar asagidadir.
        "base_elevation_m": base_elev,
        "sea_level_m": args.sea_level,
        "seabed_depth_m": args.seabed_depth,
        "height_range_m": height_range,
        "min_elevation_m": z_min,
        "max_elevation_m": z_max,
        "raw_min_elevation_m": raw_min,
        "raw_max_elevation_m": raw_max,
        "spike_suppression_m": args.smooth,
        "heightmap_file": raw_name,
        "heightmap_format": "uint16 little-endian, row-major, row0=south, col0=west",
        "generated_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "command": " ".join([os.path.basename(sys.argv[0])] + sys.argv[1:]),
    }
    meta_path = os.path.join(out_dir, "dem_meta.json")
    with open(meta_path, "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)
    log(f"wrote dem_meta.json")

    log(f"dunya: {meta['size_x_m']:.0f} x {meta['size_z_m']:.0f} m, "
        f"Galata ofseti ({meta['world_origin_offset_m']['x']:.0f}, "
        f"{meta['world_origin_offset_m']['z']:.0f}) m")
    log(f"dem_fetch OK -> {out_dir}")


if __name__ == "__main__":
    main()
