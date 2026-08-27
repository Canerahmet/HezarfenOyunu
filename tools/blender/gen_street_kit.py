"""
Hezarfen: 1632 — Sokak donatısı üreticisi: çeşme ve dükkân (plan Faz 2b).

Tek koşuşta birkaç varyant üretir ve kataloğa yazar; Unity yerleştiricisi
ölçüleri oradan okur (ev kitiyle aynı sözleşme).

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_street_kit.py --
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import street_kit as sk            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

# Tarihsel kademe ve kaynak notu -> Unity `HistoricalTag`. Boru hatti prefabi
# her kosuşta yeniden yazar; etiket elle konursa ilk yeniden uretimde sessizce
# kaybolur. Bu yuzden kaynak katalogdur.
TIERS = {
    "cesme": ("T2",
              "Klasik duvar cesmesi tipolojisi: sivri kemerli nis, ayna tasi, "
              "teknelik, kitabe, silme. Kirkcesme sistemi 1563'ten beri faal "
              "(RESEARCH.md 3-Su yapilari). Olculer rekonstruksiyon."),
    "dukkan": ("T2",
               "Arasta birimi: kepenk alt kanat tezgah, ust kanat sundurma. "
               "Sira dukkan bir sokak tipolojisidir. RESEARCH.md 4.1."),
    "avlu_duvar": ("T2", "Avlu duvari + harpusta. Cami avlusu ve sinagog avlusu icin."),
    "avlu_kapi": ("T2", "Kemerli avlu kapisi — cesmeyle ayni iki merkezli sivri kemer."),
    "sadirvan": ("T2",
                 "Cami avlusunda cok muslukli, catili sadirvan. "
                 "PLAN.md 7.1 Su ve donati."),
}

# (ad, tur, gerekce, parametreler)
VARIANTS = [
    ("Cesme_A", "cesme", "mahalle cesmesi — sokak kosesinde, tek lule",
     dict(width=2.9, height=3.7, niche_w=1.35)),
    ("Cesme_B", "cesme", "dar cesme — cikmaz basinda, kanatsiz (yapiya bitisik)",
     dict(width=2.2, height=3.2, niche_w=1.05, spring_z=1.6, kitabe=False,
          wings=0.0)),
    ("Cesme_C", "cesme", "genis kitabeli cesme — ana sokak, vakif eseri",
     dict(width=3.6, height=4.2, niche_w=1.6, spring_z=1.9, depth=1.2)),
    ("Dukkan_A", "dukkan", "kepenkli dukkan — arasta birimi",
     dict(width=3.4, depth=4.0)),
    ("Dukkan_B", "dukkan", "dar dukkan, sundurmasiz",
     dict(width=2.6, depth=3.4, open_w=1.7, awning=False)),
    ("Dukkan_C", "dukkan", "ust katli dukkan — alt tezgah, ust konut",
     dict(width=3.8, depth=4.2, open_w=2.5, upper_floor=True)),
    ("AvluDuvar", "avlu", "avlu duvari parcasi — harpustali, 4 m",
     dict(length=4.0, height=1.85)),
    ("AvluDuvarKisa", "avlu", "kisa duvar parcasi — kose dolgusu",
     dict(length=2.0, height=1.85)),
    ("AvluKapi", "avlu", "kemerli avlu kapisi — cesmeyle AYNI kemer",
     dict(length=3.4, gate=True, gate_w=1.7, gate_h=3.5, spring_z=2.05)),
    ("Sadirvan", "sadirvan", "avlu ortasinda abdest sadirvani",
     dict(radius=1.75)),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "street"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "street",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    for name, kind, why, params in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)

        tex = not args.no_textures
        if kind == "cesme":
            lod0, lod1, ucx, info = sk.build_cesme(
                sk.CesmeParams(**params), col, name, textured=tex)
        elif kind == "dukkan":
            lod0, lod1, ucx, info = sk.build_dukkan(
                sk.DukkanParams(**params), col, name, textured=tex)
        elif kind == "avlu":
            lod0, lod1, ucx, info = sk.build_avlu(
                sk.AvluParams(**params), col, name, textured=tex)
        elif kind == "sadirvan":
            lod0, lod1, ucx, info = sk.build_sadirvan(
                sk.SadirvanParams(**params), col, name, textured=tex)
        else:
            raise SystemExit(f"[HZ] bilinmeyen tur: {kind}")

        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {name}: pivot taban merkezde degil "
                             f"({info['pivot_min_z']})")

        hz.save_blend(os.path.join(args.blend_dir, f"SM_{name}.blend"))
        export_fbx(os.path.join(args.out_dir, f"SM_{name}.fbx"),
                   collection_name=COLLECTION)

        tier, source = TIERS[info["kind"]]
        info.update(name=name, why=why, prefab=f"PF_{name}",
                    tier=tier, source=source)
        catalog.append(info)
        hz.log(f"{name:12s} {info['footprint_x']:5.2f}x{info['footprint_y']:5.2f}"
               f"x{info['height']:5.2f} m  {info['tris_lod0']:5d} ucgen  {why}")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} donati; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
