"""
Hezarfen: 1632 - Theodosius kara surlari burclari.

Uc varyant: ic sur KARE burcu (cogunluk), ic sur SEKIZGEN burcu, ve
dis sur burcu (alcak).

Ayrinti ve gerekce: `lib/karasur_kit.py`, RESEARCH.md 5.15, ADR 0049.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_kara_surlari.py -- \
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
import karasur_kit as ks           # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE_BASE = (
    "Theodosius KARA SURLARI, 5. yuzyil; Osmanli doneminde bakimliydi ve "
    "**1632'de ayakta**. Yikimlar 19.-20. yuzyildir. "
    "DOGRULUK BASAMAGI **D2**: duvar ve burc yukseklikleri, burc sayisi, hendek ve toplam derinlik BELGELIDIR. Yalnizca ara kesit olculeri (peribolos, parateikhion, glasi), burc plan eni ve dis sur burcunun yuksekligi turetilmistir ve asagida D3 diye isaretlidir. "
    "OLCULER (belgeli): IC SUR 4,5-6 m kalin ve **12 m** yuksek, **96** "
    "burcla; burclar **25 m** yuksek, araliklari 21-77 m (cogu 40-60) ve "
    "plani **cogunlukla KARE, bazilari sekizgen, altigen, besgen**. DIS "
    "SUR tabanda **2 m**, **8,5-9 m** yuksek. HENDEK **20+ m** genis, "
    "**10 m** derin. Hattin uzunlugu **7,5 km**. "
    "**TOPLAM SAVUNMA DERINLIGI 70 m** — ve bu belgeli sayi kesitin "
    "omurgasidir: hendek 20 + parateikhion 17 + dis sur 2 + peribolos 20 "
    "+ ic sur 5 + glasi 6 = 70. Ara olculer tipolojiktir (**D3**) ama "
    "TOPLAMLARI belgeli sayiya oturmak zorundadir ve dogrulama bunu "
    "denetler. Uydurulan sayi yok, PAYLASILAN BIR TOPLAM var. "
    "BURC ARALIGI ELLE GIRILMEZ: sayilan 96 burc, hattin OLCULEN "
    "uzunluguna bolunur. "
    "TEK TIP BURC URETMEK BELGEYE AYKIRIDIR — Galata'da ayni dersi bir kez "
    "almistim (ADR 0034: hayatta kalan ornek orneklem degildir); burada "
    "kaynak zaten cok tipli oldugunu soyluyor. "
    "Kaynaklar: Vikipedi 'Konstantinopolis Surlari'; Koc Universitesi "
    "Istanbul Surlari; Alan Baskanligi. RESEARCH.md 5.15, ADR 0049"
)

VARIANTS = (
    dict(asset="KaraSurBurcu", plan="kare",
         width=ks.KS_TOWER_W, height=ks.KS_TOWER_H,
         wall_h=ks.KS_IC_H, wall_t=ks.KS_IC_T,
         note="IC SUR burcu, KARE plan — kaynagin 'cogunlukla kare' dedigi tip."),
    dict(asset="KaraSurBurcu_Sekizgen", plan="sekizgen",
         width=ks.KS_TOWER_W, height=ks.KS_TOWER_H,
         wall_h=ks.KS_IC_H, wall_t=ks.KS_IC_T,
         note="IC SUR burcu, SEKIZGEN plan — kaynak 'bazilari sekizgen' der."),
    dict(asset="KaraSurBurcu_Dis", plan="kare",
         width=ks.KS_DIS_TOWER_W, height=ks.KS_DIS_TOWER_H,
         wall_h=ks.KS_DIS_H, wall_t=ks.KS_DIS_T,
         note="DIS SUR burcu — ic surunkinden alcak ve kucuk; dis sur "
              "8,75 m, burcu 12 m."),
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

    total = ks.section_total()
    if abs(total - ks.KS_TOTAL_DEPTH) > 0.01:
        raise SystemExit(f"[HZ] HATA: kesit toplami {total:.1f} m — belgeli "
                         f"deger {ks.KS_TOTAL_DEPTH:.1f} m")
    hz.log(f"kesit toplami {total:.1f} m = belgeli {ks.KS_TOTAL_DEPTH:.1f} m")

    infos = []
    for v in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        p = ks.KaraSurBurcuParams(width=v["width"], height=v["height"],
                                  wall_h=v["wall_h"], wall_t=v["wall_t"],
                                  plan=v["plan"], palette=a.palette)
        lod0, lod1, ucx, info = ks.build_kara_sur_burcu(
            p, col, v["asset"], textured=a.textured)

        if abs(info["pivot_min_z"]) > 0.01:
            raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")

        # BELGELI oran, ait oldugu yerde denetlenir: IC SUR burcu 25 m ve
        # ic sur 12 m, yani burc duvarin IKI KATI. Bu, dis sur burcu icin
        # gecerli DEGILDIR — kaynak ona yukseklik vermiyor (D3).
        if v["wall_h"] == ks.KS_IC_H:
            oran = info["height"] / v["wall_h"]
            if abs(oran - 25.0 / 12.0) > 0.20:
                raise SystemExit(
                    f"[HZ] HATA: {v['asset']} burc/duvar orani {oran:.2f} — "
                    "kaynak ic sur icin 25 / 12 = 2,08 verir.")

        hz.log(f"{v['asset']}: {info['footprint_x']:.1f}x"
               f"{info['footprint_y']:.1f} m, yukseklik {info['height']:.2f} m "
               f"(duvar {p.wall_h:.1f}), tasma {info['jut']:.2f}, "
               f"LOD0={info['tris_lod0']}")

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

    hz.log("gen_kara_surlari OK")


if __name__ == "__main__":
    main()
