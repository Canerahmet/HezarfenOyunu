"""
Hezarfen: 1632 — Üsküdar Mihrimah Sultan Külliyesi'nin öteki yapıları.

Cami ayrı üretilir (`gen_uskudar_mihrimah.py`, ADR 0036). Burada 1632'de
ayakta olan ve **bugün de ayakta olan** iki yapı var: medrese ve sıbyan
mektebi. Ölçü ve gerekçeler: RESEARCH.md §5.4, ADR 0038.

## Neden yalnız ikisi

Külliyenin 1632'de ayakta olan öteki iki yapısı **yerleştirilemiyor**:

* **imaret-tabhâne** — 1722'de yandı; TDV yerinin *belirsiz* olduğunu
  söyler ve 1936'da yol genişletmesi kalıntısını da yok etti. Yeri
  bilinmeyen bir yapıyı koymak, koordinat uydurmaktır.
* **kervansaray (Kurşunlu Han)** — 1920'lerin başında çöktü ve tamamen
  kaldırıldı.

İkisi de RESEARCH.md'de kayıtlı; eksiklik bir unutma değil, kanıt sınırı.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_mihrimah_kulliye.py -- \
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
import civic_kit as ck             # noqa: E402
import hz_blender as hz            # noqa: E402
import mahalle_kit as mak          # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

#: Belgeli hücre sayısı — TDV ve İBB: "kubbeli bir dershane ve **on altı**
#: öğrenci hücresi". Sayım bir ölçüdür ve geometriyi bağlar.
MEDRESE_HUCRE = 16

SRC_MEDRESE = (
    "Uskudar Mihrimah Sultan Medresesi, 1548, Mimar Sinan. Caminin "
    "DOGUSUNDA (belgeli konum; olculu koordinat 41,027229 K / 29,016325 D). "
    "**KUBBELI BIR DERSHANE + ON ALTI OGRENCI HUCRESI** (TDV; IBB Kulturel "
    "Miras) — hucre sayisi SAYILAN bir olcudur ve modelin geometrisini "
    "baglar. 1632'de 84 yasinda ve faal. "
    "1632'DE YOK: 1961 onarimi — medrese o tarihte saglik ocagina "
    "cevrildi ve IC OZELLIKLERINI YITIRDI; bugun ozel bir tip merkezidir. "
    "OLCU YOK (hucre sayisi disinda): 1632 halinin olculu cizimi "
    "bulunmuyor; avlu ve cephe oranlari **D3**, tipolojik. "
    "RESEARCH.md 5.4, ADR 0038"
)

SRC_MEKTEP = (
    "Uskudar Mihrimah Sultan Sibyan Mektebi, yapim 1547-48, Mimar Sinan. "
    "Caminin KIBLE TARAFINDA, aradan kucuk bir yol gecer (belgeli konum; "
    "olculu koordinat 41,026531 K / 29,016231 D). "
    "BICIM: kubbeli bir dershane ve kubbeli ACIK EYVAN — kislik ve yazlik "
    "bolumleri vardir; dikdortgen planli. YAMACTA oldugu icin altina "
    "DUKKAN eklenmistir, yani yapi bir alt yapinin uzerinde YUKSELIR. "
    "1632'de 84 yasinda ve faal. "
    "1632'DE YOK: bugunku cocuk kutuphanesi islevi. "
    "OLCU YOK: kutle **D3**, tipolojik. RESEARCH.md 5.4, ADR 0038"
)


def add_args(p):
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None)
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def _finish(a, asset, info, tier, source, kind):
    info.update(name=asset, prefab=f"PF_{asset}", tier=tier, source=source,
                kind=kind, status="draft", accuracy="D3")
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


def build_medrese(a):
    """Medrese — **on altı** hücre, kubbeli dershane."""
    hz.reset_scene()
    col = hz.collection(COLLECTION)

    # Olcuye gore DEGIL, SAYIYA gore boyutlandirildi.
    #
    # Elimdeki tek sayisal belge hucre sayisi: on alti. Avlu olculeri
    # bilinmiyor. O yuzden dogru yon "makul bir avlu cizip cikan hucre
    # sayisini kabullenmek" degil, avluyu SAYI TUTANA KADAR aramaktir.
    # Ilk denemem 14 verdi ve denetim reddetti.
    #
    # Ama kisitin GUCUNU de yazmak lazim: parametre uzayi tarandiginda
    # 16 hucreyi veren **100 kombinasyon** cikti ve arch_w sonucu hic
    # etkilemiyor. Yani sayi avluyu GEVSEK sinirliyor, sikilastirmiyor —
    # buradaki olculer o kumeden en derli toplu olani, kanit degil.
    # Dogruluk basamagi bu yuzden D3 kaliyor.
    p = ck.MedreseParams(width=24.0, depth=23.0, wing=5.2, arch_w=2.30,
                         floor_h=3.90, dershane=True, dershane_w=7.6,
                         palette=a.palette)
    # Uc kademe: tam / orta / blok — `ottoman_kit.build_with_mid_lod`.
    lod0, lod1, lod2, ucx, info = kit.build_with_mid_lod(
        ck.build_medrese, p, col, "MihrimahMedrese", textured=a.textured)
    if info["hucre"] != MEDRESE_HUCRE:
        raise SystemExit(
            f"[HZ] HATA medrese {info['hucre']} hucre — belgeli sayi "
            f"{MEDRESE_HUCRE} (TDV: 'kubbeli bir dershane ve on alti "
            "ogrenci hucresi'). Avlu olculerini ayarla; sayiyi degistirme.")
    hz.log(f"MihrimahMedrese: {info['hucre']} hucre (belgeli), ayak izi "
           f"{info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, LOD0={info['tris_lod0']}")
    _finish(a, "MihrimahMedrese", info, "T1", SRC_MEDRESE, "medrese")


def build_mektep(a):
    """Sıbyan mektebi — yamaçta, altında dükkân."""
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    # base_h bir suslemesi degil BELGESI: kaynak "yamacta oldugu icin
    # altina dukkan eklenmistir" der, yani yapi bir alt yapinin uzerinde
    # yukselir. Duz zemine oturtmak o cumleyi silerdi.
    p = mak.MektepParams(room=6.00, wall_h=3.60, base_h=2.90, dome_h=2.05,
                         steps=9, eyvan=True, eyvan_d=3.60,
                         palette=a.palette)
    lod0, lod1, ucx, info = mak.build_mektep(p, col, "MihrimahMektebi",
                                             textured=a.textured)
    if not info.get("eyvan"):
        raise SystemExit("[HZ] HATA: mektepte YAZLIK EYVAN yok. Kaynak "
                         "'kubbeli bir dershane ve kubbeli ACIK EYVAN; "
                         "kislik ve yazlik bolumleri vardir' der; eyvansiz "
                         "kurmak yapinin yarisini silmektir.")
    if p.base_h < 2.5:
        raise SystemExit("[HZ] HATA: mektep alt yapisi bir DUKKAN "
                         "barindiracak kadar yuksek olmali (kaynak: "
                         "'yamacta oldugu icin altina dukkan eklenmistir')")
    hz.log(f"MihrimahMektebi: oda {p.room:.2f} m, alt yapi {p.base_h:.2f} m "
           f"(dukkan), yukseklik {info['height']:.2f} m, "
           f"LOD0={info['tris_lod0']}")
    _finish(a, "MihrimahMektebi", info, "T1", SRC_MEKTEP, "mektep")


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())
    build_medrese(a)
    build_mektep(a)
    hz.log("gen_mihrimah_kulliye OK")


if __name__ == "__main__":
    main()
