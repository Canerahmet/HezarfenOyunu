"""
Hezarfen: 1632 - Fatih Camii, **1766 oncesi ozgun sema**.

Ayrinti ve gerekce: `lib/sinan_kit.py`, RESEARCH.md 5.14, ADR 0048.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_fatih_camii.py -- \
      --textured --out-dir unity/HezarfenGame/Assets/_Import
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import ottoman_kit as kit    # noqa: E402
import hz_blender as hz            # noqa: E402
import sinan_kit as sk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Fatih Camii, 1463-1470, mimar ATIK SINAN (Sinaneddin Yusuf); bani "
    "Fatih Sultan Mehmed. 1632'de 162 yasinda. "
    "**BUGUN GORULEN YAPI 1632'DE YOKTUR** — Faz 3'un en buyuk farki "
    "budur. 1766 depremi camiyi yikti ve 'caminin geri kalan kismi ZEMINE "
    "KADAR yiktirildi'; bugunku barok yapi 1767-71, mimar MEHMED TAHIR "
    "AGA'dir. "
    "1632'DEKI SEMA (TDV, eski resimlerden): ortada BIR buyuk kubbe, "
    "MIHRAP tarafinda **BIR** yarim kubbe, yanlarda daha alcak **UCER** "
    "kucuk kubbeli bolum. Kubbe 'duvarlar ve **IKI AYAK** uzerine' "
    "oturur (Vikipedi) — bugunku DORT fil ayagi ve DORT yarim kubbe "
    "1767-71'dir. Yani plan bugunku gibi merkezi degil UZUNLAMASINA'dir "
    "ve disaridan Edirne Uc Serefeli'ye benzeyen erken klasik bir "
    "kutledir. "
    "MINARE: **IKI**, her biri **BIRER** serefeli ('simdiki minarelerin "
    "yerinde birer serefeli iki minare'); bugunkuler IKISER serefelidir. "
    "OLCU: ana kubbe **26 m** — **yuz yil boyunca Istanbul'un en buyuk "
    "kubbesi** kaldi, 1470'ten Suleymaniye'ye (1557, 26,5 m) kadar; 87 "
    "yil, 'bir yuzyil' tarifine oturur. 1767-71 bu capi korudu, gerisini "
    "korumadi. "
    "1632'DE KESINLIKLE VAR OLAN GERCEK PARCALAR — ilk yapidan bugune "
    "kalanlar sunlardir: **sadirvan avlusunun UC DUVARI**, **avlunun "
    "ortasindaki SADIRVAN**, **TACKAPI**, **MIHRAP**, ve **minarelerin "
    "serefe altina kadar kaide, pabuc ve govdeleri**. Avlunun sayilari o "
    "yuzden dogrudan 1632'yi baglar: **ON SEKIZ sutun**, **YIRMI IKI "
    "kubbe**, **UC kapi** (ikisi yanlarda). "
    "DOGRULUK: kubbe capi **D2**; sema ve sayilar **D3** (TDV bunlari "
    "'eski resimlerinden anlasilmaktadir' diye verir — olculu cizim "
    "degil, tasvir); kilit kotu **D3** ve TURETILMISTIR: yan neflerin "
    "ucer kucuk kubbesi sacagin altinda kalmak zorunda, bu sacagi ~22 "
    "m'ye koyuyor ve Osmanli zinciri oradan 50,5 m veriyor. Harim "
    "olculeri de sayilan degerden turedi (26 + 2x8,7 = 43,4 genislik) ve "
    "kaynagin 'kareye yakin planli' tarifiyle tutuyor. "
    "1632'DE AYRICA VAR: Fatih Sultan Mehmed turbesi (1481; 1766'da "
    "yikildi, yeniden yapildi), Gulbahar Hatun turbesi (depremi ATLATTI, "
    "1767-68'de onarildi), SAHN-I SEMAN medreseleri. "
    "SALT Research'te 1766 oncesi plan-kesit fotograflari var ama "
    "CC BY-NC-ND: yalnizca bakilir, kopyalanmaz — bu model onlardan degil "
    "METIN kaynaklarindan kuruldu. "
    "Kaynaklar: TDV Islam Ansiklopedisi 'Fatih Camii ve Kulliyesi'; "
    "Vikipedi 'Fatih Camii'; fatih.gov.tr. RESEARCH.md 5.14, ADR 0048"
)


def add_args(p):
    p.add_argument("--asset", default="FatihCamii")
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

    p = sk.FatihParams(palette=a.palette)
    # Uc kademe: tam / orta / blok — `ottoman_kit.build_with_mid_lod`.
    lod0, lod1, lod2, ucx, info = kit.build_with_mid_lod(
        sk.build_fatih, p, col, a.asset, textured=a.textured)

    # BUGUNKU YAPIYI URETMEDIGIMIZIN bekcileri.
    if info["half_domes"] != 1:
        raise SystemExit("[HZ] HATA: 1632'de MIHRAP yonunde BIR yarim "
                         "kubbe; dort yarim kubbe 1767-71'dir.")
    if info["piers"] != 2:
        raise SystemExit("[HZ] HATA: ozgun kubbe IKI ayak uzerindeydi.")
    if info["side_domes"] != 3:
        raise SystemExit("[HZ] HATA: yanlarda UCER kucuk kubbe.")
    if info["sherefe_each"] != 1 or info["minarets"] != 2:
        raise SystemExit("[HZ] HATA: IKI minare, BIRER serefe.")
    if info["portico_bays"] != 22 or info["court_columns"] != 18:
        raise SystemExit("[HZ] HATA: avlu ON SEKIZ sutun / YIRMI IKI kubbe.")

    hz.log(f"{a.asset}: kubbe {p.dome_d:.2f} m (yuz yil boyunca en buyuk), "
           f"kilit {p.crown_z:.2f} m (TURETILEN, D3); mesh capi "
           f"{info['measured_dome_d']:.2f}")
    hz.log(f"{info['half_domes']} yarim kubbe (MIHRAP yonunde), "
           f"{info['side_domes']}+{info['side_domes']} yan kubbe, "
           f"{info['piers']} ayak — bugunku sema 4+4'tur")
    hz.log(f"{info['minarets']} minare / {info['sherefe_total']} serefe "
           f"(bugun ikiser); avlu {info['court_columns']} sutun / "
           f"{info['portico_bays']} kubbe / {info['court_gates']} kapi "
           f"— hepsi ILK YAPIDAN")
    hz.log(f"sacak {p.arch_z:.2f} m, yan nef {p.aisle_w:.2f} m")
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
    hz.log("gen_fatih_camii OK")


if __name__ == "__main__":
    main()
