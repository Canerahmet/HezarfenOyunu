"""
Hezarfen: 1632 - Sultan Ahmed Camii.

Ayrinti ve gerekce: `lib/sinan_kit.py`, RESEARCH.md 5.13, ADR 0047.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_sultanahmet.py -- \
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
import sinan_kit as sk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Sultan Ahmed Camii, 1609-1616 (kulliye 1620), Sedefkar Mehmed Aga; "
    "bani I. Ahmed. "
    "**1632'DE ON ALTI YASINDA** — Suleymaniye'de 'degismedi' demistim, "
    "burada daha keskin: IV. Murad'in Istanbul'unda bu yapi YENIDIR. "
    "Sehrin en taninan silueti, oyunun gectigi yil daha bir kusak "
    "eskimemistir. "
    "1632'DE TAMAM: cami (1616), arasta ve hamam (1617), SULTAN AHMED "
    "TURBESI (1619, II. Osman tamamlatti), medrese-darussifa-imaret "
    "(1620). Kulliye butunuyle ayakta. "
    "1632'DE YOK: III. Selim'in su haznesi (1802 sonrasi). Bu yapida "
    "'sonradan eklendi' listesi KISADIR. "
    "DOGRULUK BASAMAGI **D2**: kubbe acikligi ve butun kutle basamaklari olculu plandan turedi; olculmeyenler asagida ayrica D3 diye isaretli. "
    "OLCULER: kubbe acikligi **23,50 m** — bu sayi PLANDAN CIKARILDI, "
    "kaynaktan kopyalanmadi: ayak duvarlarinin ekseni 30,75 m arali, "
    "duvar 3,65 m kalin, ic yuzler arasi 23,45 m; yayimlanan 23,5 tam "
    "olarak budur. Ayni kubbenin UC sayisi var ve ucu de dogru: TDV "
    "'icten' **22,40 m**, aciklik **23,50 m**, plandan okunan kursun izi "
    "(kasnak + sacak) **27,7 m**. Ayasofya'da (5.11) yalnizca IKI sayi "
    "vardi cunku Bizans kubbesinde kasnak yok — ucuncu sayiyi kasnak "
    "doguruyor, yani bu bir muhasebe sikintisi degil MIMARI bir farktir. "
    "Kilit **43 m**. "
    "SAYILAN: **DORT** yarim kubbe (dort yonde birer — Ayasofya ve "
    "Suleymaniye'de IKI, Uskudar Mihrimah'ta UC idi); her yarim kubbede "
    "**UC** eksedra, toplam **ON IKI**; **DORT** fil ayagi, capi **5 m**; "
    "**ALTI** minare ve **ON ALTI** serefe (harim kosesindeki dordu ucer, "
    "avlu kosesindeki ikisi ikiser) — o gune kadar denenmemis bir duzen; "
    "avlu revakinda **YIRMI ALTI** sutun ve **OTUZ** kubbeli birim; "
    "altigen planli kubbeli sadirvan. "
    "EKSEDRALAR BU MESH'TE VAR: Ayasofya'da girmemislerdi cunku orada IC "
    "MEKAN ogesidirler (ADR 0045); burada yarim kubbelerin eteginden disa "
    "tasarlar ve siluetin basamakli kaskadini onlar yapar. Ayni sozcuk, "
    "iki yapida iki ayri sey. "
    "KIBLE: eksen plandan YEDI bagimsiz yolla 133,6 derece olculdu ve bu "
    "olcum 1632 kiblesinin (133,7) ta kendisini verdi — ADR 0046'nin "
    "cikis noktasi bu yapidir. "
    "OLCULMEDI: minare boylari (plan izinde 64 / 54 m yaziyor ama ayni "
    "izin kubbe yuksekligi 62 m diyor ve o yayimlanan 43 m ile "
    "catisiyor) — plan GEOMETRISI guvenilir, plan YUKSEKLIKLERI degil; "
    "boylar **D3**. Yayimlanan 'harim 64 x 72 m' yapinin NERESINI "
    "anlattigini soylemiyor (Ayasofya'daki tuzagin ucuncu tekrari); "
    "olculen 61 x 55 kullanildi. "
    "Kaynaklar: TDV Islam Ansiklopedisi 'Sultan Ahmed Camii ve "
    "Kulliyesi'; Vikipedi. Plan geometrisi OpenStreetMap izlerinden "
    "olculdu (ODbL; atif refs/LICENSES.md). RESEARCH.md 5.13, ADR 0047"
)


def add_args(p):
    p.add_argument("--asset", default="Sultanahmet")
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

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = sk.SultanahmetParams(palette=a.palette)
    lod0, lod1, ucx, info = sk.build_sultanahmet(p, col, a.asset,
                                                 textured=a.textured)

    if info["minarets"] != 6 or info["sherefe_total"] != 16:
        raise SystemExit("[HZ] HATA: ALTI minare, ON ALTI serefe.")
    if info["half_domes"] != 4:
        raise SystemExit("[HZ] HATA: DORT yarim kubbe (dort yonde birer).")
    if info["exedrae"] != 12:
        raise SystemExit("[HZ] HATA: ON IKI eksedra (her yarim kubbede UC).")
    if info["minaret_h_tall"] <= info["minaret_h_short"]:
        raise SystemExit("[HZ] HATA: harim minareleri avlu minarelerinden "
                         "UZUN olmali.")

    hz.log(f"{a.asset}: kubbe aciklik {p.dome_d:.2f} m / ic "
           f"{sk.SA_DOME_D_IN:.2f} m / kursun izi 27,7 m (UC sayi), "
           f"kilit {p.crown_z:.2f}; mesh capi {info['measured_dome_d']:.2f}")
    hz.log(f"{info['half_domes']} yarim kubbe, {info['exedrae']} eksedra, "
           f"{info['piers']} fil ayagi")
    hz.log(f"{info['minarets']} minare / {info['sherefe_total']} serefe "
           f"{info['sherefe_each']} (uzun {info['minaret_h_tall']:.0f} m, "
           f"kisa {info['minaret_h_short']:.0f} m)")
    hz.log(f"sacak {p.arch_z:.2f} m (kilit 43'ten turedi); plandan okunan "
           f"kemer kati 30 m, turetilen kemer kilidi {p.arch_crown_z:.2f}")
    hz.log(f"ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, LOD0={info['tris_lod0']}")

    if abs(info["pivot_min_z"]) > 0.01:
        raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")

    info.update(name=a.asset, prefab=f"PF_{a.asset}", tier="T1", source=SOURCE)

    if a.catalog:
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

    if a.blend_dir:
        os.makedirs(a.blend_dir, exist_ok=True)
        hz.save_blend(os.path.join(a.blend_dir, f"{a.asset}.blend"))
    if a.out_dir:
        os.makedirs(a.out_dir, exist_ok=True)
        export_fbx(os.path.join(a.out_dir, f"SM_{a.asset}.fbx"),
                   collection_name=COLLECTION)
    hz.log("gen_sultanahmet OK")


if __name__ == "__main__":
    main()
