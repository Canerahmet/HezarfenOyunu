"""
Hezarfen: 1632 - Cevahir (Ic) Bedesteni ve Sandal Bedesteni.

Ayrinti ve gerekce: `lib/bedesten_kit.py`, RESEARCH.md 5.18, ADR 0053.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_bedesten.py -- \
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
import bedesten_kit as bk          # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE_BASE = (
    "Bedesten — Fatih Sultan Mehmed vakfi, fetihten kisa sure sonra "
    "(~1461). 1632'de ayakta. "
    "DOGRULUK BASAMAGI **D2**: plan olculeri, kubbe ve ayak sayilari "
    "belgelidir. "
    "**SAYILAR BIRBIRINI KAPATIYOR VE BU TESADUF DEGIL.** Bedesten bir "
    "IZGARADIR: kubbeler sira sira dizilir, ayaklar izgaranin IC "
    "dugumlerinde durur. Yani kubbe = sutun x satir, ayak = "
    "(sutun-1) x (satir-1). Kaynaklar ucunu de AYRI AYRI verir ve ucu de "
    "tutar: Cevahir 15 kubbe / 8 ayak / 5x3; Sandal 20 kubbe / 12 ayak / "
    "5x4. Dahasi izgara OLCUYLE de tutar — Cevahir'in gozu 9,06 x 9,83 m, "
    "Sandal'inki 8,00 x 8,00 m; ikisi de kareye yakin, ki kubbeli bir goz "
    "zaten kare ister. Projede ILK KEZ uc bagimsiz sayi bir geometriyi "
    "kapatiyor ve dogrulama iliskiyi denetliyor. "
    "**1632'DE KAPALICARSI BU DEGILDIR**: bugun akla gelen kagir tonozlu "
    "sokaklar agi SONRADIR. 17. yuzyilda bedestenlerin arasindaki sokaklar "
    "AHSAP ortuluydu; bugunku kagir ortu buyuk yangınlardan (1701) ve 1894 "
    "depreminden sonraki onarimlarin eseridir. Ustelik **1618 YANGINI** "
    "1632'den yalnizca ON DORT yil oncedir: oyunun gectigi yilda carsi "
    "YAKIN ZAMANDA YENIDEN KURULMUS bir yerdir. Bu yuzden yalnizca IKI "
    "BEDESTEN uretildi — onlar kagirdir, olculudur ve 1632'de ayaktadir; "
    "cevredeki carsi dokusu Faz 4'un isi. "
    "BEDESTENIN DORT KAPISI vardir ve kapaliligi onun TANIMIDIR: kiymetli "
    "mal saklanan, gece kilitlenen yerdir. "
    "Kaynaklar: Vikipedi 'Sandal Bedesteni'; Discover Islamic Art "
    "(Kapalicarsi); Osmanli Tarihi Ansiklopedisi 'Kapali Carsi'. "
    "RESEARCH.md 5.18, ADR 0053"
)

VARIANTS = (
    dict(asset="CevahirBedesteni", w=bk.CEV_W, d=bk.CEV_D,
         cols=bk.CEV_COLS, rows=bk.CEV_ROWS, crown=bk.CEV_CROWN,
         note="CEVAHIR (IC / ESKI) BEDESTEN: 45,30 x 29,50 m, ON BES kubbe, "
              "SEKIZ ayak (iki sira), kubbe kilidi **14,89 m** — hepsi "
              "OLCULU. Kiymetli mal (cevahir) burada saklanirdi."),
    dict(asset="SandalBedesteni", w=bk.SAN_W, d=bk.SAN_D,
         cols=bk.SAN_COLS, rows=bk.SAN_ROWS, crown=bk.SAN_CROWN,
         note="SANDAL BEDESTENI (Yeni Bedesten): 40 x 32 m, YIRMI kubbe, "
              "ON IKI ayak (uc sira) — plan OLCULU. Kubbe kilidi kaynakta "
              "YOK; Cevahir'in kilit/goz oranindan (14,89 / 9,44 = 1,58) "
              "turedi ve **D3**'tur. Adi 'sandal' denen ipekli kumastan "
              "gelir. Tarihi Fatih donemine, Edirne ve Bursa "
              "bedestenlerine benzerliginden verilir."),
)


def add_args(p):
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None)
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())
    infos = []

    for v in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        p = bk.BedestenParams(v["w"], v["d"], v["cols"], v["rows"],
                              v["crown"], palette=a.palette)
        lod0, lod1, ucx, info = bk.build_bedesten(p, col, v["asset"],
                                                  textured=a.textured)

        # IZGARA ILISKISI: uc sayi birbirini kapatmali.
        if info["domes"] != info["cols"] * info["rows"]:
            raise SystemExit("[HZ] HATA: kubbe = sutun x satir olmali.")
        if info["piers"] != (info["cols"] - 1) * (info["rows"] - 1):
            raise SystemExit("[HZ] HATA: ayak = (sutun-1) x (satir-1) olmali.")
        if info["doors"] != 4:
            raise SystemExit("[HZ] HATA: bedestenin DORT kapisi var.")
        if abs(info["pivot_min_z"]) > 0.01:
            raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")

        hz.log(f"{v['asset']}: {info['width']:.2f} x {info['depth']:.2f} m, "
               f"{info['cols']}x{info['rows']} izgara -> {info['domes']} "
               f"kubbe / {info['piers']} ayak (SAYILANLA TUTUYOR)")
        hz.log(f"  goz {info['bay_w']:.2f} x {info['bay_d']:.2f} m "
               f"(kareye yakin), duvar {info['wall_h']:.2f} m, kilit "
               f"{info['dome_crown_z']:.2f} m, LOD0={info['tris_lod0']}")

        info.update(name=v["asset"], prefab=f"PF_{v['asset']}", tier="T1",
                    source=SOURCE_BASE + " | " + v["note"])
        infos.append(info)

        if a.blend_dir:
            os.makedirs(a.blend_dir, exist_ok=True)
            hz.save_blend(os.path.join(a.blend_dir, f"{v['asset']}.blend"))
        if a.out_dir:
            os.makedirs(a.out_dir, exist_ok=True)
            export_fbx(os.path.join(a.out_dir, f"SM_{v['asset']}.fbx"),
                       collection_name=COLLECTION)

    if a.catalog:
        os.makedirs(os.path.dirname(os.path.abspath(a.catalog)), exist_ok=True)
        cat = {"variants": []}
        if os.path.exists(a.catalog):
            with open(a.catalog, encoding="utf-8") as fh:
                cat = json.load(fh)
        names = {i["name"] for i in infos}
        rest = [v for v in cat.get("variants", []) if v.get("name") not in names]
        rest += infos
        rest.sort(key=lambda v: v["name"])
        with open(a.catalog, "w", encoding="utf-8") as fh:
            json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
        hz.log(f"katalog: {a.catalog} ({len(rest)} kayit)")

    hz.log("gen_bedesten OK")


if __name__ == "__main__":
    main()
