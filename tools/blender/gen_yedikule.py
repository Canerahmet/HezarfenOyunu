"""
Hezarfen: 1632 - Yedikule Hisari ve kara sur kapisi.

Ayrinti ve gerekce: `lib/karasur_kit.py`, RESEARCH.md 5.16, ADR 0050.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_yedikule.py -- \
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

SOURCE_YEDIKULE = (
    "Yedikule Hisari, **Fatih Sultan Mehmed 1457-58**; Altin Kapi'nin "
    "arkasina yaptirildi. 1632'de **175 yasinda**. "
    "DOGRULUK BASAMAGI **D3**: sayilar (yedi kule, uc dairesel kule, uc "
    "kemer, 15 000 m2 alan) belgelidir; kule ve duvar YUKSEKLIKLERI "
    "kaynakta yok ve Theodosius burclarinin 25 m'sinden turedi. "
    "**1632 ICIN BU YAPI BIR HABERDIR**: kulelerden birinin adi GENC OSMAN "
    "KULESI'dir cunku **II. Osman 1622'de burada oldurulду** — oyunun "
    "gectigi yil olaydan ON YIL sonrasidir ve tahttaki IV. Murad onun "
    "kardesidir. Yedikule 1632'de harabe degil, herkesin bildigi bir yer. "
    "Bir kule HAZINE KULESI'dir (hisar devletin hazinesini tutar), bir "
    "digeri ZINDAN KULESI. "
    "**III. AHMED KULESI adi 1632'DE YOKTUR** — III. Ahmed 1703-1730 "
    "arasinda hukum surer; kule vardir, ad sonradandir. Katalog kule "
    "adlarini degil SAYISINI tasir: ad bir yorumdur, YEDI bir olgudur. "
    "PLAN: besgen. Bati yani ALTIN KAPI ve Theodosius suru; oteki uc yan "
    "Fatih'in **DAIRESEL** uc kulesi ve onlari birlestiren uc beden "
    "duvari (kaynagin kendi cumlesi). Kalan dort kule Bizans'tandir; "
    "ikisi Altin Kapi'nin iki yanindaki MERMER kulelerdir. "
    "ALTIN KAPI **UC KEMERLIDIR**: ortadaki buyuk kemer yalnizca "
    "imparatorlara, iki yanindaki kucukler halka. Klasik bir zafer taki; "
    "imparatorlarin zafer alayi basinda sehre girdigi ana toren kapisi. "
    "ALAN **15 000 m2** (belgeli) ve besgenin yaricapi bundan TUREDI "
    "(2,378 R2 = 15 000 -> R = 79,4 m, kenar ~93 m) — elle girilen bir "
    "olcu degil. "
    "Kaynaklar: Vikipedi 'Yedikule Zindanlari'; IBB Kultur Sanat; "
    "yedikulehisari.com. RESEARCH.md 5.16, ADR 0050"
)

SOURCE_KAPI = (
    "Theodosius surlarinin KARA KAPISI (Topkapi, Edirnekapi, Silivrikapi, "
    "Mevlanakapi, Belgradkapi, Egrikapi tipi). "
    "DOGRULUK BASAMAGI **D3**: kaynak kara sur kapilarina olcu vermiyor. "
    "GALATA'NIN KAPISI BURAYA KOPYALANMADI ve olcusu de alinmadi: Harup "
    "Kapi rolovesi 2,70 m aciklik verir ama o **2 m** kalinliginda bir "
    "duvarindir; burada duvar **5 m**, burclar **25 m** ve ayni aciklik bu "
    "kutlede mazgal deligi gibi okunurdu. Aciklik duvar kalinligindan "
    "turedi (4,5 m). "
    "KAPI KENDI BURCLARIYLA GELIR: gercek kara sur kapilari IKI BURCUN "
    "ARASINDADIR ve kapiyi kapi yapan sey o iki kutledir. Yerlestirici hat "
    "boyunca zaten 60,7 m'de bir burc koyuyor; kapi onlardan bagimsiz "
    "kendi ciftini tasir, yoksa 'duvarda bir delik' olur. "
    "GECIT GERCEK BIR KEMERDIR (Galata dersi, ADR 0034): kare bir delik "
    "kapi gibi okunmaz. "
    "RESEARCH.md 5.16, ADR 0050"
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

    # --- Yedikule ---------------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    yp = ks.YedikuleParams(palette=a.palette)
    lod0, lod1, ucx, info = ks.build_yedikule(yp, col, "Yedikule",
                                              textured=a.textured)
    if info["towers"] != 7:
        raise SystemExit(f"[HZ] HATA: {info['towers']} kule — hisarin ADI "
                         "Yedikule'dir.")
    if info["round_towers"] != 3:
        raise SystemExit("[HZ] HATA: Fatih'in UC dairesel kulesi.")
    if info["gate_arches"] != 3:
        raise SystemExit("[HZ] HATA: Altin Kapi UC kemerlidir.")
    hz.log(f"Yedikule: {info['towers']} kule ({info['round_towers']} "
           f"dairesel), Altin Kapi {info['gate_arches']} kemer")
    hz.log(f"besgen yaricapi {info['radius']:.1f} m — {info['area_m2']:.0f} "
           "m2 alandan TUREDI")
    hz.log(f"ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, LOD0={info['tris_lod0']}")
    if abs(info["pivot_min_z"]) > 0.01:
        raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")
    info.update(name="Yedikule", prefab="PF_Yedikule", tier="T1",
                source=SOURCE_YEDIKULE)
    infos.append(info)
    if a.blend_dir:
        os.makedirs(a.blend_dir, exist_ok=True)
        hz.save_blend(os.path.join(a.blend_dir, "Yedikule.blend"))
    if a.out_dir:
        os.makedirs(a.out_dir, exist_ok=True)
        export_fbx(os.path.join(a.out_dir, "SM_Yedikule.fbx"),
                   collection_name=COLLECTION)

    # --- Kara sur kapisi --------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    kp = ks.KaraSurKapisiParams(palette=a.palette)
    lod0, lod1, ucx, info = ks.build_kara_sur_kapisi(kp, col, "KaraSurKapisi",
                                                     textured=a.textured)
    if info["towers"] != 2:
        raise SystemExit("[HZ] HATA: kapi IKI burcuyla gelir.")
    hz.log(f"KaraSurKapisi: aciklik {info['opening']:.2f} m (duvar "
           f"{info['wall_t']:.1f} m), burc {info['height_tower']:.1f} m")
    hz.log(f"ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, LOD0={info['tris_lod0']}")
    if abs(info["pivot_min_z"]) > 0.01:
        raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")
    info.update(name="KaraSurKapisi", prefab="PF_KaraSurKapisi", tier="T1",
                source=SOURCE_KAPI)
    infos.append(info)
    if a.blend_dir:
        hz.save_blend(os.path.join(a.blend_dir, "KaraSurKapisi.blend"))
    if a.out_dir:
        export_fbx(os.path.join(a.out_dir, "SM_KaraSurKapisi.fbx"),
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

    hz.log("gen_yedikule OK")


if __name__ == "__main__":
    main()
