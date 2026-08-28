"""
Hezarfen: 1632 — Karakter taban gövdesi (Faz 5).

Bu tur yalnızca **taban gövdeyi** üretir: CC0 paketten getir, yönü ölç,
1,70 m'ye normalleştir, ölçüleri kataloğa yaz. Kıyafet, saç ve rig ayrı
turlar — çünkü her biri kendi başına yanlış olabilir ve karışık bir turda
hangisinin yanlış olduğunu ölçemezsin.

Kullanım:
  blender --background --python tools/blender/gen_hezarfen.py
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz             # noqa: E402
import karakter_kit as kar          # noqa: E402
import ottoman_kit as kit           # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

TIER = "T3"
SOURCE = (
    "HEZARFEN AHMED CELEBI'nin GOVDESI. Portresi YOKTUR — ne minyaturu, "
    "ne tarifi. RESEARCH.md: sahsi hakkinda bilinen tek sey Evliya'nin "
    "birkac cumlesidir. Bu yuzden govde bir BENZERLIK iddiasi tasimaz; "
    "donemin ve yerin genel yetiskin erkek anatomisidir. Taban geometri "
    "Blender Studio Human Base Meshes v1.4.1 (CC0, kayit: "
    "art/base/blender-studio/meta.json). Boy 1,70 m ve bu sayi keyfi "
    "degil: bu projenin butun inceleme paketleri 1,70 m'lik olcek "
    "figurune gore yargilandi; karakter baska boyda olsaydi sehir yanlis "
    "olcekte kurulmus olurdu. Yuz hedefi plan Bolum 10: stilize-gercekci, "
    "portre-fotogercekcilik DEGIL — zaten kopyalanacak bir portre yok."
)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "karakter"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "karakter",
                                                      "catalog.json"))
    ap.add_argument("--taban", default=None,
                    help="Human Base Meshes .blend yolu")
    ap.add_argument("--no-textures", action="store_true")
    # Ciplak taban govde bir OYUN VARLIGI DEGIL; rig'siz bir karakteri
    # prefab yapmak erken olur. Bu tur `art/blend/` ve katalogda durur;
    # Unity'ye rig + kiyafet tamamlaninca gider. `_Import` bos kalir
    # (CLAUDE.md: "_Import bos birakilir").
    ap.add_argument("--export", action="store_true",
                    help="FBX'i _Import'a yaz (varsayilan: yazma)")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)

    hz.reset_scene()
    col = hz.collection(COLLECTION)
    mats, tex_sizes = kit.build_materials("default",
                                          textured=not args.no_textures)

    asset = "SK_Hezarfen_Govde"
    govde = kar.taban_getir(args.taban, col=col)
    kar.temiz_ag(govde)

    # --- YON: olculur, varsayilmaz -------------------------------------
    aci = kar.one_cevir(govde)
    hz.log(f"yon duzeltmesi: {aci * 57.2958:.1f} derece")

    # --- BOY: 1,70 m ---------------------------------------------------
    k = kar.normalize(govde)
    hz.log(f"olcek carpani: {k:.4f}")

    govde.name = f"{asset}_LOD0"
    govde.data.name = govde.name
    hz.assign(govde, mats.get("skin", mats["plaster"]))

    olcu = kar.olcu_al(govde)

    # --- LOD ------------------------------------------------------------
    # Karakter ucuncu sahis kamerasinda SUREKLI ekranda; LOD1 uzak
    # NPC kullanimi ve kalabalik icin (Faz 6 ayni tabandan turetecek).
    lod1 = kar.desimasyon(govde, 0.35, f"{asset}_LOD1")
    hz.link(lod1, col)
    hz.assign(lod1, mats.get("skin", mats["plaster"]))

    for obj in (govde, lod1):
        if kar.uv_var_mi(obj):
            continue
        kit.apply_uvs(obj, tex_sizes)

    info = dict(name="Hezarfen_Govde", prefab=None,
                kind="karakter", state="base",
                prefab_notu="Rig ve kiyafet tamamlanmadan prefab uretilmez.",
                status="draft", accuracy="D3",
                tier=TIER, source=SOURCE,
                taban_paket="human-base-meshes-bundle-v1.4.1",
                taban_obje=kar.TABAN_OBJE,
                taban_lisans="CC0-1.0",
                yon_duzeltme_derece=round(aci * 57.2958, 2),
                olcek_carpani=round(k, 5),
                tris_lod0=kar.hz_tri(govde), tris_lod1=kar.hz_tri(lod1),
                uv_var=kar.uv_var_mi(govde),
                **olcu)

    hz.log(f"{asset}: boy {olcu['boy']:.3f} m, bas orani 1/{olcu['bas_orani']}, "
           f"omuz {olcu['omuz_genisligi']:.3f} m (boyun "
           f"{olcu['boyun_genisligi']:.3f} m), en genis {olcu['en_genis']:.3f} m")
    hz.log(f"{asset}: {info['tris_lod0']} / {info['tris_lod1']} ucgen, "
           f"UV {'var' if info['uv_var'] else 'YOK'}")

    # --- DENETIM: oranlar insan mi ---------------------------------------
    # Render "dogru gorunuyor" der; bu sayilar dogru OLUP OLMADIGINI soyler.
    if not 6.5 <= olcu["bas_orani"] <= 8.5:
        raise SystemExit(
            f"[HZ] HATA bas orani 1/{olcu['bas_orani']} — yetiskin insan "
            "1/7 ile 1/8 arasindadir. Taban ag ya da olcum yanlis.")
    if not 0.36 <= olcu["omuz_genisligi"] <= 0.50:
        raise SystemExit(
            f"[HZ] HATA omuz genisligi {olcu['omuz_genisligi']:.3f} m — "
            "yetiskin erkek 0,38-0,48 m arasindadir (biakromiyal + deltoid).")
    # Boyun omuzdan DAR olmali. Bu, olcumun dogru yeri buldugunun kaniti:
    # yanlis kotu okusaydi ikisi birbirine yakin cikardi.
    if olcu["boyun_genisligi"] >= olcu["omuz_genisligi"] * 0.62:
        raise SystemExit(
            f"[HZ] HATA boyun {olcu['boyun_genisligi']:.3f} m, omuz "
            f"{olcu['omuz_genisligi']:.3f} m — olcum boynu bulamadi.")

    hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
    if args.export:
        export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                   collection_name=COLLECTION)
    else:
        hz.log("FBX yazilmadi (--export ile yazilir): taban govde bir ara "
               "urundur, rig ve kiyafet tamamlanmadan Unity'ye gitmez.")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": [info]}, fh, ensure_ascii=False, indent=1)
    hz.log(f"katalog: {args.catalog}")


if __name__ == "__main__":
    main()
