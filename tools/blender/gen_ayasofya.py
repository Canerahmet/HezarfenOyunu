"""
Hezarfen: 1632 - Ayasofya.

Ayrinti ve gerekce: `lib/ayasofya_kit.py`, RESEARCH.md 5.11, ADR 0045.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_ayasofya.py -- \
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
import ayasofya_kit as ak          # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Ayasofya, 532-537 (kubbe 562'de YUKSELTILEREK yeniden kuruldu); "
    "1453'ten beri cami. 1632'de 1100 yasinda. "
    "**KUBBE OSMANLI KUBBESI DEGILDIR**: olculen basiklik orani 0,909 "
    "(15,00 / 16,50); sinan_kit'in 0,78 Osmanli orani buraya uygulanirsa "
    "kilit 42,4 m'ye duser. Kit bu yuzden ayridir ve validate 0,78'i "
    "REDDEDER. "
    "OLCULER (D2): kubbe IC acikligi 31,87 (K-G) x 30,86 m (D-B), DIS "
    "kutlesi 33,0 m, kilit 55,60 m (dosemeden). Ic/dis ikiligiyle "
    "DORDUNCU karsilasma (Uskudar Mihrimah, Yeni Cami, Suleymaniye) ve "
    "ILK KEZ IKISI BIRDEN olculdu — kaynagin hangisini kastettigini "
    "tahmin etmek gerekmedi; aradaki ~1 m kubbe kabugunun kalinligidir. "
    "SAYILAN: kubbe eteginde KIRK kaburga ve KIRK pencere (kubbe 40 "
    "dilimli uretilir, pencereler kaburgalarin ARASINA duser); IKI yarim "
    "kubbe ana eksende; DORT eksedra; DORT minare. "
    "MINARELER BIRBIRININ AYNI DEGILDIR ve bu OLCULDU: dogu cifti Ø3,6 m "
    "(ince, biri TUGLA), bati cifti Ø4,0 m (Sinan ikizleri, II. Selim'in "
    "siparisi, III. Murad'in ilk yillarinda tamam). Kaynaklar tugla "
    "minarenin kosesinde CELISIR (TDV guneybati; iki populer kaynak "
    "guneydogu ve kuzeydogu); olcu TDV'nin guneybati iddiasini ELER, "
    "cunku o kose ikiz ciftin bir uyesidir ve tugla minare tektir. "
    "Konumlar da simetrik degil (kuzey cifti eksenden 39,5 m, guney cifti "
    "33,1 m) — minareler var olan payandalara dayanarak, farkli "
    "yuzyillarda eklendi. "
    "YAPI KIBLEYE DONUK DEGILDIR: eksen azimutu 123,5 derece ve mihrap "
    "apsise EGIK oturtulmustur. Sapmanin buyuklugu neye gore olculdugune "
    "bagli: bugunun buyuk daire kiblesine (150,40) gore 26,9 derece, "
    "1632'nin OLCULEN kiblesine (133,7 — ADR 0046) gore 10,2 derece. Yani "
    "Bizans'in dogu ekseni 1632'nin kiblesine bugunkunden DAHA YAKIN. "
    "Katalog ikincisini yazar; yerlestiriciyi o ilgilendirir. "
    "1632'DE VAR: dort minare, Sinan'in payandalari, minber ve mahfil "
    "(III. Murad), uc imparator turbesi (II. Selim 1577, III. Murad 1599, "
    "III. Mehmed 1608). "
    "1632'DE YOK: I. MUSTAFA ve IBRAHIM TURBESI (1639) — o tarihte "
    "vaftizhane hala YAGHANEDIR, yapi ayakta ama islevi baska; III. "
    "Ahmed'in hunkar mahfili (1728); I. Mahmud'un kutuphanesi, "
    "SADIRVANI, sibyan mektebi ve imareti (1739-40); Fossati onarimi "
    "(1847-49) ve onun disa vurdugu SIVA + KIRMIZI YATAY SERITLER "
    "(bugunku tek ton okra ondan da sonradir); Kazasker Mustafa Izzet'in "
    "buyuk hat levhalari. "
    "KALDIRILMIS EK: Fatih'in yarim kubbe uzerindeki AHSAP minaresi "
    "1574'te sokulmustur — 1632'de kubbenin ustunde minare YOKTUR. "
    "Minare BOYU olculmedi (yaygin kaynak 'dordu de 60 m' der ama govde "
    "caplari dordunun ayni olmadigini gosteriyor) ve serefe sayisi "
    "kaynakta verilmiyor — ikisi de **D3**. Payanda SAYISI da D3. "
    "Kaynaklar: TDV Islam Ansiklopedisi 'Ayasofya'; ayasofyacamii.gov.tr; "
    "Vikipedi (yapi olculeri). Eksen azimutu ve minare govde caplari "
    "OpenStreetMap izlerinden TURETILDI (ODbL; veri kopyalanmadi, iki "
    "olcu okundu). RESEARCH.md 5.11, ADR 0045"
)


def add_args(p):
    p.add_argument("--asset", default="Ayasofya")
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

    p = ak.AyasofyaParams(palette=a.palette)
    # Uc kademe: tam / orta / blok — `ottoman_kit.build_with_mid_lod`.
    lod0, lod1, lod2, ucx, info = kit.build_with_mid_lod(
        ak.build_ayasofya, p, col, a.asset, textured=a.textured)

    # SAYILAN degerler geometriyi baglar; katalogda yazip meshde olmamasi
    # bir kayit degil bir YALANDIR.
    if info["dome_ribs"] != 40 or info["dome_windows"] != 40:
        raise SystemExit("[HZ] HATA: KIRK kaburga / KIRK pencere.")
    if info["minarets"] != 4 or info["brick_minarets"] != 1:
        raise SystemExit("[HZ] HATA: DORT minare, biri TUGLA.")
    if info["half_domes"] != 2:
        raise SystemExit("[HZ] HATA: IKI yarim kubbe (ana eksende).")
    # Eksedralar burada ARANMAZ: dordu de IC mekan ogesidir ve dis
    # kutlede gorunmezler (bkz. ayasofya_kit, "EKSEDRALAR BU MESH'TE YOK").
    # Kendi denetimini gecmek icin gomulu geometri eklemek, sayinin
    # katalogda yasayip meshte yasamamasinin aynasidir.
    if info["exedrae_interior"] != 4:
        raise SystemExit("[HZ] HATA: DORT eksedra (ic mekan) kayitli olmali.")
    if abs(info["rise_ratio"] - 0.909) > 0.005:
        raise SystemExit(f"[HZ] HATA: basiklik orani {info['rise_ratio']} — "
                         "Bizans kubbesi 0,909; Osmanli 0,78 DEGIL.")

    hz.log(f"{a.asset}: kubbe dis {p.dome_d:.2f} m / ic "
           f"{ak.AYA_DOME_D_IN_NS:.2f}x{ak.AYA_DOME_D_IN_EW:.2f} m, "
           f"kilit {p.crown_z:.2f} m (OLCULU); mesh capi "
           f"{info['measured_dome_d']:.2f}, mesh kilidi "
           f"{info['measured_crown_z']:.2f}")
    hz.log(f"basiklik orani {info['rise_ratio']:.3f} (Osmanli 0,78 DEGIL); "
           f"{info['dome_ribs']} kaburga / {info['dome_windows']} pencere")
    hz.log(f"{info['minarets']} minare ({info['brick_minarets']} tugla), "
           f"{info['half_domes']} yarim kubbe, "
           f"{info['exedrae_interior']} eksedra (IC — mesh'te yok)")
    hz.log(f"eksen {ak.AYA_AXIS_DEG:.1f} derece, giris {info['face_deg']:.1f} "
           f"— kibleden {info['qibla_offset_deg']:.1f} derece sapma")
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
    hz.log("gen_ayasofya OK")


if __name__ == "__main__":
    main()
