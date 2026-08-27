"""
Hezarfen: 1632 — İncili Köşk (Sinan Paşa Köşkü) üreticisi.

**Evliya'ya göre IV. Murad, Hezarfen'in uçuşunu buradan izledi.** Ölçü ve
gerekçeler: `lib/kosk_kit.py`, RESEARCH.md §5.6, ADR 0039.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_incili_kosk.py -- \
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
import kosk_kit as kk              # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Incili Kosk (Sinan Pasa Kosku), Sarayburnu. **998/1590'da baslandi, "
    "999/1590-91'de tamamlandi** (TDV); bani Koca Sinan Pasa (ikinci "
    "sadareti 1589-91), mimar DAVUD AGA (Sinan'in halefi). Kosk III. "
    "Murad'a sunuldu. 1632'de 41 yasinda ve ayakta. "
    "OYUN ICIN: Evliya Celebi'ye gore IV. MURAD HEZARFEN'IN UCUSUNU "
    "BURADAN IZLEDI; Lagari de onune indi. Tek kaynak — T3 anlati, ama "
    "YAPININ kendisi T1. "
    "KONUM: Topkapi'nin en dis sinirinda, MARMARA tarafindaki Bizans "
    "DENIZ SURU uzerinde; kaynak yerini 'Sarayburnu'ndan kiyi boyunca "
    "yaklasik 300 m, Soter Filantropos kalintisi ile Ahirkapi arasi' diye "
    "verir. Onceki katalog degeri 156 m yanlisti (denizden 125 m icerde, "
    "14,7 m yukarida) — kosk denize TASAR. "
    "BICIM (sayilan degerler): kesme tas kemerli alt yapi; cikmanin yan "
    "cephelerinde SARAYBURNU tarafinda BIR, AHIRKAPI tarafinda IKI kemer; "
    "denize acilan cift kemerin ARASINDA CESME; esas mekanin DORT "
    "kosesinde birer BACA; denize dogru AHSAP KONSOLLARA oturan CUMBA. "
    "ORTU TARTISMALI: TDV ortada yukselen kare kutle ve KUBBE der, bir "
    "tasvir piramidal gosterir, SEDAT HAKKI ELDEM ortunun AHSAP oldugunu "
    "savunur — iki varyant uretildi. "
    "1632'DE YOK: 1871-72 yikimi ve sahil demiryolu; bugun yalnizca temel "
    "kalintisi durur. "
    "OLCU YOK: yapi 1872'de yikildi ve olculu cizimi bulunmuyor; kutle "
    "**D3**, oranlar tipolojik. RESEARCH.md 5.6, ADR 0039"
)


def add_args(p):
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None)
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def _finish(a, asset, info):
    info.update(name=asset, prefab=f"PF_{asset}", tier="T1", source=SOURCE)
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


def one(a, roof, asset):
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    p = kk.IncliKoskParams(roof=roof, palette=a.palette)
    lod0, lod1, ucx, info = kk.build_incili_kosk(p, col, asset,
                                                 textured=a.textured)

    # SAYILAN DEGERLER kutlede gercekten var mi.
    if info["arch_sarayburnu"] != 1 or info["arch_ahirkapi"] != 2:
        raise SystemExit("[HZ] HATA: yan kemer sayilari belgeli degerlerden "
                         "sapti (Sarayburnu 1, Ahirkapi 2). Asimetri bu "
                         "yapinin kayitli ozelligidir.")
    if info["baca"] != 4:
        raise SystemExit("[HZ] HATA: baca sayisi 4 olmali — 'esas mekanin "
                         "dort kosesinde birer baca yukselir'.")

    # ALT YAPI SU ALTINA INMELI: su cizgisinde kesilmis kutle yuzer gorunur
    # (Kiz Kulesi turunda olculdu, ADR 0035).
    if info["pivot_min_z"] > -0.5:
        raise SystemExit(f"[HZ] HATA alt yapi tabani {info['pivot_min_z']:.2f} "
                         "— su cizgisinin ALTINA inmeli")

    hz.log(f"{asset}: ortu={roof}, ayak izi "
           f"{info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, taban {info['pivot_min_z']:.2f} m, "
           f"LOD0={info['tris_lod0']}")
    _finish(a, asset, info)
    return info


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())
    k = one(a, "kubbe", "IncliKosk")
    a_ = one(a, "ahsap", "IncliKosk_Ahsap")
    # Iki varyant GERCEKTEN farkli olmali; ayni cikiyorsa `roof` okunmuyor
    # demektir (Hudayi turbesinde `acik` bayragi tam boyle sessiz kalmisti).
    if abs(k["height"] - a_["height"]) < 0.2:
        raise SystemExit("[HZ] HATA: iki ortu varyanti ayni yukseklikte — "
                         "`roof` parametresi okunmuyor olabilir.")
    hz.log(f"iki varyant farki: {abs(k['height'] - a_['height']):.2f} m")
    hz.log("gen_incili_kosk OK")


if __name__ == "__main__":
    main()
