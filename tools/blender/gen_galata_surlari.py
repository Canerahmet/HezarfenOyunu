"""
Hezarfen: 1632 — Galata surlarının burcu ve kapısı (plan Faz 3, S-kademe).

Perde duvarın kendisi 2,5 km'lik bir hattır ve Unity tarafında GIS hattı
boyunca tek mesh olarak üretilir; bu betik **sayılı yapıları** üretir.
Ölçüler ve gerekçe: `lib/wall_kit.py`, RESEARCH.md §5.2.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_galata_surlari.py -- \
      --textured --out-dir unity/HezarfenGame/Assets/_Import
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import wall_kit as wk              # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE_COMMON = (
    "Galata surlari, 1632. Cenevizlilerce 1335-1349 arasinda yapildi; "
    "1864'e kadar EKSIKSIZ ayakta, yani 1632'de tam. Duvar kalinligi ~2 m; "
    "cevre 2800 m, alan ~37 ha; kara tarafinda 15 m genisliginde HENDEK "
    "(Eyice 1969; Incicyan 1976). Galata Kulesi surun BAS KULESI: yaklasik "
    "16 m dis cap. "
    "OLCULER RÖLÖVEDEN: Erdogan, Batuhan Burhan (2013), 'Galata Kent Surlari "
    "ve Koruma Onerileri', YL tezi, ITU FBE, dan. Zeynep Ahunbay — 2010 arazi "
    "olcumleri. "
    "1632'de YOK: 1864 yikimi ve hendeklerin doldurulmasi; ayakta kalan "
    "parcalarin bugunku harap hali. Dogruluk basamagi: D2 (olculu "
    "rolove; ozgun yapinin olculu cizimi degil). RESEARCH.md 5.2"
)

VARIANTS = [
    dict(name="SurBurcu", kind="burc",
         params=dict(width=9.80, depth=7.70, height=16.16),
         note="U PLANLI burc: arkasi dikdortgen, one bakan yuzu DAIRESEL. "
              "Olculer 16 no'lu burctan: 9,80 x 7,70 m, zeminden 16,16 m, "
              "kaba yonu tas yigma (Erdogan 2013)."),
    dict(name="SurBurcu_Kucuk", kind="burc",
         params=dict(width=7.02, depth=5.84, height=10.0),
         note="Kucuk U planli burc; olculer 9 no'lu burctan: 7,02 x 5,84 m, "
              "zeminden ~10 m (Erdogan 2013). Galata burclari tek boyda "
              "degildir."),
    dict(name="SurBurcu_Dortgen", kind="burc",
         params=dict(width=8.0, depth=6.4, height=14.0, plan="dortgen"),
         note="DORTGEN burc. Tez iki tipi birden belgeliyor: 'Galata Surlari "
              "belirli araliklarla insa edilmis DORTGEN VE U PLANLI burclar "
              "ile guclendirilmistir.' Bugune kalan iki ornek U planli; bu, "
              "karenin olmadigi anlamina gelmez — hayatta kalan ornek orneklem "
              "degildir. Olcusu YOK (D3), kutle U planli buyuk burctan "
              "turetildi."),
    dict(name="SurKapisi", kind="kapi",
         params=dict(),
         note="Kemerli gecit. Olculer Harup Kapi rolovesinden: aciklik "
              "2,70 m, kemere yukseklik 4,60 m, kemer uzengi kotu 3,60 m, "
              "kapi ustunde sur yuksekligi 6,50 m, kesit genisligi 1,80 m; "
              "kemer orgusunde bir sira tas iki sira tugla (Erdogan 2013). "
              "Galata kapilari kaynakta ADLARIYLA anilir (Azapkapi, Kule "
              "Kapisi, Karakoy, Balikpazari, Yagkapani, Kurkcukapi, "
              "Kursunlumahzen)."),
]


def add_args(p):
    p.add_argument("--out-dir", default=None)
    p.add_argument("--wall-h", type=float, default=wk.WALL_H,
                   help="Perde duvarin yerel zeminden yuksekligi (m, olculu)")
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())

    entries = []
    for v in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)

        if v["kind"] == "burc":
            p = wk.BurcParams(wall_h=a.wall_h, palette=a.palette, **v["params"])
            lod0, lod1, ucx, info = wk.build_burc(p, col, v["name"],
                                                  textured=a.textured)
        else:
            p = wk.KapiParams(wall_h=a.wall_h, palette=a.palette, **v["params"])
            lod0, lod1, ucx, info = wk.build_kapi(p, col, v["name"],
                                                  textured=a.textured)

        hz.log(f"{v['name']}: ayak izi {info['footprint_x']:.2f}x"
               f"{info['footprint_y']:.2f} m, yukseklik {info['height']:.2f} m "
               f"(duvar {a.wall_h:.1f} m), LOD0={info['tris_lod0']}")
        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA pivot taban merkezde degil: "
                             f"{info['pivot_min_z']}")

        info.update(name=v["name"], prefab=f"PF_{v['name']}", tier="T1",
                    palette=a.palette,
                    source=SOURCE_COMMON + " | " + v["note"])
        entries.append(info)

        blend = os.path.join("art", "blend", "landmark", f"SM_{v['name']}.blend")
        hz.save_blend(blend)
        if a.out_dir:
            export_fbx(os.path.join(a.out_dir, f"SM_{v['name']}.fbx"),
                       collection_name=COLLECTION)

    if a.catalog:
        os.makedirs(os.path.dirname(os.path.abspath(a.catalog)), exist_ok=True)
        cat = {"variants": []}
        if os.path.exists(a.catalog):
            with open(a.catalog, encoding="utf-8") as fh:
                cat = json.load(fh)
        names = {e["name"] for e in entries}
        rest = [x for x in cat.get("variants", []) if x.get("name") not in names]
        rest.extend(entries)
        rest.sort(key=lambda x: x["name"])
        with open(a.catalog, "w", encoding="utf-8") as fh:
            json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
        hz.log(f"katalog: {a.catalog} ({len(rest)} kayit)")

    hz.log("gen_galata_surlari OK")


if __name__ == "__main__":
    main()
