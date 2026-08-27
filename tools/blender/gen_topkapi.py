"""
Hezarfen: 1632 — Topkapi Sarayi siluetinin belirleyici parcalari.

Ayrinti ve gerekce: `lib/saray_kit.py`, RESEARCH.md 5.7, ADR 0040.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_topkapi.py -- \
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
import saray_kit as srk            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SRC_KULE = (
    "Topkapi Sarayi ADALET KULESI, 1632 hali. Fatih'in duzeninde tasarlandi; "
    "KANUNI 1527-29'da tas bolumu ekletti ve Kubbealti'na bakan HUNKAR "
    "PENCERESI acildi — padisah divani bu kafesli pencereden izler. "
    "1632'DEKI BICIM: UC tas kat + AHSAP bir ust kat + KURSUN kapli "
    "PIRAMIDAL kulah. "
    "1632'DE YOK: II. MAHMUD (1819-20) bir tas kat daha ekletti, ustune "
    "ahsap bir seyir bolumu koydurdu ve kursun kulahi yukseltti; "
    "ABDULAZIZ bugunku YUKSEK ve SIVRI kulahi verdi. Yani bugun fotografta "
    "gorulen kule 19. yuzyildir ve 1632 kulesi ondan ALCAKTIR — Galata "
    "Kulesi'ndekiyle ayni hata ailesi (ADR 0033). "
    "OLCU YOK: kutle **D3**; sayilan deger UC tas kattir ve geometriyi "
    "baglar. RESEARCH.md 5.7, ADR 0040"
)

SRC_KAPI = (
    "Topkapi Sarayi BABUSSELAM (Orta Kapi) — CIFTE KONIK KULAHLI kapi. "
    "Kuleler 1632'de VARDIR; tartisma yalnizca kimin eklediginedir: "
    "Gulru Necipoglu kulelerin FATIH doneminde duvarlarla birlikte "
    "yapildigini dusunur, yaygin gorus KANUNI'nin Avrupa seferindeki "
    "gozlemlerine baglar. Iki ihtimal de 1632'den oncedir, yani soru "
    "modeli etkilemez. "
    "Kapi ikinci avluya (Divan Meydani) acilir ve saraya girerken atindan "
    "inilmeyen tek kisi padisahtir. "
    "OLCU YOK: kutle **D3**; sayilan deger IKI kuledir. "
    "RESEARCH.md 5.7, ADR 0040"
)


def add_args(p):
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None)
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def _finish(a, asset, info, source):
    info.update(name=asset, prefab=f"PF_{asset}", tier="T1", source=source)
    os.makedirs(os.path.dirname(os.path.abspath(a.catalog)), exist_ok=True)
    cat = {"variants": []}
    if os.path.exists(a.catalog):
        with open(a.catalog, encoding="utf-8") as fh:
            cat = json.load(fh)
    rest = [v for v in cat.get("variants", []) if v.get("name") != asset]
    rest.append(info)
    rest.sort(key=lambda v: v["name"])
    with open(a.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
    hz.log(f"katalog: {a.catalog} ({len(rest)} kayit)")
    if a.blend_dir:
        os.makedirs(a.blend_dir, exist_ok=True)
        hz.save_blend(os.path.join(a.blend_dir, f"{asset}.blend"))
    if a.out_dir:
        os.makedirs(a.out_dir, exist_ok=True)
        export_fbx(os.path.join(a.out_dir, f"SM_{asset}.fbx"),
                   collection_name=COLLECTION)


def build_kule(a):
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    p = srk.AdaletKulesiParams(palette=a.palette)
    lod0, lod1, ucx, info = srk.build_adalet_kulesi(p, col, "TopkapiAdaletKulesi",
                                                    textured=a.textured)
    if info["stone_tiers"] != srk.ADALET_TAS_KAT:
        raise SystemExit("[HZ] HATA: 1632 kulesi UC tas katlidir; dorduncu "
                         "kat II. Mahmud'un (1819-20) eklemesidir.")
    # BELGELI YON: hunkar penceresi KUBBEALTI'na bakar ve Kubbealti
    # ikinci avlunun kuzeybati kosesinde, kulenin hemen batisindadir.
    # Egimden turetilemez.
    info["face_deg"] = 270.0
    hz.log(f"TopkapiAdaletKulesi: {info['stone_tiers']} tas kat, toplam "
           f"{info['height']:.2f} m, ayak izi {info['footprint_x']:.1f} m, "
           f"LOD0={info['tris_lod0']}")
    _finish(a, "TopkapiAdaletKulesi", info, SRC_KULE)
    return info


def build_kapi(a):
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    p = srk.BabusselamParams(palette=a.palette)
    lod0, lod1, ucx, info = srk.build_babusselam(p, col, "TopkapiBabusselam",
                                                 textured=a.textured)
    if info["towers"] != srk.BABUSSELAM_KULE:
        raise SystemExit("[HZ] HATA: Babusselam IKI kulelidir.")
    # BELGELI YON: kapi BIRINCI avludan IKINCI avluya acilir; birinci avlu
    # GUNEYDEDIR. Kulenin konumundan olculdu: kapi -> kule yonu 9 derece,
    # yani kapinin on cephesi 189 derece. Egim onu batiya donduruyordu.
    info["face_deg"] = 189.0
    hz.log(f"TopkapiBabusselam: {info['towers']} kule, toplam "
           f"{info['height']:.2f} m, cephe {info['footprint_x']:.1f} m, "
           f"LOD0={info['tris_lod0']}")
    _finish(a, "TopkapiBabusselam", info, SRC_KAPI)
    return info


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())
    k = build_kule(a)
    g = build_kapi(a)
    # Siluet kurali: Adalet Kulesi sarayin EN YUKSEK ogesidir.
    if k["height"] <= g["height"]:
        raise SystemExit(f"[HZ] HATA: Adalet Kulesi {k['height']:.1f} m, "
                         f"Babusselam {g['height']:.1f} m — kule kapidan "
                         "YUKSEK olmali, yoksa siluette kaybolur.")
    hz.log(f"siluet OK: kule kapidan {k['height'] - g['height']:.2f} m yuksek")
    hz.log("gen_topkapi OK")


if __name__ == "__main__":
    main()
