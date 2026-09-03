"""
Hezarfen: 1632 — **Hangi kanonik varlık dokusuz?**

## Neden bu ölçü

Galata Kulesi'nin inceleme karesinde önce çarpıştırıcı modelin üstüne
çiziliyordu; o düzeltilince altından ikinci bir şey çıktı: kulede
**hiç doku yoktu.** Ölçüldü — `SM_GalataKulesi.blend` içinde sıfır
görüntü, beş malzemenin beşinde sıfır doku düğümü. `--textured`
verilmeden kurulmuş.

Oyun bundan etkilenmiyor: Unity malzemeleri `OttomanMaterialBuilder`
ile paletten yeniden kuruluyor. **İnceleme** etkileniyor, ve bu deponun
temel döngüsü inceleme karesine bakmak. Dokusuz incelenen bir yüzeyde
yüzey kusuru görünmez — kanat dokusunun kereste olduğu tam bu yüzden
turlarca fark edilmedi.

Bu yüzden soru bir kez sorulup unutulmamalı: **hangi kanonik `.blend`
dosyası dokusuz?**

Kullanım:
  blender --background --python tools/olcum/blend_dokusu.py
  blender --background --python tools/olcum/blend_dokusu.py -- --kok art/blend
"""

import argparse
import glob
import os
import sys

import bpy


def _argv():
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    return []


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--kok", default=os.path.join("art", "blend"))
    a = ap.parse_args(_argv())

    yollar = sorted(glob.glob(os.path.join(a.kok, "**", "*.blend"),
                              recursive=True))
    dokusuz, dokulu = [], 0
    for y in yollar:
        try:
            bpy.ops.wm.open_mainfile(filepath=y)
        except Exception as e:                      # noqa: BLE001
            print(f"[HZ] ACILAMADI {y}: {e}")
            continue
        if len(bpy.data.images) == 0:
            dokusuz.append((y, len(bpy.data.materials)))
        else:
            dokulu += 1

    for y, m in dokusuz:
        print(f"[HZ] DOKUSUZ {m:3d} malzeme  {y}")
    print(f"[HZ] {len(dokusuz)} dokusuz / {len(yollar)} blend "
          f"({dokulu} dokulu)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
