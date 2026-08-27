"""
Hezarfen: 1632 — Galata Kulesi üreticisi (plan Faz 3, S-kademe).

Faz 3'ün ilk landmark'ı ve oyunun **dünya orijini** (ADR 0007). Ölçüler
RESEARCH.md §5.1'de kaynaklıdır; kitin kendisi `lib/tower_kit.py`.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_galata_kulesi.py -- \
      --asset GalataKulesi --textured --crown sacakli \
      --out-blend art/blend/landmark/SM_GalataKulesi.blend \
      --out-fbx  unity/HezarfenGame/Assets/_Import/SM_GalataKulesi.fbx
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import tower_kit as tk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

#: Landmark kaynak notu — **doğruluk basamağı yazılı** (PLAN §8.1 kuralı).
#: Bir landmark D1 iddia ediyorsa kamu malı ölçülü çizimi olmalı; yoksa D2'ye
#: düşer. Burada ölçüler var ama ölçülü ÇİZİM yok, ve külahın biçimi
#: tasvirlerden bile kesinleşmiyor.
SOURCE = (
    "Galata Kulesi, 1632. **D2** (gorsel/olcu cikarimi; olculu cizim YOK). "
    "Dis cap 16,45 m, ic cap 8,95 m, duvar 3,75 m — TDV. Kagir govde ~34,5 m: "
    "1831'de yikilan 32,60 m kotu + 1794'te alcaltilan 1,90 m "
    "(T.C. Kultur ve Turizm Bakanligi). Tugla kusaklar 13,20 ve 17,17 m'de; "
    "ilki 1509 depremi sonrasi Mimar Murad bin Hayreddin onariminin dikisi. "
    "Kursun kapli KULAH cagdas taniktir (Evliya Celebi) — Evliya'nin 118 arsin "
    "boy sayisi kullanilmadi (~89 m eder). Kulahin BICIMI D3: Saglam'in "
    "karsilastirdigi iki tasvirden hangisinin 1632'ye ait oldugu kesin degil. "
    "1632'de YOK: 1831 sofasi ve demir korkuluklu balkon, 1875 sekizgen iki "
    "gozlem kati, 1832 kapi kitabesi, 1794 Ampir cumbalar, Ceneviz haci/kuresi, "
    "yangin gozetleme islevi. Islev: tersane levazim ambari ve zindan. "
    "RESEARCH.md 5.1"
)


def add_args(p):
    p.add_argument("--asset", default="GalataKulesi")
    p.add_argument("--crown", default="sacakli",
                   choices=("sacakli", "mazgalli"),
                   help="kulah bicimi: sacagi mazgali orten / mazgal icinden")
    p.add_argument("--shaft-h", type=float, default=tk.SHAFT_H_1632)
    p.add_argument("--cone-h", type=float, default=None)
    p.add_argument("--parapet-h", type=float, default=1.70)
    p.add_argument("--merlons", type=int, default=24)
    p.add_argument("--segments", type=int, default=32)
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--info-json", default=None)
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = tk.GalataParams(crown=a.crown, shaft_h=a.shaft_h, cone_h=a.cone_h,
                        parapet_h=a.parapet_h, merlon_n=a.merlons,
                        segments=a.segments, palette=a.palette)
    lod0, lod1, ucx, info = tk.build_galata(p, col, a.asset,
                                            textured=a.textured)

    hz.log(f"{a.asset}: tac={info['crown']}, kagir govde "
           f"{info['shaft_h']:.2f} m, kulah {info['cone_h']:.2f} m")
    hz.log(f"ayak izi {info['footprint_x']:.2f}x{info['footprint_y']:.2f} m, "
           f"yukseklik {info['height']:.2f} m "
           f"(bugunku kule {info['today_total_h']} m)")
    hz.log(f"ucgen LOD0={info['tris_lod0']} LOD1={info['tris_lod1']}")

    # DIS CAP BELGELIDIR — modelin onu tasidigi burada olculur, varsayilmaz.
    #
    # Olculen sey GOVDE capidir, ayak izi degil: sacakli varyantta kulah
    # saciyor ve ayak izi 18,35 m cikiyor. Ilk yazimda ayak izini olcuyordum
    # ve denetim haksiz yere hata verdi — kendi aletim yanlis seyi olcuyordu.
    if abs(info["shaft_d"] - tk.OUTER_D) > 0.05:
        raise SystemExit(f"[HZ] HATA govde capi {info['shaft_d']:.2f} — "
                         f"belgeli dis cap {tk.OUTER_D} m")
    if info["footprint_x"] < info["shaft_d"] - 0.01:
        raise SystemExit("[HZ] HATA ayak izi govdeden kucuk — imkansiz")
    if abs(info["pivot_min_z"]) > 1e-3:
        raise SystemExit(f"[HZ] HATA pivot taban merkezde degil: "
                         f"min_z={info['pivot_min_z']}")
    hz.log("pivot OK, dis cap OK")

    info.update(name=a.asset, prefab=f"PF_{a.asset}", tier="T1",
                accuracy="D2", source=SOURCE)

    if a.info_json:
        os.makedirs(os.path.dirname(os.path.abspath(a.info_json)), exist_ok=True)
        with open(a.info_json, "w", encoding="utf-8") as fh:
            json.dump(info, fh, ensure_ascii=False, indent=1)

    if a.catalog:
        # Katalog BIRLESTIRILIR: bu script her kosuşta tek varyant uretir.
        os.makedirs(os.path.dirname(os.path.abspath(a.catalog)), exist_ok=True)
        cat = {"variants": []}
        if os.path.exists(a.catalog):
            with open(a.catalog, encoding="utf-8") as fh:
                cat = json.load(fh)
        rest = [v for v in cat.get("variants", []) if v.get("name") != a.asset]
        rest.append(info)
        rest.sort(key=lambda v: v["name"])
        with open(a.catalog, "w", encoding="utf-8") as fh:
            json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
        hz.log(f"katalog: {a.catalog} ({len(rest)} kayit)")

    if a.out_blend:
        hz.save_blend(a.out_blend)
    if a.out_fbx:
        export_fbx(a.out_fbx, collection_name=COLLECTION)
    hz.log("gen_galata_kulesi OK")


if __name__ == "__main__":
    main()
