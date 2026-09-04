# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — LOD'lar arasinda UV yogunlugu farkini olcer.

## Neden

Galata sokagi karesinde uzaktaki bir cati kirmizi benek yigini olarak
cikti. Once "LOD taramasi" sanildi ve `tarama_gurultusu.py` bunu eledi:
duzen yok, yani tarama degil, DOKU gurultusu. Geriye tek makul sebep
kaldi — o mesafede cizilen LOD'un doku yogunlugu LOD0'inkinden farkli.
UV'ler dunya olcegiyle uretiliyor (`ottoman_kit.apply_uvs`) ama bu her
LOD icin ayri ayri cagrilmak zorunda; cagrilmazsa LOD1 varsayilan
0..1 UV'siyle kalir ve ayni doku metrelerce yerine santimetrelerce
tekrarlanir. Uzaktan bakildiginda bunun adi benektir.

## Nasil olcer

Her nesne icin, her malzeme yuzu basina:

    yogunluk = sqrt(UV alani) / sqrt(dunya alani)     [tekrar / metre]

LOD0 ile LOD1'in ayni malzemedeki yogunluklari karsilastirilir. Saglikli
durumda oran 1'e yakindir (LOD sadeleserek biraz kayar). 2 katini asan
oran bir kusurdur.

## Kullanim

    blender --background --python tools/olcum/uv_yogunlugu.py -- <blend...>
"""

import math
import os
import sys

import bpy

#: Bunun uzerindeki oran kusur sayilir. LOD sadelestirmesi %30-40 kaydirabilir;
#: 2 kat ayri bir sebep ister.
SINIR = 2.0


def _yogunluk(obj):
    """Malzeme adi -> (tekrar/metre) sozlugu."""
    me = obj.data
    me.calc_loop_triangles()
    uvs = me.uv_layers.active
    if uvs is None:
        return {}
    mats = [m.name if m else "-" for m in me.materials] or ["-"]
    top = {}
    for tri in me.loop_triangles:
        ad = mats[min(tri.material_index, len(mats) - 1)]
        v = [me.vertices[i].co for i in tri.vertices]
        d_alan = (v[1] - v[0]).cross(v[2] - v[0]).length * 0.5
        u = [uvs.data[i].uv for i in tri.loops]
        u_alan = abs((u[1] - u[0]).cross(u[2] - u[0])) * 0.5
        d, uu = top.setdefault(ad, [0.0, 0.0])
        top[ad] = [d + d_alan, uu + u_alan]
    return {a: (math.sqrt(uu) / math.sqrt(d) if d > 1e-9 else 0.0)
            for a, (d, uu) in top.items()}


def olc(yol):
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(yol))
    lod = {}
    for o in bpy.data.objects:
        if o.type != "MESH":
            continue
        if "_LOD0" in o.name:
            lod.setdefault(0, {}).update(_yogunluk(o))
        elif "_LOD1" in o.name:
            lod.setdefault(1, {}).update(_yogunluk(o))
    ad = os.path.basename(yol)
    if 0 not in lod or 1 not in lod:
        print(f"[HZ] {ad}: LOD0/LOD1 ciftini bulamadim")
        return 0
    kusur = 0
    for mat, y0 in sorted(lod[0].items()):
        y1 = lod[1].get(mat)
        if y1 is None or y0 <= 1e-9 or y1 <= 1e-9:
            continue
        oran = max(y0, y1) / min(y0, y1)
        isaret = "KUSUR" if oran >= SINIR else "ok"
        if oran >= SINIR:
            kusur += 1
        print(f"[HZ] {ad:26} {mat:22} LOD0={y0:7.3f} LOD1={y1:7.3f} "
              f"oran={oran:5.2f}  {isaret}")
    return kusur


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if not argv:
        print("[HZ] kullanim: ... uv_yogunlugu.py -- <blend...>")
        return
    toplam = sum(olc(y) for y in argv)
    print(f"[HZ] toplam {toplam} kusurlu malzeme (oran >= {SINIR:.1f})")


if __name__ == "__main__":
    main()
