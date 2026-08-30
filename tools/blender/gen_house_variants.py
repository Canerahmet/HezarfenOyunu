"""
Hezarfen: 1632 — Osmanlı konut kitinin varyant kümesi (plan Faz 2 kabulü).

Faz 2 kabulü "20 parametre kombinasyonu" istiyor. Buradaki 20 varyant rastgele
seçilmedi; her biri bir **tipolojik durumu** temsil eder ve tablo hangi durumun
neden orada olduğunu söyler. Rastgele bir parametre taraması aynı sayıyı verir
ama şehir dokusunu vermez: sokak, birbirinin kopyası olmayan ama aynı ailenin
üyesi olan evlerden kurulur.

Tipoloji dayanağı (RESEARCH.md §4.1(e)): 17. yy başı bir İstanbul evi ~360 zira²
(≈190-208 m²) PARSEL; yapı taban alanlarının %80'i 300 zira² (≈172 m²) altında.
Bizim ayak izlerimiz 30-70 m² bandında, yani dağılımın alt-orta kısmında —
sıradan mahalle evi. Konak/saray ölçeği bu kitin işi değildir.

Gayrimüslim varyantları `palette="nonmuslim"` ile işaretlenir; kit bunu yalnızca
renge değil kat yüksekliğine ve çıkmaya da işler (ADR 0012 §2).

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_house_variants.py -- \
      --out-dir unity/HezarfenGame/Assets/_Import --blend-dir art/blend/variants
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import ottoman_kit as kit          # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

# Konut dokusunun tamami T2'dir: ev-ev kayit yoktur, kurallarla uretilir.
HOUSE_TIER = "T2"
HOUSE_SOURCE = ("Osmanli konut tipolojisi: ahsap karkas, cumbali, 1-2 kat; ev "
                "sokak cizgisine oturur, bahce arkada. Gayrimuslim varyantinda "
                "kat yuksekligi ve renk kisiti. RESEARCH.md 4 ve 4.1. Parsel "
                "olculeri erken 17. yy ortalamasindan turetildi; belirli bir "
                "eve karsilik gelmez.")

# (ad, gerekce, parametreler)
#
# `facades="sides"` olanlar KOSE evleridir — Caner'in karari (2026-08-20):
# bitisik nizamda yan pencere yok, ama kose ve ara sokak evlerinde var.
VARIANTS = [
    ("A_Dar",        "dar cepheli sokak evi — en yaygin tip",
     dict(floors=2, width=5.6, depth=6.0, cumba_type="flat", cumba=0.55)),
    ("B_Orta",       "orta boy, payandali cumba",
     dict(floors=2, width=7.0, depth=6.5, cumba_type="corbel", cumba=0.80)),
    ("C_Genis",      "genis cepheli, varlikli hane",
     dict(floors=2, width=9.0, depth=7.4, cumba_type="corbel", cumba=0.95)),
    ("D_Tek",        "tek katli kucuk ev — arka sokak",
     dict(floors=1, width=5.4, depth=5.2, cumba_type="none", plinth=0.45)),
    ("E_TekGenis",   "tek katli ama genis — atolye/dukkan uzeri degil, konut",
     dict(floors=1, width=7.6, depth=6.0, cumba_type="none", plinth=0.5)),
    ("F_Uc",         "uc katli — nadir, ana sokak uzeri",
     dict(floors=3, width=7.2, depth=6.6, cumba_type="corbel", cumba=0.85)),
    ("G_UcDar",      "uc katli dar — sikisik parselde yukari buyume",
     dict(floors=3, width=5.8, depth=6.8, cumba_type="flat", cumba=0.6)),
    ("H_Kose",       "kose evi — iki cephesi sokaga bakar",
     dict(floors=2, width=7.4, depth=7.0, cumba_type="corner", cumba=0.8,
          facades="sides")),
    ("I_KoseUc",     "uc katli kose evi",
     dict(floors=3, width=6.8, depth=6.4, cumba_type="corner", cumba=0.75,
          facades="sides")),
    ("J_Sade",       "cumbasiz sade ev — mutevazi hane",
     dict(floors=2, width=6.2, depth=5.8, cumba_type="none", plinth=0.55)),
    ("K_Yuksek",     "yuksek subasman — yamac parseli",
     dict(floors=2, width=6.6, depth=6.2, cumba_type="flat", cumba=0.6,
          plinth=0.95)),
    ("L_DikCati",    "dik cati — kar/yagmur; kitin egim ucu",
     dict(floors=2, width=7.0, depth=6.4, cumba_type="corbel", cumba=0.8,
          roof_pitch_deg=38.0)),
    ("M_YatikCati",  "yatik cati — kitin diger egim ucu",
     dict(floors=2, width=7.2, depth=6.6, cumba_type="flat", cumba=0.7,
          roof_pitch_deg=24.0)),
    ("N_GenisSacak", "cok genis sacak — sokagi golgeler",
     dict(floors=2, width=6.8, depth=6.2, cumba_type="corbel", cumba=0.8,
          eave=0.95)),
    ("O_DarSacak",   "dar sacak",
     dict(floors=2, width=6.4, depth=6.0, cumba_type="flat", cumba=0.6,
          eave=0.45)),
    ("P_Gayri",      "gayrimuslim mahalle evi — daha alcak ve koyu",
     dict(floors=2, width=6.6, depth=6.2, cumba_type="flat", cumba=0.5,
          palette="nonmuslim")),
    ("R_GayriDar",   "gayrimuslim, dar parsel",
     dict(floors=2, width=5.5, depth=6.4, cumba_type="none",
          palette="nonmuslim", plinth=0.5)),
    ("S_GayriKose",  "gayrimuslim kose evi",
     dict(floors=2, width=7.0, depth=6.8, cumba_type="corner", cumba=0.5,
          palette="nonmuslim", facades="sides")),
    # Gayrimuslim mahalle 3 varyanttan 9'a cikarildi.
    #
    # Balat sahnesi kurulunca ~80 ev 3 kaliptan uretilecekti; ayni evin sokak
    # boyunca 27 kez tekrari, dokunun organikligini kazanmak icin yapilan her
    # seyi tek basina bozar. Muslim mahallenin 17 varyantina karsi 3, doku
    # borcuydu. Yeni altisi ayni tipoloji ailesinden: daha alcak, koyu ahsap,
    # cumbasi olcusuz degil olculu.
    ("V_GayriTek",   "gayrimuslim tek katli — arka sokak",
     dict(floors=1, width=5.2, depth=5.6, cumba_type="none",
          palette="nonmuslim", plinth=0.45)),
    ("Y_GayriGenis", "gayrimuslim genis cepheli — varlikli hane",
     dict(floors=2, width=8.4, depth=7.0, cumba_type="corbel", cumba=0.7,
          palette="nonmuslim")),
    ("Z_GayriUc",    "gayrimuslim uc katli — Balat'in dik parseli",
     dict(floors=3, width=5.8, depth=6.6, cumba_type="flat", cumba=0.5,
          palette="nonmuslim")),
    ("AA_GayriYuksek", "gayrimuslim yuksek subasman — yamac parseli",
     dict(floors=2, width=6.4, depth=6.0, cumba_type="flat", cumba=0.5,
          palette="nonmuslim", plinth=0.95)),
    ("AB_GayriSade", "gayrimuslim cumbasiz sade ev",
     dict(floors=2, width=6.0, depth=5.8, cumba_type="none",
          palette="nonmuslim", plinth=0.6)),
    ("AC_GayriSeyrek", "gayrimuslim seyrek pencereli — sagir cepheye yakin",
     dict(floors=2, width=6.8, depth=6.2, cumba_type="flat", cumba=0.45,
          palette="nonmuslim", window_density=0.35)),
    ("T_SikPencere", "sik pencereli cephe — kalabalik hane",
     dict(floors=2, width=8.2, depth=6.6, cumba_type="corbel", cumba=0.85,
          window_density=0.75)),
    ("U_SeyrekPencere", "seyrek pencereli — sagir cepheye yakin",
     dict(floors=2, width=7.0, depth=6.4, cumba_type="flat", cumba=0.6,
          window_density=0.35)),
]


# =====================================================================
# TOHUMDAN UREYEN AILE
# =====================================================================

#: Kaç kalıptan üretiliyorsa, sokakta o kadar tekrar var.
#:
#: Ölçüldü: 26 varyant, 10.868 ev — **varyant başına 418 tekrar**. Örnek
#: başına değişen tek şey ±6° yaw ve birkaç santim geri çekilme. Caner'in
#: isteği açık: *"ev çeşitliliğini çok yüksek tut, benzerlik olsa bile
#: hiçbir ev birbirinin aynısı olmasın."*
#:
#: Yukarıdaki 26 varyant **çapa**dır: her biri bir tipolojik durumu
#: temsil eder ve elle yazılmış olması bilerekdir. Aile onların yerine
#: geçmez, aralarını doldurur.
#:
#: ## Dağılım nereden geliyor
#:
#: RESEARCH.md §4.1(e): 18. yy yapı taban alanları 36–715 zira²,
#: **%80'i 300 zira² (≈172 m²) altında**. Sıradan mahalle evi dağılımın
#: alt-orta bandındadır; konak ve saray bu kitin işi değil. Bu yüzden
#: taban alanı 28–95 m² arasında, **medyanı 42 m²** olan sağa çarpık bir
#: dağılımdan örneklenir — birkaç büyük ev çıkar, çoğu küçük kalır.
#:
#: Rastgele bir parametre taraması aynı sayıyı verirdi ama şehir
#: dokusunu vermezdi. Buradaki her aralık bir gerekçe taşıyor.
AILE_KURALLARI = {
    "taban_alan_m2": "28-95, medyan 42, saga carpik (RESEARCH 4.1e)",
    "en_boy_orani": "0.72-1.55; dar cephe daha sik, cunku sokak cephesi pahali",
    "kat": "2 kat %70, 1 kat %22, 3 kat %8 — uc kat nadir, ana sokak uzeri",
    "cumba": "yok %16, duz %44, payandali %34, kose %6",
    "palet": "gayrimuslim %18",
    "kose": "%14 — iki cephesi sokaga bakan parsel",
}


def _carpik(rng, alt, medyan, ust):
    """Sağa çarpık örnekleme: küçükler sık, büyükler seyrek."""
    u = rng.random()
    if u < 0.5:
        return alt + (medyan - alt) * (u / 0.5) ** 0.85
    return medyan + (ust - medyan) * ((u - 0.5) / 0.5) ** 2.1


def _sec(rng, secenekler):
    """(deger, olasilik) listesinden secer."""
    u = rng.random()
    top = 0.0
    for deger, p in secenekler:
        top += p
        if u <= top:
            return deger
    return secenekler[-1][0]


def aile_uret(sayi, tohum):
    """`sayi` kadar tohumlanmış varyant döndürür: `[(ad, neden, params)]`."""
    import random

    cikti = []
    for i in range(sayi):
        rng = random.Random(tohum * 100003 + i)

        alan = _carpik(rng, 28.0, 42.0, 95.0)
        oran = _carpik(rng, 0.72, 1.02, 1.55)      # en / derinlik
        derin = (alan / oran) ** 0.5
        en = alan / derin

        kat = _sec(rng, [(2, 0.70), (1, 0.22), (3, 0.08)])
        cumba_tip = _sec(rng, [("flat", 0.44), ("corbel", 0.34),
                               ("none", 0.16), ("corner", 0.06)])

        # TEK KATLI EVDE CUMBA OLMAZ.
        #
        # Cumba tanimi geregi UST KATIN cikmasidir; tek katli evde
        # cikacak ust kat yoktur. Kural yazilmayinca uretim bunu kendi
        # soyledi: `Aile_000` tek katli + payandali cumba cikti ve
        # payanda zeminin 27 cm altina sarkti — pivot denetimi reddetti.
        #
        # Elle yazilmis varyantlar bunu zaten gozetiyordu (D_Tek,
        # E_TekGenis, V_GayriTek hepsi cumbasiz); ornekleyici o sessiz
        # bilgiyi bilmiyordu. Simdi biliyor.
        if kat == 1:
            cumba_tip = "none"
        kose = cumba_tip == "corner" or rng.random() < 0.10
        palet = "nonmuslim" if rng.random() < 0.18 else "default"

        p = dict(
            floors=kat,
            width=round(en, 2),
            depth=round(derin, 2),
            floor_height=round(rng.uniform(2.55, 2.95), 2),
            plinth=round(rng.uniform(0.30, 0.95), 2),
            cumba_type=cumba_tip,
            cumba=0.0 if cumba_tip == "none"
                  else round(rng.uniform(0.42, 1.05), 2),
            window_density=round(rng.uniform(0.38, 0.72), 2),
            window_width=round(rng.uniform(0.66, 0.90), 2),
            kafes_bars=rng.randint(3, 5),
            eave=round(rng.uniform(0.48, 0.95), 2),
            roof_pitch_deg=round(rng.uniform(25.0, 37.0), 1),
            palette=palet,
        )
        if kose:
            p["facades"] = "sides"

        etiket = (f"{kat}k {en:.1f}x{derin:.1f} m"
                  + (" kose" if kose else "")
                  + (" gayrimuslim" if palet == "nonmuslim" else ""))
        cikti.append((f"Aile_{i:03d}",
                      f"tohumlu aile uyesi — {etiket}", p))
    return cikti


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"), help="FBX inis alani")
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "variants"),
                    help="Kanonik .blend klasoru")
    ap.add_argument("--detail", default="near", choices=kit.DETAIL_LEVELS)
    ap.add_argument("--no-textures", action="store_true")
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "variants",
                                                      "catalog.json"))
    ap.add_argument("--aile", type=int, default=0,
                    help="Tohumdan uretilecek ek varyant sayisi")
    ap.add_argument("--tohum", type=int, default=1632,
                    help="Aile tohumu — ayni tohum ayni aileyi verir")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    hepsi = list(VARIANTS)
    if args.aile > 0:
        hepsi += aile_uret(args.aile, args.tohum)
        hz.log(f"aile: {args.aile} varyant, tohum {args.tohum}")

    for name, why, params in hepsi:
        asset = f"House_{name}"
        hz.reset_scene()
        col = hz.collection(COLLECTION)

        p = kit.HouseParams(detail=args.detail, window_detail="kafes",
                            **params).apply_palette_rules()
        lod0, lod1, lod2, ucx, info = kit.build_house(
            p, col, asset, textured=not args.no_textures)

        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {asset}: pivot taban merkezde degil "
                             f"({info['pivot_min_z']})")

        blend = os.path.join(args.blend_dir, f"SM_{asset}.blend")
        hz.save_blend(blend)
        export_fbx(os.path.join(args.out_dir, f"SM_{asset}.fbx"),
                   collection_name=COLLECTION)

        info.update(name=asset, why=why, prefab=f"PF_{asset}",
                    tier=HOUSE_TIER, source=HOUSE_SOURCE)
        catalog.append(info)
        hz.log(f"{asset:22s} {info['footprint_x']:5.2f}x{info['footprint_y']:5.2f}"
               f"x{info['height']:5.2f} m  {info['tris_lod0']:5d} ucgen  {why}")

    # Katalog: Unity yerlestiricisi hangi varyantin ne oldugunu buradan okur.
    # Olculer TEK yerde yasar; Unity tarafinda elle tekrarlanmaz.
    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)

    tri = sum(v["tris_lod0"] for v in catalog)
    hz.log(f"{len(catalog)} varyant; LOD0 toplam {tri} ucgen, "
           f"ortalama {tri // max(len(catalog), 1)}")
    hz.log(f"katalog: {args.catalog}")


if __name__ == "__main__":
    main()
