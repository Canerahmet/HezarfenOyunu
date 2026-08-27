"""
Hezarfen: 1632 - Padisah turbeleri (Ayasofya haziresi + Sultanahmet).

Ayrinti ve gerekce: `lib/sultan_turbe_kit.py`, RESEARCH.md 5.19, ADR 0054.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_sultan_turbeleri.py -- \
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
import sultan_turbe_kit as tk      # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE_BASE = (
    "Padisah turbesi — Ayasofya haziresi ve Sultanahmet. "
    "DOGRULUK BASAMAGI **D3**: PLANLAR ve tarihler belgelidir, OLCULER "
    "degil. Haritadaki ayak izleri (24-30 m) revagi ve hazire duvarini da "
    "iceriyor ve ayristirilmadi; govde olculeri tipolojiktir. "
    "**UC TURBE UC AYRI PLAN TASIR** ve kaynaklar bunu ayri ayri soyler: "
    "II. Selim **kare, koseleri pahli** (ici sekizgen galerili); III. "
    "Murad **ALTIGEN**, revakli; III. Mehmed **SEKIZGEN**. Ucunu de duzgun "
    "sekizgen yapmak katalogda tutarli gorunurdu ve UC AYRI PLANI TEK PLANA "
    "INDIRIRDI — Faz 3 boyunca kovalanan hatanin aynisi (yarim kubbe "
    "sayilari, ADR 0048). Kare-pahli plan DUZGUN DEGILDIR (dort uzun, dort "
    "kisa yuz) ve katalog bunu `face_spread` ile olcerek kaydeder. "
    "CIFT KABUK: II. Selim, III. Murad ve III. Mehmed turbelerinin ucu de "
    "cift kubbelidir (Sinan'in Kanuni turbesinde kullandigi ortu). Ic kabuk "
    "disaridan gorunmez ve URETILMEZ — Ayasofya'nin eksedralarinda verilen "
    "kararin aynisi (ADR 0045); katalog `double_shell` diye kaydeder. "
    "**1632'DE AYASOFYA HAZIRESINDE DORT TURBE VARDIR, BES DEGIL**: "
    "I. Mustafa ve Ibrahim turbesi **1639**'dur ve o tarihte vaftizhane "
    "hala YAGHANEDIR (ADR 0045). "
    "Kaynaklar: TDV Islam Ansiklopedisi 'Selim II Turbesi', 'Murad III "
    "Turbesi', 'Mehmed III Turbesi'; IBB Kulturel Miras. "
    "RESEARCH.md 5.19, ADR 0054"
)

VARIANTS = (
    dict(asset="TurbeSelimII", plan="kare_pahli", half=9.0, wall_h=11.5,
         rise=7.0, revak=False, marble=False,
         note="II. SELIM TURBESI, **1577**, MIMAR SINAN. Plan KARE, disten "
              "koseleri PAHLI; ici SEKIZGEN galerili. Sinan burada da "
              "Kanuni turbesindeki gibi CIFT KABUKLU ortu kullandi. "
              "1632'de 55 yasinda."),
    dict(asset="TurbeMuradIII", plan="altigen", half=10.0, wall_h=12.5,
         rise=8.0, revak=True, marble=True,
         note="III. MURAD TURBESI, **1599**, DAVUD AGA ve yardimcisi "
              "DALGIC AHMED AGA. Plan ALTIGEN, cift kubbeli, DISTAN "
              "MERMER kapli, onunde REVAKLI bolum — **Osmanli'nin en "
              "buyuk turbelerinden**. III. Murad 1595'te oldu, turbe dort "
              "yil sonra tamamlandi; II. Selim ile Sehzadeler turbeleri "
              "arasindadir. 1632'de 33 yasinda."),
    dict(asset="TurbeMehmedIII", plan="sekizgen", half=8.5, wall_h=11.0,
         rise=6.5, revak=False, marble=False,
         note="III. MEHMED TURBESI, **1604-1608**. Plan SEKIZGEN, cift "
              "kubbeli. Yapimina 1013/1604'te I. Ahmed doneminde mimarbasi "
              "DALGIC AHMED AGA basladi, 1017/1608-09'da SEDEFKAR MEHMED "
              "AGA tamamladi — yani Sultanahmet'in mimari. 1632'de 24 "
              "yasinda."),
    dict(asset="TurbeSultanAhmed", plan="kare_pahli", half=10.0, wall_h=12.5,
         rise=8.0, revak=True, marble=False,
         note="SULTAN AHMED TURBESI, **1619**; I. Ahmed 1617'de oldu ve "
              "turbeyi oglu II. OSMAN tamamlatti. Kulliyenin parcasi ve "
              "Sultanahmet Camii'nin kuzeybatisinda. 1632'de 13 yasinda — "
              "ve yatanlarin arasinda II. Osman da vardir: 1622'de "
              "Yedikule'de oldurulup buraya gomuldu (ADR 0050)."),
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
        p = tk.SultanTurbeParams(v["plan"], v["half"], v["wall_h"], v["rise"],
                                 revak=v["revak"], marble=v["marble"],
                                 palette=a.palette)
        lod0, lod1, ucx, info = tk.build_sultan_turbe(p, col, v["asset"],
                                                      textured=a.textured)
        if abs(info["pivot_min_z"]) > 0.01:
            raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")

        hz.log(f"{v['asset']}: plan {info['plan']} ({info['sides']} yuz, "
               f"yuz yayilimi {info['face_spread']:.3f}), "
               f"{'revakli' if info['revak'] else 'revaksiz'}"
               f"{', MERMER' if info['marble'] else ''}")
        hz.log(f"  {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
               f"yukseklik {info['height']:.2f} m, LOD0={info['tris_lod0']}")

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

    # UC AYRI PLAN GERCEKTEN UC AYRI MI? Meshten olculen yuz sayisi ve
    # yuz uzunlugu yayilimi ile denetle.
    kinds = {(i["sides"], i["face_spread"] > 0.05) for i in infos}
    if len(kinds) < 3:
        raise SystemExit(f"[HZ] HATA: planlar ayrismiyor ({kinds}) — "
                         "kare-pahli, altigen ve sekizgen UC AYRI sey.")
    hz.log(f"plan cesitliligi: {sorted(kinds)} — uc ayri plan dogrulandi")

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

    hz.log("gen_sultan_turbeleri OK")


if __name__ == "__main__":
    main()
