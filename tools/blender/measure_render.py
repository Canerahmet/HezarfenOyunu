"""
Hezarfen: 1632 — Render ölçüm aracı.

Neden var: CLAUDE.md'nin kuralı "render bir gözlemdir, kanıt değil". Ama bir
kusuru ölçmek için önce **doğru bölgeyi** bulmak gerekir ve normalize koordinat
tahmin etmek bir kez şu hataya yol açtı: pencere pikselleri sanılan yerden
okundu, sonra gölgeli bir yan duvardan okundu, iki çelişkili sayı çıktı.

Bu yüzden araç iki kip sunar:

  --grid N     kareyi NxN bölgeye böler ve her bölgenin ortalamasını basar.
               Önce buna bakılır: sayılar kadrajın neresinde ne olduğunu
               tahmine gerek bırakmadan gösterir.
  --rect ...   belirli bir dikdörtgeni ölçer (normalize 0-1: x0 y0 x1 y1).

Değerler **sRGB 0-255**'tir; gözün gördüğü ölçek odur. Ayrıca doygunluk ve
R/G oranı basılır: boya tonu tartışması ancak bu oranla yapılabilir.

Kullanım:
  blender --background --factory-startup --python tools/blender/measure_render.py -- \
      --in renders/review/House_A_Eye_v1/04_pencere.png --grid 6
"""

import argparse
import os
import sys

import bpy
import numpy as np

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz          # noqa: E402


def load_srgb(path):
    """PNG → (h, w, 3) sRGB 0-255 dizi. Satır 0 = ÜST (görüntü sırası)."""
    img = bpy.data.images.load(os.path.abspath(path))
    w, h = img.size
    buf = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(buf)
    # Blender piksel tamponu ALTTAN yukari dizilir; goruntu sirasina cevir.
    # Bu ceviriyi atlamak, "ust kat" diye olculen bolgenin aslinda subasman
    # olmasi demektir — daha once tam olarak bu tur bir eksen hatasi yasandi.
    a = buf.reshape(h, w, 4)[::-1, :, :3]
    return np.clip(a, 0.0, 1.0) * 255.0


def describe(a):
    flat = a.reshape(-1, 3)
    m = flat.mean(axis=0)
    lum = 0.2126 * m[0] + 0.7152 * m[1] + 0.0722 * m[2]
    rg = m[0] / m[1] if m[1] > 1e-3 else float("inf")
    sat = (m.max() - m.min()) / m.max() if m.max() > 1e-3 else 0.0
    # Standart sapma DOKUNUN VAR OLUP OLMADIGININ olcusudur. Duz renk bir yuzey
    # ile dokulu bir yuzey ayni ORTALAMAYI verebilir; ayirt eden sey sapmadir.
    # Bu sutun olmadan "doku uygulandi mi" sorusu goz karariyla cevaplanir.
    std = float(flat.mean(axis=1).std())
    return m, lum, rg, sat, std


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--in", dest="src", required=True)
    ap.add_argument("--grid", type=int, default=0, help="NxN bolge ortalamasi")
    ap.add_argument("--rect", nargs=4, type=float, action="append", default=[],
                    metavar=("X0", "Y0", "X1", "Y1"),
                    help="Normalize dikdortgen (0-1, sol-ust orijin)")
    args = ap.parse_args(hz.argv_after_dashes())

    a = load_srgb(args.src)
    h, w, _ = a.shape
    hz.log(f"{os.path.basename(args.src)}: {w}x{h}")

    if args.grid > 0:
        n = args.grid
        hz.log(f"--- {n}x{n} bolge ortalamasi (sRGB, satir 0 = UST) ---")
        for r in range(n):
            cells = []
            for c in range(n):
                y0, y1 = r * h // n, (r + 1) * h // n
                x0, x1 = c * w // n, (c + 1) * w // n
                m, lum, _, _, std = describe(a[y0:y1, x0:x1])
                cells.append(f"{int(m[0]):3d},{int(m[1]):3d},{int(m[2]):3d}"
                             f"|L{int(lum):3d}|s{std:4.1f}")
            hz.log(f"  r{r}: " + "  ".join(cells))

    for x0, y0, x1, y1 in args.rect:
        c0, c1 = int(x0 * w), int(x1 * w)
        r0, r1 = int(y0 * h), int(y1 * h)
        m, lum, rg, sat, std = describe(a[r0:r1, c0:c1])
        hz.log(f"rect({x0:.2f},{y0:.2f},{x1:.2f},{y1:.2f}) px[{c0}:{c1},{r0}:{r1}] "
               f"RGB={int(m[0])},{int(m[1])},{int(m[2])} parlaklik={lum:.1f} "
               f"R/G={rg:.2f} doygunluk={sat:.2f} sapma={std:.2f}")


if __name__ == "__main__":
    main()
