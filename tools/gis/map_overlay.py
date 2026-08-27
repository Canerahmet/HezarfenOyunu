"""
Hezarfen: 1632 — İnceleme bindirmesi: sur hatları + semtler (plan Faz 1 madde 3/6).

`renders/review/Map1632_vN/` üretir: RGB bindirme + `info.md`.

## Neden bu var

Sayılar ("kara surları 5,82 km") doğru görünebilirken çizgi yanlış yerde olabilir.
`walls_build.py` ve `districts_build.py` bunu denetimlerle kovalar ama denetimler
yalnızca *sorduğun* soruyu yanıtlar. Bindirme, sormadığın soruyu görünür kılar —
ve Caner'in onaylayabileceği tek biçimdir: onay yazılıdır ama *bakılan* şey görsel
olmalıdır (ADR 0006).

Renk kodu, KANIT SINIFINI taşır — güzelleştirme değil. Sahnede olduğu gibi burada
da hangi çizgiye ne kadar güvenileceği bakışla ayırt edilebilmelidir.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/map_overlay.py --dir data/gis/istanbul
"""

import argparse
import json
import os
import re
import sys

import numpy as np
import rasterio.shutil
from rasterio.io import MemoryFile

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

from coastline_build import load_dem, utm_to_grid   # noqa: E402
from dem_fetch import hillshade                     # noqa: E402

# (R, G, B) — kanit sinifina gore
COLORS = {
    # Mor, mavi DEGIL. Zeminde denizi mavi boyuyoruz ve o mavi MODERN su hattidir
    # (DEM'de dolgular duruyor). Incelemenin butun anlami bu iki hattin FARKINI
    # gormek; 1632 cizgisini de maviye boyamak, gorulmesi gereken tek seyi gizler.
    "shoreline_1632":   (175, 120, 255),
    "wall_land":        (245, 235, 210),    # kirik beyaz — BUGUN AYAKTA
    "wall_sea_marmara": (215, 175, 110),    # kum — kiyidan TURETILDI
    "wall_sea_halic":   (215, 175, 110),
    "wall_galata":      (255, 110, 140),    # pembe — KABA TASLAK
    "wall_gate":        (255, 140, 40),     # turuncu — kapi
    "district":         (80, 240, 215),     # turkuaz — OYUN bolgesi
    "landmark":         (255, 205, 60),     # sari — landmark
}

LEGEND = [
    ("wall_land", "Kara surları", "BUGÜN AYAKTA — elle izlendi (~100 m)"),
    ("wall_sea_marmara", "Deniz surları", "kendi 1632 kıyımızdan türetildi"),
    ("wall_galata", "Galata surları", "KABA TASLAK — georeferanslı plan yok"),
    ("wall_gate", "Kapılar", "kara suru kapıları kesin, deniz kapıları yaklaşık"),
    ("shoreline_1632", "1632 kıyı çizgisi (mor)",
     "DEM türevi taslak (ADR 0008). Zemindeki MAVİ deniz ise BUGÜNKÜ su hattıdır — "
     "ikisinin arasındaki şerit, geri aldığımız dolgudur."),
    ("district", "Semtler", "OYUN bölgesi — tarihsel mahalle sınırı DEĞİL"),
    ("landmark", "Landmark", "konumlar ~100 m yaklaşık"),
]


def log(msg):
    print(f"[HZ] {msg}", flush=True)


def next_version(root, asset):
    os.makedirs(root, exist_ok=True)
    best = 0
    for name in os.listdir(root):
        m = re.match(rf"^{re.escape(asset)}_v(\d+)$", name)
        if m:
            best = max(best, int(m.group(1)))
    return best + 1


def write_rgb_png(path, rgb):
    """rgb: (3, H, W) uint8. GDAL PNG suruculu yalnizca CreateCopy destekler."""
    _, h, w = rgb.shape
    profile = dict(driver="GTiff", height=h, width=w, count=3, dtype="uint8")
    with MemoryFile() as mem:
        with mem.open(**profile) as ds:
            ds.write(rgb)
        rasterio.shutil.copy(mem.name, path, driver="PNG")


class Canvas:
    def __init__(self, heights, meta):
        self.n = meta["resolution"]
        shade = hillshade(heights, meta["meters_per_sample_x"], exaggeration=3.0)
        base = np.clip(shade * 150.0 + 35.0, 0, 255)
        # Denizi hafif maviye boya: kara/deniz ayrimi bindirmede okunabilir olmali.
        sea = heights <= 0.5
        rgb = np.stack([base, base, base]).astype(np.float64)
        rgb[0][sea] *= 0.55
        rgb[1][sea] *= 0.75
        rgb[2][sea] = np.minimum(255.0, rgb[2][sea] * 1.25 + 25.0)

        # DEM dizisinde satir 0 = GUNEY (heightmap_format: "row0=south").
        # Goruntude ise satir 0 = KUZEY olmali. Ceviriyi burada bir kez yapiyoruz;
        # asagidaki cizim `n-1-y` ile zaten kuzey-ust varsayar. Bu ceviri
        # atlandiginda taban rasteri ile vektorler DIKEYDE TERS olur ve
        # "Marmara'yi kuzeyde gostermek" gibi bir sonuc verir — ilk uretimde
        # tam olarak bu oldu ve ancak DEM satirlari OLCULEREK anlasildi.
        self.img = np.flip(rgb, axis=1)

    def plot(self, px, py, color, weight=1):
        xi, yi = int(round(px)), int(round(py))
        for dy in range(-weight, weight + 1):
            for dx in range(-weight, weight + 1):
                x, y = xi + dx, yi + dy
                if 0 <= x < self.n and 0 <= y < self.n:
                    # satir 0 = kuzey (goruntu duzeni); DEM'de satir 0 = guney
                    for c in range(3):
                        self.img[c][self.n - 1 - y][x] = color[c]

    def line(self, p0, p1, color, weight=1):
        steps = int(max(abs(p1[0] - p0[0]), abs(p1[1] - p0[1]))) + 1
        for t in np.linspace(0.0, 1.0, steps * 2 + 2):
            self.plot(p0[0] + (p1[0] - p0[0]) * t,
                      p0[1] + (p1[1] - p0[1]) * t, color, weight)

    def polyline(self, pts, color, closed=False, weight=1):
        for i in range(len(pts) - 1):
            self.line(pts[i], pts[i + 1], color, weight)
        if closed and len(pts) > 2:
            self.line(pts[-1], pts[0], color, weight)

    def cross(self, p, color, size=4):
        for d in range(-size, size + 1):
            self.plot(p[0] + d, p[1], color, 0)
            self.plot(p[0], p[1] + d, color, 0)

    def save(self, path):
        write_rgb_png(path, np.clip(self.img, 0, 255).astype(np.uint8))


def read_local(dir_path, filename):
    path = os.path.join(dir_path, filename)
    if not os.path.exists(path):
        log(f"UYARI {filename} yok — atlandi")
        return []
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)["features"]


def to_grid(ring, meta, gx, gz):
    return utm_to_grid([(p["x"] + gx, p["z"] + gz) for p in ring], meta)


def main():
    p = argparse.ArgumentParser(description="1632 sur + semt inceleme bindirmesi")
    p.add_argument("--dir", default="data/gis/istanbul")
    p.add_argument("--out-root", default="renders/review")
    p.add_argument("--asset", default="Map1632")
    args = p.parse_args()

    meta, heights = load_dem(args.dir)
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]

    canvas = Canvas(heights, meta)
    counts = {}

    # Cizim SIRASI onemli: onemsizden onemliye. Ustte kalan, gozle once okunandir.
    for filename, layers in (
        ("coastline_1632_local.json", ("shoreline_1632",)),
        ("districts_local.json", ("district",)),
        ("walls_1632_local.json", ("wall_land", "wall_sea_marmara",
                                   "wall_sea_halic", "wall_galata")),
    ):
        for feat in read_local(args.dir, filename):
            if feat["layer"] not in layers:
                continue
            color = COLORS[feat["layer"]]
            weight = 1 if feat["layer"] == "shoreline_1632" else 1
            for ring in feat["rings"]:
                canvas.polyline(to_grid(ring, meta, gx, gz), color,
                                closed=feat.get("closed", False), weight=weight)
            counts[feat["layer"]] = counts.get(feat["layer"], 0) + 1

    for filename, layer, size in (("walls_1632_local.json", "wall_gate", 3),
                                  ("landmarks_1632_local.json", "landmark", 5)):
        for feat in read_local(args.dir, filename):
            if feat["layer"] != layer:
                continue
            for ring in feat["rings"]:
                for pt in to_grid(ring, meta, gx, gz):
                    canvas.cross(pt, COLORS[layer], size)
            counts[layer] = counts.get(layer, 0) + 1

    version = next_version(args.out_root, args.asset)
    out_dir = os.path.join(args.out_root, f"{args.asset}_v{version}")
    os.makedirs(out_dir, exist_ok=True)
    png = os.path.join(out_dir, "overlay.png")
    canvas.save(png)
    log(f"wrote {png} ({meta['resolution']}^2)")

    lines = [
        f"# {args.asset} v{version} — 1632 sur hatları ve semtler",
        "",
        f"Bindirme: `overlay.png` ({meta['resolution']}×{meta['resolution']}, "
        f"{meta['meters_per_sample_x']:.2f} m/piksel). "
        f"Dünya orijini Galata Kulesi tabanı; kuzey yukarı.",
        "",
        "## Renk kodu — KANIT SINIFI",
        "",
        "| Renk | Katman | Ne kadar güvenilir |",
        "|---|---|---|",
    ]
    for key, name, trust in LEGEND:
        r, g, b = COLORS[key]
        lines.append(f"| `rgb({r},{g},{b})` | {name} ({counts.get(key, 0)} öğe) | {trust} |")

    lines += [
        "",
        "## Bakılması istenen üç şey",
        "",
        "1. **Galata surları (pembe).** Bu poligon kaba bir çevredir, sur güzergâhı "
        "değildir — 1860'larda yıkıldı ve elimizde georeferanslı dönem planı yok. "
        "Kule çevrenin içinde mi, çevre makul mü? Değilse ya küçültürüz ya da "
        "dönem planı bulana kadar sahneden çıkarırız.",
        "2. **Deniz surlarının (kum rengi) kıyıya oturuşu.** Bunlar elle çizilmedi; "
        "her nokta kendi 1632 kıyı çizgimize yapıştırılıp 15 m karaya itildi. "
        "Kıyıdan kopan bir kesim varsa kıyı çizgisi orada bozuk demektir.",
        "3. **Semt sınırları (turkuaz).** Bunlar OYUN bölgeleridir, tarihsel mahalle "
        "sınırı DEĞİLDİR (ADR 0011). Soru tarihsel değil oynanış: uçuş ekseni "
        "(Okmeydanı → Galata → Boğaz → Üsküdar) doğru hücrelere bölünmüş mü?",
        "",
        "## Ölçülenler",
        "",
        "Uzunluk ve alan sayıları için `refs/maps/walls_1632.geojson` ve "
        "`refs/maps/districts.geojson` içindeki `metadata` bloklarına bakın; "
        "her ikisi de üretim sırasında denetimden geçti (aksi hâlde dosya yazılmazdı).",
        "",
        "## Onay",
        "",
        "Notlar `docs/feedback/walls_districts.md` dosyasına yazılır. "
        "Onay biçimi: `OK v{}`.".format(version),
        "",
    ]
    with open(os.path.join(out_dir, "info.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))
    log(f"wrote {out_dir}/info.md")
    log("map_overlay OK")


if __name__ == "__main__":
    main()
